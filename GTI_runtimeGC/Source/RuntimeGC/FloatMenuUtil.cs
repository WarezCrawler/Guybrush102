using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Toolbox;
using UnityEngine;
using Verse;

namespace RuntimeGC;

public static class FloatMenuUtil
{
	private static readonly Dictionary<string, List<string>> groups;

	private static readonly Dictionary<string, bool> devOnly;

	private static readonly Dictionary<string, Action> items;

	public static readonly string GroupTools;

	public static readonly string GroupFix;

	public static readonly string GroupQuickbar;

	public static readonly string GroupMMUpdateMode;

	static FloatMenuUtil()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		GroupTools = "tools";
		GroupFix = "fix";
		GroupQuickbar = "quickbar";
		GroupMMUpdateMode = "mmupdate";
		groups = new Dictionary<string, List<string>>();
		items = new Dictionary<string, Action>();
		devOnly = new Dictionary<string, bool>();
		string groupFix = GroupFix;
		Add(groupFix, (Translator.Translate("FloatDebugLog")), delegate
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Expected O, but got Unknown
			if (!Find.WindowStack.TryRemove(typeof(EditWindow_Log), true))
			{
				Find.WindowStack.Add((Window)new EditWindow_Log());
			}
		});
		Add(groupFix, (Translator.Translate("FloatAGRegen")), delegate
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			foreach (Map map in Find.Maps)
			{
				map.avoidGrid.Regenerate();
			}
			Message((Translator.Translate("MsgTextAGR")), MessageTypeDefOf.PositiveEvent);
		});
		Add(groupFix, (Translator.Translate("FloatFactionFixItems1")), delegate
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			CleanserUtil.FixFactionRelationships();
			Message((Translator.Translate("MsgTextFFR")), MessageTypeDefOf.PositiveEvent);
		});
		Add(groupFix, (Translator.Translate("FloatFactionFixItems2")), delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextFFL", (CleanserUtil.FixFactionLeader_Wrapped()))), MessageTypeDefOf.PositiveEvent);
		});
		groupFix = GroupTools;
		Add(groupFix, (Translator.Translate("FloatToolsItems1")), delegate
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			int num = CleanserUtil.DeconstructAnimalFamily();
			Log.Message("CleanserUtil.DeconstructAnimalFamily():Round 1 completed.");
			CleanserUtil.DeconstructAnimalFamily();
			Log.Message("CleanserUtil.DeconstructAnimalFamily():Round 2 completed.");
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextAFT", (num))), MessageTypeDefOf.PositiveEvent);
		});
		Add(groupFix, (Translator.Translate("FloatToolsItems2")), delegate
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextRFH", (CleanserUtil.RemoveFilth(Find.CurrentMap, homearea: true)))), MessageTypeDefOf.PositiveEvent);
		});
		Add(groupFix, (Translator.Translate("FloatToolsItems2Dev1")), delegate
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextRFM", (CleanserUtil.RemoveFilth(Find.CurrentMap, homearea: false)))), MessageTypeDefOf.PositiveEvent);
		}, devonly: true);
		Add(groupFix, (Translator.Translate("FloatToolsItems2Dev2")), delegate
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			int num = 0;
			foreach (Map map2 in Find.Maps)
			{
				num += CleanserUtil.RemoveFilth(map2, homearea: false);
			}
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextRFW", (num))), MessageTypeDefOf.PositiveEvent);
		}, devonly: true);
		Add(groupFix, (Translator.Translate("FloatToolsItems2Snow")), delegate
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			CleanserUtil.RemoveSnow(Find.CurrentMap, homearea: true);
			Message((Translator.Translate("MsgTextRSH")), MessageTypeDefOf.PositiveEvent);
		});
		Add(groupFix, (Translator.Translate("FloatToolsItems2SnowDev1")), delegate
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			CleanserUtil.RemoveSnow(Find.CurrentMap, homearea: false);
			Message((Translator.Translate("MsgTextRSM")), MessageTypeDefOf.PositiveEvent);
		}, devonly: true);
		Add(groupFix, (Translator.Translate("FloatToolsItems2SnowDev2")), delegate
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			foreach (Map map3 in Find.Maps)
			{
				CleanserUtil.RemoveSnow(map3, homearea: false);
			}
			Message((Translator.Translate("MsgTextRSW")), MessageTypeDefOf.PositiveEvent);
		}, devonly: true);
		Add(groupFix, (Translator.Translate("FloatToolsItems3")), delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextRCM", (CleanserUtil.RemoveCorpses()))), MessageTypeDefOf.NeutralEvent);
		});
		Add(groupFix, (Translator.Translate("FloatToolsItems4")), delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextRBL", (CleanserUtil.RemoveAllBattleLogEntries()))), MessageTypeDefOf.PositiveEvent);
		});
		Add(groupFix, (Translator.Translate("FloatToolsItems5")), delegate
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextRAM", (CleanserUtil.RemoveIArchivable(removePinned: false)))), MessageTypeDefOf.PositiveEvent);
		});
		Add(groupFix, (Translator.Translate("FloatToolsItems5Dev")), delegate
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			Message((TranslatorFormattedStringExtensions.Translate("MsgTextRAM", (CleanserUtil.RemoveIArchivable(removePinned: true)))), MessageTypeDefOf.PositiveEvent);
		}, devonly: true);
		groupFix = GroupQuickbar;
		Add(groupFix, (Translator.Translate("QuickCloseLetStack")), delegate
		{
			LetterStack letterStack = Find.LetterStack;
			if (letterStack != null)
			{
				for (int num = letterStack.LettersListForReading.Count - 1; num > -1; num--)
				{
					letterStack.RemoveLetter(letterStack.LettersListForReading[num]);
				}
			}
		});
		Add(groupFix, (Translator.Translate("QuickUnlockSpeedLimit")), delegate
		{
			CleanserUtil.UnlockNormalSpeedLimit();
			Find.WindowStack.TryRemove(typeof(UserInterface), true);
		});
		Add(groupFix, (Translator.Translate("QuickOpenSettings")), delegate
		{
			CleanserUtil.OpenModSettingsPage();
		});
		groupFix = GroupMMUpdateMode;
		foreach (MemoryMonitorUpdateMode m in Enum.GetValues(typeof(MemoryMonitorUpdateMode)))
		{
			Add(groupFix, (Translator.Translate("MMUpdate_" + m)), delegate
			{
				RuntimeGC.Settings.MemoryMonitorUpdateInterval = (int)m;
				UIUtil.Notify_MMBtnLabelChanged();
			}, m.ToString().Contains("Debug_"));
		}
	}

	public static void Add(string group, string label, Action action, bool devonly = false)
	{
		if (!groups.ContainsKey(group))
		{
			groups.Add(group, new List<string>());
		}
		groups[group].Add(label);
		items.Add(label, action);
		devOnly.Add(label, devonly);
	}

	public static void GenerateFloatMenuGroup(string group)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (string item in groups[group])
		{
			if (!devOnly[item] || Prefs.DevMode)
			{
				list.Add(new FloatMenuOption(item, items[item], (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
			}
		}
		Find.WindowStack.Add((Window)new FloatMenu(list));
	}

	public static void GenerateMemoryReclaimOptions()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Expected O, but got Unknown
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		// GTI fix: was wired to Toolbox.Toolbox.CleanModMetaData, an unused stub that
		// throws NotImplementedException. Point at the real implementation instead,
		// matching the LanguageData/DefPackage options below.
		FloatMenuOption val = new FloatMenuOption((Translator.Translate("FloatACModMetaData")), (Action)ModMetaDataCleaner.CleanModMetaData, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0);
		if (ModMetaDataCleaner.Cleaned)
		{
			val.Label = (Translator.Translate("FloatACModMetaDataCleared"));
			val.Disabled = true;
		}
		list.Add(val);
		val = new FloatMenuOption((Translator.Translate("FloatACLanguageData")), (Action)LanguageDataCleaner.CleanLanguageData, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0);
		if (LanguageDataCleaner.Cleaned)
		{
			val.Label = (Translator.Translate("FloatACLanguageDataCleared"));
			val.Disabled = true;
		}
		list.Add(val);
		val = new FloatMenuOption((Translator.Translate("FloatACDefPackage")), (Action)DefPackageCleaner.CleanDefPackage, (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0);
		if (DefPackageCleaner.Cleaned)
		{
			val.Label = (Translator.Translate("FloatACDefPackageCleared"));
			val.Disabled = true;
		}
		list.Add(val);
		Find.WindowStack.Add((Window)new FloatMenu(list));
	}

	public static void Message(string str, MessageTypeDef type)
	{
		Messages.Message(str, type, RuntimeGC.Settings.ArchiveMessageGeneral);
	}
}
