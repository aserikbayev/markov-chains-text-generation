using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarkovChain;

public class MarkovChainSentences
{
  private readonly static Random random = new();
  private readonly Dictionary<string, List<string>> bigrams = new();
  private readonly List<string> endWords = new();

  public static void Main()
  {
    new MarkovChainSentences().Run();
  }

  public void Run()
  {
    const string inputFileName = "input.txt";

    this.Initialize(inputFileName);

    string text = MakeText().ToString();
    Console.WriteLine(text);
  }

  private void Initialize(string filename)
  {
    using var sr = new StreamReader(filename);
    string content = sr.ReadToEnd();
    string[] wordSeparators = new string[] { "!", "\"", "#", "$", "%", "&", "'", ",", "*", "+", ",", "-", ".", "/", ":", ";", "<", "=", ">", "?", "@", "[", "\\", "]", "^", "_", "`", "{", "|", "}", "~", "(", ")", " ", Environment.NewLine };
    var words = content.ToLower().Split(wordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    for (int i = 0; i < words.Length - 1; i++)
    {
      List<string> nextWords = bigrams.GetValueOrDefault(words[i], new());
      nextWords.Add(words[i + 1]);
      bigrams[words[i]] = nextWords;
    }

    GetEndWords(content);
  }

  private void GetEndWords(string content)
  {
    string[] sentenceSeparators = new string[] { "!", ".", "?", ";" };
    Regex rx = new(@"\b(?<word>\w+)[!.?;]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    MatchCollection matches = rx.Matches(content);

    endWords.AddRange(matches.Select(match => match.Groups["word"].Value));
  }

  private StringBuilder MakeText()
  {
    var sb = MakeParagraph();
    for (int i = 0; i < random.Next(2, 5); i++)
    {
      sb.Append(Environment.NewLine).Append(Environment.NewLine).Append(MakeParagraph());
    }

    return sb;
  }

  private StringBuilder MakeParagraph()
  {
    var sb = MakeSentence(GetRandomWord());
    // Generate 2 to 10 sentences.
    for (int i = 0; i < random.Next(2, 10); i++)
    {
      string initialWord = GetRandomWord();
      sb.Append(' ').Append(MakeSentence(initialWord));
    }

    return sb;
  }

  private string GetRandomWord()
  {
    int randomWordIndex = random.Next(bigrams.Count);
    string randomWord = bigrams.ElementAt(randomWordIndex).Key;
    return randomWord;
  }

  private StringBuilder MakeSentence(string initialWord)
  {
    if (initialWord == null)
    {
      throw new ArgumentNullException(nameof(initialWord));
    }
    else if (initialWord.Length == 0)
    {
      throw new ArgumentException("Initial word shouldn't be empty", nameof(initialWord));
    }

    var sb = new StringBuilder(initialWord);
    string currentWord = initialWord;
    try
    {
      for (int i = 0; i < new Random().Next(5, 30); i++)
      {
        currentWord = GetNextWord(currentWord);
        sb.Append(' ').Append(currentWord);
      }
    }
    catch (ArgumentException)
    {
      Console.WriteLine("Failed to get the next word. Will end the sentence immediately.");
    }

    sb.Append(' ').Append(GetSentenceEnding()).Append('.');

    // Capitalize sentence
    sb[0] = char.ToUpper(sb[0]);
    return sb;
  }

  private string GetNextWord(string initialWord)
  {
    if (!bigrams.TryGetValue(initialWord, out var possibleWords))
    {
      throw new ArgumentException("Failed to get words to continue");
    }

    return possibleWords[random.Next(possibleWords.Count)];
  }

  private string GetSentenceEnding()
  {
    return endWords[random.Next(endWords.Count)];
  }
}