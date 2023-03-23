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
    public List<string> IgnoredTranslate   { get; init; } = new();

    public void Save() => File.WriteAllText("SystemConfig.json", this.GetJson(true));

    public bool IsBannedUsername(string username) => Username.Contains(username, StringComparer.InvariantCultureIgnoreCase);
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

    public bool IsUserOptOut(string username) => OptOutUser.Contains(username, StringComparer.InvariantCultureIgnoreCase);
    public void AddOptOut(string username)
    {
        OptOutUser.Add(username);
        Save();
    }
    public void RemoveOptOut(string username)
    {
        OptOutUser.Remove(username);
        Save();
    }

    public bool IsInsideAutoJoinChannel(string str) => AutoJoinedChannels.Contains(str);
    public void AddAutoJoinChannel(string username)
    {
        AutoJoinedChannels.Add(username);
        Save();
    }
    public void RemoveAutoJoinChannel(string username)
    {
        AutoJoinedChannels.Remove(username);
        Save();
    }

    public bool IsIgnoredTranslate(string str) => IgnoredTranslate.Contains(str);
    public void AddIgnoredTranslate(string str)
    {
        IgnoredTranslate.Add(str);
        Save();
    }
    public void RemoveIgnoredTranslate(string str)
    {
        IgnoredTranslate.Remove(str);
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

internal class Dangerous
{
    public string Message { get; init; }
    public List<bool> Categories { get; init; }
    public List<float> Scores { get; init; }
}