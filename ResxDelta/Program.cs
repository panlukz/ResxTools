using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;

namespace ResxDelta
{
    static class StringExtensions
    {
        public static string ToFlat(this string str) => Regex.Replace(str, @"[^\w]", "").ToLower();
    }

    class Program
    {
        static readonly ITypeResolutionService tnull = null;
        private const string DeltaFileName = "delta.resx";

        public class ResXDataNodeComparer : IEqualityComparer<ResXDataNode>
        {
            public bool Equals(ResXDataNode dn1, ResXDataNode dn2)
            {
                return dn1.Name.Equals(dn2.Name) &&
                       dn1.GetValue(tnull).ToString().ToFlat().Equals(dn2.GetValue(tnull).ToString().ToFlat()) &&
                       dn1.Comment.Equals(dn2.Comment);
            }
            public int GetHashCode(ResXDataNode obj)
            {
                return string.Concat(obj.Name, obj.GetValue(tnull).ToString().ToFlat(), obj.Comment).GetHashCode();
            }
        }

        private static void ExitWithError(string errorMessage)
        {
            Console.WriteLine(errorMessage);
            Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            if(args.Length != 2) ExitWithError("Two arguments with file names have to be specified!\nusage: resxdelta newerFilePath olderFilePath");
            if(!File.Exists(args[0])) ExitWithError($"Specified file \"{args[0]}\" does not exist!");
            if (!File.Exists(args[1])) ExitWithError($"Specified file \"{args[1]}\" does not exist!");

            using (var newResxReader = new ResXResourceReader(args[0]) {UseResXDataNodes = true})
            using (var oldResxReader = new ResXResourceReader(args[1]) {UseResXDataNodes = true})
            using (var deltaResxWriter = new ResXResourceWriter(DeltaFileName))
            {
                try
                {
                    var modifiedResources = newResxReader.Cast<DictionaryEntry>().Select(n => (ResXDataNode) n.Value)
                        .Except(oldResxReader.Cast<DictionaryEntry>().Select(n2 => (ResXDataNode) n2.Value),
                            new ResXDataNodeComparer())
                        .ToList();

                    Console.WriteLine($"{modifiedResources.Count} modified resources found!");

                    if(modifiedResources.Count == 0) return;

                    modifiedResources.ForEach(r => deltaResxWriter.AddResource(new ResXDataNode(
                            r.Name,
                            r.GetValue(tnull).ToString()) {Comment = r.Comment}));

                    Console.WriteLine($"Writting to {DeltaFileName}...");

                    deltaResxWriter.Generate();
                }
                catch (Exception e)
                {
                    ExitWithError(e.Message);
                }
            }

        }
    }
}
