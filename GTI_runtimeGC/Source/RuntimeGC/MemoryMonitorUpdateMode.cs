namespace RuntimeGC;

internal enum MemoryMonitorUpdateMode
{
	Debug_Flash = 1,
	Debug_Realtime = 15,
	PerSecond = 60,
	UltraFrequent = 150,
	Frequent = 300,
	Moderate = 600,
	PerMinuteQuarter = 900,
	PerMinuteHalf = 1800,
	PerMinute = 3600,
	Debug_Lazy = 18000,
	Debug_frozen = int.MaxValue
}
