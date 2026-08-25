# Icod.Timing samples

## Icod.Timing.Sample

The sample demonstrates the public 1.0 timing surface with the system monotonic
clock. It measures one short delay, then runs a fixed-rate periodic schedule with
`PeriodicMissedTickPolicy.SkipMissed`.

After the second emitted tick, the sample deliberately performs work longer than
the interval. The next emitted `PeriodicTick.Sequence` can therefore jump,
showing that overdue schedule positions were discarded instead of replayed in a
burst.

Run it from the repository root:

```text
dotnet run --project samples/Icod.Timing.Sample/Icod.Timing.Sample.csproj
```
