using System.Collections.Generic;

namespace CompActivatableEffect
{
    public class CompActivatableEffect : CompUseEffect
    {

        #region Graphics
        
        private Graphic graphicInt;
        private Color overrideColor = Color.white;
        private bool showNow = false;
        public bool ShowNow
        {
            set => this.showNow = value;
            get => this.showNow;
        }

        public Texture2D IconActivate
        {
            get
            {
                Texture2D resolvedTexture = TexCommand.GatherSpotActive;
                if (!this.Props.uiIconPathActivate.NullOrEmpty())
                {
                    resolvedTexture = ContentFinder<Texture2D>.Get(this.Props.uiIconPathActivate, true);
                }
                return resolvedTexture;
            }
        }
        public Texture2D IconDeactivate
        {
            get
            {
                Texture2D resolvedTexture = TexCommand.ClearPrioritizedWork;
                if (!this.Props.uiIconPathDeactivate.NullOrEmpty())
                {
                    resolvedTexture = ContentFinder<Texture2D>.Get(this.Props.uiIconPathDeactivate, true);
                }
                return resolvedTexture;
            }
        }

        public CompProperties_ActivatableEffect Props => (CompProperties_ActivatableEffect)this.props;

        public virtual Graphic Graphic
        {
            set => this.graphicInt = value;
            get
            {
                Graphic badGraphic;
                if (this.graphicInt == null)
                {
                    if (this.Props.graphicData == null)
                    {
                        Log.ErrorOnce(this.parent.def + " has no SecondLayer graphicData but we are trying to access it.", 764532);
                        badGraphic = BaseContent.BadGraphic;
                        return badGraphic;
                    }
                    Color newColor1 = this.overrideColor == Color.white ? this.parent.DrawColor : this.overrideColor;
                    Color newColor2 = this.overrideColor == Color.white ? this.parent.DrawColorTwo : this.overrideColor;
                    this.graphicInt = this.Props.graphicData.Graphic.GetColoredVersion(this.parent.Graphic.Shader, newColor1, newColor2);
                    this.graphicInt = PostGraphicEffects(this.graphicInt);
                }
                badGraphic = this.graphicInt;
                return badGraphic;
            }
        }

        public virtual Graphic PostGraphicEffects(Graphic graphic) => graphic;

        public override void PostDraw()
        {
            base.PostDraw();
            if (this.ShowNow)
            {
                //float parentRotation = 0.0f;

                //if (this.parent.Graphic != null)
                //{
                //    if (this.parent.Graphic.data != null)
                //    {
                //        parentRotation = this.parent.Graphic.data.onGroundRandomRotateAngle;
                //    }
                //    else Log.ErrorOnce("ProjectJedi.CompActivatableEffect :: this.parent.graphic.data Null Reference", 7887);
                //}

                //if (parentRotation > 0.01f)
                //{
                    this.Graphic = new Graphic_RandomRotated(this.Graphic, 35f);
                //}
                
                this.Graphic.Draw(Gen.TrueCenter(this.parent.Position, this.parent.Rotation, this.parent.def.size, this.Props.Altitude), this.parent.Rotation, this.parent);
            }
        }
        #endregion Graphics

        private Sustainer sustainer = null;

        public CompEquippable GetEquippable => this.parent.GetComp<CompEquippable>();

        public Pawn GetPawn => this.GetEquippable.verbTracker.PrimaryVerb.CasterPawn;

        public List<Verb> GetVerbs => this.GetEquippable.verbTracker.AllVerbs;

        public bool GizmosOnEquip => this.Props.gizmosOnEquip;

        public enum State { Deactivated, Activated }
        private State currentState = State.Deactivated;
        public State CurrentState => this.currentState;

        public virtual bool CanActivate() => true;

        public virtual bool CanDeactivate() => true;

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
            if (this.Props.gizmosOnEquip)
            {
                info = SoundInfo.InMap(new TargetInfo(this.GetPawn.PositionHeld, this.GetPawn.MapHeld, false), MaintenanceType.None);
            }
            else
            {
                info = SoundInfo.InMap(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false), MaintenanceType.None);
            }
            soundToPlay.PlayOneShot(info);
        }

        private void StartSustainer()
        {
            if (!this.Props.sustainerSound.NullOrUndefined() && this.sustainer == null)
            {
                SoundInfo info = SoundInfo.InMap(this.GetPawn, MaintenanceType.None);
                this.sustainer = this.Props.sustainerSound.TrySpawnSustainer(info);
            }
        }

        private void EndSustainer()
        {
            if (this.sustainer != null)
            {
                this.sustainer.End();
                this.sustainer = null;
            }
        }

        public virtual void Activate()
        {
            this.graphicInt = null;
            this.currentState = State.Activated;
            if (this.Props.activateSound != null) PlaySound(this.Props.activateSound);
            StartSustainer();
            this.showNow = true;
        }

        public virtual void Deactivate()
        {
            this.currentState = State.Deactivated;
            if (this.Props.deactivateSound != null) PlaySound(this.Props.deactivateSound);
            EndSustainer();
            this.showNow = false;
            this.graphicInt = null;
        }

        public bool IsActive()
        {
            if (this.currentState == State.Activated) return true;
            return false;
        }

        public bool IsInitialized = false;
        public virtual void Initialize()
        {
            this.IsInitialized = true;
            this.currentState = State.Deactivated;
        }
        
        public override void CompTick()
        {
            if (!this.IsInitialized) Initialize();
            if (IsActive()) ActiveTick();
            base.CompTick();
        }

        public virtual void ActiveTick()
        {
        }
        
        public IEnumerable<Gizmo> EquippedGizmos()
        {
            //Add
            if ((this.Props.draftToUseGizmos && this.GetPawn.Drafted) || !this.Props.draftToUseGizmos)
            if (this.currentState == State.Activated)
            {
                yield return new Command_Action
                {
                    defaultLabel = this.Props.DeactivateLabel,
                    icon = this.IconDeactivate,
                    action = delegate
                    {
                        this.TryDeactivate();
                    }
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = this.Props.ActivateLabel,
                    icon = this.IconActivate,
                    action = delegate
                    {
                        this.TryActivate();
                    }
                };
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!this.GizmosOnEquip)
            {
                //Iterate Base Functions
                IEnumerator<Gizmo> enumerator = base.CompGetGizmosExtra().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Gizmo current = enumerator.Current;
                    yield return current;
                }

                //Iterate ActivationActions
                IEnumerator<Gizmo> enumerator2 = EquippedGizmos().GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    Gizmo current = enumerator2.Current;
                    yield return current;
                }

            }

            yield break;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.showNow, "showNow", false);
            Scribe_Values.Look<State>(ref this.currentState, "currentState", State.Deactivated);
        }
    }
}
