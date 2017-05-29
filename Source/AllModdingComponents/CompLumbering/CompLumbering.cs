using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
//
namespace CompLumbering
{
    internal class CompLumbering : ThingComp
    {
        public int ticksToCycle = -1;
        public bool cycled = false;

        public Pawn lumberer
        {
            get
            {
                return this.parent as Pawn;
            }
        }

        public CompProperties_Lumbering Props
        {
            get
            {
                return (CompProperties_Lumbering)this.props;
            }
        }

        public void ResolveCycledGraphic()
        {
            if (Props.cycledGraphic != null &&
                lumberer.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();
                pawnGraphicSet.nakedGraphic = Props.cycledGraphic.Graphic;
            }
        }

        public void ResolveBaseGraphic()
        {
            if (Props.cycledGraphic != null &&
                lumberer.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();

                //Duplicated code from -> Verse.PawnGrapic -> ResolveAllGraphics
                PawnKindLifeStage curKindLifeStage = lumberer.ageTracker.CurKindLifeStage;
                if (lumberer.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null)
                {
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.bodyGraphicData.Graphic;
                }
                else
                {
                    pawnGraphicSet.nakedGraphic = curKindLifeStage.femaleGraphicData.Graphic;
                }
                pawnGraphicSet.rottingGraphic = pawnGraphicSet.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin, PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
                if (lumberer.RaceProps.packAnimal)
                {
                    pawnGraphicSet.packGraphic = GraphicDatabase.Get<Graphic_Multi>(pawnGraphicSet.nakedGraphic.path + "Pack", ShaderDatabase.Cutout, pawnGraphicSet.nakedGraphic.drawSize, Color.white);
                }
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                {
                    pawnGraphicSet.dessicatedGraphic = curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(lumberer);
                }
            }
        }

        public override void CompTick()
        {
            if (Props.secondsBetweenSteps <= 0.0f) Log.ErrorOnce("CompLumbering :: CompProperties_Lumbering secondsBetweenSteps needs to be more than 0", 132);
            if (Props.secondsPerStep <= 0.0f) Log.ErrorOnce("CompLumbering :: CompProperties_Lumbering secondsPerStep needs to be more than 0", 133);

            if (lumberer != null && Props.secondsPerStep > 0.0f && Find.TickManager.TicksGame > ticksToCycle)
            {
                if (lumberer?.pather?.MovingNow ?? false)
                {
                        cycled = !cycled;
                        ticksToCycle = Find.TickManager.TicksGame + GenTicks.SecondsToTicks(Props.secondsPerStep);
                        if (Props.sound != null) Props.sound.PlayOneShot(SoundInfo.InMap(lumberer));
                        if (cycled)
                        {
                            ResolveCycledGraphic();
                        }
                        else
                        {
                            //Log.Message("1b");
                            ResolveBaseGraphic();
                        }
                        lumberer.stances.StaggerFor(GenTicks.SecondsToTicks(Props.secondsBetweenSteps));
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
