using System;
using System.Collections.Generic;

namespace ДипП.Models
{
    public class DataEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } // "event", "period", "organization" и т.д.
        public string Date { get; set; } // для фильтрации по времени
        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
    }
}