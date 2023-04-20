using Catalyst;
using Catalyst.Models;

using Mosaik.Core;

using Version = Mosaik.Core.Version;

namespace Xein.Discord.ChatGPT
{
    public static class CatalystManager
    {
        private static Pipeline nlp;

        public static async void Init()
        {
            English.Register();
            Chinese.Register();
            Japanese.Register();

            var testDoc = new Document("Google LLC is an American multinational technology company that specializes in internet-related services and products, which include online advertising technologies, search engine, cloud computing, software, and hardware. It is considered one of the Big Four technology companies, alongside Amazon, Apple, and Facebook.Google was founded in September 1998 by Larry Page and Sergey Brin while they were Ph.D. students at Stanford University in California.", Language.English);

            //Storage.Current = new OnlineRepositoryStorage(new DiskStorage("catalyst-models"));
            Storage.Current = new DiskStorage("catalyst-models");

            nlp = await Pipeline.ForAsync(Language.Any);
            nlp.Add(await AveragePerceptronEntityRecognizer.FromStoreAsync(Language.English, Version.Latest, "WikiNER"));
            var language = await LanguageDetector.FromStoreAsync(Language.Any, Version.Latest, "");
            language.Trials = 10;
            nlp.Add(language);

            var neuEn = new Neuralyzer(Language.English, 0, "WikiNER-sample-fixes");
            neuEn.TeachAddPattern("Person", "Xein", pattern => pattern.Add(new PatternUnit(PatternUnitPrototype.Single().WithToken("Xein"))));
            neuEn.TeachAddPattern("Organization", "Foodpanda", pattern => pattern.Add(new PatternUnit(PatternUnitPrototype.Single().WithToken("Foodpanda"))));
            nlp.UseNeuralyzer(neuEn);

            // TODO: Implement this https://github.com/pwxcoo/chinese-xinhua
            var neuCn = new Neuralyzer(Language.Chinese, 0, "WikiNER-sample-fixes");
            neuCn.TeachAddPattern("Person", "仙仙", pattern => pattern.Add(new PatternUnit(PatternUnitPrototype.Single().WithToken("仙仙"))));
            nlp.UseNeuralyzer(neuCn);

            /*
            var docs = ConfigManager.ChatLogs.Select(c => new Document($"{c.SenderUsername}: {c.Message}")).Append(testDoc);
            var processed = nlp.Process(docs);

            foreach (var pair in from doc in processed group doc by doc.Language into newGroup orderby newGroup.Key select newGroup)
                Console.Debug($"Language: {pair.Key} has {pair.Count()}");
            foreach (var doc in processed)
            {
                if (doc.Language is Language.Chinese or Language.English)
                    continue;

                Console.Debug($"[NLP '{doc.Language}'] Text: {doc.Value}\n" +
                    $"Tokenized Value: {doc.TokenizedValue(true)}\n" +
                    $"Entities: {string.Join(" | ", doc.SelectMany(s => s.GetEntities()).Select(e => $"[{e.EntityType.Type}] {e.Value}"))}");
            }
            */
        }

        public static Language GetLanguageDetection(string doc)
        {
            return nlp.ProcessSingle(new Document(doc)).Language;
        }
    }
}
