using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnLTWHQT;
using Ltwhqt.ViewModels.Admin;

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    public class DiscountsController : AdminBaseController
    {
        private readonly perwEntities _db = new perwEntities();

        // 1. INDEX: Trả về danh sách Entity gốc để khớp với View
        public ActionResult Index()
        {
            var list = _db.discounts.OrderByDescending(d => d.id).ToList();
            return View(list);
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Tạo mã giảm giá mới";
            return View("Form", new DiscountFormViewModel());
        }

        public ActionResult Edit(long id)
        {
            var discount = _db.discounts.Find(id);
            if (discount == null) return HttpNotFound();

            var vm = new DiscountFormViewModel
            {
                Id = discount.id,
                Code = discount.code,
                Value = discount.value,
                MaxUses = discount.max_uses,
                IsActive = discount.is_active,
                StartAt = discount.start_at,
                EndAt = discount.end_at
            };

            ViewBag.Title = "Cập nhật mã: " + discount.code;
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(DiscountFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = model.Id.HasValue ? "Cập nhật mã" : "Tạo mã mới";
                return View("Form", model);
            }

            try
            {
                if (model.Id.HasValue && model.Id > 0)
                {
                    // --- CẬP NHẬT ---
                    var existDiscount = _db.discounts.Find(model.Id);
                    if (existDiscount == null) return HttpNotFound();

                    // Lưu ý: Không cho sửa Mã Code và Giá Trị khi đã tạo (để tránh sai lệch đơn hàng cũ)
                    // Nếu muốn cho sửa thì bỏ comment 2 dòng dưới:
                    // existDiscount.code = model.Code; 
                    // existDiscount.value = model.Value;

                    existDiscount.max_uses = model.MaxUses;
                    existDiscount.start_at = model.StartAt;
                    existDiscount.end_at = model.EndAt;
                    existDiscount.is_active = model.IsActive;
                    existDiscount.updated_at = DateTime.Now;
                }
                else
                {
                    // --- THÊM MỚI ---
                    if (_db.discounts.Any(d => d.code == model.Code))
                    {
                        ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại!");
                        return View("Form", model);
                    }

                    var newDiscount = new discount
                    {
                        code = model.Code.ToUpper(),
                        value = model.Value,
                        max_uses = model.MaxUses,
                        used_count = 0,
                        is_active = model.IsActive,
                        start_at = model.StartAt,
                        end_at = model.EndAt,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    _db.discounts.Add(newDiscount);
                }

                _db.SaveChanges();
                TempData["SuccessMessage"] = "Đã lưu dữ liệu thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View("Form", model);
            }
        }

        // Hàm Delete (POST) để nút xóa trong View hoạt động
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(long id)
        {
            try
            {
                var discount = _db.discounts.Find(id);
                if (discount != null)
                {
                    // Nếu đã có người dùng mã này -> Chỉ ẩn đi (Soft Delete)
                    if (discount.used_count > 0)
                    {
                        discount.is_active = false;
                        TempData["SuccessMessage"] = "Mã đã được sử dụng nên chỉ tạm ẩn đi, không xóa vĩnh viễn.";
                    }
                    else
                    {
                        // Nếu chưa ai dùng -> Xóa cứng
                        _db.discounts.Remove(discount);
                        TempData["SuccessMessage"] = "Đã xóa mã giảm giá thành công!";
                    }
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xóa: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}