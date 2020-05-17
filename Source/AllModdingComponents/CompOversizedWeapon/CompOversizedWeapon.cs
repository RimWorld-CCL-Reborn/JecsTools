using System;
using System.Linq;
using HarmonyLib;
using Verse;

namespace CompOversizedWeapon
{
    public class CompOversizedWeapon : ThingComp
    {
        public CompProperties_OversizedWeapon Props => props as CompProperties_OversizedWeapon;

        public CompOversizedWeapon()
        {
            if (!(props is CompProperties_OversizedWeapon))
                props = new CompProperties_OversizedWeapon();
        }

        private bool initComps = false;
        private CompEquippable compEquippable;
        private Func<bool> compDeflectorIsAnimatingNow;

        private void InitCompsAsNeeded()
        {
            if (!initComps)
            {
                if (parent == null) return;
                compEquippable = parent.GetComp<CompEquippable>();
                var deflector = parent.AllComps.FirstOrDefault(y =>
                    y.GetType().ToString() == "CompDeflector.CompDeflector" ||
                    y.GetType().BaseType.ToString() == "CompDeflector.CompDeflector");
                if (deflector != null)
                {
                    compDeflectorIsAnimatingNow =
                        (Func<bool>) AccessTools.PropertyGetter(deflector.GetType(), "IsAnimatingNow").CreateDelegate(
                            typeof(Func<bool>), deflector);
                }
                initComps = true;
            }
        }

        public CompEquippable GetEquippable
        {
            get
            {
                InitCompsAsNeeded();
                return compEquippable;
            }
        }

        public Pawn GetPawn => GetEquippable?.verbTracker?.PrimaryVerb?.CasterPawn;

        public bool CompDeflectorIsAnimatingNow
        {
            get
            {
                InitCompsAsNeeded();
                if (compDeflectorIsAnimatingNow != null)
                    return compDeflectorIsAnimatingNow();
                return false;
            }
        }

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
    }
}