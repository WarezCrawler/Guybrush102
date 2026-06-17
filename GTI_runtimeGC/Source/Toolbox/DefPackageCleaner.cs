using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace Toolbox;

public static class DefPackageCleaner
{
	private static ModContentPack coreMod;

	public static bool Cleaned => coreMod != null && coreMod.AllDefs.Count() == 0;

	public static void CleanDefPackage()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Invalid comparison between Unknown and I4
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		FieldInfo field = typeof(ModContentPack).GetField("defPackages", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		int num = 0;
		foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
		{
			if (runningMod.IsCoreMod)
			{
				coreMod = runningMod;
			}
			num += ((List<Def>)field.GetValue(runningMod)).Count;
			field.SetValue(runningMod, new List<Def>());
		}
		Log.Message("[DefPackageCleaner] Cleaned " + num + " DefPackages.");
		if ((int)Current.ProgramState == 2)
		{
			Messages.Message((TranslatorFormattedStringExtensions.Translate("MsgDefPackageCleaned", (num))), MessageTypeDefOf.PositiveEvent, false);
		}
		GC.Collect(2);
	}
}
