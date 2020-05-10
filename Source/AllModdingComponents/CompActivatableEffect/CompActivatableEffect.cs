using System;
using System.Collections.Generic;
using System.Linq;
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

        private State currentState = State.Deactivated;

        public bool IsInitialized;

        private Sustainer sustainer;

        private bool initComps = false;
        private CompEquippable compEquippable;
        private Func<bool> compDeflectorIsAnimatingNow;
        private Func<int> compDeflectorAnimationDeflectionTicks;

        private void InitCompsAsNeeded()
        {
            if (!initComps)
            {
                if (parent == null) return;
                compEquippable = parent.GetComp<CompEquippable>();
                var deflector = parent.AllComps.FirstOrDefault(y =>
                    y.GetType().ToString() == "CompDeflector.CompDeflector" ||
                    y.GetType().BaseType.ToString() == "CompDeflector.CompDeflector");
                if (deflector != null)
                {
                    compDeflectorIsAnimatingNow =
                        (Func<bool>) AccessTools.PropertyGetter(deflector.GetType(), "IsAnimatingNow").CreateDelegate(
                            typeof(Func<bool>), deflector);
                    compDeflectorAnimationDeflectionTicks =
                        (Func<int>) AccessTools.PropertyGetter(deflector.GetType(), "AnimationDeflectionTicks").CreateDelegate(
                            typeof(Func<int>), deflector);
                }
                initComps = true;
            }
        }

        public CompEquippable GetEquippable
        {
            get
            {
                InitCompsAsNeeded();
                return compEquippable;
            }
        }

        public Pawn GetPawn =>  GetEquippable.verbTracker.PrimaryVerb.CasterPawn;

        //public List<Verb> GetVerbs => GetEquippable.verbTracker.AllVerbs;

        public bool CompDeflectorIsAnimatingNow
        {
            get
            {
                InitCompsAsNeeded();
                if (compDeflectorIsAnimatingNow != null)
                    return compDeflectorIsAnimatingNow();
                return false;
            }
        }

        public int CompDeflectorAnimationDeflectionTicks
        {
            get
            {
                InitCompsAsNeeded();
                if (compDeflectorAnimationDeflectionTicks != null)
                    return compDeflectorAnimationDeflectionTicks();
                return 0;
            }
        }

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
                //Iterate Base Functions
                var enumerator = base.CompGetGizmosExtra().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    yield return current;
                }

                //Iterate ActivationActions
                var enumerator2 = EquippedGizmos().GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    var current = enumerator2.Current;
                    yield return current;
                }
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

        public CompProperties_ActivatableEffect Props => (CompProperties_ActivatableEffect) props;

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