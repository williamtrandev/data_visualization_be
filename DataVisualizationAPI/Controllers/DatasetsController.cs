using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.DTOs.Dropdown;
using DataVisualizationAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataVisualizationAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DatasetsController : ControllerBase
    {
        private readonly IDataImportService _importService;
        private readonly IDataQueryService _queryService;
        private readonly IDatasetService _datasetService;
        private readonly ILogger<DatasetsController> _logger;

        public DatasetsController(
            IDataImportService importService,
            IDataQueryService queryService,
            IDatasetService datasetService,
            ILogger<DatasetsController> logger)
        {
            _importService = importService;
            _queryService = queryService;
            _datasetService = datasetService;
            _logger = logger;
        }

        [HttpPost("import/file")]
        public async Task<ActionResult<ImportDatasetResponse>> ImportFromFile(
            [FromForm] IFormFile file,
            [FromForm] ImportDatasetRequest request)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _importService.ImportDataAsync(file, request.DatasetName, userId);

                if (result.Status == "Error")
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while importing the file", error = ex.Message });
            }
        }

        [HttpPost("import/database")]
        public async Task<ActionResult<ImportDatasetResponse>> ImportFromDatabase(
            [FromBody] ImportFromDatabaseRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _importService.ImportFromDatabaseAsync(
                    request.ConnectionString,
                    request.Query,
                    request.DatasetName,
                    userId);

                if (result.Status == "Error")
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while importing from database", error = ex.Message });
            }
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<List<DatasetDropdownResponse>>> GetDatasetsForDropdown()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var datasets = await _datasetService.GetDatasetsForDropdownAsync(userId);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting datasets for dropdown");
                return StatusCode(500, new { message = "An error occurred while getting datasets" });
            }
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<DatasetListResponse>>> ListDatasets([FromQuery] PaginationRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var datasets = await _datasetService.ListDatasetsAsync(request, userId);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing datasets");
                return StatusCode(500, "An error occurred while listing datasets");
            }
        }

        [HttpGet("{datasetId}")]
        public async Task<ActionResult<DatasetInfo>> GetDatasetInfo(int datasetId)
        {
            try
            {
                var datasetInfo = await _queryService.GetDatasetInfoAsync(datasetId);
                return Ok(datasetInfo);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while getting dataset info", error = ex.Message });
            }
        }

        [HttpPost("{datasetId}/query")]
        public async Task<ActionResult<QueryResult>> QueryData(
            int datasetId,
            [FromBody] QueryParameters parameters)
        {
            try
            {
                var result = await _queryService.QueryDataAsync(datasetId, parameters);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while querying data", error = ex.Message });
            }
        }

        [HttpGet("{datasetId}/detail")]
        public async Task<ActionResult<DataSourceDetailResponse>> GetDataSourceDetail(
            int datasetId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery(Name = "filters")] List<FilterCondition>? filters = null)
        {
            try
            {
                var detail = await _datasetService.GetDataSourceDetailAsync(datasetId, new DataSourceDetailRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    Filters = filters?.Select(f => new FilterParameter
                    {
                        Field = f.Field,
                        Operator = f.Operator,
                        Value = f.Value
                    }).ToList()
                });
                return Ok(detail);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data source detail for dataset {DatasetId}", datasetId);
                return StatusCode(500, "An error occurred while getting data source detail");
            }
        }

        [HttpPost("{datasetId}/detail")]
        public async Task<ActionResult<DataSourceDetailResponse>> GetDataSourceDetail(
            int datasetId,
            [FromBody] FilterRequest request)
        {
            try
            {
                var detail = await _datasetService.GetDataSourceDetailAsync(datasetId, new DataSourceDetailRequest
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    SortDirection = request.SortDirection,
                    Filters = request.Filters?.Select(f => new FilterParameter
                    {
                        Field = f.Field,
                        Operator = f.Operator,
                        Value = f.Value
                    }).ToList()
                });
                return Ok(detail);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data source detail for dataset {DatasetId}", datasetId);
                return StatusCode(500, "An error occurred while getting data source detail");
            }
        }

        [HttpGet("{datasetId}/filter")]
        public async Task<ActionResult<DataSourceDetailResponse>> FilterData(
            int datasetId,
            [FromQuery] FilterRequest request)
        {
            try
            {
                var detail = await _datasetService.GetDataSourceDetailAsync(datasetId, new DataSourceDetailRequest
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    SortDirection = request.SortDirection,
                    Filters = request.Filters?.Select(f => new FilterParameter
                    {
                        Field = f.Field,
                        Operator = f.Operator,
                        Value = f.Value
                    }).ToList()
                });
                return Ok(detail);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering data for dataset {DatasetId}", datasetId);
                return StatusCode(500, "An error occurred while filtering data");
            }
        }

        [HttpPost("{datasetId}/filter")]
        public async Task<ActionResult<DataSourceDetailResponse>> FilterDataPost(
            int datasetId,
            [FromBody] FilterRequest request)
        {
            try
            {
                var detail = await _datasetService.GetDataSourceDetailAsync(datasetId, new DataSourceDetailRequest
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    SortDirection = request.SortDirection,
                    Filters = request.Filters?.Select(f => new FilterParameter
                    {
                        Field = f.Field,
                        Operator = f.Operator,
                        Value = f.Value
                    }).ToList()
                });
                return Ok(detail);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering data for dataset {DatasetId}", datasetId);
                return StatusCode(500, "An error occurred while filtering data");
            }
        }

        [HttpPost("merge")]
        public async Task<IActionResult> MergeDatasets([FromBody] DatasetMergeRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _datasetService.MergeDatasetsAsync(request, userId);
                if (result.Status == "Error")
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging datasets");
                return StatusCode(500, new { message = "An error occurred while merging datasets" });
            }
        }
    }
} 