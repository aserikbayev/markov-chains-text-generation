using System.Text.RegularExpressions;

namespace MarkovChainSentences;

public class Program
{
  public static void Main()
  {
    var generator = TextGenerator.From("input.txt");

    var text = generator.GenerateParagraphs(3, 5).ToString().Trim();

    Console.WriteLine(text);
  }
}

public class TextGenerator
{
  private readonly Random _random = new();
  private readonly Index _index = new();

  private const int MinSentenceWords = 5;
  private const int MaxSentenceWords = 20;
  private const int MinSentencesPerParagraph = 2;
  private const int MaxSentencesPerParagraph = 10;

  public static TextGenerator From(string path)
  {
    if (!File.Exists(path))
    {
      throw new FileNotFoundException("Input file not found", path);
    }

    var generator = new TextGenerator();
    generator._index.IndexFile(path);
    return generator;
  }

  public string GenerateParagraphs(int min = 1, int? max = default, string? initialWord = default)
  {
    max ??= min;

    var paragraphs = Enumerable.Range(0, _random.Next(min, max.Value))
        .Select(_ => GenerateParagraph(initialWord));

    return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
  }

  public string GenerateParagraph(string? initialWord = default)
  {
    var sentences = Enumerable.Range(MinSentencesPerParagraph, MaxSentencesPerParagraph + 1)
      .Select(_ => GenerateSentence(initialWord));

    return string.Join(' ', sentences);
  }

  public string GenerateSentence(string? initialWord = default)
  {
    initialWord ??= _index.RandomToken();
    var words = new List<string> { initialWord };

    string currentWord = initialWord;
    var sentenceLength = _random.Next(MinSentenceWords, MaxSentenceWords + 1);
    for (int i = 0; i < sentenceLength; i++)
    {
      try
      {
        currentWord = _index.RandomNextToken(currentWord);
        words.Add(currentWord);

      }
      catch (ArgumentException)
      {
        break; // End sentence early if no next token is found
      }
    }

    words.Add(_index.RandomSentenceTerminatingToken() + ".");
    words[0] = char.ToUpper(words[0][0]) + words[0][1..]; // Capitalize sentence
    return string.Join(" ", words);
  }
}

class Index
{
  // token -> list of all tokens that appear after the token
  // todo: deal with the duplicates, it would be nice to have a weighted list instead
  // e.g., https://en.wikipedia.org/wiki/Alias_method, https://github.com/cdanek/KaimiraWeightedList?tab=readme-ov-file
  private readonly Dictionary<string, List<string>> _bigrams = new();
  private readonly List<string> _sentenceTerminators = new List<string>();

  // TODO: Index a directory or a file
  public void IndexFile(string filename)
  {
    var content = File.ReadAllText(filename);
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

      var adjacentTokens = _bigrams.GetValueOrDefault(current, new List<string>());
      adjacentTokens.Add(next);
      _bigrams[current] = adjacentTokens;
    }
  }

  private void IndexSentenceTerminatingTokens(string content)
  {
    // find words right before sentence terminating characters
    var rx = new Regex(@"\b(?<word>\w+)[!.?;]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    foreach(Match match in rx.Matches(content))
    {
      _sentenceTerminators.Add(match.Groups["word"].Value);
    }
  }

  public string RandomSentenceTerminatingToken() => _sentenceTerminators.RandomItem();
  public string RandomToken() => _bigrams.RandomItem().Key;
  public string RandomNextToken(string token) => 
    _bigrams.GetValueOrDefault(token)?.RandomItem() 
      ?? throw new ArgumentException($"No bigrams found for {token}");
}

static class Tokenizer
{
  private static readonly char[] WordSeparators = "!\"#$%&'*+,-./:;<=>?@[\\]^_`{|}~() \r\n\t".ToCharArray();

  public static ICollection<string> Tokenize(string text) => text.ToLower()
      .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static class Extensions
{
  private readonly static Random random = new();
  public static T RandomItem<T>(this ICollection<T> collection) => collection.ElementAt(random.Next(collection.Count));
}