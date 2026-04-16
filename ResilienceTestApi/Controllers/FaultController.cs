using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ResilienceTestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaultController : ControllerBase
    {
        private static int counter = 0;

        [HttpGet(Name = "GetServerError")]
        public IActionResult get()
        {
            if (counter++ % 3 == 2)
            {
                return Ok("Success");
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failure");
            }
        }

        [HttpGet("timeout")]
        public async Task GetTimeout()
        {
            await Task.Delay(Timeout.Infinite);
        }

        [HttpGet("unstable")]
        public IActionResult GetUnstable()
        {
            // Skifter hvert 30. sekund baseret på uret
            if (DateTime.Now.Second >= 30)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Unstable Failure (30-59s window)");
            }
            
            return Ok("Unstable Success (0-29s window)");
        }
    }
}
