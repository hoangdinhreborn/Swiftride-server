using System.Reflection;
using NetArchTest.Rules;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Payments;
using PaymentService.Infrastructure.Persistence;
using Xunit;

namespace PaymentService.Tests.Architecture;

public class DependencyRulesTests
{
    private static readonly Assembly Domain = typeof(Payment).Assembly;
    private static readonly Assembly Application = typeof(IUnitOfWork).Assembly;
    private static readonly Assembly Infrastructure = typeof(PaymentDbContext).Assembly;

    private static void AssertNoDependency(Assembly from, params string[] forbidden)
    {
        var result = Types.InAssembly(from).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();
        Assert.True(result.IsSuccessful, "Vi phạm: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_khong_phu_thuoc_tang_nao_khac()
        => AssertNoDependency(Domain,
            "PaymentService.Application", "PaymentService.Infrastructure", "PaymentService.Api",
            "BuildingBlocks", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore",
            "RabbitMQ", "OpenTelemetry", "Npgsql");

    [Fact]
    public void Application_chi_biet_Domain()
        => AssertNoDependency(Application,
            "PaymentService.Infrastructure", "PaymentService.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql",
            "RabbitMQ", "OpenTelemetry", "System.Net.Http");

    [Fact]
    public void Infrastructure_khong_biet_Api()
        => AssertNoDependency(Infrastructure, "PaymentService.Api", "Microsoft.AspNetCore");

    [Fact]
    public void Handler_ten_ket_thuc_bang_Handler()
    {
        var result = Types.InAssembly(Application)
            .That().ImplementInterface(typeof(IHandler<,>))
            .Should().HaveNameEndingWith("Handler")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}