using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DataVisualizationAPI.Models;
using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.Data;
using DataVisualizationAPI.DTOs.Dropdown;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DataVisualizationAPI.Services
{
    public interface IDatasetService
    {
        Task<PaginatedResponse<DatasetListResponse>> ListDatasetsAsync(PaginationRequest request, string userId);
        Task<DataSourceDetailResponse> GetDataSourceDetailAsync(int datasetId, DataSourceDetailRequest request);
        Task<ImportDatasetResponse> MergeDatasetsAsync(DatasetMergeRequest request, string userId);
        Task<List<DatasetDropdownResponse>> GetDatasetsForDropdownAsync(string userId);
    }

    public class DatasetService : IDatasetService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatasetService> _logger;

        public DatasetService(AppDbContext context, ILogger<DatasetService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaginatedResponse<DatasetListResponse>> ListDatasetsAsync(PaginationRequest request, string userId)
        {
            try
            {
                var query = _context.Datasets.AsNoTracking()
                    .Where(d => d.CreatedBy == userId); // Filter by user ID from token

                // Get total count
                var totalCount = await query.CountAsync();

                // Calculate pagination
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;

                // Get paginated data
                var datasets = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip(skip)
                    .Take(request.PageSize)
                    .Select(d => new DatasetListResponse
                    {
                        DatasetId = d.DatasetId,
                        DatasetName = d.DatasetName,
                        SourceType = d.SourceType,
                        SourceName = d.SourceName,
                        CreatedAt = d.CreatedAt,
                        CreatedBy = d.CreatedBy,
                        TotalRows = d.TotalRows,
                        Status = d.Status,
                        Columns = d.Schemas
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
                    })
                    .ToListAsync();

                return new PaginatedResponse<DatasetListResponse>
                {
                    Items = datasets,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing datasets");
                throw;
            }
        }

        public async Task<DataSourceDetailResponse> GetDataSourceDetailAsync(int datasetId, DataSourceDetailRequest request)
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

                // Get all data for the dataset
                var data = await _context.DatasetData
                    .Where(d => d.DatasetId == datasetId)
                    .Select(d => d.RowData)
                    .ToListAsync();

                // Convert JSON strings to dictionaries
                var items = data.Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d)).ToList();

                // Apply filters
                if (request.Filters != null && request.Filters.Any())
                {
                    foreach (var filter in request.Filters)
                    {
                        items = items.Where(item => 
                        {
                            if (!item.ContainsKey(filter.Field) || item[filter.Field] == null)
                                return false;

                            var itemValue = item[filter.Field];
                            var filterValue = filter.Value;

                            // Try to parse as number first
                            if (long.TryParse(itemValue?.ToString(), out var itemLong) && 
                                long.TryParse(filterValue, out var filterLong))
                            {
                                return CompareValues(itemLong, filterLong, filter.Operator);
                            }
                            if (decimal.TryParse(itemValue?.ToString(), out var itemDecimal) && 
                                decimal.TryParse(filterValue, out var filterDecimal))
                            {
                                return CompareValues(itemDecimal, filterDecimal, filter.Operator);
                            }

                            // If not a number, try DateTime
                            if (DateTime.TryParse(itemValue?.ToString(), out var itemDate) && 
                                DateTime.TryParse(filterValue, out var filterDate))
                            {
                                return CompareValues(itemDate, filterDate, filter.Operator);
                            }

                            // If not a DateTime, try boolean
                            if (bool.TryParse(itemValue?.ToString(), out var itemBool) && 
                                bool.TryParse(filterValue, out var filterBool))
                            {
                                return CompareValues(itemBool, filterBool, filter.Operator);
                            }

                            // If none of the above, compare as string
                            return CompareValues(
                                itemValue?.ToString() ?? string.Empty, 
                                filterValue ?? string.Empty, 
                                filter.Operator);
                        }).ToList();
                    }
                }

                // Get total count before pagination
                var totalCount = items.Count;

                // Apply sorting
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    var isAscending = string.IsNullOrEmpty(request.SortDirection) || 
                                    request.SortDirection.ToLower() == "asc";

                    items = isAscending
                        ? items.OrderBy(item => GetSortableValue(item, request.SortBy)).ToList()
                        : items.OrderByDescending(item => GetSortableValue(item, request.SortBy)).ToList();
                }
                else
                {
                    // Default sorting by CreatedAt in descending order
                    items = items.OrderByDescending(item => 
                        item.ContainsKey("CreatedAt") && DateTime.TryParse(item["CreatedAt"]?.ToString(), out var date) 
                            ? date 
                            : DateTime.MinValue).ToList();
                }

                // Apply pagination
                items = items
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Get column statistics and distribution
                var columns = new List<ColumnDetail>();
                foreach (var schema in dataset.Schemas.OrderBy(s => s.ColumnOrder))
                {
                    var (statistics, distribution) = await GetColumnStatisticsAsync(
                        datasetId, 
                        schema.ColumnName);

                    columns.Add(new ColumnDetail
                    {
                        Name = schema.ColumnName,
                        DisplayName = schema.DisplayName,
                        DataType = schema.DataType,
                        IsRequired = schema.IsRequired,
                        Description = schema.Description,
                        Order = schema.ColumnOrder,
                        Statistics = statistics,
                        ValueDistribution = distribution
                    });
                }

                return new DataSourceDetailResponse
                {
                    DatasetId = dataset.DatasetId,
                    DatasetName = dataset.DatasetName,
                    SourceType = dataset.SourceType,
                    SourceName = dataset.SourceName,
                    CreatedAt = dataset.CreatedAt,
                    CreatedBy = dataset.CreatedBy,
                    TotalRows = dataset.TotalRows,
                    Status = dataset.Status,
                    Columns = columns,
                    Data = new PaginatedResponse<Dictionary<string, object>>
                    {
                        Items = items,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data source detail for dataset {DatasetId}", datasetId);
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

        private bool CompareValues<T>(T value1, T value2, string op) where T : IComparable<T>
        {
            return op.ToLower() switch
            {
                "eq" => value1.CompareTo(value2) == 0,
                "neq" => value1.CompareTo(value2) != 0,
                "gt" => value1.CompareTo(value2) > 0,
                "gte" => value1.CompareTo(value2) >= 0,
                "lt" => value1.CompareTo(value2) < 0,
                "lte" => value1.CompareTo(value2) <= 0,
                "contains" => value1.ToString().Contains(value2.ToString()),
                "startswith" => value1.ToString().StartsWith(value2.ToString()),
                "endswith" => value1.ToString().EndsWith(value2.ToString()),
                _ => value1.CompareTo(value2) == 0
            };
        }

        private async Task<(ColumnStatistics statistics, Dictionary<string, int> distribution)> GetColumnStatisticsAsync(
            int datasetId, 
            string columnName)
        {
            var data = await _context.DatasetData
                .Where(d => d.DatasetId == datasetId)
                .Select(d => d.RowData)
                .ToListAsync();

            var deserializedData = data.Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d)).ToList();

            var statistics = new ColumnStatistics
            {
                TotalValues = deserializedData.Count,
                NullCount = 0,
                UniqueCount = 0,
                Min = null,
                Max = null,
                Average = null,
                MostCommonValue = null,
                MostCommonCount = 0
            };

            var distribution = new Dictionary<string, int>();
            var valueCounts = new Dictionary<string, int>();
            var numericValues = new List<double>();

            foreach (var row in deserializedData)
            {
                if (row.TryGetValue(columnName, out var value))
                {
                    var stringValue = value?.ToString() ?? "null";
                    
                    // Count nulls
                    if (stringValue == "null")
                    {
                        statistics.NullCount++;
                    }

                    // Count value distribution
                    if (valueCounts.ContainsKey(stringValue))
                    {
                        valueCounts[stringValue]++;
                    }
                    else
                    {
                        valueCounts[stringValue] = 1;
                    }

                    // Try to parse numeric values
                    if (double.TryParse(stringValue, out var numericValue))
                    {
                        numericValues.Add(numericValue);
                    }
                }
            }

            // Calculate statistics
            statistics.UniqueCount = valueCounts.Count;

            if (numericValues.Any())
            {
                statistics.Min = numericValues.Min();
                statistics.Max = numericValues.Max();
                statistics.Average = numericValues.Average();
            }

            // Find most common value
            var mostCommon = valueCounts.OrderByDescending(x => x.Value).FirstOrDefault();
            if (mostCommon.Key != null)
            {
                statistics.MostCommonValue = mostCommon.Key;
                statistics.MostCommonCount = mostCommon.Value;
            }

            // Get distribution (top 10 most common values)
            distribution = valueCounts
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToDictionary(x => x.Key, x => x.Value);

            return (statistics, distribution);
        }

        public async Task<ImportDatasetResponse> MergeDatasetsAsync(DatasetMergeRequest request, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate datasets exist and belong to user
                var leftDataset = await _context.Datasets
                    .Include(d => d.Schemas)
                    .FirstOrDefaultAsync(d => d.DatasetId == request.LeftDatasetId && d.CreatedBy == userId);
                
                var rightDataset = await _context.Datasets
                    .Include(d => d.Schemas)
                    .FirstOrDefaultAsync(d => d.DatasetId == request.RightDatasetId && d.CreatedBy == userId);

                if (leftDataset == null || rightDataset == null)
                {
                    throw new KeyNotFoundException("One or both datasets not found or access denied");
                }

                // 2. Create new dataset
                var newDataset = new Dataset
                {
                    DatasetName = request.NewDatasetName,
                    SourceType = "merged",
                    SourceName = $"Merged from {leftDataset.DatasetName} and {rightDataset.DatasetName}",
                    CreatedBy = userId,
                    Status = "Processing"
                };
                _context.Datasets.Add(newDataset);
                await _context.SaveChangesAsync();

                // 3. Get data from both datasets
                var leftData = await _context.DatasetData
                    .Where(d => d.DatasetId == request.LeftDatasetId)
                    .Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d.RowData, new JsonSerializerOptions()))
                    .ToListAsync();

                var rightData = await _context.DatasetData
                    .Where(d => d.DatasetId == request.RightDatasetId)
                    .Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d.RowData, new JsonSerializerOptions()))
                    .ToListAsync();

                // 4. Create schema for new dataset
                var newSchemas = new List<DatasetSchema>();
                var columnOrder = 0;

                // Add columns from left dataset
                foreach (var schema in leftDataset.Schemas.OrderBy(s => s.ColumnOrder))
                {
                    newSchemas.Add(new DatasetSchema
                    {
                        DatasetId = newDataset.DatasetId,
                        ColumnName = $"Left_{schema.ColumnName}",
                        DataType = schema.DataType,
                        IsRequired = schema.IsRequired,
                        DisplayName = $"{leftDataset.DatasetName}.{schema.DisplayName}",
                        ColumnOrder = columnOrder++,
                        Description = $"From {leftDataset.DatasetName}: {schema.Description}"
                    });
                }

                // Add columns from right dataset
                foreach (var schema in rightDataset.Schemas.OrderBy(s => s.ColumnOrder))
                {
                    newSchemas.Add(new DatasetSchema
                    {
                        DatasetId = newDataset.DatasetId,
                        ColumnName = $"Right_{schema.ColumnName}",
                        DataType = schema.DataType,
                        IsRequired = schema.IsRequired,
                        DisplayName = $"{rightDataset.DatasetName}.{schema.DisplayName}",
                        ColumnOrder = columnOrder++,
                        Description = $"From {rightDataset.DatasetName}: {schema.Description}"
                    });
                }

                _context.DatasetSchemas.AddRange(newSchemas);
                await _context.SaveChangesAsync();

                // 5. Merge data based on join conditions
                var mergedData = new List<Dictionary<string, object>>();
                var rowCount = 0;

                switch (request.MergeType.ToLower())
                {
                    case "inner":
                        mergedData = PerformInnerJoin(leftData, rightData, request.JoinConditions);
                        break;
                    case "left":
                        mergedData = PerformLeftJoin(leftData, rightData, request.JoinConditions);
                        break;
                    case "right":
                        mergedData = PerformRightJoin(leftData, rightData, request.JoinConditions);
                        break;
                    case "full":
                        mergedData = PerformFullJoin(leftData, rightData, request.JoinConditions);
                        break;
                    case "cross":
                        mergedData = PerformCrossJoin(leftData, rightData);
                        break;
                    default:
                        throw new ArgumentException($"Invalid merge type: {request.MergeType}");
                }

                // Kiểm tra nếu không có record nào được tạo ra
                if (!mergedData.Any())
                {
                    throw new InvalidOperationException("No matching data found for merge. Please check your join conditions.");
                }

                // 6. Save merged data
                const int batchSize = 1000;
                for (int i = 0; i < mergedData.Count; i += batchSize)
                {
                    var batch = mergedData.Skip(i).Take(batchSize);
                    foreach (var row in batch)
                    {
                        _context.DatasetData.Add(new DatasetData
                        {
                            DatasetId = newDataset.DatasetId,
                            RowData = JsonSerializer.Serialize(row)
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 7. Update dataset status
                newDataset.Status = "Completed";
                newDataset.TotalRows = mergedData.Count;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return new ImportDatasetResponse
                {
                    DatasetId = newDataset.DatasetId,
                    Status = "Success",
                    Message = $"Successfully merged datasets with {mergedData.Count} rows"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error merging datasets");
                return new ImportDatasetResponse
                {
                    Status = "Error",
                    Message = "Failed to merge datasets: " + ex.Message
                };
            }
        }

        private List<Dictionary<string, object>> PerformInnerJoin(
            List<Dictionary<string, object>> leftData,
            List<Dictionary<string, object>> rightData,
            List<MergeJoinCondition> conditions)
        {
            var result = new List<Dictionary<string, object>>();
            
            foreach (var left in leftData)
            {
                foreach (var right in rightData)
                {
                    // Kiểm tra tất cả điều kiện join
                    var isMatch = conditions.All(c => 
                    {
                        var leftValue = left[c.LeftColumn]?.ToString();
                        var rightValue = right[c.RightColumn]?.ToString();
                        
                        // Log để debug
                        _logger.LogDebug($"Comparing {c.LeftColumn}={leftValue} with {c.RightColumn}={rightValue} using operator {c.Operator}");
                        
                        return CompareValues(leftValue ?? string.Empty, rightValue ?? string.Empty, c.Operator);
                    });

                    if (isMatch)
                    {
                        result.Add(MergeRows(left, right));
                    }
                }
            }

            return result;
        }

        private List<Dictionary<string, object>> PerformLeftJoin(
            List<Dictionary<string, object>> leftData,
            List<Dictionary<string, object>> rightData,
            List<MergeJoinCondition> conditions)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var left in leftData)
            {
                var matchingRights = rightData.Where(right =>
                    conditions.All(c => CompareValues(
                        left[c.LeftColumn]?.ToString(),
                        right[c.RightColumn]?.ToString(),
                        c.Operator.ToString())));

                if (matchingRights.Any())
                {
                    result.AddRange(matchingRights.Select(right => MergeRows(left, right)));
                }
                else
                {
                    result.Add(MergeRows(left, null));
                }
            }
            return result;
        }

        private List<Dictionary<string, object>> PerformRightJoin(
            List<Dictionary<string, object>> leftData,
            List<Dictionary<string, object>> rightData,
            List<MergeJoinCondition> conditions)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var right in rightData)
            {
                var matchingLefts = leftData.Where(left =>
                    conditions.All(c => CompareValues(
                        left[c.LeftColumn]?.ToString(),
                        right[c.RightColumn]?.ToString(),
                        c.Operator.ToString())));

                if (matchingLefts.Any())
                {
                    result.AddRange(matchingLefts.Select(left => MergeRows(left, right)));
                }
                else
                {
                    result.Add(MergeRows(null, right));
                }
            }
            return result;
        }

        private List<Dictionary<string, object>> PerformFullJoin(
            List<Dictionary<string, object>> leftData,
            List<Dictionary<string, object>> rightData,
            List<MergeJoinCondition> conditions)
        {
            var result = new List<Dictionary<string, object>>();
            result.AddRange(PerformLeftJoin(leftData, rightData, conditions));
            result.AddRange(PerformRightJoin(leftData, rightData, conditions));
            return result.Distinct(new DictionaryComparer()).ToList();
        }

        private List<Dictionary<string, object>> PerformCrossJoin(
            List<Dictionary<string, object>> leftData,
            List<Dictionary<string, object>> rightData)
        {
            return leftData.SelectMany(left =>
                rightData.Select(right => MergeRows(left, right)))
                .ToList();
        }

        private Dictionary<string, object> MergeRows(
            Dictionary<string, object> left,
            Dictionary<string, object> right)
        {
            var result = new Dictionary<string, object>();

            if (left != null)
            {
                foreach (var kvp in left)
                {
                    result[$"Left_{kvp.Key}"] = kvp.Value;
                }
            }

            if (right != null)
            {
                foreach (var kvp in right)
                {
                    result[$"Right_{kvp.Key}"] = kvp.Value;
                }
            }

            return result;
        }

        private class DictionaryComparer : IEqualityComparer<Dictionary<string, object>>
        {
            public bool Equals(Dictionary<string, object> x, Dictionary<string, object> y)
            {
                if (x == null || y == null)
                    return x == y;

                if (x.Count != y.Count)
                    return false;

                return x.All(kvp => y.ContainsKey(kvp.Key) && 
                    Equals(kvp.Value?.ToString(), y[kvp.Key]?.ToString()));
            }

            public int GetHashCode(Dictionary<string, object> obj)
            {
                if (obj == null)
                    return 0;

                return obj.Aggregate(0, (hash, kvp) =>
                    hash ^ (kvp.Key?.GetHashCode() ?? 0) ^ 
                    (kvp.Value?.ToString()?.GetHashCode() ?? 0));
            }
        }

        public async Task<List<DatasetDropdownResponse>> GetDatasetsForDropdownAsync(string userId)
        {
            try
            {
                var datasets = await _context.Datasets
                    .Include(d => d.Schemas)
                    .Where(d => d.CreatedBy == userId)
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new DatasetDropdownResponse
                    {
                        DatasetId = d.DatasetId,
                        DatasetName = d.DatasetName,
                        Columns = d.Schemas
                            .OrderBy(s => s.ColumnOrder)
                            .Select(s => new DropdownColumnInfo
                            {
                                ColumnName = s.ColumnName,
                                DisplayName = s.DisplayName,
                                DataType = s.DataType
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return datasets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting datasets for dropdown");
                throw;
            }
        }
    }
} 