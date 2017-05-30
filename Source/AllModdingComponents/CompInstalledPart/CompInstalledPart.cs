using RimWorld;
using Verse;

namespace CompInstalledPart
{
    public class CompInstalledPart : ThingComp
    {
        public bool uninstalled = false;

        public CompProperties_InstalledPart Props => (CompProperties_InstalledPart)this.props;

        public void GiveInstallJob(Pawn actor, Thing target)
        {

        }

        public void GiveUninstallJob(Pawn actor, Thing target)
        {

        }

        public void Notify_Installed(Pawn installer, Thing target)
        {
            this.uninstalled = false;

            //Installed on a character
            if (target is Pawn targetPawn)
            {
                if (this.parent.def != null)
                {
                    //Add apparel
                    if (this.parent.def.IsApparel)
                    {
                        if (targetPawn.apparel != null)
                        {
                            this.parent.DeSpawn();
                            targetPawn.apparel.Wear((Apparel)this.parent);
                        }
                    }
                    //Add equipment
                    if (this.parent.def.IsWeapon)
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
                            this.parent.DeSpawn();
                            targetPawn.equipment.MakeRoomFor(this.parent);
                            targetPawn.equipment.AddEquipment(this.parent);
                        }
                    }
                }
            }
            else
            {
                ThingOwner addableSource = target.TryGetInnerInteractableThingOwner();
                if (addableSource != null)
                {
                    this.parent.DeSpawn();
                    addableSource.TryAdd(this.parent);
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
            this.uninstalled = true;

            if (partOrigin is Pawn targetPawn)
            {
                if (this.parent.def != null)
                {
                    //Remove apparel
                    if (this.parent.def.IsApparel)
                    {
                        if (targetPawn.apparel != null)
                        {
                            if (targetPawn.apparel.WornApparel.Contains((Apparel)this.parent))
                            {
                                if (targetPawn.apparel.TryDrop((Apparel)this.parent, out Apparel apparel))
                                {
                                }
                            }
                        }
                    }
                    //Remove equipment
                    if (this.parent.def.IsWeapon)
                    {
                        if (targetPawn.equipment != null)
                        {
                            if (targetPawn.equipment.TryDropEquipment(this.parent, out ThingWithComps dropped, targetPawn.Position))
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
                    addableSource.Remove(this.parent);
                    GenPlace.TryPlaceThing(this.parent, partOrigin.Position, partOrigin.Map, ThingPlaceMode.Near);
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
