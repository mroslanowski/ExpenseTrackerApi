using SecureAuthApi.Models.Dtos;

namespace SecureAuthApi.Services.Interfaces
{
    public interface IReportService
    {
        // zwraca dane dzienne (lub miesięczne) do wykresu liniowego
        Task<IEnumerable<TimeSeriesPointDto>> GetTimeSeriesAsync(string userId,
            DateTime from, DateTime to);

        // zwraca dane do wykresu kołowego (podział po kategoriach)
        Task<IEnumerable<PieSliceDto>> GetPieChartAsync(string userId,
            DateTime from, DateTime to);

        // generuje Excel (xlsx) i zwraca binarnie
        Task<MemoryStream> GenerateExcelReportAsync(string userId,
            DateTime from, DateTime to);

        // generuje PDF i zwraca binarnie
        Task<MemoryStream> GeneratePdfReportAsync(string userId,
            DateTime from, DateTime to);
    }
}
