using System.Reflection;

using TwitchLib.Client.Models;

namespace Xein.Discord.ChatGPT;

public static class Commands
{
    public delegate void CommandHandler(string input, string[] args, ChatMessage chatMsg);
    
    public static Dictionary<string, CommandHandler> Functions { get; } = new();

    public static void Init()
    {
        foreach (var func in typeof(Commands)
                             .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             .Where(mi => mi.IsDefined(typeof(CommandAttribute)))
                             .ToDictionary(mi => mi.GetCustomAttribute<CommandAttribute>()!.Command,
                                           mi => (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), null, mi))
                )
            Functions.Add(func.Key, func.Value);

        Console.Log($"[Commands] Loaded {Functions.Count} Functions");
    }

    public static bool           IsCommandExist   (string command) => Functions.ContainsKey(command);
    public static CommandHandler GetCommandHandler(string command) => IsCommandExist(command) ? Functions[command] : null;

    [Command("xautojoin")]
    private static void TwitchAutoJoin(string input, string[] args, ChatMessage chatMsg)
    {
        if (chatMsg?.IsAbleToUse() ?? false)
            return;
        
        if (args.Length < 2)
        {
            if (chatMsg is not null)
                TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, "Invalid Usage: xautojoin <channelName>");
            else
                Console.Error("Invalid Usage: xautojoin <channelName>");
            return;
        }

        ConfigManager.SystemConfig.AddAutoJoinChannel(args[1]);

        TwitchManager.JoinChannel(args[1]);
        TwitchManager.SendMessage(args[1], "Start Scanning Message, to optout message collecting(for training purpose), please type xoptout | 开始检测聊天信息，如果不想被本机器人收集聊天信息数据(训练检测使用)，请在聊天室输入 xoptout");
    }
    
    [Command("xjoin")]
    private static void TwitchJoin(string input, string[] args, ChatMessage chatMsg)
    {
        if (chatMsg?.IsAbleToUse() ?? false)
            return;
        
        if (args.Length < 2)
        {
            if (chatMsg is not null)
                TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, "Invalid Usage: xjoin <channelName>");
            else
                Console.Error("Invalid Usage: xjoin <channelName>");
            return;
        }

        TwitchManager.JoinChannel(args[1]);
        TwitchManager.SendMessage(args[1], "Start Scanning Message, to optout message collecting(for training purpose), please type xoptout | 开始检测聊天信息，如果不想被本机器人收集聊天信息数据(训练检测使用)，请在聊天室输入 xoptout");
    }

    [Command("xleave")]
    private static void TwitchLeave(string input, string[] args, ChatMessage chatMsg)
    {
        if (chatMsg?.IsAbleToUse() ?? false)
            return;
        
        if (args.Length < 2 && chatMsg is null)
        {
            Console.Error("Invalid Usage: xleave <channelName>");
            return;
        }
        
        var channelName = chatMsg is not null ? chatMsg.Channel : args[1];

        TwitchManager.SendMessage(args[1], $"Leaving '{args[1]}' Chat | 正在离开 '{args[1]}' 聊天室");
        TwitchManager.LeaveChannel(args[1]);
    }

    [Command("xoptout")]
    private static void OptoutUser(string input, string[] args, ChatMessage chatMsg)
    {
        if (chatMsg is null)
        {
            Console.Error("OPTOUT FEATERE NOT ABLE IN CONSOLE");
            return;
        }
        
        ConfigManager.SystemConfig.AddOptOut(chatMsg.Username);
        TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, "You have optout successfully | 你已成功OPTOUT");
    }

    [Command("xoptin")]
    private static void OptInUser(string input, string[] args, ChatMessage chatMsg)
    {
        if (chatMsg is null)
        {
            Console.Error("OPTIN FEATERE NOT ABLE IN CONSOLE");
            return;
        }
        
        ConfigManager.SystemConfig.RemoveOptOut(chatMsg.Username);
        TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, "You have OPTIN successfully | 你已成功OPTIN");
    }

    [Command("xban")]
    private static void BanUser(string input, string[] args, ChatMessage chatMsg)
    {
        if (chatMsg?.IsAbleToUse() ?? false)
            return;
        
        if (args.Length < 2)
        {
            if (chatMsg is null)
                Console.Error("Invalid Usage: xban <userName>");
            else
                TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, "Invalid Usage: xban <userName> | 错误参数: xban <用户名>");
            return;
        }
        
        // unbannable username :D
        if (string.Compare(args[1], "xein0708", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            if (chatMsg is null)
                Console.Error("UNBANNABLE USER");
            else
                TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, "UNBANNABLE USER | 无法BAN的用户");
            return;
        }
        
        ConfigManager.SystemConfig.AddBanned(args[1]);
        TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, $"User '{args[1]}' has been BANNED, Not Scanning Any Message Given | 用户 '{args[1]}' 已被BANNED，将不会检测他任何信息");
    }
    
    [Command("xunban")]
    private static void UnBanUser(string input, string[] args, ChatMessage chatMsg)
    {
        if (chatMsg?.IsAbleToUse() ?? false)
            return;
        
        if (args.Length < 2)
        {
            if (chatMsg is null)
                Console.Error("Invalid Usage: xunban <userName>");
            else
                TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, "Invalid Usage: xunban <userName> | 错误参数: xunban <用户名>");
            return;
        }
        
        if (!ConfigManager.SystemConfig.IsBannedUsername(args[1]))
        {
            if (chatMsg is null)
                Console.Error($"User '{args[1]}' aren't banned");
            else
                TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, $"User '{args[1]}' aren't banned | 用户 '{args[1]}' 未被BANNED");
            return;
        }
        
        ConfigManager.SystemConfig.RemoveBanned(args[1]);
        TwitchManager.ReplyMessage(chatMsg.Channel, chatMsg.Id, $"User '{args[1]}' has been UNBANNED, Scanning Any Message Given | 用户 '{args[1]}' 已UNBANNED，将检测他任何信息");
    }
}
