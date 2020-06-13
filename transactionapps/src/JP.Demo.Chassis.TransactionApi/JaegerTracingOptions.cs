namespace JP.Demo.Chassis.TransactionApi
{
    public class JaegerTracingOptions
    {
        public double SamplingRate { get; set; }
        public double LowerBound { get; set; }
        public string JaegerAgentHost { get; set; }
        public int JaegerAgentPort { get; set; }
        public string ServiceName { get; set; }

        public JaegerTracingOptions()
        {
            SamplingRate = 1d;
            LowerBound = 1d;
            JaegerAgentHost = "i-do-not-exist";
            JaegerAgentPort = 6831;
        }
    }
}
