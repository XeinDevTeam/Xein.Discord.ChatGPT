using LanguageDetection;

namespace Xein.Discord.ChatGPT
{
    public static class LangDetect
    {
        private static LanguageDetector langDetector;

        public static void Init()
        {
            langDetector = new();
            langDetector.AddAllLanguages();
        }

        public static string GetLanguageDetection(string message)
        {
            return langDetector.Detect(message);
        }
    }
}
