using colanta_backend.App.Products.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace colanta_backend.App.Products.Controllers
{
    [Route("api/productos")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        [HttpPost]
        [Route("renderizar")]
        public void Post([FromServices] RenderProductsAndSkus job)
        {
            job.Invoke();
            Ok("Renderizando productos");
        }
    }
}