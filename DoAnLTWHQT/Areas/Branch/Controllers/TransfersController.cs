using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class TransfersController : BranchBaseController
    {
        public ActionResult Index(string status = "all")
        {
            var transfers = BuildTransfers();
            if (!string.Equals(status, "all", System.StringComparison.OrdinalIgnoreCase))
            {
                transfers = transfers.Where(t => string.Equals(t.Status, status, System.StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            return View(transfers);
        }

        public ActionResult Details(long id)
        {
            var transfer = BuildTransfers().FirstOrDefault(t => t.Id == id) ?? BuildTransfers().First();
            return View(transfer);
        }

        private static List<BranchTransferViewModel> BuildTransfers()
        {
            return new List<BranchTransferViewModel>
            {
                new BranchTransferViewModel { Id = 9101, FromWarehouse = "Kho trung tâm", Branch = "Chi nhánh Q1", Variant = "Aurora / 39", Quantity = 24, Status = "transferred", CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-4) },
                new BranchTransferViewModel { Id = 9102, FromWarehouse = "Kho trung tâm", Branch = "Chi nhánh Q1", Variant = "Varsity / L", Quantity = 12, Status = "pending", CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-2) }
            };
        }
    }
}
