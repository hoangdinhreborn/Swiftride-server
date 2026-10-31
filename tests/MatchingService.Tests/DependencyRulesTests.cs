using MatchingService.Application.Common.Interfaces;
using MatchingService.Domain.Drivers;
using MatchingService.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace MatchingService.Tests;

public class DependencyRulesTests
{
    private static readonly System.Reflection.Assembly Domain = typeof(DriverLocation).Assembly;
    private static readonly System.Reflection.Assembly Application = typeof(IHandler<,>).Assembly;
    private static readonly System.Reflection.Assembly Infrastructure = typeof(MatchingMongoContext).Assembly;

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
            "MatchingService.Application", "MatchingService.Infrastructure", "MatchingService.Api",
            "MongoDB.Driver", "Microsoft.AspNetCore");

    [Fact]
    public void Application_khong_biet_Infrastructure_va_Api()
        => AssertNoDependency(Application,
            "MatchingService.Infrastructure", "MatchingService.Api",
            "MongoDB.Driver", "Microsoft.AspNetCore.Http");

    [Fact]
    public void Infrastructure_khong_biet_Api()
        => AssertNoDependency(Infrastructure, "MatchingService.Api", "Microsoft.AspNetCore");

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
