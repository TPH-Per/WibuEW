// =========================================================
// SCRIPT: TAO USER MOI VA DAT 10 DON HANG CHO TAN PHU
// =========================================================

const CONFIG = {
    baseUrl: window.location.origin,
    tanPhuBranchId: 2,
    numberOfOrders: 10,

    // Thong tin user moi
    newUser: {
        username: `testuser_${Date.now()}`,
        email: `testuser_${Date.now()}@test.com`,
        password: "123456",
        fullName: "Nguyen Van Test",
        phone: "0909123456"
    },

    // San pham dat mua
    products: [
        { variantId: 1, quantity: 2 },
        { variantId: 2, quantity: 1 },
        { variantId: 3, quantity: 3 }
    ],

    // Thong tin giao hang
    shipping: {
        name: "Nguyen Van Test",
        phone: "0909123456",
        address: "123 Duong ABC, Phuong Tan Phu, Quan 7, TP.HCM"
    }
};

// =========================================================
// HELPER FUNCTIONS
// =========================================================

async function registerUser(userInfo) {
    try {
        // Get antiforgery token from register page
        const registerPageResponse = await fetch(`${CONFIG.baseUrl}/Account/Register`, {
            method: 'GET',
            credentials: 'include'
        });
        const registerPageHtml = await registerPageResponse.text();

        // Extract antiforgery token
        const tokenMatch = registerPageHtml.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
        if (!tokenMatch) {
            throw new Error('Khong tim thay antiforgery token');
        }
        const token = tokenMatch[1];

        // Prepare form data
        const formData = new FormData();
        formData.append('__RequestVerificationToken', token);
        formData.append('Username', userInfo.username);
        formData.append('FullName', userInfo.fullName);
        formData.append('Email', userInfo.email);
        formData.append('PhoneNumber', userInfo.phone);
        formData.append('Password', userInfo.password);
        formData.append('ConfirmPassword', userInfo.password);
        formData.append('AgreeTerms', 'true');

        // Submit registration
        const response = await fetch(`${CONFIG.baseUrl}/Account/Register`, {
            method: 'POST',
            body: formData,
            credentials: 'include',
            redirect: 'manual'
        });

        // Check if redirected to login (success)
        if (response.status === 0 || response.type === 'opaqueredirect' || response.redirected) {
            return { success: true, message: 'Dang ky thanh cong' };
        }

        const result = await response.text();
        if (result.includes('thanh cong') || result.includes('success')) {
            return { success: true, message: 'Dang ky thanh cong' };
        }

        return { success: false, message: 'Dang ky that bai' };
    } catch (error) {
        return { success: false, message: error.message };
    }
}

async function loginUser(email, password) {
    try {
        // Get login page to get antiforgery token
        const loginPageResponse = await fetch(`${CONFIG.baseUrl}/Account/Login`, {
            method: 'GET',
            credentials: 'include'
        });
        const loginPageHtml = await loginPageResponse.text();

        // Extract antiforgery token if exists (may not be required for login)
        const tokenMatch = loginPageHtml.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);

        // Prepare form data
        const formData = new FormData();
        if (tokenMatch) {
            formData.append('__RequestVerificationToken', tokenMatch[1]);
        }
        formData.append('Email', email);
        formData.append('Password', password);
        formData.append('RememberMe', 'false');

        // Submit login
        const response = await fetch(`${CONFIG.baseUrl}/Account/Login`, {
            method: 'POST',
            body: formData,
            credentials: 'include',
            redirect: 'manual'
        });

        // Check if successful (redirected)
        if (response.status === 0 || response.type === 'opaqueredirect' || response.redirected || response.status === 302) {
            return { success: true, message: 'Dang nhap thanh cong' };
        }

        return { success: false, message: 'Dang nhap that bai' };
    } catch (error) {
        return { success: false, message: error.message };
    }
}

async function addToCart(variantId, quantity, branchId) {
    try {
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
    } catch (error) {
        console.error('Add to cart error:', error);
        return false;
    }
}

async function createOrder(branchId, shipping, orderIndex) {
    try {
        const response = await fetch(`${CONFIG.baseUrl}/api/orders`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                BranchId: branchId,
                ShippingRecipientName: `${shipping.name} - Don #${orderIndex}`,
                ShippingRecipientPhone: shipping.phone,
                ShippingAddress: shipping.address,
                PaymentMethodId: 1,
                ShippingFee: 30000,
                DiscountAmount: 0
            }),
            credentials: 'include'
        });
        return await response.json();
    } catch (error) {
        console.error('Create order error:', error);
        return { Success: false, Message: error.message };
    }
}

async function clearCart() {
    try {
        await fetch(`${CONFIG.baseUrl}/api/cart/clear`, {
            method: 'POST',
            credentials: 'include'
        });
    } catch (error) {
        console.error('Clear cart error:', error);
    }
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// =========================================================
// MAIN FUNCTION
// =========================================================

async function createUserAndOrders() {
    console.log('%c========================================', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log('%c  TAO USER MOI VA DAT HANG TAN PHU', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log('%c========================================', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log('');

    // Step 1: Register new user
    console.log('%c[1] DANG KY USER MOI...', 'color: #ff9800; font-weight: bold');
    console.log(`    Email: ${CONFIG.newUser.email}`);
    console.log(`    Username: ${CONFIG.newUser.username}`);

    const registerResult = await registerUser(CONFIG.newUser);
    if (!registerResult.success) {
        console.log('%c    X LOI: ' + registerResult.message, 'color: #f44336; font-weight: bold');
        return;
    }
    console.log('%c    THANH CONG!', 'color: #4caf50; font-weight: bold');
    await sleep(1000);

    // Step 2: Login with new user
    console.log('');
    console.log('%c[2] DANG NHAP VOI USER MOI...', 'color: #ff9800; font-weight: bold');

    const loginResult = await loginUser(CONFIG.newUser.email, CONFIG.newUser.password);
    if (!loginResult.success) {
        console.log('%c    X LOI: ' + loginResult.message, 'color: #f44336; font-weight: bold');
        return;
    }
    console.log('%c    THANH CONG!', 'color: #4caf50; font-weight: bold');
    await sleep(1000);

    // Step 3: Create 10 orders
    console.log('');
    console.log(`%c[3] TAO ${CONFIG.numberOfOrders} DON HANG CHO TAN PHU...`, 'color: #ff9800; font-weight: bold');
    console.log('');

    let successCount = 0;
    let failCount = 0;

    for (let i = 1; i <= CONFIG.numberOfOrders; i++) {
        console.log(`%c--- Don hang #${i} ---`, 'color: #00bcd4');

        // Clear cart
        await clearCart();
        await sleep(200);

        // Add products to cart
        console.log('  Them san pham vao gio...');
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
            await sleep(100);
        }

        if (!addSuccess) {
            console.log('%c  X Loi them san pham', 'color: #f44336');
            failCount++;
            continue;
        }
        console.log('%c  OK', 'color: #4caf50');

        // Create order
        console.log('  Tao don hang...');
        await sleep(200);

        const orderResult = await createOrder(
            CONFIG.tanPhuBranchId,
            CONFIG.shipping,
            i
        );

        if (orderResult.Success) {
            console.log('%c  THANH CONG', 'color: #4caf50; font-weight: bold');
            console.log(`    Order Code: ${orderResult.Data.OrderCode}`);
            console.log(`    Order ID: ${orderResult.Data.Id}`);
            console.log(`    Total: ${orderResult.Data.TotalAmount.toLocaleString('vi-VN')} VND`);
            successCount++;
        } else {
            console.log('%c  LOI: ' + orderResult.Message, 'color: #f44336');
            failCount++;
        }

        console.log('');
        await sleep(800);
    }

    // Summary
    console.log('%c========================================', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log('%c  KET QUA', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log('%c========================================', 'color: #00bcd4; font-weight: bold; font-size: 14px');
    console.log(`%cUser moi: ${CONFIG.newUser.email}`, 'color: #2196f3; font-weight: bold');
    console.log(`%cPassword: ${CONFIG.newUser.password}`, 'color: #2196f3; font-weight: bold');
    console.log('');
    console.log(`%cThanh cong: ${successCount} don hang`, 'color: #4caf50; font-weight: bold');
    console.log(`%cThat bai: ${failCount} don hang`, 'color: #f44336; font-weight: bold');
    console.log('');
    console.log(`Chi nhanh: Tan Phu(ID: ${CONFIG.tanPhuBranchId})`);
    console.log('%c========================================', 'color: #00bcd4; font-weight: bold; font-size: 14px');
}

// =========================================================
// RUN
// =========================================================
console.log('%cScript da load! Goi: createUserAndOrders()', 'color: #2196f3; font-size: 16px; font-weight: bold');
console.log('%cHoac sua doi CONFIG truoc khi chay', 'color: #757575');

// Auto-run (uncomment to auto-execute)
// createUserAndOrders();
