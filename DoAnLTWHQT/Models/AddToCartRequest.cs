namespace DoAnLTWHQT.Models
{
    public class AddToCartRequest
    {
        public long ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
