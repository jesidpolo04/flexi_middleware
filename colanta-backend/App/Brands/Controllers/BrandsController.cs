using colanta_backend.App.Brands.Jobs;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace colanta_backend.App.Brands.Controllers
{
    [Route("api/marcas")]
    [ApiController]
    public class BrandsController : ControllerBase
    {

        [HttpPost]
        [Route("renderizar")]
        public void Post([FromServices] RenderBrands job)
        {
            job.Invoke();
            Ok("Renderizando marcas");
        }
    }
}
