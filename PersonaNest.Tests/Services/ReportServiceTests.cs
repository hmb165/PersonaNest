using System.Linq.Expressions;
using Moq;
using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Implementations;

namespace PersonaNest.Tests.Services;

/// <summary>Business-rule tests for <see cref="ReportService"/> (§6, D-4).</summary>
public class ReportServiceTests
{
    [Fact]
    public async Task SubmitAsync_RejectsUndefinedReasonEnumValue()
    {
        var uow = new Mock<IUnitOfWork>();
        var service = new ReportService(uow.Object);

        var result = await service.SubmitAsync(
            new CreateReportRequest
            {
                TargetType = ReportTargetType.Media, TargetId = 1, Reason = (ReportReason)99
            },
            "reporter-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That report reason is not valid.", result.FirstError);
    }

    [Fact]
    public async Task SubmitAsync_RejectsMissingMediaTarget()
    {
        var media = new Mock<IMediaRepository>();
        media.Setup(m => m.AnyAsync(It.IsAny<Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(false);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Media).Returns(media.Object);

        var service = new ReportService(uow.Object);

        var result = await service.SubmitAsync(
            new CreateReportRequest
            {
                TargetType = ReportTargetType.Media, TargetId = 999, Reason = ReportReason.Spam
            },
            "reporter-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That media item no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task SubmitAsync_RejectsMissingEntryTarget()
    {
        var entries = new Mock<IEntryRepository>();
        entries.Setup(e => e.AnyAsync(It.IsAny<Expression<Func<Entry, bool>>>(), default))
            .ReturnsAsync(false);

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Entries).Returns(entries.Object);

        var service = new ReportService(uow.Object);

        var result = await service.SubmitAsync(
            new CreateReportRequest
            {
                TargetType = ReportTargetType.Entry, TargetId = 999, Reason = ReportReason.Harassment
            },
            "reporter-1");

        Assert.False(result.Succeeded);
        Assert.Equal("That entry no longer exists.", result.FirstError);
    }

    [Fact]
    public async Task SubmitAsync_CreatesAMediaReport_WhenTargetExists()
    {
        var media = new Mock<IMediaRepository>();
        media.Setup(m => m.AnyAsync(It.IsAny<Expression<Func<Media, bool>>>(), default))
            .ReturnsAsync(true);

        var mediaReports = new Mock<IRepository<MediaReport>>();

        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(u => u.Media).Returns(media.Object);
        uow.Setup(u => u.Repository<MediaReport>()).Returns(mediaReports.Object);

        var service = new ReportService(uow.Object);

        var result = await service.SubmitAsync(
            new CreateReportRequest
            {
                TargetType = ReportTargetType.Media, TargetId = 1, Reason = ReportReason.Duplicate
            },
            "reporter-1");

        Assert.True(result.Succeeded);
        mediaReports.Verify(r => r.AddAsync(
            It.Is<MediaReport>(mr =>
                mr.MediaId == 1 && mr.ReporterId == "reporter-1" &&
                mr.Reason == ReportReason.Duplicate && mr.Status == ReportStatus.Open),
            default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
