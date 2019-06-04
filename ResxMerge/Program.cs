using System;
using System.Collections;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;

namespace ResxMerge
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
                    "Two arguments with file names have to be specified!\nusage: resxmerge originalResxFilePath translatedDeltaFilePath");
            if (!File.Exists(args[0])) ExitWithError($"Specified file \"{args[0]}\" does not exist!");
            if (!File.Exists(args[1])) ExitWithError($"Specified file \"{args[1]}\" does not exist!");

            using (var originalResxReader = new ResXResourceReader(args[0]) { UseResXDataNodes = true })
            using (var deltaResxReader = new ResXResourceReader(args[1]) { UseResXDataNodes = true })
            using (var outputResxWriter = new ResXResourceWriter(args[0]))
            {
                try
                {
                    var originalResources = originalResxReader.Cast<DictionaryEntry>().Select(n => (ResXDataNode) n.Value).ToList();
                    var deltaResources = deltaResxReader.Cast<DictionaryEntry>().Select(n => (ResXDataNode) n.Value).ToList();

                    Console.WriteLine($"Original resources: {originalResources.Count}");
                    Console.WriteLine($"Delta resources: {deltaResources.Count}");

                    int counter = 0;
                    foreach (var resx in originalResources)
                    {
                        var match = deltaResources.FirstOrDefault(x => x.Name.Equals(resx.Name));
                        if (match != null) counter++;

                        outputResxWriter.AddResource(new ResXDataNode(
                                resx.Name,
                                match != null ? match.GetValue(tnull).ToString() : resx.GetValue(tnull).ToString())
                            {Comment = resx.Comment});
                    }

                    Console.WriteLine($"Modified resources: {counter}");
                    Console.WriteLine($"Writting to {args[0]}...");

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
