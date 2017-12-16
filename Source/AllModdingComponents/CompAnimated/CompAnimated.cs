using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
namespace CompAnimated
{
    public class CompAnimated : ThingComp
    {
        public int ticksToCycle = -1;
        public int curIndex = 0;
        public int MaxFrameIndexMoving => this.Props.movingFrames.Count();
        public int MaxFrameIndexStill => this.Props.stillFrames.Count();

        public Pawn Animatee => this.parent as Pawn;

        public CompProperties_Animated Props => (CompProperties_Animated)this.props;

        public bool dirty = false;

        private Graphic curGraphic = null;
        public Graphic CurGraphic
        {
            get
            {
                if (curGraphic == null || dirty)
                {
                    curGraphic = CompAnimated.ResolveCurGraphic(Animatee, Props, ref curGraphic, ref curIndex, ref ticksToCycle, ref dirty);
                }
                return curGraphic;
            }
        }

        public static Graphic ResolveCurGraphic(Pawn pAnimatee, CompProperties_Animated pProps, ref Graphic pCurGraphic, ref int pCurIndex, ref int pTicksToCycle, ref bool pDirty, bool useBaseGraphic = true)
        {
            Graphic result = pCurGraphic;
            if (pProps.secondsBetweenFrames <= 0.0f) Log.ErrorOnce("CompAnimated :: CompProperties_Animated secondsBetweenFrames needs to be more than 0", 132);

            if (pAnimatee != null && pProps.secondsBetweenFrames > 0.0f && Find.TickManager.TicksGame > pTicksToCycle)
            {
                pTicksToCycle = Find.TickManager.TicksGame + GenTicks.SecondsToTicks(pProps.secondsBetweenFrames);

                if (pAnimatee?.pather?.MovingNow ?? false)
                {
                    pCurIndex = (pCurIndex + 1) % pProps.movingFrames.Count();
                    if (pProps.sound != null) pProps.sound.PlayOneShot(SoundInfo.InMap(pAnimatee));
                    result = CompAnimated.ResolveCycledGraphic(pAnimatee, pProps, pCurIndex);
                }
                else
                {

                    if (!pProps.stillFrames.NullOrEmpty())
                    {
                        pCurIndex = (pCurIndex + 1) % pProps.stillFrames.Count();
                        result = CompAnimated.ResolveCycledGraphic(pAnimatee, pProps, pCurIndex);
                        pDirty = false;
                        return result;
                    }
                    if (useBaseGraphic)
                        result = CompAnimated.ResolveBaseGraphic(pAnimatee);
                    else
                    {
                        //pCurIndex = (pCurIndex + 1) % pProps.movingFrames.Count();
                        result = CompAnimated.ResolveCycledGraphic(pAnimatee, pProps, pCurIndex);
                    }
                }
            }
            pDirty = false;
            return result;
        }

        public static Graphic ResolveCycledGraphic(Pawn pAnimatee, CompProperties_Animated pProps, int pCurIndex)
        {
            Graphic result = null;
            
            if (!pProps.movingFrames.NullOrEmpty() &&
                pAnimatee.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();
                if (pAnimatee.pather.MovingNow)
                {
                    result = pProps.movingFrames[pCurIndex].Graphic;
                    pawnGraphicSet.nakedGraphic = result;
                }
                else if (!pProps.stillFrames.NullOrEmpty())
                {
                    result = pProps.stillFrames[pCurIndex].Graphic;
                    pawnGraphicSet.nakedGraphic = result;

                }
                else
                {
                    result = pProps.movingFrames[pCurIndex].Graphic;
                }
            }
            return result;
        }
        
        public static Graphic ResolveBaseGraphic(Pawn pAnimatee)
        {
            Graphic result = null;
            if (pAnimatee.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                pawnGraphicSet.ClearCache();

                //Duplicated code from -> Verse.PawnGrapic -> ResolveAllGraphics
                PawnKindLifeStage curKindLifeStage = pAnimatee.ageTracker.CurKindLifeStage;
                if (pAnimatee.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null)
                {
                    result = curKindLifeStage.bodyGraphicData.Graphic;
                    pawnGraphicSet.nakedGraphic = result;
                }
                else
                {
                    result = curKindLifeStage.femaleGraphicData.Graphic;
                    pawnGraphicSet.nakedGraphic = result;
                }
                pawnGraphicSet.rottingGraphic = pawnGraphicSet.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin, PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
                if (pAnimatee.RaceProps.packAnimal)
                {
                    pawnGraphicSet.packGraphic = GraphicDatabase.Get<Graphic_Multi>(pawnGraphicSet.nakedGraphic.path + "Pack", ShaderDatabase.Cutout, pawnGraphicSet.nakedGraphic.drawSize, Color.white);
                }
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                {
                    pawnGraphicSet.dessicatedGraphic = curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(pAnimatee);
                }
            }
            return result;
        }

        public override void CompTick()
        {
            CompAnimated.ResolveCurGraphic(Animatee, Props, ref curGraphic, ref curIndex, ref ticksToCycle, ref dirty);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.curIndex, "curIndex", 0);
            Scribe_Values.Look<int>(ref this.ticksToCycle, "ticksToCycle", -1);
        }

    }
}
