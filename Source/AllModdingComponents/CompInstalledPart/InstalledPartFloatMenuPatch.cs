using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JecsTools;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Sound;

namespace CompInstalledPart
{
    public class InstalledPartFloatMenuPatch : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> GetFloatMenus()
        {
            List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> FloatMenus = new List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();

            _Condition curCondition = new _Condition(_ConditionType.IsType, typeof(ThingWithComps));
            Func<Vector3, Pawn, Thing, List<FloatMenuOption>> curFunc = delegate (Vector3 clickPos, Pawn pawn, Thing curThing)
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                if (curThing is ThingWithComps groundThing && groundThing.GetComp<CompInstalledPart>() is CompInstalledPart groundPart)
                {
                    if (pawn.equipment != null)
                    {
                        //Remove "Equip" option from right click.
                        if (groundThing.GetComp<CompEquippable>() != null)
                        {
                            var optToRemove = opts.FirstOrDefault((x) => x.Label.Contains(groundThing.Label));
                            if (optToRemove != null) opts.Remove(optToRemove);
                        }

                        string text = "CompInstalledPart_Install".Translate();
                        opts.Add(new FloatMenuOption(text, delegate
                        {
                            CompProperties_InstalledPart props = groundPart.Props;
                            if (props != null)
                            {
                                if (props.allowedToInstallOn != null && props.allowedToInstallOn.Count > 0)
                                {
                                    SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                                    Find.Targeter.BeginTargeting(new TargetingParameters
                                    {
                                        canTargetPawns = true,
                                        canTargetBuildings = true,
                                        mapObjectTargetsMustBeAutoAttackable = false,
                                        validator = delegate (TargetInfo targ)
                                        {
                                            if (!targ.HasThing)
                                            {
                                                return false;
                                            }
                                            return props.allowedToInstallOn.Contains(targ.Thing.def);
                                        }
                                    }, delegate (LocalTargetInfo target)
                                    {
                                        groundThing.SetForbidden(false);
                                        groundPart.GiveInstallJob(pawn, target.Thing);
                                    }, null, null, null);
                                }
                                else
                                {
                                    Log.ErrorOnce("CompInstalledPart :: allowedToInstallOn list needs to be defined in XML.", 3242);
                                }
                            }
                        }, MenuOptionPriority.Default, null, null, 29f, null, null));
                    }
                }
                return opts;
            };
            KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>> curSec =
                new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(curCondition, curFunc);
            FloatMenus.Add(curSec);
            return FloatMenus;
        }

    }
}
