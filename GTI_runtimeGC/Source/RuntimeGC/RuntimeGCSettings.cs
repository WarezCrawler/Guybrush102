using System;
using Verse;

namespace RuntimeGC;

public class RuntimeGCSettings : ModSettings
{
	private bool EnableMemoryUsageBar;

	private int MemoryUsageBarLowerBoundMb;

	private int MemoryUsageBarUpperBoundMb;

	private int MemoryUsageUpdateInterval;

	private bool EnableMemoryUsageTip;

	public bool AutoCleanModMetaData;

	public bool AutoCleanLanguageData;

	public bool AutoCleanDefPackage;

	public bool DoMuteGC;

	public bool DoMuteBL;

	public bool ArchiveGCDialog;

	public bool ArchiveMessageGeneral;

	public bool DevOnScreenMemoryUsage;

	// GTI: gates all optional "[RuntimeGC]" diagnostics via RGCLog. Defaults on
	// during the fork's bug-hunting phase. (Errors always log regardless.)
	public bool DebugLogging;

	internal int restartFlags = 0;

	public bool EnableMemoryMonitorBar
	{
		get
		{
			return EnableMemoryUsageBar;
		}
		set
		{
			if (EnableMemoryUsageBar != value)
			{
				EnableMemoryUsageBar = value;
				UpdateCache();
			}
		}
	}

	public int MemoryMonitorBarLowerBoundMb
	{
		get
		{
			return MemoryUsageBarLowerBoundMb;
		}
		set
		{
			if (MemoryUsageBarLowerBoundMb != value)
			{
				MemoryUsageBarLowerBoundMb = value;
				UpdateCache();
			}
		}
	}

	public int MemoryMonitorBarUpperBoundMb
	{
		get
		{
			return MemoryUsageBarUpperBoundMb;
		}
		set
		{
			if (MemoryUsageBarUpperBoundMb != value)
			{
				MemoryUsageBarUpperBoundMb = value;
				UpdateCache();
			}
		}
	}

	public int MemoryMonitorUpdateInterval
	{
		get
		{
			return MemoryUsageUpdateInterval;
		}
		set
		{
			if (MemoryUsageUpdateInterval != value)
			{
				MemoryUsageUpdateInterval = value;
				MainButtonWorker_RuntimeGC.Notify_UpdateIntervalChanged(value);
			}
		}
	}

	public bool EnableMemoryMonitorTip
	{
		get
		{
			return EnableMemoryUsageTip;
		}
		set
		{
			if (EnableMemoryUsageTip != value)
			{
				EnableMemoryUsageTip = value;
				UpdateCache();
			}
		}
	}

	public Mod Mod
	{
		get
		{
			RGCLog.Warn("RuntimeGCSettings.get_Mod() is called!");
			return (Mod)(object)LoadedModManager.GetMod<RuntimeGC>();
		}
		set
		{
		}
	}

	public RuntimeGCSettings()
	{
		Init();
		UpdateCache();
	}

	public void Init()
	{
		InitMemoryMonitor();
		AutoCleanModMetaData = true;
		AutoCleanLanguageData = true;
		AutoCleanDefPackage = false;
		DoMuteGC = true;
		DoMuteBL = false;
		ArchiveGCDialog = true;
		ArchiveMessageGeneral = false;
		DevOnScreenMemoryUsage = false;
		DebugLogging = true;
	}

	public void InitMemoryMonitor()
	{
		EnableMemoryUsageBar = true;
		MemoryUsageBarLowerBoundMb = 0;
		MemoryUsageBarUpperBoundMb = 1024 * ((IntPtr.Size == 4) ? 1 : 2);
		MemoryUsageUpdateInterval = 600;
		EnableMemoryUsageTip = true;
	}

	public void ResetToDefault()
	{
		if (!AutoCleanModMetaData)
		{
			restartFlags ^= 1;
		}
		if (!AutoCleanLanguageData)
		{
			restartFlags ^= 2;
		}
		if (AutoCleanDefPackage)
		{
			restartFlags ^= 4;
		}
		if (!DoMuteGC)
		{
			restartFlags ^= 8;
		}
		if (DoMuteBL)
		{
			restartFlags ^= 16;
		}
		Init();
		UpdateCache();
		UIUtil.Notify_MMBtnLabelChanged();
	}

	public void UpdateCache()
	{
		MainButtonWorker_RuntimeGC.UpdateSettings(this);
	}

	public override void ExposeData()
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Invalid comparison between Unknown and I4
		Scribe_Values.Look<bool>(ref EnableMemoryUsageBar, "EnableMemoryUsageBar", true, false);
		Scribe_Values.Look<int>(ref MemoryUsageBarLowerBoundMb, "MemoryUsageBarLowerBoundMb", 0, false);
		Scribe_Values.Look<int>(ref MemoryUsageBarUpperBoundMb, "MemoryUsageBarUpperBoundMb", 1024 * ((IntPtr.Size == 4) ? 1 : 2), false);
		Scribe_Values.Look<int>(ref MemoryUsageUpdateInterval, "MemoryUsageUpdateInterval", 600, false);
		Scribe_Values.Look<bool>(ref EnableMemoryUsageTip, "EnableMemoryUsageTip", true, false);
		Scribe_Values.Look<bool>(ref AutoCleanModMetaData, "AutoCleanModMetaData", true, false);
		Scribe_Values.Look<bool>(ref AutoCleanLanguageData, "AutoCleanLanguageData", true, false);
		Scribe_Values.Look<bool>(ref AutoCleanDefPackage, "AutoCleanDefPackage", false, false);
		Scribe_Values.Look<bool>(ref DoMuteGC, "DoMuteGC", true, false);
		Scribe_Values.Look<bool>(ref DoMuteBL, "DoMuteBL", false, false);
		Scribe_Values.Look<bool>(ref ArchiveGCDialog, "ArchiveGCDialog", true, false);
		Scribe_Values.Look<bool>(ref ArchiveMessageGeneral, "ArchiveMessageGeneral", false, false);
		Scribe_Values.Look<bool>(ref DevOnScreenMemoryUsage, "DevOnScreenMemoryUsage", false, false);
		Scribe_Values.Look<bool>(ref DebugLogging, "DebugLogging", true, false);
		if ((int)Scribe.mode == 2)
		{
			if (MemoryUsageBarLowerBoundMb < 0)
			{
				MemoryUsageBarLowerBoundMb = 0;
			}
			if (MemoryUsageBarUpperBoundMb > 1024 * ((IntPtr.Size == 4) ? 4 : 128))
			{
				MemoryUsageBarUpperBoundMb = 1024 * ((IntPtr.Size == 4) ? 4 : 128);
			}
			if (MemoryUsageBarUpperBoundMb <= MemoryUsageBarLowerBoundMb)
			{
				MemoryUsageBarLowerBoundMb = 0;
				MemoryUsageBarUpperBoundMb = 1024 * ((IntPtr.Size == 4) ? 1 : 2);
			}
			UpdateCache();
		}
	}

	public bool RequiresRestart()
	{
		return restartFlags != 0;
	}
}
