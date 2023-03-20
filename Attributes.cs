namespace Xein.Discord.ChatGPT;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public readonly string Command;
    public CommandAttribute(string command) => Command = command;
}