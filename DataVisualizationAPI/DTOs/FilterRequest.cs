using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataVisualizationAPI.DTOs
{
    public class FilterRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public List<FilterCondition>? Filters { get; set; }
    }

    public class FilterCondition
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("operator")]
        public string Operator { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
} 