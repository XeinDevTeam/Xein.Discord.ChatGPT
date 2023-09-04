using Google.Cloud.TextToSpeech.V1;

using Microsoft.Extensions.Logging;

using Mosaik.Core;

namespace Xein.Discord.ChatGPT;

public partial class Program
{
    public static bool IsRequestClosing { get; private set; } = false;

    public static void Main(string[] args) => new Program().Async(args).GetAwaiter().GetResult();

    private Task Async(string[] args)
    {
        Console.Log("Hello World");
        Console.Warn($"Current Path: {Directory.GetCurrentDirectory()}");

        System.Console.OutputEncoding = System.Text.Encoding.Unicode;

        ApplicationLogging.SetLoggerFactory(LoggerFactory.Create(lb => lb.AddConsole()));
        
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        System.Console.CancelKeyPress += Console_CancelKeyPress;

        Commands.Init();
        ConfigManager.Init();
        OpenAIManager.Init();
        TwitchManager.Init();
        //DiscordManager.Init();
        CatalystManager.Init();
        LangDetect.Init();

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
        if (false)
        {
            Console.Log($"Init Environment");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", ConfigManager.Config.GoogleServiceAccountPath);

            Console.Log($"Init Google TTS");
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

        Console.Log($"Done Initialize");

        while (true)
        {
            var input = System.Console.ReadLine();
            if (IsRequestClosing && input.IsEmpty())
                break;
            if (string.Compare(input, "exit", StringComparison.InvariantCultureIgnoreCase) == 0)
                break;

            // real Args
            var realArgs = input.Split(' ');
            if (realArgs.Length >= 1 && Commands.IsCommandExist(realArgs[0]))
                Commands.GetCommandHandler(realArgs[0])(input, realArgs, null);
            else
                Console.Error("Invalid Command Input");
        }

        Shutdown();
        return Task.CompletedTask;
    }

    private void Shutdown()
    {
        if (IsRequestClosing)
            return;

        TwitchManager.Shutdown();
        //DiscordManager.Shutdown();

        Console.Warn("Sleeping 3 seconds waiting graceful exits");
        Thread.Sleep(3000);

        Environment.Exit(0);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.Error($"[FATAL ERROR] Unhandled Exception\n{((Exception)e.ExceptionObject).Format()}");
        Shutdown();
    }

    private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Shutdown();
    }
}
