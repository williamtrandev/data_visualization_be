using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DataVisualizationAPI.DTOs;
using DataVisualizationAPI.Services;
using System.Linq;

namespace DataVisualizationAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/datasets")]
    public class ChartDataController : ControllerBase
    {
        private readonly IChartDataService _chartDataService;
        private readonly ILogger<ChartDataController> _logger;

        public ChartDataController(
            IChartDataService chartDataService,
            ILogger<ChartDataController> logger)
        {
            _chartDataService = chartDataService;
            _logger = logger;
        }

        [HttpPost("{datasetId}/aggregate")]
        public async Task<ActionResult<ChartAggregationResponse>> GetAggregatedData(
            int datasetId,
            [FromBody] ChartAggregationRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { message = "Invalid request", errors });
                }

                // Additional validation
                if (string.IsNullOrWhiteSpace(request.CategoryField))
                {
                    return BadRequest(new { message = "Category field is required" });
                }

                if (string.IsNullOrWhiteSpace(request.ValueField))
                {
                    return BadRequest(new { message = "Value field is required" });
                }

                var result = await _chartDataService.GetAggregatedDataAsync(datasetId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Dataset not found");
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aggregated data");
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }
    }
} 