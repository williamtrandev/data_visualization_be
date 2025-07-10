using DataVisualizationAPI.DTOs;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DataVisualizationAPI.Services
{
    public interface IDataImportService
    {
        Task<ImportDatasetResponse> ImportDataAsync(IFormFile file, string datasetName, string userId);
        Task<ImportDatasetResponse> ImportFromDatabaseAsync(string connectionString, string query, string datasetName, string userId);
        Task<ImportDatasetResponse> ImportFromRestApiAsync(string apiUrl, string datasetName, string userId, RestApiImportOptions options = null);
    }
} 