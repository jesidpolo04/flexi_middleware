namespace colanta_backend.App.Prices.Infraestructure
{
    using Prices.Domain;
    using Products.Domain;
    using Products.Infraestructure;
    public class EFPrice
    {
        public int? id { get; set; }
        public decimal price { get; set; }
        public decimal list_price { get; set; }
        public decimal cost_price { get; set; }
        public string sku_erp_id { get; set; }
        public int sku_id { get; set; }
        public EFSku sku { get; set; }
        public string business { get; set; }

        public void setEfPriceFromPrice(Price price)
        {
            this.id = price.id;
            this.price = price.price;
            this.list_price = price.list_price;
            this.cost_price = price.cost_price;
            this.sku_erp_id = price.sku_erp_id;
            this.sku_id = price.sku_id;
            this.business = price.business;
        }

        public Price getPriceFromEfPrice()
        {
            Price price = new Price
            {
                id = this.id,
                price = this.price,
                sku_erp_id = this.sku_erp_id,
                sku_id = this.sku_id,
                business = this.business,
                list_price = this.list_price,
                cost_price = this.cost_price
            };

            Sku sku = new Sku
            {
                id = this.sku.id,
                ref_id = this.sku.ref_id,
                siesa_id = this.sku.siesa_id,
                concat_siesa_id = this.sku.concat_siesa_id,
                vtex_id = this.sku.vtex_id,
                name = this.sku.name,
                description = this.sku.description,
                measurement_unit = this.sku.measurement_unit,
                unit_multiplier = this.sku.unit_multiplier,
                packaged_weight_kg = this.sku.packaged_weight_kg,
                packaged_length = this.sku.packaged_length,
                packaged_height = this.sku.packaged_height,
                packaged_width = this.sku.packaged_width
            };

            Product product = new Product
            {
                id = this.sku.product.id,
                name = this.sku.product.name,
                ean = this.sku.product.ean,
                description = this.sku.product.description,
                ref_id = this.sku.product.ref_id,
                business = this.sku.product.business,
                type = this.sku.product.type,
                brand_id = this.sku.product.brand_id,
                category_id = this.sku.product.category_id,
                concat_siesa_id = this.sku.product.concat_siesa_id,
                vtex_id = this.sku.product.vtex_id,
                siesa_id = this.sku.product.siesa_id,
                is_active = this.sku.product.is_active
            };

            sku.product = product;
            price.sku = sku;

            return price;
        }
    }
}
