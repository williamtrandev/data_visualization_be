using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DataVisualizationAPI.Data;
using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.Models;

namespace DataVisualizationAPI.Services
{
    public interface IChartDataService
    {
        Task<ChartAggregationResponse> GetAggregatedDataAsync(int datasetId, ChartAggregationRequest request);
    }

    public class ChartDataService : IChartDataService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChartDataService> _logger;

        public ChartDataService(AppDbContext context, ILogger<ChartDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ChartAggregationResponse> GetAggregatedDataAsync(int datasetId, ChartAggregationRequest request)
        {
            try
            {
                // Get dataset and validate fields
                var dataset = await _context.Datasets
                    .Include(d => d.Schemas)
                    .FirstOrDefaultAsync(d => d.DatasetId == datasetId);

                if (dataset == null)
                {
                    throw new KeyNotFoundException($"Dataset with ID {datasetId} not found");
                }

                ValidateFields(dataset, request);

                // Get all data
                var data = await _context.DatasetData
                    .Where(d => d.DatasetId == datasetId)
                    .Select(d => d.RowData)
                    .ToListAsync();

                // Convert JSON strings to dictionaries
                var items = data.Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d)).ToList();

                // Apply filters
                if (request.Filters != null && request.Filters.Any())
                {
                    items = ApplyFilters(items, request.Filters);
                }

                // Process time interval if specified
                if (!string.IsNullOrEmpty(request.TimeInterval))
                {
                    items = ProcessTimeInterval(items, request.CategoryField, request.TimeInterval);
                }

                // Group and aggregate data
                var result = new ChartAggregationResponse();

                if (string.IsNullOrEmpty(request.SeriesField))
                {
                    // Simple aggregation without series
                    var groupedData = items
                        .GroupBy(d => GetStringValue(d, request.CategoryField))
                        .Select(g => new
                        {
                            Category = g.Key,
                            Value = AggregateValues(g.Select(d => d[request.ValueField]), request.Aggregation)
                        })
                        .OrderBy(x => x.Category)
                        .ToList();

                    result.Categories = groupedData.Select(x => x.Category).ToList();
                    result.Values = groupedData.Select(x => x.Value).ToList();
                }
                else
                {
                    // Aggregation with series
                    var groupedData = items
                        .GroupBy(d => new
                        {
                            Category = GetStringValue(d, request.CategoryField),
                            Series = GetStringValue(d, request.SeriesField)
                        })
                        .Select(g => new
                        {
                            g.Key.Category,
                            g.Key.Series,
                            Value = AggregateValues(g.Select(d => d[request.ValueField]), request.Aggregation)
                        })
                        .ToList();

                    var categories = groupedData.Select(x => x.Category).Distinct().OrderBy(x => x).ToList();
                    var series = groupedData.Select(x => x.Series).Distinct().OrderBy(x => x).ToList();

                    result.Categories = categories;
                    result.Series = series.Select(s => new ChartSeries
                    {
                        Name = s,
                        Data = categories.Select(c =>
                        {
                            var value = groupedData.FirstOrDefault(x => x.Category == c && x.Series == s);
                            return value != null ? value.Value : 0;
                        }).ToList()
                    }).ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aggregated data for dataset {DatasetId}", datasetId);
                throw;
            }
        }

        private void ValidateFields(Dataset dataset, ChartAggregationRequest request)
        {
            var schema = dataset.Schemas.ToDictionary(s => s.ColumnName);

            if (!schema.ContainsKey(request.CategoryField))
                throw new ArgumentException($"Category field '{request.CategoryField}' not found in dataset");

            if (!schema.ContainsKey(request.ValueField))
                throw new ArgumentException($"Value field '{request.ValueField}' not found in dataset");

            if (!string.IsNullOrEmpty(request.SeriesField) && !schema.ContainsKey(request.SeriesField))
                throw new ArgumentException($"Series field '{request.SeriesField}' not found in dataset");

            if (!new[] { "sum", "avg", "count", "min", "max" }.Contains(request.Aggregation.ToLower()))
                throw new ArgumentException($"Invalid aggregation method: {request.Aggregation}");

            if (!string.IsNullOrEmpty(request.TimeInterval) &&
                !new[] { "day", "week", "month", "quarter", "year" }.Contains(request.TimeInterval.ToLower()))
                throw new ArgumentException($"Invalid time interval: {request.TimeInterval}");
        }

        private List<Dictionary<string, object>> ApplyFilters(
            List<Dictionary<string, object>> data,
            List<ChartFilter> filters)
        {
            return data.Where(item =>
            {
                return filters.All(filter =>
                {
                    if (!item.ContainsKey(filter.Field) || item[filter.Field] == null)
                        return false;

                    var itemValue = item[filter.Field];
                    var filterValue = filter.Value;

                    return filter.Operator.ToLower() switch
                    {
                        "eq" => itemValue.ToString() == filterValue.ToString(),
                        "gt" => CompareValues(itemValue, filterValue) > 0,
                        "lt" => CompareValues(itemValue, filterValue) < 0,
                        "gte" => CompareValues(itemValue, filterValue) >= 0,
                        "lte" => CompareValues(itemValue, filterValue) <= 0,
                        "contains" => itemValue.ToString().Contains(filterValue.ToString()),
                        _ => false
                    };
                });
            }).ToList();
        }

        private List<Dictionary<string, object>> ProcessTimeInterval(
            List<Dictionary<string, object>> data,
            string dateField,
            string interval)
        {
            return data.Select(item =>
            {
                if (item[dateField] == null || !DateTime.TryParse(item[dateField].ToString(), out var date))
                    return item;

                var newItem = new Dictionary<string, object>(item);
                newItem[dateField] = interval.ToLower() switch
                {
                    "day" => date.ToString("yyyy-MM-dd"),
                    "week" => $"{date:yyyy}-W{GetWeekNumber(date)}",
                    "month" => date.ToString("yyyy-MM"),
                    "quarter" => $"{date:yyyy}-Q{(date.Month - 1) / 3 + 1}",
                    "year" => date.ToString("yyyy"),
                    _ => item[dateField]
                };

                return newItem;
            }).ToList();
        }

        private double AggregateValues(IEnumerable<object> values, string aggregation)
        {
            var nonNullValues = values.Where(v => v != null).ToList();

            if (!nonNullValues.Any())
                return 0;

            if (aggregation.Equals("count", StringComparison.OrdinalIgnoreCase))
            {
                return nonNullValues.Count;
            }

            var numericValues = nonNullValues.Select(v =>
            {
                if (v is JsonElement jsonElement)
                {
                    return jsonElement.ValueKind switch
                    {
                        JsonValueKind.Number => jsonElement.GetDouble(),
                        JsonValueKind.String => double.TryParse(jsonElement.GetString(), out var result) ? result : double.NaN,
                        JsonValueKind.True => 1,
                        JsonValueKind.False => 0,
                        _ => double.NaN
                    };
                }
                else if (v is bool b)
                {
                    return b ? 1 : 0;
                }
                else if (v is IConvertible convertible)
                {
                    try
                    {
                        return Convert.ToDouble(convertible);
                    }
                    catch
                    {
                        return double.NaN;
                    }
                }
                return double.NaN;
            })
            .Where(v => !double.IsNaN(v))
            .ToList();

            if (!numericValues.Any())
                return 0;

            return aggregation.ToLower() switch
            {
                "sum" => numericValues.Sum(),
                "avg" => numericValues.Average(),
                "min" => numericValues.Min(),
                "max" => numericValues.Max(),
                _ => numericValues.Sum() // default fallback
            };
        }

        private int CompareValues(object value1, object value2)
        {
            if (value1 == null && value2 == null) return 0;
            if (value1 == null) return -1;
            if (value2 == null) return 1;

            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
            {
                return comparable1.CompareTo(comparable2);
            }

            return value1.ToString().CompareTo(value2.ToString());
        }

        private int GetWeekNumber(DateTime date)
        {
            var calendar = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            return calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private string GetStringValue(Dictionary<string, object> data, string field)
        {
            if (data.TryGetValue(field, out var value) && value != null)
            {
                return value.ToString();
            }
            return "Unknown";
        }
    }
} 