using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    public class CompAbilityItem : ThingComp
    {

        public CompAbilityUser AbilityUserTarget = null;
        
        private Graphic Overlay;

        public CompProperties_AbilityItem Props => (CompProperties_AbilityItem)this.props;


        public List<PawnAbility> Abilities = new List<PawnAbility>(); // should these exist or only in CompAbilityUser.temporaryWeaponPowers?

        public void GetOverlayGraphic()
        {
            // Cool effect if you uncomment. Places this graffic behind the item.
//            this.Overlay = GraphicDatabase.Get<Graphic_Single>("UI/Glow_Corrupt", ShaderDatabase.MetaOverlay, Vector2.one, Color.white);
        }


        public override void PostSpawnSetup(bool respawningAfterLoad)
        {

            if (this.parent.def.tickerType == TickerType.Never)
            {
                this.parent.def.tickerType = TickerType.Rare;
            }
            base.PostSpawnSetup(respawningAfterLoad);

            GetOverlayGraphic();
            Find.TickManager.RegisterAllTickabilityFor(this.parent);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            if (this.Overlay == null) Log.Message("NoOverlay");
            if (this.Overlay != null)
            {
                Vector3 drawPos = this.parent.DrawPos;
                drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                Vector3 s = new Vector3(2.0f, 2.0f, 2.0f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(0, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, this.Overlay.MatSingle, 0);
            }
        }

        //        public override void CompTick() { }
        //        public override void CompTickRare() { }

        public override void PostExposeData() => base.PostExposeData();

        public override string GetDescriptionPart()
        {
            string str = string.Empty;

            if ( this.Props.Abilities.Count == 1 )
            str += "Item Ability:";
            else if ( this.Props.Abilities.Count > 1 )
            str += "Item Abilities:";

            foreach ( AbilityDef pa in this.Props.Abilities ) {
                str += "\n\n";
                str += pa.label.CapitalizeFirst() + " - ";
                str += pa.GetDescription();
            }
            return str;
        }
    }
}
