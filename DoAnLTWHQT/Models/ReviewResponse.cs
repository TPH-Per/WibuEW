using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnLTWHQT.Models
{
    public class ReviewResponse
    {

        public long UserId { get; set; }
        public long ProductId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public bool IsApproved { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }
}