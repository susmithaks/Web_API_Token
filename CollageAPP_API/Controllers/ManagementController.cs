using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CollageAPP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagementController : ControllerBase
    {
        public ManagementController()
        { }
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Welcome to ManagementController ");
        }
    }
}
