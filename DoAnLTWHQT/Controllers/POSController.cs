using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using DoAnLTWHQT;

namespace DoAnLTWHQT.Controllers
{
    public class POSController : Controller
    {
        private Entities db = new Entities();

        // GET: POS
        public ActionResult Index()
        {
            // Get available payment methods
            var paymentMethods = db.payment_methods
                                   .Where(p => p.is_active)
                                   .Select(p => new { p.id, p.name })
                                   .ToList();
            
            ViewBag.PaymentMethods = new SelectList(paymentMethods, "id", "name");

            // TODO: Get real BranchID and UserID from Session/Auth
            // For now, hardcode or pass via ViewBag
            ViewBag.CurrentBranchID = 1; 
            ViewBag.CurrentUserID = 1;

            return View();
        }

        [HttpGet]
        public ActionResult SearchProducts(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var products = db.product_variants
                .Where(p => p.name.Contains(query) || p.sku.Contains(query) || p.product.name.Contains(query))
                .Select(p => new
                {
                    id = p.id,
                    name = p.name,
                    sku = p.sku,
                    price = p.price,
                    image = p.image_url ?? "/Content/images/no-image.png",
                    // Simple inventory check (optional, optimal to check branch inventory)
                    stock = p.branch_inventories.Where(b => b.branch_id == 1).Sum(b => (int?)b.quantity_on_hand) ?? 0
                })
                .Take(10)
                .ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Checkout(long branchId, long userId, long paymentMethodId, List<CartItemViewModel> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống!" });
            }

            try
            {
                // Create DataTable for TVP
                DataTable table = new DataTable();
                table.Columns.Add("VariantID", typeof(long));
                table.Columns.Add("Qty", typeof(int));

                foreach (var item in cartItems)
                {
                    table.Rows.Add(item.VariantID, item.Qty);
                }

                // Setup parameters
                var pBranch = new SqlParameter("@BranchID", branchId);
                var pUser = new SqlParameter("@UserID", userId);
                var pPayment = new SqlParameter("@PaymentMethodID", paymentMethodId);
                var pCart = new SqlParameter("@CartItems", SqlDbType.Structured)
                {
                    TypeName = "dbo.CartItemTableType",
                    Value = table
                };

                // Execute Stored Procedure
                db.Database.ExecuteSqlCommand("EXEC sp_POS_Checkout_Classic @BranchID, @UserID, @PaymentMethodID, @CartItems",
                    pBranch, pUser, pPayment, pCart);

                return Json(new { success = true, message = "Thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " - " + ex.InnerException.Message;
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        public class CartItemViewModel
        {
            public long VariantID { get; set; }
            public int Qty { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
