using System.Collections.Generic;

namespace ДипП.Models
{
    public class StorageField
    {
        public string Key { get; set; }
        public string Display { get; set; }
        public bool Required { get; set; }
    }

    public class StorageConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string FilePath { get; set; }
        public string Type { get; set; }
        public List<StorageField> Fields { get; set; }
    }

    public class StorageConfigRoot
    {
        public List<StorageConfig> Storages { get; set; }
    }
}