using colanta_backend.App.Categories.Jobs;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace colanta_backend.App.Brands.Controllers
{
    [Route("api/categorias")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {

        [HttpPost]
        [Route("renderizar")]
        public void Post([FromServices] RenderCategories job)
        {
            job.Invoke();
            Ok("Renderizando categorias");
        }
    }
}
