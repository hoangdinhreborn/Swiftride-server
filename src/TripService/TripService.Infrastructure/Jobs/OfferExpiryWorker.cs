using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TripService.Application.Common.Interfaces;
using TripService.Application.Trips;
using TripService.Application.Trips.Commands.ExpireOffer;
using TripService.Application.Trips.Interfaces;

namespace TripService.Infrastructure.Jobs;

public sealed class OfferExpiryWorker(
    IServiceScopeFactory scopes, OfferExpiryOptions options, TimeProvider clock, ILogger<OfferExpiryWorker> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.ScanInterval);
        do
        {
            try { await ScanAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { log.LogWarning(ex, "Lượt quét lời đề xuất quá hạn lỗi, thử lại sau"); }   // ví dụ DB chưa sẵn sàng
        }
        while (await WaitNextAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitNextAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        IReadOnlyList<Guid> ids;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ITripRepository>();
            var cutoff = clock.GetUtcNow().UtcDateTime - options.Ttl;
            ids = await repo.GetExpiredOfferIdsAsync(cutoff, options.BatchSize, ct);
        }

        foreach (var id in ids)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();   // mỗi chuyến một scope (một DbContext) riêng
                var handler = scope.ServiceProvider.GetRequiredService<IHandler<ExpireOfferCommand, bool>>();
                if (await handler.HandleAsync(new ExpireOfferCommand(id), ct))
                    log.LogInformation("Chuyến {TripId}: tài xế không trả lời, coi như từ chối", id);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // Matching lỗi, hoặc tài xế vừa trả lời cùng lúc (xmin): bỏ qua, lượt sau thử lại
                log.LogWarning("Chuyến {TripId}: chưa xử lý được lời đề xuất quá hạn: {Error}", id, ex.Message);
            }
        }
    }
}