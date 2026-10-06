
namespace Product.BLL.Interfaces
{
    public interface IQrService
    {
        Task<byte[]?> GenerateProductQrAsync(int productId);
    }
}
