namespace colanta_backend.App.Prices.Jobs
{
    using System.Threading.Tasks;
    using App.Prices.Domain;
    using App.Specifications.Domain;
    using App.Products.Domain;
    using App.Shared.Domain;
    using App.Shared.Application;
    using System.Collections.Generic;
    using System.Text.Json;
    using System;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class RenderPrices : IDisposable
    {
        public string processName = "Renderizado de precios";
        public PricesRepository localRepository;
        public PricesVtexRepository vtexRepository;
        public PricesSiesaRepository siesaRepository;
        public SpecificationsVtexRepository specificationsVtexRepository;
        public SkusRepository skusLocalRepository;
        public ILogger logger;
        private IRenderPricesMail mail;

        public List<Price> loadPrices = new List<Price>();
        public List<Price> updatedPrices = new List<Price>();
        public List<Price> failedPrices = new List<Price>();
        public List<Price> notProccecedPrices = new List<Price>();
        public int obtainedPrices = 0;
        public List<Detail> details = new List<Detail>();

        public CustomConsole console = new CustomConsole();
        public JsonSerializerOptions jsonOptions;

        public RenderPrices(
            PricesRepository localRepository,
            PricesVtexRepository vtexRepository,
            PricesSiesaRepository siesaRepository,
            SpecificationsVtexRepository specificationsVtexRepository,
            SkusRepository skusLocalRepository,
            ILogger logger,
            IRenderPricesMail mail)
        {
            this.localRepository = localRepository;
            this.vtexRepository = vtexRepository;
            this.siesaRepository = siesaRepository;
            this.skusLocalRepository = skusLocalRepository;
            this.specificationsVtexRepository = specificationsVtexRepository;
            this.logger = logger;
            this.mail = mail;

            this.jsonOptions = new JsonSerializerOptions();
            this.jsonOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        }

        public async Task Invoke()
        {
            try
            {
                this.console.processStartsAt(processName, DateTime.Now);
                int page = 1;
                while (true)
                {
                    try
                    {
                        Price[] allSiesaPrices = await this.siesaRepository.getAllPrices(page);
                        foreach (Price siesaPrice in allSiesaPrices)
                        {
                            this.obtainedPrices++;

                            if (!this.skuExist(siesaPrice))
                            {
                                this.notProccecedPrices.Add(siesaPrice);
                                continue;
                            }

                            Price localPrice = this.localRepository.getPriceBySkuErpId(siesaPrice.sku_erp_id).Result;

                            if (localPrice != null)
                            {
                                try
                                {
                                    if (localPrice.differentPricesFrom(siesaPrice))
                                    {
                                        localPrice.updatePricesFrom(siesaPrice);
                                        await this.localRepository.updatePrice(localPrice);
                                    }

                                    Price vtexPrice = this.vtexRepository.getPriceByVtexId(localPrice.sku.vtex_id).Result;

                                    if (vtexPrice == null)
                                    {
                                        this.vtexRepository.savePrice(localPrice).Wait();
                                        this.loadPrices.Add(localPrice);
                                        continue;
                                    }
                                    if (vtexPrice != null)
                                    {
                                        if (vtexPrice.differentPricesFrom(localPrice))
                                        {
                                            this.vtexRepository.savePrice(localPrice).Wait();
                                            this.updatedPrices.Add(localPrice);
                                            continue;
                                        }
                                        if (!vtexPrice.differentPricesFrom(localPrice))
                                        {
                                            this.notProccecedPrices.Add(localPrice);
                                            continue;
                                        }
                                    }
                                }
                                catch (VtexException vtexException)
                                {
                                    this.console.throwException(vtexException.Message);
                                    this.failedPrices.Add(localPrice);
                                    this.logger.writelog(vtexException);
                                }
                                catch (Exception exception)
                                {
                                    this.failedPrices.Add(localPrice);
                                    this.console.throwException(exception.Message);
                                    this.logger.writelog(exception);
                                }
                            }

                            if (localPrice == null)
                            {
                                try
                                {
                                    localPrice = await this.localRepository.savePrice(siesaPrice);
                                    Price vtexPrice = await this.vtexRepository.getPriceByVtexId(localPrice.sku.vtex_id);
                                    if (vtexPrice == null)
                                    {
                                        await this.vtexRepository.savePrice(localPrice);
                                        this.loadPrices.Add(localPrice);
                                        continue;
                                    }
                                    if (vtexPrice != null)
                                    {
                                        if (vtexPrice.price != localPrice.price)
                                        {
                                            await this.vtexRepository.savePrice(localPrice);
                                            this.loadPrices.Add(localPrice);
                                            continue;
                                        }
                                    }
                                }
                                catch (VtexException vtexException)
                                {
                                    this.console.throwException(vtexException.Message);
                                    this.failedPrices.Add(localPrice);
                                    this.logger.writelog(vtexException);
                                }
                                catch (Exception exception)
                                {
                                    this.failedPrices.Add(localPrice);
                                    this.console.throwException(exception.Message);
                                    this.logger.writelog(exception);
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        console.throwException(exception.Message);
                        break;
                    }
                    page ++;
                }
            }
            catch (SiesaException siesaException)
            {
                this.console.throwException(siesaException.Message);
                this.logger.writelog(siesaException);
            }
            catch (Exception genericException)
            {
                this.console.throwException(genericException.Message);
                this.logger.writelog(genericException);
            }
            this.console.processEndstAt(processName, DateTime.Now);
            /* this.mail.sendMail(this.loadPrices, this.updatedPrices, this.failedPrices); */
        }

        private bool skuExist(Price price)
        {
            Task<Sku?> sku = this.skusLocalRepository.getSkuBySiesaId(price.sku_erp_id);
            if (sku.Result == null)
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            this.loadPrices.Clear();
            this.updatedPrices.Clear();
            this.failedPrices.Clear();
            this.notProccecedPrices.Clear();
            this.details.Clear();
        }
    }
}
