using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class WarehouseTransfersController : AdminBaseController
    {
        public ActionResult Index(string status = "all")
        {
            var transfers = BuildTransfers();
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                transfers = transfers.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            return View(transfers);
        }

        public ActionResult Create()
        {
            ViewBag.BranchOptions = BuildBranchOptions();
            ViewBag.VariantOptions = BuildVariantOptions();
            ViewBag.Title = "Phiếu xuất kho đi chi nhánh";
            return View("Form", new WarehouseTransferViewModel { Status = "pending", CreatedAt = DateTimeOffset.UtcNow });
        }

        public ActionResult Details(long id)
        {
            var transfer = BuildTransfers().FirstOrDefault(t => t.Id == id) ?? BuildTransfers().First();
            return View(transfer);
        }

        private static List<WarehouseTransferViewModel> BuildTransfers()
        {
            return new List<WarehouseTransferViewModel>
            {
                new WarehouseTransferViewModel { Id = 701, FromWarehouse = "Kho trung tâm", ToBranch = "Chi nhánh Quận 1", Variant = "Aurora / 40", Quantity = 24, Status = "transferred", CreatedAt = DateTimeOffset.UtcNow.AddHours(-12) },
                new WarehouseTransferViewModel { Id = 702, FromWarehouse = "Kho trung tâm", ToBranch = "Chi nhánh Hà Đông", Variant = "Varsity / M", Quantity = 18, Status = "pending", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                new WarehouseTransferViewModel { Id = 703, FromWarehouse = "Kho trung tâm", ToBranch = "Chi nhánh Thủ Đức", Variant = "Transit / Đen", Quantity = 30, Status = "received", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildBranchOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "1", Label = "Chi nhánh Quận 1" },
                new SelectOptionViewModel { Value = "2", Label = "Chi nhánh Hà Đông" },
                new SelectOptionViewModel { Value = "3", Label = "Chi nhánh Thủ Đức" }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildVariantOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "101", Label = "Sneaker Aurora / 39" },
                new SelectOptionViewModel { Value = "102", Label = "Sneaker Aurora / 40" },
                new SelectOptionViewModel { Value = "202", Label = "Áo khoác Varsity / L" }
            };
        }
    }
}
