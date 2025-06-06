namespace DataVisualizationAPI.DTOs
{
    public class DataSourceDetailResponse
    {
        public int DatasetId { get; set; }
        public string DatasetName { get; set; }
        public string SourceType { get; set; }
        public string SourceName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public int TotalRows { get; set; }
        public string Status { get; set; }
        public List<ColumnDetail> Columns { get; set; }
        public PaginatedResponse<Dictionary<string, object>> Data { get; set; }
    }

    public class ColumnDetail
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public ColumnStatistics Statistics { get; set; }
        public Dictionary<string, int> ValueDistribution { get; set; }
    }

    public class ColumnStatistics
    {
        public int TotalValues { get; set; }
        public int NullCount { get; set; }
        public int UniqueCount { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Average { get; set; }
        public string MostCommonValue { get; set; }
        public int MostCommonCount { get; set; }
    }
} 