param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5179
)

$ErrorActionPreference = 'Stop'
$webRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\HhbShop')).Path
$iis = Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'
if (-not (Test-Path -LiteralPath $iis)) {
    $iis = Join-Path ${env:ProgramFiles(x86)} 'IIS Express\iisexpress.exe'
}
if (-not (Test-Path -LiteralPath $iis)) {
    throw 'Cần cài IIS Express để chạy dự án.'
}

& dotnet build (Join-Path $webRoot 'HhbShop.csproj')
if ($LASTEXITCODE -ne 0) { throw 'Build thất bại.' }

Write-Host "Mở http://localhost:$Port/ trong trình duyệt. Nhấn Ctrl+C để tắt IIS Express."
& $iis "/path:$webRoot" "/port:$Port" /systray:false
