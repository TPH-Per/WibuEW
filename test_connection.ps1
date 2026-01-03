# Test connection
$baseUrl = "http://localhost:8080"

Write-Host "Testing connection to $baseUrl..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/" -Method GET -UseBasicParsing
    Write-Host "SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "BaseUri: $($response.BaseResponse.ResponseUri.AbsoluteUri)" -ForegroundColor Cyan
}
catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTesting API endpoint..." -ForegroundColor Yellow
try {
    $testBody = '{"email":"customer@test.com","password":"123456"}' 
    $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/login" -Method POST -ContentType "application/json" -Body $testBody -UseBasicParsing
    Write-Host "API SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
}
catch {
    Write-Host "API FAILED: $($_.Exception.Message)" -ForegroundColor Red
}
