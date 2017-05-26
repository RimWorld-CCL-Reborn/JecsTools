using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    public class CompAbilityItem : ThingComp
    {
//        public Pawn Owner = new Pawn(); owner should be item equipper
//        protected CompEquippable compEquippable = null;

        public CompAbilityUser AbilityUserTarget = null;

        private Graphic Overlay;

        public CompProperties_AbilityItem Props
        {
            get
            {
                return (CompProperties_AbilityItem)this.props;
            }
        }

/*
        public CompAbilityItem() {
            Log.Message("CompAbilityItem constructor");
        }
        */

        public List<PawnAbility> Abilities = new List<PawnAbility>(); // should these exist or only in CompAbilityUser.temporaryWeaponPowers?

        public void GetOverlayGraphic()
        {
            this.Overlay = GraphicDatabase.Get<Graphic_Single>("UI/Glow_Corrupt", ShaderDatabase.MetaOverlay, Vector2.one, Color.white);
        }


/*
        public virtual void UpdateAbilities()
        {
            if ( this.compEquippable.PrimaryVerb.CasterPawn != null ) {
                Log.Message("Found holder in UpdateAbilities... doing nothing to add in abilities yet");
                / *
                CompAbilityUser cau = this.compEquippable.Holder.Get  get the one that creates the power this will add in so AbilityDef needs to point to the AbilityUser that it stems from
                List<AbilityDef> list = Props.Abilities;
                for (int i = 0; i < list.Count; i++)
                {
                    this.parent.TryGetComp<CompPsyker>().psykerPowerManager.AddPsykerPower(list[i]);
                }
                * /
            } else {
                Log.Message("UpdateAbilites coudl not find holder... order of loading comps?");
            }
        }
        */

/*
This code should be handled in CompAbilityUser.... moving all of this into that class so done in one place
        public void UpdatePsykerVerbs()
        {
            if (Owner != null)
            {
                ThingWithComps equipment = this.parent as ThingWithComps;
                List<VerbProperties_WarpPower> list = SProps.UnlockedPsykerVerbs;
                for (int i = 0; i < SProps.UnlockedPsykerVerbs.Count; i++)
                {
                    if (list.Any(x => x.ReplacesStandardAttack))
                    {
                        List<VerbProperties> verbs = typeof(ThingDef).GetField("verbs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(equipment) as List<VerbProperties>;
                        verbs.Clear();
                        if (list[i].ReplacesStandardAttack)
                        {
                            if (verbs != null)
                            {
                                verbs.Add(list[i]);
                            }
                        }
                    }

                }
            }
        }
        */

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {

//            Log.Message("COmpAbilityItem PostSpawnSetup 1 : " + this.parent.def.tickerType);
            if (this.parent.def.tickerType == TickerType.Never)
            {
                this.parent.def.tickerType = TickerType.Rare;
            }
            base.PostSpawnSetup(respawningAfterLoad);

            Log.Message("COmpAbilityItem PostSpawnSetup 3 : AbilityUserClass is : "+this.Props.AbilityUserClass);
//            Log.Message("COmpAbilityItem PostSpawnSetup 2 : "+this.parent + " : " + this.parent.def.tickerType);
/*            this.compEquippable = this.parent.GetComp<CompEquippable>();
            if ( this.compEquippable == null ) {
                Log.Error("CompAbilityItem needs to have CompEqippable on item as well");
            }
            */

            //    Log.Message("GettingOverlay");
            GetOverlayGraphic();
            Find.TickManager.RegisterAllTickabilityFor(this.parent);
        }

        /*
        public void CheckForOwner()
        {
        CompEquippable tempcomp;
        Apparel tempthing;
        if (this.parent != null && !this.parent.Spawned && this.parent.holdingContainer == null)
        {
        //         Log.Message("Begin Check");
        if (this.parent is Apparel)
        {
        //             Log.Message("Soul item is Apparel");
        tempthing = this.parent as Apparel;
        this.Owner = tempthing.wearer;
    }
    else if ((tempcomp = this.parent.TryGetComp<CompEquippable>()) != null && tempcomp.PrimaryVerb.CasterPawn != null)
    {
    //         Log.Message("IsGun");
    this.Owner = tempcomp.PrimaryVerb.CasterPawn;
}
else if (this.parent.holdingContainer != null && this.parent.holdingContainer.owner is Pawn_CarryTracker)
{
Pawn_CarryTracker tracker = this.parent.holdingContainer.owner as Pawn_CarryTracker;
this.Owner = tracker.pawn;
}
if ((this.Owner != null))
{
if ((soul = this.Owner.needs.TryGetNeed<Need_Soul>()) != null)
{
this.CalculateSoulChanges(soul, SProps);
}
if (!PsykerPowerAdded)
{
CompPsyker compPsyker;
if ((compPsyker = Owner.TryGetComp<CompPsyker>()) != null)
{
for (int i = 0; i < SProps.UnlockedPsykerPowers.Count; i++)
{
if (soul.PsykerPowerLevel >= SProps.UnlockedPsykerPowers[i].PowerLevel)
{
//    Log.Message("Adding Power to: " + compPsyker.psyker + " : " + SProps.UnlockedPsykerPowers[i].defName);
compPsyker.allpsykerPowers.Add(new PsykerPowerEntry(SProps.UnlockedPsykerPowers[i], true, this.parent.def));
}
}
compPsyker.UpdatePowers();
}
PsykerPowerAdded = true;
}

}
}
if (this.parent.Spawned)
{
PsykerPowerAdded = false;
}
}

public void CalculateSoulChanges(Need_Soul nsoul, CompProperties_SoulItem cprops)
{
float num;
switch (cprops.Category)
{
case (SoulItemCategories.Neutral):
{
//            Log.Message("NeutralItem");
sign = 0;
break;
}
case (SoulItemCategories.Corruption):
{
//           Log.Message("Corrupted Item");
sign = -1;
break;
}
case (SoulItemCategories.Redemption):
{
//                  Log.Message("Redemptive Item");
sign = 0.5f;
break;
}
default:
{
Log.Error("No Soul Item Category Found");
break;
}
}
num = sign * cprops.GainRate * 0.2f / 14000;
//           Log.Message(num.ToString());
nsoul.GainNeed(num);
}
*/

public override void PostDrawExtraSelectionOverlays()
{
    if (Overlay == null) Log.Message("NoOverlay");
    if (Overlay != null)
    {
        Vector3 drawPos = this.parent.DrawPos;
        drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
        Vector3 s = new Vector3(2.0f, 2.0f, 2.0f);
        Matrix4x4 matrix = default(Matrix4x4);
        matrix.SetTRS(drawPos, Quaternion.AngleAxis(0, Vector3.up), s);
        Graphics.DrawMesh(MeshPool.plane10, matrix, this.Overlay.MatSingle, 0);
    }
}

public override void CompTick()
{
//    Log.Message("CompAbilityItem.CompTick");
}
public override void CompTickRare()
{
//    Log.Message("CompAbilityItem CompTickRare");
    //            this.CheckForOwner();
}

public override void PostExposeData()
{
    base.PostExposeData();
    //            Scribe_Values.LookValue<bool>(ref this.PsykerPowerAdded, "PsykerPowerAdded", false, false);
}

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
