using Ltwhqt.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class PaymentsController : AdminBaseController
    {
        public ActionResult Index(string status = "all")
        {
            var payments = BuildPayments();
            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                payments = payments.Where(p => string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.StatusFilter = status;
            return View(payments);
        }

        public ActionResult Details(long id)
        {
            var payment = BuildPayments().FirstOrDefault(p => p.Id == id) ?? BuildPayments().First();
            return View(payment);
        }

        private static List<PaymentManagementViewModel> BuildPayments()
        {
            return new List<PaymentManagementViewModel>
            {
                new PaymentManagementViewModel { Id = 1, OrderCode = "PO24001", Method = "COD", Amount = 2450000, Status = "pending", CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) },
                new PaymentManagementViewModel { Id = 2, OrderCode = "PO24002", Method = "VNPay", Amount = 1750000, Status = "completed", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                new PaymentManagementViewModel { Id = 3, OrderCode = "PO24003", Method = "Momo", Amount = 480000, Status = "deposit", IsDeposit = true, CreatedAt = DateTimeOffset.UtcNow.AddHours(-5) }
            };
        }
    }
}
