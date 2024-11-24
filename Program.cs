using System.Text;
using System.Text.RegularExpressions;

namespace MarkovChainSentences;

public class TextGenerator
{
  private readonly static Random random = new();
  private Index index = new Index();

  public static void Main()
  {
    var generator = TextGenerator.From("input.txt");

    var text = generator.GenerateParagraphs(3, 5).ToString().Trim();

    Console.WriteLine(text);
  }

  public static TextGenerator From(string path)
  {
    var generator = new TextGenerator();
    generator.index.IndexFile(path);
    return generator;
  }

  public StringBuilder GenerateParagraphs(int min = 1, int? max = default, string? initialWord = default)
  {
    max ??= min;
    var sb = new StringBuilder();
    for (int i = 0; i < random.Next(min, max.Value); i++)
    {
      sb.Append(GenerateParagraph(initialWord)).Append(Environment.NewLine).Append(Environment.NewLine);
    }
    return sb;
  }

  public StringBuilder GenerateParagraph(string? initialWord = default)
  {
    var sb = new StringBuilder();
    for (int i = 0; i < random.Next(2, 10); i++)
    {
      sb.Append(GenerateSentence(initialWord)).Append(' ');
    }
    return sb;
  }

  public StringBuilder GenerateSentence(string? initialWord = default)
  {
    initialWord ??= index.RandomToken();

    var sb = new StringBuilder(initialWord).Append(' ');
    string currentWord = initialWord;
    try
    {
      for (int i = 0; i < new Random().Next(5, 30); i++)
      {
        currentWord = index.RandomNextToken(currentWord);
        sb.Append(currentWord).Append(' ');
      }
    }
    catch (ArgumentException)
    {
      Console.WriteLine("Failed to get the next word. Will end the sentence immediately.");
    }

    sb.Append(index.RandomSentenceTerminatingToken()).Append(".");

    // Capitalize sentence
    sb[0] = char.ToUpper(sb[0]);
    return sb;
  }
}

class Index
{
  // token -> list of all tokens that appear after the token
  // todo: deal with the duplicates, it would be nice to have a weighted list instead
  // e.g., https://en.wikipedia.org/wiki/Alias_method, https://github.com/cdanek/KaimiraWeightedList?tab=readme-ov-file
  private readonly Dictionary<string, List<string>> bigrams = new();
  private readonly List<string> sentenceTerminators = new List<string>();

  // TODO: Index a directory or a file
  public void IndexFile(string filename)
  {
    using var sr = new StreamReader(filename);
    var content = sr.ReadToEnd();

    IndexBigrams(content);
    IndexSentenceTerminatingTokens(content);
  }

  /// <summary>
  /// Create a word -> list of words mapping. When "predicting" the next token, we will sample from that list.
  /// </summary>
  /// <param name="content"></param>
  private void IndexBigrams(string content)
  {
    var tokens = Tokenizer.Tokenize(content);
    for (int i = 0; i < tokens.Count - 1; i++)
    {
      var current = tokens.ElementAt(i);
      var next = tokens.ElementAt(i + 1);
      
      var adjacentTokens = bigrams.GetValueOrDefault(current, new List<string>());
      adjacentTokens.Add(next);
      bigrams[current] = adjacentTokens;
    }
  }

  private void IndexSentenceTerminatingTokens(string content)
  {
    // find words right before sentence terminating characters
    Regex rx = new(@"\b(?<word>\w+)[!.?;]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    MatchCollection matches = rx.Matches(content);

    sentenceTerminators.AddRange(matches.Select(match => match.Groups["word"].Value));
  }

  public string RandomSentenceTerminatingToken() => sentenceTerminators.RandomItem();

  public string RandomToken() => bigrams.RandomItem().Key;

  public string RandomNextToken(string token) {
    if (!bigrams.TryGetValue(token, out var candidates))
    {
      throw new ArgumentException($"There are no bigrams for {token}");
    }
    return candidates.RandomItem();
  }
}

static class Tokenizer
{
  private static string[] wordSeparators = new string[] { "!", "\"", "#", "$", "%", "&", "'", ",", "*", "+", ",", "-", ".", "/", ":", ";", "<", "=", ">", "?", "@", "[", "\\", "]", "^", "_", "`", "{", "|", "}", "~", "(", ")", " ", Environment.NewLine };

  public static ICollection<string> Tokenize(string text)
  {
    return text.ToLower().Split(wordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }
}

static class Extensions 
{
  private readonly static Random random = new();
  public static T RandomItem<T>(this ICollection<T> collection)
  {
    return collection.ElementAt(random.Next(collection.Count));
  }
}