using colanta_backend.App.Prices.Domain;
using System.Threading.Tasks;

namespace colanta_backend.App.Prices.Infraestructure
{
    using App.Shared.Infraestructure;
    using App.Prices.Domain;
    using App.Products.Domain;
    using App.Products.Infraestructure;
    using Microsoft.Extensions.Configuration;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;
    public class PricesEFRepository : Domain.PricesRepository
    {
        private ColantaContext dbContext;
        public PricesEFRepository(IConfiguration configuration)
        {
            this.dbContext = new ColantaContext(configuration);
        }

        public async Task<Price?> getPriceBySkuErpId(string skuErpId)
        {
            var efPrices = this.dbContext.Prices
                .Include(price => price.sku)
                .ThenInclude(sku => sku.product)
                .Where(e => e.sku_erp_id == skuErpId);

            if (efPrices.ToArray().Length > 0)
            {
                EFPrice efPrice = efPrices.First();
                return efPrice.getPriceFromEfPrice();
            }

            return null;
        }

        public async Task<Price?> getPriceBySkuId(int sku_id)
        {
            var efPrices = this.dbContext.Prices
                .Include(price => price.sku)
                .ThenInclude(sku => sku.product)
                .Where(e => e.sku.id == sku_id);

            if (efPrices.ToArray().Length > 0)
            {
                EFPrice efPrice = efPrices.First();
                return efPrice.getPriceFromEfPrice();
            }

            return null;
        }

        public async Task<Price> savePrice(Price price)
        {
            EFPrice efPrice = new EFPrice();
            efPrice.setEfPriceFromPrice(price);
            EFSku efSku = this.dbContext.Skus.Where(sku => sku.siesa_id == price.sku_erp_id).First();
            efPrice.sku = efSku;
            this.dbContext.Add(efPrice);
            this.dbContext.SaveChanges();
            return await this.getPriceBySkuErpId(price.sku_erp_id);
        }

        public async Task<Price> updatePrice(Price price)
        {
            EFPrice efPrice = this.dbContext.Prices.Find(price.id);

            efPrice.price = price.price;
            efPrice.business = price.business;
            efPrice.sku_erp_id = price.sku_erp_id;
            efPrice.sku_id = price.sku_id;
            dbContext.SaveChanges();

            return price;
        }

        public async Task<Price[]> updatePrices(Price[] prices)
        {
            foreach(Price price in prices)
            {
                EFPrice efPrice = this.dbContext.Prices.Find(price.id);

                efPrice.price = price.price;
                efPrice.business = price.business;
                efPrice.sku_erp_id = price.sku_erp_id;
                efPrice.sku_id = price.sku_id;
            }
            dbContext.SaveChanges();
            return prices;
        }
    }
}
