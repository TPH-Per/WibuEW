using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class TransfersController : WarehouseBaseController
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
            var vm = new WarehouseTransferViewModel { CreatedAt = DateTimeOffset.UtcNow, Status = "pending", FromWarehouse = "Kho trung tâm" };
            return View(vm);
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
                new WarehouseTransferViewModel { Id = 8001, FromWarehouse = "Kho trung tâm", ToBranch = "Chi nhánh Q1", Variant = "Aurora / 39", Quantity = 24, Status = "transferred", CreatedAt = DateTimeOffset.UtcNow.AddHours(-4) },
                new WarehouseTransferViewModel { Id = 8002, FromWarehouse = "Kho trung tâm", ToBranch = "Chi nhánh Hà Đông", Variant = "Varsity / L", Quantity = 12, Status = "pending", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildBranchOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "1", Label = "Chi nhánh Q1" },
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
