using FluentAssertions;
using OmniRoute.Domain.Constants;

namespace OmniRoute.UnitTests.Domain;

public class RoleCatalogTests
{
    [Fact]
    public void All_ShouldContainNineCanonicalRolesWithCorrectLabels()
    {
        RoleCatalog.All.Should().HaveCount(9);
        RoleCatalog.GetDisplayName(RoleCatalog.Consultant).Should().Be("Nhân viên tư vấn");
        RoleCatalog.GetDisplayName(RoleCatalog.Sales).Should().Be("Nhân viên sale");
        RoleCatalog.GetDisplayName(RoleCatalog.CustomerService).Should().Be("Nhân viên chăm sóc khách hàng");
        RoleCatalog.GetDisplayName(RoleCatalog.Dispatcher).Should().Be("Nhân viên điều phối");
        RoleCatalog.GetDisplayName(RoleCatalog.TeamLead).Should().Be("Trưởng nhóm / giám sát vận hành");
        RoleCatalog.GetDisplayName(RoleCatalog.StoreManager).Should().Be("Quản lý cửa hàng");
        RoleCatalog.GetDisplayName(RoleCatalog.SystemAdmin).Should().Be("Quản trị hệ thống");
        RoleCatalog.GetDisplayName(RoleCatalog.BoardManagement).Should().Be("Ban quản lý");
        RoleCatalog.GetDisplayName(RoleCatalog.StoreSales).Should().Be("Nhân viên sale cửa hàng");
    }
}
