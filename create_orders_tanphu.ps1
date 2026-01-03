# Script Tao Orders Cho Chi Nhanh Tan Phu - VERSION 2

$baseUrl = "http://localhost:8080"
$email = "customer@test.com"
$password = "123456"
$tanPhuBranchId = 2
$numberOfOrders = 3

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "SCRIPT TAO DON HANG CHO CHI NHANH TAN PHU" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# NOTE: Script nay se tao data truc tiep qua JavaScript trong browser
# Vi ASP.NET MVC khong ho tro login qua WebRequest don gian

Write-Host "HUONG DAN SU DUNG:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Mo website: $baseUrl" -ForegroundColor White
Write-Host "2. Dang nhap voi email: $email va password: $password" -ForegroundColor White
Write-Host "3. Mo Developer Console (F12)" -ForegroundColor White
Write-Host "4. Copy va paste code JavaScript ben duoi vao Console:" -ForegroundColor White
Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan

$jsCode = @"
// ==========================================
// TAO DON HANG CHO CHI NHANH TAN PHU
// ==========================================

const CONFIG = {
    baseUrl: window.location.origin,
    tanPhuBranchId: $tanPhuBranchId,
    numberOfOrders: $numberOfOrders,
    products: [
        { variantId: 1, quantity: 2 },
        { variantId: 2, quantity: 1 },
        { variantId: 3, quantity: 3 }
    ],
    shipping: {
        name: "Nguyen Van A",
        phone: "0901234567",
        address: "123 Duong ABC, Phuong Tan Phu, Quan 7, TP.HCM"
    }
};

async function addToCart(variantId, quantity, branchId) {
    const response = await fetch(`+"'"+`${CONFIG.baseUrl}/api/cart/add`+"'"+`, {
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
    const response = await fetch(`+"'"+`${CONFIG.baseUrl}/api/orders`+"'"+`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            BranchId: branchId,
            ShippingRecipientName: shipping.name,
            ShippingRecipientPhone: shipping.phone,
            ShippingAddress: shipping.address,
            PaymentMethodId: 1,
            ShippingFee: 30000,
            DiscountAmount: 0
        }),
        credentials: 'include'
    });
    return await response.json();
}

async function clearCart() {
    await fetch(`+"'"+`${CONFIG.baseUrl}/api/cart/clear`+"'"+`, {
        method: 'POST',
        credentials: 'include'
    });
}

async function createOrdersForTanPhu() {
    console.log('%c===========================================', 'color: #00bcd4; font-weight: bold');
    console.log('%c  TAO DON HANG CHO TAN PHU', 'color: #00bcd4; font-weight: bold');
    console.log('%c===========================================', 'color: #00bcd4; font-weight: bold');
    
    let successCount = 0;
    let failCount = 0;
    
    for (let i = 1; i <= CONFIG.numberOfOrders; i++) {
        console.log(`+"'"+`\n--- Don hang #${i} ---`+"'"+`);
        
        await clearCart();
        await new Promise(r => setTimeout(r, 300));
        
        console.log('  Them san pham...');
        let addSuccess = true;
        for (const product of CONFIG.products) {
            const added = await addToCart(product.variantId, product.quantity, CONFIG.tanPhuBranchId);
            if (!added) {
                addSuccess = $false;
                break;
            }
            await new Promise(r => setTimeout(r, 200));
        }
        
        if (!addSuccess) {
            console.log('%c  LOI: Khong them duoc san pham', 'color: #f44336');
            failCount++;
            continue;
        }
        
        console.log('  Tao don hang...');
        const orderResult = await createOrder(CONFIG.tanPhuBranchId, {
            name: `+"'"+`${CONFIG.shipping.name} - Don #${i}`+"'"+`,
            phone: CONFIG.shipping.phone,
            address: CONFIG.shipping.address
        });
        
        if (orderResult.Success) {
            console.log('%c  THANH CONG', 'color: #4caf50');
            console.log(`+"'"+`    Order Code: ${orderResult.Data.OrderCode}`+"'"+`);
            console.log(`+"'"+`    Order ID: ${orderResult.Data.Id}`+"'"+`);
            console.log(`+"'"+`    Total: ${orderResult.Data.TotalAmount} VND`+"'"+`);
            success Count++;
        } else {
            console.log('%c  LOI:', 'color: #f44336', orderResult.Message);
            failCount++;
        }
        
        await new Promise(r => setTimeout(r, 1000));
    }
    
    console.log('%c===========================================', 'color: #00bcd4; font-weight: bold');
    console.log(`+"'"+`Thanh cong: ${successCount} don`+"'"+`);
    console.log(`+"'"+`That bai: ${failCount} don`+"'"+`);
    console.log('%c===========================================', 'color: #00bcd4; font-weight: bold');
}

// Chay script
createOrdersForTanPhu();
"@

Write-Host $jsCode -ForegroundColor Green
Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "Copy code phia tren va paste vao Console!" -ForegroundColor Yellow
Write-Host "=================================================" -ForegroundColor Cyan
