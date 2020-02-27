using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbilityUser
{
    /*
        "This class is primarily formed from code made by Cpt. Ohu for his Warhammer 40k mod.
         Credit goes where credit is due.
         Bless you, Ohu."
                                        -Jecrell
    */

    public class Command_PawnAbility : Command_Target
    {
        public CompAbilityUser compAbilityUser;
        public int curTicks = -1;
        public PawnAbility pawnAbility;
        public Verb_UseAbility verb = null;

        public Command_PawnAbility(CompAbilityUser compAbilityUser, PawnAbility ability, int ticks)
        {
            this.compAbilityUser = compAbilityUser;
            pawnAbility = ability;
            curTicks = ticks;
        }

        // RimWorld.Targeter
        public void BeginTargetingWithVerb(Verb_UseAbility verbToAdd, TargetingParameters targetParams,
            Action<LocalTargetInfo> action, Pawn caster = null, Action actionWhenFinished = null,
            Texture2D mouseAttachment = null)
        {
            verbToAdd.timeSavingActionVariable = this.action;
            // Tad changed
            // Find.Targeter.targetingVerb = verbToAdd;
            // Find.Targeter.targetingVerbAdditionalPawns = null;
            Find.Targeter.targetingSource = verbToAdd;
            Find.Targeter.targetingSourceAdditionalPawns = null;
            AccessTools.Field(typeof(Targeter), "action").SetValue(Find.Targeter, action);
            AccessTools.Field(typeof(Targeter), "targetParams").SetValue(Find.Targeter, targetParams);
            AccessTools.Field(typeof(Targeter), "caster").SetValue(Find.Targeter, caster);
            AccessTools.Field(typeof(Targeter), "actionWhenFinished").SetValue(Find.Targeter, actionWhenFinished);
            AccessTools.Field(typeof(Targeter), "mouseAttachment").SetValue(Find.Targeter, mouseAttachment);
        }


        public override void ProcessInput(Event ev)
        {
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

            Find.Targeter.StopTargeting();
            BeginTargetingWithVerb(verb, verb.verbProps.targetParams, delegate(LocalTargetInfo info)
            {
                action.Invoke(info.Thing);
                if (CurActivateSound != null)
                    CurActivateSound.PlayOneShotOnCamera();
            }, compAbilityUser.AbilityUser, null, null);
            //(info.Thing ?? null);
        }

        //public override bool GroupsWith(Gizmo other)
        //{
        //    if (other is Command_PawnAbility p && p.pawnAbility.Def.abilityClass == this.pawnAbility.Def.abilityClass)
        //        return true;
        //    return false;
        //}

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            var rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            var isMouseOver = false;
            if (Mouse.IsOver(rect))
            {
                isMouseOver = true;
                GUI.color = GenUI.MouseoverColor;
            }
            var badTex = icon;
            if (badTex == null) badTex = BaseContent.BadTex;

            GUI.DrawTexture(rect, BGTex);
            MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
            GUI.color = IconDrawColor;
            Widgets.DrawTextureFitted(new Rect(rect), badTex, iconDrawScale * 0.85f, iconProportions, iconTexCoords);
            GUI.color = Color.white;
            var isUsed = false;
            //Rect rectFil = new Rect(topLeft.x, topLeft.y, this.Width, this.Width);

            var keyCode = hotKey != null ? hotKey.MainKey : KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                var rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                Widgets.Label(rect2, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (hotKey.KeyDownEvent)
                {
                    isUsed = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(rect, false)) isUsed = true;
            var labelCap = LabelCap;
            if (!labelCap.NullOrEmpty())
            {
                var num = Text.CalcHeight(labelCap, rect.width);
                num -= 2f;
                var rect3 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
                GUI.DrawTexture(rect3, TexUI.GrayTextBG);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect3, labelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            GUI.color = Color.white;
            if (DoTooltip)
            {
                TipSignal tip = Desc;
                if (disabled && !disabledReason.NullOrEmpty())
                    tip.text = tip.text + "\n" + StringsToTranslate.AU_DISABLED + ": " + disabledReason;
                TooltipHandler.TipRegion(rect, tip);
            }
            if (pawnAbility.CooldownTicksLeft != -1 && pawnAbility.CooldownTicksLeft < pawnAbility.MaxCastingTicks)
            {
                var math = curTicks / (float) pawnAbility.MaxCastingTicks;
                Widgets.FillableBar(rect, math, AbilityButtons.FullTex, AbilityButtons.EmptyTex, false);
            }
            if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null ||
                                                !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
                UIHighlighter.HighlightOpportunity(rect, HighlightTag);
            if (isUsed)
            {
                if (disabled)
                {
                    if (!disabledReason.NullOrEmpty())
                        Messages.Message(disabledReason, MessageTypeDefOf.RejectInput);
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                if (!TutorSystem.AllowAction(TutorTagSelect))
                    return new GizmoResult(GizmoState.Mouseover, null);
                var result = new GizmoResult(GizmoState.Interacted, Event.current);
                TutorSystem.Notify_Event(TutorTagSelect);
                return result;
            }
            if (isMouseOver) return new GizmoResult(GizmoState.Mouseover, null);
            return new GizmoResult(GizmoState.Clear, null);
        }

        public void FillableBarBottom(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex, bool doBorder)
        {
            if (doBorder)
            {
                GUI.DrawTexture(rect, BaseContent.BlackTex);
                rect = rect.ContractedBy(3f);
            }
            if (fillTex != null)
                GUI.DrawTexture(rect, fillTex);
            rect.height *= fillPercent;
            GUI.DrawTexture(rect, bgTex);
        }
    }
}