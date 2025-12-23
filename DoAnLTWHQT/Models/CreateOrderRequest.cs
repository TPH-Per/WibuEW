using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnLTWHQT.Models
{
    public class CreateOrderRequest
    {

        public string ShippingRecipientName { get; set; }
        public string ShippingRecipientPhone { get; set; }
        public string ShippingAddress { get; set; }
        public long? PaymentMethodId { get; set; }
        public string DiscountCode { get; set; }

    }
}