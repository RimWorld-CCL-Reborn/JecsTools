using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace AbilityUser
{
    public class FlyingObject_Equipable : FlyingObject
    {
        protected override void Impact(Thing hitThing)
        {
            if (this.flyingThing != null)
            {
                GenSpawn.Spawn(this.flyingThing, this.Position, this.Map);
                if (this.launcher != null)
                {
                    if (this.launcher is Pawn equipper)
                    {
                        if (equipper.equipment != null)
                        {
                            if (this.flyingThing is ThingWithComps flyingThingWithComps)
                            {
                                Equip(equipper, flyingThingWithComps);
                            }
                        }
                    }
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }

        public void Equip(Pawn equipper, ThingWithComps thingWithComps)
        {
            bool flag = false;
            ThingWithComps thingWithComps2;
            if (thingWithComps.def.stackLimit > 1 && thingWithComps.stackCount > 1)
            {
                thingWithComps2 = (ThingWithComps)thingWithComps.SplitOff(1);
            }
            else
            {
                thingWithComps2 = thingWithComps;
                flag = true;
            }
            equipper.equipment.MakeRoomFor(thingWithComps2);
            equipper.equipment.AddEquipment(thingWithComps2);
            if (thingWithComps.def.soundInteract != null)
            {
                thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(equipper.Position, equipper.Map, false));
            }
            if (flag)
            {
                thingWithComps.DeSpawn();
            }
        }
    }
}
