using Icod.Timing;

IMonotonicClock clock = SystemMonotonicClock.Instance;

Console.WriteLine( "Icod.Timing sample" );
Console.WriteLine();
Console.WriteLine( "Monotonic delay" );
Console.WriteLine( "---------------" );

TimeSpan requestedDelay = TimeSpan.FromMilliseconds( 100 );
long started = clock.GetTimestamp();

await clock.DelayAsync(
	requestedDelay
);

TimeSpan observedDelay = clock.GetElapsedTime(
	started,
	clock.GetTimestamp()
);

Console.WriteLine(
	$"Requested: {requestedDelay.TotalMilliseconds:F0} ms"
);
Console.WriteLine(
	$"Observed:  {observedDelay.TotalMilliseconds:F1} ms"
);

Console.WriteLine();
Console.WriteLine( "Cancellation" );
Console.WriteLine( "------------" );

using ( var cancellation = new CancellationTokenSource() ) {
	cancellation.CancelAfter(
		TimeSpan.FromMilliseconds( 50 )
	);

	try {
		await clock.DelayAsync(
			TimeSpan.FromSeconds( 5 ),
			cancellation.Token
		);
	} catch ( OperationCanceledException ) when ( cancellation.IsCancellationRequested ) {
		Console.WriteLine( "A five-second delay was canceled after about 50 ms." );
	}
}

Console.WriteLine();
Console.WriteLine( "Periodic schedule" );
Console.WriteLine( "-----------------" );
Console.WriteLine( "The default policy skips overdue schedule positions." );

await foreach ( PeriodicTick tick in MonotonicPeriodicScheduler.Instance.ScheduleAsync(
	TimeSpan.FromMilliseconds( 500 ),
	fireImmediately: true,
	missedTickPolicy: PeriodicMissedTickPolicy.SkipMissed
) ) {
	Console.WriteLine(
		$"tick {tick.Sequence}: scheduled={tick.ScheduledElapsed.TotalSeconds:F3}s, "
		+ $"late={tick.Lateness.TotalMilliseconds:F1}ms"
	);

	if ( 1 == tick.Sequence ) {
		Console.WriteLine(
			"Simulating 1.1 seconds of work so at least one schedule position is missed."
		);
		await clock.DelayAsync(
			TimeSpan.FromMilliseconds( 1100 )
		);
	}

	if ( 5 <= tick.Sequence ) {
		break;
	}
}

return 0;
