using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace Xein.Discord.ChatGPT;

// should I uses configs...?
public static class DiscordManager
{
    private static DiscordSocketClient discord;
    
    public static async void Init()
    {
        discord                      =  new();
        discord.Log                  += Discord_Log;
        discord.SlashCommandExecuted += Discord_SlashCommandExecuted;
        discord.Ready                += Discord_Ready;

        await discord.LoginAsync(TokenType.Bot, ConfigManager.Config.DiscordBotToken);
        await discord.StartAsync();
    }
    
    private static async Task Discord_Ready()
    {
        // register commands
        // for now, only support english, chinese, japanese (need add more...)
        var commandBuilder = new SlashCommandBuilder()
                             .WithName("translate")
                             .WithDescription("translate to specific language")
                             .AddOption(new SlashCommandOptionBuilder()
                                        .WithName("english")
                                        .WithDescription("translate to english")
                                        .WithType(ApplicationCommandOptionType.SubCommand)
                                        .AddOption("content", ApplicationCommandOptionType.String, "the content"))
                             .AddOption(new SlashCommandOptionBuilder()
                                        .WithName("chinese")
                                        .WithDescription("translate to chinese")
                                        .WithType(ApplicationCommandOptionType.SubCommand)
                                        .AddOption("content", ApplicationCommandOptionType.String, "the content"));
        
        // temporarily register on here only (globally need 1-2hr)
        // https://discord.com/channels/1081540759620177951/1101680330764734485/1122718947301658685
        // get first ulong which this part ^
        try
        {
            await discord.Rest.CreateGuildCommand(commandBuilder.Build(), 1081540759620177951);
        }
        catch (HttpException e)
        {
            Console.Error($"[Discord] command register failed\nCode: {e.HttpCode}, JsonCode: {e.DiscordCode}, Message: {e.Message}");
            foreach (var error in e.Errors)
                foreach (var er in error.Errors)
                    Console.Log($"Code: {er.Code}, Message: {er.Message}");
        }
    }
    
    private static async Task Discord_SlashCommandExecuted(SocketSlashCommand command)
    {
        // ignore bot actions
        if (command.User.IsBot)
            return;
        
        // main function
        var function         = command.Data.Name;
        var subFunction      = command.Data.Options.FirstOrDefault()?.Name;
        var subFunctionValue = command.Data.Options.FirstOrDefault()?.Options.FirstOrDefault()?.Value;

        Console.Debug($"[Discord Command] Received User({command.User}): {function}, {subFunction}, {subFunctionValue}");

        if (function is "translate")
        {
            if (subFunction is "english" or "chinese")
            {
                if ((subFunctionValue as string).IsEmpty())
                {
                    await command.RespondAsync($"Translate Content are Empty", ephemeral: true);
                    return;
                }
                
                var result = await OpenAIManager.Translate(subFunction, subFunctionValue as string);
                if (result.successful)
                    await command.RespondAsync($"{command.User.Mention} says: {result.message}");
                else
                    await command.RespondAsync($"Failed to translate {command.User.Mention} messages to {subFunction}, please check backend error");
            }
        }
        else
            await command.RespondAsync($"Unknown Function", ephemeral: true);

        //await command.RespondAsync($"Executed: {command.Data.Name}, Options: {command.Data.Options.FirstOrDefault()?.Name}, OptionValue: {command.Data.Options.FirstOrDefault()?.Value}");
    }

    private static Task Discord_Log(LogMessage msg)
    {
        Console.Log($"[Discord] {msg}");
        return Task.CompletedTask;
    }
}
