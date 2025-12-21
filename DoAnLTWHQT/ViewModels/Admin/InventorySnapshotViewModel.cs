using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnLTWHQT.ViewModels.Admin
{
    public class InventorySnapshotViewModel
    {
        public long VariantId { get; set; }
        public string VariantName { get; set; }
        public string WarehouseName { get; set; }
        public int QuantityOnHand { get; set; }   // Tồn thực tế
        public int QuantityReserved { get; set; } // Đang giữ
        public int ReorderLevel { get; set; }

        // Tự động tính số lượng khả dụng để hiển thị lên bảng chi tiết
        public int Available => QuantityOnHand - QuantityReserved;
    }
}