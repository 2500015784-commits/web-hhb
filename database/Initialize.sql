USE [master];
GO
IF DB_ID(N'HhbStudio') IS NULL CREATE DATABASE [HhbStudio];
GO
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'HhbStudio' AND is_auto_close_on = 1)
    ALTER DATABASE [HhbStudio] SET AUTO_CLOSE OFF;
GO
USE [HhbStudio];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Category', N'U') IS NULL
CREATE TABLE dbo.Category (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CatName NVARCHAR(80) NOT NULL UNIQUE
);

IF OBJECT_ID(N'dbo.Product', N'U') IS NULL
CREATE TABLE dbo.Product (
    ProId INT IDENTITY(1,1) PRIMARY KEY,
    ProName NVARCHAR(120) NOT NULL,
    ProImage NVARCHAR(100) NOT NULL,
    ProPrice DECIMAL(18,2) NOT NULL CHECK (ProPrice > 0),
    CatId INT NOT NULL REFERENCES dbo.Category(Id),
    Description NVARCHAR(1000) NOT NULL,
    Stock INT NOT NULL CHECK (Stock >= 0),
    IsActive BIT NOT NULL DEFAULT (1),
    Featured BIT NOT NULL DEFAULT (0),
    Version ROWVERSION NOT NULL
);

IF OBJECT_ID(N'dbo.[Order]', N'U') IS NULL
CREATE TABLE dbo.[Order] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CheckoutToken UNIQUEIDENTIFIER NOT NULL UNIQUE,
    CreatedUtc DATETIME2 NOT NULL,
    CustomerName NVARCHAR(100) NOT NULL,
    CustomerPhone NVARCHAR(20) NOT NULL,
    CustomerEmail NVARCHAR(150),
    CustomerAddress NVARCHAR(250) NOT NULL,
    Note NVARCHAR(350),
    Total DECIMAL(18,2) NOT NULL CHECK (Total > 0),
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Pending','Processing','Completed','Cancelled')),
    Version ROWVERSION NOT NULL
);

IF OBJECT_ID(N'dbo.OrderDetail', N'U') IS NULL
CREATE TABLE dbo.OrderDetail (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL REFERENCES dbo.[Order](Id) ON DELETE CASCADE,
    ProductId INT NOT NULL REFERENCES dbo.Product(ProId),
    ProductName NVARCHAR(120) NOT NULL,
    Price DECIMAL(18,2) NOT NULL CHECK (Price > 0),
    Quantity INT NOT NULL CHECK (Quantity BETWEEN 1 AND 99)
);

IF OBJECT_ID(N'dbo.AdminUser', N'U') IS NULL
CREATE TABLE dbo.AdminUser (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(80) NOT NULL UNIQUE,
    PasswordHash VARCHAR(64) NOT NULL,
    Salt VARCHAR(32) NOT NULL
);

IF OBJECT_ID(N'dbo.Customer', N'U') IS NULL
CREATE TABLE dbo.Customer (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(64) NOT NULL,
    Salt VARCHAR(32) NOT NULL
);

IF COL_LENGTH(N'dbo.Order', N'CustomerId') IS NULL
    ALTER TABLE dbo.[Order] ADD CustomerId INT NULL
        CONSTRAINT FK_Order_Customer REFERENCES dbo.Customer(Id);

IF COL_LENGTH(N'dbo.Order', N'PaymentMethod') IS NULL
    ALTER TABLE dbo.[Order] ADD PaymentMethod VARCHAR(12) NOT NULL
        CONSTRAINT DF_Order_PaymentMethod DEFAULT ('COD') WITH VALUES
        CONSTRAINT CK_Order_PaymentMethod CHECK (PaymentMethod IN ('COD','Simulation'));

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.[Order]') AND name = N'IX_Order_CustomerId')
    EXEC(N'CREATE INDEX IX_Order_CustomerId ON dbo.[Order] (CustomerId, Id DESC)');

IF OBJECT_ID(N'dbo.SchemaSeed', N'U') IS NULL
CREATE TABLE dbo.SchemaSeed (Id NVARCHAR(60) PRIMARY KEY);

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaSeed WHERE Id = N'eshopper-catalogue-v1')
BEGIN
    INSERT dbo.Category (CatName) VALUES (N'Thời trang nữ'), (N'Thời trang nam'), (N'Giày dép'), (N'Túi xách');
    DECLARE @Women INT = (SELECT Id FROM dbo.Category WHERE CatName = N'Thời trang nữ');
    DECLARE @Men INT = (SELECT Id FROM dbo.Category WHERE CatName = N'Thời trang nam');
    DECLARE @Shoes INT = (SELECT Id FROM dbo.Category WHERE CatName = N'Giày dép');
    DECLARE @Bags INT = (SELECT Id FROM dbo.Category WHERE CatName = N'Túi xách');

    INSERT dbo.Product (ProName, ProImage, ProPrice, CatId, Description, Stock, Featured) VALUES
      (N'Chân váy chữ A', N'w1.jpg', 389000, @Women, N'Chân váy dáng chữ A, dễ phối cùng áo sơ mi hoặc áo thun.', 20, 1),
      (N'Áo sát nách phối ren', N'w2.jpg', 329000, @Women, N'Áo xanh đậm, có phần ren ở cổ và vai.', 18, 1),
      (N'Quần đen dáng ôm', N'w3.jpg', 549000, @Women, N'Quần đen dáng ôm, dễ phối cùng áo sáng và giày thấp.', 16, 1),
      (N'Quần shorts họa tiết', N'w4.jpg', 369000, @Women, N'Quần shorts họa tiết đen trắng cho những ngày cần trang phục thoải mái.', 22, 0),
      (N'Sơ mi xanh cổ điển', N'm1.jpg', 459000, @Men, N'Sơ mi xanh đậm, có thể mặc riêng hoặc khoác ngoài áo thun.', 26, 1),
      (N'Áo hoodie xám in chữ', N'm2.jpg', 489000, @Men, N'Áo nỉ xám có mũ, túi trước và họa tiết chữ.', 18, 0),
      (N'Quần thể thao sọc', N'm3.jpg', 419000, @Men, N'Quần thể thao dáng dài với chi tiết sọc tương phản.', 12, 0),
      (N'Giày tây đen buộc dây', N's1.jpg', 619000, @Shoes, N'Giày đen kiểu cổ điển, đi cùng quần âu hoặc jeans tối màu.', 14, 1),
      (N'Giày thể thao đen trắng', N's2.jpg', 579000, @Shoes, N'Giày thể thao buộc dây, thân đen với đế trắng.', 11, 0),
      (N'Giày vải dây buộc', N's3.jpg', 529000, @Shoes, N'Giày vải đen trắng kiểu cổ thấp.', 14, 0),
      (N'Túi công sở đen', N'b1.jpg', 479000, @Bags, N'Túi đen dáng chữ nhật, có quai xách và dây đeo vai.', 16, 1),
      (N'Ba lô Puma xanh đen', N'b2.jpg', 449000, @Bags, N'Ba lô phối xanh đen, có quai đeo vai và ngăn kéo khóa.', 17, 0);

    INSERT dbo.SchemaSeed (Id) VALUES (N'eshopper-catalogue-v1');
END;

COMMIT TRANSACTION;
GO
