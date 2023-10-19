﻿using colanta_backend.App.Products.Domain;
using System.Threading.Tasks;

namespace colanta_backend.App.Products.Infraestructure
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Products.Domain;
    using Shared.Domain;
    using Shared.Infraestructure;
    using Microsoft.Extensions.Configuration;
    public class SkusVtexRepository : Domain.SkusVtexRepository
    {
        private IConfiguration configuration;
        private ILogger logger;
        private HttpClient httpClient;
        private string apiKey;
        private string apiToken;
        private string accountName;
        private string vtexEnvironment;

        public SkusVtexRepository(IConfiguration configuration, ILogger logger)
        {
            this.configuration = configuration;
            this.apiKey = configuration["MercolantaVtexApiKey"];
            this.apiToken = configuration["MercolantaVtexToken"];
            this.accountName = configuration["MercolantaAccountName"];
            this.vtexEnvironment = configuration["MercolantaEnvironment"];

            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.setCredentialHeaders();
            this.logger = logger;
        }

        private void setCredentialHeaders()
        {
            this.httpClient.DefaultRequestHeaders.Remove("X-VTEX-API-AppToken");
            this.httpClient.DefaultRequestHeaders.Remove("X-VTEX-API-AppKey");

            this.httpClient.DefaultRequestHeaders.Add("X-VTEX-API-AppToken", this.apiToken);
            this.httpClient.DefaultRequestHeaders.Add("X-VTEX-API-AppKey", this.apiKey);
        }

        public void changeEnvironment(string environment)
        {
            environment = environment.Trim();
            string[] possibleValues = { "mercolanta", "agrocolanta" };

            foreach (string possibleValue in possibleValues)
            {
                if (environment == possibleValue)
                {
                    if (possibleValue == "mercolanta")
                    {
                        this.apiKey = configuration["MercolantaVtexApiKey"];
                        this.apiToken = configuration["MercolantaVtexToken"];
                        this.accountName = configuration["MercolantaAccountName"];
                        this.vtexEnvironment = configuration["MercolantaEnvironment"];
                    }
                    if (possibleValue == "agrocolanta")
                    {
                        this.apiKey = configuration["AgrocolantaVtexApiKey"];
                        this.apiToken = configuration["AgrocolantaVtexToken"];
                        this.accountName = configuration["AgrocolantaAccountName"];
                        this.vtexEnvironment = configuration["AgrocolantaEnvironment"];
                    }
                    this.setCredentialHeaders();
                    return;
                }
            }
            throw new ArgumentOutOfRangeException(paramName: "enviroment", message: "Invalid Enviroment, Only can be: 'mercolanta' or 'agrocolanta'");
        }

        public async Task<Sku> getSkuByInVtexRef(string inVtexRef)
        {
            string endpoint = "/api/catalog/pvt/stockkeepingunit?refId=";
            string refId = inVtexRef;
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpoint + refId;
            HttpResponseMessage vtexResponse = await this.httpClient.GetAsync(url);
            if (vtexResponse.StatusCode != System.Net.HttpStatusCode.OK && vtexResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            if (vtexResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            string vtexResponseBody = await vtexResponse.Content.ReadAsStringAsync();
            if (vtexResponseBody == "null")
            {
                return null;
            }
            VtexSkuDto skuDto = JsonSerializer.Deserialize<VtexSkuDto>(vtexResponseBody);
            return skuDto.getSkuFromDto();
        }

        public async Task<Sku> getSkuByVtexId(string vtexId)
        {
            string endpoint = "/api/catalog/pvt/stockkeepingunit/";
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpoint + vtexId;
            HttpResponseMessage vtexResponse = await this.httpClient.GetAsync(url);
            if (vtexResponse.StatusCode != System.Net.HttpStatusCode.OK && vtexResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            if (vtexResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await this.logger.writelog(new Exception($"No se encontró en vtex el sku con vtex id {vtexId}"));
                return null;
            }
            string vtexResponseBody = await vtexResponse.Content.ReadAsStringAsync();
            if (vtexResponseBody == "null")
            {
                await this.logger.writelog(new Exception($"No se encontró en vtex el sku con vtex id {vtexId}"));
                return null;
            }
            VtexSkuDto skuDto = JsonSerializer.Deserialize<VtexSkuDto>(vtexResponseBody);
            return skuDto.getSkuFromDto();
        }

        public async Task<Sku> saveSku(Sku sku)
        {
            Sku existSku = await this.getSkuByInVtexRef(sku.concat_siesa_id);
            if(existSku != null)
            {
                return existSku;
            }
            string endpoint = "/api/catalog/pvt/stockkeepingunit";
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpoint;
            CreateVtexSkuDto requestBody = new CreateVtexSkuDto
            {
                ProductId = (int)sku.product.vtex_id,
                Name = sku.name,
                RefId = sku.siesa_id,
                UnitMultiplier = sku.unit_multiplier,
                MeasurementUnit = sku.measurement_unit,
                IsActive = sku.is_active,
                PackagedHeight = sku.packaged_height,
                PackagedWidth = sku.packaged_width,
                PackagedLength = sku.packaged_length,
                PackagedWeightKg = sku.packaged_weight_kg
            };

            string jsonContent = JsonSerializer.Serialize(requestBody);
            HttpContent httpContent = new StringContent(jsonContent, encoding: System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage vtexResponse = await this.httpClient.PostAsync(url, httpContent);
            if (!vtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            string vtexResponseBody = await vtexResponse.Content.ReadAsStringAsync();
            CreatedVtexSkuDto skuDto = JsonSerializer.Deserialize<CreatedVtexSkuDto>(vtexResponseBody);
            this.associateEanSku((int)skuDto.Id, sku.ean).Wait();
            return skuDto.getSkuFromDto();
        }

        public async Task<Sku> updateSku(Sku sku)
        {
            string endpoint = "/api/catalog/pvt/stockkeepingunit/" + sku.vtex_id;
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpoint;
            UpdateVtexSkuDto requestBody = new UpdateVtexSkuDto
            {
                Id = (int)sku.vtex_id,
                ProductId = (int)sku.product.vtex_id,
                Name = sku.name,
                RefId = sku.siesa_id,
                UnitMultiplier = sku.unit_multiplier,
                MeasurementUnit = sku.measurement_unit,
                IsActive = sku.is_active,
                PackagedHeight = sku.packaged_height,
                PackagedWidth = sku.packaged_width,
                PackagedLength = sku.packaged_length,
                PackagedWeightKg = sku.packaged_weight_kg
            };

            string jsonContent = JsonSerializer.Serialize(requestBody);
            HttpContent httpContent = new StringContent(jsonContent, encoding: System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage vtexResponse = await this.httpClient.PutAsync(url, httpContent);
            if (!vtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            string vtexResponseBody = await vtexResponse.Content.ReadAsStringAsync();
            CreatedVtexSkuDto skuDto = JsonSerializer.Deserialize<CreatedVtexSkuDto>(vtexResponseBody);
            return skuDto.getSkuFromDto();
        }

        public async Task<bool> changeSkuState(int vtexId, bool state)
        {
            string getEndpoint = "/api/catalog/pvt/stockkeepingunit/";
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + getEndpoint + vtexId;
            HttpResponseMessage vtexResponse = await this.httpClient.GetAsync(url);
            if (vtexResponse.StatusCode != System.Net.HttpStatusCode.OK && vtexResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            if (vtexResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            string getResponseBody = await vtexResponse.Content.ReadAsStringAsync();
            VtexSkuDto vtexSkuDto = JsonSerializer.Deserialize<VtexSkuDto>(getResponseBody);
            vtexSkuDto.IsActive = state;

            //return to vtex updated

            string updateEndpoint = "/api/catalog/pvt/stockkeepingunit/" + vtexId;
            string urlUpdate = "https://" + this.accountName + "." + this.vtexEnvironment + updateEndpoint;

            string updateRequestBody = JsonSerializer.Serialize(vtexSkuDto);
            HttpContent httpContent = new StringContent(updateRequestBody, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage updateVtexResponse = await this.httpClient.PutAsync(urlUpdate, httpContent);
            if (!updateVtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(updateVtexResponse, $"Vtex respondió con status {updateVtexResponse.StatusCode}");
            }
            return true;
        }

        public Task associateEanSku(int skuVtexId, string ean)
        {
            string endpoint = $"/api/catalog/pvt/stockkeepingunit/{skuVtexId}/ean/{ean}";
            string url = $"https://{accountName}.{vtexEnvironment}{endpoint}";
            HttpResponseMessage vtexResponse = this.httpClient.PostAsync(url, null).Result;
            if (!vtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            return Task.CompletedTask;
        }
    }
}
