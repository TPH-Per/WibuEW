using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Data.Entity;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/branches")]
    public class ApiBranchesController : ApiController
    {
        private readonly Entities _db = new Entities();

        // ========================================
        // GET /api/branches
        // Lấy danh sách tất cả chi nhánh đang hoạt động
        // ========================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var branches = _db.branches
                    .Where(b => b.created_at != null) // Hoặc b.deleted_at == null tùy DB của bạn
                    .Select(b => new
                    {
                        Id = b.id,
                        Name = b.name,
                        Location = b.location
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy danh sách chi nhánh thành công",
                    Data = branches
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAll Branches Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/branches/stock/variant/{variantId}
        // Lấy tồn kho của 1 variant cụ thể tại tất cả chi nhánh
        // ========================================
        [HttpGet]
        [Route("stock/variant/{variantId:long}")]
        public IHttpActionResult GetStockByVariant(long variantId)
        {
            try
            {
                // Truy vấn từ bảng branch_inventories (bảng trung gian giữa chi nhánh và variant)
                var stocks = _db.branch_inventories
                    .Include(i => i.branch)
                    .Include(i => i.product_variants)
                    .Where(i => i.product_variant_id == variantId)
                    .Select(i => new
                    {
                        BranchId = i.branch_id,
                        BranchName = i.branch.name,
                        Location = i.branch.location,
                        VariantId = i.product_variant_id,
                        VariantName = i.product_variants.name,
                        Stock = i.quantity_on_hand,    // Số lượng thực tế trong kho
                        Reserved = i.quantity_reserved // Số lượng đã được khách đặt nhưng chưa giao
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy thông tin tồn kho theo variant thành công",
                    Data = stocks
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetStockByVariant Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/branches/stock/product/{productId}
        // Lấy tồn kho của tất cả các variants thuộc về 1 sản phẩm
        // ========================================
        [HttpGet]
        [Route("stock/product/{productId:long}")]
        public IHttpActionResult GetStockByProduct(long productId)
        {
            try
            {
                var stocks = _db.branch_inventories
                    .Include(i => i.branch)
                    .Include(i => i.product_variants)
                    .Where(i => i.product_variants.product_id == productId)
                    .Select(i => new
                    {
                        BranchId = i.branch_id,
                        BranchName = i.branch.name,
                        Location = i.branch.location,
                        VariantId = i.product_variant_id,
                        VariantName = i.product_variants.name,
                        Stock = i.quantity_on_hand,
                        Reserved = i.quantity_reserved
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy thông tin tồn kho theo sản phẩm thành công",
                    Data = stocks
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetStockByProduct Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}