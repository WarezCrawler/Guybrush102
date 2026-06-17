using System;
using Verse;

namespace Toolbox;

public static class Launcher
{
	// TODO(GTI): DEAD CODE — this Launch() has NO call sites. The auto-cleanup-on-
	// startup feature (AutoClean* settings) does nothing until this is invoked from
	// RuntimeGC.StaticConstructor. See the TODO block there for the exact call.
	// Currently inert and harmless: never called == nothing happens at startup.
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
