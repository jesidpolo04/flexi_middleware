using colanta_backend.App.Inventory.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace colanta_backend.App.Inventory.Controllers
{
    [Route("api/inventarios")]
    [ApiController]
    public class InventoriesController : ControllerBase
    {
        [HttpPost]
        [Route("renderizar")]
        public void Post([FromServices] RenderInventories job)
        {
            job.Invoke();
            Ok("Renderizando precios");
        }
    }
}