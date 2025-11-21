using System.Collections.Generic;
using System.Web.Mvc;
using Ltwhqt.ViewModels.Branch;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class PaymentsController : BranchBaseController
    {
        public ActionResult Index(string method = "all")
        {
            var payments = BuildPayments();
            if (!string.Equals(method, "all", System.StringComparison.OrdinalIgnoreCase))
            {
                payments = payments.FindAll(p => string.Equals(p.Method, method, System.StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.MethodFilter = method;
            return View(payments);
        }

        private static List<BranchPaymentViewModel> BuildPayments()
        {
            return new List<BranchPaymentViewModel>
            {
                new BranchPaymentViewModel { Id = 5001, Method = "Tiền mặt", Amount = 1800000, Status = "completed", CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-1) },
                new BranchPaymentViewModel { Id = 5002, Method = "QR", Amount = 1250000, Status = "completed", CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-3) },
                new BranchPaymentViewModel { Id = 5003, Method = "Chuyển khoản", Amount = 750000, Status = "completed", CreatedAt = System.DateTimeOffset.UtcNow.AddHours(-5) }
            };
        }
    }
}
