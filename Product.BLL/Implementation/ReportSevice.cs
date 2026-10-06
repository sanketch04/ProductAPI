
using ClosedXML.Excel;
using Product.BLL.Interfaces;
using Product.DAL.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Product.BLL.Implementation
{
    public class ReportService : IReportService
    {
        private readonly IProductRepository _repository;

        public ReportService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<byte[]> GenerateExcelAsync()
        {
            Console.WriteLine("EXCEL 1: Starting report generation");

            var products = await _repository.GetAllAsync();

            Console.WriteLine($"EXCEL 2: Retrieved {products.Count} products");

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

            Console.WriteLine("EXCEL 3: Creating Excel file");

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var result = stream.ToArray();

            Console.WriteLine($"EXCEL 4: Generated {result.Length} bytes");

            return result;
        }

        public async Task<byte[]> GeneratePdfAsync()
        {
            Console.WriteLine("PDF 1: Starting report generation");

            var products = await _repository.GetAllAsync();

            Console.WriteLine($"PDF 2: Retrieved {products.Count} products");

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

            Console.WriteLine("PDF 3: Generating PDF bytes");

            var result = document.GeneratePdf();

            Console.WriteLine($"PDF 4: Generated {result.Length} bytes");

            return result;
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