using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompActivatableEffect
{

    public class CompActivatableEffect : CompUseEffect
    {
        public enum State
        {
            Deactivated,
            Activated
        }

        public CompProperties_ActivatableEffect Props => (CompProperties_ActivatableEffect)props;

        private State currentState = State.Deactivated;

        public bool IsInitialized;

        private Sustainer sustainer;

        private CompEquippable compEquippable;
        private Func<bool> compDeflectorIsAnimatingNow;
        private Func<int> compDeflectorAnimationDeflectionTicks;
        private CompOversizedWeapon.CompOversizedWeapon compOversizedWeapon;

        private static readonly Type compDeflectorType = GenTypes.GetTypeInAnyAssembly("CompDeflector.CompDeflector");

        public CompEquippable GetEquippable => compEquippable;

        public CompOversizedWeapon.CompOversizedWeapon GetOversizedWeapon => compOversizedWeapon;

        public Pawn GetPawn => GetEquippable.verbTracker.PrimaryVerb.CasterPawn;

        //public List<Verb> GetVerbs => GetEquippable.verbTracker.AllVerbs;

        public bool CompDeflectorIsAnimatingNow => compDeflectorIsAnimatingNow?.Invoke() ?? false;

        public int CompDeflectorAnimationDeflectionTicks => compDeflectorAnimationDeflectionTicks?.Invoke() ?? 0;

        public bool GizmosOnEquip => Props.gizmosOnEquip;
        public State CurrentState => currentState;

        public virtual bool CanActivate()
        {
            return true;
        }

        public virtual bool CanDeactivate()
        {
            return true;
        }

        public virtual bool TryActivate()
        {
            if (CanActivate())
            {
                Activate();
                return true;
            }
            return false;
        }

        public virtual bool TryDeactivate()
        {
            if (CanDeactivate())
            {
                Deactivate();
                return true;
            }
            return false;
        }

        public virtual void PlaySound(SoundDef soundToPlay)
        {
            SoundInfo info;
            if (Props.gizmosOnEquip)
                info = SoundInfo.InMap(new TargetInfo(GetPawn.PositionHeld, GetPawn.MapHeld, false),
                    MaintenanceType.None);
            else
                info = SoundInfo.InMap(new TargetInfo(parent.PositionHeld, parent.MapHeld, false),
                    MaintenanceType.None);
            soundToPlay?.PlayOneShot(info);
        }

        private void StartSustainer()
        {
            if (!Props.sustainerSound.NullOrUndefined() && sustainer == null)
            {
                var info = SoundInfo.InMap(GetPawn, MaintenanceType.None);
                sustainer = Props.sustainerSound.TrySpawnSustainer(info);
            }
        }

        private void EndSustainer()
        {
            if (sustainer != null)
            {
                sustainer.End();
                sustainer = null;
            }
        }

        public virtual void Activate()
        {
            graphicInt = null;
            currentState = State.Activated;
            if (Props.activateSound != null) PlaySound(Props.activateSound);
            StartSustainer();
            showNow = true;
        }

        public virtual void Deactivate()
        {
            currentState = State.Deactivated;
            if (Props.deactivateSound != null) PlaySound(Props.deactivateSound);
            EndSustainer();
            showNow = false;
            graphicInt = null;
        }

        public bool IsActive()
        {
            if (currentState == State.Activated) return true;
            return false;
        }

        // This is called during ThingWithComps.InitializeComps, after constructor is called and parent is set.
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
            // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
            // while `isinst` instruction against non-generic type operand like used below is fast.
            // For the optional CompDeflector, we have to use the slower IsAssignableFrom reflection check.
            var comps = parent.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                var comp = comps[i];
                if (comp is CompEquippable compEquippable)
                    this.compEquippable = compEquippable;
                else if (compDeflectorType != null)
                {
                    var compType = comp.GetType();
                    if (compDeflectorType.IsAssignableFrom(compType))
                    {
                        compDeflectorIsAnimatingNow =
                            (Func<bool>)AccessTools.PropertyGetter(compType, "IsAnimatingNow").CreateDelegate(typeof(Func<bool>), comp);
                        compDeflectorAnimationDeflectionTicks =
                            (Func<int>)AccessTools.PropertyGetter(compType, "AnimationDeflectionTicks").CreateDelegate(typeof(Func<int>), comp);
                    }
                }
                else if (comp is CompOversizedWeapon.CompOversizedWeapon compOversizedWeapon)
                    this.compOversizedWeapon = compOversizedWeapon;
            }
        }

        // This is called on the first tick - not rolled into above Initialize since it's still needed in case subclasses implement it.
        public virtual void Initialize()
        {
            IsInitialized = true;
            currentState = State.Deactivated;
        }

        public override void CompTick()
        {
            if (!IsInitialized) Initialize();
            if (IsActive()) ActiveTick();
            base.CompTick();
        }

        public virtual void ActiveTick()
        {
        }

        public IEnumerable<Gizmo> EquippedGizmos()
        {
            //Add
            if (Props.draftToUseGizmos && (GetPawn != null && GetPawn.Drafted) || !Props.draftToUseGizmos)
                if (currentState == State.Activated)
                    yield return new Command_Action
                    {
                        defaultLabel = Props.DeactivateLabel,
                        icon = IconDeactivate,
                        action = delegate { TryDeactivate(); }
                    };
                else
                    yield return new Command_Action
                    {
                        defaultLabel = Props.ActivateLabel,
                        icon = IconActivate,
                        action = delegate { TryActivate(); }
                    };
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!GizmosOnEquip)
            {
                foreach (var current in base.CompGetGizmosExtra())
                    yield return current;

                foreach (var current in EquippedGizmos())
                    yield return current;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref showNow, "showNow", false);
            Scribe_Values.Look(ref currentState, "currentState", State.Deactivated);
        }

        #region Graphics

        private Graphic graphicInt;
        private readonly Color overrideColor = Color.white;
        private bool showNow;

        public bool ShowNow
        {
            set => showNow = value;
            get => showNow;
        }

        public Texture2D IconActivate
        {
            get
            {
                var resolvedTexture = TexCommand.GatherSpotActive;
                if (!Props.uiIconPathActivate.NullOrEmpty())
                    resolvedTexture = ContentFinder<Texture2D>.Get(Props.uiIconPathActivate, true);
                return resolvedTexture;
            }
        }

        public Texture2D IconDeactivate
        {
            get
            {
                var resolvedTexture = TexCommand.ClearPrioritizedWork;
                if (!Props.uiIconPathDeactivate.NullOrEmpty())
                    resolvedTexture = ContentFinder<Texture2D>.Get(Props.uiIconPathDeactivate, true);
                return resolvedTexture;
            }
        }

        public virtual Graphic Graphic
        {
            set => graphicInt = value;
            get
            {
                if (graphicInt == null)
                {
                    if (Props.graphicData == null)
                    {
                        Log.ErrorOnce(parent.def + " has no SecondLayer graphicData but we are trying to access it.",
                            764532);
                        return BaseContent.BadGraphic;
                    }
                    var newColor1 = overrideColor == Color.white ? parent.DrawColor : overrideColor;
                    var newColor2 = overrideColor == Color.white ? parent.DrawColorTwo : overrideColor;
                    graphicInt =
                        Props.graphicData.Graphic.GetColoredVersion(parent.Graphic.Shader, newColor1, newColor2);
                    graphicInt = PostGraphicEffects(graphicInt);
                }
                return graphicInt;
            }
        }

        public virtual Graphic PostGraphicEffects(Graphic graphic)
        {
            return graphic;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (ShowNow)
            {
                Graphic = new Graphic_RandomRotated(Graphic, 35f);
                Graphic.Draw(GenThing.TrueCenter(parent.Position, parent.Rotation, parent.def.size, Props.Altitude),
                    parent.Rotation, parent);
            }
        }

        #endregion Graphics
    }
}
