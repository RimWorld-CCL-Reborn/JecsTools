﻿using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    public class CompAbilityItem : ThingComp
    {
        // TODO: Should these exist or only in CompAbilityUser.temporaryWeaponPowers?
        // Abilities is currently never used within the framework (only Props.Abilities is used),
        // while AbilityUserTarget is set via Harmony patches.
        // For backwards compatibility, in case any other mod is using these, must leave these as-is,
        // including the potentially wasteful empty List initialization.
        public List<PawnAbility> Abilities = new List<PawnAbility>(0);
        public CompAbilityUser AbilityUserTarget = null;

        // Compatibility note: as of 2020-10-20, a mod (A RimWorld of Magic) uses reflection to access this to
        // workaround a now-fixed oversight where PostDrawExtraSelectionOverlays constantly logged "NoOverlay".
        private Graphic Overlay;

        public CompProperties_AbilityItem Props => (CompProperties_AbilityItem)props;

        public virtual Graphic GetOverlayGraphic()
        {
            // TODO: Remove - UI/Glow_Corrupt texture was never added to JecsTools.
            //return GraphicDatabase.Get<Graphic_Single>("UI/Glow_Corrupt", ShaderDatabase.MetaOverlay, Vector2.one, Color.white);
            return null;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (parent.def.tickerType == TickerType.Never)
                parent.def.tickerType = TickerType.Rare;
            base.PostSpawnSetup(respawningAfterLoad);

            Overlay = GetOverlayGraphic();
            Find.TickManager.RegisterAllTickabilityFor(parent);
        }

        //public override void CompTick() { }
        //public override void CompTickRare() { }

        public override void PostDrawExtraSelectionOverlays()
        {
            if (Overlay != null)
            {
                var drawPos = parent.DrawPos;
                drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                var s = new Vector3(2.0f, 2.0f, 2.0f);
                var matrix = Matrix4x4.TRS(drawPos, Quaternion.AngleAxis(0, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Overlay.MatSingle, 0);
            }
        }

        public override string GetDescriptionPart()
        {
            var s = new StringBuilder();

            if (Props.Abilities.Count == 1)
                s.Append("Item Ability:");
            else if (Props.Abilities.Count > 1)
                s.Append("Item Abilities:");

            foreach (var pa in Props.Abilities)
            {
                s.Append("\n\n");
                s.Append(pa.label.CapitalizeFirst());
                s.Append(" - ");
                s.Append(pa.GetDescription());
            }
            return s.ToString();
        }
    }
}
