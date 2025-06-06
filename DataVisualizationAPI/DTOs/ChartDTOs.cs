using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataVisualizationAPI.DTOs
{
    public class ChartAggregationRequest
    {
        [Required(ErrorMessage = "Category field is required")]
        [MinLength(1, ErrorMessage = "Category field cannot be empty")]
        public string CategoryField { get; set; }

        [Required(ErrorMessage = "Value field is required")]
        [MinLength(1, ErrorMessage = "Value field cannot be empty")]
        public string ValueField { get; set; }

        public string? SeriesField { get; set; }

        [Required(ErrorMessage = "Aggregation method is required")]
        [RegularExpression("^(sum|avg|count|min|max)$", ErrorMessage = "Aggregation must be one of: sum, avg, count, min, max")]
        public string Aggregation { get; set; }

        public string? TimeInterval { get; set; }

        public List<ChartFilter> Filters { get; set; } = new List<ChartFilter>();
    }

    public class ChartFilter
    {
        [Required(ErrorMessage = "Filter field is required")]
        [MinLength(1, ErrorMessage = "Filter field cannot be empty")]
        public string Field { get; set; }

        [Required(ErrorMessage = "Filter operator is required")]
        [RegularExpression("^(eq|gt|lt|gte|lte|contains)$", ErrorMessage = "Operator must be one of: eq, gt, lt, gte, lte, contains")]
        public string Operator { get; set; }

        [Required(ErrorMessage = "Filter value is required")]
        public object Value { get; set; }
    }

    public class ChartAggregationResponse
    {
        public List<string> Categories { get; set; } = new List<string>();
        public List<double> Values { get; set; } = new List<double>();
        public List<ChartSeries> Series { get; set; } = new List<ChartSeries>();
    }

    public class ChartSeries
    {
        public string Name { get; set; }
        public List<double> Data { get; set; } = new List<double>();
    }
} 