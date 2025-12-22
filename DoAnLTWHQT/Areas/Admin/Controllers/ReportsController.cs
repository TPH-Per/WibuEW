using Ltwhqt.ViewModels.Admin;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;
// using System.Data.Entity; // Bỏ comment dòng này nếu bị lỗi LINQ

namespace DoAnLTWHQT.Areas.Admin.Controllers
{
    // BẠN ĐANG THIẾU DÒNG KHAI BÁO CLASS NÀY:
    public class ReportsController : AdminBaseController
    {
        // 1. Hàm private lấy dữ liệu từ SQL
        // 1. Hàm private lấy dữ liệu báo cáo (KHÔNG DÙNG BẢNG ORDERS)
        private List<ReportIndicatorViewModel> GetKPIData()
        {
            using (var db = new perwEntities())
            {
                var sevenDaysAgo = DateTime.Now.AddDays(-7);

                // --- KPI 1: KHÁCH HÀNG MỚI (Dùng bảng users) ---
                // Đếm số user tạo mới trong 7 ngày qua
                var newCustomers = db.users
                    .Count(u => u.created_at >= sevenDaysAgo);

                var stockWarehouse = db.inventories.Sum(i => (int?)i.quantity_on_hand) ?? 0;
                var stockBranch = db.branch_inventories.Sum(b => (int?)b.quantity_on_hand) ?? 0;
                var totalStock = stockWarehouse + stockBranch;

                // --- KPI 3: TỔNG SẢN PHẨM/SKU (Dùng bảng product_variants) ---
                // Đếm tổng số lượng mặt hàng (SKU) đang quản lý
                var totalSKUs = db.product_variants.Count();

                // --- KPI 4: SỐ LƯỢNG KHO & CHI NHÁNH (Dùng bảng warehouses & branches) ---
                var totalLocations = db.warehouses.Count() + db.branches.Count();

                // --- TRẢ VỀ DỮ LIỆU ---
                return new List<ReportIndicatorViewModel>
                {
                    new ReportIndicatorViewModel
                    {
                        Label = "Khách hàng mới (7 ngày)",
                        Value = newCustomers.ToString("N0"), // Format số: 1,234
                        Trend = "---",
                        Description = "Tài khoản đăng ký mới"
                    },
                    new ReportIndicatorViewModel
                    {
                        Label = "Tổng tồn kho hệ thống",
                        Value = totalStock.ToString("N0"),
                        Trend = "---",
                        Description = "Tổng sản phẩm thực tế (Kho + CN)"
                    },
                    new ReportIndicatorViewModel
                    {
                        Label = "Tổng mã sản phẩm",
                        Value = totalSKUs.ToString("N0"),
                        Trend = "---",
                    },
                    new ReportIndicatorViewModel
                    {
                        Label = "Điểm vận hành",
                        Value = totalLocations.ToString("N0"),
                        Trend = "---",
                        Description = "Tổng số Kho và Chi nhánh"
                    }
                };
            }
        }

        // GET: Admin/Reports
        public ActionResult Index()
        {
            var indicators = GetKPIData();
            return View(indicators);
        }

        // GET: Admin/Reports/ExportToExcel
        public ActionResult ExportToExcel()
        {
            var data = GetKPIData();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Báo Cáo KPI");

                // --- HEADER ---
                worksheet.Cells[1, 1].Value = "Chỉ số KPI";
                worksheet.Cells[1, 2].Value = "Giá trị";
                worksheet.Cells[1, 3].Value = "Xu hướng";
                worksheet.Cells[1, 4].Value = "Mô tả chi tiết";

                // Format Header
                using (var range = worksheet.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 123, 255));
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // --- DATA ---
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.Label;
                    worksheet.Cells[row, 2].Value = item.Value;

                    var trendCell = worksheet.Cells[row, 3];
                    trendCell.Value = item.Trend;
                    if (item.Trend.Contains("+")) trendCell.Style.Font.Color.SetColor(Color.Green);
                    else if (item.Trend.Contains("-")) trendCell.Style.Font.Color.SetColor(Color.Red);

                    worksheet.Cells[row, 4].Value = item.Description;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"BaoCaoKPI_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}