using OpenAI.GPT3.ObjectModels.ResponseModels;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

#pragma warning disable CS8618
#pragma warning disable CS8603

namespace Xein.Discord.ChatGPT;

public static class Extension
{
    private static JsonSerializerOptions jsoMin;
    private static JsonSerializerOptions jso;
    private static JsonSerializerOptions JsonOptionsMinify() { return jsoMin ??= new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), }; }
    private static JsonSerializerOptions DefaultJsonOptions() { return jso ??= new() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), }; }

    public static string GetJson<T>(this T type, bool beautify = false) => JsonSerializer.Serialize(type, beautify ? DefaultJsonOptions() : JsonOptionsMinify());
    public static T FromJson<T>(this string json) => JsonSerializer.Deserialize<T>(json);

    public static IEnumerable<float> GetScores(this Result result)
    {
        yield return result.CategoryScores.Hate;
        yield return result.CategoryScores.HateThreatening;
        yield return result.CategoryScores.Selfharm;
        yield return result.CategoryScores.Sexual;
        yield return result.CategoryScores.SexualMinors;
        yield return result.CategoryScores.Violence;
        yield return result.CategoryScores.Violencegraphic;
    }

    public static IEnumerable<bool> GetCategories(this Result result)
    {
        yield return result.Categories.Hate;
        yield return result.Categories.HateThreatening;
        yield return result.Categories.Selfharm;
        yield return result.Categories.Sexual;
        yield return result.Categories.Sexualminors;
        yield return result.Categories.Violence;
        yield return result.Categories.Violencegraphic;
    }

    public static string GetFormat(this DateTime time) => $"[{time.ToShortDateString()} {time.ToShortTimeString()}]";

    public static bool IsEmpty(this string s) => string.IsNullOrWhiteSpace(s) || string.IsNullOrEmpty(s);

    public static bool IsNumeric(this string str)
    {
        if (str.IsEmpty())
            return false;

        foreach (var c in str)
            if (!char.IsNumber(c))
                return false;

        return true;
    }
}
