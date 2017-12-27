using Verse;
using Verse.Sound;

namespace AbilityUser
{
    public class FlyingObject_Equipable : FlyingObject
    {
        protected override void Impact(Thing hitThing)
        {
            if (flyingThing != null)
            {
                GenSpawn.Spawn(flyingThing, Position, Map);
                if (launcher != null)
                    if (launcher is Pawn equipper)
                        if (equipper.equipment != null)
                            if (flyingThing is ThingWithComps flyingThingWithComps)
                                Equip(equipper, flyingThingWithComps);
            }
            Destroy(DestroyMode.Vanish);
        }

        public void Equip(Pawn equipper, ThingWithComps thingWithComps)
        {
            var flag = false;
            ThingWithComps thingWithComps2;
            if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
            {
                thingWithComps2 = (ThingWithComps) thingWithComps.SplitOff(1);
            }
            else
            {
                thingWithComps2 = thingWithComps;
                flag = true;
            }
            equipper.equipment.MakeRoomFor(thingWithComps2);
            equipper.equipment.AddEquipment(thingWithComps2);
            if (thingWithComps.def.soundInteract != null)
                thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(equipper.Position, equipper.Map, false));
            if (flag)
                thingWithComps.DeSpawn();
        }
    }
}