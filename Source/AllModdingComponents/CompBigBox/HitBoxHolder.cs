using Verse;

namespace DefModExtension_BigBox
{
    internal class ThingWithComps_HitBox : ThingWithComps
    {
        public Pawn master = null;

        public override void Draw()
        {
        }

        public override void Tick()
        {
            base.Tick();
            CheckNeedsDestruction();
        }

        public void CheckNeedsDestruction()
        {
            if (master != null && Spawned)
            {
                if (!master.Spawned)
                {
                    Destroy(0);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref master, nameof(master));
        }
    }
}
