using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CompareHgvs.DataStructures;
using Compression;
using Newtonsoft.Json.Linq;

namespace CompareHgvs
{
    public class NirvanaJsonParser : IDisposable
    {
        private readonly HashSet<string> _entrezGeneIds;
        private readonly StreamReader    _reader;

        private const string GeneSection = "],\"genes\":[";
        private const string EndSection  = "]}";
        private const string RefSeq      = "RefSeq";

        public NirvanaJsonParser(Stream stream, HashSet<string> entrezGeneIds)
        {
            _entrezGeneIds = entrezGeneIds;
            _reader        = new StreamReader(new BlockGZipStream(stream, CompressionMode.Decompress));

            // skip the header line
            _reader.ReadLine();
        }

        public Position GetPosition()
        {
            string line = _reader.ReadLine();
            return line is null or GeneSection or EndSection ? null : ParsePosition(line);
        }

        private Position ParsePosition(string line)
        {
            dynamic position = JObject.Parse(line.TrimEnd(','));

            string chromosome = position.chromosome;
            int    pos        = position.position;
            string refAllele  = position.refAllele;

            var altAlleles = new List<string>();
            foreach (string altAllele in position.altAlleles) altAlleles.Add(altAllele);

            dynamic variants = position.variants;

            var annotatedVariants = new List<Variant>();

            int altIndex = 0;
            foreach (dynamic variant in variants)
            {
                string  vid              = CreateVid(chromosome, pos, refAllele, altAlleles[altIndex]);
                bool    refAlleleHasNs   = refAllele.Contains('N');
                Variant annotatedVariant = ParseVariant(variant, vid, refAlleleHasNs);
                annotatedVariants.Add(annotatedVariant);
                altIndex++;
            }

            return new Position(annotatedVariants.ToArray());
        }

        private Variant ParseVariant(dynamic variant, string vid, bool refAlleleHasNs)
        {
            string  hgvsGenomic = variant.hgvsg;
            dynamic transcripts = variant.transcripts;
            
            var idToTranscript = new Dictionary<string, Transcript>();

            if (transcripts != null)
            {
                foreach (dynamic transcript in transcripts)
                {
                    ParseTranscript(transcript, idToTranscript);
                }
            }

            return new Variant(vid, refAlleleHasNs, hgvsGenomic, idToTranscript);
        }

        private static string CreateVid(string chromosomeName, int start, string refAllele, string altAllele) =>
            chromosomeName + '-' + start + '-' + refAllele + '-' + altAllele;

        private void ParseTranscript(dynamic transcript, Dictionary<string, Transcript> idToTranscript)
        {
            string source     = transcript.source;
            string hgvsCoding = transcript.hgvsc;
            bool   isIntron   = hgvsCoding != null && (hgvsCoding.Contains('+') || hgvsCoding.Contains('-'));
            
            // skip intronic HGVS (since the HGVS library doesn't apply any normalization
            if (source != RefSeq || isIntron || string.IsNullOrEmpty(hgvsCoding)) return;
            
            // skip transcripts that are not in TSO500
            string geneId = transcript.geneId;
            if (!_entrezGeneIds.Contains(geneId)) return;

            string hgvsProtein  = transcript.hgvsp;
            string transcriptId = transcript.transcript;
            string proteinId    = transcript.proteinId;

            if (transcriptId.StartsWith('X')) return;
            
            var nirvanaTranscript = new Transcript(transcriptId, proteinId, hgvsCoding, hgvsProtein);
            if (idToTranscript.ContainsKey(transcriptId))
                throw new InvalidDataException($"Found duplicate entry for {transcriptId}");
            idToTranscript[transcriptId] = nirvanaTranscript;
        }

        public void Dispose() => _reader.Dispose();
    }
}