namespace OmniRoute.Domain.Constants;

public sealed record RoleDefinition(Guid RoleId, string Code, string DisplayName);

public static class RoleCatalog
{
    public const string Consultant = "TV";
    public const string Sales = "SA";
    public const string CustomerService = "CS";
    public const string Dispatcher = "DP";
    public const string TeamLead = "TN";
    public const string StoreManager = "QL";
    public const string SystemAdmin = "QT";
    public const string BoardManagement = "BQL";
    public const string StoreSales = "SS";

    public static readonly Guid ConsultantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SalesId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CustomerServiceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DispatcherId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid TeamLeadId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid StoreManagerId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid SystemAdminId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid BoardManagementId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid StoreSalesId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        new(ConsultantId, Consultant, "Nhân viên tư vấn"),
        new(SalesId, Sales, "Nhân viên sale"),
        new(CustomerServiceId, CustomerService, "Nhân viên chăm sóc khách hàng"),
        new(DispatcherId, Dispatcher, "Nhân viên điều phối"),
        new(TeamLeadId, TeamLead, "Trưởng nhóm / giám sát vận hành"),
        new(StoreManagerId, StoreManager, "Quản lý cửa hàng"),
        new(SystemAdminId, SystemAdmin, "Quản trị hệ thống"),
        new(BoardManagementId, BoardManagement, "Ban quản lý"),
        new(StoreSalesId, StoreSales, "Nhân viên sale cửa hàng")
    ];

    private static readonly Dictionary<string, RoleDefinition> Definitions =
        All.ToDictionary(role => role.Code, StringComparer.OrdinalIgnoreCase);

    public static string? Normalize(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return null;
        }

        return Definitions.TryGetValue(roleCode.Trim(), out var role)
            ? role.Code
            : roleCode.Trim().ToUpperInvariant();
    }

    public static bool TryGetDefinition(string? roleCode, out RoleDefinition role)
    {
        if (!string.IsNullOrWhiteSpace(roleCode) &&
            Definitions.TryGetValue(roleCode.Trim(), out role!))
        {
            return true;
        }

        role = null!;
        return false;
    }

    public static string? GetDisplayName(string? roleCode)
        => TryGetDefinition(roleCode, out var role) ? role.DisplayName : null;
}
