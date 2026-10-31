namespace MatchingService.Application.Drivers;

public static class MatchingPolicy
{
    public const double SearchRadiusKm = 5;     // bán kính tìm tài xế
    public const double SurgeRadiusKm = 3;      // bán kính đếm cung để tính surge
    public const int MaxCandidates = 3;
    public static readonly TimeSpan MaxLocationAge = TimeSpan.FromMinutes(5); // vị trí cũ hơn thì bỏ qua
}