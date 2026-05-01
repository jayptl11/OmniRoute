namespace OmniRoute.Application.Common.Interfaces;

public interface IReportExportService
{
    /// <summary>Exports a report object to Excel bytes. reportType: "overview" | "unitComparison" | "sales"</summary>
    byte[] ExportToExcel(string reportType, object data);
}
