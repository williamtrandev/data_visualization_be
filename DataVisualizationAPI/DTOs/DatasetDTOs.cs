using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class ImportDatasetRequest
    {
        [Required]
        public string DatasetName { get; set; }
    }

    public class ImportFromDatabaseRequest
    {
        [Required]
        public string DatasetName { get; set; }

        [Required]
        public string ConnectionString { get; set; }

        [Required]
        public string Query { get; set; }
    }

    public class ImportDatasetResponse
    {
        public int DatasetId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class QueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; }
        public string SortDirection { get; set; }
        public List<FilterParameter> Filters { get; set; }
    }

    public class FilterParameter
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }

    public class QueryResult
    {
        public List<Dictionary<string, object>> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class DatasetInfo
    {
        public int DatasetId { get; set; }
        public string DatasetName { get; set; }
        public string SourceType { get; set; }
        public string SourceName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalRows { get; set; }
        public string Status { get; set; }
        public List<DatasetColumnInfo> Columns { get; set; }
    }

    public class DatasetColumnInfo
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string DisplayName { get; set; }
        public bool IsRequired { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
    }

    public class RestApiImportRequest
    {
        [Required]
        public string DatasetName { get; set; }
        
        [Required]
        public string ApiUrl { get; set; }
        
        public RestApiImportOptions Options { get; set; }
    }

    public class RestApiImportOptions
    {
        public string HttpMethod { get; set; } = "GET";
        public Dictionary<string, string>? Headers { get; set; }
        public Dictionary<string, string>? QueryParameters { get; set; }
        public string? RequestBody { get; set; }
        public string? DataPath { get; set; } // JSON path to extract data array
        public int MaxRecords { get; set; } = 1000; // Limit number of records to import
        public int TimeoutSeconds { get; set; } = 30;
        public bool FlattenNestedObjects { get; set; } = true;
    }
} 