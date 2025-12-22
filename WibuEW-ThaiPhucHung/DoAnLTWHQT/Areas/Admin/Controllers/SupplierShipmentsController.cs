using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Admin;
using Ltwhqt.ViewModels.Shared;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class SupplierShipmentsController : AdminBaseController
    {
        public ActionResult Index(string status = "all")
        {
            var shipments = BuildShipments();
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                shipments = shipments.Where(s => string.Equals(s.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            return View(shipments);
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Tạo phiếu nhập:";
            ViewBag.SupplierOptions = BuildSupplierOptions();
            ViewBag.VariantOptions = BuildVariantOptions();
            return View("Form", new SupplierShipmentViewModel { Status = "pending", ReceivedAt = DateTimeOffset.UtcNow });
        }

        public ActionResult Details(long id)
        {
            var shipment = BuildShipments().FirstOrDefault(s => s.Id == id) ?? BuildShipments().First();
            return View(shipment);
        }

        private static List<SupplierShipmentViewModel> BuildShipments()
        {
            return new List<SupplierShipmentViewModel>
            {
                new SupplierShipmentViewModel { Id = 501, Supplier = "PERW Garment", Warehouse = "Kho trung tâm", Variant = "Varsity / L", Quantity = 60, Status = "received", ReceivedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                new SupplierShipmentViewModel { Id = 502, Supplier = "SneakerHub", Warehouse = "Kho trung tâm", Variant = "Aurora / 40", Quantity = 80, Status = "pending", ReceivedAt = DateTimeOffset.UtcNow.AddDays(1) },
                new SupplierShipmentViewModel { Id = 503, Supplier = "CarryOn", Warehouse = "Kho trung tâm", Variant = "Transit / Đen", Quantity = 120, Status = "quality_check", ReceivedAt = DateTimeOffset.UtcNow }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildSupplierOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "1", Label = "PERW Garment" },
                new SelectOptionViewModel { Value = "2", Label = "SneakerHub" },
                new SelectOptionViewModel { Value = "3", Label = "CarryOn" }
            };
        }

        private static IEnumerable<SelectOptionViewModel> BuildVariantOptions()
        {
            return new List<SelectOptionViewModel>
            {
                new SelectOptionViewModel { Value = "101", Label = "Sneaker Aurora / 39" },
                new SelectOptionViewModel { Value = "202", Label = "Áo khoác Varsity / L" },
                new SelectOptionViewModel { Value = "303", Label = "Balo Transit / Đen" }
            };
        }
    }
}
