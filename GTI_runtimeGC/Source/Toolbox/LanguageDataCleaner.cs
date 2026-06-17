using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using RuntimeGC;
using Verse;

namespace Toolbox;

public static class LanguageDataCleaner
{
	private static int cacheLanguageDataCount = -1;

	public static bool Cleaned => cacheLanguageDataCount >= LanguageDatabase.AllLoadedLanguages.Count();

	public static void CleanLanguageData()
	{
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Invalid comparison between Unknown and I4
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		FieldInfo field = typeof(LanguageDatabase).GetField("languages", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		List<LoadedLanguage> list = (List<LoadedLanguage>)field.GetValue(null);
		int num = 0;
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		for (int num3 = list.Count - 1; num3 > -1; num3--)
		{
			if (list[num3] != LanguageDatabase.activeLanguage && list[num3] != LanguageDatabase.defaultLanguage)
			{
				GenText.AppendWithComma(stringBuilder, list[num3].FriendlyNameNative);
				list.RemoveAt(num3);
				num++;
			}
			else
			{
				num2 += list[num3].defInjections.Count;
				list[num3].defInjections = new List<DefInjectionPackage>();
			}
		}
		RGCLog.Msg("[LanguageDataCleaner] Removed " + num + " LoadedLanguages and cleaned " + num2 + " DefInjectionPackages.\nRemoved Languages: " + stringBuilder.ToString());
		if ((int)Current.ProgramState == 2)
		{
			Messages.Message((TranslatorFormattedStringExtensions.Translate("MsgLanguageDataCleaned", (num), (num2))), MessageTypeDefOf.PositiveEvent, false);
		}
		field.SetValue(null, list);
		cacheLanguageDataCount = list.Count;
		GC.Collect(2);
	}
}
