using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    public class POSController : BranchBaseController
    {
        private perwEntities db = new perwEntities();

        // GET: Branch/POS
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
            // Giả sử branch 1 = Chi Nhánh Quận 1
            ViewBag.CurrentBranchID = 1; 
            ViewBag.CurrentUserID = 3; // Branch Manager DEV (existing user)
            ViewBag.BranchName = GetBranchName(1);

            return View();
        }

        // GET: Branch/POS/Debug - Test page
        public ActionResult Debug()
        {
            var paymentMethods = db.payment_methods
                                   .Where(p => p.is_active)
                                   .Select(p => new { p.id, p.name })
                                   .ToList();
            
            ViewBag.PaymentMethods = new SelectList(paymentMethods, "id", "name");
            ViewBag.CurrentBranchID = 1; 
            ViewBag.CurrentUserID = 3; // Branch Manager DEV (existing user)
            ViewBag.BranchName = GetBranchName(1);

            return View();
        }

        [HttpGet]
        public ActionResult SearchProducts(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var branchId = 1; // TODO: Get from session

            try
            {
                var products = db.product_variants
                    .Where(p => p.deleted_at == null && 
                               (p.name.Contains(query) || p.sku.Contains(query)))
                    .Select(p => new
                    {
                        id = p.id,
                        name = p.name,
                        sku = p.sku,
                        price = p.price,
                        image = p.image_url ?? "/Content/images/no-image.png",
                        stock = 0 // Tạm thời trả về 0, sẽ load sau
                    })
                    .Take(10)
                    .ToList();

                // Load stock separately để tránh subquery phức tạp
                var variantIds = products.Select(p => p.id).ToList();
                var stocks = db.branch_inventories
                    .Where(bi => bi.branch_id == branchId && variantIds.Contains(bi.product_variant_id))
                    .ToDictionary(bi => bi.product_variant_id, bi => bi.quantity_on_hand);

                // Merge stock vào kết quả
                var result = products.Select(p => new
                {
                    p.id,
                    p.name,
                    p.sku,
                    p.price,
                    p.image,
                    stock = stocks.ContainsKey(p.id) ? stocks[p.id] : 0
                }).ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log error và return empty
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
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
                var result = db.Database.ExecuteSqlCommand(
                    "EXEC sp_POS_Checkout_Classic @BranchID, @UserID, @PaymentMethodID, @CartItems",
                    pBranch, pUser, pPayment, pCart
                );

                return Json(new { success = true, message = "Thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) 
                    msg += " - " + ex.InnerException.Message;
                
                return Json(new { success = false, message = "Lỗi: " + msg });
            }
        }

        private string GetBranchName(long branchId)
        {
            var branch = db.branches.Find(branchId);
            return branch?.name ?? "Chi Nhánh";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // Helper class for JSON deserialization
        public class CartItemViewModel
        {
            public long VariantID { get; set; }
            public int Qty { get; set; }
        }
    }
}
