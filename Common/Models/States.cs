using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public enum States
    {
        [EnumMember]
        Waiting,
        [EnumMember]
        Processing,
        [EnumMember]
        Starting,
        [EnumMember]
        Stopping
    }
}
