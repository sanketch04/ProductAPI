
namespace Product.BLL.Interfaces
{
    public interface IReportService
    {
        Task<byte[]> GeneratePdfAsync();

        Task<byte[]> GenerateExcelAsync();
    }
}
