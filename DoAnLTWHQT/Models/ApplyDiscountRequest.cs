using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnLTWHQT.Models
{
    public class ApplyDiscountRequest
    {

        public string Code { get; set; }
        public decimal OrderTotal { get; set; }

    }
}