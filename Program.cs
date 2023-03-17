// See https://aka.ms/new-console-template for more information

using Google.Cloud.TextToSpeech.V1;

using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace Xein.Discord.ChatGPT;

internal class Config
{
    public string ChatGPTApiKey { get; init; } = "Enter Your API Key From https://platform.openai.com/account/api-keys";
    public string TwitchUsername { get; init; } = "Enter Your Twitch Username";
    public string TwitchOAuthToken { get; init; } = "Enter Your OAuth Token From https://twitchapps.com/tmi/";
    public string GoogleServiceAccountPath { get; init; } = "yourKeyFile.json";
}

internal class Program
{
    private static Config conf;

    private static OpenAIService openAI;

    private static TwitchClient twitchClient;
    private static ConnectionCredentials twitchCredentials;

    public static void Main(string[] args) => new Program().Async(args).GetAwaiter().GetResult();

    private async Task Async(string[] args)
    {
        Console.Log("Hello World");

        if (!File.Exists("config.json"))
            await File.WriteAllTextAsync("config.json", new Config().GetJson(true));

        Console.Log("Reading Config");
        conf = File.ReadAllText("config.json").FromJson<Config>();

        Console.Log($"Init Environment");
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", conf.GoogleServiceAccountPath);

        Console.Log($"Init OpenAI");
        openAI = new OpenAIService(new() { ApiKey = conf.ChatGPTApiKey, });

        /*
         * [GoogleTTS] cmn-CN-Standard-C (Male); Language codes: cmn-CN
         * [GoogleTTS] cmn-CN-Standard-B (Male); Language codes: cmn-CN
         * [GoogleTTS] cmn-CN-Standard-A (Female); Language codes: cmn-CN
         * [GoogleTTS] cmn-CN-Standard-D (Female); Language codes: cmn-CN
         * [GoogleTTS] cmn-TW-Standard-A (Female); Language codes: cmn-TW
         * [GoogleTTS] cmn-TW-Standard-B (Male); Language codes: cmn-TW
         * [GoogleTTS] cmn-TW-Standard-C (Male); Language codes: cmn-TW
         * =============== WAVENET ===============
         * [GoogleTTS] cmn-CN-Wavenet-A (Female); Language codes: cmn-CN
         * [GoogleTTS] cmn-CN-Wavenet-B (Male); Language codes: cmn-CN
         * [GoogleTTS] cmn-CN-Wavenet-C (Male); Language codes: cmn-CN
         * [GoogleTTS] cmn-CN-Wavenet-D (Female); Language codes: cmn-CN
         * [GoogleTTS] cmn-TW-Wavenet-A (Female); Language codes: cmn-TW
         * [GoogleTTS] cmn-TW-Wavenet-B (Male); Language codes: cmn-TW
         * [GoogleTTS] cmn-TW-Wavenet-C (Male); Language codes: cmn-TW
         */

        Console.Log($"Init Google TTS");
        File.Delete("output.mp3");

        var ttsClient = TextToSpeechClient.Create();

        var speechInput = new SynthesisInput { Text = "你好我是 Google Text-To-Speech，这是一个我测试的声音。", };
        var speechSelection = new VoiceSelectionParams { LanguageCode = "cmn-TW", SsmlGender = SsmlVoiceGender.Female, };
        var audioConfig = new AudioConfig { AudioEncoding = AudioEncoding.Mp3, SampleRateHertz = 48000, };
        var speechTest = ttsClient.SynthesizeSpeech(speechInput, speechSelection, audioConfig);

        if (speechTest is not null)
            using (var output = File.Create("output.mp3"))
                speechTest.AudioContent.WriteTo(output);

        if (!File.Exists("output.mp3"))
            Console.Error($"[GoogleTTS] Failed to create a demo output.mp3");

        Console.Log($"Init Twitch");
        twitchCredentials = new(conf.TwitchUsername, conf.TwitchOAuthToken);
        twitchClient = new();

        twitchClient.Initialize(twitchCredentials);

        twitchClient.OnLog += TwitchClient_OnLog;
        twitchClient.OnConnected += TwitchClient_OnConnected;
        twitchClient.OnMessageReceived += TwitchClient_OnMessageReceived;

        Console.Log($"Connecting Twitch");
        twitchClient.Connect();

        Console.Log($"Done Initialize");

        while (true)
        {
            var input = System.Console.ReadLine();
            if (string.Compare(input, "exit", StringComparison.InvariantCultureIgnoreCase) == 0)
                break;

            FindCommand(input.Split(' '), input);
        }

        twitchClient.Disconnect();

        Console.Warn("Sleeping 3 seconds waiting graceful exits");
        Thread.Sleep(3000);
    }

    private async void FindCommand(string[] args, string input)
    {
        if (args is null || args.Length < 1)
        {
            Console.Error("Not Enough Arguments");
            return;
        }

        switch (args[0])
        {
            #region Twitch
            case "twitchjoin":
                {
                    if (args.Length < 2)
                    {
                        Console.Error($"Invalid Usage: twitchjoin <channel>");
                        return;
                    }

                    twitchClient.JoinChannel(args[1]);
                }
                break;
            case "twitchchat":
                {
                    if (args.Length < 3)
                    {
                        Console.Error($"Invalid Usage: twitchchat <channel> <message>");
                        return;
                    }

                    var channel = twitchClient.GetJoinedChannel(args[1]);
                    var message = input[input.IndexOf(args[2])..];

                    if (channel is null)
                    {
                        Console.Error($"Bot not join channel({args[1]}) yet");
                        return;
                    }

                    twitchClient.SendMessage(channel, message);
                }
                break;
            #endregion

            #region ChatGPT
            case "moderation":
                {
                    if (args.Length < 2)
                    {
                        Console.Error($"Invalid Usage: moderation <textToModeration>");
                        return;
                    }

                    string moderationInput = input[input.IndexOf(args[1])..];

                    Console.Debug($"{moderationInput}");

                    var moderation = await openAI.CreateModeration(new CreateModerationRequest() { Input = moderationInput, });
                    Console.Warn($"ChatGPT Moderation: {moderationInput}\n" +
                        $"Flags: {moderation.Results.FirstOrDefault()?.Flagged ?? false}\n" +
                        $"Categories: {string.Join(", ", moderation.Results.FirstOrDefault()?.GetCategories() ?? Array.Empty<bool>())}\n" +
                        $"Level     : {string.Join(", ", moderation.Results.FirstOrDefault()?.GetScores() ?? Array.Empty<float>())}");
                }
                break;

            case "completion":
                {
                    if (args.Length < 2)
                    {
                        Console.Error($"Invalid Usage: completion <toAskChatGPT>");
                        return;
                    }

                    string completionInput = input[input.IndexOf(args[1])..];

                    var result = await openAI.ChatCompletion.CreateCompletion(new()
                    {
                        Messages = new List<OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage>
                        {
                            OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromUser(completionInput),
                        },
                        Model = Models.ChatGpt3_5Turbo,
                    });

                    Console.Debug($"Ask ChatGPT '{completionInput}' result: {result.Successful}");

                    if (result.Successful)
                        foreach (var choice in result.Choices)
                            Console.Log($"ChatGPT replied: [Role '{choice.Message.Role}'] {choice.Message.Content[1..]}");
                    else
                        Console.Error($"ChatGPT returns Error: {result.Error.Code}, {result.Error.Message}");
                }
                break;

            case "translate":
                {
                    if (args.Length < 3)
                    {
                        Console.Error($"Invalid Usage: translate <language> <inputToRequestedLanguage>");
                        return;
                    }

                    string completionInput = input[input.IndexOf(args[2])..];

                    var result = await openAI.ChatCompletion.CreateCompletion(new()
                    {
                        Messages = new List<OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage>
                        {
                            OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromUser($"translate to {args[1]}: {completionInput}"),
                        },
                        Model = Models.ChatGpt3_5Turbo,
                    });

                    Console.Debug($"Ask ChatGPT '{completionInput}' result: {result.Successful}");

                    if (result.Successful)
                        foreach (var choice in result.Choices)
                            Console.Log($"ChatGPT replied: {choice.Message.Content[1..]}");
                    else
                        Console.Error($"ChatGPT returns Error: {result.Error.Code}, {result.Error.Message}");
                }
                break;
            #endregion

            default:
                Console.Error($"Unknown Command");
                break;
        }
    }

    #region Twitch Events
    private async void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        // return if common bots username, commands prefix
        if (e.ChatMessage.Username == "nightbot" ||
            e.ChatMessage.Username == "streamelements" ||
            e.ChatMessage.Message.StartsWith('!'))
            return;

        Console.Log($"[Twitch] [{e.ChatMessage.UserType} '{e.ChatMessage.Username}'] says in '{e.ChatMessage.Channel}': {e.ChatMessage.Message}");

        Console.Debug($"[Twitch] [Chat Debug '{e.ChatMessage.Id}'] IsParentNull: {e.ChatMessage.ChatReply is null} | ParentId: {e.ChatMessage.ChatReply?.ParentMsgId}, ParentMsg: {e.ChatMessage.ChatReply?.ParentMsgBody}");

        // check if replies requires me to translate?
        if (e.ChatMessage.ChatReply is not null)
        {
            // never null in here
            var reply = e.ChatMessage.ChatReply;
            var toTranslate = reply.ParentMsgBody;

            // is user calling me
            var isMention = e.ChatMessage.Message.Contains($"@{conf.TwitchUsername}");

            // split message?
            var split = e.ChatMessage.Message.Split(' ');

            // is Translate exists
            var isTranslate = split.ToList().FindAll(s => string.Compare(s, "translate", StringComparison.InvariantCultureIgnoreCase) == 0).Any();

            Console.Debug($"[Twitch] [Reply Checks] IsMention: {isMention}, IsTranslate: {isTranslate}\nMsg: {toTranslate}\nSplit: {string.Join(" | ", split)}\nExecute: translate to {split[2]}: {toTranslate}");

            if (split.Length >= 3 && isMention && isTranslate)
            {
                var result = await openAI.ChatCompletion.CreateCompletion(new()
                {
                    Messages = new List<OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage>
                        {
                            OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromUser($"translate to {(split.Length == 3 ? split[2] : split[3])}: {toTranslate}"),
                        },
                    Model = Models.ChatGpt3_5Turbo,
                });


                if (result.Successful)
                {
                    var translated = result.Choices.First().Message.Content[1..];
                    twitchClient.SendMessage(e.ChatMessage.Channel, translated);
                }
                else
                    twitchClient.SendMessage(e.ChatMessage.Channel, $"Failed To Translate, Error: {result.Error.Code}, {result.Error.Message}");
            }
        }

        var realMessage = string.Empty;

        // after check with Chinese '操你媽垃圾機器人' which means 'fuck you rubbish bot' does not trigger flags, need to be trained or machine learning it
        var moderation = await openAI.CreateModeration(new CreateModerationRequest() { Input = e.ChatMessage.Message, });
        Console.Warn($"ChatGPT Moderation: {e.ChatMessage.Message}\n" +
            $"Flags: {moderation.Results.FirstOrDefault()?.Flagged ?? false}\n" +
            $"Categories: {string.Join(", ", moderation.Results.FirstOrDefault()?.GetCategories() ?? Array.Empty<bool>())}\n" +
            $"Level     : {string.Join(", ", moderation.Results.FirstOrDefault()?.GetScores() ?? Array.Empty<float>())}");
    }

    private void TwitchClient_OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.Log($"[Twitch] Connected to {e.AutoJoinChannel}");
    }

    private void TwitchClient_OnLog(object? sender, OnLogArgs e)
    {
        Console.Debug($"[Twitch] {e.DateTime.GetFormat()}: {e.BotUsername} - {e.Data}");
    }
    #endregion
}
