using System.Net;
using BuildingBlocks.Resilience;
using Xunit;

namespace BuildingBlocks.Resilience.Tests;

public class RetryHandlerTests
{
    private delegate Task<HttpResponseMessage> Step(HttpRequestMessage request, CancellationToken ct);

    // Mỗi lần gọi chạy một bước theo thứ tự, hết thì lặp lại bước cuối
    private sealed class ScriptedHandler(params Step[] steps) : HttpMessageHandler
    {
        public int Calls;
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var step = steps[Math.Min(Calls++, steps.Length - 1)];
            if (request.Content is not null) Bodies.Add(await request.Content.ReadAsStringAsync(ct));
            return await step(request, ct);
        }
    }

    private static Step Respond(HttpStatusCode code) => (_, _) => Task.FromResult(new HttpResponseMessage(code));
    private static Step Throw(Exception ex) => (_, _) => throw ex;
    private static Step Hang() => async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(); };

    private static HttpClient Client(ScriptedHandler inner, int maxAttempts = 3, int attemptTimeoutMs = 2000)
        => new(new RetryHandler(new RetryOptions
        {
            MaxAttempts = maxAttempts,
            BaseDelay = TimeSpan.Zero,
            AttemptTimeout = TimeSpan.FromMilliseconds(attemptTimeoutMs)
        }) { InnerHandler = inner }) { BaseAddress = new Uri("http://test") };

    [Fact]
    public async Task Loi_5xx_roi_thanh_cong_thi_thu_lai()
    {
        var inner = new ScriptedHandler(Respond(HttpStatusCode.ServiceUnavailable), Respond(HttpStatusCode.BadGateway), Respond(HttpStatusCode.OK));

        var res = await Client(inner).GetAsync("/x");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task Het_luot_thi_tra_ve_response_loi_cuoi_cung()
    {
        var inner = new ScriptedHandler(Respond(HttpStatusCode.ServiceUnavailable));

        var res = await Client(inner).GetAsync("/x");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
        Assert.Equal(3, inner.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task Cac_ma_tam_thoi_duoc_thu_lai(HttpStatusCode code)
    {
        var inner = new ScriptedHandler(Respond(code), Respond(HttpStatusCode.OK));

        await Client(inner).GetAsync("/x");

        Assert.Equal(2, inner.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task Loi_dut_khoat_khong_thu_lai(HttpStatusCode code)
    {
        var inner = new ScriptedHandler(Respond(code), Respond(HttpStatusCode.OK));

        var res = await Client(inner).GetAsync("/x");

        Assert.Equal(code, res.StatusCode);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Mat_ket_noi_roi_khoi_thi_thu_lai()
    {
        var inner = new ScriptedHandler(Throw(new HttpRequestException("refused")), Respond(HttpStatusCode.OK));

        var res = await Client(inner).GetAsync("/x");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Mat_ket_noi_mai_thi_nem_lai_loi_goc_sau_du_luot()
    {
        var inner = new ScriptedHandler(Throw(new HttpRequestException("refused")));

        await Assert.ThrowsAsync<HttpRequestException>(() => Client(inner).GetAsync("/x"));

        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task Mot_lan_thu_bi_treo_thi_het_timeout_roi_thu_lai()
    {
        var inner = new ScriptedHandler(Hang(), Respond(HttpStatusCode.OK));

        var res = await Client(inner, attemptTimeoutMs: 50).GetAsync("/x");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Treo_mai_thi_het_luot_va_nem_loi_huy()
    {
        var inner = new ScriptedHandler(Hang());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(inner, attemptTimeoutMs: 30).GetAsync("/x"));

        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task Nguoi_goi_huy_thi_khong_thu_lai()
    {
        var inner = new ScriptedHandler(Hang());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(inner).GetAsync("/x", cts.Token));

        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Body_cua_POST_duoc_gui_lai_day_du_o_moi_lan_thu()
    {
        var inner = new ScriptedHandler(Respond(HttpStatusCode.ServiceUnavailable), Respond(HttpStatusCode.OK));

        await Client(inner).PostAsync("/x", new StringContent("{\"a\":1}"));

        Assert.Equal(["{\"a\":1}", "{\"a\":1}"], inner.Bodies);
    }
}