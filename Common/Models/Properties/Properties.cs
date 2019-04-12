using System.Runtime.Serialization;

namespace Common.Models.Properties
{
    [DataContract]
    public class Properties
    {
        [DataMember]
        public int ScanTimeout { get; set; }
        [DataMember]
        public string BarcodeText { get; set; }

        public Properties(int scanTimeout, string barcodeText)
        {
            ScanTimeout = scanTimeout;
            BarcodeText = barcodeText;
        }
    }
}
