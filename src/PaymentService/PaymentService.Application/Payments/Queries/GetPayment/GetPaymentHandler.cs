using BuildingBlocks.Security;
using PaymentService.Application.Common.Exceptions;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Payments.Dtos;
using PaymentService.Application.Payments.Interfaces;

namespace PaymentService.Application.Payments.Queries.GetPayment;

public sealed class GetPaymentHandler(IPaymentRepository repo, ICurrentUser user)
    : IHandler<GetPaymentQuery, PaymentDto>
{
    public async Task<PaymentDto> HandleAsync(GetPaymentQuery q, CancellationToken ct = default)
    {
        var payment = await repo.GetByIdAsync(q.PaymentId, ct)
                      ?? throw new NotFoundException($"Không tìm thấy thanh toán {q.PaymentId}");

        if (user.Role != Roles.Admin && payment.PayerId != user.Id)
            throw new ForbiddenException("Bạn không có quyền xem thanh toán này");

        return payment.ToDto();
    }
}