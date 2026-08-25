namespace Icod.Timing;

using System.Diagnostics;

/// <summary>
/// Supplies monotonic timestamps and cancellable delays for timeout and scheduling logic.
/// </summary>
public interface IMonotonicClock {
	/// <summary>Gets a monotonic timestamp in provider-defined units.</summary>
	/// <returns>A monotonic timestamp suitable for later use with <see cref="GetElapsedTime"/>.</returns>
	long GetTimestamp();

	/// <summary>Gets the elapsed duration between two timestamps from this clock.</summary>
	/// <param name="startingTimestamp">The earlier timestamp.</param>
	/// <param name="endingTimestamp">The later timestamp.</param>
	/// <returns>The elapsed duration between the supplied timestamps.</returns>
	TimeSpan GetElapsedTime(
		long startingTimestamp,
		long endingTimestamp
	);

	/// <summary>Waits for a duration without depending on wall-clock adjustments.</summary>
	/// <param name="delay">The nonnegative elapsed duration to wait.</param>
	/// <param name="cancellationToken">Cancellation for the delay.</param>
	/// <returns>A value task that completes when the duration elapses.</returns>
	ValueTask DelayAsync(
		TimeSpan delay,
		CancellationToken cancellationToken = default
	);
}

/// <summary>
/// Provides monotonic timestamps through <see cref="Stopwatch"/> and delays through the task scheduler.
/// </summary>
public sealed class SystemMonotonicClock : IMonotonicClock {
	private static readonly TimeSpan MaximumDelaySlice = TimeSpan.FromDays(
		7
	);

	/// <summary>Gets the shared system monotonic clock.</summary>
	public static SystemMonotonicClock Instance {
		get;
	} = new();

	private SystemMonotonicClock() {
	}

	/// <inheritdoc />
	public long GetTimestamp() => Stopwatch.GetTimestamp();

	/// <inheritdoc />
	public TimeSpan GetElapsedTime(
		long startingTimestamp,
		long endingTimestamp
	) => Stopwatch.GetElapsedTime(
		startingTimestamp,
		endingTimestamp
	);

	/// <inheritdoc />
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
		if ( TimeSpan.Zero == delay ) {
			return ValueTask.CompletedTask;
		}
		return new ValueTask(
			DelayCoreAsync(
				delay,
				cancellationToken
			)
		);
	}

	private static async Task DelayCoreAsync(
		TimeSpan delay,
		CancellationToken cancellationToken
	) {
		var started = Stopwatch.GetTimestamp();
		while ( true ) {
			var elapsed = Stopwatch.GetElapsedTime(
				started,
				Stopwatch.GetTimestamp()
			);
			var remaining = delay - elapsed;
			if ( TimeSpan.Zero >= remaining ) {
				return;
			}
			await Task.Delay(
				remaining < MaximumDelaySlice
					? remaining
					: MaximumDelaySlice,
				cancellationToken
			).ConfigureAwait( false );
		}
	}
}
