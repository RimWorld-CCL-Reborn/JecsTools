using UnityEngine;
using Verse;

namespace CompAnimated
{
    public class CompAnimatedOver : CompAnimated
    {
        protected Graphic scaled;
        /// <summary>
        /// Additional programatic movement hooks 
        /// </summary>
        public float yOffset = 0f, xOffset = 0f, xScale = 1f, yScale = 1f;
        
        public CompProperties_AnimatedOver OverProps => (CompProperties_AnimatedOver) props;
        
        public override void Render()
        {
            Vector3 drawPos = this.parent.DrawPos;
            
            //apply offset
            drawPos.x += OverProps.xOffset + xOffset;
            drawPos.z += OverProps.yOffset + yOffset;
            
            scaled.Draw(drawPos, Rot4.North, this.parent);
        }

        public override void NotifyGraphicsChange()
        {
            var vector2 = new Vector2(OverProps.xScale*xScale, OverProps.yScale*yScale);
            
            var sz = curGraphic.drawSize;
            sz.Scale(vector2);
            scaled = curGraphic.GetCopy(sz);
            base.NotifyGraphicsChange();
        }

        public void Invalidate()
        {
            curGraphic = null;
            this.dirty = true;
        }
    }
}