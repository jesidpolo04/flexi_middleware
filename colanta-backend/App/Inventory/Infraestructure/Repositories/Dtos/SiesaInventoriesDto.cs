namespace colanta_backend.App.Inventory.Infraestructure
{
    using App.Inventory.Domain;
    public class SiesaInventoriesDto
    {
        public SiesaInventoryDto[] inventario_almacen { get; set; }
    }

    public class SiesaInventoryDto
    {
        public string id_producto { get; set; }
        public string? id_variacion { get; set; }
        public int cantidad { get; set; }
        public string negocio { get; set; }
        public string id_bodega { get; set; }

        public Inventory getInventoryFromDto()
        {
            Inventory inventory = new Inventory
            {
                quantity = cantidad,
                business = negocio,
                warehouse_siesa_id = id_bodega
            };
            if (id_variacion != null)
            {
                inventory.sku_erp_id = id_variacion;
            }
            else
            {
                inventory.sku_erp_id = id_producto;
            }
            return inventory;
        }


    }
}
