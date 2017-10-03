using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace JecsTools
{
    public class CaravanJobDef : Def
    {
        public Type driverClass;

        [MustTranslate]
        public string reportString = "Doing something.";

        public bool playerInterruptible = true;

        public CheckJobOverrideOnDamageMode checkOverrideOnDamage = CheckJobOverrideOnDamageMode.Always;

        public static CaravanJobDef Named(string defName)
        {
            return DefDatabase<CaravanJobDef>.GetNamed(defName, true);
        }


        //public bool alwaysShowWeapon;

        //public bool neverShowWeapon;

        public bool suspendable = true;

        public bool casualInterruptible = true;

        public bool collideWithCaravans;

        //public bool makeTargetPrisoner;

        public int joyDuration = 4000;

        public int joyMaxParticipants = 1;

        public float joyGainRate = 1f;

        public SkillDef joySkill;

        public float joyXpPerTick;

        public JoyKindDef joyKind;

        //public Rot4 faceDir = Rot4.Invalid;

        [DebuggerHidden]
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (this.joySkill != null && this.joyXpPerTick == 0f)
            {
                yield return "funSkill is not null but funXpPerTick is zero";
            }
        }
    }
}
