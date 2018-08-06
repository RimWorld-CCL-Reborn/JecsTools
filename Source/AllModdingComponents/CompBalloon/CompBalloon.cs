using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompBalloon
{
    public class CompBalloon : ThingComp
    {
        public int curTicks = Int32.MinValue;

        public bool deflating = true;

        public Pawn Ballooner => parent as Pawn;

        public CompProperties_Balloon Props => (CompProperties_Balloon) props;


        private float MaxTicks => (Props.secondsBetweenCycles * 60);
        
        private float Range => Props.balloonRange.max - Props.balloonRange.min;
        
        public void ResolveBaseGraphic()
        {
            var sizeFactor = 1.0f;
            var curSizeAdjustment = curTicks * Range;
            
            //Initialize or Deflate
            if (curTicks == Int32.MinValue ||
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

            if (deflating)
            {
                sizeFactor = Props.balloonRange.max - curSizeAdjustment;
            }
            else
            {
                sizeFactor = Props.balloonRange.min + curSizeAdjustment;
            }
            
            if (Ballooner.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();
                
                var nakedGraphicDrawSize = pawnGraphicSet.nakedGraphic.drawSize;
                
                //Duplicated code from -> Verse.PawnGrapic -> ResolveAllGraphics
                var curKindLifeStage = Ballooner.ageTracker.CurKindLifeStage;
                if (Ballooner.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null)
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.bodyGraphicData.Graphic;
                else
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.femaleGraphicData.Graphic;
                pawnGraphicSet.rottingGraphic = pawnGraphicSet.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin,
                    PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
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

            if (deflating) curTicks--;
            else curTicks++;
        }

        public override void CompTick()
        {
            ResolveBaseGraphic();
        }
        

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref curTicks, "curTicks", Int32.MinValue);
        }
    }
}