
using Product.BLL.Interfaces;
using Product.DAL.Interfaces;
using QRCoder;

namespace Product.BLL.Implementation
{
    public class QrService : IQrService
    {
        private readonly IProductRepository _repository;

        public QrService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<byte[]?> GenerateProductQrAsync(int productId)
        {
            var product = await _repository.GetByIdAsync(productId);

            if (product == null)
                return null;

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

            return qrCode.GetGraphic(20);
        }
    }
}
