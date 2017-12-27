using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public static class CaravanToils_Effects
    {
        // Verse.AI.ToilEffects
        public static CaravanToil WithProgressBar(this CaravanToil CaravanToil, TargetIndex ind,
            Func<float> progressGetter, bool interpolateBetweenActorAndTarget = false, float offsetZ = -0.5f)
        {
            WorldObject_ProgressBar progressBar = null;
            CaravanToil.AddPreTickAction(delegate
            {
                if (CaravanToil.actor.Faction != Faction.OfPlayer)
                    return;
                var curProgress = Mathf.Clamp01(progressGetter());
                //Log.Message(curProgress.ToString());
                //WorldProgressBarDrawer.DrawProgressBarOnGUIFor(target, curProgress);
                if (progressBar == null)
                {
                    progressBar =
                        (WorldObject_ProgressBar) WorldObjectMaker.MakeWorldObject(
                            DefDatabase<WorldObjectDef>.GetNamed("WorldObject_ProgressBar"));
                    progressBar.Tile = Find.World.GetComponent<CaravanJobGiver>().CurJob(CaravanToil.actor)
                        .GetTarget(ind).Tile;
                    progressBar.offset = offsetZ;
                    Find.WorldObjects.Add(progressBar);
                }
                else
                {
                    progressBar.curProgress = Mathf.Clamp01(progressGetter());
                    if (CaravanToil.actor == null || !CaravanToil.actor.Spawned ||
                        CaravanToil.actor.Tile != Find.World.GetComponent<CaravanJobGiver>().CurJob(CaravanToil.actor)
                            .GetTarget(ind).Tile)
                        if (progressBar.Spawned)
                            Find.WorldObjects.Remove(progressBar);
                }
            });
            CaravanToil.AddFinishAction(delegate
            {
                if (progressBar != null && progressBar.Spawned)
                {
                    Find.WorldObjects.Remove(progressBar);
                    progressBar = null;
                }
            });
            return CaravanToil;
        }
    }
}