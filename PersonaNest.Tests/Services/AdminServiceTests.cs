using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Constants;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Implementations;

namespace PersonaNest.Tests.Services;

/// <summary>
/// Moderation/admin business-rule tests (§6/§7) - ban-reason and resolution-notes validation
/// (both Phase 13 fixes, since <c>AdminController</c> binds them as raw parameters rather than a
/// validated request DTO) and the admin-cannot-be-banned rule.
/// </summary>
public class AdminServiceTests
{
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static AdminService NewService(
        out Mock<IUnitOfWork> uow, out Mock<UserManager<ApplicationUser>> userManager)
    {
        uow = new Mock<IUnitOfWork>();
        userManager = MockUserManager();
        return new AdminService(uow.Object, userManager.Object, Mock.Of<ILogger<AdminService>>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("no")] // below the 3-character minimum
    public async Task BanAsync_RejectsTooShortReason(string reason)
    {
        var service = NewService(out _, out var userManager);

        var result = await service.BanAsync(new BanUserRequest { UserId = "user-1", Reason = reason });

        Assert.False(result.Succeeded);
        Assert.Equal("A ban reason of at least 3 characters is required.", result.FirstError);
        userManager.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BanAsync_RejectsOverLongReason()
    {
        var service = NewService(out _, out _);
        var reason = new string('x', 301);

        var result = await service.BanAsync(new BanUserRequest { UserId = "user-1", Reason = reason });

        Assert.False(result.Succeeded);
        Assert.Equal("Ban reason cannot exceed 300 characters.", result.FirstError);
    }

    [Fact]
    public async Task BanAsync_RejectsBanningAnAdministrator()
    {
        var service = NewService(out _, out var userManager);
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin" };

        userManager.Setup(m => m.FindByIdAsync("admin-1")).ReturnsAsync(admin);
        userManager.Setup(m => m.IsInRoleAsync(admin, Roles.Admin)).ReturnsAsync(true);

        var result = await service.BanAsync(
            new BanUserRequest { UserId = "admin-1", Reason = "Valid reason text" });

        Assert.False(result.Succeeded);
        Assert.Equal("Administrators cannot be banned.", result.FirstError);
    }

    [Fact]
    public async Task ResolveReportAsync_RejectsOverLongNotes()
    {
        var service = NewService(out var uow, out _);
        var reports = new Mock<IReportRepository>();
        uow.SetupGet(u => u.Reports).Returns(reports.Object);

        var result = await service.ResolveReportAsync(
            ReportTargetType.Media, 1, "moderator-1", new string('x', 2001));

        Assert.False(result.Succeeded);
        Assert.Equal("Notes cannot exceed 2000 characters.", result.FirstError);
        reports.Verify(r => r.ResolveAsync(
            It.IsAny<ReportTargetType>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<ReportStatus>(), It.IsAny<string?>(), default), Times.Never);
    }

    [Fact]
    public async Task ResolveReportAsync_ReportsFailure_WhenNoOpenReportMatches()
    {
        var service = NewService(out var uow, out _);
        var reports = new Mock<IReportRepository>();
        reports.Setup(r => r.ResolveAsync(
                ReportTargetType.Media, 1, "moderator-1", ReportStatus.Resolved, null, default))
            .ReturnsAsync(false);
        uow.SetupGet(u => u.Reports).Returns(reports.Object);

        var result = await service.ResolveReportAsync(ReportTargetType.Media, 1, "moderator-1", null);

        Assert.False(result.Succeeded);
        Assert.Equal("That report no longer needs review.", result.FirstError);
    }

    [Fact]
    public async Task DismissReportAsync_Succeeds_WhenAnOpenReportMatches()
    {
        var service = NewService(out var uow, out _);
        var reports = new Mock<IReportRepository>();
        reports.Setup(r => r.ResolveAsync(
                ReportTargetType.Entry, 5, "moderator-1", ReportStatus.Dismissed, "not a violation", default))
            .ReturnsAsync(true);
        uow.SetupGet(u => u.Reports).Returns(reports.Object);

        var result = await service.DismissReportAsync(
            ReportTargetType.Entry, 5, "moderator-1", "not a violation");

        Assert.True(result.Succeeded);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
