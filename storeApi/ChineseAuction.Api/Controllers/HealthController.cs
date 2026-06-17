using Microsoft.AspNetCore.Mvc;

namespace ChineseAuction.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get() => Ok(new { status = "Healthy", time = System.DateTime.UtcNow });
    }
}
