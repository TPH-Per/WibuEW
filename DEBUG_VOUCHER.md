# 🔍 HƯỚNG DẪN DEBUG VOUCHER

## Bước 1: Mở POS Page

1. Truy cập: `https://localhost:44377/Branch/POS`
2. Thêm ít nhất 1 sản phẩm vào giỏ hàng

## Bước 2: Mở Developer Console

1. Nhấn **F12** để mở DevTools
2. Chuyển sang tab **Console**
3. Clear console (click icon 🚫 hoặc Ctrl+L)

## Bước 3: Test Voucher

1. Trong phần "Mã giảm giá (Voucher)", nhập: `TEST2026`
2. Click nút "Áp dụng"
3. **QUAN SÁT CONSOLE** - sẽ thấy 2 dòng log:

### ✅ Trường hợp THÀNH CÔNG

```
[Voucher] Validating code: TEST2026
[Voucher] API Response: {success: true, voucher: {id: ..., code: "TEST2026", value: 50000, type: "fixed"}}
```

→ Alert success màu xanh xuất hiện
→ Hiển thị "Mã TEST2026: Giảm 50,000₫"

### ❌ Trường hợp LỖI

```
[Voucher] Validating code: TEST2026
[Voucher] Validation failed: Mã voucher không tồn tại hoặc đã hết hiệu lực
```

→ Dòng chữ đỏ hiển thị lỗi

### 🔴 Trường hợp AJAX ERROR

```
[Voucher] Validating code: TEST2026
[Voucher] AJAX Error: error Not Found <!DOCTYPE html>...
```

→ Endpoint không tồn tại hoặc routing sai

---

## Bước 4: Test Thủ Công API

### Option A: Dùng Console

```javascript
// Paste vào Console và Enter:
$.get('/Branch/POS/ValidateVoucher', { code: 'TEST2026' }, function(res) {
    console.log('Result:', res);
});
```

### Option B: Dùng Browser URL

Mở tab mới, truy cập:

```
https://localhost:44377/Branch/POS/ValidateVoucher?code=TEST2026
```

**Kết quả mong đợi:**

```json
{
    "success": true,
    "voucher": {
        "id": 10,
        "code": "TEST2026",
        "value": 50000,
        "type": "fixed"
    }
}
```

---

## Các Lỗi Thường Gặp

### 1. "Mã voucher không tồn tại hoặc đã hết hiệu lực"

**Nguyên nhân:**

- Database chưa có mã này
- is_active = 0 (đã tắt)

**Giải pháp:**

```sql
-- Check database
SELECT * FROM discounts WHERE code = 'TEST2026';

-- Nếu không có, chạy lại script:
sqlcmd -S localhost -E -i "create_test_vouchers.sql"
```

### 2. "Mã voucher đã hết hạn"

**Nguyên nhân:** `end_at < GETDATE()`

**Giải pháp:**

```sql
UPDATE discounts 
SET end_at = DATEADD(MONTH, 1, GETDATE())
WHERE code = 'TEST2026';
```

### 3. "Mã voucher chưa đến thời gian sử dụng"

**Nguyên nhân:** `start_at > GETDATE()`

**Giải pháp:**

```sql
UPDATE discounts 
SET start_at = GETDATE()
WHERE code = 'TEST2026';
```

### 4. "Mã voucher đã hết lượt sử dụng"

**Nguyên nhân:** `used_count >= max_uses`

**Giải pháp:**

```sql
UPDATE discounts 
SET used_count = 0 
WHERE code = 'TEST2026';
```

### 5. AJAX Error 404

**Nguyên nhân:** Routing không đúng

**Giải pháp:** Check RouteConfig hoặc Area registration

### 6. AJAX Error 500

**Nguyên nhân:** Lỗi server-side

**Giải pháp:**

- Check Output window trong Visual Studio
- Xem Exception details

---

## Debug Script Nhanh

Paste vào Console để test tất cả:

```javascript
// Test connection
console.log('Testing API...');
$.get('/Branch/POS/ValidateVoucher', { code: 'TEST2026' })
    .done(function(res) {
        console.log('✅ API Response:', res);
        if (res.success) {
            console.log('✅ Voucher hợp lệ!');
            console.log('   Code:', res.voucher.code);
            console.log('   Value:', res.voucher.value);
            console.log('   Type:', res.voucher.type);
        } else {
            console.error('❌ Validation failed:', res.message);
        }
    })
    .fail(function(xhr, status, error) {
        console.error('❌ AJAX Error:', status, error);
        console.error('Response:', xhr.responseText);
    });
```

---

## Checklist

- [ ] Database có mã TEST2026
- [ ] is_active = 1
- [ ] start_at <= NOW
- [ ] end_at > NOW  
- [ ] used_count < max_uses (hoặc max_uses = NULL)
- [ ] API endpoint hoạt động
- [ ] Console không có lỗi JavaScript
- [ ] Network tab thấy request thành công

**Sau khi làm xong các bước trên, chụp ảnh Console gửi cho tôi!** 📸
