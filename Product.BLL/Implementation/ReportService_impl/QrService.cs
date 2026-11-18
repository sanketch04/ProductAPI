using Microsoft.Extensions.Logging;
using Product.BLL.Interfaces.ReportSevrice;
using Product.DAL.Interfaces.ProductInterfaces;
using QRCoder;

namespace Product.BLL.Implementation.ReportService_impl
{
    public class QrService : IQrService
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<QrService> _logger;

        public QrService(
            IProductRepository repository,
            ILogger<QrService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<byte[]?> GenerateProductQrAsync(int productId)
        {
            _logger.LogInformation(
                "QR code requested for product {ProductId}", productId);

            try
            {
                var product = await _repository.GetByIdAsync(productId);

                if (product == null)
                {
                    _logger.LogWarning(
                        "QR generation failed: product {ProductId} not found", productId);
                    return null;
                }

                var productData =
                    $"Product ID: {product.Id}\n" +
                    $"Name: {product.Name}\n" +
                    $"Price: {product.Price:0.00}\n" +
                    $"Stock: {product.Stock}";

                using var generator = new QRCodeGenerator();

                using var qrData = generator.CreateQrCode(
                    productData,
                    QRCodeGenerator.ECCLevel.Q);

                var qrCode = new PngByteQRCode(qrData);

                var result = qrCode.GetGraphic(20);

                _logger.LogInformation(
                    "QR code generated for product {ProductId}: {SizeBytes} bytes",
                    productId, result.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "QR generation failed for product {ProductId}", productId);
                throw;
            }
        }
    }
}