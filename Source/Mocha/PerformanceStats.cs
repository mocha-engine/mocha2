namespace Mocha;

public static class PerformanceStats
{
	record FrameAverage
	{
		private TimeSince _timeSinceAverageCalculated = 0;
		private List<double> frameDeltas = new();

		public Action? OnAverageCalculated;
		public double AverageDelta { get; private set; }

		public void OnFrame( double deltaTime )
		{
			if ( _timeSinceAverageCalculated > 1 )
			{
				AverageDelta = (frameDeltas.Count > 0) ? frameDeltas.Average() : 0;	
				frameDeltas.Clear();
				_timeSinceAverageCalculated = 0;

				OnAverageCalculated?.Invoke();
			}
			
			frameDeltas.Add( deltaTime );
		}
	}
	
	private static FrameAverage s_frameAverage = new();
	public static int AverageFPS
	{
		get
		{
			double avgDelta = s_frameAverage.AverageDelta;
			return avgDelta != 0 ? (int)Math.Round( 1.0d / avgDelta ) : 0;
		}
	}

	public static double AverageDelta
	{
		get => s_frameAverage.AverageDelta;
	}

	public static Action? OnAverageCalculated
	{
		get => s_frameAverage.OnAverageCalculated;
		set => s_frameAverage.OnAverageCalculated = value;
	}

	internal static void OnFrame( double deltaTime )
	{
		s_frameAverage.OnFrame( deltaTime );
	}
}
