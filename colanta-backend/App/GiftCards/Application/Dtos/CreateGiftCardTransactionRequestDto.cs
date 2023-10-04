﻿namespace colanta_backend.App.GiftCards.Application
{
    using System.Collections.Generic;
    using System;
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class CreateGiftCardTransactionDto
    {
        public string operation { get; set; }
        public decimal value { get; set; }
        public string description { get; set; }
        public string redemptionToken { get; set; }
        public string redemptionCode { get; set; }
        public string requestId { get; set; }
        public CreateGiftCardTransactionOrderInfoDto orderInfo { get; set; }
    }

    public class CreateGiftCardTransactionCartDto
    {
        public List<CreateGiftCardTransactionItemDto> items { get; set; }
        public decimal? grandTotal { get; set; }
        public decimal? discounts { get; set; }
        public decimal? shipping { get; set; }
        public decimal? taxes { get; set; }
        public decimal? itemsTotal { get; set; }
    }

    public class CreateGiftCardTransactionClientProfileDto
    {
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string document { get; set; }
        public string documentType { get; set; }
        public string phone { get; set; }
        public DateTime birthDate { get; set; }
        public bool isCorporate { get; set; }
    }

    public class CreateGiftCardTransactionItemDto
    {
        public string id { get; set; }
        public string productId { get; set; }
        public string refId { get; set; }
        public string name { get; set; }
        public decimal value { get; set; }
        public decimal price { get; set; }
        public int quantity { get; set; }
        public decimal? shippingDiscount { get; set; }
        public decimal? discount { get; set; }
    }

    public class CreateGiftCardTransactionOrderInfoDto
    {
        public string orderId { get; set; }
        public int sequence { get; set; }
        public CreateGiftCardTransactionCartDto cart { get; set; }
        public CreateGiftCardTransactionClientProfileDto clientProfile { get; set; }
        public CreateGiftCardTransactionShippingDto shipping { get; set; }
    }

    public class CreateGiftCardTransactionShippingDto
    {
        public string receiverName { get; set; }
        public string postalCode { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string street { get; set; }
        public string number { get; set; }
        public string neighborhood { get; set; }
        public string complement { get; set; }
        public string reference { get; set; }
    }
}
