namespace OrderManagement.Infrastructure.Tests.Support;

internal sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
