using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace JecsTools
{
    public class WorldSubEffecter_ProgressBar : WorldSubEffecter
    {
        private const float Width = 0.68f;

        private const float Height = 0.12f;

        public MoteProgressBar mote;

        public WorldSubEffecter_ProgressBar(SubEffecterDef def) : base(def)
        {
        }

        public override void SubEffectTick(GlobalTargetInfo A, GlobalTargetInfo B)
        {
            if (this.mote == null)
            {
                this.mote = (MoteProgressBar)MoteMaker.MakeInteractionOverlay(this.def.moteDef, A, B);
                this.mote.exactScale.x = 0.68f;
                this.mote.exactScale.z = 0.12f;
            }
        }

        public override void SubCleanup()
        {
            if (this.mote != null && !this.mote.Destroyed)
            {
                this.mote.Destroy(DestroyMode.Vanish);
            }
        }

        // RimWorld.MoteMaker
        public static MoteDualAttached MakeInteractionOverlay(ThingDef moteDef, GlobalTargetInfo A, GlobalTargetInfo B)
        {
            MoteDualAttached moteDualAttached = (MoteDualAttached)ThingMaker.MakeThing(moteDef, null);
            moteDualAttached.Scale = 0.5f;
            moteDualAttached.Attach(A, B);
            GenSpawn.Spawn(moteDualAttached, A.Cell, A.Map ?? B.Map);
            return moteDualAttached;
        }

    }



}
