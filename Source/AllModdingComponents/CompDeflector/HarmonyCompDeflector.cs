using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;

namespace CompDeflector
{
    [StaticConstructorOnStartup]
    static class HarmonyCompDeflector
    {
        static HarmonyCompDeflector()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.deflector");

            harmony.Patch(typeof(Thing).GetMethod("TakeDamage"), new HarmonyMethod(typeof(HarmonyCompDeflector).GetMethod("TakeDamage_PreFix")), null);
            harmony.Patch(typeof(PawnRenderer).GetMethod("DrawEquipmentAiming"), null, new HarmonyMethod(typeof(HarmonyCompDeflector).GetMethod("DrawEquipmentAimingPostFix")), null);
        }



        //=================================== COMPDEFLECTOR

        //public static void SpecialDisplayStatsPostFix(Thing __instance, ref IEnumerable<StatDrawEntry> __result)
        //{
        //    ////Log.Message("3");
        //    ThingWithComps thingWithComps = __instance as ThingWithComps;
        //    if (thingWithComps != null)
        //    {
        //        CompDeflector compDeflector = thingWithComps.GetComp<CompDeflector>();
        //        if (compDeflector != null)
        //        {
        //            List<StatDrawEntry> origin = new List<StatDrawEntry>();
        //            foreach (StatDrawEntry entry in __result)
        //            {
        //                //Log.Message("Entry");
        //                origin.Add(entry);
        //            }

        //            List<StatDrawEntry> entries = new List<StatDrawEntry>();
        //            foreach (StatDrawEntry entry in compDeflector.PostSpecialDisplayStats())
        //            {
        //                //Log.Message("Hey!");
        //                entries.Add(entry);
        //            }

        //            origin.Concat(entries);

        //            __result = origin;
        //        }
        //    }
        //}

        public static void DrawEquipmentAimingPostFix(PawnRenderer __instance, Thing eq, Vector3 drawLoc, float aimAngle)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(PawnRenderer), "pawn").GetValue(__instance);
            if (pawn != null)
            {
                ////Log.Message("1");
                Pawn_EquipmentTracker pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                {

                    ////Log.Message("2");
                    foreach (ThingWithComps thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                    {

                        ////Log.Message("3");
                        if (thingWithComps != null)
                        {

                            ////Log.Message("4");
                            ////Log.Message("3");
                            CompDeflector compDeflector = thingWithComps.GetComp<CompDeflector>();
                            if (compDeflector != null)
                            {

                                ////Log.Message("5");
                                if (compDeflector.IsAnimatingNow)
                                {
                                bool flip = false;
                                if (!Find.TickManager.Paused && compDeflector.IsAnimatingNow) compDeflector.AnimationDeflectionTicks -= 20;
                                float offset = eq.def.equippedAngleOffset;
                                float num = aimAngle - 90f;
                                if (aimAngle > 20f && aimAngle < 160f)
                                {
                                    //mesh = MeshPool.plane10;
                                    num += offset;
                                    if (compDeflector.IsAnimatingNow) num += ((compDeflector.AnimationDeflectionTicks + 1) / 2);
                                }
                                else if (aimAngle > 200f && aimAngle < 340f)
                                {
                                    //mesh = MeshPool.plane10Flip;
                                    flip = true;
                                    num -= 180f;
                                    num -= offset;
                                    if (compDeflector.IsAnimatingNow) num -= ((compDeflector.AnimationDeflectionTicks + 1) / 2);
                                }
                                else
                                {
                                    //mesh = MeshPool.plane10;
                                    num += offset;
                                    if (compDeflector.IsAnimatingNow) num += ((compDeflector.AnimationDeflectionTicks + 1) / 2);
                                }
                                num %= 360f;
                                Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
                                Material matSingle;
                                if (graphic_StackCount != null)
                                {
                                    matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
                                }
                                else
                                {
                                    matSingle = eq.Graphic.MatSingle;
                                }
                                //mesh = MeshPool.GridPlane(thingWithComps.def.graphicData.drawSize);
                                //Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);

                                Vector3 s = new Vector3(eq.def.graphicData.drawSize.x, 1f, eq.def.graphicData.drawSize.y);
                                Matrix4x4 matrix = default(Matrix4x4);
                                matrix.SetTRS(drawLoc, Quaternion.AngleAxis(num, Vector3.up), s);
                                if (!flip) Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
                                else Graphics.DrawMesh(MeshPool.plane10Flip, matrix, matSingle, 0);
                                
                                ////Log.Message("DeflectDraw");
                                }
                            }
                        }
                    }
                }
            }
            
        }

        public static bool TakeDamage_PreFix(Thing __instance, ref DamageInfo dinfo)
        {
            //Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            //if (dinfo.Instigator == null) return true;
            try
            {
                Pawn pawn = __instance as Pawn;
                if (pawn != null)
                {
                    Pawn_EquipmentTracker pawn_EquipmentTracker = pawn.equipment;
                    if (pawn_EquipmentTracker != null)
                    {
                        if (pawn_EquipmentTracker.AllEquipmentListForReading != null && pawn_EquipmentTracker.AllEquipmentListForReading.Count > 0)
                        {
                            foreach (ThingWithComps thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                            {
                                if (thingWithComps != null)
                                {
                                    ////Log.Message("3");
                                    CompDeflector compDeflector = thingWithComps.GetComp<CompDeflector>();
                                    if (compDeflector != null)
                                    {
                                        if (dinfo.Def != DamageDefOf.Bomb)
                                        {
                                            if (dinfo.Def != DamageDefOf.Flame)
                                            {
                                                if (!dinfo.Def.isExplosive)
                                                {
                                                    if (!dinfo.WeaponGear.IsMeleeWeapon)
                                                    {
                                                        bool newAbsorbed = false;
                                                        compDeflector.PostPreApplyDamage(dinfo, out newAbsorbed);
                                                        if (newAbsorbed)
                                                        {
                                                            compDeflector.AnimationDeflectionTicks = 1200;
                                                            dinfo.SetAmount(0);
                                                            return false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (compDeflector.TrySpecialMeleeBlock())
                                                        {
                                                            dinfo.SetAmount(0);
                                                            return false;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException) { }
            return true;
        }

    }
}
