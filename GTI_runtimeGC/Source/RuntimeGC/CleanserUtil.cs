using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RuntimeGC;

internal static class CleanserUtil
{
	private static FieldInfo field = typeof(Pawn_RelationsTracker).GetField("pawnsWithDirectRelationsWithMe", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo battles = typeof(BattleLog).GetField("battles", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo activeEntries = typeof(BattleLog).GetField("activeEntries", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo archivables = typeof(Archive).GetField("archivables", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo pinnedArchivables = typeof(Archive).GetField("pinnedArchivables", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo forceNormalSpeedUntil = typeof(TimeSlower).GetField("forceNormalSpeedUntil", BindingFlags.Instance | BindingFlags.NonPublic);

	private static FieldInfo selMod = typeof(Dialog_ModSettings).GetField("selMod", BindingFlags.Instance | BindingFlags.NonPublic);

	private static WorldPawnCleaner gcobject = new WorldPawnCleaner();

	public static WorldPawnCleaner GCObject
	{
		get
		{
			if (gcobject == null)
			{
				gcobject = new WorldPawnCleaner();
			}
			return gcobject;
		}
	}

	public static IEnumerable<Pawn> getPawnsWithDirectRelationsWithMe(Pawn_RelationsTracker r)
	{
		return (HashSet<Pawn>)field.GetValue(r);
	}

	public static int RemoveAllBattleLogEntries()
	{
		try
		{
			int num = 0;
			foreach (Battle battle in Find.BattleLog.Battles)
			{
				num = battle.Entries.Count;
				if (num <= 0)
				{
					num = 0;
					continue;
				}
				battles.SetValue(Find.BattleLog, null);
				battles.SetValue(Find.BattleLog, new List<Battle>(20));
				activeEntries.SetValue(Find.BattleLog, null);
			}
			return num;
		}
		catch (Exception ex)
		{
			string[] array = new string[12]
			{
				"RuntimeGC Error Start: ",
				Environment.NewLine,
				ex.StackTrace,
				Environment.NewLine,
				ex.Message,
				Environment.NewLine,
				ex.Source,
				Environment.NewLine,
				null,
				null,
				null,
				null
			};
			int num2 = 8;
			array[num2] = ex.Data?.ToString();
			array[9] = Environment.NewLine;
			array[10] = Environment.NewLine;
			array[11] = "RuntimeGC End.";
			Log.Message(string.Concat(array));
			return 0;
		}
	}

	/// <summary>
	/// Manual finalizer for WorldPawnCleaner.GC().
	/// Deconstruct all animal families on map and discard the redundant members.
	/// </summary>
	/// <returns>The count of discarded members.</returns>
	public static int DeconstructAnimalFamily()
	{
		List<Pawn> list = new List<Pawn>();
		foreach (Pawn item in Find.WorldPawns.AllPawnsAliveOrDead)
		{
			list.Add(item);
		}
		List<Pawn> list2 = new List<Pawn>();
		List<Pawn> list3 = new List<Pawn>();
		foreach (Map map in Find.Maps)
		{
			foreach (Pawn allPawn in map.mapPawns.AllPawns)
			{
				if (allPawn.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0 && !allPawn.RaceProps.Humanlike)
				{
					list3.Add(allPawn);
				}
			}
		}
		foreach (Pawn item2 in list3)
		{
			if (item2.relations == null)
			{
				continue;
			}
			foreach (Pawn item3 in expandRelation(item2))
			{
				if (list.Contains(item3) && !((Thing)item3).Spawned && !CaravanUtility.IsPlayerControlledCaravanMember(item3) && !PawnUtility.IsTravelingInTransportPodWorldObject(item3) && item2.Corpse == null)
				{
					list2.Add(item3);
					list.Remove(item3);
				}
			}
		}
		InitUsedTalePawns(out var concernedPawns);
		foreach (Pawn item4 in concernedPawns)
		{
			list2.Remove(item4);
		}
		int count = list2.Count;
		foreach (Pawn item5 in list2)
		{
			Find.WorldPawns.RemovePawn(item5);
			if (!((Thing)item5).Destroyed)
			{
				((Thing)item5).Destroy((DestroyMode)0);
			}
			if (!((Thing)item5).Discarded)
			{
				((Thing)item5).Discard(true);
			}
		}
		Find.WindowStack.WindowOfType<UserInterface>().Notify_PawnsCountDirty();
		return count;
	}

	private static IEnumerable<Pawn> expandRelation(Pawn p)
	{
		foreach (DirectPawnRelation r in p.relations.DirectRelations)
		{
			yield return r.otherPawn;
		}
		foreach (Pawn item in getPawnsWithDirectRelationsWithMe(p.relations))
		{
			yield return item;
		}
	}

	/// <summary>
	/// Remove all filth in home area.Won't raise errors even the filth is queued up by a Toil.
	/// </summary>
	/// <returns>The count of filth being cleaned.</returns>
	public static int RemoveFilth(Map map, bool homearea)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		if (map == null || map.listerFilthInHomeArea == null || map.listerThings == null)
		{
			return 0;
		}
		int num = 0;
		List<Thing> list = ((!homearea) ? map.listerThings.ThingsInGroup((ThingRequestGroup)15) : map.listerFilthInHomeArea.FilthInHomeArea);
		num += list.Count;
		for (int num2 = list.Count - 1; num2 > -1; num2--)
		{
			Filth val = (Filth)list[num2];
			((Entity)val).DeSpawn((DestroyMode)0);
			if (!((Thing)val).Destroyed)
			{
				((Thing)val).Destroy((DestroyMode)0);
			}
			if (!((Thing)val).Discarded)
			{
				Log.Warning("A thing_filth_object destroyed before is not discarded!That's wierd.");
				((Thing)val).Discard(false);
			}
		}
		return num;
	}

	/// <summary>
	/// Fix faction relationships.This will fix the error "Dummy Relation".
	/// </summary>
	public static void FixFactionRelationships()
	{
		List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
		foreach (Faction item in allFactionsListForReading)
		{
			for (int i = 0; i < allFactionsListForReading.Count; i++)
			{
				if (allFactionsListForReading[i] != item)
				{
					item.TryMakeInitialRelationsWith(allFactionsListForReading[i]);
				}
			}
		}
	}

	/// <summary>
	/// Please use FixFactionLeader_Wrapped instead.
	/// Generates a leader for null-leader factions.
	/// Spacers factions will also be generated,but they will remain null-leadered.
	/// Return value is inconclusive.
	/// </summary>
	/// <returns>Leaders generated,but it is inconclusive.</returns>
	public static int FixFactionLeader()
	{
		int num = 0;
		foreach (Faction item in Find.FactionManager.AllFactionsVisible)
		{
			if (item.leader == null || item.leader.Dead || ((Thing)item.leader).Destroyed)
			{
				item.GenerateNewLeader();
				num++;
			}
		}
		Find.WindowStack.WindowOfType<UserInterface>().Notify_PawnsCountDirty();
		return num;
	}

	/// <summary>
	/// Try to generate a leader for null-leader factions.
	/// Factions without leader slot will also be processed,but they will remain null-leadered.
	/// </summary>
	/// <returns>Leaders generated for null-leader factions.</returns>
	public static int FixFactionLeader_Wrapped()
	{
		int num = FixFactionLeader();
		int num2 = FixFactionLeader();
		return num - num2;
	}

	/// <summary>
	/// Remove all corpses in currently-active map.
	/// Won't raise errors even the corpse is being burned.Corpses hauled by other pawns will be remained.
	/// </summary>
	/// <returns>The count of corpses being removed.</returns>
	public static int RemoveCorpses()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		Map currentMap = Find.CurrentMap;
		if (currentMap == null)
		{
			return 0;
		}
		List<Thing> list = currentMap.listerThings.ThingsInGroup((ThingRequestGroup)8);
		int count = list.Count;
		for (int num = list.Count - 1; num > -1; num--)
		{
			Corpse val = (Corpse)list[num];
			((Entity)val).DeSpawn((DestroyMode)0);
			if (!((Thing)val).Destroyed)
			{
				((Thing)val).Destroy((DestroyMode)0);
			}
			if (!((Thing)val).Discarded)
			{
				((Thing)val).Discard(false);
			}
		}
		Find.WindowStack.WindowOfType<UserInterface>().Notify_PawnsCountDirty();
		return count;
	}

	/// <summary>
	/// Form a list containing all pawns concerned by used tales.
	/// </summary>
	public static void InitUsedTalePawns(out List<Pawn> concernedPawns)
	{
		List<Tale> list = Find.TaleManager.AllTalesListForReading.FindAll((Tale t) => t.Uses > 0 || (int)t.def.type == 2);
		List<Pawn> list2 = new List<Pawn>();
		int count = list.Count;
		int num = 0;
		foreach (Pawn item in Find.WorldPawns.AllPawnsAliveOrDead)
		{
			try
			{
				for (num = 0; num < count; num++)
				{
					if (list[num].Concerns((Thing)(object)item))
					{
						list2.Add(item);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Exception in InitUsedTalePawns with Tale id=" + list[num].id + " :\n" + ex.ToString());
				list2.Add(item);
			}
		}
		concernedPawns = list2;
	}

	public static void RemoveSnow(Map map, bool homearea)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (map == null || map.snowGrid == null || map.mapDrawer == null)
		{
			return;
		}
		if (homearea)
		{
			SnowGrid snowGrid = map.snowGrid;
			{
				foreach (IntVec3 activeCell in ((Area)map.areaManager.Home).ActiveCells)
				{
					snowGrid.SetDepth(activeCell, 0f);
				}
				return;
			}
		}
		map.snowGrid = new SnowGrid(map);
		map.mapDrawer.WholeMapChanged(64uL);
		map.mapDrawer.WholeMapChanged(1uL);
		map.pathing.RecalculateAllPerceivedPathCosts();
	}

	public static int RemoveIArchivable(bool removePinned)
	{
		if (Find.Archive == null)
		{
			return 0;
		}
		List<IArchivable> list = new List<IArchivable>();
		if (!removePinned)
		{
			list.AddRange((HashSet<IArchivable>)pinnedArchivables.GetValue(Find.Archive));
			GenCollection.SortBy<IArchivable, int>(list, (Func<IArchivable, int>)((IArchivable iA) => iA.CreatedTicksGame));
		}
		int result = GenList.CountAllowNull<IArchivable>((IList<IArchivable>)(List<IArchivable>)archivables.GetValue(Find.Archive)) - list.Count;
		archivables.SetValue(Find.Archive, list);
		if (removePinned)
		{
			pinnedArchivables.SetValue(Find.Archive, new HashSet<IArchivable>());
		}
		return result;
	}

	public static void UnlockNormalSpeedLimit()
	{
		forceNormalSpeedUntil.SetValue(Find.TickManager.slower, Find.TickManager.TicksGame);
	}

	public static void OpenModSettingsPage()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		Mod val = GenCollection.FirstOrFallback<Mod>(LoadedModManager.ModHandles, (Func<Mod, bool>)((Mod m) => m is RuntimeGC), (Mod)null);
		Dialog_ModSettings val2 = new Dialog_ModSettings(val);
		Find.WindowStack.Add((Window)(object)val2);
	}
}
