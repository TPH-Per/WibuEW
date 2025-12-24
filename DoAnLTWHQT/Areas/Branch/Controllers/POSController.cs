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

            // Get current user's branch
            var branchId = GetCurrentBranchId();
            var userId = GetCurrentUserId();
            
            if (branchId == null)
            {
                TempData["Error"] = "Không tìm thấy chi nhánh của bạn.";
                return RedirectToAction("Index", "Dashboard");
            }
            
            ViewBag.CurrentBranchID = branchId; 
            ViewBag.CurrentUserID = userId;
            ViewBag.BranchName = GetBranchName(branchId.Value);

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
            
            var branchId = GetCurrentBranchId();
            var userId = GetCurrentUserId();
            
            ViewBag.CurrentBranchID = branchId ?? 1; 
            ViewBag.CurrentUserID = userId ?? 3;
            ViewBag.BranchName = GetBranchName(branchId ?? 1);

            return View();
        }

        [HttpGet]
        public ActionResult SearchProducts(string query)
        {
            if (string.IsNullOrEmpty(query))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var branchId = GetCurrentBranchId() ?? 1;

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

        /// <summary>
        /// Lấy tất cả sản phẩm đang có tồn kho trong chi nhánh hiện tại
        /// </summary>
        [HttpGet]
        public ActionResult GetBranchInventoryProducts(string query = "")
        {
            var branchId = GetCurrentBranchId() ?? 1;

            try
            {
                var inventoryQuery = db.branch_inventories
                    .Where(bi => bi.branch_id == branchId && bi.quantity_on_hand > 0 && bi.product_variants.deleted_at == null);

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(query))
                {
                    query = query.ToLower();
                    inventoryQuery = inventoryQuery.Where(bi => 
                        bi.product_variants.name.ToLower().Contains(query) || 
                        bi.product_variants.sku.ToLower().Contains(query));
                }

                var products = inventoryQuery
                    .OrderByDescending(bi => bi.quantity_on_hand)
                    .Take(50)
                    .Select(bi => new
                    {
                        id = bi.product_variant_id,
                        name = bi.product_variants.name,
                        sku = bi.product_variants.sku,
                        price = bi.product_variants.price,
                        image = bi.product_variants.image_url ?? "/Content/images/no-image.png",
                        stock = bi.quantity_on_hand
                    })
                    .ToList();

                return Json(products, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBranchInventory error: {ex.Message}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Checkout(long branchId, long userId, long paymentMethodId, string paymentType, long? discountId, decimal discountAmount, List<CartItemViewModel> cartItems)
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

                // Determine payment type (qr_atBranch or cash_atBranch)
                var orderStatus = string.IsNullOrEmpty(paymentType) ? "cash_atBranch" : paymentType;

                // Setup parameters
                var pBranch = new SqlParameter("@BranchID", branchId);
                var pUser = new SqlParameter("@UserID", userId);
                var pPayment = new SqlParameter("@PaymentMethodID", paymentMethodId);
                var pCart = new SqlParameter("@CartItems", SqlDbType.Structured)
                {
                    TypeName = "dbo.CartItemTableType",
                    Value = table
                };
                var pPaymentType = new SqlParameter("@PaymentType", orderStatus);
                var pDiscountId = new SqlParameter("@DiscountID", (object)discountId ?? DBNull.Value);
                var pDiscountAmount = new SqlParameter("@DiscountAmount", discountAmount);

                // Execute Stored Procedure with PaymentType and Discount
                var result = db.Database.ExecuteSqlCommand(
                    "EXEC sp_POS_Checkout_Classic @BranchID, @UserID, @PaymentMethodID, @CartItems, @PaymentType, @DiscountID, @DiscountAmount",
                    pBranch, pUser, pPayment, pCart, pPaymentType, pDiscountId, pDiscountAmount
                );

                // Update discount used_count if voucher was applied
                if (discountId.HasValue)
                {
                    var discount = db.discounts.Find(discountId.Value);
                    if (discount != null)
                    {
                        discount.used_count++;
                        discount.updated_at = DateTime.Now;
                        db.SaveChanges();
                    }
                }

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

        [HttpGet]
        public ActionResult ValidateVoucher(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã voucher" }, JsonRequestBehavior.AllowGet);
            }

            var voucher = db.discounts
                .FirstOrDefault(d => d.code.ToUpper() == code.ToUpper() 
                                    && d.is_active);

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã voucher không tồn tại hoặc đã hết hiệu lực" }, JsonRequestBehavior.AllowGet);
            }

            // Check if voucher is expired
            if (voucher.end_at.HasValue && voucher.end_at.Value < DateTime.Now)
            {
                return Json(new { success = false, message = "Mã voucher đã hết hạn" }, JsonRequestBehavior.AllowGet);
            }

            // Check if voucher hasn't started yet
            if (voucher.start_at.HasValue && voucher.start_at.Value > DateTime.Now)
            {
                return Json(new { success = false, message = "Mã voucher chưa đến thời gian sử dụng" }, JsonRequestBehavior.AllowGet);
            }

            // Check usage limit
            if (voucher.max_uses.HasValue && voucher.used_count >= voucher.max_uses.Value)
            {
                return Json(new { success = false, message = "Mã voucher đã hết lượt sử dụng" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                voucher = new
                {
                    id = voucher.id,
                    code = voucher.code,
                    value = voucher.value,
                    type = voucher.type
                }
            }, JsonRequestBehavior.AllowGet);
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
