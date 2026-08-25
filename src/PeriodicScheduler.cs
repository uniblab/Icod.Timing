namespace Icod.Timing;

using System.Runtime.CompilerServices;

/// <summary>
/// Defines how a periodic scheduler handles scheduled ticks that are already overdue.
/// </summary>
public enum PeriodicMissedTickPolicy {
	/// <summary>
	/// Skips overdue schedule positions and emits only the most recent tick due at the observation time.
	/// </summary>
	SkipMissed = 0,

	/// <summary>
	/// Emits every overdue schedule position in sequence until the schedule catches up.
	/// </summary>
	CatchUp = 1
}

/// <summary>
/// Describes one fixed-rate periodic scheduling observation.
/// </summary>
public sealed class PeriodicTick {
	/// <summary>Gets how late the observation occurred relative to the fixed-rate schedule.</summary>
	public TimeSpan Lateness => this.ObservedElapsed > this.ScheduledElapsed
		? this.ObservedElapsed - this.ScheduledElapsed
		: TimeSpan.Zero
	;

	/// <summary>Gets the elapsed duration observed when the tick was emitted.</summary>
	public TimeSpan ObservedElapsed {
		get;
	}

	/// <summary>Gets the elapsed duration at which the tick was scheduled.</summary>
	public TimeSpan ScheduledElapsed {
		get;
	}

	/// <summary>
	/// Gets the zero-based logical schedule sequence. Values can jump when missed ticks are skipped.
	/// </summary>
	public long Sequence {
		get;
	}

	/// <summary>Initializes a periodic tick.</summary>
	/// <param name="sequence">The zero-based logical schedule sequence.</param>
	/// <param name="scheduledElapsed">The elapsed time at which the tick was scheduled.</param>
	/// <param name="observedElapsed">The elapsed time observed when the tick was emitted.</param>
	public PeriodicTick(
		long sequence,
		TimeSpan scheduledElapsed,
		TimeSpan observedElapsed
	) {
		if ( 0 > sequence ) {
			throw new ArgumentOutOfRangeException(
				nameof( sequence )
			);
		}
		if ( TimeSpan.Zero > scheduledElapsed ) {
			throw new ArgumentOutOfRangeException(
				nameof( scheduledElapsed )
			);
		}
		if ( TimeSpan.Zero > observedElapsed ) {
			throw new ArgumentOutOfRangeException(
				nameof( observedElapsed )
			);
		}
		this.Sequence = sequence;
		this.ScheduledElapsed = scheduledElapsed;
		this.ObservedElapsed = observedElapsed;
	}
}

/// <summary>
/// Produces cancellable fixed-rate periodic ticks without using wall-clock time.
/// </summary>
public interface IPeriodicScheduler {
	/// <summary>Schedules periodic ticks.</summary>
	/// <param name="interval">The positive fixed-rate interval between scheduled ticks.</param>
	/// <param name="fireImmediately">Whether to emit sequence zero immediately at elapsed time zero.</param>
	/// <param name="cancellationToken">Cancellation for the schedule.</param>
	/// <param name="missedTickPolicy">The policy applied when one or more schedule positions are overdue.</param>
	/// <returns>An asynchronous sequence of periodic tick observations.</returns>
	IAsyncEnumerable<PeriodicTick> ScheduleAsync(
		TimeSpan interval,
		bool fireImmediately = false,
		CancellationToken cancellationToken = default,
		PeriodicMissedTickPolicy missedTickPolicy = PeriodicMissedTickPolicy.SkipMissed
	);
}

/// <summary>
/// Implements drift-resistant fixed-rate scheduling over an injectable monotonic clock.
/// </summary>
public sealed class MonotonicPeriodicScheduler : IPeriodicScheduler {
	private readonly IMonotonicClock _clock;

	/// <summary>Gets the shared system periodic scheduler.</summary>
	public static MonotonicPeriodicScheduler Instance {
		get;
	} = new(
		SystemMonotonicClock.Instance
	);

	/// <summary>Initializes a monotonic periodic scheduler.</summary>
	/// <param name="clock">The monotonic clock used to observe elapsed time and wait between ticks.</param>
	public MonotonicPeriodicScheduler(
		IMonotonicClock clock
	) {
		ArgumentNullException.ThrowIfNull(
			clock
		);
		this._clock = clock;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<PeriodicTick> ScheduleAsync(
		TimeSpan interval,
		bool fireImmediately = false,
		[EnumeratorCancellation] CancellationToken cancellationToken = default,
		PeriodicMissedTickPolicy missedTickPolicy = PeriodicMissedTickPolicy.SkipMissed
	) {
		if ( TimeSpan.Zero >= interval ) {
			throw new ArgumentOutOfRangeException(
				nameof( interval )
			);
		}
		if ( missedTickPolicy is not PeriodicMissedTickPolicy.SkipMissed
			and not PeriodicMissedTickPolicy.CatchUp ) {
			throw new ArgumentOutOfRangeException(
				nameof( missedTickPolicy )
			);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var started = this._clock.GetTimestamp();
		var sequence = 0L;

		if ( fireImmediately ) {
			yield return new PeriodicTick(
				sequence++,
				TimeSpan.Zero,
				TimeSpan.Zero
			);
		}

		while ( true ) {
			cancellationToken.ThrowIfCancellationRequested();

			var now = this._clock.GetTimestamp();
			var observedElapsed = this._clock.GetElapsedTime(
				started,
				now
			);

			sequence = ApplyMissedTickPolicy(
				sequence,
				interval,
				observedElapsed,
				fireImmediately,
				missedTickPolicy
			);

			var scheduledElapsed = GetScheduledElapsed(
				sequence,
				interval,
				fireImmediately
			);
			var remaining = scheduledElapsed - observedElapsed;

			if ( TimeSpan.Zero < remaining ) {
				await this._clock.DelayAsync(
					remaining,
					cancellationToken
				).ConfigureAwait( false );
			}

			cancellationToken.ThrowIfCancellationRequested();

			now = this._clock.GetTimestamp();
			observedElapsed = this._clock.GetElapsedTime(
				started,
				now
			);

			sequence = ApplyMissedTickPolicy(
				sequence,
				interval,
				observedElapsed,
				fireImmediately,
				missedTickPolicy
			);
			scheduledElapsed = GetScheduledElapsed(
				sequence,
				interval,
				fireImmediately
			);

			yield return new PeriodicTick(
				sequence++,
				scheduledElapsed,
				observedElapsed
			);
		}
	}

	private static long ApplyMissedTickPolicy(
		long sequence,
		TimeSpan interval,
		TimeSpan observedElapsed,
		bool fireImmediately,
		PeriodicMissedTickPolicy missedTickPolicy
	) {
		if ( PeriodicMissedTickPolicy.CatchUp == missedTickPolicy ) {
			return sequence;
		}

		var elapsedIntervals = observedElapsed.Ticks / interval.Ticks;
		var sequenceOffset = fireImmediately
			? 0L
			: 1L
		;
		var latestDueSequence = elapsedIntervals - sequenceOffset;

		return latestDueSequence > sequence
			? latestDueSequence
			: sequence
		;
	}

	private static TimeSpan GetScheduledElapsed(
		long sequence,
		TimeSpan interval,
		bool fireImmediately
	) {
		var sequenceOffset = fireImmediately
			? 0L
			: 1L
		;
		var multiplier = checked(
			sequence + sequenceOffset
		);

		return TimeSpan.FromTicks(
			checked(
				interval.Ticks * multiplier
			)
		);
	}
}
