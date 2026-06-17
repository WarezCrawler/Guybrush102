using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RuntimeGC;

public class UserInterface : MainTabWindow
{
	private int pawnsAliveCount;

	private int pawnsDeadCount;

	private bool pawnsCountDirty = true;

	public override Vector2 RequestedTabSize => new Vector2(350f, 500f);

	public int PawnsAliveCount
	{
		get
		{
			if (pawnsCountDirty)
			{
				pawnsCountDirty = false;
				pawnsAliveCount = Find.WorldPawns.AllPawnsAlive.Count();
				pawnsDeadCount = Find.WorldPawns.AllPawnsDead.Count();
			}
			return pawnsAliveCount;
		}
	}

	public int PawnsDeadCount
	{
		get
		{
			if (pawnsCountDirty)
			{
				pawnsCountDirty = false;
				pawnsAliveCount = Find.WorldPawns.AllPawnsAlive.Count();
				pawnsDeadCount = Find.WorldPawns.AllPawnsDead.Count();
			}
			return pawnsDeadCount;
		}
	}

	public override void DoWindowContents(Rect canvas)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		GUI.BeginGroup(canvas);
		Listing_Standard val = new Listing_Standard();
		((Listing)val).Begin(canvas);
		Text.Font = (GameFont)2;
		val.Label(Translator.Translate("RuntimeGCTitle"), -1f);
		Text.Font = (GameFont)1;
		val.Label(TranslatorFormattedStringExtensions.Translate("RuntimeGCVer", ("1.5")), -1f);
		val.Label("By user19990313", -1f);
		((Listing)val).Gap(12f);
		val.Label("pawnsAlive:" + PawnsAliveCount + " pawnsDead:" + PawnsDeadCount, -1f);
		((Listing)val).Gap(12f);
		float curHeight = ((Listing)val).CurHeight;
		((Listing)val).End();
		DoComponents(new Rect(canvas.x + 35f, canvas.y + curHeight, canvas.width, canvas.height - curHeight));
		DoHelpContents(new Rect(canvas.x, canvas.y + curHeight, canvas.width, canvas.height - curHeight));
		GUI.EndGroup();
	}

	public void DoComponents(Rect rect)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Expected O, but got Unknown
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		GUI.BeginGroup(rect);
		Rect val = new(0f, 0f, 170f, rect.height);
		Text.Font = (GameFont)1;
		List<ListableOption> list = new List<ListableOption>();
		list.Add(new ListableOption((Translator.Translate("BtnTextGCWP")), (Action)delegate
		{
			GC_wrapped();
		}, (string)null));
		list.Add(new ListableOption((Translator.Translate("BtnTextGCV")), (Action)delegate
		{
			GC_wrapped(verbose: true);
		}, (string)null));
		list.Add(new ListableOption((Translator.Translate("BtnTextAddOn")), (Action)delegate
		{
			FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupTools);
		}, (string)null));
		list.Add(new ListableOption((Translator.Translate("BtnTextFix")), (Action)delegate
		{
			FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupFix);
		}, (string)null));
		list.Add(new ListableOption((Translator.Translate("BtnTextQB")), (Action)delegate
		{
			FloatMenuUtil.GenerateFloatMenuGroup(FloatMenuUtil.GroupQuickbar);
		}, (string)null));
		list.Add(new ListableOption((Translator.Translate("BtnTextSysGC")), (Action)delegate
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			if (Event.current.shift)
			{
				FloatMenuUtil.GenerateMemoryReclaimOptions();
			}
			CleanserUtil.GCObject.DisposeTmpForSystemGC();
			GC.Collect();
			Messages.Message((Translator.Translate("MsgTextSysGC")), MessageTypeDefOf.PositiveEvent, true);
		}, (string)null));
		OptionListingUtility.DrawOptionListing(val, list);
		GUI.EndGroup();
	}

	public void DoHelpContents(Rect rect)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected O, but got Unknown
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Expected O, but got Unknown
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected O, but got Unknown
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		GUI.BeginGroup(rect);
		Rect val = new(0f, 0f, 35f, rect.height);
		Text.Font = (GameFont)1;
		List<ListableOption> list = new List<ListableOption>();
		list.Add(new ListableOption("?", (Action)delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			Find.WindowStack.Add((Window)new Dialog_MessageBox(Translator.Translate("HelpTextGCWP"), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, (WindowLayer)1));
		}, (string)null));
		list.Add(new ListableOption("?", (Action)delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			Find.WindowStack.Add((Window)new Dialog_MessageBox(Translator.Translate("HelpTextGCV"), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, (WindowLayer)1));
		}, (string)null));
		list.Add(new ListableOption("?", (Action)delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			Find.WindowStack.Add((Window)new Dialog_MessageBox(Translator.Translate("HelpTextAddOn"), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, (WindowLayer)1));
		}, (string)null));
		list.Add(new ListableOption("?", (Action)delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			Find.WindowStack.Add((Window)new Dialog_MessageBox(Translator.Translate("HelpTextFix"), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, (WindowLayer)1));
		}, (string)null));
		list.Add(new ListableOption("?", (Action)delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			Find.WindowStack.Add((Window)new Dialog_MessageBox(Translator.Translate("HelpTextQB"), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, (WindowLayer)1));
		}, (string)null));
		list.Add(new ListableOption("?", (Action)delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			Find.WindowStack.Add((Window)new Dialog_MessageBox(Translator.Translate("HelpTextSysGC"), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, (WindowLayer)1));
		}, (string)null));
		OptionListingUtility.DrawOptionListing(val, list);
		GUI.EndGroup();
	}

	public void GC_wrapped(bool verbose = false)
	{
		int num = PawnsAliveCount;
		int num2 = PawnsDeadCount;
		int num3 = CleanserUtil.GCObject.GC(verbose);
		Notify_PawnsCountDirty();
		int num4 = num + num2 - PawnsAliveCount - PawnsDeadCount;
		string text = (TranslatorFormattedStringExtensions.Translate("DlgTextGC", (num), (PawnsAliveCount), (num2), (PawnsDeadCount), (num4)));
		// Find.WindowStack.Add((Window)new Dialog_MessageBox((text + "\n\n" + ((num3 == num4) ? (("DlgTextGCAdvice1").Translate()) : ("DlgTextGCAdvice2".Translate(num3 - num4)) + ((!verbose) ? "" : ("\n\n" + "DlgTextGCV".Translate())))), (string)null, (Action)null, (string)null, (Action)null, (string)null, false, (Action)null, (Action)null, (WindowLayer)1));
		Find.WindowStack.Add(new Dialog_MessageBox(text + "\n\n" + ((num3 == num4) ? "DlgTextGCAdvice1".Translate() : "DlgTextGCAdvice2".Translate(num3 - num4) + (string)((!verbose) ? "" : "\n\n" + "DlgTextGCV".Translate())), null, null, null, null, null, false, null, null, WindowLayer.Dialog));
		if (RuntimeGC.Settings.ArchiveGCDialog)
		{
			Find.Archive.Add((IArchivable)new ArchivedDialog(text, (Translator.Translate("DlgArchiveTitle")), (Faction)null));
		}
	}

	public void Notify_PawnsCountDirty()
	{
		pawnsCountDirty = true;
	}

	public override void PreOpen()
	{
		base.PreOpen();
		forcePause = true;
		Notify_PawnsCountDirty();
	}
}
