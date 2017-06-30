namespace CompInstalledPart
{
    public class CompInstalledPart : ThingComp
    {
        public bool uninstalled = false;

        public CompProperties_InstalledPart Props => (CompProperties_InstalledPart)this.props;

        public void GiveInstallJob(Pawn actor, Thing target)
        {
            var actorFac = actor?.Faction;
            var targetFac = target?.Faction;
            if (actorFac != null && targetFac != null)
            { 
                if (actorFac == targetFac)
                {
                    Job newJob = new Job(DefDatabase<JobDef>.GetNamed("CompInstalledPart_InstallPart"), this.parent, target, target.Position);
                    newJob.count = 2;
                    actor?.jobs?.TryTakeOrderedJob(newJob);
                }
                else if (actorFac != targetFac) Messages.Message("CompInstalledPart_WrongFaction".Translate(), MessageSound.RejectInput);
            }
        }

        public void GiveUninstallJob(Pawn actor, Thing target)
        {

            var actorFac = actor?.Faction;
            var targetFac = target?.Faction;
            if (actorFac != null && targetFac != null)
            {
                if (actorFac == targetFac)
                {
                    Job newJob = new Job(DefDatabase<JobDef>.GetNamed("CompInstalledPart_UninstallPart"), this.parent, target, target.Position);
                    newJob.count = 1;
                    actor?.jobs?.TryTakeOrderedJob(newJob);
                }
                else if (actorFac != targetFac) Messages.Message("CompInstalledPart_WrongFaction".Translate(), MessageSound.RejectInput);
            }
        }

        public void Notify_Installed(Pawn installer, Thing target)
        {
            this.uninstalled = false;

            //Installed on a character
            if (target is Pawn targetPawn && this.parent.def != null)
            {
             
                //Add apparel
                if (this.parent.def.IsApparel && targetPawn.apparel != null)
                {
                  this.parent.DeSpawn();
                  targetPawn.apparel.Wear((Apparel)this.parent);
                }

                //Add equipment
                if (this.parent.def.IsWeapon)
                {
                    if (targetPawn?.equipment?.Primary?.GetComp<CompInstalledPart>() is CompInstalledPart otherPart)
                    {
                        otherPart.Notify_Uninstalled(installer, targetPawn);
                    }
                    this.parent.DeSpawn();
                    targetPawn.equipment.MakeRoomFor(this.parent);
                    targetPawn.equipment.AddEquipment(this.parent);
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
                    if (this.parent.def.IsApparel && targetPawn.apparel is Pawn_ApparelTracker tracker &&
                        tracker.WornApparel.Contains((Apparel)this.parent) && tracker.TryDrop((Apparel)this.parent, out Apparel apparel))
                    {

                    }
                    //Remove equipment
                    if (this.parent.def.IsWeapon && targetPawn.equipment is Pawn_EquipmentTracker eqTracker &&
                        eqTracker.TryDropEquipment(this.parent, out ThingWithComps dropped, targetPawn.Position))
                    {

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
