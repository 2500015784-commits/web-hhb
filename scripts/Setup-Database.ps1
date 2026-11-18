param(
    [string]$Server = '.\SQLEXPRESS',
    [string]$AdminUser = 'admin',
    [securestring]$AdminPassword,
    [switch]$ResetAdminPassword
)

$ErrorActionPreference = 'Stop'
if ($AdminUser -notmatch '^[a-zA-Z0-9_.-]{2,60}$') {
    throw 'Tên admin chỉ dùng 2–60 ký tự chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.'
}
if (-not $AdminPassword) {
    $AdminPassword = Read-Host 'Mật khẩu admin (tối thiểu 12 ký tự)' -AsSecureString
}

$secretPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword)
try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($secretPointer)
    if ($plain.Length -lt 12 -or $plain.Length -gt 256) { throw 'Mật khẩu phải có 12–256 ký tự.' }

    $salt = New-Object byte[] 16
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($salt) } finally { $rng.Dispose() }
    $derive = [Security.Cryptography.Rfc2898DeriveBytes]::new(
        $plain, $salt, 210000, [Security.Cryptography.HashAlgorithmName]::SHA256
    )
    try { $hash64 = [Convert]::ToBase64String($derive.GetBytes(32)) } finally { $derive.Dispose() }
    $salt64 = [Convert]::ToBase64String($salt)
} finally {
    $plain = $null
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer)
}

$source = Join-Path $PSScriptRoot '..\database\Initialize.sql'
& sqlcmd -S $Server -E -C -b -f 65001 -i $source
if ($LASTEXITCODE -ne 0) { throw 'Không thể khởi tạo cơ sở dữ liệu.' }

$command = if ($ResetAdminPassword) {
    "IF EXISTS(SELECT 1 FROM dbo.AdminUser WHERE UserName=N'$AdminUser') UPDATE dbo.AdminUser SET PasswordHash='$hash64', Salt='$salt64' WHERE UserName=N'$AdminUser'; ELSE INSERT dbo.AdminUser(UserName,PasswordHash,Salt) VALUES(N'$AdminUser','$hash64','$salt64');"
} else {
    "IF NOT EXISTS(SELECT 1 FROM dbo.AdminUser WHERE UserName=N'$AdminUser') INSERT dbo.AdminUser(UserName,PasswordHash,Salt) VALUES(N'$AdminUser','$hash64','$salt64');"
}
& sqlcmd -S $Server -d HhbStudio -E -C -b -Q $command
if ($LASTEXITCODE -ne 0) { throw 'Không thể ghi tài khoản admin.' }
Write-Host "HhbStudio đã sẵn sàng trên $Server. Tài khoản: $AdminUser."
