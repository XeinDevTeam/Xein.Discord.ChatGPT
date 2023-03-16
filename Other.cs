using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Xein.Discord.ChatGPT;

public static class Extension
{
    private static JsonSerializerOptions jsoMin;
    private static JsonSerializerOptions jso;
    private static JsonSerializerOptions JsonOptionsMinify()
    {
        return jsoMin ??= new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };
    }
    private static JsonSerializerOptions DefaultJsonOptions()
    {
        return jso ??= new()
        {
            WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };
    }

    public static string GetJson<T>(this T type) => JsonSerializer.Serialize(type, JsonOptionsMinify());
    public static T FromJson<T>(this string json) => JsonSerializer.Deserialize<T>(json);
}
