namespace Product.BLL.Interfaces.ReportSevrice
{
    public interface IReportService
    {
        Task<byte[]> GeneratePdfAsync();

        Task<byte[]> GenerateExcelAsync();
    }
}
