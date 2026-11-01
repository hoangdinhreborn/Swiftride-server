namespace TripService.Domain.Trips.States;

internal sealed class SearchingState : TripState
{
    public static readonly SearchingState Instance = new();
    private SearchingState() { }

    public override TripStatus Status => TripStatus.Searching;
    public override TripState OfferTo() => DriverOfferedState.Instance;
    public override TripState NoDriverFound() => FailedState.Instance;
}
