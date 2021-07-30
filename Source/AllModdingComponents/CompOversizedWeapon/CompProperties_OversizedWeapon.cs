using UnityEngine;
using Verse;

namespace CompOversizedWeapon
{
    public class CompProperties_OversizedWeapon : CompProperties
    {
        //public SoundDef soundMiss;
        //public SoundDef soundHitPawn;
        //public SoundDef soundHitBuilding;
        //public SoundDef soundExtra;
        //public SoundDef soundExtraTwo;

        public Vector3 northOffset = new Vector3(0, 0, 0);
        public Vector3 eastOffset = new Vector3(0, 0, 0);
        public Vector3 southOffset = new Vector3(0, 0, 0);
        public Vector3 westOffset = new Vector3(0, 0, 0);
        public bool verticalFlipOutsideCombat = false;
        public bool verticalFlipNorth = false;
        public bool isDualWeapon = false;
        public float angleAdjustmentEast = 0f;
        public float angleAdjustmentWest = 0f;
        public float angleAdjustmentNorth = 0f;
        public float angleAdjustmentSouth = 0f;

        public GraphicData groundGraphic = null;

        public CompProperties_OversizedWeapon()
        {
            compClass = typeof(CompOversizedWeapon);
        }

        public float NonCombatAngleAdjustment(Rot4 rotation)
        {
            if (rotation == Rot4.North)
                return angleAdjustmentNorth;
            else if (rotation == Rot4.East)
                return angleAdjustmentEast;
            else if (rotation == Rot4.West)
                return angleAdjustmentWest;
            else
                return angleAdjustmentSouth;
        }

        public Vector3 OffsetFromRotation(Rot4 rotation)
        {
            if (rotation == Rot4.North)
                return northOffset;
            else if (rotation == Rot4.East)
                return eastOffset;
            else if (rotation == Rot4.West)
                return westOffset;
            else
                return southOffset;
        }
    }
}
