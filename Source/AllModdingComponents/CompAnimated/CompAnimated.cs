using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompAnimated
{
    public class CompAnimated : ThingComp
    {
        protected Graphic curGraphic;
        public int curIndex;

        public bool dirty;
        public int ticksToCycle = -1;
        public int MaxFrameIndexMoving => Props.movingFrames.Count();
        public int MaxFrameIndexStill => Props.stillFrames.Count();

        private static bool AsPawn(ThingWithComps pAnimatee, out Pawn pawn)
        {
            bool asPawn = pAnimatee is Pawn;
            pawn = asPawn? (Pawn) pAnimatee : null;
            return asPawn;
        }
        
        /**
        * render over thing when not a pawn; rather than use as base layer like the PawnGraphicSet does for the pawns graphics managment
        */
        public override void PostDraw()
        {
            if (parent is Pawn) return;
            base.PostDraw();
            if (curGraphic != null)
                Render();
        }

        public virtual void Render()
        {
            Vector3 drawPos = this.parent.DrawPos;
            curGraphic.Draw(drawPos, Rot4.North, this.parent, 0f);
        }

        public CompProperties_Animated Props => (CompProperties_Animated) props;

        public Graphic CurGraphic
        {
            get
            {
                if (curGraphic == null || dirty || !(parent is Pawn)) //Buildings and the like use us as a renderer.
                {
                    curGraphic = DefaultGraphic();
                }

                return curGraphic;
            }
        }

        [Obsolete("ResolveCurGraphic for pawns is deprecated, please use ResolveCurGraphic for ThingWithComps instead.")]
        public static Graphic ResolveCurGraphic(Pawn pawn, CompProperties_Animated pProps, ref Graphic result,
            ref int pCurIndex, ref int pTicksToCycle, ref bool pDirty, bool useBaseGraphic = true)
        {
            return ResolveCurGraphic(pawn as ThingWithComps, pProps, ref result, ref pCurIndex, ref pTicksToCycle, ref pDirty, useBaseGraphic);
        }

        public static Graphic ResolveCurGraphic(ThingWithComps pThingWithComps, CompProperties_Animated pProps, ref Graphic result,
            ref int pCurIndex, ref int pTicksToCycle, ref bool pDirty, bool useBaseGraphic = true)
        {
            if (pProps.secondsBetweenFrames <= 0.0f)
                Log.ErrorOnce("CompAnimated :: CompProperties_Animated secondsBetweenFrames needs to be more than 0",
                    132);

            if (pThingWithComps != null && pProps.secondsBetweenFrames > 0.0f && Find.TickManager.TicksGame > pTicksToCycle)
            {
                pTicksToCycle = Find.TickManager.TicksGame + pProps.secondsBetweenFrames.SecondsToTicks();

                bool asPawn = AsPawn(pThingWithComps, out var pAnimatee);
                if (asPawn && (pAnimatee?.pather?.MovingNow ?? false))
                {
                    pCurIndex = (pCurIndex + 1) % pProps.movingFrames.Count();
                    pProps.sound?.PlayOneShot(SoundInfo.InMap(pAnimatee));
                    result = ResolveCycledGraphic(pThingWithComps, pProps, pCurIndex);
                }
                else
                {
                    if (!pProps.stillFrames.NullOrEmpty())
                    {
                        //Log.Message("ticked still");
                        pCurIndex = (pCurIndex + 1) % pProps.stillFrames.Count();
                        result = ResolveCycledGraphic(pThingWithComps, pProps, pCurIndex);
                        pDirty = false;
                        return result;
                    }
                    if (pAnimatee!=null && useBaseGraphic)
                        result = ResolveBaseGraphic(pAnimatee);
                    else
                        result = ResolveCycledGraphic(pThingWithComps, pProps, pCurIndex);
                }
            }
            pDirty = false;
            return result;
        }

        /** Primary call to above */
        private Graphic DefaultGraphic()
        {
            Graphic proxyGraphic = null;
            var resolveCurGraphic = ResolveCurGraphic(parent, Props, ref proxyGraphic, ref curIndex, ref ticksToCycle, ref dirty);
            if (proxyGraphic != null)
            {
                curGraphic = proxyGraphic;
                NotifyGraphicsChange();
            }
           
            return resolveCurGraphic ?? curGraphic;
        }

        [Obsolete("ResolveCycledGraphic for pawns is deprecated, please use ResolveCycledGraphic for ThingWithComps instead.")]
        public static Graphic ResolveCycledGraphic(Pawn pAnimatee, CompProperties_Animated pProps, int pCurIndex)
        {
            return ResolveCycledGraphic(pAnimatee as ThingWithComps, pProps, pCurIndex);
        }

        public static Graphic ResolveCycledGraphic(ThingWithComps pAnimatee, CompProperties_Animated pProps, int pCurIndex)
        {
            Graphic result = null;
            bool haveMovingFrames = !pProps.movingFrames.NullOrEmpty();
            if (!pProps.movingFrames.NullOrEmpty() &&
                AsPawn(pAnimatee, out var pPawn) &&
                pPawn.Drawer?.renderer?.graphics is PawnGraphicSet pawnGraphicSet)
            {
                /*Start Pawn*/
                pawnGraphicSet.ClearCache();
                
                if (haveMovingFrames && AsPawn(pAnimatee, out var p) && (p?.pather?.MovingNow ?? false))
                {
                    result = pProps.movingFrames[pCurIndex].Graphic;
                    pawnGraphicSet.nakedGraphic = result;
                }
                else if (!pProps.stillFrames.NullOrEmpty())
                {
                    result = pProps.stillFrames[pCurIndex].Graphic;
                    pawnGraphicSet.nakedGraphic = result;
                }
                else if(haveMovingFrames)
                {
                    result = pProps.movingFrames[pCurIndex].Graphic;
                }
            } /*Start Non Pawn*/ else if (!pProps.stillFrames.NullOrEmpty())
            {
                result = pProps.stillFrames[pCurIndex].Graphic;
            }
            else if(haveMovingFrames)
            {
                result = pProps.movingFrames[pCurIndex].Graphic;
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
                var curKindLifeStage = pAnimatee.ageTracker.CurKindLifeStage;
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
                pawnGraphicSet.rottingGraphic = pawnGraphicSet.nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin,
                    PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
                if (pAnimatee.RaceProps.packAnimal)
                    pawnGraphicSet.packGraphic = GraphicDatabase.Get<Graphic_Multi>(
                        pawnGraphicSet.nakedGraphic.path + "Pack", ShaderDatabase.Cutout,
                        pawnGraphicSet.nakedGraphic.drawSize, Color.white);
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                    pawnGraphicSet.dessicatedGraphic =
                        curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(pAnimatee);
            }
            return result;
        }

        public override void CompTick()
        {
            curGraphic = DefaultGraphic(); //update cache on tick as well
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref curIndex, "curIndex", 0);
            Scribe_Values.Look(ref ticksToCycle, "ticksToCycle", -1);
        }

        public virtual void NotifyGraphicsChange()
        {
        }
    }
}
