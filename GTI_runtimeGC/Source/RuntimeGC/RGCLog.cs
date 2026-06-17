using System;
using System.Diagnostics;
using Verse;

namespace RuntimeGC;

// Central diagnostic-logging gate for the GTI fork. All optional "[RuntimeGC]"
// diagnostics route through here so the whole set can be toggled from the mod
// options (RuntimeGCSettings.DebugLogging).
//
// Defaults ON while the fork is being shaken down for bugs — see FORK_NOTES.md;
// consider flipping the default to off before any wider release.
//
// NOTE: Error() is ALWAYS logged regardless of the toggle. A real failure must
// never be silently hidden — a swallowed exception is exactly what masked the
// original battle-log bug.
internal static class RGCLog
{
	// True before settings have loaded too, so early-startup logging still works.
	public static bool Enabled => RuntimeGC.Settings == null || RuntimeGC.Settings.DebugLogging;

	public static void Msg(string message)
	{
		if (Enabled)
		{
			Log.Message("[RuntimeGC] " + message);
		}
	}

	public static void Warn(string message)
	{
		if (Enabled)
		{
			Log.Warning("[RuntimeGC] " + message);
		}
	}

	// Always logged — failures are never hidden behind the toggle.
	public static void Error(string message)
	{
		Log.Error("[RuntimeGC] " + message);
	}

	// Runs a named tool safely. When debug is on it logs start + finish (with
	// timing); on any exception it logs a clearly-labelled error naming the tool
	// instead of leaving an unlabelled RimWorld stack trace. Returns true on
	// success. Wrap every user-triggered cleanup action with this.
	public static bool Guard(string toolName, Action body)
	{
		Stopwatch sw = Stopwatch.StartNew();
		if (Enabled)
		{
			Log.Message("[RuntimeGC] Running: " + toolName);
		}
		try
		{
			body();
			sw.Stop();
			if (Enabled)
			{
				Log.Message("[RuntimeGC] Done: " + toolName + " (" + sw.ElapsedMilliseconds + " ms)");
			}
			return true;
		}
		catch (Exception ex)
		{
			sw.Stop();
			Log.Error("[RuntimeGC] Tool '" + toolName + "' failed after " + sw.ElapsedMilliseconds + " ms:\n" + ex);
			return false;
		}
	}

	// Value-returning variant for tools that report a count. Returns 'fallback' if
	// the tool throws (and logs the labelled error).
	public static T Guard<T>(string toolName, Func<T> body, T fallback)
	{
		Stopwatch sw = Stopwatch.StartNew();
		if (Enabled)
		{
			Log.Message("[RuntimeGC] Running: " + toolName);
		}
		try
		{
			T result = body();
			sw.Stop();
			if (Enabled)
			{
				Log.Message("[RuntimeGC] Done: " + toolName + " (" + sw.ElapsedMilliseconds + " ms)");
			}
			return result;
		}
		catch (Exception ex)
		{
			sw.Stop();
			Log.Error("[RuntimeGC] Tool '" + toolName + "' failed after " + sw.ElapsedMilliseconds + " ms:\n" + ex);
			return fallback;
		}
	}
}
