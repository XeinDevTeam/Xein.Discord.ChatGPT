using Google.Cloud.TextToSpeech.V1;

using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

using System.Text.RegularExpressions;

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

internal class IgnoredUsers
{
    // globally ignored
    public List<string> Username { get; init; } = new();

    public bool IsBannedUsername(string username) => Username.Contains(username);

    public void AddBanned(string username)
    {
        Username.Add(username);
        File.WriteAllText("ignored.json", this.GetJson(true));
    }

    public void RemoveBanned(string username)
    {
        Username.Remove(username);
        File.WriteAllText("ignored.json", this.GetJson(true));
    }
}

public partial class Program
{
    private static Config conf;
    private static IgnoredUsers ignored;

    private static OpenAIService openAI;

    private static TwitchClient twitchClient;
    private static ConnectionCredentials twitchCredentials;
    private static string twitchRealUsername;

    public static void Main(string[] args) => new Program().Async(args).GetAwaiter().GetResult();

    private async Task Async(string[] args)
    {
        Console.Log("Hello World");

        Console.Warn($"Current Path: {Directory.GetCurrentDirectory()}");

        if (!File.Exists("config.json"))
            await File.WriteAllTextAsync("config.json", new Config().GetJson(true));

        Console.Log("Reading Config");
        conf = File.ReadAllText("config.json").FromJson<Config>();

        Console.Log("Reading Ignore List");
        if (!File.Exists("ignored.json"))
            await File.WriteAllTextAsync("ignored.json", new IgnoredUsers() { Username = new() { "nightbot", "streamelements" }, }.GetJson(true));
        ignored = File.ReadAllText("ignored.json").FromJson<IgnoredUsers>();

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
        if (false)
        {
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
        }

        Console.Log($"Init Twitch (Username: {conf.TwitchUsername})");
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

                    twitchClient.JoinChannel(args[1], twitchClient.JoinedChannels.ToList().Find(c => c.Channel == args[1]) is not null);
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

            case "twitchleave":
                {
                    if (args.Length < 2)
                    {
                        Console.Error($"Invalid Usage: twitchjoin <channel>");
                        return;
                    }

                    twitchClient.LeaveChannel(args[1]);
                }
                break;

            case "twitchlist":
                {
                    Console.Log($"Currently Joined: {string.Join(", ", twitchClient.JoinedChannels.ToList().Select(c => c.Channel))}");
                }
                break;

            case "twitchbanned":
                {
                    Console.Log($"Currently Blocked: {string.Join(", ", ignored.Username)}");
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

    private void InternalCommand(string channelName, string message, List<string> args, bool isOwner, bool isMod)
    {
        if (isOwner || isMod)
        {
            switch (args[0])
            {
                case "xban":
                    {
                        if (args.Count < 2)
                            return;

                        // WHY
                        if (args[1].Contains("xein0708", StringComparison.InvariantCultureIgnoreCase))
                            return;

                        ignored.AddBanned(args[1]);
                        twitchClient.SendMessage(channelName, $"已屏蔽: {args[1]} 的任何检测");
                    }
                    break;

                case "xunban":
                    {
                        if (args.Count < 2)
                            return;

                        // WHY
                        if (args[1].Contains("xein0708", StringComparison.InvariantCultureIgnoreCase))
                            return;

                        ignored.RemoveBanned(args[1]);
                        twitchClient.SendMessage(channelName, $"已解除 {args[1]} 屏蔽");
                    }
                    break;

                case "xjoin":
                    {
                        if (args.Count < 2)
                            return;

                        if (channelName == twitchRealUsername)
                        {
                            twitchClient.JoinChannel(args[1], true);
                            twitchClient.SendMessage(channelName, $"Joining Channel: {args[1]}");
                        }
                    }
                    break;

                case "xleave":
                    {
                        twitchClient.SendMessage(channelName, $"正在离开 {channelName} 的聊天室... 88");
                        twitchClient.LeaveChannel(channelName);
                    }
                    break;
            }
        }
    }

    private async Task<(bool, string)> ChatGptTranslate(string language, string toTranslate)
    {
        var result = await openAI.ChatCompletion.CreateCompletion(new()
        {
            Messages = new List<OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage>
                    {
                        OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromUser($"翻译成 {language}: {toTranslate}"),
                    },
            Model = Models.ChatGpt3_5Turbo,
        });

        bool retResult = result.Successful;

        string message = retResult ? result.Choices.First().Message.Content[1..] : $"Error: {result.Error.Code}, {result.Error.Message}";
        return (retResult, message);
    }

    #region Twitch Events
    [GeneratedRegex("[\u4E00-\u9FFF]+")]
    private static partial Regex RegexCJK();
    private static readonly Regex regexCJK = RegexCJK();

    [GeneratedRegex("[\uFF00-\uFFEF\u0000-\u0019\u0021-\u0040\u2000-\u206F\u005B-\u0060\u007B-\u007F\u2E00-\u2E7F]+")]
    private static partial Regex RegexSymbols();
    private static readonly Regex regexSymbols = RegexSymbols();

    private async void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        Console.Log($"[Twitch] [{e.ChatMessage.UserType} '{e.ChatMessage.Username}'] says in '{e.ChatMessage.Channel}': {e.ChatMessage.Message}");

        // return if common bots username, commands prefix
        if (e.ChatMessage.Username == "nightbot" ||
            e.ChatMessage.Username == "streamelements" ||
            e.ChatMessage.Message.StartsWith('!') ||
            e.ChatMessage.Message.StartsWith('！') ||
            e.ChatMessage.Message.Contains("www", StringComparison.InvariantCultureIgnoreCase) ||
            e.ChatMessage.Message.Contains("http", StringComparison.InvariantCultureIgnoreCase) ||
            e.ChatMessage.Message.Contains(".com", StringComparison.InvariantCultureIgnoreCase) ||
            ignored.IsBannedUsername(e.ChatMessage.Username))
            return;

        // remove emotes
        var realMessage = e.ChatMessage.Message;
        foreach (var emote in e.ChatMessage.EmoteSet.Emotes)
            realMessage = realMessage.Replace(emote.Name, "");

        Console.Debug($"[Twitch] ['{e.ChatMessage.Id}'>'{e.ChatMessage.Username}'] realMessage: {realMessage}");

        if (realMessage.IsEmpty())
            return;

        var args = realMessage.Split(' ').ToList();
        if (args[0].StartsWith('x'))
        {
            InternalCommand(e.ChatMessage.Channel, realMessage, args, e.ChatMessage.Username == "xein0708", e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster);
            return;
        }

        // Don't Check Self stuff...
        if (e.ChatMessage.Username == twitchRealUsername)
            return;

        // check if replies requires me to translate?
        if (e.ChatMessage.ChatReply is not null)
        {
            Console.Debug($"[Twitch] [Chat Reply '{e.ChatMessage.Id}'] ParentId: {e.ChatMessage.ChatReply?.ParentMsgId}, ParentMsg: {e.ChatMessage.ChatReply?.ParentMsgBody}");

            // never null in here
            var reply = e.ChatMessage.ChatReply;
            var toTranslate = reply.ParentMsgBody;

            // is user calling me
            var isMention = e.ChatMessage.Message.Contains($"@{conf.TwitchUsername}");

            // split message?
            var split = e.ChatMessage.Message.Split(' ');

            // is Translate exists
            var isTranslate = split.ToList().FindAll(s => string.Compare(s, "translate", StringComparison.InvariantCultureIgnoreCase) == 0).Any();

            if (split.Length >= 3 && isMention && isTranslate)
            {
                var language = (split.Length == 3 ? split[2] : split[3]);

                Console.Debug($"[Twitch] [Reply Checks] IsMention: {isMention}, IsTranslate: {isTranslate}\n" +
                    $"Msg: {toTranslate}\n" +
                    $"Split: {string.Join(" | ", split)}\n" +
                    $"Execute: translate to {language}: {toTranslate}");

                var (result, translated) = await ChatGptTranslate(language, toTranslate);

                if (result)
                    twitchClient.SendMessage(e.ChatMessage.Channel, $"{e.ChatMessage.Username} says: {translated}");
                else
                    twitchClient.SendMessage(e.ChatMessage.Channel, $"翻译失败: {translated}");
            }
        }
        // or scans first 2 string && first must be translate
        else if (args.Count >= 2 && string.Compare(args[0], "translate", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            // and second must be language? (TODO: checks lang)
            var language = args[1];
            var toTranslate = realMessage[(realMessage.IndexOf(args[1]) + args[1].Length)..];

            if (toTranslate.IsEmpty())
            {
                twitchClient.SendMessage(e.ChatMessage.Channel, "Nothing to translate | 沒有東西可以翻譯");
                return;
            }

            var (result, translated) = await ChatGptTranslate(language, toTranslate);
            if (result)
                twitchClient.SendMessage(e.ChatMessage.Channel, $"{e.ChatMessage.Username} says/說: {translated}");
            else
                twitchClient.SendMessage(e.ChatMessage.Channel, $"Translate Failed/翻译失败: {translated}");
        }
        // or automatically?
        else if (!realMessage.IsEmpty() && !regexCJK.IsMatch(realMessage) && !realMessage.IsNumeric() && !regexSymbols.IsMatch(realMessage))
        //else if (false)
        {
            var (result, translated) = await ChatGptTranslate("繁体中文", realMessage);

            if (result)
                twitchClient.SendReply(e.ChatMessage.Channel, e.ChatMessage.Id, $"{e.ChatMessage.Username} says/說: {translated}");
            else
                twitchClient.SendReply(e.ChatMessage.Channel, e.ChatMessage.Id, $"Translate Failed/翻译失败: {translated}");
        }

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

        // FIX LIBRARY PLS
        // first Received: can be ignored
        var args = e.Data.Split(' ');
        switch (args[1])
        {
            case ":tmi.twitch.tv":
                {
                    switch (args[2])
                    {
                        case "001":
                            twitchRealUsername = args[3];
                            twitchClient.JoinChannel(twitchRealUsername);
                            break;
                    }
                }
                break;
        }
    }
    #endregion
}
