namespace Xein.Discord.ChatGPT;

public class Config
{
    public string ChatGPTApiKey            { get; init; } = "Enter Your API Key From https://platform.openai.com/account/api-keys";
    public string TwitchUsername           { get; init; } = "Enter Your Twitch Username";
    public string TwitchOAuthToken         { get; init; } = "Enter Your OAuth Token From https://twitchapps.com/tmi/";
    public string GoogleServiceAccountPath { get; init; } = "yourKeyFile.json";
}

public class SystemConfig
{
    public List<string> AutoJoinedChannels { get; init; } = new();
    public List<string> Username           { get; init; } = new();
    public List<string> OptOutUser         { get; init; } = new();

    public bool IsBannedUsername(string username) => Username  .Contains(username, StringComparer.InvariantCultureIgnoreCase);
    public bool IsUserOptOut    (string username) => OptOutUser.Contains(username, StringComparer.InvariantCultureIgnoreCase);

    public void Save() => File.WriteAllText("SystemConfig.json", this.GetJson(true));

    public void AddBanned(string username)
    {
        Username.Add(username);
        Save();
    }

    public void RemoveBanned(string username)
    {
        Username.Remove(username);
        Save();
    }

    public void AddOptOut(string username)
    {
        OptOutUser.Add(username);
        Save();
    }

    public void RemoveOptOut(string username)
    {
        if (OptOutUser.Contains(username))
            OptOutUser.Remove(username);
        Save();
    }

    public void AddAutoJoinChannel(string username)
    {
        AutoJoinedChannels.Add(username);
        Save();
    }
}

internal class TwitchChatLogging
{
    public string MsgId          { get; init; }
    public string FromChannel    { get; init; }
    public string SenderUsername { get; init; }
    public string Message        { get; init; }
}

internal class Translated
{
    public string Language    { get; init; }
    public string ToTranslate { get; init; }
    public string FinalResult { get; init; }
}