using ClosedXML.Excel;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Features.Dashboard.DTOs;

namespace OmniRoute.Infrastructure.Services;

public sealed class ReportExportService : IReportExportService
{
    public byte[] ExportToExcel(string reportType, object data)
    {
        using var workbook = new XLWorkbook();

        switch (reportType)
        {
            case "overview" when data is DashboardOverviewDto overview:
                BuildOverviewSheet(workbook, overview);
                break;
            case "unitComparison" when data is UnitComparisonDto comparison:
                BuildUnitComparisonSheet(workbook, comparison);
                break;
            case "sales" when data is SalesReportDto sales:
                BuildSalesReportSheet(workbook, sales);
                break;
            default:
                var ws = workbook.Worksheets.Add("Report");
                ws.Cell(1, 1).Value = "No data";
                break;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildOverviewSheet(XLWorkbook workbook, DashboardOverviewDto data)
    {
        var ws = workbook.Worksheets.Add("Overview");

        ws.Cell(1, 1).Value = "Dashboard Overview Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = $"Period: {data.Period} ({data.PeriodStart:yyyy-MM-dd} - {data.PeriodEnd:yyyy-MM-dd})";
        ws.Cell(3, 1).Value = $"Generated At: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}";

        // KPI Cards
        ws.Cell(5, 1).Value = "KPI";
        ws.Cell(5, 2).Value = "Value";
        ws.Range(5, 1, 5, 2).Style.Font.Bold = true;
        ws.Range(5, 1, 5, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;

        var kpiRows = new[]
        {
            ("Total Leads Today", (object)data.KpiCards.TotalLeadsToday),
            ("Total Leads This Week", data.KpiCards.TotalLeadsThisWeek),
            ("Total Leads This Month", data.KpiCards.TotalLeadsThisMonth),
            ("SLA Achieved Rate (%)", (object)(data.KpiCards.SlaAchievedRate?.ToString("F1") ?? "N/A")),
            ("Win Rate (%)", (object)(data.KpiCards.WinRate?.ToString("F1") ?? "N/A")),
            ("SLA Violated Count", (object)data.KpiCards.SlaViolatedCount),
        };

        for (var i = 0; i < kpiRows.Length; i++)
        {
            ws.Cell(6 + i, 1).Value = kpiRows[i].Item1;
            ws.Cell(6 + i, 2).Value = kpiRows[i].Item2.ToString();
        }

        // Leads by channel
        var channelRow = 14;
        ws.Cell(channelRow, 1).Value = "Channel";
        ws.Cell(channelRow, 2).Value = "Leads";
        ws.Range(channelRow, 1, channelRow, 2).Style.Font.Bold = true;
        ws.Range(channelRow, 1, channelRow, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
        var r = channelRow + 1;
        foreach (var kv in data.LeadsByChannel)
        {
            ws.Cell(r, 1).Value = kv.Key;
            ws.Cell(r, 2).Value = kv.Value;
            r++;
        }

        // Top 5 stores
        var storeRow = r + 2;
        ws.Cell(storeRow, 1).Value = "Top 5 Stores";
        ws.Cell(storeRow, 1).Style.Font.Bold = true;
        ws.Cell(storeRow + 1, 1).Value = "Store Name";
        ws.Cell(storeRow + 1, 2).Value = "Leads";
        ws.Range(storeRow + 1, 1, storeRow + 1, 2).Style.Font.Bold = true;
        var sr = storeRow + 2;
        foreach (var store in data.Top5Stores)
        {
            ws.Cell(sr, 1).Value = store.StoreName;
            ws.Cell(sr, 2).Value = store.LeadCount;
            sr++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildUnitComparisonSheet(XLWorkbook workbook, UnitComparisonDto data)
    {
        var ws = workbook.Worksheets.Add("Unit Comparison");

        ws.Cell(1, 1).Value = "Unit Comparison Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Period: {data.Period} ({data.PeriodStart:yyyy-MM-dd} - {data.PeriodEnd:yyyy-MM-dd})";

        var headers = new[] { "Store Name", "Region", "Lead Count", "Win Rate (%)", "SLA Achieved Rate (%)", "Avg Processing Time (h)" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
            ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var row = 5;
        foreach (var item in data.Items)
        {
            ws.Cell(row, 1).Value = item.StoreName;
            ws.Cell(row, 2).Value = item.Region ?? "";
            ws.Cell(row, 3).Value = item.LeadCount;
            ws.Cell(row, 4).Value = item.WinRate.HasValue ? item.WinRate.Value.ToString("F1") : "N/A";
            ws.Cell(row, 5).Value = item.SlaAchievedRate.HasValue ? item.SlaAchievedRate.Value.ToString("F1") : "N/A";
            ws.Cell(row, 6).Value = item.AvgProcessingTimeHours.HasValue ? item.AvgProcessingTimeHours.Value.ToString("F1") : "N/A";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildSalesReportSheet(XLWorkbook workbook, SalesReportDto data)
    {
        var ws = workbook.Worksheets.Add("Sales Report");

        ws.Cell(1, 1).Value = "Sales Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Period: {data.Period} ({data.PeriodStart:yyyy-MM-dd} - {data.PeriodEnd:yyyy-MM-dd})";

        var summaryRows = new[]
        {
            ("Total Leads", (object)data.TotalLeads),
            ("Contacted", data.ContactedCount),
            ("Won", data.WonCount),
            ("Contact Rate (%)", (object)(data.ContactRate?.ToString("F1") ?? "N/A")),
            ("Win Rate (%)", (object)(data.WinRate?.ToString("F1") ?? "N/A")),
        };

        ws.Cell(4, 1).Value = "Summary";
        ws.Cell(4, 1).Style.Font.Bold = true;
        for (var i = 0; i < summaryRows.Length; i++)
        {
            ws.Cell(5 + i, 1).Value = summaryRows[i].Item1;
            ws.Cell(5 + i, 2).Value = summaryRows[i].Item2.ToString();
        }

        // Won by channel
        var channelRow = 12;
        ws.Cell(channelRow, 1).Value = "Won by Channel";
        ws.Cell(channelRow, 1).Style.Font.Bold = true;
        ws.Cell(channelRow + 1, 1).Value = "Channel";
        ws.Cell(channelRow + 1, 2).Value = "Won Count";
        ws.Range(channelRow + 1, 1, channelRow + 1, 2).Style.Font.Bold = true;
        var r = channelRow + 2;
        foreach (var kv in data.WonByChannel)
        {
            ws.Cell(r, 1).Value = kv.Key;
            ws.Cell(r, 2).Value = kv.Value;
            r++;
        }

        // Daily trend
        var trendRow = r + 2;
        ws.Cell(trendRow, 1).Value = "Daily Trend";
        ws.Cell(trendRow, 1).Style.Font.Bold = true;
        ws.Cell(trendRow + 1, 1).Value = "Date";
        ws.Cell(trendRow + 1, 2).Value = "Total Leads";
        ws.Cell(trendRow + 1, 3).Value = "Won";
        ws.Range(trendRow + 1, 1, trendRow + 1, 3).Style.Font.Bold = true;
        var tr = trendRow + 2;
        foreach (var day in data.DailyTrend)
        {
            ws.Cell(tr, 1).Value = day.Date;
            ws.Cell(tr, 2).Value = day.TotalLeads;
            ws.Cell(tr, 3).Value = day.WonCount;
            tr++;
        }

        ws.Columns().AdjustToContents();
    }
}
