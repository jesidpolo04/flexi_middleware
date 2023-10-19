namespace colanta_backend.App.Products.Domain
{
    using System.Threading.Tasks;
    public interface ProductsSiesaRepository
    {
        Task<Product[]> getAllProducts(int page);
        Task<Sku[]> getAllSkus(int page);
    }
}
