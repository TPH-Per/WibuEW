using System;
using System.Linq;
using System.Web.Http;
using DoAnLTWHQT.Models;

namespace DoAnLTWHQT.Controllers
{
    [RoutePrefix("api/suppliers")]
    public class ApiSuppliersController : ApiController
    {
        // Sử dụng tên DbContext của bạn (PerwDbContext hoặc Entities)
        private readonly perwEntities _db = new perwEntities();

        // ========================================
        // GET /api/suppliers
        // Lấy danh sách tất cả nhà cung cấp chưa xóa
        // ========================================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var suppliers = _db.suppliers
                    .Where(s => s.deleted_at == null)
                    .Select(s => new
                    {
                        Id = s.id,
                        Name = s.name,
                        ContactInfo = s.contact_info,
                        // Bạn có thể lấy thêm số lượng sản phẩm từ nhà cung cấp này
                        ProductCount = _db.products.Count(p => p.supplier_id == s.id && p.deleted_at == null)
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy danh sách nhà cung cấp thành công",
                    Data = suppliers
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAll Suppliers Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ========================================
        // GET /api/suppliers/{id}
        // Lấy chi tiết một nhà cung cấp
        // ========================================
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetById(int id)
        {
            try
            {
                var supplier = _db.suppliers
                    .Where(s => s.id == id && s.deleted_at == null)
                    .Select(s => new
                    {
                        Id = s.id,
                        Name = s.name,
                        ContactInfo = s.contact_info
                    })
                    .FirstOrDefault();

                if (supplier == null)
                {
                    return NotFound();
                }

                return Ok(new { Success = true, Data = supplier });
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
