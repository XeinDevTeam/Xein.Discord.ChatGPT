namespace Xein.Discord.ChatGPT;

public class Config
{
    public string ChatGPTApiKey            { get; set; } = "REPLACE_TO_YOUR_OPENAI_API_KEY_https://platform.openai.com/account/api-keys";
    public string TwitchUsername           { get; set; } = "REPLACE_TO_YOUR_TWITCH_USERNAME";
    public string TwitchOAuthToken         { get; set; } = "REPLACE_TO_YOUR_TWITCH_OAUTH_TOKEN_https://twitchapps.com/tmi/";
    public string GoogleServiceAccountPath { get; set; } = "yourKeyFile.json";
    public string DiscordBotToken          { get; set; } = "REPLACE_TO_YOUR_DISCORD_BOT_TOKEN";
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

public class TwitchChatLogging
{
    public string MsgId          { get; init; }
    public string FromChannel    { get; init; }
    public string SenderUsername { get; init; }
    public string Message        { get; init; }
}

public class Translated
{
    public string Language    { get; init; }
    public string ToTranslate { get; init; }
    public string FinalResult { get; init; }
}

public class Dangerous
{
    public string Message { get; init; }
    public List<bool> Categories { get; init; }
    public List<float> Scores { get; init; }
}