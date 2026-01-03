// ========================================
// Script JavaScript: Tạo Orders Cho Chi Nhánh Tân Phú
// Chạy trong Browser Console
// ========================================

const CONFIG = {
    baseUrl: window.location.origin, // Tự động lấy URL hiện tại
    tanPhuBranchId: 2, // ID chi nhánh Tân Phú
    email: "customer@test.com",
    password: "123456",
    
    // Danh sách sản phẩm
    products: [
        { variantId: 1, quantity: 2 },
        { variantId: 2, quantity: 1 },
        { variantId: 3, quantity: 3 }
    ],
    
    // Thông tin giao hàng
    shipping: {
        name: "Nguyễn Văn A",
        phone: "0901234567",
        address: "123 Đường ABC, Phường Tân Phú, Quận 7, TP.HCM"
    },
    
    // Số lượng đơn hàng
    numberOfOrders: 5
};

// ========================================
// Helper Functions
// ========================================

async function login(email, password) {
    const response = await fetch(`${CONFIG.baseUrl}/Account/Login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
        credentials: 'include'
    });
    return response.ok;
}

async function addToCart(variantId, quantity, branchId) {
    const response = await fetch(`${CONFIG.baseUrl}/api/cart/add`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            ProductVariantId: variantId,
            Quantity: quantity,
            BranchId: branchId
        }),
        credentials: 'include'
    });
    const result = await response.json();
    return result.Success;
}

async function createOrder(branchId, shipping) {
    const response = await fetch(`${CONFIG.baseUrl}/api/orders`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            BranchId: branchId,
            ShippingRecipientName: shipping.name,
            ShippingRecipientPhone: shipping.phone,
            ShippingAddress: shipping.address,
            PaymentMethodId: 1, // COD
            ShippingFee: 30000,
            DiscountAmount: 0
        }),
        credentials: 'include'
    });
    const result = await response.json();
    return result;
}

async function clearCart() {
    await fetch(`${CONFIG.baseUrl}/api/cart/clear`, {
        method: 'POST',
        credentials: 'include'
    });
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// ========================================
// Main Function
// ========================================

async function createOrdersForTanPhu() {
    console.log('%c═══════════════════════════════════════════════', 'color: #00bcd4; font-weight: bold');
    console.log('%c  TẠO ĐƠN HÀNG CHO CHI NHÁNH TÂN PHÚ', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log('%c═══════════════════════════════════════════════', 'color: #00bcd4; font-weight: bold');
    console.log('');
    
    // 1. Đăng nhập
    console.log('%c[1] Đăng nhập...', 'color: #ff9800; font-weight: bold');
    const loginSuccess = await login(CONFIG.email, CONFIG.password);
    
    if (!loginSuccess) {
        console.log('%c✗ Đăng nhập thất bại!', 'color: #f44336; font-weight: bold');
        return;
    }
    console.log('%c✓ Đăng nhập thành công!', 'color: #4caf50; font-weight: bold');
    console.log('');
    await sleep(500);
    
    // 2. Tạo đơn hàng
    console.log(`%c[2] Tạo ${CONFIG.numberOfOrders} đơn hàng...`, 'color: #ff9800; font-weight: bold');
    console.log('');
    
    let successCount = 0;
    let failCount = 0;
    
    for (let i = 1; i <= CONFIG.numberOfOrders; i++) {
        console.log(`%c--- Đơn hàng #${i} ---`, 'color: #00bcd4');
        
        // Xóa giỏ cũ
        await clearCart();
        await sleep(300);
        
        // Thêm sản phẩm vào giỏ
        console.log('  → Thêm sản phẩm vào giỏ...');
        let addSuccess = true;
        
        for (const product of CONFIG.products) {
            const added = await addToCart(
                product.variantId,
                product.quantity,
                CONFIG.tanPhuBranchId
            );
            
            if (!added) {
                addSuccess = false;
                break;
            }
            await sleep(200);
        }
        
        if (!addSuccess) {
            console.log('%c  ✗ Lỗi thêm sản phẩm', 'color: #f44336');
            failCount++;
            continue;
        }
        console.log('%c  ✓ Đã thêm sản phẩm', 'color: #4caf50');
        
        // Tạo đơn hàng
        console.log('  → Tạo đơn hàng...');
        await sleep(300);
        
        const currentShipping = {
            name: `${CONFIG.shipping.name} - Đơn #${i}`,
            phone: CONFIG.shipping.phone,
            address: CONFIG.shipping.address
        };
        
        const orderResult = await createOrder(CONFIG.tanPhuBranchId, currentShipping);
        
        if (orderResult.Success) {
            console.log('%c  ✓ Tạo đơn thành công', 'color: #4caf50');
            console.log(`     Order Code: ${orderResult.Data.OrderCode}`);
            console.log(`     Order ID: ${orderResult.Data.Id}`);
            console.log(`     Total: ${orderResult.Data.TotalAmount.toLocaleString('vi-VN')} VND`);
            successCount++;
        } else {
            console.log('%c  ✗ Tạo đơn thất bại', 'color: #f44336');
            console.log(`     Lỗi: ${orderResult.Message}`);
            failCount++;
        }
        
        console.log('');
        await sleep(1000);
    }
    
    // 3. Tổng kết
    console.log('%c═══════════════════════════════════════════════', 'color: #00bcd4; font-weight: bold');
    console.log('%c  KẾT QUẢ TẠO ĐƠN HÀNG', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log('%c═══════════════════════════════════════════════', 'color: #00bcd4; font-weight: bold');
    console.log(`%c✓ Thành công: ${successCount} đơn hàng`, 'color: #4caf50; font-weight: bold');
    console.log(`%c✗ Thất bại: ${failCount} đơn hàng`, 'color: #f44336; font-weight: bold');
    console.log('');
    console.log(`Chi nhánh: Tân Phú (ID: ${CONFIG.tanPhuBranchId})`);
    console.log('%c═══════════════════════════════════════════════', 'color: #00bcd4; font-weight: bold');
}

// ========================================
// Chạy script
// ========================================
console.log('%c📦 Script đã load! Gõ: createOrdersForTanPhu()', 'color: #2196f3; font-size: 16px; font-weight: bold');
console.log('%cHoặc gõ: CONFIG để xem/sửa cấu hình', 'color: #757575');

// Auto-run (bỏ comment dòng dưới nếu muốn tự chạy)
// createOrdersForTanPhu();
