# Icod.Timing

[![PR Staging build](https://github.com/uniblab/Icod.Timing/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.Timing/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.Timing/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.Timing/actions/workflows/main.yaml)

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
  logical schedule sequence, scheduled elapsed time, observed elapsed time, and
  lateness.
- `PeriodicMissedTickPolicy` makes overdue-tick behavior explicit. The default
  `SkipMissed` policy advances to the most recent schedule position that is
  already due; `CatchUp` emits each overdue position in sequence.
- `IPeriodicScheduler` supplies cancellable fixed-rate periodic ticks.
- `MonotonicPeriodicScheduler` schedules against elapsed time from a monotonic
  clock instead of repeatedly delaying from the previous tick, avoiding
  cumulative drift.

When `SkipMissed` is used, `PeriodicTick.Sequence` is the logical fixed-rate
schedule sequence and can therefore jump when one or more overdue ticks are
discarded.

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
	fireImmediately: true,
	missedTickPolicy: PeriodicMissedTickPolicy.SkipMissed
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

## Sample

`Icod.Timing.Sample` demonstrates elapsed-time measurement, cancellable delay,
fixed-rate scheduling, and skipped-tick behavior with the system clock.

```text
dotnet run --project samples/Icod.Timing.Sample/Icod.Timing.Sample.csproj
```

## Build and test

`Icod.Timing` targets .NET 7.0, 8.0, 9.0, and 10.0 and uses C# 13. Debug information is portable in Debug, Staging, and Release configurations.

The repository follows the canonical Icod build cycle:

| Lifecycle | Configuration |
| --- | --- |
| local `build.cmd` / `build.sh` | `Debug` |
| pull request | `Staging` |
| push to `main` | `Release` validation |
| `v<semver>` tag contained in `main` | `Release` publication |

Local development uses:

```text
build.cmd
```

or:

```text
./build.sh
```

With no argument, the scripts run:

```text
clean -> restore -> build -> test -> pack -> validate
```

The repository contains the library project at the root, a sample under
`samples/Icod.Timing.Sample`, and its test project under `tests/Timing.Tests`.

Pull requests build and test Staging on Windows, Linux, and macOS. Linux produces
and exact-verifies the Staging `.nupkg` and `.snupkg`. Pushes to `main` perform
validation-only Release builds on Windows/Linux/macOS x64 and ARM64. Package
publication occurs only from `.github/workflows/release.yaml` after an immutable
`v<semver>` tag is verified to match the package version and be contained in
`main`.

See [`packaging/README.md`](packaging/README.md) for build and distribution tooling details.

## Author and copyright

Author: Timothy J. Bruce <uniblab@hotmail.com>

Copyright (c) 2026 Timothy J. Bruce

## License

`Icod.Timing` is licensed under the GNU Lesser General Public License, version
3.0 or later. See `LICENSE`.
