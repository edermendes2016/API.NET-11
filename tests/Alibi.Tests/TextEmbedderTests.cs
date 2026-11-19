using Alibi.Domain;

namespace Alibi.Tests;

public class TextEmbedderTests
{
    [Fact]
    public void Embeddings_are_normalized_and_deterministic()
    {
        var a = TextEmbedder.Embed("Onde você estava às 21h?");
        var b = TextEmbedder.Embed("Onde você estava às 21h?");

        Assert.Equal(TextEmbedder.Dimensions, a.Length);
        Assert.Equal(a, b);
        Assert.Equal(1.0, Math.Sqrt(a.Sum(x => x * x)), 3);
    }

    [Fact]
    public void Paraphrases_are_closer_than_unrelated_questions()
    {
        var reference = TextEmbedder.Embed("onde voce estava as 21h | as nove horas onde estava");
        var paraphrase = TextEmbedder.Embed("Às nove da noite, você estava onde?");
        var unrelated = TextEmbedder.Embed("Qual a capital da França?");

        Assert.True(TextEmbedder.CosineDistance(reference, paraphrase) < TextEmbedder.CosineDistance(reference, unrelated));
    }

    [Theory]
    [InlineData("Às 21:00", "21h")]
    [InlineData("VÍTIMA", "vitima")]
    public void Tokenizer_normalizes_times_and_accents(string input, string expected) =>
        Assert.Contains(expected, TextEmbedder.Tokenize(input));
}
