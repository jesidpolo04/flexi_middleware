namespace colanta_backend.App.Inventory.Jobs
{
    using App.Inventory.Domain;
    using Products.Domain;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using App.Shared.Domain;
    using App.Shared.Application;
    public class RenderInventories : IDisposable
    {
        private string processName = "Renderizado de inventarios";
        private IServiceProvider serviceProvider;
        private List<Inventory> loadInventories = new List<Inventory>();
        private List<Inventory> updatedInventories = new List<Inventory>();
        private List<Inventory> failedInventories = new List<Inventory>();
        private List<Inventory> notProccecedInventories = new List<Inventory>();
        private int obtainedInventories = 0;
        private int securityStock = 2;

        private List<Detail> details = new List<Detail>();

        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
        private CustomConsole console = new CustomConsole();
        public RenderInventories(
            IServiceProvider serviceProvider,
            WarehousesRepository warehousesRepository,
            IProcess process,
            ILogger logger,
            IRenderInventoriesMail mail
            )
        {
            this.serviceProvider = serviceProvider;
            this.jsonOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        }

        public async Task Invoke()
        {
            this.console.processStartsAt(processName, DateTime.Now);

            InventoriesRepository inventoriesLocalRepository = (InventoriesRepository)this.serviceProvider.GetService(typeof(InventoriesRepository));
            InventoriesVtexRepository inventoriesVtexRepository = (InventoriesVtexRepository)this.serviceProvider.GetService(typeof(InventoriesVtexRepository));
            InventoriesSiesaRepository inventoriesSiesaRepository = (InventoriesSiesaRepository)this.serviceProvider.GetService(typeof(InventoriesSiesaRepository));
            SkusRepository skusLocalRepository = (SkusRepository)this.serviceProvider.GetService(typeof(SkusRepository));
            ILogger logger = (ILogger)this.serviceProvider.GetService(typeof(ILogger));

            try
            {
                int page = 1;
                while (true)
                {
                    try
                    {
                        Inventory[] siesaInventories = inventoriesSiesaRepository.getAllInventories(page).Result;
                        obtainedInventories += siesaInventories.Length;
                        foreach (Inventory siesaInventory in siesaInventories)
                        {
                            try
                            {
                                if (!skuExists(siesaInventory, skusLocalRepository))
                                {
                                    this.notProccecedInventories.Add(siesaInventory);
                                    continue;
                                }
                                Inventory localInventory = inventoriesLocalRepository.getInventoryBySkuErpIdAndWarehouseSiesaId(siesaInventory.sku_erp_id, siesaInventory.warehouse_siesa_id).Result;

                                if (localInventory != null)
                                {
                                    if (localInventory.quantity != siesaInventory.quantity)
                                    {
                                        localInventory.quantity = siesaInventory.quantity;
                                        localInventory = inventoriesLocalRepository.updateInventory(localInventory).Result;
                                        inventoriesVtexRepository.updateInventory(localInventory);
                                        /* inventoriesVtexRepository.removeReservedInventory(localInventory); */
                                        updatedInventories.Add(localInventory);
                                    }

                                    if (localInventory.quantity == siesaInventory.quantity)
                                    {
                                        inventoriesVtexRepository.updateInventory(localInventory);
                                        /* inventoriesVtexRepository.removeReservedInventory(localInventory); */
                                        notProccecedInventories.Add(localInventory);
                                    }
                                }
                                if (localInventory == null)
                                {
                                    try
                                    {
                                        localInventory = inventoriesLocalRepository.saveInventory(siesaInventory).Result;
                                    }
                                    catch (Exception exception)
                                    {
                                        logger.writelog(new Exception($"No se pudo guardar el inventario {localInventory.ToString()} en la tienda ${localInventory.warehouse_siesa_id}"));
                                        continue;
                                    }
                                    inventoriesVtexRepository.updateInventory(localInventory);
                                    loadInventories.Add(localInventory);
                                }
                            }
                            catch (VtexException vtexException)
                            {
                                failedInventories.Add(siesaInventory);
                                console.throwException(vtexException.Message);
                                logger.writelog(vtexException);
                            }
                            catch (Exception exception)
                            {
                                failedInventories.Add(siesaInventory);
                                console.throwException(exception.Message);
                                logger.writelog(exception);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        console.throwException(e.Message);
                        break;
                    }
                    page++;
                }
            }
            catch (SiesaException siesaException)
            {
                console.throwException(siesaException.Message);
                logger.writelog(siesaException);
            }
            catch (Exception genericException)
            {
                console.throwException(genericException.Message);
                logger.writelog(genericException);
            }

            /* mail.sendMail(loadInventories, updatedInventories, failedInventories); */
            console.processEndstAt(processName, DateTime.Now);
        }

        private bool skuExists(Inventory inventory, SkusRepository repository)
        {
            Task<Sku> sku = repository.getSkuBySiesaId(inventory.sku_erp_id);
            if (sku.Result == null)
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            loadInventories.Clear();
            updatedInventories.Clear();
            failedInventories.Clear();
            notProccecedInventories.Clear();
            details.Clear();
        }
    }
}
