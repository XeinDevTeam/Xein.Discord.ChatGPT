using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace Xein.Discord.ChatGPT;

public static class OpenAIManager
{
    public static OpenAIService openAI;

    public static void Init()
    {
        Console.Log($"Init OpenAI");
        openAI = new OpenAIService(new() { ApiKey = ConfigManager.Config.ChatGPTApiKey, });
    }
    
    public static async Task<(bool successful, string message)> Translate(string language, string toTranslate)
    {
        Thread.Sleep(100);

        try
        {
            var result = await openAI.ChatCompletion.CreateCompletion(new() { Messages = new List<ChatMessage> { ChatMessage.FromUser($"翻译成{language}: {toTranslate}"), }, Model = Models.ChatGpt3_5Turbo, });

            var retResult = result.Successful;
            var message   = retResult ? result.Choices.First().Message.Content : $"Error: {result.Error.Code}, {result.Error.Message}";

            // logging for training purpose?
            // TODO: implement MachineLearning
            await File.AppendAllTextAsync("translated.txt", new Translated() { Language = language, ToTranslate = toTranslate, FinalResult = message, }.GetJson() + Environment.NewLine, System.Text.Encoding.Unicode);

            Console.Debug($"[Translate] Result: {retResult}, Message: {message}");
            
            // Check more failed cases
            if (message.Contains("help.openai.com"))
                return (false, "翻译出现错误, OpenAI伺服器太多请求或者有其他问题发生");
            else
                return (retResult, message);
        }
        catch (Exception ex)
        {
            Console.Error(ex.Format());
            return (false, "翻译出现错误，请叫 @xein0708 看后台并且修复");
        }
    }

    public static async Task<(bool flag, IEnumerable<bool> categories, IEnumerable<float> scores)> Moderation(string message)
    {
        var result = await openAI.CreateModeration(new CreateModerationRequest() { Input = message, });
        return (result.Results.First()?.Flagged ?? false, result.Results.First()?.GetCategories() ?? Array.Empty<bool>(), result.Results.First()?.GetScores() ?? Array.Empty<float>());
    }
}
