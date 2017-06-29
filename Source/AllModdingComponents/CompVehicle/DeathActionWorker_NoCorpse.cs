using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
namespace CompVehicle
{
	public class DeathActionWorker_NoCorpse : DeathActionWorker
	{

		Map map;

		public override void PawnDied(Corpse corpse)
		{
			//Corpse NullCheck
			if (corpse == null)
				return;
			//Get Corpse Properties
			map = corpse.Map;
			IntVec3 pos = corpse.Position;
            Pawn pawn = corpse.InnerPawn;

			//Destroy Corpse
			corpse.Destroy();
			//Read through killedLeavings of the pawn
			ThingOwner<Thing> thingOwner = new ThingOwner<Thing>();
            for (int i = 0; i < pawn.def.killedLeavings.Count; i++){
                Thing thing = ThingMaker.MakeThing(pawn.def.killedLeavings[i].thingDef, null);
                thing.stackCount = pawn.def.killedLeavings[i].count;
                thingOwner.TryAdd(thing,true);  
            }
			//Generate items/amount in list
			for (int i = 0; i < thingOwner.Count; i++)
			{
				GenPlace.TryPlaceThing(thingOwner[i], pos, map, ThingPlaceMode.Near, null);

			}

		}
	}
}

