using System;
using Verse;

namespace Toolbox;

public static class Launcher
{
	public static void Launch(bool modMetaData, bool languageData, bool defPackage)
	{
		if (modMetaData)
		{
			LongEventHandler.QueueLongEvent((Action)ModMetaDataCleaner.CleanModMetaData, "Reclaiming Memory", false, (Action<Exception>)null, true);
		}
		if (languageData)
		{
			LongEventHandler.QueueLongEvent((Action)LanguageDataCleaner.CleanLanguageData, "Reclaiming Memory", false, (Action<Exception>)null, true);
		}
		if (defPackage)
		{
			LongEventHandler.QueueLongEvent((Action)DefPackageCleaner.CleanDefPackage, "Reclaiming Memory", false, (Action<Exception>)null, true);
		}
	}
}
