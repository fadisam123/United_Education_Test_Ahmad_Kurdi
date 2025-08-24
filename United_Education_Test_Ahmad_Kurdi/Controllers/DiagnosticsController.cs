namespace United_Education_Test_Ahmad_Kurdi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DiagnosticsController : ControllerBase
    {


        [HttpGet("success")]
        public async Task<IActionResult> Success()
        {
            await Task.CompletedTask; // just an async placeholder
            return Ok(new { message = "Success" });
        }

        [HttpGet("slow")]
        public async Task<IActionResult> GetSlow()
        {
            await Task.Delay(2000); // 2 second delay
            return Ok(new { message = "Slow response completed" });
        }

        [HttpGet("not-found")]
        public async Task<IActionResult> GetNotFound()
        {
            await Task.CompletedTask;
            return NotFound(new { error = "Resource not found" });
        }

        [HttpGet("bad-request")]
        public async Task<IActionResult> GetBadRequest()
        {
            await Task.CompletedTask;
            return BadRequest(new { error = "Invalid request parameters" });
        }

        [HttpGet("server-error")]
        public async Task<IActionResult> GetServerError()
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("This is a test exception");
        }

        [HttpPost("echo")]
        public async Task<IActionResult> PostEcho([FromBody] object data)
        {
            await Task.CompletedTask;
            return Ok(new { echo = data });
        }

    }
}
