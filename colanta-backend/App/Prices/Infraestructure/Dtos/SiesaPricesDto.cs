namespace colanta_backend.App.Prices.Infraestructure
{
    using App.Prices.Domain;
    using App.Products.Domain;
    public class SiesaPricesDto
    {
        public SiesaPriceDto[] productos { get; set; }
    }

    public class SiesaPriceDto
    {
        public string id_producto { get; set; }
        public string? id_variacion { get; set; }
        public decimal precio_publico { get; set; }
        public decimal precio_anterior { get; set; }
        public decimal costo_produccion { get; set; }
        public string negocio { get; set; }

        public Price GetPriceFromDto()
        {
            Price price = new Price
            {
                price = this.precio_publico,
                business = this.negocio,
                list_price = this.precio_anterior,
                cost_price = this.costo_produccion
            };

            if (id_variacion != null)
            {
                price.sku_erp_id = id_variacion;
            }
            else
            {
                price.sku_erp_id = id_producto;
            }
            return price;
        }
    }
}
