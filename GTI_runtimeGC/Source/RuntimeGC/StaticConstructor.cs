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
	}
}
