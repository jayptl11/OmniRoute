namespace OmniRoute.Application.Common.Interfaces;

public interface IRoutingEngine
{
    Task ProcessAsync(Guid leadId, CancellationToken ct = default);

    /// <summary>
    /// Sau khi DP dispatch lead về store, tự động gán cho SS ít việc nhất trong store đó (BR-02).
    /// Nếu không có SS available → không làm gì (QL xử lý thủ công).
    /// </summary>
    Task AssignToStoreStaffAsync(Guid leadId, Guid storeId, CancellationToken ct = default);
}
