using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    public class WorldObject_ProgressBar : WorldObject
    {
        private static readonly Material UnfilledMat =
            SolidColorMaterials.NewSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f, 0.65f), ShaderDatabase.MetaOverlay);

        private static readonly Material FilledMat =
            SolidColorMaterials.NewSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f, 0.65f), ShaderDatabase.MetaOverlay);

        public float curProgress = -1f;
        public float offset = -0.5f;

        public override bool SelectableNow => false;
        public override bool NeverMultiSelect => true;

        //public override Vector3 DrawPos
        //{
        //    get
        //    {
        //        float d = 0.15f * Find.WorldGrid.averageTileSize;
        //        Rand.PushState();
        //        Rand.Seed = this.ID;
        //        float f = Rand.Range(0f, 360f);
        //        Rand.PopState();
        //        Vector2 point = new Vector2(Mathf.Cos(f), Mathf.Sin(f)) * d;
        //        return WorldRendererUtility.ProjectOnQuadTangentialToPlanet(Find.WorldGrid.GetTileCenter(this.Tile), point);
        //    }
        //}


        public override void Draw()
        {
            if (curProgress < 0)
            {
                base.Draw();
            }
            else
            {
                var averageTileSize = Find.WorldGrid.averageTileSize;
                var totalSize = 0.7f * averageTileSize;
                var curSize = totalSize * curProgress;
                var curPos = DrawPos;
                //Log.ErrorOnce("CurPos: " + curPos.x + " " + curPos.y + " " + curPos.z, 12368123);
                curPos += new Vector3(0, 0, offset); //Progress bar offset
                //Log.ErrorOnce("NextCurPos: " + curPos.x + " " + curPos.y + " " + curPos.z, 12368124);
                DrawShrinkableQuadTangentialToPlanet(curPos, curSize, totalSize, 1f, 0.3f, 0.035f, FilledMat, false,
                    false, null);
                DrawShrinkableQuadTangentialToPlanet(curPos, totalSize, totalSize, 1f, 0.3f, 0.033f, UnfilledMat, false,
                    false, null);
            }
        }

        // RimWorld.Planet.WorldRendererUtility
        public static void DrawShrinkableQuadTangentialToPlanet(Vector3 pos, float curSize, float totalSize,
            float scaleWidth, float scaleHeight, float altOffset, Material material, bool counterClockwise = false,
            bool useSkyboxLayer = false, MaterialPropertyBlock propertyBlock = null)
        {
            if (material == null)
            {
                Log.Warning("Tried to draw quad with null material.");
                return;
            }
            var normalized = pos.normalized;
            Vector3 vector;
            if (counterClockwise)
                vector = -normalized;
            else
                vector = normalized;
            var q = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector);
            var leftDrawStart = (curSize - totalSize) / 2;
            pos += new Vector3(leftDrawStart, 0, 0);
            var s = new Vector3(totalSize * scaleHeight, 1f, curSize * scaleWidth);
            var matrix = default(Matrix4x4);
            matrix.SetTRS(pos + normalized * altOffset, q, s);
            var layer = !useSkyboxLayer ? WorldCameraManager.WorldLayer : WorldCameraManager.WorldSkyboxLayer;
            if (propertyBlock != null)
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer, null, 0, propertyBlock);
            else
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer);
        }
    }
}