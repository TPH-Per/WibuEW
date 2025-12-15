using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class DiscountsController : AdminBaseController
    {
        public ActionResult Index()
        {
            return View(BuildDiscounts());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Tạo mã giảm giá";
            return View("Form", BuildForm());
        }

        public ActionResult Edit(long id)
        {
            var discount = BuildDiscounts().FirstOrDefault(d => d.Id == id) ?? BuildDiscounts().First();
            var form = BuildForm(id);
            form.Code = discount.Code;
            form.Type = discount.Type;
            form.Value = discount.Value;
            form.MinOrderAmount = discount.MinOrderAmount;
            form.MaxUses = discount.MaxUses;
            form.IsActive = discount.IsActive;
            form.StartAt = discount.StartAt;
            form.EndAt = discount.EndAt;
            ViewBag.Title = "Cập nhật mã giảm giá";
            return View("Form", form);
        }

        private static List<DiscountManagementViewModel> BuildDiscounts()
        {
            return new List<DiscountManagementViewModel>
            {
                new DiscountManagementViewModel { Id = 1, Code = "NEW50", Type = "percent", Value = 50, MinOrderAmount = 500000, MaxUses = 200, UsedCount = 120, IsActive = true, StartAt = DateTimeOffset.UtcNow.AddDays(-3), EndAt = DateTimeOffset.UtcNow.AddDays(7) },
                new DiscountManagementViewModel { Id = 2, Code = "FREESHIP", Type = "fixed", Value = 30000, MaxUses = 1000, UsedCount = 400, IsActive = true, StartAt = DateTimeOffset.UtcNow.AddMonths(-1), EndAt = DateTimeOffset.UtcNow.AddMonths(1) },
                new DiscountManagementViewModel { Id = 3, Code = "CLEAR2023", Type = "percent", Value = 30, MaxUses = 50, UsedCount = 50, IsActive = false, StartAt = DateTimeOffset.UtcNow.AddMonths(-6), EndAt = DateTimeOffset.UtcNow.AddMonths(-5) }
            };
        }

        private static DiscountFormViewModel BuildForm(long? id = null)
        {
            return new DiscountFormViewModel
            {
                Id = id,
                TypeOptions = new List<SelectOptionViewModel>
                {
                    new SelectOptionViewModel { Value = "percent", Label = "Phần trăm" },
                    new SelectOptionViewModel { Value = "fixed", Label = "Số tiền" }
                }
            };
        }
    }
}
