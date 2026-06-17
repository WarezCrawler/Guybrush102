using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RuntimeGC;

public class WorldPawnCleaner
{
	private enum Flags
	{
		Colonist = 1,
		Prisoner = 2,
		FactionLeader = 8,
		KeptWorldPawn = 16,
		CorpseOwner = 4,
		RelationLvl0 = 32,
		RelationLvl1 = 64,
		RelationLvl2 = 128,
		TaleEntryOwner = 256,
		OnSale = 512,
		Animal = 1024,
		None = 0
	}

	private static string CopyrightStr = "RuntimeGC for 1.5,user19990313,Baidu Tieba&Ludeon forum";

	/// Verbosity
	private Dictionary<Pawn, int> allPawnsCounter = new Dictionary<Pawn, int>();

	private Dictionary<Flags, int> allFlagsCounter = new Dictionary<Flags, int>();

	private bool verbose = false;

	/// Debug only.Well,useless.
	private bool debug = false;

	private static int FlagsCountNotNull = Enum.GetNames(typeof(Flags)).Length - 1;

	private List<Pawn> reference;

	private Dictionary<Pawn, Flags> allFlags = new Dictionary<Pawn, Flags>();

	/// <summary>
	/// GC().The best way to shrink Rimworld saves,I think.
	/// </summary>
	/// <param name="verbose">Determine if GC() should log details very very verbosely</param>
	/// <returns>Count of disposed World pawns</returns>
	public int GC(bool verbose = false)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)Current.ProgramState != 2)
		{
			RGCLog.Error("You must be kidding me...GC a save without loading one?");
			return 0;
		}
		RGCLog.Msg("[GC Log] Pre-Initializing GC...");
		reference = Find.WorldPawns.AllPawnsAliveOrDead.ToList();
		allFlags.Clear();
		if (verbose)
		{
			allFlagsCounter.Clear();
			allFlagsCounter.Add(Flags.None, 0);
			for (int i = 0; i < FlagsCountNotNull; i++)
			{
				allFlagsCounter.Add((Flags)(1 << i), 0);
			}
		}
		RGCLog.Msg("[GC Log] Generating EntryPoints from Map Pawns...");
		DiagnoseMapPawns(out var mapPawnEntryPoints);
		if (verbose)
		{
			RGCLog.Msg("[GC Log][Verbose] " + allPawnsCounter.Count() + " Map Pawns marked during diagnosis");
		}
		allPawnsCounter.Clear();
		if (verbose)
		{
			allFlagsCounter.Clear();
			allFlagsCounter.Add(Flags.None, 0);
			for (int j = 0; j < FlagsCountNotNull; j++)
			{
				allFlagsCounter.Add((Flags)(1 << j), 0);
			}
		}
		RGCLog.Msg("[GC Log] Collecting Pawns concerned by Used Tales...");
		CleanserUtil.InitUsedTalePawns(out var concernedPawns);
		RGCLog.Msg("[GC Log] Running diagnosis on WorldPawns...");
		foreach (Pawn item in reference)
		{
			if (item == null)
			{
				RGCLog.Msg("Encountered a null pawn.");
				continue;
			}
			if (item.IsColonist)
			{
				addFlag(item, Flags.Colonist | Flags.RelationLvl2);
			}
			if (item.IsPrisonerOfColony)
			{
				addFlag(item, Flags.Prisoner | Flags.RelationLvl2);
			}
			if (PawnUtility.IsFactionLeader(item))
			{
				// Original: 88. Flags: FactionLeader (8) | KeptWorldPawn (16) | RelationLvl1 (64)
				addFlag(item, Flags.FactionLeader | Flags.KeptWorldPawn | Flags.RelationLvl1);
			}
			if (PawnUtility.IsKidnappedPawn(item))
			{
				// Original: 144. Flags: KeptWorldPawn (16) | RelationLvl2 (128)
				addFlag(item, Flags.KeptWorldPawn | Flags.RelationLvl2);
			}
			if (item.Corpse != null)
			{
				// Original: 68. Flags: CorpseOwner (4) | RelationLvl1 (64)
				addFlag(item, Flags.CorpseOwner | Flags.RelationLvl1);
			}
			if (concernedPawns.Contains(item))
			{
				// Original: 288. Flags: RelationLvl0 (32) | TaleEntryOwner (256)
				addFlag(item, Flags.RelationLvl0 | Flags.TaleEntryOwner);
			}
			if (item.InContainerEnclosed)
			{
				// Original: 48. Flags: KeptWorldPawn (16) | RelationLvl0 (32)
				addFlag(item, Flags.KeptWorldPawn | Flags.RelationLvl0);
			}
			if (((Thing)item).Spawned)
			{
				addFlag(item, Flags.RelationLvl0);
			}
			if (CaravanUtility.IsPlayerControlledCaravanMember(item))
			{
				// Original: 144. Flags: KeptWorldPawn (16) | RelationLvl2 (128)
				addFlag(item, Flags.KeptWorldPawn | Flags.RelationLvl2);
			}
			if (PawnUtility.IsTravelingInTransportPodWorldObject(item))
			{
				// Original: 144. Flags: KeptWorldPawn (16) | RelationLvl2 (128)
				addFlag(item, Flags.KeptWorldPawn | Flags.RelationLvl2);
			}
			if (PawnUtility.ForSaleBySettlement(item))
			{
				// Original: 544. Flags: RelationLvl0 (32) | OnSale (512)
				addFlag(item, Flags.RelationLvl0 | Flags.OnSale);
			}
			if (verbose)
			{
				RGCLog.Msg("[worldPawn] " + ((Entity)item).LabelShort + " [flag] " + markedFlagsString(item));
			}
		}
		if (verbose)
		{
			RGCLog.Msg("[GC Log][Verbose] " + allPawnsCounter.Count() + " World Pawns marked during diagnosis");
		}
		RGCLog.Msg("[GC Log] Expanding Relation networks through Map Pawn Entry Points...");
		for (int num = mapPawnEntryPoints.Count - 1; num > -1; num--)
		{
			if (containsFlag(mapPawnEntryPoints[num], Flags.RelationLvl2))
			{
				expandRelation(mapPawnEntryPoints[num], Flags.RelationLvl1);
				mapPawnEntryPoints.RemoveAt(num);
			}
		}
		for (int num2 = mapPawnEntryPoints.Count - 1; num2 > -1; num2--)
		{
			if (containsFlag(mapPawnEntryPoints[num2], Flags.RelationLvl1))
			{
				expandRelation(mapPawnEntryPoints[num2], Flags.RelationLvl0);
				mapPawnEntryPoints.RemoveAt(num2);
			}
		}
		RGCLog.Msg("[GC Log] Expanding Relation networks on marked World Pawns...");
		for (int num3 = reference.Count - 1; num3 > -1; num3--)
		{
			if (containsFlag(reference[num3], Flags.RelationLvl2))
			{
				expandRelation(reference[num3], Flags.RelationLvl1);
				reference.RemoveAt(num3);
			}
		}
		for (int num4 = reference.Count - 1; num4 > -1; num4--)
		{
			if (containsFlag(reference[num4], Flags.RelationLvl1))
			{
				expandRelation(reference[num4], Flags.RelationLvl0);
				reference.RemoveAt(num4);
			}
		}
		for (int num5 = reference.Count - 1; num5 > -1; num5--)
		{
			if (containsFlag(reference[num5], Flags.RelationLvl0))
			{
				reference.RemoveAt(num5);
			}
		}
		int num6 = 0;
		if (verbose)
		{
			foreach (KeyValuePair<Pawn, int> item2 in allPawnsCounter)
			{
				num6 += item2.Value;
			}
			RGCLog.Msg("[GC Log][Verbose] " + allPawnsCounter.Count() + " World Pawns marked during Expanding");
			if (debug)
			{
				RGCLog.Msg("addFlag() called " + num6 + " times");
			}
		}
		RGCLog.Msg("[GC Log] Excluding Pawns concerned by Used Tales...");
		foreach (Pawn item3 in concernedPawns)
		{
			reference.Remove(item3);
		}
		List<Thing> list = Find.QuestManager.QuestsListForReading
			.Where((Quest q) => q.State == QuestState.NotYetAccepted || q.State == QuestState.Ongoing || q.State == QuestState.EndedSuccess)
			.SelectMany((Quest q) => q.QuestLookTargets).Where(delegate (GlobalTargetInfo x)
			{
				return x.Thing != null;
			})
			.Select(delegate (GlobalTargetInfo x)
			{
				return x.Thing;
			})
			.Distinct()
			.ToList();
		RGCLog.Msg($"[GC Log] Excluding Quest Pawns: {reference.RemoveAll((Pawn p) => list.Contains((Thing)(object)p))}");
		RGCLog.Msg("[GC Log] Disposing World Pawns...");
		num6 = reference.Count;
		for (int num7 = reference.Count - 1; num7 > -1; num7--)
		{
			Pawn val = reference[num7];
			Find.WorldPawns.RemovePawn(val);
			if (!((Thing)val).Destroyed)
			{
				((Thing)val).Destroy((DestroyMode)0);
			}
			if (!((Thing)val).Discarded)
			{
				((Thing)val).Discard(true);
			}
		}
		if (verbose)
		{
			string text = "[GC Log][Verbose] Flag calls stat:";
			allFlagsCounter.Remove(Flags.None);
			foreach (KeyValuePair<Flags, int> item4 in allFlagsCounter)
			{
				string[] array = new string[5] { text, "\n  ", null, null, null };
				array[2] = item4.Key.ToString();
				array[3] = " : ";
				array[4] = item4.Value.ToString();
				text = string.Concat(array);
			}
			RGCLog.Msg(text);
		}
		RGCLog.Msg("[GC Log] GC() completed with " + num6 + " World Pawns disposed");
		return num6;
	}

	private Flags getFlag(Pawn pawn)
	{
		return allFlags.ContainsKey(pawn) ? allFlags[pawn] : Flags.None;
	}

	private bool containsFlag(Pawn pawn, Flags flag)
	{
		return containsFlag(getFlag(pawn), flag);
	}

	private bool containsFlag(Flags f1, Flags f2)
	{
		return (f1 & f2) != 0;
	}

	private IEnumerable<Flags> splitFlag(Flags flag)
	{
		int f = 1;
		for (int j = 0; j < FlagsCountNotNull; j++)
		{
			WorldPawnCleaner worldPawnCleaner = this;
			int f2;
			f = (f2 = f << 1);
			if (worldPawnCleaner.containsFlag(flag, (Flags)f2))
			{
				yield return (Flags)f;
			}
		}
		yield return Flags.None;
	}

	private string markedFlagsString(Pawn p)
	{
		List<Flags> list = splitFlag(getFlag(p)).ToList();
		string text = "";
		int i;
		for (i = 0; i < list.Count - 2; i++)
		{
			text += list[i];
			text += "|";
		}
		return text + list[i];
	}

	private void addFlag(Pawn pawn, Flags flag)
	{
		if (!allFlags.ContainsKey(pawn))
		{
			allFlags.Add(pawn, flag);
		}
		else
		{
			allFlags[pawn] |= flag;
		}
		if (!verbose)
		{
			return;
		}
		if (!allPawnsCounter.ContainsKey(pawn))
		{
			allPawnsCounter.Add(pawn, 1);
		}
		else
		{
			allPawnsCounter[pawn]++;
		}
		foreach (Flags item in splitFlag(flag))
		{
			allFlagsCounter[item]++;
		}
	}

	private void expandRelation(Pawn p, Flags flag)
	{
		if (p.relations == null)
		{
			return;
		}
		if (debug)
		{
			foreach (Pawn item in p.relations.FamilyByBlood)
			{
				foreach (PawnRelationDef relation in PawnRelationUtility.GetRelations(p, item))
				{
					RGCLog.Msg("(Family) " + ((Entity)p).LabelShort + " <" + ((Def)relation).label + "> " + ((Entity)item).LabelShort);
				}
			}
		}
		foreach (DirectPawnRelation directRelation in p.relations.DirectRelations)
		{
			if (reference.Contains(directRelation.otherPawn))
			{
				addFlag(directRelation.otherPawn, flag);
				if (verbose)
				{
					RGCLog.Msg("(Relation) " + ((Entity)p).LabelShort + " <" + ((Def)directRelation.def).label + "> " + ((Entity)directRelation.otherPawn).LabelShort);
				}
			}
		}
		foreach (Pawn item2 in CleanserUtil.getPawnsWithDirectRelationsWithMe(p.relations))
		{
			if (reference.Contains(item2) && PawnRelationUtility.GetRelations(item2, p).Count() > 0)
			{
				addFlag(item2, flag);
				if (verbose)
				{
					RGCLog.Msg("(Reflexed) <" + ((Entity)item2).LabelShort + "," + ((Entity)p).LabelShort + ">");
				}
			}
		}
	}

	private void DiagnoseMapPawns(out List<Pawn> mapPawnEntryPoints)
	{
		List<Pawn> list = new List<Pawn>();
		foreach (Map map in Find.Maps)
		{
			foreach (Pawn allPawn in map.mapPawns.AllPawns)
			{
				if (allPawn.IsColonist)
				{
					addFlag(allPawn, (Flags)129);
				}
				if (allPawn.IsPrisonerOfColony)
				{
					addFlag(allPawn, (Flags)130);
				}
				if (PawnUtility.IsFactionLeader(allPawn))
				{
					addFlag(allPawn, (Flags)72);
				}
				if (PawnUtility.IsKidnappedPawn(allPawn))
				{
					addFlag(allPawn, (Flags)144);
				}
				if (allPawn.Corpse != null)
				{
					addFlag(allPawn, (Flags)68);
				}
				if (allPawn.RaceProps.Humanlike)
				{
					addFlag(allPawn, Flags.RelationLvl1);
				}
				if (allFlags.ContainsKey(allPawn))
				{
					list.Add(allPawn);
					if (verbose)
					{
						RGCLog.Msg("[mapPawn] " + ((Entity)allPawn).LabelShort + " [flag] " + markedFlagsString(allPawn));
					}
				}
			}
		}
		mapPawnEntryPoints = list.ToList();
	}

	public void DisposeTmpForSystemGC()
	{
		reference = null;
		allPawnsCounter.Clear();
		allFlags.Clear();
		allFlagsCounter.Clear();
	}
}
