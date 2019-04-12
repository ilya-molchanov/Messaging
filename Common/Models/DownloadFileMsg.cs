using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class DownloadFileMsg
    {
        [DataMember]
        public string QueueName { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public int AmountOfParts { get; set; }
    }
}
