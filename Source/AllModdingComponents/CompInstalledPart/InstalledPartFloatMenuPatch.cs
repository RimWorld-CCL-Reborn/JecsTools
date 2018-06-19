using System;
using System.Collections.Generic;
using System.Linq;
using JecsTools;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompInstalledPart
{
    public class InstalledPartFloatMenuPatch : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>
            GetFloatMenus()
        {
            var FloatMenus = new List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();

            var curCondition = new _Condition(_ConditionType.IsType, typeof(ThingWithComps));
            Func<Vector3, Pawn, Thing, List<FloatMenuOption>> curFunc =
                delegate(Vector3 clickPos, Pawn pawn, Thing curThing)
                {
                    var opts = new List<FloatMenuOption>();
                    if (curThing is ThingWithComps groundThing &&
                        groundThing.GetComp<CompInstalledPart>() is CompInstalledPart groundPart)
                        if (pawn.equipment != null)
                        {
                            //Remove "Equip" option from right click.
                            if (groundThing.GetComp<CompEquippable>() != null)
                            {
                                var optToRemove = opts.FirstOrDefault(x => x.Label.Contains(groundThing.Label));
                                if (optToRemove != null) opts.Remove(optToRemove);
                            }

                            var text = "CompInstalledPart_Install".Translate();
                            opts.Add(new FloatMenuOption(text, delegate
                            {
                                var props = groundPart.Props;
                                if (props != null)
                                    if (props.allowedToInstallOn != null && props.allowedToInstallOn.Count > 0)
                                    {
                                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                                        Find.Targeter.BeginTargeting(new TargetingParameters
                                        {
                                            canTargetPawns = true,
                                            canTargetBuildings = true,
                                            mapObjectTargetsMustBeAutoAttackable = false,
                                            validator = delegate(TargetInfo targ)
                                            {
                                                if (!targ.HasThing)
                                                    return false;
                                                return props.allowedToInstallOn.Contains(targ.Thing.def);
                                            }
                                        }, delegate(LocalTargetInfo target)
                                        {
                                            groundThing.SetForbidden(false);
                                            groundPart.GiveInstallJob(pawn, target.Thing);
                                        }, null, null, null);
                                    }
                                    else
                                    {
                                        Log.ErrorOnce(
                                            "CompInstalledPart :: allowedToInstallOn list needs to be defined in XML.",
                                            3242);
                                    }
                            }, MenuOptionPriority.Default, null, null, 29f, null, null));
                        }
                    return opts;
                };
            var curSec =
                new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(curCondition, curFunc);
            FloatMenus.Add(curSec);
            return FloatMenus;
        }
    }
}