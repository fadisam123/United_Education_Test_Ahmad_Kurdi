using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using United_Education_Test_Ahmad_Kurdi.DTOs.Response;

namespace United_Education_Test_Ahmad_Kurdi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public class DiagnosticsController : ControllerBase
    {

        [HttpGet("success")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> GetSuccess()
        {
            await Task.CompletedTask; // just an async placeholder

            return Ok(ApiResponse<object>.Scucces("Dumy data placeholder"));
        }

        [HttpGet("slow")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> GetSlow(
            [FromQuery, Range(100, 10000)] int delayMs = 2000)
        {
            if (delayMs < 100 || delayMs > 10000)
            {
                return BadRequest(ApiErrorResponse.Create(
                    "Delay must be between 100 and 10000 milliseconds",
                    "INVALID_DELAY",
                    GetCorrelationId()));
            }

            await Task.Delay(delayMs);

            return Ok(ApiResponse<object>.Scucces($"delay = {delayMs}ms"));
        }

        [HttpGet("not-found")]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiErrorResponse>> GetNotFound()
        {
            await Task.CompletedTask;

            return NotFound(ApiErrorResponse.Create(
                "This is a test not found response",
                "TEST_NOT_FOUND",
                GetCorrelationId()));
        }

        [HttpGet("bad-request")]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiErrorResponse>> GetBadRequest()
        {
            await Task.CompletedTask;

            return BadRequest(ApiErrorResponse.Create(
                "This is a test bad request response",
                "TEST_BAD_REQUEST",
                GetCorrelationId(),
                new[] { "Invalid request parameters", "Missing required fields" }));
        }

        [HttpGet("server-error")]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetServerError()
        {
            await Task.CompletedTask;

            throw new InvalidOperationException("This is a test exception for middleware testing");
        }

        private string GetCorrelationId()
        {
            return HttpContext.Items["CorrelationId"]?.ToString() ?? String.Empty;
        }
    }
}
