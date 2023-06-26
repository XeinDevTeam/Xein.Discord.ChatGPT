using OpenAI.GPT3.ObjectModels.ResponseModels;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

using TwitchLib.Client.Models;

#pragma warning disable CS8618
#pragma warning disable CS8603

namespace Xein.Discord.ChatGPT;

public static class Extension
{
    private static JsonSerializerOptions jsoMin;
    private static JsonSerializerOptions jso;
    private static JsonSerializerOptions JsonOptionsMinify() { return jsoMin ??= new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), }; }
    private static JsonSerializerOptions DefaultJsonOptions() { return jso ??= new() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), }; }

    public static string GetJson<T>(this  T      type, bool beautify = false) => JsonSerializer.Serialize(type, beautify ? DefaultJsonOptions() : JsonOptionsMinify());
    public static T      FromJson<T>(this string json, bool isFile   = false) => JsonSerializer.Deserialize<T>(isFile ? File.ReadAllText(json) : json);

    public static IEnumerable<float> GetScores(this Result result)
    {
        yield return result.CategoryScores.Hate;
        yield return result.CategoryScores.HateThreatening;
        yield return result.CategoryScores.SelfHarm;
        yield return result.CategoryScores.Sexual;
        yield return result.CategoryScores.SexualMinors;
        yield return result.CategoryScores.Violence;
        yield return result.CategoryScores.ViolenceGraphic;
    }

    public static IEnumerable<bool> GetCategories(this Result result)
    {
        yield return result.Categories.Hate;
        yield return result.Categories.HateThreatening;
        yield return result.Categories.SelfHarm;
        yield return result.Categories.Sexual;
        yield return result.Categories.SexualMinors;
        yield return result.Categories.Violence;
        yield return result.Categories.ViolenceGraphic;
    }

    public static bool IsAbleToUse(this ChatMessage chatMsg) => chatMsg.IsModerator || chatMsg.IsBroadcaster || string.Compare(chatMsg.Username, "xein0708", StringComparison.InvariantCultureIgnoreCase) == 0;
    
    public static string Format(this Exception e) => $"Exception: {e}\n{e.Message}\nStacktrace:\n{e.StackTrace}";

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
