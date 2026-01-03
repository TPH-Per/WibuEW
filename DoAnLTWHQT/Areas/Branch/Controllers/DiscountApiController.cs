using System;
using System.Linq;
using System.Web.Mvc;

namespace DoAnLTWHQT.Areas.Branch.Controllers
{
    // Controller doc lap, khong ke thua BranchBaseController de tranh loi phan quyen
    public class DiscountApiController : Controller
    {
        private perwEntities db = new perwEntities();

        [HttpGet]
        [AllowAnonymous] // Cho phep truy cap thoai mai
        public ActionResult Check(string code)
        {
            // Enable CORS if needed (optional)
            Response.AddHeader("Access-Control-Allow-Origin", "*");

            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã voucher" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                code = code.Trim().ToUpper();
                
                // Query DB
                var voucher = db.discounts
                    .FirstOrDefault(d => d.code == code && d.is_active);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Mã voucher không tồn tại" }, JsonRequestBehavior.AllowGet);
                }

                var now = DateTime.Now;

                if (voucher.end_at.HasValue && voucher.end_at.Value < now)
                {
                    return Json(new { success = false, message = "Mã đã hết hạn" }, JsonRequestBehavior.AllowGet);
                }

                if (voucher.start_at.HasValue && voucher.start_at.Value > now)
                {
                    return Json(new { success = false, message = "Mã chưa đến ngày áp dụng" }, JsonRequestBehavior.AllowGet);
                }

                if (voucher.max_uses.HasValue && voucher.used_count >= voucher.max_uses.Value)
                {
                    return Json(new { success = false, message = "Mã đã hết lượt dùng" }, JsonRequestBehavior.AllowGet);
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
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
