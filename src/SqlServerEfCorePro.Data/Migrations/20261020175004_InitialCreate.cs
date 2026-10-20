using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqlServerEfCorePro.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "dbo",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Email",
                schema: "dbo",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                schema: "dbo",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "UX_OrderItems_OrderId_ProductId",
                schema: "dbo",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_OrderDateUtc",
                schema: "dbo",
                table: "Orders",
                columns: new[] { "CustomerId", "OrderDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_OrderDateUtc",
                schema: "dbo",
                table: "Orders",
                columns: new[] { "Status", "OrderDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Name",
                schema: "dbo",
                table: "Products",
                columns: new[] { "Category", "Name" });

            migrationBuilder.CreateIndex(
                name: "UX_Products_Sku",
                schema: "dbo",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.Sql(
                "ALTER TABLE dbo.Products ADD CONSTRAINT CK_Products_StockQuantity_NonNegative CHECK (StockQuantity >= 0);");
            migrationBuilder.Sql(
                "ALTER TABLE dbo.OrderItems ADD CONSTRAINT CK_OrderItems_Quantity_Positive CHECK (Quantity > 0);");

            // The same procedure text is kept in database/02-stored-procedures.sql
            // so it can be read and run directly in SSMS/Azure Data Studio.
            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_RestockProduct;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetTopCustomers;");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "dbo");
        }
    }
}
