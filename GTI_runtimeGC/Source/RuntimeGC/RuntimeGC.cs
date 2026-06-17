using System;
using UnityEngine;
using Verse;

namespace RuntimeGC;

public class RuntimeGC : Mod
{
	public static RuntimeGCSettings Settings;

	public RuntimeGC(ModContentPack content)
		: base(content)
	{
		Settings = ((Mod)this).GetSettings<RuntimeGCSettings>();
	}

	public override string SettingsCategory()
	{
		return "RuntimeGC";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0524: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0616: Unknown result type (might be due to invalid IL or missing references)
		//IL_0656: Unknown result type (might be due to invalid IL or missing references)
		//IL_0671: Unknown result type (might be due to invalid IL or missing references)
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		//IL_0687: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0701: Unknown result type (might be due to invalid IL or missing references)
		//IL_0708: Unknown result type (might be due to invalid IL or missing references)
		//IL_0717: Unknown result type (might be due to invalid IL or missing references)
		//IL_0760: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0808: Unknown result type (might be due to invalid IL or missing references)
		//IL_080f: Unknown result type (might be due to invalid IL or missing references)
		//IL_081e: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0869: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0916: Unknown result type (might be due to invalid IL or missing references)
		//IL_091d: Unknown result type (might be due to invalid IL or missing references)
		//IL_093c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0943: Unknown result type (might be due to invalid IL or missing references)
		//IL_0948: Unknown result type (might be due to invalid IL or missing references)
		//IL_0996: Unknown result type (might be due to invalid IL or missing references)
		//IL_099d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_040f: Unknown result type (might be due to invalid IL or missing references)
		GUI.BeginGroup(inRect);
		float num = UIUtil.DrawSectionLabel(inRect.x, inRect.y, (Translator.Translate("SettingsMMCategory")), inRect.xMax);
		string text = (Translator.Translate("SettingsMMTipLabel"));
		float num2 = inRect.width / 2f - 0f - 20f;
		float num3 = Text.CalcHeight(text, num2);
		Rect val = new(inRect.x + 0f + 5f, num, inRect.width - 0f - 10f, num3 * 4f + 15f + (Prefs.DevMode ? (num3 + 5f) : 0f));
		GUI.BeginGroup(val);
		Rect val2 = new(0f, 0f, num2 + 5f, num3);
		bool enableMemoryMonitorTip = Settings.EnableMemoryMonitorTip;
		Widgets.CheckboxLabeled(val2, text, ref enableMemoryMonitorTip, false, (Texture2D)null, (Texture2D)null, false, false);
		Settings.EnableMemoryMonitorTip = enableMemoryMonitorTip;
		TooltipHandler.TipRegion(val2, (Translator.Translate("SettingsMMTipTip")));
		val2 = new(val2.x, val2.yMax + 5f, num2 + 5f, num3);
		enableMemoryMonitorTip = Settings.EnableMemoryMonitorBar;
		Widgets.CheckboxLabeled(val2, (Translator.Translate("SettingsMMBarLabel")), ref enableMemoryMonitorTip, false, (Texture2D)null, (Texture2D)null, false, false);
		Settings.EnableMemoryMonitorBar = enableMemoryMonitorTip;
		TooltipHandler.TipRegion(val2, (Translator.Translate("SettingsMMBarTip")));
		if (Settings.EnableMemoryMonitorBar)
		{
			Rect val3 = new(5f, val2.yMax + 5f, num2, num3);
			Widgets.Label(val3, Translator.Translate("SettingsMMRangeLabel"));
			TooltipHandler.TipRegion(val3, (Translator.Translate("SettingsMMRangeTip")));
			float x = Text.CalcSize("x32").x;
			Rect val4 = new(val3.xMax - x * 2f - 5f - 5f, val3.y, x, num3);
			if (Mouse.IsOver(val4))
			{
				GUI.color = Color.cyan;
			}
			else
			{
				GUI.color = Color.gray;
			}
			Widgets.Label(val4, "x32");
			Widgets.DrawLineHorizontal(val4.x, val4.yMax - 1f, val4.width);
			GUI.color = Color.white;
			if (Widgets.ButtonInvisible(val4, true))
			{
				Settings.MemoryMonitorBarLowerBoundMb = 0;
				Settings.MemoryMonitorBarUpperBoundMb = 1024;
			}
			Rect val5 = new(val3.xMax - x - 5f, val3.y, x, num3);
			if (Mouse.IsOver(val5) && IntPtr.Size == 8)
			{
				GUI.color = Color.cyan;
			}
			else
			{
				GUI.color = Color.gray;
			}
			Widgets.Label(val5, "x64");
			if (IntPtr.Size == 8)
			{
				Widgets.DrawLineHorizontal(val5.x, val5.yMax - 1f, val5.width);
				if (Widgets.ButtonInvisible(val5, true))
				{
					Settings.MemoryMonitorBarLowerBoundMb = 0;
					Settings.MemoryMonitorBarUpperBoundMb = 2048;
				}
			}
			else
			{
				Widgets.DrawLine(new Vector2(val5.x, val5.y), new Vector2(val5.xMax, val5.yMax), Color.gray, 1f);
			}
			GUI.color = Color.white;
			Rect val6 = new(val3.x, val3.yMax + 5f, num2, num3);
			IntRange val7 = new(Settings.MemoryMonitorBarLowerBoundMb, Settings.MemoryMonitorBarUpperBoundMb);
			Widgets.IntRange(val6, 233, ref val7, 0, 8192, (string)null, 0);
			Settings.MemoryMonitorBarLowerBoundMb = val7.min;
			Settings.MemoryMonitorBarUpperBoundMb = val7.max;
		}
		Rect val8 = new(val.width / 2f + (val.width / 2f - 130f) / 2f, (num3 * 2f + 5f - 35f) / 2f, 130f, 35f);
		UIUtil.MainButtonWorker.DoButton(val8);
		Rect val9 = new(val.width / 2f + 10f, num3 * 2f + 10f + (35f - num3) / 2f, val.width / 2f - 5f - 125f, num3);
		Widgets.Label(val9, Translator.Translate("SettingsMMIntervalLabel"));
		TooltipHandler.TipRegion(val9, (Translator.Translate("SettingsMMIntervalTip")));
		Rect val10 = new(val.xMax - 125f - 10f, num3 * 2f + 10f, 125f, 35f);
		if (Widgets.ButtonText(val10, UIUtil.MMIntervalButtonLabelCache, true, true, true, (TextAnchor?)null))
		{
			FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupMMUpdateMode);
		}
		if (Prefs.DevMode)
		{
			Rect val11 = default(Rect);
			val11 = new(val.width / 2f + 10f, val10.yMax + 5f, val.width / 2f - 15f, num3);
			enableMemoryMonitorTip = Settings.DevOnScreenMemoryUsage;
			Widgets.CheckboxLabeled(val11, (Translator.Translate("SettingsDevOnScreenMemoryUsageLabel")), ref enableMemoryMonitorTip, false, (Texture2D)null, (Texture2D)null, false, false);
			if (enableMemoryMonitorTip != Settings.DevOnScreenMemoryUsage)
			{
				Settings.DevOnScreenMemoryUsage = enableMemoryMonitorTip;
				Settings.UpdateCache();
			}
		}
		GUI.EndGroup();
		UIUtil.BeginRestartCheck();
		num = UIUtil.DrawSectionLabel(inRect.x, val.yMax + num3, (Translator.Translate("SettingsAutoCleanupCategory")), inRect.width / 2f - 5f);
		Rect val12 = new(val.x, num, num2, num3 * 3f + 10f);
		GUI.BeginGroup(val12);
		val2 = new(0f, 0f, num2, num3);
		UIUtil.DrawCheckboxRestartIfApplied(val2, (Translator.Translate("SettingsACModMetaDataLabel")), (Translator.Translate("SettingsACModMetaDataTip")), ref Settings.AutoCleanModMetaData);
		val2 = new(0f, num3 + 5f, num2, num3);
		UIUtil.DrawCheckboxRestartIfApplied(val2, (Translator.Translate("SettingsACLanguageDataLabel")), (Translator.Translate("SettingsACLanguageDataTip")), ref Settings.AutoCleanLanguageData);
		val2 = new(0f, num3 * 2f + 10f, num2, num3);
		UIUtil.DrawCheckboxRestartIfApplied(val2, (Translator.Translate("SettingsACDefPackageLabel")), (Translator.Translate("SettingsACDefPackageTip")), ref Settings.AutoCleanDefPackage);
		GUI.EndGroup();
		num = UIUtil.DrawSectionLabel(val.x + val.width / 2f + 5f, val.yMax + num3, (Translator.Translate("SettingsMuteCategory")), inRect.xMax);
		Rect val13 = new(val.x + val.width / 2f + 10f, num, num2, num3 * 2f + 5f);
		GUI.BeginGroup(val13);
		val2 = new(0f, 0f, num2, num3);
		UIUtil.DrawCheckboxRestartIfApplied(val2, (Translator.Translate("SettingsMuteGCLabel")), (Translator.Translate("SettingsMuteGCTip")), ref Settings.DoMuteGC);
		val2 = new(0f, num3 + 5f, num2, num3);
		UIUtil.DrawCheckboxRestartIfApplied(val2, (Translator.Translate("SettingsMuteBLLabel")), (Translator.Translate("SettingsMuteBLTip")), ref Settings.DoMuteBL);
		GUI.EndGroup();
		num = UIUtil.DrawSectionLabel(inRect.x, val12.yMax + num3 + (Prefs.DevMode ? (num3 + 5f) : 0f), (Translator.Translate("SettingsGeneralCategory")), inRect.width / 2f - 5f);
		Rect val14 = new(val.x, num, num2, num3 * 3f + 10f);
		GUI.BeginGroup(val14);
		val2 = new(0f, 0f, num2, num3);
		Widgets.CheckboxLabeled(val2, (Translator.Translate("SettingsArchiveGCLabel")), ref Settings.ArchiveGCDialog, false, (Texture2D)null, (Texture2D)null, false, false);
		TooltipHandler.TipRegion(val2, (Translator.Translate("SettingsArchiveGCTip")));
		val2 = new(0f, num3 + 5f, num2, num3);
		Widgets.CheckboxLabeled(val2, (Translator.Translate("SettingsArchiveGeneralLabel")), ref Settings.ArchiveMessageGeneral, false, (Texture2D)null, (Texture2D)null, false, false);
		TooltipHandler.TipRegion(val2, (Translator.Translate("SettingsArchiveGeneralTip")));
		GUI.EndGroup();
		Rect val15 = new(inRect.xMax - 5f - 135f - 50f, inRect.yMax - 35f - 65f, 135f, 35f);
		if (Widgets.ButtonText(val15, (Translator.Translate("SettingsReset")), true, true, true, (TextAnchor?)null))
		{
			Settings.ResetToDefault();
		}
		GUI.EndGroup();
	}
}
