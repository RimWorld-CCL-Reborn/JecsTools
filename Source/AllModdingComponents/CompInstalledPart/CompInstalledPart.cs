using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompInstalledPart
{
    public class CompInstalledPart : ThingComp
    {
        public bool uninstalled = false;

        public CompProperties_InstalledPart Props
        {
            get
            {
                return (CompProperties_InstalledPart)this.props;
            }
        }

        public void GiveInstallJob(Pawn actor, Thing target)
        {

        }

        public void GiveUninstallJob(Pawn actor, Thing target)
        {

        }

        public void Notify_Installed(Pawn installer, Thing target)
        {
            uninstalled = false;

            //Installed on a character
            Pawn targetPawn = target as Pawn;
            if (targetPawn != null)
            {
                if (parent.def != null)
                {
                    //Add apparel
                    if (parent.def.IsApparel)
                    {
                        if (targetPawn.apparel != null)
                        {
                            parent.DeSpawn();
                            targetPawn.apparel.Wear((Apparel)parent);
                        }
                    }
                    //Add equipment
                    if (parent.def.IsWeapon)
                    {
                        if (targetPawn.equipment != null)
                        {
                            if (targetPawn.equipment.Primary != null)
                            {
                                CompInstalledPart otherPart = targetPawn.equipment.Primary.GetComp<CompInstalledPart>();
                                if (otherPart != null)
                                {
                                    Notify_Uninstalled(installer, targetPawn);
                                }
                            }
                            parent.DeSpawn();
                            targetPawn.equipment.MakeRoomFor(parent);
                            targetPawn.equipment.AddEquipment(parent);
                        }
                    }
                }
            }
            else
            {
                ThingOwner addableSource = target.TryGetInnerInteractableThingOwner();
                if (addableSource != null)
                {
                    parent.DeSpawn();
                    addableSource.TryAdd(parent);
                }
            }

            Messages.Message("CompInstalledPart_Installed".Translate(new object[]
            {
                installer.LabelShort,
                this.parent.LabelShort,
                target.LabelShort
            }), MessageSound.Benefit);
        }

        public void Notify_Uninstalled(Pawn uninstaller, Thing partOrigin)
        {
            uninstalled = true;

            Pawn targetPawn = partOrigin as Pawn;
            if (targetPawn != null)
            {
                if (parent.def != null)
                {
                    //Remove apparel
                    if (parent.def.IsApparel)
                    {
                        if (targetPawn.apparel != null)
                        {
                            if (targetPawn.apparel.WornApparel.Contains((Apparel)parent))
                            {
                                Apparel apparel;
                                if (targetPawn.apparel.TryDrop((Apparel)parent, out apparel))
                                {
                                }
                            }
                        }
                    }
                    //Remove equipment
                    if (parent.def.IsWeapon)
                    {
                        if (targetPawn.equipment != null)
                        {
                            ThingWithComps dropped;
                            if (targetPawn.equipment.TryDropEquipment(parent, out dropped, targetPawn.Position))
                            {

                            }
                        }
                    }
                }
            }
            else
            {
                ThingOwner addableSource = partOrigin.TryGetInnerInteractableThingOwner();
                if (addableSource != null)
                {
                    addableSource.Remove(parent);
                    GenPlace.TryPlaceThing(parent, partOrigin.Position, partOrigin.Map, ThingPlaceMode.Near);
                }
            }

            Messages.Message("CompInstalledPart_Uninstalled".Translate(new object[]
            {
                uninstaller.LabelShort,
                this.parent.LabelShort,
                partOrigin.LabelShort
            }), MessageSound.Benefit);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.uninstalled, "uninstalled", false);
        }
    }
}
