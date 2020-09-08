using System;
using HarmonyLib;
using Verse;

namespace CompOversizedWeapon
{
    public static class CompOversizedWeaponUtility
    {
        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompOversizedWeapon GetCompOversizedWeapon(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompOversizedWeapon comp)
                    return comp;
            }
            return null;
        }

        public static CompOversizedWeapon TryGetCompOversizedWeapon(this Thing thing)
        {
            return thing is ThingWithComps thingWithComps ? thingWithComps.GetCompOversizedWeapon() : null;
        }
    }

    public class CompOversizedWeapon : ThingComp
    {
        public CompProperties_OversizedWeapon Props => props as CompProperties_OversizedWeapon;

        public CompOversizedWeapon()
        {
            if (!(props is CompProperties_OversizedWeapon))
                props = new CompProperties_OversizedWeapon();
        }

        private CompEquippable compEquippable;
        private Func<bool> compDeflectorIsAnimatingNow;

        private static readonly Type compDeflectorType = GenTypes.GetTypeInAnyAssembly("CompDeflector.CompDeflector");

        public CompEquippable GetEquippable => compEquippable;

        public Pawn GetPawn => GetEquippable?.verbTracker?.PrimaryVerb?.CasterPawn;

        public bool CompDeflectorIsAnimatingNow => compDeflectorIsAnimatingNow?.Invoke() ?? false;

        private bool isEquipped = false;
        public bool IsEquipped
        {
            get
            {
                if (Find.TickManager.TicksGame % 60 != 0) return isEquipped;
                isEquipped = GetPawn != null;
                return isEquipped;
            }
        }

        private bool firstAttack = false;
        public bool FirstAttack
        {
            get => firstAttack;
            set => firstAttack = value;
        }

        // This is called during ThingWithComps.InitializeComps, after constructor is called and parent is set.
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
            // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
            // while `isinst` instruction against non-generic type operand like used below is fast.
            // For the optional CompDeflector, we have to use the slower IsAssignableFrom reflection check.
            var comps = parent.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                var comp = comps[i];
                if (comp is CompEquippable compEquippable)
                    this.compEquippable = compEquippable;
                else if (compDeflectorType != null)
                {
                    var compType = comp.GetType();
                    if (compDeflectorType.IsAssignableFrom(compType))
                        compDeflectorIsAnimatingNow =
                            (Func<bool>)AccessTools.PropertyGetter(compType, "IsAnimatingNow").CreateDelegate(typeof(Func<bool>), comp);
                }
            }
        }
    }
}
