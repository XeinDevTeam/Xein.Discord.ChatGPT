namespace Xein.Discord.ChatGPT;

public static class ConfigManager
{
    public static Config       Config;
    public static SystemConfig SystemConfig;

    public static void Init()
    {
        Console.Log("Init Config");
        
        if (!File.Exists("config.json"))
            File.WriteAllText("config.json", new Config().GetJson(true));

        Console.Log("Reading Config");
        Config = "config.json".FromJson<Config>(true);

        Console.Log("Reading System Configs");
        if (!File.Exists("SystemConfig.json"))
            File.WriteAllText("SystemConfig.json", new SystemConfig() { Username = new() { "nightbot", "streamelements" }, }.GetJson(true));
        SystemConfig = "SystemConfig.json".FromJson<SystemConfig>(true);
    }
}
