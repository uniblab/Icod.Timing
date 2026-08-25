using Xunit;

namespace Icod.Timing.Tests;

/// <summary>Exercises monotonic clock and fixed-rate scheduling contracts.</summary>
public sealed class TimingTests {
	/// <summary>Verifies zero delay completes synchronously and negative delay is rejected.</summary>
	[Fact]
	public async Task SystemClockValidatesDelay() {
		await SystemMonotonicClock.Instance.DelayAsync(
			TimeSpan.Zero
		);

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			async () => await SystemMonotonicClock.Instance.DelayAsync(
				TimeSpan.FromTicks( -1 )
			)
		);
	}

	/// <summary>Verifies periodic ticks expose positive lateness only after their scheduled time.</summary>
	[Fact]
	public void PeriodicTickComputesLateness() {
		var early = new PeriodicTick(
			0,
			TimeSpan.FromSeconds( 2 ),
			TimeSpan.FromSeconds( 1 )
		);
		var late = new PeriodicTick(
			1,
			TimeSpan.FromSeconds( 2 ),
			TimeSpan.FromMilliseconds( 2250 )
		);

		Assert.Equal(
			TimeSpan.Zero,
			early.Lateness
		);
		Assert.Equal(
			TimeSpan.FromMilliseconds( 250 ),
			late.Lateness
		);
	}

	/// <summary>Verifies negative periodic sequence values are rejected.</summary>
	[Fact]
	public void PeriodicTickRejectsNegativeSequence() {
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new PeriodicTick(
				-1,
				TimeSpan.Zero,
				TimeSpan.Zero
			)
		);
	}

	/// <summary>Verifies fixed-rate scheduling remains anchored to the original start time.</summary>
	[Fact]
	public async Task SchedulerUsesFixedRateElapsedTargets() {
		var clock = new SyntheticMonotonicClock();
		var scheduler = new MonotonicPeriodicScheduler(
			clock
		);

		await using var enumerator = scheduler.ScheduleAsync(
			TimeSpan.FromMilliseconds( 250 )
		).GetAsyncEnumerator();

		for ( var sequence = 0; sequence < 3; sequence++ ) {
			Assert.True(
				await enumerator.MoveNextAsync()
			);
			var expected = TimeSpan.FromMilliseconds(
				250 * ( sequence + 1 )
			);
			Assert.Equal(
				(long)sequence,
				enumerator.Current.Sequence
			);
			Assert.Equal(
				expected,
				enumerator.Current.ScheduledElapsed
			);
			Assert.Equal(
				expected,
				enumerator.Current.ObservedElapsed
			);
			Assert.Equal(
				TimeSpan.Zero,
				enumerator.Current.Lateness
			);
		}
	}

	/// <summary>Verifies immediate schedules emit elapsed-time zero before the first interval.</summary>
	[Fact]
	public async Task SchedulerCanFireImmediately() {
		var clock = new SyntheticMonotonicClock();
		var scheduler = new MonotonicPeriodicScheduler(
			clock
		);

		await using var enumerator = scheduler.ScheduleAsync(
			TimeSpan.FromSeconds( 1 ),
			fireImmediately: true
		).GetAsyncEnumerator();

		Assert.True(
			await enumerator.MoveNextAsync()
		);
		Assert.Equal(
			0L,
			enumerator.Current.Sequence
		);
		Assert.Equal(
			TimeSpan.Zero,
			enumerator.Current.ScheduledElapsed
		);

		Assert.True(
			await enumerator.MoveNextAsync()
		);
		Assert.Equal(
			1L,
			enumerator.Current.Sequence
		);
		Assert.Equal(
			TimeSpan.FromSeconds( 1 ),
			enumerator.Current.ScheduledElapsed
		);
	}

	/// <summary>Verifies periodic scheduling requires a positive interval.</summary>
	[Fact]
	public async Task SchedulerRejectsNonPositiveInterval() {
		var scheduler = new MonotonicPeriodicScheduler(
			new SyntheticMonotonicClock()
		);

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			async () => {
				await using var enumerator = scheduler.ScheduleAsync(
					TimeSpan.Zero
				).GetAsyncEnumerator();

				await enumerator.MoveNextAsync();
			}
		);
	}

	private sealed class SyntheticMonotonicClock : IMonotonicClock {
		private long timestamp;

		public long GetTimestamp() => this.timestamp;

		public TimeSpan GetElapsedTime(
			long startingTimestamp,
			long endingTimestamp
		) => TimeSpan.FromTicks(
			endingTimestamp - startingTimestamp
		);

		public ValueTask DelayAsync(
			TimeSpan delay,
			CancellationToken cancellationToken = default
		) {
			if ( TimeSpan.Zero > delay ) {
				throw new ArgumentOutOfRangeException(
					nameof( delay )
				);
			}
			cancellationToken.ThrowIfCancellationRequested();
			this.timestamp = checked(
				this.timestamp + delay.Ticks
			);
			return ValueTask.CompletedTask;
		}
	}
}
