using RimWorld;
using Verse;

namespace CompInstalledPart
{
    public class CompInstalledPart : ThingComp
    {
        public bool uninstalled;

        public CompProperties_InstalledPart Props => (CompProperties_InstalledPart) props;

        public void GiveInstallJob(Pawn actor, Thing target)
        {
            if (actor?.Faction is Faction actorFac && target?.Faction is Faction targetFac)
                if (actorFac == targetFac)
                {
                    var newJob = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("CompInstalledPart_InstallPart"), parent, target,
                        target.Position);
                    newJob.count = 2;
                    actor.jobs?.TryTakeOrderedJob(newJob);
                }
                else if (actorFac != targetFac)
                {
                    Messages.Message("CompInstalledPart_WrongFaction".Translate(), MessageTypeDefOf.RejectInput);
                }
        }

        public void GiveUninstallJob(Pawn actor, Thing target)
        {
            if (actor?.Faction is Faction actorFac && target?.Faction is Faction targetFac)
                if (actorFac == targetFac)
                {
                    var newJob = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("CompInstalledPart_UninstallPart"), parent,
                        target, target.Position);
                    newJob.count = 1;
                    actor.jobs?.TryTakeOrderedJob(newJob);
                }
                else if (actorFac != targetFac)
                {
                    Messages.Message("CompInstalledPart_WrongFaction".Translate(), MessageTypeDefOf.RejectInput);
                }
        }

        public void Notify_Installed(Pawn installer, Thing target)
        {
            uninstalled = false;

            //Installed on a character
            if (target is Pawn targetPawn && parent.def != null)
            {
                //Add apparel
                if (parent.def.IsApparel && targetPawn.apparel != null)
                {
                    parent.DeSpawn();
                    targetPawn.apparel.Wear((Apparel) parent);
                }

                //Add equipment
                if (parent.def.IsWeapon)
                {
                    if (targetPawn.equipment.Primary?.GetComp<CompInstalledPart>() is CompInstalledPart otherPart)
                        otherPart.Notify_Uninstalled(installer, targetPawn);
                    parent.DeSpawn();
                    targetPawn.equipment.MakeRoomFor(parent);
                    targetPawn.equipment.AddEquipment(parent);
                }
            }
            else
            {
                var addableSource = target.TryGetInnerInteractableThingOwner();
                if (addableSource != null)
                {
                    parent.DeSpawn();
                    addableSource.TryAdd(parent);
                }
            }

            Messages.Message(
                "CompInstalledPart_Installed".Translate(installer.LabelShort, parent.LabelShort, target.LabelShort),
                MessageTypeDefOf.PositiveEvent);
        }

        public void Notify_Uninstalled(Pawn uninstaller, Thing partOrigin)
        {
            uninstalled = true;

            if (partOrigin is Pawn targetPawn)
            {
                if (parent.def != null)
                {
                    //Remove apparel
                    if (parent.def.IsApparel && targetPawn.apparel is Pawn_ApparelTracker tracker &&
                        tracker.WornApparel.Contains((Apparel) parent) &&
                        tracker.TryDrop((Apparel) parent, out _))
                    {
                    }
                    //Remove equipment
                    if (parent.def.IsWeapon && targetPawn.equipment is Pawn_EquipmentTracker eqTracker &&
                        eqTracker.TryDropEquipment(parent, out _, targetPawn.Position))
                    {
                    }
                }
            }
            else
            {
                var addableSource = partOrigin.TryGetInnerInteractableThingOwner();
                if (addableSource != null)
                {
                    addableSource.Remove(parent);
                    GenPlace.TryPlaceThing(parent, partOrigin.Position, partOrigin.Map, ThingPlaceMode.Near);
                }
            }

            Messages.Message(
                "CompInstalledPart_Uninstalled".Translate(uninstaller.LabelShort, parent.LabelShort,
                    partOrigin.LabelShort), MessageTypeDefOf.PositiveEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref uninstalled, "uninstalled", false);
        }
    }
}