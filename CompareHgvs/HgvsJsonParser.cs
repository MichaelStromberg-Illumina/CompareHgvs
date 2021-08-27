using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CompareHgvs.DataStructures;
using CompareHgvs.Utilities;
using Newtonsoft.Json.Linq;

namespace CompareHgvs
{
    public class HgvsJsonParser : IDisposable
    {
        private readonly StreamReader _reader;

        public HgvsJsonParser(Stream stream)
        {
            _reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress));
        }

        public Position GetPosition()
        {
            string line = _reader.ReadLine();
            return line is null ? null : ParsePosition(line);
        }

        private Position ParsePosition(string line)
        {
            dynamic position = JObject.Parse(line);
            dynamic variants = position.variants;

            var annotatedVariants = new List<Variant>();

            foreach (dynamic variant in variants)
            {
                Variant annotatedVariant = ParseVariant(variant);
                annotatedVariants.Add(annotatedVariant);
            }

            return new Position(annotatedVariants.ToArray());
        }

        private Variant ParseVariant(dynamic variant)
        {
            string vid = variant.vid;

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

            return new Variant(vid, false, hgvsGenomic, idToTranscript);
        }

        private static void ParseTranscript(dynamic transcript, Dictionary<string, Transcript> idToTranscript)
        {
            string hgvsCoding  = transcript.hgvsc;
            bool   isIntron    = hgvsCoding != null && (hgvsCoding.Contains('+') || hgvsCoding.Contains('-'));
            
            // skip intronic HGVS (since the HGVS library doesn't apply any normalization
            if (isIntron) return;
            
            string hgvsProtein                                     = transcript.hgvsp;
            if (hgvsProtein is "non-coding" or "None") hgvsProtein = null;

            string transcriptId = HgvsUtilities.GetAccession(hgvsCoding);
            string proteinId    = HgvsUtilities.GetAccession(hgvsProtein);

            if (transcriptId == null || transcriptId.StartsWith('X')) return;

            var hgvsTranscript = new Transcript(transcriptId, proteinId, hgvsCoding, hgvsProtein, null);
            if (idToTranscript.ContainsKey(transcriptId))
                throw new InvalidDataException($"Found duplicate entry for {transcriptId}");
            idToTranscript[transcriptId] = hgvsTranscript;
        }

        public void Dispose() => _reader.Dispose();
    }
}