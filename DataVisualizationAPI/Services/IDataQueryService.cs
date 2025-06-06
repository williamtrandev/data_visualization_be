using DataVisualizationAPI.DTOs;
using System.Threading.Tasks;

namespace DataVisualizationAPI.Services
{
    public interface IDataQueryService
    {
        Task<QueryResult> QueryDataAsync(int datasetId, QueryParameters parameters);
        Task<DatasetInfo> GetDatasetInfoAsync(int datasetId);
    }
} 