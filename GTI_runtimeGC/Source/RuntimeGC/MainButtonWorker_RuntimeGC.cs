using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RuntimeGC;

public class MainButtonWorker_RuntimeGC : MainButtonWorker_ToggleTab
{
	private static bool enableBar;

	private static int memoryBarLowerMb;

	private static int memoryBarStepMb;

	private static bool enableTip;

	public static int updateInterval = 600;

	public static bool onScreenMemUsage = false;

	internal static string TabDescriptionTranslated;

	internal static string MMTipTranslated;

	private static float progress = 0f;

	private static string tipCache = "";

	private static int updatetick = 32767;

	private static string labelCache = "";

	internal static void UpdateSettings(RuntimeGCSettings settings)
	{
		enableBar = settings.EnableMemoryMonitorBar;
		enableTip = settings.EnableMemoryMonitorTip;
		memoryBarLowerMb = settings.MemoryMonitorBarLowerBoundMb;
		memoryBarStepMb = settings.MemoryMonitorBarUpperBoundMb - memoryBarLowerMb;
		updateInterval = settings.MemoryMonitorUpdateInterval;
		onScreenMemUsage = settings.DevOnScreenMemoryUsage;
		progress = 0f;
		tipCache = "";
		updatetick = 32767;
	}

	internal static void Notify_UpdateIntervalChanged(int newint)
	{
		updateInterval = newint;
		updatetick = 32767;
	}

	public override void DoButton(Rect rect)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Invalid comparison between Unknown and I4
		Text.Font = (GameFont)1;
		string text = (((Def)((MainButtonWorker)this).def).LabelCap);
		float num = ((MainButtonWorker)this).def.LabelCapWidth;
		if (num > rect.width - 2f)
		{
			text = ((MainButtonWorker)this).def.ShortenedLabelCap;
			num = ((MainButtonWorker)this).def.ShortenedLabelCapWidth;
		}
		if (enableBar || enableTip || onScreenMemUsage)
		{
			updatetick++;
			if (updatetick > updateInterval)
			{
				updatetick = 0;
				long num2 = GC.GetTotalMemory(forceFullCollection: false) / 1024;
				float num3 = (float)num2 / 1024f;
				if (enableTip)
				{
					tipCache = string.Format(MMTipTranslated, num3);
				}
				if (enableBar)
				{
					progress = Mathf.Clamp01((num3 - (float)memoryBarLowerMb) / (float)memoryBarStepMb);
				}
				if (onScreenMemUsage)
				{
					labelCache = $"{num3:F2} Mb\n{num2} Kb";
				}
			}
		}
		bool flag = num > 0.85f * rect.width - 1f;
		string text2 = (onScreenMemUsage ? labelCache : text);
		float num4 = ((!flag) ? (-1f) : 2f);
		if (Widgets.ButtonTextSubtle(rect, text2, progress, num4, SoundDefOf.Mouseover_Category, default(Vector2), (Color?)null, false) && (int)Current.ProgramState == 2)
		{
			((MainButtonWorker)this).InterfaceTryActivate();
		}
		TooltipHandler.TipRegion(rect, (TabDescriptionTranslated + tipCache));
	}
}
