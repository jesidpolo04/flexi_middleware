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
            Ok("Renderizando productos.");
        }

        [HttpPost]
        [Route("actualizar-estado")]
        public ActionResult UpdateState([FromServices] UpdateProductsAndSkusStates job){
            job.Invoke();
            return Ok("Actualizando el estado de los productos.");
        }

        [HttpPost]
        [Route("reintentar")]
        public ActionResult Retry([FromServices] UpToVtexNullProductsAndSkus job){
            job.Invoke();
            return Ok("Reintentando productos nulos a VTEX.");
        }
    }
}