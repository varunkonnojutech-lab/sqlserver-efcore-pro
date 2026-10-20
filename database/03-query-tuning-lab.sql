/*
    QUERY TUNING LAB
    ================
    Run this in SQL Server Management Studio against CommercePro after:
      1. docker compose up -d sqlserver
      2. dotnet run --project src/SqlServerEfCorePro.Demo -- seed

    Turn on Include Actual Execution Plan (Ctrl+M), then run each section.
    The seed data is deliberately small enough for a laptop. On a tiny table
    SQL Server may correctly choose a scan because a scan is cheaper; the point
    is to learn how to read the plan, not to force an index seek at all costs.
    For a larger experiment, clone the seed orders in your own sandbox and
    compare logical reads before/after adding the covering index below.
*/

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

/* -------------------------------------------------------------------------
   1) WEAK SHAPE: function on the filtered column + SELECT *
   CONVERT makes the predicate non-SARGable: SQL Server cannot turn the date
   range into a clean index range as easily, and SELECT * drags every column
   through the plan. Watch for scans and a high logical-read count.
-------------------------------------------------------------------------- */
SELECT *
FROM dbo.Orders
WHERE CONVERT(date, OrderDateUtc) = '2026-10-03';
GO

/* -------------------------------------------------------------------------
   2) TUNED SHAPE: half-open date range + only the columns the UI needs
   A half-open range [start, nextStart) is exact for datetime2 and SARGable.
   IX_Orders_CustomerId_OrderDateUtc supports customer + date ordering.
-------------------------------------------------------------------------- */
SELECT Id, CustomerId, OrderDateUtc, Status, TotalAmount
FROM dbo.Orders
WHERE CustomerId = 1
  AND OrderDateUtc >= '2026-01-01'
  AND OrderDateUtc <  '2027-01-01'
ORDER BY OrderDateUtc DESC;
GO

/* -------------------------------------------------------------------------
   3) OPTIONAL COVERING INDEX
   The demo's built-in index finds the rows. This wider lab index also carries
   Status and TotalAmount, so the query above can be answered from the index
   alone and avoid key lookups. More coverage is not free: every insert/update
   now maintains another index, so measure before adopting it in production.
-------------------------------------------------------------------------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Orders_CustomerId_OrderDateUtc_Covering'
      AND object_id = OBJECT_ID(N'dbo.Orders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_OrderDateUtc_Covering
        ON dbo.Orders(CustomerId, OrderDateUtc DESC)
        INCLUDE (Status, TotalAmount);
END;
GO

-- Re-run section 2 and compare: operator (seek vs scan), key lookups,
-- logical reads in the Messages tab, and estimated vs actual rows.
SELECT Id, CustomerId, OrderDateUtc, Status, TotalAmount
FROM dbo.Orders
WHERE CustomerId = 1
  AND OrderDateUtc >= '2026-01-01'
  AND OrderDateUtc <  '2027-01-01'
ORDER BY OrderDateUtc DESC;
GO

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO

/*
    HOW TO READ THE BEFORE/AFTER PLAN

    Before (weak shape):
      * Scan or an index choice constrained by CONVERT(date, ...)
      * SELECT * can introduce lookups when the chosen index lacks columns
      * Sort work may appear because the access path does not provide order

    After (range + covering index):
      * Index Seek on CustomerId + OrderDateUtc range
      * No Key Lookup when every requested column is in the index/INCLUDE list
      * Ordered index scan/seek output reduces or removes the Sort operator

    Do not memorize "seek good, scan bad." A scan over 20 rows can be the
    cheapest correct plan. Senior tuning starts with workload, cardinality,
    logical reads, and the actual execution plan — then changes one thing.
*/
