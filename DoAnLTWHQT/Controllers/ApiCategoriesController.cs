using System;
using System.Linq;
using System.Web.Http;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/categories")]
    public class ApiCategoriesController : ApiController
    {
        private readonly perwEntities _db = new perwEntities();

        // ========================================
        // GET /api/categories
        // Lấy danh sách tất cả danh mục chưa xóa
        // ========================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var categories = _db.categories
                    .Where(c => c.deleted_at == null)
                    .Select(c => new
                    {
                        Id = c.id,
                        Name = c.name,
                        Slug = c.slug,
                        // Bạn có thể thêm ProductCount nếu cần hiển thị số lượng sản phẩm
                        ProductCount = _db.products.Count(p => p.category_id == c.id && p.deleted_at == null)
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy danh sách danh mục thành công",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAll Categories Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/categories/{id}
        // Lấy chi tiết một danh mục (nếu cần)
        // ========================================
        [HttpGet]
        [Route("{id:long}")]
        public IHttpActionResult GetById(long id)
        {
            try
            {
                var category = _db.categories
                    .Where(c => c.id == id && c.deleted_at == null)
                    .Select(c => new {
                        Id = c.id,
                        Name = c.name,
                        Slug = c.slug,
                    })
                    .FirstOrDefault();

                if (category == null)
                {
                    return NotFound();
                }

                return Ok(new { Success = true, Data = category });
            }
            catch (Exception ex)
            {
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
