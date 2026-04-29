namespace OmniRoute.Application.Features.Stores.DTOs;

/// <summary>Kết quả tìm kiếm QL để gán làm quản lý đơn vị.</summary>
public record StoreManagerDto(
    Guid UserId,
    string FullName,
    string Username,
    bool HasStore,         // đang quản lý cửa hàng khác
    string? CurrentStore); // tên cửa hàng hiện tại (nếu có)
