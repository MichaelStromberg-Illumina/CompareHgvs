using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompareHgvs.DataStructures;
using CompareHgvs.Utilities;

namespace CompareHgvs
{
    internal static class Program
    {
        private const           int                                  MaxDifferences     = 10000;
        private static readonly Dictionary<string, TranscriptMetric> TranscriptMetrics  = new();
        private static readonly HashSet<string>                      IgnoredTranscripts = new();
        private static readonly HashSet<string>                      IgnoredProteins    = new();

        private static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                string programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                Console.WriteLine($"{programName} <Nirvana JSON path> <HGVS truth JSON path> <gene ID path>");
                Environment.Exit(1);
            }

            string nirvanaJsonPath = args[0];
            string hgvsJsonPath    = args[1];
            string geneIdPath      = args[2];
            
            Console.Write("- loading Entrez gene IDs... ");
            HashSet<string> entrezGeneIds = SimpleParser.GetHashSet(geneIdPath);
            Console.WriteLine($"{entrezGeneIds.Count:N0} loaded.");

            // these transcripts have been shown to be problematic in the biocommons HGVS library
            IgnoredTranscripts.Add("NM_001278433.1");
            IgnoredProteins.Add("NP_001303939.1"); // biocommons doesn't like the translational readthrough

            var vidMetric   = new Metric("VID",     MaxDifferences);
            var hgvsgMetric = new Metric("HGVS g.", MaxDifferences);
            var hgvscMetric = new Metric("HGVS c.", MaxDifferences);
            var hgvspMetric = new Metric("HGVS p.", MaxDifferences);

            using var nirvanaParser = new NirvanaJsonParser(FileUtilities.GetReadStream(nirvanaJsonPath), entrezGeneIds);
            using var hgvsParser    = new HgvsJsonParser(FileUtilities.GetReadStream(hgvsJsonPath));

            var benchmark = new Benchmark();

            while (true)
            {
                Position nirvanaPosition = nirvanaParser.GetPosition();
                Position hgvsPosition    = hgvsParser.GetPosition();
                if (nirvanaPosition == null || hgvsPosition == null) break;

                ComparePosition(nirvanaPosition, hgvsPosition, vidMetric, hgvsgMetric, hgvscMetric, hgvspMetric);
            }

            vidMetric.DisplayDifferences();
            hgvsgMetric.DisplayDifferences();
            hgvscMetric.DisplayDifferences();
            hgvspMetric.DisplayDifferences();

            DisplayTranscriptMetrics();

            vidMetric.DisplayResults();
            hgvsgMetric.DisplayResults();
            hgvscMetric.DisplayResults();
            hgvspMetric.DisplayResults();

            Console.WriteLine($"Elapsed time: {benchmark.GetElapsedTime()}");
        }

        private static void DisplayTranscriptMetrics()
        {
            Console.WriteLine("Top transcripts (error > 1%)");
            Console.WriteLine(new string('=', 29));

            foreach (TranscriptMetric metric in TranscriptMetrics.Values.OrderByDescending(x => x.PercentIncorrect()))
            {
                double percentIncorrect = metric.PercentIncorrect();
                if (percentIncorrect < 1.0) break;
                Console.WriteLine($"- {metric.Description,-14}  {metric.PercentIncorrect(),6:0.00}%");
            }

            Console.WriteLine();
        }

        private static void ComparePosition(Position nirvanaPosition, Position hgvsPosition, Metric vidMetric,
            Metric hgvsgMetric, Metric hgvscMetric, Metric hgvspMetric)
        {
            if (nirvanaPosition.Variants.Length != hgvsPosition.Variants.Length)
                throw new InvalidDataException(
                    $"Expected the same number of variants for the current position, but found {nirvanaPosition.Variants.Length} in Nirvana and {hgvsPosition.Variants.Length} in HGVS.");

            for (var variantIndex = 0; variantIndex < nirvanaPosition.Variants.Length; variantIndex++)
            {
                Variant nirvanaVariant = nirvanaPosition.Variants[variantIndex];
                Variant hgvsVariant    = hgvsPosition.Variants[variantIndex];

                // skip variants that occur in genomic regions consisting of N bases
                if (nirvanaVariant.RefAlleleHasNs) continue;
                
                vidMetric.Add(nirvanaVariant.VID, nirvanaVariant.HgvsGenomic, null, hgvsVariant.VID,
                    nirvanaVariant.VID);
                hgvsgMetric.Add(nirvanaVariant.VID, nirvanaVariant.HgvsGenomic, null, hgvsVariant.HgvsGenomic,
                    nirvanaVariant.HgvsGenomic);

                foreach (Transcript nirvanaTranscript in nirvanaVariant.IdToTranscript.Values)
                {
                    if (!hgvsVariant.IdToTranscript.TryGetValue(nirvanaTranscript.TranscriptId,
                            out Transcript hgvsTranscript)) continue;

                    // ======================
                    // check HGVS c. notation
                    // ======================

                    bool isHgvsCodingGood = false;
                    
                    if (!IgnoredTranscripts.Contains(nirvanaTranscript.TranscriptId))
                    {
                        if (!TranscriptMetrics.TryGetValue(nirvanaTranscript.TranscriptId, out TranscriptMetric transcriptMetric))
                        {
                            transcriptMetric = new TranscriptMetric(nirvanaTranscript.TranscriptId);
                            TranscriptMetrics[nirvanaTranscript.TranscriptId] = transcriptMetric;
                        }
                        
                        hgvscMetric.Add(nirvanaVariant.VID, nirvanaVariant.HgvsGenomic, nirvanaTranscript.TranscriptId,
                            hgvsTranscript.HgvsCoding, nirvanaTranscript.HgvsCoding);
                        transcriptMetric.Add(hgvsTranscript.HgvsCoding, nirvanaTranscript.HgvsCoding);
                        isHgvsCodingGood = hgvsTranscript.HgvsCoding == nirvanaTranscript.HgvsCoding;
                    }
                    
                    // ======================
                    // check HGVS p. notation
                    // ======================

                    if (!isHgvsCodingGood                                                           ||
                        nirvanaTranscript.HgvsProtein == null && hgvsTranscript.HgvsProtein == null ||
                        IgnoredProteins.Contains(nirvanaTranscript.ProteinId)) continue;
                    
                    if (nirvanaTranscript.ProteinId == null && hgvsTranscript.ProteinId == null || 
                        nirvanaTranscript.ProteinId != hgvsTranscript.ProteinId) continue;
                    
                    if(hgvsTranscript.HgvsProtein.EndsWith(":p.?") && nirvanaTranscript.HgvsProtein == null) continue;

                    string nirvanaHgvsProtein = nirvanaTranscript.HgvsProtein;
                    string hgvsHgvsProtein    = hgvsTranscript.HgvsProtein;

                    hgvsHgvsProtein = HgvsProteinTransforms.TransformBiocommons(nirvanaHgvsProtein, hgvsHgvsProtein);

                    if (!TranscriptMetrics.TryGetValue(nirvanaTranscript.ProteinId, out TranscriptMetric proteinMetric))
                    {
                        proteinMetric = new TranscriptMetric(nirvanaTranscript.ProteinId);
                        TranscriptMetrics[nirvanaTranscript.ProteinId] = proteinMetric;
                    }
                    
                    hgvspMetric.Add(nirvanaVariant.VID, nirvanaVariant.HgvsGenomic, nirvanaTranscript.TranscriptId,
                        hgvsHgvsProtein, nirvanaHgvsProtein);
                    proteinMetric.Add(hgvsHgvsProtein, nirvanaHgvsProtein);
                }
            }
        }
    }
}