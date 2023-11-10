namespace colanta_backend.App.Inventory.Domain
{
    using System.Threading.Tasks;
    public interface InventoriesSiesaRepository
    {
        Task<Inventory[]> getAllInventories(int page);
    }
}
