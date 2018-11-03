using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class ThinkNode_ConditionalDelay : ThinkNode_Conditional
    {
        public int min = 1000, max= 2000;

        private Dictionary<string, int> delays = new Dictionary<string, int>();
        protected override bool Satisfied(Pawn pawn)
        {
            var key = GetKey(pawn);
            if (delays.TryGetValue(key, out var tick))
            {
                var tik = Find.TickManager.TicksGame;

                if (tik > tick)
                {
                    AddDelay(key);
                    return true;
                }
            }
            else
            {
                AddDelay(key);
            }
            return false;
        }

        public virtual string GetKey(Pawn pawn)
        {
            return pawn.GetUniqueLoadID();
        }

        public virtual void AddDelay(string key)
        {
            delays[key] = Find.TickManager.TicksGame + Rand.Range(min, max);
        }
    }
}