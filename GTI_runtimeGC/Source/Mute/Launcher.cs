using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld.Planet;
using Verse;

namespace Mute;

public static class Launcher
{
	public static void WorldPawnGCTick()
	{
	}

	public static void Add(LogEntry entry)
	{
	}

	public static void ExposeData()
	{
		List<Battle> list = new List<Battle>();
		Scribe_Collections.Look<Battle>(ref list, "battles", (LookMode)2, Array.Empty<object>());
	}

	public static void Launch(bool doGC, bool doBL)
	{
		BindingFlags bflag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		BindingFlags bflag2 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		if (doGC)
		{
			LongEventHandler.QueueLongEvent((Action)delegate
			{
				DoDetour(typeof(WorldPawnGC).GetMethod("WorldPawnGCTick", bflag), typeof(Launcher).GetMethod("WorldPawnGCTick", bflag2));
				Log.Message("[RuntimeGC] Detour completed: MuteGC");
			}, "Initializing", false, (Action<Exception>)null, true);
		}
		if (doBL)
		{
			LongEventHandler.QueueLongEvent((Action)delegate
			{
				DoDetour(typeof(BattleLog).GetMethod("Add", bflag), typeof(Launcher).GetMethod("Add", bflag2));
				DoDetour(typeof(BattleLog).GetMethod("ExposeData", bflag), typeof(Launcher).GetMethod("ExposeData", bflag2));
				Log.Message("[RuntimeGC] Detour completed: MuteBL");
			}, "Initializing", false, (Action<Exception>)null, true);
		}
	}

	public unsafe static bool DoDetour(MethodInfo source, MethodInfo destination)
	{
		if (IntPtr.Size == 8)
		{
			byte* ptr = (byte*)source.MethodHandle.GetFunctionPointer().ToInt64();
			long num = destination.MethodHandle.GetFunctionPointer().ToInt64();
			byte* ptr2 = ptr;
			long* ptr3 = (long*)(ptr2 + 2);
			*ptr2 = 72;
			ptr2[1] = 184;
			*ptr3 = num;
			ptr2[10] = byte.MaxValue;
			ptr2[11] = 224;
		}
		else
		{
			int num2 = source.MethodHandle.GetFunctionPointer().ToInt32();
			int num3 = destination.MethodHandle.GetFunctionPointer().ToInt32();
			byte* ptr4 = (byte*)num2;
			int* ptr5 = (int*)(ptr4 + 1);
			int num4 = num3 - num2 - 5;
			*ptr4 = 233;
			*ptr5 = num4;
		}
		return true;
	}
}
