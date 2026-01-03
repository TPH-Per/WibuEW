# 📋 PHÂN CÔNG CÔNG VIỆC - CHIA 2 NGƯỜI

## 🔔 **THÔNG TIN VỀ ALERT/NOTIFICATION**

### ✅ Hiện tại đã thay thế `alert()` bằng

#### 1. **ModalPopup** (thay thế `alert()`, `confirm()`)

```javascript
// Success
ModalPopup.success('Tiêu đề', 'Nội dung', callback);

// Error
ModalPopup.error('Lỗi!', 'Mô tả lỗi');

// Warning
ModalPopup.warning('Cảnh báo!', 'Vui lòng kiểm tra...');

// Confirm
ModalPopup.confirm({
    type: 'question',
    title: 'Xác nhận',
    message: 'Bạn có chắc chắn?',
    confirmText: 'Đồng ý',
    cancelText: 'Hủy',
    onConfirm: function() { /* code */ }
});

// Loading
ModalPopup.showLoading('Đang xử lý...');
ModalPopup.hideLoading();
```

#### 2. **Toast** (thông báo nhỏ góc màn hình)

```javascript
Toast.success('Thành công!');
Toast.error('Có lỗi xảy ra!');
Toast.warning('Cảnh báo!');
Toast.info('Thông tin');
```

**📁 File:** `DoAnLTWHQT/Scripts/modal-popup.js` và `toast.js`

---

## 👤 **NGƯỜI 1 - ADMIN + WAREHOUSE NHẬP KHO**

### 📊 **A. ADMIN ROLE**

#### **A1. Báo cáo thống kê với biểu đồ** ⭐⭐⭐

**File:** `Areas/Admin/Views/Reports/Index.cshtml`

**Nhiệm vụ:**

- [ ] Thêm **Chart.js** vào project
- [ ] Tạo biểu đồ cột: Doanh thu theo tháng
- [ ] Tạo biểu đồ tròn: Phân bổ doanh thu theo chi nhánh
- [ ] API endpoint: `/Admin/Reports/GetChartData`
- [ ] Responsive design

**Code mẫu:**

```javascript
// Biểu đồ cột
new Chart(ctx, {
    type: 'bar',
    data: {
        labels: ['T1', 'T2', 'T3', ...],
        datasets: [{
            label: 'Doanh thu',
            data: [100000, 150000, ...],
            backgroundColor: 'rgba(102, 126, 234, 0.8)'
        }]
    }
});

// Biểu đồ tròn
new Chart(ctx, {
    type: 'pie',
    data: {
        labels: ['Chi nhánh 1', 'Chi nhánh 2', ...],
        datasets: [{
            data: [30, 40, 30],
            backgroundColor: ['#667eea', '#764ba2', '#11998e']
        }]
    }
});
```

---

#### **A2. Validate Password Confirmation** ⭐

**File:** `Areas/Admin/Views/Users/Form.cshtml`

**Nhiệm vụ:**

- [ ] Thêm field "Xác nhận mật khẩu"
- [ ] Client-side validation: so sánh 2 password
- [ ] Server-side validation trong Controller
- [ ] Hiển thị lỗi bằng **ModalPopup.warning()**

**Code mẫu:**

```javascript
$('#registerForm').on('submit', function(e) {
    var password = $('#Password').val();
    var confirmPassword = $('#ConfirmPassword').val();
    
    if (password !== confirmPassword) {
        e.preventDefault();
        ModalPopup.warning('Mật khẩu không khớp!', 
            'Vui lòng nhập lại mật khẩu xác nhận');
        return false;
    }
});
```

---

#### **A3. Chức năng XÓA** ⭐⭐

##### **A3.1. Xóa Chi nhánh**

**File:** `Areas/Admin/Controllers/BranchesController.cs`

- [ ] Action `Delete(id)` - soft delete
- [ ] Kiểm tra: có đơn hàng đang xử lý không?
- [ ] ModalPopup.confirm() trước khi xóa
- [ ] Toast.success() sau khi xóa thành công

##### **A3.2. Xóa Nhà cung cấp**

**File:** `Areas/Admin/Controllers/SuppliersController.cs`

- [ ] Tương tự như xóa chi nhánh
- [ ] Check: có phiếu nhập đang pending không?

##### **A3.3. Xóa Danh mục**

**File:** `Areas/Admin/Controllers/CategoriesController.cs`

- [ ] Check: có sản phẩm trong danh mục không?
- [ ] Nếu có → hiển thị warning, yêu cầu di chuyển SP trước

**Pattern chung:**

```javascript
function deleteItem(id, type) {
    ModalPopup.confirm({
        type: 'warning',
        title: 'Xác nhận xóa',
        message: `Bạn có chắc muốn xóa ${type} này?`,
        confirmText: 'Xóa',
        cancelText: 'Hủy',
        confirmClass: 'btn-danger',
        onConfirm: function() {
            ModalPopup.showLoading('Đang xóa...');
            $.post(`/Admin/${type}/Delete/${id}`, function(res) {
                ModalPopup.hideLoading();
                if (res.success) {
                    Toast.success('Xóa thành công!');
                    location.reload();
                } else {
                    ModalPopup.error('Lỗi!', res.message);
                }
            });
        }
    });
}
```

##### **A3.4. Xóa Sản phẩm & Variant**

**File:** `Areas/Admin/Controllers/ProductsController.cs`

- [ ] Xóa product → cascade xóa variants
- [ ] Xóa variant riêng lẻ
- [ ] Check inventory trước khi xóa

##### **A3.5. Xóa Mã giảm giá**

**File:** `Areas/Admin/Controllers/DiscountsController.cs`

- [ ] Check: đã được sử dụng chưa?
- [ ] Nếu `used_count > 0` → warning, confirm "vẫn muốn xóa?"

---

### 📦 **B. WAREHOUSE ROLE - PHIẾU NHẬP KHO**

#### **B1. Tạo Phiếu Nhập Kho** ⭐⭐⭐

**File:** `Areas/Warehouse/Views/Shipments/Create.cshtml`

**Nhiệm vụ đã làm:**

- [x] Chọn nhà cung cấp ✅
- [x] Load sản phẩm theo NCC ✅
- [x] Nhập giá nhập ✅
- [x] Validation form trống ✅ (đã làm ở Step 376)

**Nhiệm vụ bổ sung:**

- [ ] Xử lý trường hợp "NCC không có sản phẩm"
  - Hiển thị: ModalPopup.info('NCC chưa có sản phẩm', 'Vui lòng thêm sản phẩm cho NCC này')
- [ ] Tạo phiếu → gọi stored procedure
- [ ] Toast.success('Tạo phiếu nhập thành công!')

---

#### **B2. Xác Nhận Hoàn Thành Phiếu Nhập** ⭐⭐

**File:** `Areas/Warehouse/Views/Shipments/Detail.cshtml`

**Nhiệm vụ:**

- [ ] Thêm button "Hoàn thành" khi status = 'pending'
- [ ] Click → ModalPopup.confirm()
- [ ] Gọi API `/Warehouse/Shipments/Complete/{id}`
- [ ] Server: Update status → 'completed', cập nhật inventory
- [ ] Toast.success('Đã cập nhật tồn kho!')

**Code mẫu:**

```javascript
$('#btnComplete').click(function() {
    ModalPopup.confirm({
        type: 'question',
        title: 'Xác nhận hoàn thành',
        message: 'Xác nhận đã nhận đủ hàng và cập nhật vào kho?',
        confirmText: 'Hoàn thành',
        onConfirm: function() {
            ModalPopup.showLoading('Đang cập nhật...');
            $.post('/Warehouse/Shipments/Complete/@Model.Id', function(res) {
                ModalPopup.hideLoading();
                if (res.success) {
                    Toast.success('Phiếu nhập đã hoàn thành!');
                    location.href = '/Warehouse/Shipments';
                } else {
                    ModalPopup.error('Lỗi!', res.message);
                }
            });
        }
    });
});
```

---

## 👤 **NGƯỜI 2 - BRANCH + WAREHOUSE XUẤT KHO**

### 🏪 **C. BRANCH ROLE**

#### **C1. Xác Nhận Nhận Hàng từ Warehouse** ⭐⭐

**File:** `Areas/Branch/Views/Transfers/Detail.cshtml`

**Nhiệm vụ:**

- [ ] Hiển thị chi tiết phiếu chuyển kho
- [ ] Khi status = 'shipping' → hiển thị button "Xác nhận đã nhận"
- [ ] Click → ModalPopup.confirm()
- [ ] API: `/Branch/Transfers/ConfirmReceived/{id}`
- [ ] Server: Update status → 'completed', cập nhật branch_inventories
- [ ] Toast.success('Đã nhận hàng thành công!')

**Code mẫu:**

```javascript
$('#btnConfirmReceived').click(function() {
    var transferId = $(this).data('id');
    ModalPopup.confirm({
        type: 'question',
        title: 'Xác nhận nhận hàng',
        message: 'Xác nhận đã nhận đủ hàng theo phiếu chuyển?',
        confirmText: 'Đã nhận',
        confirmClass: 'btn-success',
        onConfirm: function() {
            ModalPopup.showLoading('Đang xử lý...');
            $.post(`/Branch/Transfers/ConfirmReceived/${transferId}`, function(res) {
                ModalPopup.hideLoading();
                if (res.success) {
                    ModalPopup.success('Thành công!', 
                        'Đã cập nhật tồn kho chi nhánh', 
                        function() {
                            location.reload();
                        });
                } else {
                    ModalPopup.error('Lỗi!', res.message);
                }
            });
        }
    });
});
```

---

#### **C2. Cập Nhật Trạng Thái Đơn Hàng** ⭐⭐

**File:** `Areas/Branch/Views/Orders/Detail.cshtml`

**Nhiệm vụ:**

- [ ] Dropdown chọn trạng thái: pending → processing → shipping → completed
- [ ] Button "Cập nhật trạng thái"
- [ ] ModalPopup.confirm() trước khi cập nhật
- [ ] API: `/Branch/Orders/UpdateStatus`
- [ ] Toast.success() khi thành công

**Code mẫu:**

```javascript
$('#btnUpdateStatus').click(function() {
    var orderId = $('#orderId').val();
    var newStatus = $('#statusSelect').val();
    var statusText = $('#statusSelect option:selected').text();
    
    ModalPopup.confirm({
        type: 'question',
        title: 'Cập nhật trạng thái',
        message: `Chuyển đơn hàng sang trạng thái "${statusText}"?`,
        confirmText: 'Cập nhật',
        onConfirm: function() {
            ModalPopup.showLoading('Đang cập nhật...');
            $.post('/Branch/Orders/UpdateStatus', {
                orderId: orderId,
                status: newStatus
            }, function(res) {
                ModalPopup.hideLoading();
                if (res.success) {
                    Toast.success('Đã cập nhật trạng thái!');
                    location.reload();
                } else {
                    ModalPopup.error('Lỗi!', res.message);
                }
            });
        }
    });
});
```

---

#### **C3. Thanh Toán Tại Quầy (POS)** ⭐⭐⭐

**File:** `Areas/Branch/Views/POS/Index.cshtml`

**Nhiệm vụ đã làm:**

- [x] Chọn sản phẩm, thêm vào giỏ ✅
- [x] Chọn phương thức thanh toán ✅
- [x] Áp mã giảm giá ✅

**Nhiệm vụ bổ sung:**

- [ ] Xác nhận thanh toán → ModalPopup.confirm()
- [ ] Sau khi thanh toán thành công:
  - ModalPopup.success() với thông tin đơn hàng
  - Hiển thị mã đơn hàng
  - Option: In hóa đơn
- [ ] Clear giỏ hàng sau khi thành công

**Code đã có sẵn, cần update:**

```javascript
$('#btnCheckout').click(function() {
    if (cart.length === 0) {
        ModalPopup.warning('Giỏ hàng trống!', 'Vui lòng thêm sản phẩm');
        return;
    }
    
    var paymentMethod = $('#paymentMethodSelect option:selected').text();
    var total = calculateTotal();
    
    ModalPopup.confirm({
        type: 'question',
        title: 'Xác nhận thanh toán',
        message: `Thanh toán ${formatVND(total)} bằng ${paymentMethod}?`,
        confirmText: 'Thanh toán',
        confirmClass: 'btn-success',
        onConfirm: function() {
            ModalPopup.showLoading('Đang xử lý thanh toán...');
            
            $.post('/Branch/POS/Checkout', {
                branchId: branchId,
                userId: userId,
                paymentMethodId: paymentMethodId,
                paymentType: paymentType,
                discountId: appliedVoucher ? appliedVoucher.id : null,
                discountAmount: appliedVoucher ? appliedVoucher.value : 0,
                cartItems: cart
            }, function(res) {
                ModalPopup.hideLoading();
                
                if (res.success) {
                    ModalPopup.success('Thanh toán thành công!', 
                        `Mã đơn hàng: ${res.orderCode || 'N/A'}`,
                        function() {
                            // Clear cart
                            cart = [];
                            appliedVoucher = null;
                            renderCart();
                        });
                } else {
                    ModalPopup.error('Thanh toán thất bại!', res.message);
                }
            });
        }
    });
});
```

---

### 📦 **D. WAREHOUSE ROLE - PHIẾU XUẤT KHO**

#### **D1. Xác Nhận hoặc Hủy Yêu Cầu từ Chi Nhánh** ⭐⭐

**File:** `Areas/Warehouse/Views/Transfers/Detail.cshtml`

**Nhiệm vụ:**

- [ ] Hiển thị danh sách yêu cầu chuyển kho từ chi nhánh (status = 'requested')
- [ ] 2 buttons: "Chấp nhận" và "Từ chối"

**Action "Chấp nhận":**

```javascript
$('#btnApprove').click(function() {
    var transferId = $(this).data('id');
    ModalPopup.confirm({
        type: 'success',
        title: 'Chấp nhận yêu cầu',
        message: 'Xác nhận chuyển hàng cho chi nhánh?',
        confirmText: 'Chấp nhận',
        confirmClass: 'btn-success',
        onConfirm: function() {
            ModalPopup.showLoading('Đang xử lý...');
            $.post(`/Warehouse/Transfers/Approve/${transferId}`, function(res) {
                ModalPopup.hideLoading();
                if (res.success) {
                    Toast.success('Đã chấp nhận yêu cầu!');
                    // Update status → 'shipping'
                    // Deduct inventory from warehouse
                    location.reload();
                } else {
                    ModalPopup.error('Lỗi!', res.message);
                }
            });
        }
    });
});
```

**Action "Từ chối":**

```javascript
$('#btnReject').click(function() {
    var transferId = $(this).data('id');
    
    ModalPopup.prompt({
        title: 'Từ chối yêu cầu',
        message: 'Vui lòng nhập lý do từ chối:',
        inputType: 'textarea',
        confirmText: 'Từ chối',
        confirmClass: 'btn-danger',
        onConfirm: function(reason) {
            if (!reason) {
                ModalPopup.warning('Thiếu thông tin!', 'Vui lòng nhập lý do');
                return false;
            }
            
            ModalPopup.showLoading('Đang xử lý...');
            $.post(`/Warehouse/Transfers/Reject/${transferId}`, {
                reason: reason
            }, function(res) {
                ModalPopup.hideLoading();
                if (res.success) {
                    Toast.info('Đã từ chối yêu cầu');
                    location.reload();
                } else {
                    ModalPopup.error('Lỗi!', res.message);
                }
            });
        }
    });
});
```

---

## 📊 **TỔNG KẾT PHÂN CÔNG**

### **NGƯỜI 1** (Admin + Warehouse Nhập)

| # | Tính năng | Độ ưu tiên | Thời gian ước tính |
|---|-----------|------------|---------------------|
| 1 | Báo cáo với biểu đồ | ⭐⭐⭐ | 4-6h |
| 2 | Validate password | ⭐ | 1h |
| 3 | Xóa chi nhánh | ⭐⭐ | 2h |
| 4 | Xóa nhà cung cấp | ⭐⭐ | 1h |
| 5 | Xóa danh mục | ⭐⭐ | 1h |
| 6 | Xóa sản phẩm/variant | ⭐⭐ | 2h |
| 7 | Xóa mã giảm giá | ⭐ | 1h |
| 8 | Phiếu nhập: xử lý NCC không có SP | ⭐⭐ | 1h |
| 9 | Phiếu nhập: hoàn thành | ⭐⭐ | 2h |
| **TỔNG** | | | **15-17h** |

### **NGƯỜI 2** (Branch + Warehouse Xuất)

| # | Tính năng | Độ ưu tiên | Thời gian ước tính |
|---|-----------|------------|---------------------|
| 1 | Xác nhận nhận hàng | ⭐⭐ | 2-3h |
| 2 | Cập nhật trạng thái đơn | ⭐⭐ | 2-3h |
| 3 | POS: hoàn thiện checkout | ⭐⭐⭐ | 3-4h |
| 4 | Phiếu xuất: chấp nhận | ⭐⭐ | 2h |
| 5 | Phiếu xuất: từ chối | ⭐⭐ | 2h |
| **TỔNG** | | | **11-14h** |

---

## 🎯 **CHECKLIST HOÀN THÀNH**

### ✅ ModalPopup/Toast đã replace alert()

- [x] ModalPopup.success()
- [x] ModalPopup.error()
- [x] ModalPopup.warning()
- [x] ModalPopup.confirm()
- [x] ModalPopup.showLoading() / hideLoading()
- [x] Toast.success()
- [x] Toast.error()
- [x] Toast.info()

### 📁 Files cần tham khảo

- `Scripts/modal-popup.js` - ModalPopup implementation
- `Scripts/toast.js` - Toast notification
- `Areas/Branch/Views/POS/Index.cshtml` - Mẫu sử dụng ModalPopup
- `Areas/Warehouse/Views/Shipments/Create.cshtml` - Form validation

**BẮT ĐẦU TỪ TÍNH NĂNG ⭐⭐⭐ TRƯỚC!** 🚀
