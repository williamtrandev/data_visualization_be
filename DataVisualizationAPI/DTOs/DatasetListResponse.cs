using System;
using System.Collections.Generic;

namespace DataVisualizationAPI.DTOs
{
    public class DatasetListResponse
    {
        public int DatasetId { get; set; }
        public string DatasetName { get; set; }
        public string SourceType { get; set; }
        public string SourceName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public int TotalRows { get; set; }
        public string Status { get; set; }
        public List<DatasetColumnInfo> Columns { get; set; } = new List<DatasetColumnInfo>();
    }
} 