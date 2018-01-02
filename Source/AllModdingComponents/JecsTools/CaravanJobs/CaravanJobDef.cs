using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class CaravanJobDef : Def
    {
        public bool casualInterruptible = true;

        public CheckJobOverrideOnDamageMode checkOverrideOnDamage = CheckJobOverrideOnDamageMode.Always;

        public bool collideWithCaravans;
        public Type driverClass;

        //public bool makeTargetPrisoner;

        public int joyDuration = 4000;

        public float joyGainRate = 1f;

        public JoyKindDef joyKind;

        public int joyMaxParticipants = 1;

        public SkillDef joySkill;

        public float joyXpPerTick;

        public bool playerInterruptible = true;

        [MustTranslate] public string reportString = "Doing something.";


        //public bool alwaysShowWeapon;

        //public bool neverShowWeapon;

        public bool suspendable = true;

        public static CaravanJobDef Named(string defName)
        {
            return DefDatabase<CaravanJobDef>.GetNamed(defName, true);
        }

        //public Rot4 faceDir = Rot4.Invalid;

        [DebuggerHidden]
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors())
                yield return e;
            if (joySkill != null && joyXpPerTick == 0f)
                yield return "funSkill is not null but funXpPerTick is zero";
        }
    }
}