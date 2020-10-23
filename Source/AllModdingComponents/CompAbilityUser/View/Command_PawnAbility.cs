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
            var targeter = Find.Targeter;
            targeter.targetingSource = verbToAdd;
            targeter.targetingSourceAdditionalPawns = null;
            targeterActionField(targeter) = action;
            targeterCasterField(targeter) = caster;
            targeterTargetParamsField(targeter) = targetParams;
            targeterActionWhenFinishedField(targeter) = actionWhenFinished;
            targeterMouseAttachmentField(targeter) = mouseAttachment;
        }

        private static readonly AccessTools.FieldRef<Targeter, Action<LocalTargetInfo>> targeterActionField =
            AccessTools.FieldRefAccess<Targeter, Action<LocalTargetInfo>>("action");
        private static readonly AccessTools.FieldRef<Targeter, Pawn> targeterCasterField =
            AccessTools.FieldRefAccess<Targeter, Pawn>("caster");
        private static readonly AccessTools.FieldRef<Targeter, TargetingParameters> targeterTargetParamsField =
            AccessTools.FieldRefAccess<Targeter, TargetingParameters>("targetParams");
        private static readonly AccessTools.FieldRef<Targeter, Action> targeterActionWhenFinishedField =
            AccessTools.FieldRefAccess<Targeter, Action>("actionWhenFinished");
        private static readonly AccessTools.FieldRef<Targeter, Texture2D> targeterMouseAttachmentField =
            AccessTools.FieldRefAccess<Targeter, Texture2D>("mouseAttachment");

        public override void ProcessInput(Event ev)
        {
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

            Find.Targeter.StopTargeting();
            BeginTargetingWithVerb(verb, verb.verbProps.targetParams, info =>
            {
                action.Invoke(info.Thing);
                CurActivateSound?.PlayOneShotOnCamera();
            }, compAbilityUser.Pawn);
        }

        //public override bool GroupsWith(Gizmo other)
        //{
        //    return other is Command_PawnAbility p && p.pawnAbility.Def.abilityClass == pawnAbility.Def.abilityClass;
        //}

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            var isMouseOver = false;
            if (Mouse.IsOver(rect))
            {
                isMouseOver = true;
                GUI.color = GenUI.MouseoverColor;
            }
            var badTex = icon ?? BaseContent.BadTex;

            GUI.DrawTexture(rect, BGTex);
            MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
            GUI.color = IconDrawColor;
            Widgets.DrawTextureFitted(new Rect(rect), badTex, iconDrawScale * 0.85f, iconProportions, iconTexCoords);
            GUI.color = Color.white;
            var isUsed = false;

            var keyCode = hotKey != null ? hotKey.MainKey : KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                var hotkeyRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                Widgets.Label(hotkeyRect, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (hotKey.KeyDownEvent)
                {
                    isUsed = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(rect, false))
                isUsed = true;
            var labelCap = LabelCap;
            if (!labelCap.NullOrEmpty())
            {
                var labelHeight = Text.CalcHeight(labelCap, rect.width) - 2f;
                var labelRect = new Rect(rect.x, rect.yMax - labelHeight + 12f, rect.width, labelHeight);
                GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(labelRect, labelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            GUI.color = Color.white;
            if (DoTooltip)
            {
                TipSignal tip = Desc;
                if (disabled && !disabledReason.NullOrEmpty())
                    tip.text += "\n" + StringsToTranslate.AU_DISABLED + ": " + disabledReason;
                TooltipHandler.TipRegion(rect, tip);
            }
            if (pawnAbility.CooldownTicksLeft != -1 && pawnAbility.CooldownTicksLeft < pawnAbility.MaxCastingTicks)
            {
                var math = curTicks / (float)pawnAbility.MaxCastingTicks;
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
            return new GizmoResult(isMouseOver ? GizmoState.Mouseover : GizmoState.Clear, null);
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
