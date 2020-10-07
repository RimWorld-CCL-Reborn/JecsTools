using System;
using HarmonyLib;
using Verse;

namespace CompOversizedWeapon
{
    public class CompOversizedWeapon : ThingComp
    {
        public CompProperties_OversizedWeapon Props => props as CompProperties_OversizedWeapon;

        private Func<bool> compDeflectorIsAnimatingNow = AlwaysFalse;

        private static bool AlwaysFalse() => false;

        private static readonly Type compDeflectorType = GenTypes.GetTypeInAnyAssembly("CompDeflector.CompDeflector");

        public bool CompDeflectorIsAnimatingNow => compDeflectorIsAnimatingNow();

        public bool IsOnGround => ParentHolder is Map;

        // This is called during ThingWithComps.InitializeComps, after constructor is called and parent is set.
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (compDeflectorType != null)
            {
                // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
                // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
                // while `isinst` instruction against non-generic type operand like used below is fast.
                // For the optional CompDeflector, we have to use the slower IsAssignableFrom reflection check.
                var comps = parent.AllComps;
                for (int i = 0, count = comps.Count; i < count; i++)
                {
                    var comp = comps[i];
                    var compType = comp.GetType();
                    if (compDeflectorType.IsAssignableFrom(compType))
                    {
                        compDeflectorIsAnimatingNow =
                            (Func<bool>)AccessTools.PropertyGetter(compType, "IsAnimatingNow").CreateDelegate(typeof(Func<bool>), comp);
                        break;
                    }
                }
            }
        }
    }
}
