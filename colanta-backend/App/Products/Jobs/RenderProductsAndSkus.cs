﻿namespace colanta_backend.App.Products.Jobs
{
    using System.Threading.Tasks;
    using Products.Domain;
    using Brands.Domain;
    using Categories.Domain;
    using System;
    using System.Collections.Generic;
    using Shared.Application;
    using Shared.Domain;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    public class RenderProductsAndSkus : IDisposable
    {
        private string processName = "Renderizado de productos";
        private ProductsRepository productsLocalRepository;
        private SkusRepository skusLocalRepository;
        private ProductsVtexRepository productsVtexRepository;
        private SkusVtexRepository skusVtexRepository;
        private ProductsSiesaRepository siesaRepository;
        private BrandsRepository brandsLocalRepository;
        private CategoriesRepository categoriesLocalRepository;
        private IProcess processLogger;
        private ILogger logger;
        private IRenderProductsMail mail;
        private IInvalidCategoryMail invalidCategoryMail;
        private IInvalidBrandMail invalidBrandMail;
        private CustomConsole console = new CustomConsole();

        private List<Product> failedProducts;
        private List<Sku> loadSkus;
        private List<Sku> failedSkus;
        private List<Sku> inactiveSkus;
        private List<Sku> inactivatedSkus;
        private List<Sku> notProccecedSkus;
        private int obtainedSkus;

        private JsonSerializerOptions jsonOptions;
        public RenderProductsAndSkus
        (
            ProductsRepository productsLocalRepository,
            ProductsVtexRepository productsVtexRepository,
            SkusRepository skusLocalRepository,
            SkusVtexRepository skusVtexRepository,
            ProductsSiesaRepository siesaRepository,
            BrandsRepository brandsLocalRepository,
            CategoriesRepository categoriesLocalRepository,

            IProcess processLogger,
            ILogger logger,
            IRenderProductsMail mail,
            IInvalidCategoryMail invalidCategoryMail,
            IInvalidBrandMail invalidBrandMail
        )
        {
            this.productsLocalRepository = productsLocalRepository;
            this.skusLocalRepository = skusLocalRepository;
            this.productsVtexRepository = productsVtexRepository;
            this.skusVtexRepository = skusVtexRepository;
            this.siesaRepository = siesaRepository;
            this.brandsLocalRepository = brandsLocalRepository;
            this.categoriesLocalRepository = categoriesLocalRepository;
            this.processLogger = processLogger;
            this.logger = logger;
            this.mail = mail;
            this.invalidCategoryMail = invalidCategoryMail;
            this.invalidBrandMail = invalidBrandMail;

            this.failedProducts = new List<Product>();
            this.loadSkus = new List<Sku>();
            this.failedSkus = new List<Sku>();
            this.inactiveSkus = new List<Sku>();
            this.inactivatedSkus = new List<Sku>();
            this.notProccecedSkus = new List<Sku>();

            this.jsonOptions = new JsonSerializerOptions();
            this.jsonOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            this.jsonOptions.ReferenceHandler = ReferenceHandler.Preserve;

        }

        public async Task Invoke()
        {
            try
            {
                console.processStartsAt(processName, DateTime.Now);
                int page = 1;
                List<Sku> allPagesSkus = new List<Sku>();

                while (true)
                {
                    try
                    {
                        Product[] allSiesaProducts = await siesaRepository.getAllProducts(page);
                        Sku[] allSiesaSkus = await siesaRepository.getAllSkus(page);
                        this.obtainedSkus = allSiesaSkus.Length;
                        Product[] deltaProducts = await productsLocalRepository.getDeltaProducts(allSiesaProducts);

                        console.writeLine($"Productos obtenidos pagina {page}: {allSiesaProducts.Length}");
                        console.writeLine($"Variaciones obtenidos pagina {page}: {allSiesaSkus.Length}");

                        foreach (Product siesaProduct in allSiesaProducts)
                        {
                            try
                            {
                                this.validProduct(siesaProduct);
                            }
                            catch (InvalidBrandException exception)
                            {
                                this.console.throwException(exception.Message);
                                this.invalidBrandMail.sendMail(exception);
                                this.logger.writelog(exception);
                                this.failedProducts.Add(siesaProduct);
                                continue;
                            }
                            catch (InvalidCategoryException exception)
                            {
                                this.console.throwException(exception.Message);
                                this.invalidCategoryMail.sendMail(exception);
                                this.logger.writelog(exception);
                                this.failedProducts.Add(siesaProduct);
                                continue;
                            }

                            Product? localProduct = await productsLocalRepository.getProductBySiesaId(siesaProduct.siesa_id);

                            if (localProduct != null && localProduct.is_active == false)
                            {
                                //Producto inactivo
                            }

                            if (localProduct != null && localProduct.is_active == true)
                            {
                                //todo ok
                            }

                            if (localProduct == null)
                            {
                                try
                                {
                                    localProduct = await productsLocalRepository.saveProduct(siesaProduct);
                                    Product vtexProduct = await productsVtexRepository.saveProduct(localProduct);
                                    localProduct.vtex_id = vtexProduct.vtex_id;
                                    productsLocalRepository.updateProduct(localProduct);
                                }
                                catch (VtexException vtexException)
                                {
                                    this.console.throwException(vtexException.Message);
                                    this.logger.writelog(vtexException);
                                    this.failedProducts.Add(siesaProduct);
                                }
                                catch (Exception exception)
                                {
                                    this.console.throwException(exception.Message);
                                    this.logger.writelog(exception);
                                    this.failedProducts.Add(siesaProduct);
                                }
                            }
                        }

                        foreach (Sku siesaSku in allSiesaSkus)
                        {
                            allPagesSkus.Add(siesaSku);
                            if (this.isFailedProduct(siesaSku.product.siesa_id))
                            {
                                continue;
                            }

                            Sku? localSku = await skusLocalRepository.getSkuBySiesaId(siesaSku.siesa_id);

                            if (localSku != null && localSku.is_active == false)
                            {
                                if (localSku.vtex_id.HasValue)
                                {
                                    this.skusVtexRepository.changeSkuState((int)localSku.vtex_id, true);
                                    localSku.is_active = true;
                                }
                                this.inactiveSkus.Add(localSku);
                            }

                            if (localSku != null && localSku.is_active == true)
                            {
                                this.notProccecedSkus.Add(localSku);
                            }

                            if (localSku == null)
                            {
                                try
                                {
                                    localSku = await skusLocalRepository.saveSku(siesaSku);
                                    Sku vtexSku = await skusVtexRepository.saveSku(localSku);
                                    localSku.vtex_id = vtexSku.vtex_id;
                                    skusLocalRepository.updateSku(localSku);
                                    this.loadSkus.Add(localSku);
                                }
                                catch (VtexException vtexException)
                                {
                                    this.console.throwException(vtexException.Message);
                                    this.failedSkus.Add(siesaSku);
                                    this.logger.writelog(vtexException);
                                }
                                catch (Exception exception)
                                {
                                    this.failedSkus.Add(siesaSku);
                                    this.console.throwException(exception.Message);
                                    this.logger.writelog(exception);
                                }
                            }
                        }
                        this.skusLocalRepository.updateSkus(inactiveSkus.ToArray());
                    }
                    catch (SiesaException exception)
                    {
                        if(exception.status == 404){
                            console.throwException(exception.Message);
                            break;
                        }else{
                            throw exception;
                        }
                    }
                    page++;
                }

                Sku[] deltaSkus = await this.skusLocalRepository.getDeltaSkus( allPagesSkus.ToArray() );
                foreach (Sku deltaSku in deltaSkus)
                {
                    try
                    {
                        deltaSku.is_active = false;
                        if (deltaSku.vtex_id.HasValue)
                        {
                            skusVtexRepository.changeSkuState((int)deltaSku.vtex_id, false);
                            this.inactivatedSkus.Add(deltaSku);
                        }
                    }
                    catch (VtexException vtexException)
                    {
                        deltaSku.is_active = true;
                        this.console.throwException(vtexException.Message);
                        this.logger.writelog(vtexException);
                    }
                    catch (Exception exception)
                    {
                        deltaSku.is_active = true;
                        this.console.throwException(exception.Message);
                        this.logger.writelog(exception);
                    }

                }
                skusLocalRepository.updateSkus(deltaSkus);

                this.console.processEndstAt(processName, DateTime.Now);
            }
            catch (SiesaException exception)
            {
                this.console.throwException(exception.Message);
                this.logger.writelog(exception);
                this.console.processEndstAt(processName, DateTime.Now);
            }
            catch (Exception exception)
            {
                this.console.throwException(exception.Message);
                this.logger.writelog(exception);
                this.console.processEndstAt(processName, DateTime.Now);
            }
            mail.sendMail(this.loadSkus, this.inactivatedSkus, this.failedSkus);
        }

        private void validProduct(Product siesaProduct)
        {
            if (siesaProduct.brand.id_siesa == null)
            {
                throw new InvalidBrandException("La marca fue nula", siesaProduct);
            }
            if (siesaProduct.category.siesa_id == null)
            {
                throw new InvalidCategoryException("La categoría fue nula", siesaProduct);
            }

            Brand brand = this.brandsLocalRepository.getBrandBySiesaId(siesaProduct.brand.id_siesa);
            if (brand == null)
            {
                throw new InvalidBrandException("La marca no existe en el middleware", siesaProduct);
            }

            Task<Category> category = this.categoriesLocalRepository.getCategoryBySiesaId(siesaProduct.category.siesa_id);
            if (category.Result == null)
            {
                throw new InvalidCategoryException("La categoría no existe en el middleware", siesaProduct);
            }
        }

        private bool isFailedProduct(string productSiesaId)
        {
            foreach (Product product in this.failedProducts)
            {
                if (product.siesa_id == productSiesaId)
                {
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            this.failedProducts.Clear();
            this.loadSkus.Clear();
            this.failedSkus.Clear();
            this.notProccecedSkus.Clear();
            this.inactiveSkus.Clear();
            this.inactivatedSkus.Clear();
        }
    }
}
