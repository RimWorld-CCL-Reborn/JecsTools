using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace JecsTools
{
    public class WorldEffecter
    {
        public EffecterDef def;

        public List<SubEffecter> children = new List<SubEffecter>();

        public WorldEffecter(EffecterDef def)
        {
            this.def = def;
            for (int i = 0; i < def.children.Count; i++)
            {
                this.children.Add(def.children[i].Spawn());
            }
        }

        public void EffectTick(GlobalTargetInfo A, GlobalTargetInfo B)
        {
            for (int i = 0; i < this.children.Count; i++)
            {
                this.children[i].SubEffectTick(A, B);
            }
        }

        public void Trigger(GlobalTargetInfo A, GlobalTargetInfo B)
        {
            for (int i = 0; i < this.children.Count; i++)
            {
                this.children[i].SubTrigger(A, B);
            }
        }

        public void Cleanup()
        {
            for (int i = 0; i < this.children.Count; i++)
            {
                this.children[i].SubCleanup();
            }
        }
    }

}
