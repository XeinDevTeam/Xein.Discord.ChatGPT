using System.Text;
using System.Text.RegularExpressions;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

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
        
        twitchClient.OnJoinedChannel    += TwitchClient_OnJoinedChannel;
        twitchClient.OnMessageReceived  += TwitchClient_OnMessageReceived;

        twitchClient.OnSendReceiveData  += TwitchClient_OnSendReceiveData;

        twitchClient.OnUnaccountedFor   += TwitchClient_OnUnaccountedFor;
        twitchClient.OnRateLimit        += TwitchClient_OnRateLimit;
        twitchClient.OnError            += TwitchClient_OnError;
        twitchClient.OnMessageThrottled += TwitchClient_OnMessageThrottled;

        Console.Log($"Connecting Twitch");
        twitchClient.Connect();
    }

    private static void TwitchClient_OnSendReceiveData(object? sender, OnSendReceiveDataArgs e)
    {
        Console.Debug($"[Twitch] [{e.Direction} Data] {e.Data}");

        var args = e.Data.Split(' ');
        switch (args[0])
        {
            case ":tmi.twitch.tv":
                {
                    switch (args[1])
                    {
                        case "001":
                            twitchRealUsername = args[2];
                            JoinChannel(twitchRealUsername);
                            SendMessage(twitchRealUsername, "Hi, I'm Online Now");
                            break;
                    }
                }
                break;
        }
    }

    private static void TwitchClient_OnUnaccountedFor(object? sender, OnUnaccountedForArgs e)
    {
        Console.Error($"[Twitch] [OnUnAccountedFor] {e.BotUsername} - {e.Channel}\nRAW: {e.RawIRC}\nLocation: {e.Location}\n");
    }

    private static void TwitchClient_OnMessageThrottled(object? sender, OnMessageThrottledEventArgs e)
    {
        Console.Error($"[Twitch] Throttled: {e.Message}\nPeroid: {e.Period} | Allowed In Peroid: {e.AllowedInPeriod}\nSent Cound:{e.SentMessageCount}");
    }

    private static void TwitchClient_OnError(object? sender, OnErrorEventArgs e)
    {
        Console.Error($"[Twitch] Error: {e.Exception.Format()}");
    }

    private static void TwitchClient_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.Log($"[Twitch] Joined Channel: {e.Channel}");
    }

    public static void Shutdown()
    {
        twitchClient?.Disconnect();
    }

    public static void JoinChannel (string channelName)                               => twitchClient.JoinChannel (channelName, true);
    public static void LeaveChannel(string channelName)                               => twitchClient.LeaveChannel(channelName);
    
    private static DateTime lastResetTime = DateTime.Now;
    private static int      lastSentCount;
    private static bool CheckSelfRatelimit()
    {
        if ((DateTime.Now - lastResetTime).TotalSeconds < 30 && lastSentCount > 15)
            return false;
        else if ((DateTime.Now - lastResetTime).TotalSeconds > 30.0f)
        {
            lastResetTime = DateTime.Now;
            lastSentCount = 0;
        }
        Console.Debug($"[SELFRATELIMIT] remaining: {15 - lastSentCount} | next reset: {(30 - (DateTime.Now - lastResetTime).TotalSeconds)}");
        return true;
    }
    
    public static void SendMessage(string channelName, string message)
    {
        if (!CheckSelfRatelimit())
        {
            Console.Warn($"[TWITCH] [SELFRATELIMIT], NOT SENDING TO: {channelName} > {message}");
            return;
        }
        lastSentCount += 1;
        twitchClient.SendMessage(channelName, message);
    }

    public static void ReplyMessage(string channelName, string msgId, string message)
    {
        if (!CheckSelfRatelimit())
        {
            Console.Warn($"[TWITCH] [SELFRATELIMIT], NOT REPLYING TO: {channelName} > {msgId} > {message}");
            return;
        }
        lastSentCount += 1;
        twitchClient.SendReply(channelName, msgId, message);
    }

    #region Twitch Events
    private static void TwitchClient_OnRateLimit(object? sender, OnRateLimitArgs e)
    {
        Console.Error($"[TWITCH] [RATELIMIT] Channel '{e.Channel}': {e.Message}");
    }

    [GeneratedRegex("[\\p{IsCJKUnifiedIdeographs}]+")]
    private static partial Regex RegexCJK();
    private static readonly Regex regexCJK = RegexCJK();

    [GeneratedRegex("[\\p{IsHiragana}\\p{IsKatakana}\\p{IsKatakanaPhoneticExtensions}]+")]
    private static partial Regex RegexJapanese();
    private static readonly Regex regexJapanese = RegexJapanese();

    [GeneratedRegex("[\uFF00-\uFFEF\u0000-\u0019\u0021-\u0040\u2000-\u206F\u005B-\u0060\u007B-\u007F\u2E00-\u2E7F]+")]
    private static partial Regex RegexSymbols();
    private static readonly Regex regexSymbols = RegexSymbols();

    [GeneratedRegex("\\p{So}|\\p{Cs}\\p{Cs}(\\p{Cf}\\p{Cs}\\p{Cs})*")]
    private static partial Regex RegexEmoji();
    private static readonly Regex regexEmoji = RegexEmoji();

    private static async void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        Console.Log($"[Twitch] [{e.ChatMessage.UserType} '{e.ChatMessage.Username}'] says in '{e.ChatMessage.Channel}': {e.ChatMessage.Message}");

        // logging for training purpose?
        // TODO: implement MachineLearning
        if (!ConfigManager.SystemConfig.IsUserOptOut(e.ChatMessage.Username))
            await File.AppendAllTextAsync("chatlogs.txt", new TwitchChatLogging() { MsgId = e.ChatMessage.Id, FromChannel = e.ChatMessage.Channel, SenderUsername = e.ChatMessage.Username, Message = e.ChatMessage.Message, }.GetJson() + Environment.NewLine, Encoding.Unicode);
        
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

        var realMessage = e.ChatMessage.Message;
        realMessage = realMessage.Replace("  ", " ").Replace("\t\t", " ");
        Console.Debug($"[Twitch] ['{e.ChatMessage.Channel}'>'{e.ChatMessage.Id}'>'{e.ChatMessage.Username}'] realMessage: {realMessage}");

        var args = realMessage.Split(' ').ToArray();
        if (args[0].StartsWith('x'))
        {
            if (Commands.IsCommandExist(args[0]))
                Commands.GetCommandHandler(args[0])(realMessage, args, e.ChatMessage);
            return;
        }

        // remove twitch emotes
        foreach (var emote in e.ChatMessage.EmoteSet.Emotes)
            realMessage = realMessage.Replace(emote.Name, "");
        // remove unicode emotes
        foreach (var emote in regexEmoji.Matches(realMessage).Cast<Match>())
            realMessage = realMessage.Replace(emote.Value, "");
        // remove ignored translate
        foreach (var replace in ConfigManager.SystemConfig.IgnoredTranslate)
            realMessage = realMessage.Replace(replace, "");
        // remove mention(at)
        if (realMessage.Contains('@'))
        {
            foreach (var str in realMessage.Split(' '))
            {
                if (str.StartsWith("@"))
                    realMessage = realMessage.Replace(str, "");
            }
        }
        // check first realMessage are ' ' or not
        if (realMessage.StartsWith(' '))
            realMessage = realMessage[1..];
        // Update to reparsed
        realMessage = realMessage.Replace("  ", " ").Replace("\t\t", " ");
        args        = realMessage.Split(' ').ToArray();
        if (realMessage.IsEmpty())
            return;

        // Don't Check Self stuff...
        if (e.ChatMessage.Username == twitchRealUsername)
            return;

        // Debug Regex and language predicts
        Console.Debug($"realMessage: {realMessage.Length}\n" +
            $"CJK     : {regexCJK     .Matches(realMessage).Select(m => m.Length).Sum()}\n" +
            $"Japanese: {regexJapanese.Matches(realMessage).Select(m => m.Length).Sum()}\n" +
            $"Symbol  : {regexSymbols .Matches(realMessage).Select(m => m.Length).Sum()}\n" +
            $"Language Predicts:\n" +
            $"NLP  : {CatalystManager.GetLanguageDetection(realMessage)}\n" +
            $"Lang : {LangDetect.GetLanguageDetection(realMessage)}");
        
        // if mentions translate
        if (
            // scans first 2 string && first must be translate
            (args.Length >= 2 && string.Compare(args[0], "translate", StringComparison.InvariantCultureIgnoreCase) == 0) ||
            (args.Length >= 2 && args[0] == "翻译") ||
            (args.Length >= 2 && args[0] == "翻譯")
            )
        {
            // and second must be language? TODO: checks lang
            var language    = args[1];
            var toTranslate = realMessage[(realMessage.IndexOf(args[1], StringComparison.InvariantCultureIgnoreCase) + args[1].Length)..];

            if (toTranslate.IsEmpty())
            {
                SendMessage(e.ChatMessage.Channel, "Nothing to translate | 沒有東西可以翻譯");
                return;
            }

            var result = await OpenAIManager.Translate(language, toTranslate);
            SendMessage(e.ChatMessage.Channel, result.successful ? $"{e.ChatMessage.Username} says/说: {result.message}" : $"Translate Failed/翻译失败: {result.message}");
        }
        // skippable...
        else if (
            // if its symbols and length are same
            (regexSymbols.Matches(realMessage).Select(m => m.Length).Sum() == realMessage.Length)
            )
        { }
        // automatically? TODO: Smart Checks, Check Shit Symbols only, or pure Symbols
        /*
        else if (!realMessage.IsEmpty() &&
                 !regexCJK.IsMatch(realMessage) &&
                 !realMessage.IsNumeric() &&
                 //!regexSymbols.IsMatch(realMessage)
                 realMessage[0] != '@' &&
                 (realMessage.Length > 3 && realMessage[0] != ' ' && realMessage[1] != '@')
                 )
        */
        else if (
            // if not contains numberics and not match any CJK Codes (which pretty much translate everything except Chinese/Japanese/Vietnamese/Korean
            (!realMessage.IsNumeric() && !regexCJK.IsMatch(realMessage)) ||
            // if contains HanJi(KanJi) and Katagana/Kiragana
            (regexCJK.IsMatch(realMessage) && regexJapanese.IsMatch(realMessage))
            )
        {
            var result = await OpenAIManager.Translate("繁体中文", realMessage);
            SendMessage(e.ChatMessage.Channel, result.successful ? $"{e.ChatMessage.Username} says/说: {result.message}" : $"Translate Failed/翻译失败: {result.message}");
        }

        try
        {
            // after check with Chinese '操你媽垃圾機器人' which means 'fuck you rubbish bot' does not trigger flags, need to be trained or machine learning it
            var result = await OpenAIManager.Moderation(e.ChatMessage.Message);
            Console.Warn($"ChatGPT Moderation: {e.ChatMessage.Message}\n"        +
                         $"Flags     : {result.flag}\n"                          +
                         $"Categories: {string.Join(", ", result.categories)}\n" +
                         $"Level     : {string.Join(", ", result.scores)}");

            await File.AppendAllTextAsync("dangerous.txt", new Dangerous() { Message = e.ChatMessage.Message, Categories = result.categories.ToList(), Scores = result.scores.ToList(), }.GetJson() + Environment.NewLine, System.Text.Encoding.Unicode);
            //if (result.flag)
                //ReplyMessage(e.ChatMessage.Channel, e.ChatMessage.Id, "该讯息被ChatGPT标识为有危险成分!");
        }
        catch (Exception ex)
        {
            Console.Error(ex.Format());
        }
    }
    #endregion
}
