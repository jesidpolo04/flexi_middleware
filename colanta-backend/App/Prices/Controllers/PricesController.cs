using colanta_backend.App.Prices.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace colanta_backend.App.Prices.Controllers
{
    [Route("api/precios")]
    [ApiController]
    public class PricesController : ControllerBase
    {
        [HttpPost]
        [Route("renderizar")]
        public void Post([FromServices] RenderPrices job)
        {
            job.Invoke();
            Ok("Renderizando precios");
        }
    }
}