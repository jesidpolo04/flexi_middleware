﻿using colanta_backend.App.Promotions.Domain;
using System.Threading.Tasks;

namespace colanta_backend.App.Promotions.Infraestructure
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using App.Products.Domain;
    using App.Brands.Domain;
    using App.Categories.Domain;
    using App.Shared.Domain;
    public class PromotionsVtexRepository : Domain.PromotionsVtexRepository
    {
        private HttpClient httpClient;
        private IConfiguration configuration;
        private string apiKey;
        private string apiToken;
        private string accountName;
        private string vtexEnvironment;
        private JsonSerializerOptions jsonOptions;

        public PromotionsVtexRepository(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.apiKey = configuration["MercolantaVtexApiKey"];
            this.apiToken = configuration["MercolantaVtexToken"];
            this.accountName = configuration["MercolantaAccountName"];
            this.vtexEnvironment = configuration["MercolantaEnvironment"];
            this.jsonOptions = new JsonSerializerOptions();
            this.jsonOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.setCredentialHeaders();
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

        public async Task<Promotion> getPromotionByVtexId(string vtexId, string environment)
        {
            this.changeEnvironment(environment);
            string endpoint = "/api/rnb/pvt/calculatorconfiguration/";
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpoint + vtexId;
            
            HttpResponseMessage vtexResponse = await this.httpClient.GetAsync(url);
            if(vtexResponse.StatusCode != System.Net.HttpStatusCode.OK && vtexResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }
            if(vtexResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            string vtexResponseBody = await vtexResponse.Content.ReadAsStringAsync();
            VtexPromotionDto vtexPromotionDto = JsonSerializer.Deserialize<VtexPromotionDto>(vtexResponseBody);
            return vtexPromotionDto.getPromotionFromDto();
        }
   
        public async Task<Promotion> savePromotion(Promotion promotion)
        {
            this.changeEnvironment(promotion.business);
            string endpoint = "/api/rnb/pvt/calculatorconfiguration";
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpoint;

            RequestCreateVtexPromotionDto requestBody = new RequestCreateVtexPromotionDto();
            requestBody.type = promotion.type;
            requestBody.discountType = promotion.discount_type;
            requestBody.discountExpression = promotion.discount_expression;
            requestBody.name = promotion.name;
            requestBody.beginDateUtc = promotion.begin_date_utc;
            requestBody.endDateUtc = promotion.end_date_utc;
            requestBody.isActive = promotion.is_active;
            requestBody.maximumUnitPriceDiscount = promotion.maximum_unit_price_discount;
            requestBody.nominalDiscountValue = promotion.nominal_discount_value;
            requestBody.percentualDiscountValue = promotion.percentual_discount_value;
            requestBody.percentualShippingDiscountValue = promotion.percentual_shipping_discount_value;
            requestBody.maxNumberOfAffectedItems = promotion.max_number_of_affected_items;
            requestBody.maxNumberOfAffectedItemsGroupKey = promotion.max_number_of_affected_items_group_key;
            requestBody.minimumQuantityBuyTogether = promotion.minimum_quantity_buy_together;
            requestBody.quantityToAffectBuyTogether = promotion.quantity_to_affect_buy_together;

            List<VtexPromotionProduct> vtexPromotionProducts = new List<VtexPromotionProduct>();
            foreach (Product product in promotion.products)
            {
                VtexPromotionProduct vtexPromotionProduct = new VtexPromotionProduct();
                vtexPromotionProduct.id = Convert.ToString(product.vtex_id);
                vtexPromotionProduct.name = product.name;
                vtexPromotionProducts.Add(vtexPromotionProduct);
            }
            requestBody.products = vtexPromotionProducts.ToArray();
            requestBody.productsAreInclusive = true;

            List<VtexPromotionSku> vtexPromotionSkus = new List<VtexPromotionSku>();
            foreach(Sku sku in promotion.skus)
            {
                VtexPromotionSku vtexPromotionSku = new VtexPromotionSku();
                vtexPromotionSku.id = Convert.ToString(sku.vtex_id);
                vtexPromotionSku.name = sku.name;
                vtexPromotionSkus.Add(vtexPromotionSku);
            }
            requestBody.skus = vtexPromotionSkus.ToArray();
            requestBody.skusAreInclusive = true;

            List<VtexPromotionCategory> vtexPromotionCategories = new List<VtexPromotionCategory>();
            foreach(Category category in promotion.categories) 
            {
                VtexPromotionCategory vtexPromotionCategory = new VtexPromotionCategory();
                vtexPromotionCategory.id = Convert.ToString(category.vtex_id);
                vtexPromotionCategory.name = category.name;
                vtexPromotionCategories.Add(vtexPromotionCategory);
            }
            requestBody.categories = vtexPromotionCategories.ToArray();
            requestBody.categoriesAreInclusive = true;

            List<VtexPromotionBrand> vtexPromotionBrands = new List<VtexPromotionBrand>();
            foreach(Brand brand in promotion.brands)
            {
                VtexPromotionBrand vtexPromotionBrand = new VtexPromotionBrand();
                vtexPromotionBrand.id = Convert.ToString(brand.id_vtex);
                vtexPromotionBrand.name = brand.name;
                vtexPromotionBrands.Add(vtexPromotionBrand);
            }
            requestBody.brands = vtexPromotionBrands.ToArray();
            requestBody.brandsAreInclusive = true;

            requestBody.clusterExpressions = JsonSerializer.Deserialize<string[]>(promotion.cluster_expressions, jsonOptions);

            VtexPromotionGifts vtexPromotionGifts = new VtexPromotionGifts();
            vtexPromotionGifts.quantitySelectable = promotion.gift_quantity_selectable;
            List<VtexPromotionGift> vtexGiftList = new List<VtexPromotionGift>();
            foreach(Sku gift in promotion.gifts)
            {
                VtexPromotionGift vtexPromotionGift = new VtexPromotionGift();
                vtexPromotionGift.id = Convert.ToString(gift.vtex_id);
                vtexPromotionGift.name = gift.name;
                vtexGiftList.Add(vtexPromotionGift);
            }
            vtexPromotionGifts.gifts = vtexGiftList.ToArray();
            requestBody.skusGift = vtexPromotionGifts;

            List<VtexPromotionSku> vtexListBuyTogether1 = new List<VtexPromotionSku>();
            foreach(Sku sku in promotion.list_sku_1_buy_together)
            {
                VtexPromotionSku vtexPromotionSku = new VtexPromotionSku();
                vtexPromotionSku.id = sku.vtex_id.ToString();
                vtexPromotionSku.name = sku.name;
                vtexListBuyTogether1.Add(vtexPromotionSku);
            }
            requestBody.listSku1BuyTogether = vtexListBuyTogether1.ToArray();

            List<VtexPromotionSku> vtexListBuyTogether2 = new List<VtexPromotionSku>();
            foreach (Sku sku in promotion.list_sku_2_buy_together)
            {
                VtexPromotionSku vtexPromotionSku = new VtexPromotionSku();
                vtexPromotionSku.id = sku.vtex_id.ToString();
                vtexPromotionSku.name = sku.name;
                vtexListBuyTogether2.Add(vtexPromotionSku);
            }
            requestBody.listSku2BuyTogether = vtexListBuyTogether2.ToArray();

            requestBody.totalValueCeling = promotion.total_value_celing;
            requestBody.totalValueFloor = promotion.total_value_floor;
            requestBody.multipleUsePerClient = promotion.multiple_use_per_client;
            requestBody.cumulative = promotion.cumulative;
            requestBody.origin = "Marketplace";

            string jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
            HttpContent httpContent = new StringContent(jsonContent, encoding: System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage vtexResponse = await this.httpClient.PostAsync(url, httpContent);
            if (!vtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }

            string vtexResponseBody = await vtexResponse.Content.ReadAsStringAsync();
            ResponseCreateVtexPromotionDto responseCreateVtexPromotionDto = JsonSerializer.Deserialize<ResponseCreateVtexPromotionDto>(vtexResponseBody);
            return responseCreateVtexPromotionDto.getPromotionFromDto();
        }

        public async Task<PromotionSummary[]> getPromotionsList()
        {
            string endpoint = "/api/rnb/pvt/benefits/calculatorconfiguration";
            string url = $"https://{this.accountName}.{this.vtexEnvironment}{endpoint}";
            HttpResponseMessage vtexResponse = await this.httpClient.GetAsync(url);
            if (!vtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status: {vtexResponse.StatusCode}");
            }
            string stringBody = await vtexResponse.Content.ReadAsStringAsync();
            GetAllPromotionsVtexResponseDto responseDto = JsonSerializer.Deserialize<GetAllPromotionsVtexResponseDto>(stringBody);
            List<PromotionSummary> promotionsSummaries = new List<PromotionSummary>();
            foreach(VtexPromotionSummaryDto vtexPromotionSummary in responseDto.items)
            {
                promotionsSummaries.Add(vtexPromotionSummary.getPromotionSummary());
            }
            return promotionsSummaries.ToArray();
        }

        public Task changePromotionState(string vtexId, bool state)
        {
            string getPromotionEndpoint = $"/api/rnb/pvt/calculatorconfiguration/{vtexId}";
            string updatePromotionEndpoint = "/api/rnb/pvt/calculatorconfiguration";
            HttpResponseMessage getPromotionVtexResponse = this.httpClient.GetAsync($"https://{accountName}.{vtexEnvironment}{getPromotionEndpoint}").Result;
            if (!getPromotionVtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(getPromotionVtexResponse, $"Vtex respondió con status: {getPromotionVtexResponse.StatusCode}");
            }
            string getPromotionResponseBody = getPromotionVtexResponse.Content.ReadAsStringAsync().Result;
            VtexPromotionDto dto = JsonSerializer.Deserialize<VtexPromotionDto>(getPromotionResponseBody);
            dto.isActive = state;
            HttpContent httpContent = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage updatePromotionVtexResponse = this.httpClient.PostAsync($"https://{accountName}.{vtexEnvironment}{updatePromotionEndpoint}", httpContent).Result;
            if (!updatePromotionVtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(updatePromotionVtexResponse, $"Vtex respondió con status: {updatePromotionVtexResponse.StatusCode}");
            }
            return Task.CompletedTask;
        }
    }
}
