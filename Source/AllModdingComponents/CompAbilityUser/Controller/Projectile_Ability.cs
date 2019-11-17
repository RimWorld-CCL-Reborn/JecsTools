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
                if (targetVec != null)
                    return targetVec;
                return ExactPosition;
            }
        }

        public override void Draw()
        {
            if (selectedTarget != null || targetVec != null)
            {
                var vector = ProjectileDrawPos;
                var distance = destination - origin;
                var curpos = destination - Position.ToVector3();
                var angle = 0f;
                var mat = Graphic.MatSingle;
                var s = new Vector3(2.5f, 1f, 2.5f);
                var matrix = default(Matrix4x4);
                vector.y = 3;
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
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
                var damageAmountBase = def.projectile.GetDamageAmount(1f);
                var equipmentDef = this.equipmentDef;
                var dinfo = new DamageInfo(def.projectile.damageDef, damageAmountBase, this.def.projectile.GetArmorPenetration(1f), ExactRotation.eulerAngles.y,
                    launcher,  null, equipmentDef);
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
                    var compAbility = Caster.TryGetComp<CompAbilityUser>();
                    if (compAbility != null)
                        if (compAbility.IgnoredHediffs() != null)
                            if (compAbility.IgnoredHediffs().Contains(hediff.def))
                                return true;
                }

            return false;
        }
    }
}