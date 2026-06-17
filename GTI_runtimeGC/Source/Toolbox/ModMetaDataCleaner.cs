using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using RuntimeGC;
using Verse;

namespace Toolbox;

public static class ModMetaDataCleaner
{
	private static int cacheMetaDataCount = -1;

	public static bool Cleaned => cacheMetaDataCount >= ModLister.AllInstalledMods.Count();

	public static void CleanModMetaData()
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Invalid comparison between Unknown and I4
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		FieldInfo field = typeof(ModLister).GetField("mods", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		List<ModMetaData> list = (List<ModMetaData>)field.GetValue(null);
		int num = 0;
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		for (int num3 = list.Count - 1; num3 > -1; num3--)
		{
			if (list[num3].Active)
			{
				GenText.AppendWithComma(stringBuilder2, list[num3].Name);
				list[num3].UnsetPreviewImage();
				num2++;
			}
			else
			{
				GenText.AppendWithComma(stringBuilder, list[num3].Name);
				list.RemoveAt(num3);
				num++;
			}
		}
		RGCLog.Msg("[ModMetaDataCleaner] Removed " + num + " Metadata and cleaned " + num2 + " PreviewImage.\nRemoved: " + stringBuilder.ToString() + "\nCleaned: " + stringBuilder2.ToString());
		if ((int)Current.ProgramState == 2)
		{
			Messages.Message((TranslatorFormattedStringExtensions.Translate("MsgModMetaDataCleaned", (num), (num2))), MessageTypeDefOf.PositiveEvent, false);
		}
		field.SetValue(null, list);
		cacheMetaDataCount = list.Count;
		GC.Collect(2);
	}
}
