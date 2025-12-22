using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnLTWHQT.Models
{
    public class AddToCartRequest
    {

        public long ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public long? BranchId { get; set; }

    }
}