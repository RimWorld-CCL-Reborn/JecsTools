using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace AbilityUser
{
    public class PawnSummoned : Pawn
    {
        private int ticksToDestroy = 1800; //30 seconds
        private int ticksLeft;
        private Effecter effecter = null;
        private Pawn spawner = null;
        private bool temporary = false;
        
        bool setup = false;

        public Pawn Spawner { get => spawner; set => spawner = value; }
        public bool Temporary { get => temporary; set => temporary = value; }
        public int TicksToDestroy => ticksToDestroy;
        public int TicksLeft => ticksLeft;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ticksLeft = ticksToDestroy;
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public virtual void PostSummonSetup()
        {

        }


        public override void Tick()
        {
            base.Tick();
            if (setup == false && Find.TickManager.TicksGame % 10 == 0)
            {
                setup = true;
                PostSummonSetup();
            }
            if (temporary)
            {
                ticksLeft--;
                if (ticksLeft <= 0) this.Destroy();

                if (Spawned)
                {
                    if (effecter == null)
                    {
                        EffecterDef progressBar = EffecterDefOf.ProgressBar;
                        effecter = progressBar.Spawn();
                    }
                    else
                    {
                        LocalTargetInfo target = this;
                        if (this.Spawned)
                        {
                            effecter.EffectTick(this, TargetInfo.Invalid);
                        }
                        MoteProgressBar mote = ((SubEffecter_ProgressBar)effecter.children[0]).mote;
                        if (mote != null)
                        {
                            float result = 1f - (float)(TicksToDestroy - this.ticksLeft) / (float)TicksToDestroy;

                            mote.progress = Mathf.Clamp01(result);
                            mote.offsetZ = -0.5f;
                        }
                    }
                }
            }
        }

        public override void DeSpawn()
        {
            if (effecter != null) effecter.Cleanup();
            base.DeSpawn();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.temporary, "temporary", false);
            Scribe_Values.Look<int>(ref this.ticksLeft, "ticksLeft", 0);
            Scribe_Values.Look<int>(ref this.ticksToDestroy, "ticksToDestroy", 1800);
        }
    }
}
