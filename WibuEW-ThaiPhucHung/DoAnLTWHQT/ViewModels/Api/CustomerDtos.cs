using System;
using System.Collections.Generic;

namespace Ltwhqt.ViewModels.Api
{
    public class ProductDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public decimal Rating { get; set; }

        public int ReviewCount { get; set; }

        public IList<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
    }

    public class ProductVariantDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public int AvailableQuantity { get; set; }
    }

    public class CartItemDto
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public ProductVariantDto Variant { get; set; } = new ProductVariantDto();

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class OrderDto
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public IList<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "pending";

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public PaymentDto Payment { get; set; }
    }

    public class OrderLineDto
    {
        public long ProductVariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }

    public class PaymentDto
    {
        public long Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Method { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        public bool IsDeposit { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
