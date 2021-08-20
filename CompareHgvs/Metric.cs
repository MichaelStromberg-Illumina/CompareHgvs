using System;
using System.Collections.Generic;

namespace CompareHgvs
{
    public class Metric
    {
        private readonly string           _description;
        private readonly int              _maxDifferences;
        private readonly List<Difference> _differences;

        private int _numCorrect;
        private int _numIncorrect;

        public static readonly string Divider = new('=', 35);

        public Metric(string description, int maxDifferences)
        {
            _description    = description;
            _maxDifferences = maxDifferences;
            _differences    = new List<Difference>(maxDifferences);
        }

        public void Add(string vid, string hgvsg, string key, string expected, string actual)
        {
            bool isCorrect = expected == actual;

            if (isCorrect)
            {
                _numCorrect++;
                return;
            }

            if (_differences.Count < _maxDifferences)
            {
                var difference = new Difference(vid, hgvsg, key, expected, actual);
                _differences.Add(difference);
            }

            _numIncorrect++;
        }

        public void DisplayResults()
        {
            int    total            = _numCorrect + _numIncorrect;
            double percentCorrect   = _numCorrect   / (double) total * 100.0;
            double percentIncorrect = _numIncorrect / (double) total * 100.0;

            Console.WriteLine($"{_description} results");
            Console.WriteLine(Divider);
            Console.WriteLine($"- correct:   {_numCorrect:N0} ({percentCorrect:0.000}%)");
            Console.WriteLine($"- incorrect: {_numIncorrect:N0} ({percentIncorrect:0.00000}%)");
            Console.WriteLine($"- total:     {total:N0}\n");
        }

        public void DisplayDifferences()
        {
            if (_differences.Count == 0) return;
            Console.WriteLine($"{_description} differences");
            Console.WriteLine(Divider);
            foreach (Difference difference in _differences) Console.WriteLine(difference);
            Console.WriteLine();
        }
    }
}