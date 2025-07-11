using DataVisualizationAPI.Data;
using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using OfficeOpenXml;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DataVisualizationAPI.Services
{
    public class DataImportService : IDataImportService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataImportService> _logger;

        public DataImportService(AppDbContext context, ILogger<DataImportService> logger)
        {
            _context = context;
            _logger = logger;
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ImportDatasetResponse> ImportDataAsync(IFormFile file, string datasetName, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create dataset record
                var dataset = new Dataset
                {
                    DatasetName = datasetName,
                    SourceType = Path.GetExtension(file.FileName).ToLower(),
                    SourceName = file.FileName,
                    CreatedBy = userId,
                    Status = "Processing"
                };
                _context.Datasets.Add(dataset);
                await _context.SaveChangesAsync();

                // 2. Read and analyze file
                var (schema, data) = await ReadAndAnalyzeFileAsync(file);

                // 3. Save schema
                foreach (var column in schema)
                {
                    _context.DatasetSchemas.Add(new DatasetSchema
                    {
                        DatasetId = dataset.DatasetId,
                        ColumnName = column.Name,
                        DataType = column.DataType,
                        IsRequired = column.IsRequired,
                        DisplayName = column.DisplayName,
                        ColumnOrder = column.Order,
                        Description = $"Column {column.DisplayName} of type {column.DataType}"
                    });
                }
                await _context.SaveChangesAsync();

                // 4. Save data in batches
                const int batchSize = 1000;
                for (int i = 0; i < data.Count; i += batchSize)
                {
                    var batch = data.Skip(i).Take(batchSize);
                    foreach (var row in batch)
                    {
                        _context.DatasetData.Add(new DatasetData
                        {
                            DatasetId = dataset.DatasetId,
                            RowData = JsonSerializer.Serialize(row)
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 5. Update dataset status
                dataset.Status = "Completed";
                dataset.TotalRows = data.Count;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return new ImportDatasetResponse 
                { 
                    DatasetId = dataset.DatasetId,
                    Status = "Success",
                    Message = "Data imported successfully"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error importing data");
                return new ImportDatasetResponse
                {
                    Status = "Error",
                    Message = "Failed to import data: " + ex.Message
                };
            }
        }

        public async Task<ImportDatasetResponse> ImportFromDatabaseAsync(string connectionString, string query, string datasetName, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create dataset record
                var dataset = new Dataset
                {
                    DatasetName = datasetName,
                    SourceType = "database",
                    SourceName = "Database Query",
                    CreatedBy = userId,
                    Status = "Processing"
                };
                _context.Datasets.Add(dataset);
                await _context.SaveChangesAsync();

                // 2. Read data from source database
                var (schema, data) = await ReadFromDatabaseAsync(connectionString, query);

                // 3. Save schema
                foreach (var column in schema)
                {
                    _context.DatasetSchemas.Add(new DatasetSchema
                    {
                        DatasetId = dataset.DatasetId,
                        ColumnName = column.Name,
                        DataType = column.DataType,
                        IsRequired = column.IsRequired,
                        DisplayName = column.DisplayName,
                        ColumnOrder = column.Order,
                        Description = $"Column {column.DisplayName} of type {column.DataType}"
                    });
                }
                await _context.SaveChangesAsync();

                // 4. Save data in batches
                const int batchSize = 1000;
                for (int i = 0; i < data.Count; i += batchSize)
                {
                    var batch = data.Skip(i).Take(batchSize);
                    foreach (var row in batch)
                    {
                        _context.DatasetData.Add(new DatasetData
                        {
                            DatasetId = dataset.DatasetId,
                            RowData = JsonSerializer.Serialize(row)
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 5. Update dataset status
                dataset.Status = "Completed";
                dataset.TotalRows = data.Count;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return new ImportDatasetResponse
                {
                    DatasetId = dataset.DatasetId,
                    Status = "Success",
                    Message = "Data imported successfully from database"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error importing data from database");
                return new ImportDatasetResponse
                {
                    Status = "Error",
                    Message = "Failed to import data from database: " + ex.Message
                };
            }
        }

        private async Task<(List<ColumnSchema> schema, List<Dictionary<string, object>> data)> ReadFromDatabaseAsync(string connectionString, string query)
        {
            var schema = new List<ColumnSchema>();
            var data = new List<Dictionary<string, object>>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Read schema
                    var schemaTable = reader.GetSchemaTable();
                    if (schemaTable != null)
                    {
                        int order = 0;
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            schema.Add(new ColumnSchema
                            {
                                Name = row["ColumnName"].ToString(),
                                DataType = GetDataTypeName(row["DataType"].ToString()),
                                IsRequired = !(bool)row["AllowDBNull"],
                                DisplayName = row["ColumnName"].ToString(),
                                Order = order++
                            });
                        }
                    }

                    // Read data
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        data.Add(row);
                    }
                }
            }

            return (schema, data);
        }

        private string GetDataTypeName(string sqlDataType)
        {
            return sqlDataType.ToLower() switch
            {
                "system.int32" => "int",
                "system.int64" => "long",
                "system.string" => "string",
                "system.datetime" => "datetime",
                "system.decimal" => "decimal",
                "system.double" => "double",
                "system.boolean" => "boolean",
                _ => "string"
            };
        }

        private async Task<(List<ColumnSchema> schema, List<Dictionary<string, object>> data)> ReadAndAnalyzeFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            
            return extension switch
            {
                ".csv" => await ReadCsvFileAsync(file),
                ".xlsx" or ".xls" => await ReadExcelFileAsync(file),
                _ => throw new ArgumentException($"Unsupported file type: {extension}")
            };
        }

        private async Task<(List<ColumnSchema> schema, List<Dictionary<string, object>> data)> ReadCsvFileAsync(IFormFile file)
        {
            var schema = new List<ColumnSchema>();
            var data = new List<Dictionary<string, object>>();

            List<string> headers;

            // First pass: sample rows to infer schema
            var sampleRows = new List<Dictionary<string, string>>();
            const int sampleSize = 100;

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                headers = csv.HeaderRecord.ToList();

                int rowCount = 0;
                while (csv.Read() && rowCount < sampleSize)
                {
                    var row = new Dictionary<string, string>();
                    foreach (var header in headers)
                    {
                        row[header] = csv.GetField(header);
                    }
                    sampleRows.Add(row);
                    rowCount++;
                }
            }

            // Determine column types and create schema
            int order = 0;
            foreach (var header in headers)
            {
                var columnType = DetermineColumnType(sampleRows.Select(r => r[header]).ToList());
                schema.Add(new ColumnSchema
                {
                    Name = header,
                    DataType = columnType,
                    IsRequired = !sampleRows.Any(r => string.IsNullOrEmpty(r[header])),
                    DisplayName = header,
                    Order = order++
                });
            }

            // Second pass: read entire CSV data
            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    var row = new Dictionary<string, object>();
                    foreach (var header in headers)
                    {
                        var value = csv.GetField(header);
                        row[header] = ConvertValue(value, schema.First(s => s.Name == header).DataType);
                    }
                    data.Add(row);
                }
            }

            return (schema, data);
        }

        private async Task<(List<ColumnSchema> schema, List<Dictionary<string, object>> data)> ReadExcelFileAsync(IFormFile file)
        {
            var schema = new List<ColumnSchema>();
            var data = new List<Dictionary<string, object>>();

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0]; // Get first worksheet

            // Get header row
            var headers = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text);
            }

            // Analyze first few rows to determine data types
            var sampleRows = new List<Dictionary<string, string>>();
            var rowCount = 0;
            const int sampleSize = 100;

            for (int row = 2; row <= Math.Min(worksheet.Dimension.End.Row, sampleSize + 1); row++)
            {
                var rowData = new Dictionary<string, string>();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    rowData[headers[col - 1]] = worksheet.Cells[row, col].Text;
                }
                sampleRows.Add(rowData);
                rowCount++;
            }

            // Determine column types and create schema
            int order = 0;
            foreach (var header in headers)
            {
                var columnType = DetermineColumnType(sampleRows.Select(r => r[header]).ToList());
                schema.Add(new ColumnSchema
                {
                    Name = header,
                    DataType = columnType,
                    IsRequired = !sampleRows.Any(r => string.IsNullOrEmpty(r[header])),
                    DisplayName = header,
                    Order = order++
                });
            }

            // Read all data
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var header = headers[col - 1];
                    var value = worksheet.Cells[row, col].Text;
                    rowData[header] = ConvertValue(value, schema.First(s => s.Name == header).DataType);
                }
                data.Add(rowData);
            }

            return (schema, data);
        }

        private string DetermineColumnType(List<string> values)
        {
            if (values.All(v => string.IsNullOrEmpty(v)))
                return "string";

            if (values.All(v => string.IsNullOrEmpty(v) || int.TryParse(v, out _)))
                return "int";

            if (values.All(v => string.IsNullOrEmpty(v) || long.TryParse(v, out _)))
                return "long";

            if (values.All(v => string.IsNullOrEmpty(v) || decimal.TryParse(v, out _)))
                return "decimal";

            if (values.All(v => string.IsNullOrEmpty(v) || double.TryParse(v, out _)))
                return "double";

            if (values.All(v => string.IsNullOrEmpty(v) || DateTime.TryParse(v, out _)))
                return "datetime";

            if (values.All(v => string.IsNullOrEmpty(v) || bool.TryParse(v, out _)))
                return "boolean";

            return "string";
        }

        private object ConvertValue(string value, string targetType)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            return targetType switch
            {
                "int" => int.TryParse(value, out var intResult) ? intResult : null,
                "long" => long.TryParse(value, out var longResult) ? longResult : null,
                "decimal" => decimal.TryParse(value, out var decimalResult) ? decimalResult : null,
                "double" => double.TryParse(value, out var doubleResult) ? doubleResult : null,
                "datetime" => DateTime.TryParse(value, out var dateResult) ? dateResult : null,
                "boolean" => bool.TryParse(value, out var boolResult) ? boolResult : null,
                _ => value
            };
        }

        public async Task<ImportResult> ImportFromFileAsync(int datasetId, IFormFile file)
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

                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("No file was uploaded");
                }

                // Read file content
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                // Read header row
                csv.Read();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                // Validate headers against schema
                var schemaColumns = dataset.Schemas.Select(s => s.ColumnName).ToList();
                var missingColumns = schemaColumns.Except(headers, StringComparer.OrdinalIgnoreCase).ToList();
                if (missingColumns.Any())
                {
                    throw new InvalidOperationException($"Missing required columns: {string.Join(", ", missingColumns)}");
                }

                // Read and process data
                var rows = new List<DatasetData>();
                var rowCount = 0;

                while (csv.Read())
                {
                    var rowData = new Dictionary<string, object>();
                    foreach (var header in headers)
                    {
                        var value = csv.GetField(header);
                        rowData[header] = value;
                    }

                    rows.Add(new DatasetData
                    {
                        DatasetId = datasetId,
                        RowData = JsonSerializer.Serialize(rowData),
                        CreatedAt = DateTime.UtcNow
                    });

                    rowCount++;

                    // Save in batches of 1000
                    if (rowCount % 1000 == 0)
                    {
                        await _context.DatasetData.AddRangeAsync(rows);
                        await _context.SaveChangesAsync();
                        rows.Clear();
                    }
                }

                // Save any remaining rows
                if (rows.Any())
                {
                    await _context.DatasetData.AddRangeAsync(rows);
                    await _context.SaveChangesAsync();
                }

                // Update dataset status
                dataset.Status = "Completed";
                dataset.TotalRows = rowCount;
                await _context.SaveChangesAsync();

                return new ImportResult
                {
                    DatasetId = datasetId,
                    Status = "Success",
                    Message = $"Successfully imported {rowCount} rows"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data from file for dataset {DatasetId}", datasetId);

                // Update dataset status to error
                var dataset = await _context.Datasets.FindAsync(datasetId);
                if (dataset != null)
                {
                    dataset.Status = "Error";
                    await _context.SaveChangesAsync();
                }

                return new ImportResult
                {
                    DatasetId = datasetId,
                    Status = "Error",
                    Message = $"Failed to import data: {ex.Message}"
                };
            }
        }

        public async Task<ImportDatasetResponse> ImportFromRestApiAsync(string apiUrl, string datasetName, string userId, RestApiImportOptions options = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create dataset record
                var dataset = new Dataset
                {
                    DatasetName = datasetName,
                    SourceType = "api",
                    SourceName = apiUrl,
                    CreatedBy = userId,
                    Status = "Processing"
                };
                _context.Datasets.Add(dataset);
                await _context.SaveChangesAsync();

                // 2. Fetch data from API
                var data = new List<Dictionary<string, object>>();
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? 30);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // Add custom headers
                    if (options?.Headers != null)
                    {
                        foreach (var header in options.Headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }

                    // Build request URL with query parameters
                    var requestUrl = apiUrl;
                    if (options?.QueryParameters != null && options.QueryParameters.Any())
                    {
                        var queryString = string.Join("&", options.QueryParameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                        requestUrl += (apiUrl.Contains("?") ? "&" : "?") + queryString;
                    }

                    HttpResponseMessage response;
                    if (options?.HttpMethod?.ToUpper() == "POST" && !string.IsNullOrEmpty(options.RequestBody))
                    {
                        var content = new StringContent(options.RequestBody, System.Text.Encoding.UTF8, "application/json");
                        response = await client.PostAsync(requestUrl, content);
                    }
                    else
                    {
                        response = await client.GetAsync(requestUrl);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to fetch data from API: {response.StatusCode} - {response.ReasonPhrase}");
                    }

                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var jsonObject = JToken.Parse(jsonContent);

                    // Extract data using JSON path if specified
                    JToken dataToken = jsonObject;
                    if (!string.IsNullOrEmpty(options?.DataPath))
                    {
                        dataToken = jsonObject.SelectToken(options.DataPath);
                        if (dataToken == null)
                        {
                            throw new Exception($"Data path '{options.DataPath}' not found in API response");
                        }
                    }

                    // Process data array
                    if (dataToken.Type == JTokenType.Array)
                    {
                        var maxRecords = options?.MaxRecords ?? 1000;
                        var recordCount = 0;
                        
                        foreach (JToken item in dataToken)
                        {
                            if (recordCount >= maxRecords) break;
                            
                            var row = FlattenJsonObject(item, options?.FlattenNestedObjects ?? true);
                            data.Add(row);
                            recordCount++;
                        }
                    }
                    else if (dataToken.Type == JTokenType.Object)
                    {
                        // Single object, wrap in array
                        var row = FlattenJsonObject(dataToken, options?.FlattenNestedObjects ?? true);
                        data.Add(row);
                    }
                    else
                    {
                        throw new Exception("API response does not contain an array or object of data");
                    }
                }

                // 3. Save schema
                var schema = new List<ColumnSchema>();
                if (data.Any())
                {
                    // Infer schema from all rows to handle missing fields
                    var allKeys = data.SelectMany(row => row.Keys).Distinct().ToList();
                    foreach (var key in allKeys)
                    {
                        var values = data.Where(row => row.ContainsKey(key))
                                       .Select(row => row[key]?.ToString() ?? "")
                                       .ToList();
                        
                        schema.Add(new ColumnSchema
                        {
                            Name = key,
                            DataType = DetermineColumnType(values),
                            IsRequired = false, // API data is often optional
                            DisplayName = key,
                            Order = schema.Count
                        });
                    }
                }
                else
                {
                    throw new Exception("No data received from API to infer schema");
                }

                foreach (var column in schema)
                {
                    _context.DatasetSchemas.Add(new DatasetSchema
                    {
                        DatasetId = dataset.DatasetId,
                        ColumnName = column.Name,
                        DataType = column.DataType,
                        IsRequired = column.IsRequired,
                        DisplayName = column.DisplayName,
                        ColumnOrder = column.Order,
                        Description = $"Column {column.DisplayName} of type {column.DataType}"
                    });
                }
                await _context.SaveChangesAsync();

                // 4. Save data in batches
                const int batchSize = 1000;
                for (int i = 0; i < data.Count; i += batchSize)
                {
                    var batch = data.Skip(i).Take(batchSize);
                    foreach (var row in batch)
                    {
                        _context.DatasetData.Add(new DatasetData
                        {
                            DatasetId = dataset.DatasetId,
                            RowData = JsonSerializer.Serialize(row)
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 5. Update dataset status
                dataset.Status = "Completed";
                dataset.TotalRows = data.Count;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return new ImportDatasetResponse
                {
                    DatasetId = dataset.DatasetId,
                    Status = "Success",
                    Message = $"Data imported successfully from API. {data.Count} records imported."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error importing data from API: {ApiUrl}", apiUrl);
                return new ImportDatasetResponse
                {
                    Status = "Error",
                    Message = "Failed to import data from API: " + ex.Message
                };
            }
        }

        private Dictionary<string, object> FlattenJsonObject(JToken token, bool flattenNested = true)
        {
            var result = new Dictionary<string, object>();
            
            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty property in token.Children<JProperty>())
                {
                    if (flattenNested && property.Value.Type == JTokenType.Object)
                    {
                        var nested = FlattenJsonObject(property.Value, flattenNested);
                        foreach (var kvp in nested)
                        {
                            result[$"{property.Name}_{kvp.Key}"] = kvp.Value;
                        }
                    }
                    else if (flattenNested && property.Value.Type == JTokenType.Array)
                    {
                        // Convert array to string representation
                        result[property.Name] = property.Value.ToString();
                    }
                    else
                    {
                        result[property.Name] = property.Value.ToObject<object>();
                    }
                }
            }
            
            return result;
        }

        private class ColumnSchema
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public bool IsRequired { get; set; }
            public string DisplayName { get; set; }
            public int Order { get; set; }
        }
    }
} 