using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompareHgvs;
using CompareHgvs.DataStructures;
using CompareHgvs.Utilities;

namespace ExtractGenesFromNirvanaJson
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                string programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                Console.WriteLine($"{programName} <Nirvana JSON path> <gene ID path> <output path>");
                Environment.Exit(1);
            }

            string nirvanaJsonPath = args[0];
            string geneIdPath      = args[1];
            string outputPath      = args[2];
            
            Console.Write("- loading Entrez gene IDs... ");
            HashSet<string> entrezGeneIds = SimpleParser.GetHashSet(geneIdPath);
            Console.WriteLine($"{entrezGeneIds.Count:N0} loaded.");

            using var nirvanaParser =
                new NirvanaJsonParser(FileUtilities.GetReadStream(nirvanaJsonPath), entrezGeneIds);
            using var writer = new StreamWriter(FileUtilities.GetWriteStream(outputPath));
            
            while (true)
            {
                Position position = nirvanaParser.GetPosition();
                if (position == null) break;

                WritePosition(writer, position);
            }
        }

        private static void WritePosition(StreamWriter writer, Position position)
        {
            foreach (Variant variant in position.Variants)
            {
                var wroteHgvsGenomic = false;
                
                foreach (Transcript transcript in variant.IdToTranscript.Values.OrderBy(x => x.TranscriptId))
                {
                    if (!wroteHgvsGenomic)
                    {
                        writer.WriteLine($"{variant.VID}\t{variant.HgvsGenomic}");
                        wroteHgvsGenomic = true;
                    }
                    
                    writer.WriteLine($"{variant.VID}\t{transcript.HgvsCoding}");
                    writer.WriteLine($"{variant.VID}\t{transcript.HgvsProtein}");
                    writer.WriteLine($"{transcript.TranscriptId}\t{transcript.Consequences}");
                }
            }
        }
    }
}