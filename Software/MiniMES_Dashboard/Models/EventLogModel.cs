using System;

namespace MiniMES_Dashboard.Models
{
    public class EventLogModel
    {
        public string Timestamp { get; set; }
        public string NodeId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}