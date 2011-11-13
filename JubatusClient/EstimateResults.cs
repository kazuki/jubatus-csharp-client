namespace Jubatus.Client
{
    public class EstimateResults
    {
        public EstimateResults (string label, double prob)
        {
            Label = label;
            Probability = prob;
        }

        public string Label { get; private set; }
        public double Probability { get; private set; }

        public static EstimateResults ChooseMostLikely(EstimateResults[] estimates)
        {
            if (estimates == null || estimates.Length == 0)
                return null;
            EstimateResults ret = estimates[0];
            for (int i = 1; i < estimates.Length; i ++) {
                if (ret.Probability < estimates[i].Probability)
                    ret = estimates[i];
            }
            return ret;
        }
    }
}
