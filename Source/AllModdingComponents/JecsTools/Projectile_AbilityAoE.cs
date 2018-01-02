using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    public class Projectile_AbilityAoE : Projectile_AbilityBase
    {
        public Thing target;

        public Vector3 drawpos
        {
            get
            {
                if (this.target != null)
                {
                    return target.DrawPos;
                }
                return destination;
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetPawns = true;
            target = this.Map.thingGrid.ThingAt(destination.ToIntVec3(), ThingCategory.Pawn);
            if (target != null) Log.Message("FoundTarget");
            if (target == null) Log.Message("No Target??");
        }

        public override void Draw()
        {
            if (target != null)
            {
                Vector3 vector = drawpos;
                Vector3 distance = this.destination - this.origin;
                Vector3 curpos = this.destination - this.Position.ToVector3();
                var num = 1 - (Mathf.Sqrt(Mathf.Pow(curpos.x, 2) + Mathf.Pow(curpos.z, 2)) / (Mathf.Sqrt(Mathf.Pow(distance.x, 2) + Mathf.Pow(distance.z, 2))));
                float angle = 0f;
                Material mat = this.Graphic.MatSingle;
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default(Matrix4x4);
                vector.y = 3;
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            if (target != null)
            {
                Vector3 vector = drawpos;
                Vector3 distance = this.destination - this.origin;
                Vector3 curpos = this.destination - this.Position.ToVector3();
                var num = 1 - (Mathf.Sqrt(Mathf.Pow(curpos.x, 2) + Mathf.Pow(curpos.z, 2)) / (Mathf.Sqrt(Mathf.Pow(distance.x, 2) + Mathf.Pow(distance.z, 2))));
                float angle = 0f;
                Material mat = this.Graphic.MatSingle;
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default(Matrix4x4);
                vector.y = 3;
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }

    }
}