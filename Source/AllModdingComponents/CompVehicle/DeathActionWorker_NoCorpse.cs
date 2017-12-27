using Verse;

namespace CompVehicle
{
    public class DeathActionWorker_NoCorpse : DeathActionWorker
    {
        private Map map;

        public override void PawnDied(Corpse corpse)
        {
            //Corpse NullCheck
            if (corpse == null)
                return;
            //Get Corpse Properties
            map = corpse.Map;
            var pos = corpse.Position;
            var pawn = corpse.InnerPawn;

            //Destroy Corpse
            corpse.Destroy();
            //Read through killedLeavings of the pawn
            var thingOwner = new ThingOwner<Thing>();
            for (var i = 0; i < pawn.def.killedLeavings.Count; i++)
            {
                var thing = ThingMaker.MakeThing(pawn.def.killedLeavings[i].thingDef, null);
                thing.stackCount = pawn.def.killedLeavings[i].count;
                thingOwner.TryAdd(thing, true);
            }
            //Generate items/amount in list
            for (var i = 0; i < thingOwner.Count; i++)
                GenPlace.TryPlaceThing(thingOwner[i], pos, map, ThingPlaceMode.Near, null);
        }
    }
}