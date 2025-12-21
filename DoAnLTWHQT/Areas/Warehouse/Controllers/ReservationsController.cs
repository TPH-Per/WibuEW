using System.Collections.Generic;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Warehouse;

namespace DoAnLTWHQT.Areas.Warehouse.Controllers
{
    public class ReservationsController : WarehouseBaseController
    {
        public ActionResult Index()
        {
            return View(BuildReservations());
        }

        private static IList<WarehouseReservationViewModel> BuildReservations()
        {
            return new List<WarehouseReservationViewModel>
            {
                new WarehouseReservationViewModel { OrderId = 9101, OrderCode = "PO24010", Channel = "Online", Variant = "Aurora / 39", ReservedQuantity = 5, Status = "processing", ReservedAt = System.DateTimeOffset.UtcNow.AddHours(-6) },
                new WarehouseReservationViewModel { OrderId = 9102, OrderCode = "BR24015", Channel = "Branch POS", Variant = "Aurora / 40", ReservedQuantity = 3, Status = "pending_pick", ReservedAt = System.DateTimeOffset.UtcNow.AddHours(-3) },
                new WarehouseReservationViewModel { OrderId = 9103, OrderCode = "PR24020", Channel = "Pre-order", Variant = "Varsity / L", ReservedQuantity = 4, Status = "deposit_paid", ReservedAt = System.DateTimeOffset.UtcNow.AddDays(-1) }
            };
        }
    }
}
