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
    }
}
