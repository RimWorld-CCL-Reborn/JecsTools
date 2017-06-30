//
namespace CompLumbering
{
    internal class CompLumbering : ThingComp
    {
        public int ticksToCycle = -1;
        public bool cycled = false;

        public Pawn Lumberer => this.parent as Pawn;

        public CompProperties_Lumbering Props => (CompProperties_Lumbering)this.props;

        public void ResolveCycledGraphic()
        {
            if (this.Props.cycledGraphic != null &&
                Lumberer.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();
                pawnGraphicSet.nakedGraphic = this.Props.cycledGraphic.Graphic;
            }
        }

        public void ResolveBaseGraphic()
        {
            if (this.Props.cycledGraphic != null &&
                Lumberer.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();

                //Duplicated code from -> Verse.PawnGrapic -> ResolveAllGraphics
                PawnKindLifeStage curKindLifeStage = this.Lumberer.ageTracker.CurKindLifeStage;
                if (this.Lumberer.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null)
                {
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.bodyGraphicData.Graphic;
                }
                else
                {
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.femaleGraphicData.Graphic;
                }
                pawnGraphicSet.rottingGraphic = pawnGraphicSet.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin, PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
                if (this.Lumberer.RaceProps.packAnimal)
                {
                    pawnGraphicSet.packGraphic = GraphicDatabase.Get<Graphic_Multi>(pawnGraphicSet.nakedGraphic.path + "Pack", ShaderDatabase.Cutout, pawnGraphicSet.nakedGraphic.drawSize, Color.white);
                }
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                {
                    pawnGraphicSet.dessicatedGraphic = curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(this.Lumberer);
                }
            }
        }

        public override void CompTick()
        {
            if (this.Props.secondsBetweenSteps <= 0.0f) Log.ErrorOnce("CompLumbering :: CompProperties_Lumbering secondsBetweenSteps needs to be more than 0", 132);
            if (this.Props.secondsPerStep <= 0.0f) Log.ErrorOnce("CompLumbering :: CompProperties_Lumbering secondsPerStep needs to be more than 0", 133);

            if (this.Lumberer != null && this.Props.secondsPerStep > 0.0f && Find.TickManager.TicksGame > this.ticksToCycle)
            {
                if (this.Lumberer?.pather?.MovingNow ?? false)
                {
                    this.cycled = !this.cycled;
                    this.ticksToCycle = Find.TickManager.TicksGame + GenTicks.SecondsToTicks(this.Props.secondsPerStep);
                        if (this.Props.sound != null) this.Props.sound.PlayOneShot(SoundInfo.InMap(this.Lumberer));
                        if (this.cycled)
                        {
                            ResolveCycledGraphic();
                        }
                        else
                        {
                            //Log.Message("1b");
                            ResolveBaseGraphic();
                        }
                    this.Lumberer.stances.StaggerFor(GenTicks.SecondsToTicks(this.Props.secondsBetweenSteps));
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.cycled, "cycled", false);
            Scribe_Values.Look<int>(ref this.ticksToCycle, "ticksToCycle", -1);
        }

    }
}
