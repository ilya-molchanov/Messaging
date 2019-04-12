namespace Common.Models.Properties
{
    public class ScanProperties : Properties
    {
        public string InDir { get; set; }
        public string ErrorDir { get; set; }
        public string InfoQueueName { get; set; }
        public string StateQueueName { get; set; }
        public string CnString { get; set; }
        public int StateInterval { get; set; }

        public ScanProperties(string inDir, string errorDir, string infoQueueName, string stateQueueName, 
            string cnString, int scanTimeout, int stateInterval, string barcodeText) : base(scanTimeout, barcodeText)
        {
            InDir = inDir;
            ErrorDir = errorDir;
            InfoQueueName = infoQueueName;
            StateQueueName = stateQueueName;
            CnString = cnString;
            StateInterval = stateInterval;
        }
    }
}
