using System.Collections.Generic;

namespace ДипП.Models
{
    public class TemplateConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string TemplateFile { get; set; }
        public string StorageId { get; set; }

        public Dictionary<string, FieldMapping> TextFieldMappings { get; set; }
        public Dictionary<string, FieldMapping> TableFieldMappings { get; set; }
        public string AutoNumberMarker { get; set; }

        public List<int> DataTableIndices { get; set; }      // Индексы таблиц для данных
        public bool ShowTotalRow { get; set; } = true;       // Показывать итоговую строку
        public string TotalRowCaption { get; set; } = "ИТОГО:"; // Подпись в итоговой строке
        public string PeriodType { get; set; } = "month"; // "day", "month", "year"
        public List<string> SummaryColumns { get; set; }     // Какие колонки суммировать

        public List<int> TotalRowTableIndices { get; set; }
        public TemplateConfig()
        {
            TextFieldMappings = new Dictionary<string, FieldMapping>();
            TableFieldMappings = new Dictionary<string, FieldMapping>();
            DataTableIndices = new List<int>();
            SummaryColumns = new List<string>();
            TotalRowTableIndices = new List<int>();
        }
    }

    public class FieldMapping
    {
        public string StorageFieldId { get; set; }
        public string DateFormat { get; set; }
        public bool IsStatic { get; set; }
        public string StaticValue { get; set; }
        public bool IsAggregate { get; set; }
        public string AggregateType { get; set; } // "Count", "Sum", "Average", "Max", "Min"
    }
}