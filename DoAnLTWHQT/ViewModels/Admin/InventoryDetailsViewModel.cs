using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DoAnLTWHQT;

namespace DoAnLTWHQT.ViewModels.Admin
{
    public class InventoryDetailsViewModel
    {
        // Thông tin tồn kho hiện tại
        public inventory Inventory { get; set; }

        // Danh sách lịch sử giao dịch tương ứng
        public List<inventory_transactions> Transactions { get; set; }
    }
}