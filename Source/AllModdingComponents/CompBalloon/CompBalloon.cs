using UnityEngine;
using Verse;

namespace CompBalloon
{
    public class CompBalloon : ThingComp
    {
        public int curTicks = int.MinValue;

        public bool deflating = true;

        public Pawn Ballooner => parent as Pawn;

        public CompProperties_Balloon Props => (CompProperties_Balloon)props;


        private float MaxTicks => Props.secondsBetweenCycles * GenTicks.TicksPerRealSecond;

        //private float Range => Props.balloonRange.max - Props.balloonRange.min;

        public void ResolveBaseGraphic()
        {
            // TODO: sizeFactor and curSizeAdjustment end up being unused, and so I commented them out - what are they for?
            //var curSizeAdjustment = curTicks * Range;

            //Initialize or Deflate
            if (curTicks == int.MinValue ||
                (!deflating && curTicks >= MaxTicks))
            {
                curTicks = (int)MaxTicks;
                deflating = true;
            }

            //Inflate
            if (deflating && curTicks <= 0)
            {
                deflating = false;
                curTicks = 1;
            }

            //var sizeFactor = deflating ? Props.balloonRange.max - curSizeAdjustment : Props.balloonRange.min + curSizeAdjustment;

            if (Ballooner.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();

                var nakedGraphicDrawSize = pawnGraphicSet.nakedGraphic.drawSize;

                //Duplicated code from -> Verse.PawnGrapic -> ResolveAllGraphics
                var curKindLifeStage = Ballooner.ageTracker.CurKindLifeStage;
                pawnGraphicSet.nakedGraphic = Ballooner.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null
                    ? curKindLifeStage.bodyGraphicData.Graphic
                    : curKindLifeStage.femaleGraphicData.Graphic;
                pawnGraphicSet.rottingGraphic = pawnGraphicSet.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin,
                    PawnGraphicSet.RottingColorDefault, PawnGraphicSet.RottingColorDefault);
                if (Ballooner.RaceProps.packAnimal)
                {
                    pawnGraphicSet.packGraphic = GraphicDatabase.Get<Graphic_Multi>(
                        pawnGraphicSet.nakedGraphic.path + "Pack", ShaderDatabase.Cutout,
                        nakedGraphicDrawSize, Color.white);
                }
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                    pawnGraphicSet.dessicatedGraphic =
                        curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(Ballooner);
            }

            if (deflating)
                curTicks--;
            else
                curTicks++;
        }

        public override void CompTick()
        {
            ResolveBaseGraphic();
        }


        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref curTicks, nameof(curTicks), int.MinValue);
        }
    }
}
