using System;
using System.Runtime.Serialization;

namespace LBTester.Models
{
    [DataContract(Name = "status", Namespace = "")]
    public class StatusModel
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }
    }
}