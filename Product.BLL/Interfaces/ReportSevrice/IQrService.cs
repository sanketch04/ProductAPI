namespace Product.BLL.Interfaces.ReportSevrice
{
    public interface IQrService
    {
        Task<byte[]?> GenerateProductQrAsync(int productId);
    }
}
