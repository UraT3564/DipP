using System;
using System.Collections.Generic;


namespace ДипП.Models
{
    public class TemplateConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string TemplateFile { get; set; }
        public List<string> Fields { get; set; }
        public List<string> TableColumns { get; set; }
        public List<string> SummaryColumns { get; set; } = new List<string>();
    }
}
