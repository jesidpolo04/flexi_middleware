﻿namespace colanta_backend.App.Products.Infraestructure
{
    using Products.Domain;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Shared.Domain;
    using Shared.Infraestructure;
    using Microsoft.Extensions.Configuration;
    using FluentEmail.Core;

    public class ProductsSiesaRepository : Domain.ProductsSiesaRepository
    {
        private IConfiguration configuration;
        private EmailSender emailSender;
        private HttpClient httpClient;
        private SiesaAuth siesaAuth;


        public ProductsSiesaRepository(IConfiguration configuration, EmailSender emailSender)
        {
            this.configuration = configuration;
            this.httpClient = new HttpClient();
            this.siesaAuth = new SiesaAuth(configuration);
            this.emailSender = emailSender;
        }

        public async Task<Product[]> getAllProducts(int page)
        {
            //await this.setHeaders();
            string endpoint = $"/productos_solicitado?pagina={page}";
            HttpResponseMessage siesaResponse = await this.httpClient.GetAsync($"{configuration["SiesaUrl"]}{endpoint}");
            if (!siesaResponse.IsSuccessStatusCode)
            {
                throw new SiesaException(siesaResponse, $"Siesa respondió con status: {siesaResponse.StatusCode}");
            }
            string siesaBodyResponse = await siesaResponse.Content.ReadAsStringAsync();
            SiesaProductsDto siesaProductsDto = JsonSerializer.Deserialize<SiesaProductsDto>(siesaBodyResponse);
            List<Product> products = new List<Product>();
            foreach (SiesaProductDto siesaProductDto in siesaProductsDto.productos)
            {
                products.Add(siesaProductDto.getProductFromDto());
            }
            return products.ToArray();
        }

        public async Task<Sku[]> getAllSkus(int page)
        {
            List<Sku> skus = new List<Sku>();
            Product[] allProducts = await this.getAllProducts(page);
            foreach(Product product in allProducts)
            {
                foreach(Sku sku in product.skus)
                {
                    skus.Add(sku);
                }
            }
            return skus.ToArray();
        }

        private async Task setHeaders()
        {
            this.httpClient.DefaultRequestHeaders.Remove("Authorization");
            this.httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + await this.siesaAuth.getToken());
        }
    }
}
