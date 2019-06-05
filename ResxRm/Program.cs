using System;
using System.Collections;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;

namespace ResxRm
{
    class Program
    {
        static readonly ITypeResolutionService tnull = null;

        private static void ExitWithError(string errorMessage)
        {
            Console.WriteLine(errorMessage);
            Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
                ExitWithError(
                    "Two arguments with file names have to be specified!\nusage: resxrm referenceFilePath translationFilePath");
            if (!File.Exists(args[0])) ExitWithError($"Specified file \"{args[0]}\" does not exist!");
            if (!File.Exists(args[1])) ExitWithError($"Specified file \"{args[1]}\" does not exist!");

            using (var referenceResxReader = new ResXResourceReader(args[0]) { UseResXDataNodes = true })
            using (var translationResxReader = new ResXResourceReader(args[1]) { UseResXDataNodes = true })
            using (var outputResxWriter = new ResXResourceWriter(args[1]))
            {
                try
                {
                    var referenceResources = referenceResxReader.Cast<DictionaryEntry>().Select(n2 => (ResXDataNode) n2.Value).ToList();
                    var translatedResources = translationResxReader.Cast<DictionaryEntry>().Select(n => (ResXDataNode) n.Value).ToList();
                    var translatedAfterCleanUp = translatedResources.Where(x => referenceResources.Any(y => y.Name.Equals(x.Name))).ToList();

                    translatedAfterCleanUp.ForEach(r => outputResxWriter.AddResource(new ResXDataNode(
                            r.Name,
                            r.GetValue(tnull).ToString())
                        {Comment = r.Comment}));

                    var removedCounter = translatedResources.Count - translatedAfterCleanUp.Count;
                    Console.WriteLine($"Number of entries in reference resources: {referenceResources.Count}\n" +
                                      $"Number of entries in translation resources: {translatedResources.Count}\n" +
                                      $"Translation file has been cleaned up. Removed entries: {removedCounter}\n" +
                                      $"Writting to {args[1]}");
                    outputResxWriter.Generate();
                }
                catch (Exception e)
                {
                    ExitWithError(e.Message);
                }
            }
        }
    }
}
