using UnityEngine;
using Verse;

namespace AbilityUser
{
    public class Projectile_Ability : Projectile_AbilityBase
    {
        public int TicksToImpact => ticksToImpact;

        public Vector3 ProjectileDrawPos
        {
            get
            {
                if (selectedTarget != null)
                    return selectedTarget.DrawPos;
                if (targetVec != default)
                    return targetVec;
                return ExactPosition;
            }
        }

        public override void Draw()
        {
            if (selectedTarget != null || targetVec != default)
            {
                var drawPos = ProjectileDrawPos;
                drawPos.y = 3;
                var s = new Vector3(2.5f, 1f, 2.5f);
                var matrix = Matrix4x4.TRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, def.DrawMatSingle, 0);
            }
            Comps_PostDraw();
        }

        public override void Impact_Override(Thing hitThing)
        {
            base.Impact_Override(hitThing);
            if (hitThing != null)
            {
                var dinfo = new DamageInfo(def.projectile.damageDef, def.projectile.GetDamageAmount(1f),
                    def.projectile.GetArmorPenetration(1f), ExactRotation.eulerAngles.y,
                    launcher, weapon: equipmentDef);
                //Log.Message($"Projectile_Ability.Impact_Override({this}, {hitThing}) dinfo={dinfo}");
                hitThing.TakeDamage(dinfo);
                PostImpactEffects(hitThing);
            }
        }

        public virtual void PostImpactEffects(Thing hitThing)
        {
        }

        public virtual bool IsInIgnoreHediffList(Hediff hediff)
        {
            if (hediff != null)
                if (hediff.def != null)
                {
                    foreach (var abilityUser in Caster.GetCompAbilityUsers())
                    {
                        if (abilityUser.IgnoredHediffs() != null)
                            if (abilityUser.IgnoredHediffs().Contains(hediff.def))
                                return true;
                    }
                }
            return false;
        }
    }
}
