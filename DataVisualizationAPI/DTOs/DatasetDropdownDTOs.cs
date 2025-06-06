using System.Collections.Generic;

namespace DataVisualizationAPI.DTOs.Dropdown
{
    public class DatasetDropdownResponse
    {
        public int DatasetId { get; set; }
        public string DatasetName { get; set; }
        public List<DropdownColumnInfo> Columns { get; set; }
    }

    public class DropdownColumnInfo
    {
        public string ColumnName { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }
    }
} 