using System.Text.RegularExpressions;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace Xein.Discord.ChatGPT;

public static partial class TwitchManager
{
    public  static TwitchClient          twitchClient;
    private static ConnectionCredentials twitchCredentials;
    private static string                twitchRealUsername;

    public static void Init()
    {
        Console.Log($"Init Twitch (Username: {ConfigManager.Config.TwitchUsername})");
        twitchCredentials = new(ConfigManager.Config.TwitchUsername, ConfigManager.Config.TwitchOAuthToken);
        twitchClient      = new();

        twitchClient.Initialize(twitchCredentials, ConfigManager.SystemConfig.AutoJoinedChannels);

        twitchClient.OnLog             += TwitchClient_OnLog;
        twitchClient.OnConnected       += TwitchClient_OnConnected;
        twitchClient.OnMessageReceived += TwitchClient_OnMessageReceived;

        Console.Log($"Connecting Twitch");
        twitchClient.Connect();
    }

    public static void Shutdown()
    {
        twitchClient.Disconnect();
    }

    public static void JoinChannel (string channelName)                               => twitchClient.JoinChannel (channelName, true);
    public static void LeaveChannel(string channelName)                               => twitchClient.LeaveChannel(channelName);
    public static void ReplyMessage(string channelName, string msgId, string message) => twitchClient.SendReply   (channelName, msgId, message);
    public static void SendMessage (string channelName, string message)               => twitchClient.SendMessage (channelName, message);

    
    #region Twitch Events
    [GeneratedRegex("[\u4E00-\u9FFF]+")]
    private static partial Regex RegexCJK();
    private static readonly Regex regexCJK = RegexCJK();

    [GeneratedRegex("[\uFF00-\uFFEF\u0000-\u0019\u0021-\u0040\u2000-\u206F\u005B-\u0060\u007B-\u007F\u2E00-\u2E7F]+")]
    private static partial Regex RegexSymbols();
    private static readonly Regex regexSymbols = RegexSymbols();

    private static async void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        Console.Log($"[Twitch] [{e.ChatMessage.UserType} '{e.ChatMessage.Username}'] says in '{e.ChatMessage.Channel}': {e.ChatMessage.Message}");

        // logging for training purpose?
        // TODO: implement MachineLearning
        if (!ConfigManager.SystemConfig.IsUserOptOut(e.ChatMessage.Username))
            await File.AppendAllTextAsync("chatlogs.txt", new TwitchChatLogging() { MsgId = e.ChatMessage.Id, FromChannel = e.ChatMessage.Channel, SenderUsername = e.ChatMessage.Username, Message = e.ChatMessage.Message, }.GetJson() + Environment.NewLine);
        
        // return if common bots username, commands prefix
        // TODO: make a model from MachineLearning? to predicts it
        if (e.ChatMessage.Username == "nightbot" ||
            e.ChatMessage.Username == "streamelements" ||
            e.ChatMessage.Message.StartsWith('!') ||
            e.ChatMessage.Message.StartsWith('！') ||
            e.ChatMessage.Message.Contains("www", StringComparison.InvariantCultureIgnoreCase) ||
            e.ChatMessage.Message.Contains("http", StringComparison.InvariantCultureIgnoreCase) ||
            e.ChatMessage.Message.Contains(".com", StringComparison.InvariantCultureIgnoreCase) ||
            ConfigManager.SystemConfig.IsBannedUsername(e.ChatMessage.Username))
            return;

        // remove emotes
        var realMessage = e.ChatMessage.Message;
        foreach (var emote in e.ChatMessage.EmoteSet.Emotes)
            realMessage = realMessage.Replace(emote.Name, "");

        Console.Debug($"[Twitch] ['{e.ChatMessage.Id}'>'{e.ChatMessage.Username}'] realMessage: {realMessage}");

        if (realMessage.IsEmpty())
            return;

        var args = realMessage.Split(' ').ToArray();
        if (args[0].StartsWith('x'))
        {
            if (Commands.IsCommandExist(args[0]))
                Commands.GetCommandHandler(args[0])(realMessage, args, e.ChatMessage);
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
            var isMention = e.ChatMessage.Message.Contains($"@{ConfigManager.Config.TwitchUsername}");
            // split message?
            var split = e.ChatMessage.Message.Split(' ');
            // is Translate exists
            var isTranslate = split.ToList().FindAll(s => string.Compare(s, "translate", StringComparison.InvariantCultureIgnoreCase) == 0).Any();

            if (split.Length >= 3 && isMention && isTranslate)
            {
                var language = split.Length == 3 ? split[2] : split[3];

                Console.Debug($"[Twitch] [Reply Checks] IsMention: {isMention}, IsTranslate: {isTranslate}\n" +
                    $"Msg: {toTranslate}\n" +
                    $"Split: {string.Join(" | ", split)}\n" +
                    $"Execute: translate to {language}: {toTranslate}");

                var result = await OpenAIManager.Translate(language, toTranslate);
                twitchClient.SendMessage(e.ChatMessage.Channel, result.successful ? $"{e.ChatMessage.Username} says/说: {result.message}" : $"Translate Failed/翻译失败: {result.message}");
            }
        }
        // or scans first 2 string && first must be translate
        else if (args.Length >= 2 && string.Compare(args[0], "translate", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            // and second must be language? (TODO: checks lang)
            var language    = args[1];
            var toTranslate = realMessage[(realMessage.IndexOf(args[1], StringComparison.InvariantCultureIgnoreCase) + args[1].Length)..];

            if (toTranslate.IsEmpty())
            {
                twitchClient.SendMessage(e.ChatMessage.Channel, "Nothing to translate | 沒有東西可以翻譯");
                return;
            }

            var result = await OpenAIManager.Translate(language, toTranslate);
            twitchClient.SendMessage(e.ChatMessage.Channel, result.successful ? $"{e.ChatMessage.Username} says/说: {result.message}" : $"Translate Failed/翻译失败: {result.message}");
        }
        // or automatically? (TODO: Smart Checks, Check Shit Symbols only, or pure Symbols)
        else if (!realMessage.IsEmpty() &&
                 !regexCJK.IsMatch(realMessage) &&
                 !realMessage.IsNumeric() &&
                 //!regexSymbols.IsMatch(realMessage)
                 realMessage[0] != '@'
                 )
        //else if (false)
        {
            var result = await OpenAIManager.Translate("繁体中文", realMessage);
            twitchClient.SendMessage(e.ChatMessage.Channel, result.successful ? $"{e.ChatMessage.Username} says/说: {result.message}" : $"Translate Failed/翻译失败: {result.message}");
        }

        try
        {
            // after check with Chinese '操你媽垃圾機器人' which means 'fuck you rubbish bot' does not trigger flags, need to be trained or machine learning it
            var result = await OpenAIManager.Moderation(e.ChatMessage.Message);
            Console.Warn($"ChatGPT Moderation: {e.ChatMessage.Message}\n"        +
                         $"Flags     : {result.flag}\n"                          +
                         $"Categories: {string.Join(", ", result.categories)}\n" +
                         $"Level     : {string.Join(", ", result.scores)}");

            if (result.flag)
                twitchClient.SendReply(e.ChatMessage.Channel, e.ChatMessage.Id, "该讯息被ChatGPT标识为有危险成分!");
        }
        catch (Exception ex)
        {
            Console.Error(ex.Format());
        }
    }

    private static void TwitchClient_OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.Log($"[Twitch] Connected to {e.AutoJoinChannel}");
    }

    private static void TwitchClient_OnLog(object? sender, OnLogArgs e)
    {
        Console.Debug($"[Twitch] {e.DateTime.GetFormat()}: {e.BotUsername} - {e.Data}");

        // TODO: FIX INSIDE LIBRARY PLS
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
