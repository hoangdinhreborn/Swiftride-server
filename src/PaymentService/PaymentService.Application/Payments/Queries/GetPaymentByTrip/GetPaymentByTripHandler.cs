using BuildingBlocks.Security;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Interfaces;

namespace PaymentService.Application.Payments.Queries.GetPaymentByTrip;

public sealed class GetPaymentByTripHandler(IPaymentRepository repo, ICurrentUser user)
    : IHandler<GetPaymentByTripQuery, PaymentDto>
{
    public async Task<PaymentDto> HandleAsync(GetPaymentByTripQuery q, CancellationToken ct = default)
    {
        var payment = await repo.GetLatestByTripAsync(q.TripId, ct)
                      ?? throw new NotFoundException($"Chuyến {q.TripId} chưa có thanh toán");

        if (user.Role != Roles.Admin && payment.PayerId != user.Id)
            throw new ForbiddenException("Bạn không có quyền xem thanh toán này");

        return payment.ToDto();
    }
}