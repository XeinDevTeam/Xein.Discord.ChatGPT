// See https://aka.ms/new-console-template for more information

using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace Xein.Discord.ChatGPT;

internal class Config
{
    public string ApiKey { get; init; }
}

internal class Program
{
    private static Config conf;

    public static void Main(string[] args) => new Program().Async(args).GetAwaiter().GetResult();

    private async Task Async(string[] args)
    {
        Console.Log("Hello World");

        if (!File.Exists("config.json"))
            await File.WriteAllTextAsync("config.json", new Config
            {
                ApiKey = "Enter Your API Key From ChatGPT",
            }.GetJson());

        Console.Log("Reading Config");
        conf = File.ReadAllText("config.json").FromJson<Config>();

        Console.Log($"Init OpenAI");
        var openAI = new OpenAIService(new()
        {
            ApiKey = conf.ApiKey,
        });

        while (true)
        {
            var input = System.Console.ReadLine();

            if (input == "exit")
                break;
            
            Console.Debug($"Asking ChatGPT: {input}");

            var result = await openAI.ChatCompletion.CreateCompletion(new()
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromUser(input),
                },
                Model = Models.ChatGpt3_5Turbo,
            });
        
            Console.Debug($"ChatGPT result: {result.Successful}, ErrorCode: {result.Error?.Code} ErrorMessage: {result.Error?.Message}");
        
            if (result.Successful)
                foreach (var choice in result.Choices)
                    Console.Log($"ChatGPT replied: [Role '{choice.Message.Role}'] {choice.Message.Content}");
        }
    }
}
