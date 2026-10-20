/* Stored procedures used by OrderReadRepository. */
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetTopCustomers
    @FromUtc datetime2,
    @ToUtc datetime2,
    @Top int = 5
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToUtc <= @FromUtc
        THROW 50001, 'The end date must be later than the start date.', 1;

    IF @Top < 1 OR @Top > 100
        THROW 50002, 'Top must be between 1 and 100.', 1;

    SELECT TOP (@Top)
        c.Id AS CustomerId,
        CONCAT(c.FirstName, N' ', c.LastName) AS FullName,
        c.Email,
        COUNT(o.Id) AS OrderCount,
        SUM(o.TotalAmount) AS TotalSpent
    FROM dbo.Customers AS c
    INNER JOIN dbo.Orders AS o ON o.CustomerId = c.Id
    WHERE o.OrderDateUtc >= @FromUtc
      AND o.OrderDateUtc < @ToUtc
      AND o.Status <> N'Cancelled'
    GROUP BY c.Id, c.FirstName, c.LastName, c.Email
    ORDER BY TotalSpent DESC, OrderCount DESC, c.Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_RestockProduct
    @ProductId int,
    @Quantity int
AS
BEGIN
    SET NOCOUNT OFF;

    IF @Quantity <= 0
        THROW 50003, 'Quantity must be positive.', 1;

    UPDATE dbo.Products
    SET StockQuantity = StockQuantity + @Quantity
    WHERE Id = @ProductId;
END;
GO
