using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Product.BLL.Interfaces;
using Product.DAL.Context;
using Product.DAL.Interfaces;

namespace Product.BLL.Implementation
{
    public class ProductImageService : IProductImageService
    {
        private readonly IProductRepository _repository;
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;

        private const long MaxFileSize = 5 * 1024 * 1024;

        public ProductImageService(
            IProductRepository repository,
            IWebHostEnvironment environment,
            AppDbContext context)
        {
            _repository = repository;
            _environment = environment;
            _context = context;
        }

        public async Task<bool> UploadAsync(int productId, IFormFile file)
        {
            var product = await _repository.GetByIdAsync(productId);

            if (product == null)
                return false;

            if (file == null || file.Length == 0)
                throw new ArgumentException("Please select an image.");

            if (file.Length > MaxFileSize)
                throw new ArgumentException("Maximum image size is 5 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Only JPG, PNG and WebP images are allowed.");

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
                throw new ArgumentException("The file content is not a valid supported image.");

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
            catch
            {
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
                }
            }

            return true;
        }

        public async Task<(byte[] Content, string ContentType, string FileName)?>
            DownloadAsync(int productId)
        {
            var product = await _repository.GetByIdAsync(productId);

            if (product == null || string.IsNullOrWhiteSpace(product.ImagePath))
                return null;

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
                return null;
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
                "uploads", "products");

            var fullPath = Path.Combine(uploadFolder, fileName);

            if (!File.Exists(fullPath))
                return null;

            var bytes = await File.ReadAllBytesAsync(fullPath);

            return (bytes, contentType, fileName);
        }
    }
}
