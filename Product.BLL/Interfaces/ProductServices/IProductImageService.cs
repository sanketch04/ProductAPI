using Microsoft.AspNetCore.Http;

namespace Product.BLL.Interfaces.ProductServices
{
    public interface IProductImageService
    {
        Task<bool> UploadAsync(int productId, IFormFile file);

        Task<(byte[] Content, string ContentType, string FileName)?>
            DownloadAsync(int productId);
    }
}
