using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class ProductReviewsController : AdminBaseController
    {
        public ActionResult Index(string status = "pending")
        {
            ViewBag.StatusFilter = status;
            return View(BuildReviews());
        }

        public ActionResult Moderate(long productId)
        {
            ViewBag.ProductId = productId;
            ViewBag.ProductName = "Sneaker Aurora";
            return View("Index", BuildReviews());
        }

        private static IList<ProductReviewManagementViewModel> BuildReviews()
        {
            return new List<ProductReviewManagementViewModel>
            {
                new ProductReviewManagementViewModel
                {
                    ProductId = 10,
                    ProductName = "Sneaker Aurora",
                    CustomerName = "Nguyễn An",
                    Rating = 5,
                    Comment = "Giày nhẹ, đi êm chân.",
                    Status = "approved",
                    IsApproved = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
                },
                new ProductReviewManagementViewModel
                {
                    ProductId = 11,
                    ProductName = "Áo khoác Varsity",
                    CustomerName = "Trần Bình",
                    Rating = 4,
                    Comment = "Áo đẹp, nhưng size hơi nhỏ.",
                    Status = "pending",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                },
                new ProductReviewManagementViewModel
                {
                    ProductId = 12,
                    ProductName = "Balo Transit",
                    CustomerName = "Lê Chi",
                    Rating = 2,
                    Comment = "Dây kéo hơi khó dùng.",
                    Status = "rejected",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-6)
                }
            };
        }
    }
}
