namespace CompareHgvs
{
    public class TranscriptMetric
    {
        public readonly string Description;
        private         int    _numCorrect;
        private         int    _numIncorrect;

        public TranscriptMetric(string description)
        {
            Description = description;
        }

        public void Add(string expected, string actual)
        {
            bool isCorrect = expected == actual;
            if (isCorrect) _numCorrect++;
            else _numIncorrect++;
        }

        public double PercentIncorrect()
        {
            int total = _numCorrect + _numIncorrect;
            return _numIncorrect / (double) total * 100.0;
        }
    }
}