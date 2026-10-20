/*
    Reference schema for readers who prefer SSMS/T-SQL first.

    The supported application path is EF Core migrations:
        dotnet ef database update --project src/SqlServerEfCorePro.Data

    This script mirrors the EF model. It is intentionally idempotent enough
    for a disposable local database: if the tables already exist, skip it.
*/
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        FirstName nvarchar(80) NOT NULL,
        LastName nvarchar(80) NOT NULL,
        Email nvarchar(256) NOT NULL,
        CreatedUtc datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_Customers_Email ON dbo.Customers(Email);
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Sku nvarchar(40) NOT NULL,
        Name nvarchar(160) NOT NULL,
        Category nvarchar(80) NOT NULL,
        UnitPrice decimal(18,2) NOT NULL,
        StockQuantity int NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CONSTRAINT CK_Products_StockQuantity_NonNegative CHECK (StockQuantity >= 0)
    );
    CREATE UNIQUE INDEX UX_Products_Sku ON dbo.Products(Sku);
    CREATE INDEX IX_Products_Category_Name ON dbo.Products(Category, Name);
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        CustomerId int NOT NULL,
        OrderDateUtc datetime2 NOT NULL,
        Status nvarchar(20) NOT NULL,
        TotalAmount decimal(18,2) NOT NULL,
        CONSTRAINT FK_Orders_Customers_CustomerId
            FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
    );
    CREATE INDEX IX_Orders_CustomerId_OrderDateUtc
        ON dbo.Orders(CustomerId, OrderDateUtc);
    CREATE INDEX IX_Orders_Status_OrderDateUtc
        ON dbo.Orders(Status, OrderDateUtc);
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
        OrderId int NOT NULL,
        ProductId int NOT NULL,
        Quantity int NOT NULL,
        UnitPrice decimal(18,2) NOT NULL,
        LineTotal decimal(18,2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders_OrderId
            FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderItems_Products_ProductId
            FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT CK_OrderItems_Quantity_Positive CHECK (Quantity > 0)
    );
    CREATE UNIQUE INDEX UX_OrderItems_OrderId_ProductId
        ON dbo.OrderItems(OrderId, ProductId);
END;
GO
