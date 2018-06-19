using RimWorld;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    public class PawnSummoned : Pawn
    {
        private Effecter effecter;

        private bool setup;
        private bool temporary;
        private int ticksLeft;
        private int ticksToDestroy = 1800; //30 seconds

        public Pawn Spawner { get; set; } = null;

        public bool Temporary
        {
            get => temporary;
            set => temporary = value;
        }

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
                if (ticksLeft <= 0) Destroy();

                if (Spawned)
                    if (effecter == null)
                    {
                        var progressBar = EffecterDefOf.ProgressBar;
                        effecter = progressBar.Spawn();
                    }
                    else
                    {
                        LocalTargetInfo target = this;
                        if (Spawned)
                            effecter.EffectTick(this, TargetInfo.Invalid);
                        var mote = ((SubEffecter_ProgressBar) effecter.children[0]).mote;
                        if (mote != null)
                        {
                            var result = 1f - (TicksToDestroy - ticksLeft) / (float) TicksToDestroy;

                            mote.progress = Mathf.Clamp01(result);
                            mote.offsetZ = -0.5f;
                        }
                    }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (effecter != null) effecter.Cleanup();
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref temporary, "temporary", false);
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
            Scribe_Values.Look(ref ticksToDestroy, "ticksToDestroy", 1800);
        }
    }
}