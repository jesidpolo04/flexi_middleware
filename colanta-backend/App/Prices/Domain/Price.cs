namespace colanta_backend.App.Prices.Domain
{
    using Products.Domain;
    public class Price
    {
        public int? id { get; set; }
        public decimal price { get; set; }
        public decimal list_price { get; set; }
        public decimal cost_price { get; set; }
        public string sku_erp_id { get; set; }
        public int sku_id { get; set; }
        public Sku sku { get; set; }
        public string business { get; set; }

        public bool differentPricesFrom(Price price){
            if(this.price != price.price) return true;
            if(this.list_price != price.list_price) return true;
            if(this.cost_price != price.cost_price) return true;
            return false;
        }

        public void updatePricesFrom(Price price){
            this.price = price.price;
            this.list_price = price.list_price;
            this.cost_price = price.cost_price;
        }

    }
}
