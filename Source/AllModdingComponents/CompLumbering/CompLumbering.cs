using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompLumbering
{
    public class CompLumbering : ThingComp
    {
        public bool cycled;
        public int ticksToCycle = -1;

        public Pawn Lumberer => parent as Pawn;

        public CompProperties_Lumbering Props => (CompProperties_Lumbering) props;

        public void ResolveCycledGraphic()
        {
            if (Props.cycledGraphic != null &&
                Lumberer.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();
                pawnGraphicSet.nakedGraphic = Props.cycledGraphic.Graphic;
            }
        }

        public void ResolveBaseGraphic()
        {
            if (Props.cycledGraphic != null &&
                Lumberer.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();

                //Duplicated code from -> Verse.PawnGrapic -> ResolveAllGraphics
                var curKindLifeStage = Lumberer.ageTracker.CurKindLifeStage;
                if (Lumberer.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null)
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.bodyGraphicData.Graphic;
                else
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.femaleGraphicData.Graphic;
                pawnGraphicSet.rottingGraphic = pawnGraphicSet.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin,
                    PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
                if (Lumberer.RaceProps.packAnimal)
                    pawnGraphicSet.packGraphic = GraphicDatabase.Get<Graphic_Multi>(
                        pawnGraphicSet.nakedGraphic.path + "Pack", ShaderDatabase.Cutout,
                        pawnGraphicSet.nakedGraphic.drawSize, Color.white);
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                    pawnGraphicSet.dessicatedGraphic =
                        curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(Lumberer);
            }
        }

        public override void CompTick()
        {
            if (Props.secondsBetweenSteps <= 0.0f)
                Log.ErrorOnce("CompLumbering :: CompProperties_Lumbering secondsBetweenSteps needs to be more than 0",
                    132);
            if (Props.secondsPerStep <= 0.0f)
                Log.ErrorOnce("CompLumbering :: CompProperties_Lumbering secondsPerStep needs to be more than 0", 133);

            if (Lumberer != null && Props.secondsPerStep > 0.0f && Find.TickManager.TicksGame > ticksToCycle)
                if (Lumberer?.pather?.MovingNow ?? false)
                {
                    cycled = !cycled;
                    ticksToCycle = Find.TickManager.TicksGame + Props.secondsPerStep.SecondsToTicks();
                    if (Props.sound != null) Props.sound.PlayOneShot(SoundInfo.InMap(Lumberer));
                    if (cycled)
                        ResolveCycledGraphic();
                    else
                        ResolveBaseGraphic();
                    if (Props.staggerEffect) Lumberer.stances.StaggerFor(Props.secondsBetweenSteps.SecondsToTicks());
                }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref cycled, "cycled", false);
            Scribe_Values.Look(ref ticksToCycle, "ticksToCycle", -1);
        }
    }
}