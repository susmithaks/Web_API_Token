using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CollageAPP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HomeController : ControllerBase
    {

        public HomeController()
        { }


        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Welcome to HomeController");
        }

    }
}
