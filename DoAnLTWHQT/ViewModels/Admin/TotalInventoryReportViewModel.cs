using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnLTWHQT.ViewModels.Admin
{
    public class TotalInventoryReportViewModel
    {
        public string TenKho { get; set; }
        public string LoaiKho { get; set; }

        // Thuộc tính hỗ trợ kiểm tra loại kho nhanh trong View
        public bool IsWarehouse => LoaiKho == "Kho Tổng";

        public int TongSoLuongTon { get; set; }
        public int HangDatTruoc { get; set; }

        // Tự động tính số lượng khả dụng: Khả dụng = Tổng tồn - Đang giữ
        public int SoLuongKhaDung => TongSoLuongTon - HangDatTruoc;
    }
}