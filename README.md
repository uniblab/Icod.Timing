# Icod.Timing

`Icod.Timing` is a small, command-neutral .NET library for monotonic elapsed-time
measurement, cancellable delays, and drift-resistant periodic scheduling.

The library deliberately does not own civil or calendar time. Date parsing,
formatting, time zones, local/UTC wall clocks, and system-clock administration
belong in domain-specific libraries. `Icod.Timing` is concerned with questions
such as "how much time has elapsed?", "wait for this duration", and "run again at
this fixed cadence."

## What the library provides

- `IMonotonicClock` abstracts monotonic timestamp observation, elapsed-time
  calculation, and cancellable delays.
- `SystemMonotonicClock` uses `Stopwatch` for elapsed-time measurement and
  rechecks long delays in bounded slices so wall-clock adjustments cannot change
  the requested elapsed duration.
- `PeriodicTick` describes one fixed-rate scheduling observation, including its
  sequence, scheduled elapsed time, observed elapsed time, and lateness.
- `IPeriodicScheduler` supplies cancellable fixed-rate periodic ticks.
- `MonotonicPeriodicScheduler` schedules against elapsed time from a monotonic
  clock instead of repeatedly delaying from the previous tick, avoiding
  cumulative drift.

## Basic use

```csharp
using Icod.Timing;

IMonotonicClock clock = SystemMonotonicClock.Instance;

long started = clock.GetTimestamp();
await clock.DelayAsync( TimeSpan.FromMilliseconds( 250 ) );
TimeSpan elapsed = clock.GetElapsedTime(
	started,
	clock.GetTimestamp()
);

await foreach ( PeriodicTick tick in MonotonicPeriodicScheduler.Instance.ScheduleAsync(
	TimeSpan.FromSeconds( 1 ),
	fireImmediately: true
) ) {
	Console.WriteLine(
		$"tick {tick.Sequence}: scheduled={tick.ScheduledElapsed}, late={tick.Lateness}"
	);
}
```

## Intended consumers

The package is suitable for utility suites, terminal libraries, services,
networking code, retry/backoff infrastructure, test runners, monitoring tools,
and other applications that need elapsed-time or fixed-cadence behavior without
taking a dependency on a command framework.

Typical consumers include timeout enforcement, `tail -f` polling, progress
reporting, refresh loops, input ambiguity windows, health checks, keepalives,
rate limiting, and periodic sampling.

## Build and test

`Icod.Timing` targets .NET 7.0, 8.0, 9.0, and 10.0 and uses C# 13.

```text
dotnet build Icod.Timing.sln
dotnet test Icod.Timing.sln
```

The repository contains the library project at the root and its test project
under `tests/Timing.Tests`.

## License

`Icod.Timing` is licensed under the GNU Lesser General Public License, version
3.0 or later. See `LICENSE`.
