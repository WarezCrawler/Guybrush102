using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RuntimeGC;

internal static class UIUtil
{
	public const float MarginLarge = 0f;

	public const float MarginHorizontal = 5f;

	public const float MarginVertical = 5f;

	private static int resetFlags;

	private static int resetFlagPtr;

	private static MainButtonWorker worker;

	public static string MMIntervalButtonLabelCache;

	public static MainButtonWorker MainButtonWorker
	{
		get
		{
			if (worker == null)
			{
				worker = DefDatabase<MainButtonDef>.GetNamed("RGC_UI", true).Worker;
			}
			return worker;
		}
	}

	public static float DrawSectionLabel(float x, float y, string text, float xMax)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		Text.Font = (GameFont)2;
		Vector2 val = Text.CalcSize(text);
		Rect val2 = default(Rect);
		val2 = new(x + 0f, y + 0f, val.x, val.y);
		Widgets.Label(val2, text);
		Color color = GUI.color;
		GUI.color = Color.gray;
		Widgets.DrawLineHorizontal(val2.xMax + 5f, val2.y + val2.height / 2f, xMax - val2.xMax - 5f);
		Text.Font = (GameFont)1;
		GUI.color = color;
		return val2.yMax + 5f;
	}

	public static void BeginRestartCheck()
	{
		resetFlagPtr = 0;
	}

	public static void DrawCheckboxRestartIfApplied(Rect rect, string label, string tip, ref bool checkOn)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected O, but got Unknown
		if ((resetFlags & (1 << resetFlagPtr)) > 0)
		{
			checkOn = !checkOn;
			resetFlags ^= 1 << resetFlagPtr;
		}
		resetFlagPtr++;
		bool flag = checkOn;
		Widgets.CheckboxLabeled(rect, label, ref checkOn, false, (Texture2D)null, (Texture2D)null, false, false);
		TooltipHandler.TipRegion(rect, (tip));
		if (flag == checkOn)
		{
			return;
		}
		int i = resetFlagPtr - 1;
		if ((RuntimeGC.Settings.restartFlags & (1 << i)) > 0)
		{
			RuntimeGC.Settings.restartFlags ^= 1 << i;
			return;
		}
		Dialog_MessageBox val = new Dialog_MessageBox(Translator.Translate("DlgTextRestartNotice"), (Translator.Translate("OK")), (Action)delegate
		{
			RuntimeGC.Settings.restartFlags |= 1 << i;
		}, (Translator.Translate("UndoChange")), (Action)delegate
		{
			resetFlags |= 1 << i;
		}, (Translator.Translate("DlgTitleRestart")), false, (Action)null, (Action)null, (WindowLayer)1);
		Find.WindowStack.Add((Window)(object)val);
	}

	public static void Notify_MMBtnLabelChanged()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		int memoryMonitorUpdateInterval = RuntimeGC.Settings.MemoryMonitorUpdateInterval;
		TaggedString val;
		if (!Enum.IsDefined(typeof(MemoryMonitorUpdateMode), memoryMonitorUpdateInterval))
		{
			val = TranslatorFormattedStringExtensions.Translate("UITicks", (memoryMonitorUpdateInterval));
		}
		else
		{
			MemoryMonitorUpdateMode memoryMonitorUpdateMode = (MemoryMonitorUpdateMode)memoryMonitorUpdateInterval;
			val = Translator.Translate("MMUpdate_" + memoryMonitorUpdateMode);
		}
		MMIntervalButtonLabelCache = (val);
	}
}
