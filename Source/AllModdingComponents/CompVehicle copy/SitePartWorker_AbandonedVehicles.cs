using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System;
using UnityEngine;
using CompVehicle;
using RimWorld.Planet;
using System.Text;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using System.Runtime.CompilerServices;
namespace CompVehicle
{
    //WIP 
    //Issue with the contents of the site dissappearing or never being saved
    //Site generates but vehicles in the contents do not
    //Contents return as empty
	public class SitePartWorker_AbandonedVehicles : SitePartWorker
	{

		private List<Pawn> cachepawns;
		public override void PostMapGenerate(Map map)
		{
			base.PostMapGenerate(map);
			AbandonedVehicleContentsComp component = map.info.parent.GetComponent<AbandonedVehicleContentsComp>();
			Log.Error(map.info.parent.GetUniqueLoadID());
			Log.Error(map.info.parent.GetUniqueLoadID());
			for (int i = 0; i < map.info.parent.AllComps.Count; i++)
			{
				Log.Error(map.info.parent.AllComps[i].ToString());
			}
			for (int i = 0; i < component.contents.Count; i++)
			{
				Thing thing = ThingMaker.MakeThing(component.contents[i].def, null);
				if (thing as Pawn != null)
				{
					cachepawns.Add((Pawn)thing);
				}
			}
			if (cachepawns != null && cachepawns.Count > 0)
			{
				IntVec3 intVec;
				for (int i = 0; i < cachepawns.Count; i++)
				{
					if (cachepawns[i].GetComp<CompVehicle>() != null && cachepawns[i].GetComp<CompVehicle>().Props.isWater)
					{
						foreach (IntVec3 tile in map.AllCells)
						{
							if (tile.GetTerrain(map) == TerrainDefOf.WaterMovingDeep || tile.GetTerrain(map) == TerrainDefOf.WaterMovingShallow || tile.GetTerrain(map) == TerrainDefOf.WaterOceanDeep || tile.GetTerrain(map) == TerrainDefOf.WaterOceanShallow)
							{
								if (tile.GetFirstBuilding(map) == null && tile.GetFirstThing(map, new ThingDef()) == null)
								{
									GenSpawn.Spawn(cachepawns[i], tile, map);
								}
							}
						}
					}
					else
					{
						RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => GenGrid.Standable(x, map) && !GridsUtility.Fogged(x, map), map, out intVec);
						GenSpawn.Spawn(cachepawns[i], intVec, map);
					}

				}
			}
		}

	}
}
