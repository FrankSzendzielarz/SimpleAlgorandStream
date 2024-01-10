namespace SimpleAlgorandStream.Config
{
    internal class AlgodSource
    {
        public string ApiUri { get; set; }
        public string ApiToken { get; set; }
        public bool ExponentialBackoff { get; set; }
        public TimeSpan RetryFrequency { get; set; }

    }
}
