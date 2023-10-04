﻿namespace colanta_backend.App.Inventory.Infraestructure
{
    using Inventory.Domain;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Shared.Domain;
    using Microsoft.Extensions.Configuration;

    public class InventoriesVtexRepository : Domain.InventoriesVtexRepository
    {
        private IConfiguration configuration;
        private HttpClient httpClient;
        private string apiKey;
        private string apiToken;
        private string accountName;
        private string vtexEnvironment;

        public InventoriesVtexRepository(IConfiguration configuration) 
        {
            this.configuration = configuration;
            this.httpClient = new HttpClient();
            this.configuration = configuration;
            this.apiKey = configuration["MercolantaVtexApiKey"];
            this.apiToken = configuration["MercolantaVtexToken"];
            this.accountName = configuration["MercolantaAccountName"];
            this.vtexEnvironment = configuration["MercolantaEnvironment"];
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

        public async Task<Inventory> updateInventory(Inventory inventory)
        {
            string warehouse_id = inventory.warehouse.siesa_id;
            int? sku_id = inventory.sku.vtex_id;
            string endpoint = "/api/logistics/pvt/inventory/skus/" + sku_id + "/warehouses/" + warehouse_id;
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpoint;
            UpdateVtexInventoryRequestDto requestBody = new UpdateVtexInventoryRequestDto();
            requestBody.quantity = inventory.quantity;
            requestBody.unlimitedQuantity = false;

            string jsonContent = JsonSerializer.Serialize(requestBody);
            HttpContent httpContent = new StringContent(jsonContent, encoding: System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage vtexResponse = await this.httpClient.PutAsync(url, httpContent);

            if (!vtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(vtexResponse, $"Vtex respondió con status {vtexResponse.StatusCode}");
            }

            return inventory;
        }

        public async Task removeReservedInventory(Inventory inventory)
        {
            string wharehouseId = inventory.warehouse.siesa_id;
            int? skuId = inventory.sku.vtex_id;
            string endpointListReserves = $"/api/logistics/pvt/inventory/reservations/{wharehouseId}/{skuId}";
            string url = "https://" + this.accountName + "." + this.vtexEnvironment + endpointListReserves;
            HttpResponseMessage listReservesVtexResponse = await this.httpClient.GetAsync(url);
            if (!listReservesVtexResponse.IsSuccessStatusCode)
            {
                throw new VtexException(listReservesVtexResponse, $"Vtex respondió con status {listReservesVtexResponse.StatusCode}, al intentar listar las reservas del sku con vtex id {skuId} en el almacen {wharehouseId}");
            }
            string responseBody = await listReservesVtexResponse.Content.ReadAsStringAsync();
            var reservations = JsonSerializer.Deserialize<ReservationsDto>(responseBody);
            foreach(var reservation in reservations.items)
            {
                string endpointRemoveReserve = $"/api/logistics/pvt/inventory/reservations/{reservation.LockId}/cancel";
                url = "https://" + this.accountName + "." + this.vtexEnvironment + endpointRemoveReserve;
                HttpResponseMessage removeReserveVtexResponse = await this.httpClient.PostAsync(url, null);
                if (!removeReserveVtexResponse.IsSuccessStatusCode)
                {
                    throw new VtexException(removeReserveVtexResponse, $"Vtex respondió con status {removeReserveVtexResponse.StatusCode}, al intentar remover la reserva {reservation.LockId} del SKU {reservation.ItemId} en el almacen {wharehouseId}");
                }
            }
        }
    }
}
