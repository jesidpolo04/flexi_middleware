namespace colanta_backend.App.Prices.Domain
{
    using System.Threading.Tasks;
    public interface PricesRepository
    {
        public Task<Price> getPriceBySkuErpId(string skuErpId);
        public Task<Price> getPriceBySkuId(int sku_id);
        public Task<Price> savePrice(Price price);
        public Task<Price> updatePrice(Price price);
        public Task<Price[]> updatePrices(Price[] prices);
    }
}
