using RimWorld;
using System;
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
        public CompAbilityUser compAbilityUser = null;
        public Verb verb = null;
        public PawnAbility pawnAbility = null;
        public int curTicks = -1;

        public Command_PawnAbility(CompAbilityUser compAbilityUser, PawnAbility ability, int ticks)
        {
            this.compAbilityUser = compAbilityUser;
            this.pawnAbility = ability;
            this.curTicks = ticks;
        }

        public override void ProcessInput(Event ev)
        {
            if (this.pawnAbility.Def.MainVerb.AbilityTargetCategory != AbilityTargetCategory.TargetSelf)
            {
                base.ProcessInput(ev);
                return;
            }
            this.action(null);

            if (this.CurActivateSound != null)
            {
                this.CurActivateSound.PlayOneShotOnCamera();
            }

            SoundDefOf.TickTiny.PlayOneShotOnCamera();

        }

        //public override bool GroupsWith(Gizmo other)
        //{
        //    if (other is Command_PawnAbility p && p.pawnAbility.Def.abilityClass == this.pawnAbility.Def.abilityClass)
        //        return true;
        //    return false;
        //}

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, this.Width, 75f);
            bool isMouseOver = false;
            if (Mouse.IsOver(rect))
            {
                isMouseOver = true;
                GUI.color = GenUI.MouseoverColor;
            }
            Texture2D badTex = this.icon;
            if (badTex == null) badTex = BaseContent.BadTex;

            GUI.DrawTexture(rect, Command.BGTex);
            MouseoverSounds.DoRegion(rect, SoundDefOf.MouseoverCommand);
            GUI.color = this.IconDrawColor;
            Widgets.DrawTextureFitted(new Rect(rect), badTex, this.iconDrawScale * 0.85f, this.iconProportions, this.iconTexCoords);
            GUI.color = Color.white;
            bool isUsed = false;
            //Rect rectFil = new Rect(topLeft.x, topLeft.y, this.Width, this.Width);

            KeyCode keyCode = (this.hotKey != null) ? this.hotKey.MainKey : KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                Rect rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                Widgets.Label(rect2, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (this.hotKey.KeyDownEvent)
                {
                    isUsed = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(rect, false)) isUsed = true;
            string labelCap = this.LabelCap;
            if (!labelCap.NullOrEmpty())
            {
                float num = Text.CalcHeight(labelCap, rect.width);
                num -= 2f;
                Rect rect3 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
                GUI.DrawTexture(rect3, TexUI.GrayTextBG);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect3, labelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            GUI.color = Color.white;
            if (this.DoTooltip)
            {
                TipSignal tip = this.Desc;
                if (this.disabled && !this.disabledReason.NullOrEmpty())
                {
                    tip.text = tip.text + "\n" + StringsToTranslate.AU_DISABLED + ": " + this.disabledReason;
                }
                TooltipHandler.TipRegion(rect, tip);
            }
            if (!this.HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
            {
                UIHighlighter.HighlightOpportunity(rect, this.HighlightTag);
            }
            float x = this.curTicks;
            float y = this.pawnAbility.MaxCastingTicks;
            float fill = x / y;
            if (x != -1 && x < y)
            {
                Log.Message(x.ToString());
                Widgets.FillableBar(rect, fill, AbilityButtons.FullTex, AbilityButtons.EmptyTex, false);
            }
            if (isUsed)
            {
                if (this.disabled)
                {
                    if (!this.disabledReason.NullOrEmpty())
                    {
                        Messages.Message(this.disabledReason, MessageSound.RejectInput);
                    }
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                if (!TutorSystem.AllowAction(this.TutorTagSelect))
                {
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                GizmoResult result = new GizmoResult(GizmoState.Interacted, Event.current);
                TutorSystem.Notify_Event(this.TutorTagSelect);
                return result;
            }
            else
            {
                if (isMouseOver) return new GizmoResult(GizmoState.Mouseover, null);
                return new GizmoResult(GizmoState.Clear, null);
            }
        }

        public void FillableBarBottom(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex, bool doBorder)
        {
            if (doBorder)
            {
                GUI.DrawTexture(rect, BaseContent.BlackTex);
                rect = rect.ContractedBy(3f);
            }
            if (fillTex != null)
            {
                GUI.DrawTexture(rect, fillTex);
            }
            rect.height *= fillPercent;
            GUI.DrawTexture(rect, bgTex);
        }
    }
}
