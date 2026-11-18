using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Product.BLL.Interfaces.ProductServices;
using Product.DAL.Context;
using Product.DAL.Interfaces.ProductInterfaces;

namespace Product.BLL.Implementation.ProductService
{
    public class ProductImageService : IProductImageService
    {
        private readonly IProductRepository _repository;
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;
        private readonly ILogger<ProductImageService> _logger;

        private const long MaxFileSize = 5 * 1024 * 1024;

        public ProductImageService(
            IProductRepository repository,
            IWebHostEnvironment environment,
            AppDbContext context,
            ILogger<ProductImageService> logger)
        {
            _repository = repository;
            _environment = environment;
            _context = context;
            _logger = logger;
        }

        public async Task<bool> UploadAsync(int productId, IFormFile file)
        {
            _logger.LogInformation(
                "Image upload started for product {ProductId}", productId);

            var product = await _repository.GetByIdAsync(productId);

            if (product == null)
            {
                _logger.LogWarning(
                    "Image upload failed: product {ProductId} not found", productId);
                return false;
            }

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning(
                    "Image upload rejected for product {ProductId}: no file selected",
                    productId);
                throw new ArgumentException("Please select an image.");
            }

            if (file.Length > MaxFileSize)
            {
                _logger.LogWarning(
                    "Image upload rejected for product {ProductId}: file size {FileSizeBytes} exceeds limit {MaxFileSizeBytes}",
                    productId, file.Length, MaxFileSize);
                throw new ArgumentException("Maximum image size is 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning(
                    "Image upload rejected for product {ProductId}: extension {FileExtension} not allowed",
                    productId, extension);
                throw new ArgumentException("Only JPG, PNG and WebP images are allowed.");
            }

            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => string.Empty
            };

            // Verify that the file content matches the claimed image type.
            await using var input = file.OpenReadStream();
            var header = new byte[12];
            var bytesRead = await input.ReadAsync(header.AsMemory(0, header.Length));

            bool valid = extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3 &&
                    header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,

                ".png" => bytesRead >= 8 &&
                    header.Take(8).SequenceEqual(
                        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),

                ".webp" => bytesRead >= 12 &&
                    header.Take(4).SequenceEqual(
                        new byte[] { 0x52, 0x49, 0x46, 0x46 }) &&
                    header.Skip(8).Take(4).SequenceEqual(
                        new byte[] { 0x57, 0x45, 0x42, 0x50 }),

                _ => false
            };

            if (!valid)
            {
                // Possible spoofed file (e.g. a .exe renamed to .jpg), so worth watching
                _logger.LogWarning(
                    "Image upload rejected for product {ProductId}: content does not match extension {FileExtension}",
                    productId, extension);
                throw new ArgumentException("The file content is not a valid supported image.");
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
                "uploads", "products");

            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadFolder, fileName);

            await using (var output = new FileStream(fullPath, FileMode.CreateNew))
            {
                input.Position = 0;
                await input.CopyToAsync(output);
            }

            _logger.LogInformation(
                "Image file saved for product {ProductId}: {StoredFileName}, {FileSizeBytes} bytes",
                productId, fileName, file.Length);

            var oldPath = product.ImagePath;

            product.ImagePath = $"/uploads/products/{fileName}";

            try
            {
                // Save the new image path to the product record.
                var updated = await _repository.UpdateAsync(product);

                if (!updated)
                {
                    File.Delete(fullPath);
                    throw new InvalidOperationException("Could not update the product image.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to save image path for product {ProductId}; removing uploaded file {StoredFileName}",
                    productId, fileName);

                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                throw;
            }

            // Delete the previous image only if it belongs to our upload folder.
            if (!string.IsNullOrWhiteSpace(oldPath))
            {
                var oldFileName = Path.GetFileName(oldPath);
                var oldFullPath = Path.Combine(uploadFolder, oldFileName);

                if (oldPath.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(oldFullPath))
                {
                    File.Delete(oldFullPath);

                    _logger.LogInformation(
                        "Previous image {OldFileName} removed for product {ProductId}",
                        oldFileName, productId);
                }
            }

            _logger.LogInformation(
                "Image upload completed for product {ProductId}", productId);

            return true;
        }

        public async Task<(byte[] Content, string ContentType, string FileName)?>
            DownloadAsync(int productId)
        {
            _logger.LogInformation(
                "Image download requested for product {ProductId}", productId);

            var product = await _repository.GetByIdAsync(productId);

            if (product == null || string.IsNullOrWhiteSpace(product.ImagePath))
            {
                _logger.LogWarning(
                    "Image download failed: product {ProductId} not found or has no image",
                    productId);
                return null;
            }

            var fileName = Path.GetFileName(product.ImagePath);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };

            if (contentType == null ||
                !product.ImagePath.StartsWith(
                    "/uploads/products/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Image download blocked for product {ProductId}: unexpected image path or type",
                    productId);
                return null;
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
                "uploads", "products");

            var fullPath = Path.Combine(uploadFolder, fileName);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning(
                    "Image file {StoredFileName} missing on disk for product {ProductId}",
                    fileName, productId);
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(fullPath);

            _logger.LogInformation(
                "Image downloaded for product {ProductId}: {StoredFileName}, {SizeBytes} bytes",
                productId, fileName, bytes.Length);

            return (bytes, contentType, fileName);
        }
    }
}