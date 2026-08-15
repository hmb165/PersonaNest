using PersonaNest.Domain.Abstractions;
using PersonaNest.Domain.Entities;
using PersonaNest.Domain.Enums;
using PersonaNest.Services.Common;
using PersonaNest.Services.DTOs.Requests;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IReportService"/>
public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<ServiceResult> SubmitAsync(
        CreateReportRequest request, string reporterId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(typeof(ReportReason), request.Reason))
        {
            return ServiceResult.Failure("That report reason is not valid.");
        }

        switch (request.TargetType)
        {
            case ReportTargetType.Media:
                if (!await _uow.Media.AnyAsync(m => m.Id == request.TargetId, cancellationToken))
                {
                    return ServiceResult.Failure("That media item no longer exists.");
                }

                await _uow.Repository<MediaReport>().AddAsync(new MediaReport
                {
                    MediaId = request.TargetId,
                    ReporterId = reporterId,
                    Reason = request.Reason,
                    Status = ReportStatus.Open,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
                break;

            case ReportTargetType.Entry:
                if (!await _uow.Entries.AnyAsync(e => e.Id == request.TargetId, cancellationToken))
                {
                    return ServiceResult.Failure("That entry no longer exists.");
                }

                await _uow.Repository<EntryReport>().AddAsync(new EntryReport
                {
                    EntryId = request.TargetId,
                    ReporterId = reporterId,
                    Reason = request.Reason,
                    Status = ReportStatus.Open,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
                break;

            case ReportTargetType.Comment:
                if (!await _uow.Repository<Comment>().AnyAsync(c => c.Id == request.TargetId, cancellationToken))
                {
                    return ServiceResult.Failure("That comment no longer exists.");
                }

                await _uow.Repository<CommentReport>().AddAsync(new CommentReport
                {
                    CommentId = request.TargetId,
                    ReporterId = reporterId,
                    Reason = request.Reason,
                    Status = ReportStatus.Open,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
                break;

            default:
                return ServiceResult.Failure("Unknown report target.");
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}
