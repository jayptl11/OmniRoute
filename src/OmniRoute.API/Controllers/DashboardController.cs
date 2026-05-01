using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.Dashboard.DTOs;
using OmniRoute.Application.Features.Dashboard.Queries.ExportReport;
using OmniRoute.Application.Features.Dashboard.Queries.GetDashboardOverview;
using OmniRoute.Application.Features.Dashboard.Queries.GetDrillDown;
using OmniRoute.Application.Features.Dashboard.Queries.GetRoutingKpi;
using OmniRoute.Application.Features.Dashboard.Queries.GetSalesReport;
using OmniRoute.Application.Features.Dashboard.Queries.GetUnitComparison;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "CanViewDashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender) => _sender = sender;

    /// <summary>BQL-01 — Dashboard tổng hợp toàn hệ thống. KPI cards, breakdown kênh/nhu cầu, trend 30 ngày, top-5 cửa hàng.</summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetDashboardOverviewQuery(period, dateFrom, dateTo), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>
    /// BQL-02 — Drill-down dashboard. level = "unit" | "channel".
    /// id = storeId (Guid) for level=unit, or channel name for level=channel.
    /// </summary>
    [HttpGet("drill-down")]
    [ProducesResponseType(typeof(DrillDownDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDrillDown(
        [FromQuery] string level = "unit",
        [FromQuery] string? id = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetDrillDownQuery(level, id, dateFrom, dateTo), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>BQL-03 — KPI phân luồng: rule match rate, avg time to assign, SLA rate, escalation rate. Kèm so sánh kỳ trước.</summary>
    [HttpGet("routing-kpi")]
    [ProducesResponseType(typeof(RoutingKpiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRoutingKpi(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetRoutingKpiQuery(period, dateFrom, dateTo), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>BQL-04 — So sánh hiệu suất giữa các đơn vị. sortBy: leadCount | winRate | slaAchievedRate | avgProcessingTime.</summary>
    [HttpGet("unit-comparison")]
    [ProducesResponseType(typeof(UnitComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUnitComparison(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string sortBy = "leadCount",
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetUnitComparisonQuery(period, dateFrom, dateTo, sortBy), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>BQL-05 — Báo cáo hiệu quả bán hàng: funnel total→contacted→won, breakdown theo kênh/nhu cầu, daily trend.</summary>
    [HttpGet("sales-report")]
    [ProducesResponseType(typeof(SalesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetSalesReportQuery(period, dateFrom, dateTo), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>BQL-06 — Xuất báo cáo theo kỳ. reportType: overview | unitComparison | sales. format: excel (mặc định) | pdf.</summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string reportType = "overview",
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new ExportReportQuery(reportType, period, dateFrom, dateTo, format), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return File(result.Value!.FileBytes, result.Value.ContentType, result.Value.FileName);
    }
}
