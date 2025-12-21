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

        public bool IsWarehouse => LoaiKho == "Kho Tổng";

        public int TongSoLuongTon { get; set; }
        public int HangDatTruoc { get; set; }


        public int SoLuongKhaDung => TongSoLuongTon - HangDatTruoc;
    }
}