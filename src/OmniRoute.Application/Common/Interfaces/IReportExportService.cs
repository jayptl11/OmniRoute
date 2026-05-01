using OmniRoute.Application.Features.Audit.DTOs;

namespace OmniRoute.Application.Common.Interfaces;

public interface IReportExportService
{
    /// <summary>Exports a report object to Excel bytes. reportType: "overview" | "unitComparison" | "sales"</summary>
    byte[] ExportToExcel(string reportType, object data);

    /// <summary>QT-13: Exports audit log entries to Excel bytes.</summary>
    byte[] ExportAuditToExcel(IEnumerable<AuditLogDto> items);

    /// <summary>BQL-06: Exports a report object to PDF bytes. reportType: "overview" | "unitComparison" | "sales"</summary>
    byte[] ExportToPdf(string reportType, object data);
}
