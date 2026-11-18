using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Product.BLL.Interfaces.ReportSevrice;
using Product.DAL.Interfaces.ProductInterfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;

namespace Product.BLL.Implementation.ReportService_impl
{
    public class ReportService : IReportService
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IProductRepository repository,
            ILogger<ReportService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<byte[]> GenerateExcelAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting {ReportType} report generation", "Excel");

            try
            {
                var products = await _repository.GetAllAsync();

                _logger.LogInformation(
                    "Retrieved {ProductCount} products for {ReportType} report",
                    products.Count, "Excel");

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("Products");

                sheet.Cell(1, 1).Value = "Product ID";
                sheet.Cell(1, 2).Value = "Name";
                sheet.Cell(1, 3).Value = "Description";
                sheet.Cell(1, 4).Value = "Price";
                sheet.Cell(1, 5).Value = "Stock";
                sheet.Cell(1, 6).Value = "Created At";

                var header = sheet.Range(1, 1, 1, 6);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightBlue;

                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    int row = i + 2;

                    sheet.Cell(row, 1).Value = product.Id;
                    sheet.Cell(row, 2).Value = product.Name;
                    sheet.Cell(row, 3).Value = product.Description ?? "";
                    sheet.Cell(row, 4).Value = product.Price;
                    sheet.Cell(row, 5).Value = product.Stock;
                    sheet.Cell(row, 6).Value = product.CreatedAt;
                }

                sheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                var result = stream.ToArray();

                _logger.LogInformation(
                    "{ReportType} report generated: {SizeBytes} bytes, {ProductCount} products, {ElapsedMs} ms",
                    "Excel", result.Length, products.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{ReportType} report generation failed after {ElapsedMs} ms",
                    "Excel", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<byte[]> GeneratePdfAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting {ReportType} report generation", "PDF");

            try
            {
                var products = await _repository.GetAllAsync();

                _logger.LogInformation(
                    "Retrieved {ProductCount} products for {ReportType} report",
                    products.Count, "PDF");

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .Text("Product Inventory Report")
                            .FontSize(20)
                            .Bold();

                        page.Content()
                            .PaddingVertical(15)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(45);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("ID");
                                    header.Cell().Element(HeaderCell).Text("Name");
                                    header.Cell().Element(HeaderCell).Text("Description");
                                    header.Cell().Element(HeaderCell).Text("Price");
                                    header.Cell().Element(HeaderCell).Text("Stock");
                                });

                                foreach (var product in products)
                                {
                                    table.Cell().Element(BodyCell)
                                        .Text(product.Id.ToString());

                                    table.Cell().Element(BodyCell)
                                        .Text(product.Name ?? "");

                                    table.Cell().Element(BodyCell)
                                        .Text(product.Description ?? "");

                                    table.Cell().Element(BodyCell)
                                        .Text(product.Price.ToString("0.00"));

                                    table.Cell().Element(BodyCell)
                                        .Text(product.Stock.ToString());
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Generated on ");
                                text.Span(
                                    DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                            });
                    });
                });

                var result = document.GeneratePdf();

                _logger.LogInformation(
                    "{ReportType} report generated: {SizeBytes} bytes, {ProductCount} products, {ElapsedMs} ms",
                    "PDF", result.Length, products.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{ReportType} report generation failed after {ElapsedMs} ms",
                    "PDF", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .Padding(5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Medium)
                .DefaultTextStyle(x => x.Bold());
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .Padding(5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3);
        }
    }
}