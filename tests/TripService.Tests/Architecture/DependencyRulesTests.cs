using NetArchTest.Rules;
using TripService.Api.Auth;
using TripService.Application.Common.Interfaces;
using TripService.Domain.Trips;
using TripService.Infrastructure.Persistence;
using Xunit;

namespace TripService.Tests.Architecture;

public class DependencyRulesTests
{
    private static readonly System.Reflection.Assembly Domain = typeof(Trip).Assembly;
    private static readonly System.Reflection.Assembly Application = typeof(IUnitOfWork).Assembly;
    private static readonly System.Reflection.Assembly Infrastructure = typeof(TripDbContext).Assembly;

    private static void AssertNoDependency(System.Reflection.Assembly from, params string[] forbidden)
    {
        var result = Types.InAssembly(from)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Vi phạm: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_khong_phu_thuoc_tang_nao_khac()
        => AssertNoDependency(Domain,
            "TripService.Application", "TripService.Infrastructure", "TripService.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");

    [Fact]
    public void Application_chi_biet_Domain()
        => AssertNoDependency(Application,
            "TripService.Infrastructure", "TripService.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql");

    [Fact]
    public void Infrastructure_khong_biet_Api()
        => AssertNoDependency(Infrastructure, "TripService.Api", "Microsoft.AspNetCore");

    [Fact]
    public void Handler_nam_trong_Application_va_ten_ket_thuc_bang_Handler()
    {
        var result = Types.InAssembly(Application)
            .That().ImplementInterface(typeof(IHandler<,>))
            .Should().HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}