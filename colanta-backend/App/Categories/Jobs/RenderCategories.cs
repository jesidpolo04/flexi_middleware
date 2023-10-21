﻿namespace colanta_backend.App.Categories.Jobs
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text.Json;
    using Categories.Domain;
    using Shared.Domain;
    using Shared.Application;
    using System.Text.Json.Serialization;

    public class RenderCategories : IDisposable
    {
        private string processName = "Renderizado de categorías";
        private CategoriesRepository localRepository;
        private CategoriesVtexRepository vtexRepository;
        private CategoriesSiesaRepository siesaRepository;
        private ILogger logger;
        private IRenderCategoriesMail mail;
        private CustomConsole console;

        private List<Category> loadCategories = new List<Category>();
        private List<Category> failedLoadCategories = new List<Category>();
        private List<Category> inactiveCategories = new List<Category>();
        private List<Category> inactivatedCategories = new List<Category>();
        private List<Category> notProccecedCategories = new List<Category>();
        private int obtainedCategories = 0;

        private List<Detail> details = new List<Detail>();
        private JsonSerializerOptions jsonOptions;
        public RenderCategories(
            CategoriesRepository categoriesLocalRepository, 
            CategoriesVtexRepository categoriesVtexRepository, 
            CategoriesSiesaRepository categoriesSiesaRepository,
            ILogger logger,
            IRenderCategoriesMail mail
        )
        {
            this.localRepository = categoriesLocalRepository;
            this.vtexRepository = categoriesVtexRepository;
            this.siesaRepository = categoriesSiesaRepository;
            this.mail = mail;
            this.logger = logger;
            this.console = new CustomConsole();

            this.jsonOptions = new JsonSerializerOptions();
            this.jsonOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            this.jsonOptions.ReferenceHandler = ReferenceHandler.Preserve;
        }

        public async Task Invoke()
        {
            try
            {
                this.console.processStartsAt(processName, DateTime.Now);

                Category[] siesaCategories = await this.siesaRepository.getAllCategories();
                obtainedCategories = siesaCategories.Length;

                Category[] deltaCategories = await this.localRepository.getDeltaCategories(siesaCategories);
                foreach (Category deltaCategory in deltaCategories)
                {
                    try
                    {
                        deltaCategory.isActive = false;
                        vtexRepository.updateCategoryState((int)deltaCategory.vtex_id, false).Wait();
                        await localRepository.updateCategory(deltaCategory);

                        //metrics
                        this.inactivatedCategories.Add(deltaCategory);
                    }
                    catch (VtexException exception)
                    {
                        this.console.throwException(exception.Message);
                        await this.logger.writelog(exception);
                    }
                }

                foreach(Category siesaCategory in siesaCategories)
                {
                    Category localCategory = await this.localRepository.getCategoryBySiesaId(siesaCategory.siesa_id);

                    if(localCategory != null)
                    {
                        if (localCategory.isActive)
                        {
                            this.notProccecedCategories.Add(localCategory);
                        }
                        else
                        {
                            this.inactiveCategories.Add(localCategory);
                        }
                        foreach(Category childLocalCategory in localCategory.childs)
                        {
                            if (childLocalCategory.isActive)
                            {
                                this.notProccecedCategories.Add(childLocalCategory);
                            }
                            else
                            {
                                this.inactiveCategories.Add(childLocalCategory);
                            }
                        }
                        foreach(Category childSiesaCategory in siesaCategory.childs)
                        {
                            Category childLocalCategory = await this.localRepository.getCategoryBySiesaId(childSiesaCategory.siesa_id);
                            if(childLocalCategory != null)
                            {
                                if (childLocalCategory.isActive)
                                {
                                    this.notProccecedCategories.Add(childSiesaCategory);
                                }
                                else
                                {
                                    this.inactiveCategories.Add(childSiesaCategory);
                                }
                            }
                            else
                            {
                                try
                                {
                                    childLocalCategory = await this.localRepository.saveCategory(childSiesaCategory);
                                    Category vtexChildCategory = await this.vtexRepository.saveCategory(childLocalCategory);
                                    childLocalCategory.vtex_id = vtexChildCategory.vtex_id;
                                    childLocalCategory = await this.localRepository.updateCategory(childLocalCategory);
                                    this.loadCategories.Add(childLocalCategory);
                                }
                                catch(VtexException exception)
                                {
                                    this.console.throwException(exception.Message);
                                    this.failedLoadCategories.Add(childSiesaCategory);
                                    this.logger.writelog(exception);
                                }
                                catch(Exception exception)
                                {
                                    this.console.throwException(exception.Message);
                                    this.logger.writelog(exception);
                                }
                            }
                        }
                    }
                    if(localCategory == null)
                    {
                        try
                        {
                            Console.WriteLine($"G - id: {siesaCategory.siesa_id}, nombre: {siesaCategory.name}");
                            localCategory = await this.localRepository.saveCategory(siesaCategory); //save familys and lines
                            Category vtexCategory = await this.vtexRepository.saveCategory(localCategory);
                            Console.WriteLine($"Ha guardado en VTEX la categoria con id {siesaCategory.id}");
                            localCategory.vtex_id = vtexCategory.vtex_id;
                            localCategory = await this.localRepository.updateCategory(localCategory);
                            this.loadCategories.Add(localCategory);
                            foreach (Category localChildCategory in localCategory.childs)
                            {
                                try
                                {
                                    Console.WriteLine($"GL - id: {localChildCategory.id}, nombre: {localChildCategory.name}");
                                    Category vtexChildCategory = await this.vtexRepository.saveCategory(localChildCategory);
                                    localChildCategory.vtex_id = vtexChildCategory.vtex_id;
                                    await this.localRepository.updateCategory(localChildCategory);
                                    this.loadCategories.Add(localChildCategory);
                                }
                                catch(VtexException exception)
                                {
                                    this.console.throwException(exception.Message);
                                    this.failedLoadCategories.Add(localChildCategory);
                                    this.details.Add(new Detail("vtex", exception.requestUrl, exception.responseBody, exception.Message, false));
                                    this.logger.writelog(exception);
                                }
                                catch(Exception exception)
                                {
                                    this.console.throwException(exception.Message);
                                    this.logger.writelog(exception);
                                }
                            }
                        }
                        catch(VtexException exception)
                        {
                            this.console.throwException(exception.Message);
                            this.failedLoadCategories.Add(localCategory);
                            foreach (Category child in localCategory.childs)
                            {
                                this.failedLoadCategories.Add(child);
                            }
                            this.logger.writelog(exception);
                        }
                        catch(Exception exception)
                        {
                            this.console.throwException(exception.Message);
                            this.logger.writelog(exception);
                        }
                    }
                }
                this.console.processEndstAt(processName, DateTime.Now);
            }
            catch (SiesaException exception)
            {
                this.console.throwException(exception.Message);
                this.console.processEndstAt(processName, DateTime.Now);
                this.logger.writelog(exception);
            }
            catch(Exception exception)
            {
                this.console.throwException(exception.Message);
                this.console.processEndstAt(processName, DateTime.Now);
                this.logger.writelog(exception);
                this.console.processEndstAt(processName, DateTime.Now);
            }
            this.mail.sendMail(this.loadCategories, this.inactivatedCategories, this.failedLoadCategories);
        }

        public void Dispose()
        {
            this.loadCategories.Clear();
            this.inactivatedCategories.Clear();
            this.inactiveCategories.Clear();
            this.failedLoadCategories.Clear();
            this.notProccecedCategories.Clear();
            this.details.Clear();
        }
    }
}
