
using Microsoft.AspNetCore.Mvc;
using Product.BLL.Interfaces;

namespace ProductAPI.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("pdf")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> DownloadPdf()
        {
            try
            {
                var bytes = await _reportService.GeneratePdfAsync();

                SetDownloadHeaders();

                return File(
                    bytes,
                    "application/pdf",
                    $"products-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                    enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Problem(
                    title: "PDF report generation failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        }

        [HttpGet("excel")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> DownloadExcel()
        {
            Console.WriteLine("EXCEL A: Controller started");

            try
            {
                var bytes = await _reportService.GenerateExcelAsync();

                Console.WriteLine($"EXCEL B: Received {bytes.Length} bytes");

                SetDownloadHeaders();

                var response = File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"products-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx",
                    enableRangeProcessing: true);

                Console.WriteLine("EXCEL C: Returning file response");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEL ERROR: {ex}");
                return Problem(
                    title: "Excel report generation failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        }

        private void SetDownloadHeaders()
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers.XContentTypeOptions = "nosniff";
        }
    }
}
