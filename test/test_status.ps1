$baseUrl = "http://localhost:8091"

function Test-Endpoint($path) {
    Write-Host "Testing $path ..." -NoNewline
    try {
        $start = Get-Date
        $res = Invoke-RestMethod -Uri "$baseUrl$path" -Method Get -TimeoutSec 2
        $elapsed = ((Get-Date) - $start).TotalMilliseconds
        Write-Host " [SUCCESS] ($($elapsed)ms)" -ForegroundColor Green
        return $res
    } catch {
        Write-Host " [FAILED] - $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

Write-Host "--- OpenClaw API Diagnostic ---" -ForegroundColor Cyan

# 1. 测试 Health (不走主线程)
$health = Test-Endpoint "/api/health"
if ($health) { 
    Write-Host "Server logic is ALIVE. Version: $($health.data.serverVersion)" -ForegroundColor Gray
}

Write-Host "--- Fetching Full Game Status ---" -ForegroundColor Cyan

try {
    # 请求 API
    $res = Invoke-RestMethod -Uri "$baseUrl/api/status" -Method Get -TimeoutSec 2
    
    if ($res.success) {
        # 将对象转化回漂亮的 JSON 字符串
        $jsonOutput = $res.data | ConvertTo-Json -Depth 5
        Write-Host "Success! Data received:" -ForegroundColor Green
        Write-Host $jsonOutput -ForegroundColor White
    } else {
        Write-Host "API returned error: $($res.error.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "Request Failed: $($_.Exception.Message)" -ForegroundColor Red
}