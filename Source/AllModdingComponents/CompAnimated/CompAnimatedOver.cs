using UnityEngine;
using Verse;

namespace CompAnimated
{
    public class CompAnimatedOver : CompAnimated
    {
        /// <summary>
        /// Additional programatic movement hooks 
        /// </summary>
        public float yOffset = 0f, xOffset = 0f, xScale = 1f, yScale = 1f;
        
        public CompProperties_AnimatedOver OverProps => (CompProperties_AnimatedOver) props;
        
        public override void Render()
        {
            Vector3 drawPos = this.parent.DrawPos;
            
            //apply offset
            drawPos.x += OverProps.xOffset + yOffset;
            drawPos.y += OverProps.yOffset + xOffset;
            
            curGraphic.Draw(drawPos, Rot4.North, this.parent);
        }

        public override void NotifyGraphicsChange()
        {
            //re scale once
            curGraphic.drawSize.Scale(new Vector2(OverProps.xScale*xScale, OverProps.yScale*yScale));
            base.NotifyGraphicsChange();
        }

        public void Invalidate()
        {
            curGraphic = null;
            this.dirty = true;
        }
    }
}