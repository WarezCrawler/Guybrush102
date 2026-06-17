using UnityEngine;
using Verse;

namespace RuntimeGC;

[StaticConstructorOnStartup]
internal class StaticConstructor
{
	static StaticConstructor()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)GameObject.Find("RuntimeGCInstance") != (Object)null)
		{
			Log.Warning("[RuntimeGC] More than one RuntimeGC instance is running!");
		}
		else
		{
			GameObject val = new GameObject("RuntimeGCInstance");
			Object.DontDestroyOnLoad((Object)(object)val);
		}
		MainButtonWorker_RuntimeGC.TabDescriptionTranslated = (Translator.Translate("MainTabWindowDescription"));
		MainButtonWorker_RuntimeGC.MMTipTranslated = (Translator.Translate("MMTip"));
		UIUtil.Notify_MMBtnLabelChanged();

		// ====================================================================
		// TODO(GTI): MISSING STARTUP WIRING — two advertised features are dead.
		// This decompiled fork never calls either startup launcher, so the
		// matching mod-settings currently have no effect. This static ctor is
		// the natural place to wire them up. See also Toolbox/Launcher.cs and
		// Mute/Launcher.cs (both Launch() methods have zero call sites).
		//
		//   1. Auto-cleanup on startup (settings exist, nothing runs them):
		//        Toolbox.Launcher.Launch(
		//            RuntimeGC.Settings.AutoCleanModMetaData,
		//            RuntimeGC.Settings.AutoCleanLanguageData,
		//            RuntimeGC.Settings.AutoCleanDefPackage);
		//      The underlying cleaners now work (see the GTI fixes in
		//      DefPackageCleaner.cs and FloatMenuUtil.cs); only this call is
		//      missing.
		//
		//   2. "Integrated MuteGC / MuteBL":
		//        Mute.Launcher.Launch(
		//            RuntimeGC.Settings.DoMuteGC,
		//            RuntimeGC.Settings.DoMuteBL);
		//      CAUTION before re-enabling #2: Mute.Launcher uses raw function-
		//      pointer patching (unsafe DoDetour), which is fragile on the
		//      current Mono runtime. Prefer reimplementing the mutes via Harmony.
		//
		// SAFETY: leaving these unwired is inert. No Launch() call means no
		// detours are installed and no auto-clean runs, so vanilla behaviour is
		// untouched and the current build is safe as-is.
		// ====================================================================
	}
}
