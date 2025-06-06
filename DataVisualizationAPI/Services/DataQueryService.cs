using DataVisualizationAPI.Data;
using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataVisualizationAPI.Services
{
    public class DataQueryService : IDataQueryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataQueryService> _logger;

        public DataQueryService(AppDbContext context, ILogger<DataQueryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<QueryResult> QueryDataAsync(int datasetId, QueryParameters parameters)
        {
            try
            {
                // Get all data for the dataset
                var data = await _context.DatasetData
                    .Where(d => d.DatasetId == datasetId)
                    .ToListAsync();

                // Convert JSON strings to dictionaries
                var items = data.Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d.RowData)).ToList();

                // Apply filters
                if (parameters.Filters != null && parameters.Filters.Any())
                {
                    foreach (var filter in parameters.Filters)
                    {
                        items = items.Where(item => 
                            item.ContainsKey(filter.Field) && 
                            item[filter.Field]?.ToString() == filter.Value).ToList();
                    }
                }

                // Get total count before pagination
                var totalCount = items.Count;

                // Apply sorting
                if (!string.IsNullOrEmpty(parameters.SortBy))
                {
                    var isAscending = string.IsNullOrEmpty(parameters.SortDirection) || 
                                    parameters.SortDirection.ToLower() == "asc";

                    items = isAscending
                        ? items.OrderBy(item => GetSortableValue(item, parameters.SortBy)).ToList()
                        : items.OrderByDescending(item => GetSortableValue(item, parameters.SortBy)).ToList();
                }

                // Apply pagination
                items = items
                    .Skip((parameters.Page - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToList();

                return new QueryResult
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = parameters.Page,
                    PageSize = parameters.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying data for dataset {DatasetId}", datasetId);
                throw;
            }
        }

        private IComparable GetSortableValue(Dictionary<string, object> item, string key)
        {
            if (!item.ContainsKey(key) || item[key] == null)
                return null;

            var value = item[key];

            // Handle primitive types directly
            if (value is string stringValue)
            {
                // Try to parse as number first
                if (long.TryParse(stringValue, out var longResult))
                    return longResult;
                if (decimal.TryParse(stringValue, out var decimalResult))
                    return decimalResult;
                return stringValue;
            }
            
            if (value is int intValue) return intValue;
            if (value is long longValue) return longValue;
            if (value is decimal decimalValue) return decimalValue;
            if (value is double doubleValue) return doubleValue;
            if (value is DateTime dateTimeValue) return dateTimeValue;
            if (value is bool boolValue) return boolValue;

            // For other types, try to convert to string and parse as number
            var strValue = value?.ToString() ?? string.Empty;
            if (long.TryParse(strValue, out var parsedLong))
                return parsedLong;
            if (decimal.TryParse(strValue, out var parsedDecimal))
                return parsedDecimal;
            
            return strValue;
        }

        public async Task<DatasetInfo> GetDatasetInfoAsync(int datasetId)
        {
            try
            {
                var dataset = await _context.Datasets
                    .Include(d => d.Schemas)
                    .FirstOrDefaultAsync(d => d.DatasetId == datasetId);

                if (dataset == null)
                {
                    throw new KeyNotFoundException($"Dataset with ID {datasetId} not found");
                }

                return new DatasetInfo
                {
                    DatasetId = dataset.DatasetId,
                    DatasetName = dataset.DatasetName,
                    SourceType = dataset.SourceType,
                    SourceName = dataset.SourceName,
                    CreatedAt = dataset.CreatedAt,
                    TotalRows = dataset.TotalRows,
                    Status = dataset.Status,
                    Columns = dataset.Schemas
                        .OrderBy(s => s.ColumnOrder)
                        .Select(s => new DTOs.DatasetColumnInfo
                        {
                            ColumnName = s.ColumnName,
                            DisplayName = s.DisplayName,
                            DataType = s.DataType,
                            IsRequired = s.IsRequired,
                            Description = s.Description,
                            Order = s.ColumnOrder
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dataset info for dataset {DatasetId}", datasetId);
                throw;
            }
        }
    }
} 