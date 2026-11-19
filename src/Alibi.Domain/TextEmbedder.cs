using System.Globalization;
using System.Text;

namespace Alibi.Domain;

/// <summary>
/// Embedder determinístico e 100% offline (feature hashing).
/// Transforma texto em português num vetor normalizado de <see cref="Dimensions"/> posições,
/// combinando radicais de palavras e trigramas de caracteres.
/// Não é um modelo de linguagem — é o suficiente para a busca vetorial do SQL Server
/// casar "onde você estava às nove?" com "Às 21h eu estava na biblioteca", sem chave de API.
/// </summary>
public static class TextEmbedder
{
    public const int Dimensions = 256;

    private static readonly HashSet<string> StopWords =
    [
        "a","o","as","os","um","uma","uns","umas","de","do","da","dos","das","em","no","na","nos","nas",
        "por","pelo","pela","para","pra","com","sem","e","ou","que","se","me","te","lhe","voce","vc",
        "eu","ele","ela","eles","elas","isso","isto","aquilo","esse","essa","este","esta","foi","era",
        "ser","estar","ao","aos","mas","mais","muito","ja","tambem","sr","sra","senhor","senhora","ai","la","ha","so"
    ];

    // Sinônimos simples para o domínio do jogo (horários e palavras de interrogatório).
    private static readonly Dictionary<string, string> Synonyms = new()
    {
        ["nove"] = "21h", ["21"] = "21h", ["2100"] = "21h", ["21h00"] = "21h",
        ["oito"] = "20h", ["20"] = "20h", ["2000"] = "20h", ["20h00"] = "20h",
        ["dez"] = "22h", ["22"] = "22h", ["2200"] = "22h", ["22h00"] = "22h",
        ["2030"] = "20h30", ["2130"] = "21h30", ["2230"] = "22h30",
        ["onde"] = "local", ["lugar"] = "local", ["estava"] = "local",
        ["vitima"] = "vitima", ["morto"] = "vitima", ["morta"] = "vitima", ["falecido"] = "vitima",
        ["relacao"] = "relacao", ["conhecia"] = "relacao", ["relacionamento"] = "relacao",
        ["inimigo"] = "inimigo", ["odiava"] = "inimigo", ["briga"] = "inimigo", ["discussao"] = "inimigo",
        ["viu"] = "testemunha", ["vista"] = "testemunha", ["visto"] = "testemunha", ["notou"] = "testemunha", ["estranho"] = "testemunha",
    };

    public static float[] Embed(string text)
    {
        var v = new float[Dimensions];
        foreach (var token in Tokenize(text))
        {
            Add(v, "w:" + token, 1.0f);
            var padded = $"^{token}$";
            for (var i = 0; i + 3 <= padded.Length; i++)
                Add(v, "c:" + padded.Substring(i, 3), 0.25f);
        }

        var norm = MathF.Sqrt(v.Sum(x => x * x));
        if (norm > 0) for (var i = 0; i < v.Length; i++) v[i] /= norm;
        return v;
    }

    public static double CosineDistance(float[] a, float[] b)
    {
        double dot = 0;
        for (var i = 0; i < a.Length; i++) dot += a[i] * b[i];
        return 1 - dot; // vetores já normalizados
    }

    public static IEnumerable<string> Tokenize(string text)
    {
        var normalized = RemoveDiacritics(text.ToLowerInvariant());
        var sb = new StringBuilder();
        foreach (var ch in normalized + " ")
        {
            if (char.IsLetterOrDigit(ch)) { sb.Append(ch); continue; }
            if (sb.Length == 0) continue;
            var w = sb.ToString(); sb.Clear();
            if (StopWords.Contains(w)) continue;
            // "21h", "21:00", "21h30" → normaliza horários
            w = w.Replace(":", "");
            if (Synonyms.TryGetValue(w, out var syn)) w = syn;
            yield return Stem(w);
        }
    }

    // Radical grosseiro: corta sufixos comuns e limita a 6 caracteres.
    private static string Stem(string w)
    {
        if (w.Any(char.IsDigit)) return w;
        foreach (var suffix in new[] { "mente", "coes", "cao", "ndo", "ram", "va", "s" })
            if (w.Length > suffix.Length + 3 && w.EndsWith(suffix)) { w = w[..^suffix.Length]; break; }
        return w.Length > 6 ? w[..6] : w;
    }

    private static void Add(float[] v, string feature, float weight)
    {
        // FNV-1a 32 bits: índice + sinal
        uint h = 2166136261;
        foreach (var c in feature) { h ^= c; h *= 16777619; }
        var index = (int)(h % Dimensions);
        var sign = (h & 0x80000000) == 0 ? 1f : -1f;
        v[index] += sign * weight;
    }

    private static string RemoveDiacritics(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
