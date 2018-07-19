using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace JecsTools
{
	public static class JecsToolsFactionDialogMaker
	{
		public static DiaNode FactionDialogFor(Pawn negotiator, Faction faction)
		{
			var map = negotiator.Map;
			JecsToolsFactionDialogMaker.negotiator = negotiator;
			JecsToolsFactionDialogMaker.faction = faction;
			var text = (faction.leader != null) ? faction.leader.Name.ToStringFull : faction.Name;
			var factionSettings = faction?.def?.GetModExtension<FactionSettings>();
			var greetingHostileKey = factionSettings?.greetingHostileKey ?? "FactionGreetingHostile";
			var greetingWaryKey = factionSettings?.greetingWaryKey ?? "FactionGreetingWary";
			var greetingWarmKey = factionSettings?.greetingWarmKey ?? "FactionGreetingWarm";
			var waryMinimum = factionSettings?.waryMinimumRelations ?? -70f;
			var warmMinimum = factionSettings?.warmMinimumRelations ?? 40f;
			
			var greetingHostile = greetingHostileKey.Translate(text);
			var greetingWary = greetingWaryKey.Translate(new object[]
			{
				text,
				negotiator.LabelShort
			});
			var greetingWarm = greetingWarmKey.Translate(new object[]
			{
				text,
				negotiator.LabelShort
			});
			
			if (faction.PlayerGoodwill < waryMinimum)
			{
				root = new DiaNode(greetingHostile);
				if (!SettlementUtility.IsPlayerAttackingAnySettlementOf(faction) && negotiator.Spawned && negotiator.Map.IsPlayerHome)
				{
					root.options.Add(PeaceTalksOption(faction, negotiator.Map));
				}
			}
			else
			{
				if (faction.PlayerGoodwill < warmMinimum)
				{
					greetingWary = greetingWary.AdjustedFor(negotiator);
					root = new DiaNode(greetingWary);
					if (!SettlementUtility.IsPlayerAttackingAnySettlementOf(faction))
					{
						root.options.Add(OfferGiftOption(negotiator.Map));
					}
					if (!faction.HostileTo(Faction.OfPlayer) && negotiator.Spawned && negotiator.Map.IsPlayerHome)
					{
						root.options.Add(RequestTraderOption(map, 1100));
					}
				}
				else
				{
					root = new DiaNode(greetingWarm);
					if (!SettlementUtility.IsPlayerAttackingAnySettlementOf(faction))
					{
						root.options.Add(OfferGiftOption(negotiator.Map));
					}
					if (!faction.HostileTo(Faction.OfPlayer) && negotiator.Spawned && negotiator.Map.IsPlayerHome)
					{
						root.options.Add(RequestTraderOption(map, 700));
						root.options.Add(RequestMilitaryAidOption(map));
					}
				}
			}
			if (Prefs.DevMode)
			{
				foreach (var item in DebugOptions())
				{
					root.options.Add(item);
				}
			}
			var diaOption = new DiaOption("(" + "Disconnect".Translate() + ")");
			diaOption.resolveTree = true;
			root.options.Add(diaOption);
			return root;
		}

		private static IEnumerable<DiaOption> DebugOptions()
		{
			var opt = new DiaOption("(Debug) Goodwill +10");
			opt.action = delegate
			{
				faction.TryAffectGoodwillWith(Faction.OfPlayer, 10, false, true, null, null);
			};
			opt.linkLateBind = (() => FactionDialogFor(negotiator, faction));
			yield return opt;
			var opt2 = new DiaOption("(Debug) Goodwill -10");
			opt2.action = delegate
			{
				faction.TryAffectGoodwillWith(Faction.OfPlayer, -10, false, true, null, null);
			};
			opt2.linkLateBind = (() => FactionDialogFor(negotiator, faction));
			yield return opt2;
			var opt3 = new DiaOption("(Debug) Potatoes");
			opt3.action = delegate
			{
				Messages.Message("Boil 'em, mash 'em, stick em in a stew.", MessageTypeDefOf.PositiveEvent);
			};
			opt3.linkLateBind = (() => FactionDialogFor(negotiator, faction));
			yield return opt3;
			yield break;
		}

		private static int AmountSendableSilver(Map map)
		{
			return (from t in TradeUtility.AllLaunchableThingsForTrade(map)
			where t.def == ThingDefOf.Silver
			select t).Sum((Thing t) => t.stackCount);
		}

		
		private static DiaOption PeaceTalksOption(Faction faction, Map map)
		{
			var def = IncidentDef.Named("QuestPeaceTalks");
			if (PeaceTalksExist(faction))
			{
				var diaOption = new DiaOption(def.letterLabel);
				diaOption.Disable("InProgress".Translate());
				return diaOption;
			}

			var diaOption2 = new DiaOption(def.letterLabel)
			{
				action = delegate
				{
					PlaySoundFor(faction); 
					if (!TryStartPeaceTalks(faction))
						Log.Error("Peace talks event failed to start. This should never happen.");
				}
			};
			var text = string.Format(def.letterText.AdjustedFor(faction.leader), faction.def.leaderTitle, faction.Name, 15)
				.CapitalizeFirst();
			diaOption2.link = new DiaNode(text)
			{
				options = 
				{
					OKToRoot()
				}
			};
			return diaOption2;
		}

		
		private static DiaOption OfferGiftOption(Map map)
		{
			if (AmountSendableSilver(map) < 300)
			{
				var diaOption = new DiaOption("OfferGift".Translate());
				diaOption.Disable("NeedSilverLaunchable".Translate(new object[]
				{
					300
				}));
				return diaOption;
			}
			var goodwillDelta = 12f * negotiator.GetStatValue(StatDefOf.NegotiationAbility, true);
			var diaOption2 = new DiaOption("OfferGift".Translate() + " (" + "SilverForGoodwill".Translate(new object[]
			{
				300,
				goodwillDelta.ToString("#####0")
			}) + ")");
			diaOption2.action = delegate
			{
				TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, 300, map, null);
				faction.TryAffectGoodwillWith(Faction.OfPlayer, (int)goodwillDelta);
				PlaySoundFor(faction);
			};
			var text = "SilverGiftSent".Translate(new object[]
			{
				faction.leader.LabelIndefinite(),
				Mathf.RoundToInt(goodwillDelta)
			}).CapitalizeFirst();
			diaOption2.link = new DiaNode(text)
			{
				options = 
				{
					OKToRoot()
				}
			};
			return diaOption2;
		}

		private static DiaOption RequestTraderOption(Map map, int silverCost)
		{
			var text = "RequestTrader".Translate(new object[]
			{
				silverCost.ToString()
			});
			if (AmountSendableSilver(map) < silverCost)
			{
				var diaOption = new DiaOption(text);
				diaOption.Disable("NeedSilverLaunchable".Translate(new object[]
				{
					silverCost
				}));
				return diaOption;
			}
			if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp))
			{
				var diaOption2 = new DiaOption(text);
				diaOption2.Disable("BadTemperature".Translate());
				return diaOption2;
			}
			var num = faction.lastTraderRequestTick + 240000 - Find.TickManager.TicksGame;
			if (num > 0)
			{
				var diaOption3 = new DiaOption(text);
				diaOption3.Disable("WaitTime".Translate(new object[]
				{
					num.ToStringTicksToPeriod()
				}));
				return diaOption3;
			}
			var diaOption4 = new DiaOption(text);
			var diaNode = new DiaNode("TraderSent".Translate(new object[]
			{
				faction.leader.LabelIndefinite()
			}).CapitalizeFirst());
			diaNode.options.Add(OKToRoot());
			var diaNode2 = new DiaNode("ChooseTraderKind".Translate(new object[]
			{
				faction.leader.LabelIndefinite()
			}));
			foreach (var localTk2 in faction.def.caravanTraderKinds)
			{
				var localTk = localTk2;
				var diaOption5 = new DiaOption(localTk.LabelCap);
				diaOption5.action = delegate
				{
					var incidentParms = new IncidentParms();
					incidentParms.target = map;
					incidentParms.faction = faction;
					incidentParms.traderKind = localTk;
					incidentParms.forced = true;
					Find.Storyteller.incidentQueue.Add(IncidentDefOf.TraderCaravanArrival, Find.TickManager.TicksGame + 120000, incidentParms);
					faction.lastTraderRequestTick = Find.TickManager.TicksGame;
					TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, silverCost, map, null);
					PlaySoundFor(faction);
				};
				diaOption5.link = diaNode;
				diaNode2.options.Add(diaOption5);
			}
			var diaOption6 = new DiaOption("GoBack".Translate());
			diaOption6.linkLateBind = ResetToRoot();
			diaNode2.options.Add(diaOption6);
			diaOption4.link = diaNode2;
			return diaOption4;
		}

		private static DiaOption RequestMilitaryAidOption(Map map)
		{
			var text = "RequestMilitaryAid".Translate(new object[]
			{
				-25f
			});
			if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp))
			{
				var diaOption = new DiaOption(text);
				diaOption.Disable("BadTemperature".Translate());
				return diaOption;
			}
			var diaOption2 = new DiaOption(text);
			IEnumerable<IAttackTarget> targetsHostileToColony = map.attackTargetsCache.TargetsHostileToColony;
			if (JecsToolsFactionDialogMaker.megaOne == null)
			{
				JecsToolsFactionDialogMaker.megaOne = new Func<IAttackTarget, bool>(GenHostility.IsActiveThreatToPlayer);
			}
			if (targetsHostileToColony.Any(JecsToolsFactionDialogMaker.megaOne))
			{
				if (!map.attackTargetsCache.TargetsHostileToColony.Any((IAttackTarget p) => ((Thing)p).Faction != null && ((Thing)p).Faction.HostileTo(faction)))
				{
					IEnumerable<IAttackTarget> targetsHostileToColony2 = map.attackTargetsCache.TargetsHostileToColony;
					if (JecsToolsFactionDialogMaker.megaTwo == null)
					{
						JecsToolsFactionDialogMaker.megaTwo = new Func<IAttackTarget, bool>(GenHostility.IsActiveThreatToPlayer);
					}
					IEnumerable<Faction> source = (from pa in targetsHostileToColony2.Where(JecsToolsFactionDialogMaker.megaTwo)
					select ((Thing)pa).Faction into fa
					where fa != null && !fa.HostileTo(faction)
					select fa).Distinct<Faction>();
					var key = "MilitaryAidConfirmMutualEnemy";
					var array = new object[2];
					array[0] = faction.Name;
					array[1] = GenText.ToCommaList(from fa in source
					select fa.Name, true);
					var diaNode = new DiaNode(key.Translate(array));
					var diaOption3 = new DiaOption("CallConfirm".Translate());
					diaOption3.action = delegate
					{
						CallForAid(map);
					};
					diaOption3.link = FightersSent();
					var diaOption4 = new DiaOption("CallCancel".Translate());
					diaOption4.linkLateBind = ResetToRoot();
					diaNode.options.Add(diaOption3);
					diaNode.options.Add(diaOption4);
					diaOption2.link = diaNode;
					return diaOption2;
				}
			}
			diaOption2.action = delegate
			{
				CallForAid(map);
			};
			diaOption2.link = FightersSent();
			return diaOption2;
		}

		private static DiaNode FightersSent()
		{
			return new DiaNode("MilitaryAidSent".Translate(new object[]
			{
				faction.leader.LabelIndefinite()
			}).CapitalizeFirst())
			{
				options = 
				{
					OKToRoot()
				}
			};
		}

		private static void CallForAid(Map map)
		{
			PlaySoundFor(faction);
			faction.TryAffectGoodwillWith(Faction.OfPlayer, -25);
			var incidentParms = new IncidentParms
			{
				target = map,
				faction = faction,
				points = (float) Rand.Range(150, 400)
			};
			IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
			HarmonyPatches.lastPhoneAideFaction = faction;
			HarmonyPatches.lastPhoneAideTick = Find.TickManager.TicksGame;
		}

		public static void PlaySoundFor(Faction faction)
		{
			if (faction.def.GetModExtension<FactionSettings>() is FactionSettings fs)
			{
				fs?.entrySoundDef?.PlayOneShotOnCamera();
			}
		}

		private static DiaOption OKToRoot()
		{
			return new DiaOption("OK".Translate())
			{
				linkLateBind = ResetToRoot()
			};
		}

		private static Func<DiaNode> ResetToRoot()
		{
			return () => FactionDialogFor(negotiator, faction);
		}

		private static DiaNode root;

		private static Pawn negotiator;

		private static Faction faction;

		private const float MinRelationsToCommunicate = -70f;

		private const float MinRelationsFriendly = 40f;

		private const int GiftSilverAmount = 300;

		private const float GiftSilverGoodwillChange = 12f;

		private const float MilitaryAidRelsChange = -25f;

		private const int TradeRequestCost_Wary = 1100;

		private const int TradeRequestCost_Warm = 700;

		[CompilerGenerated]
		private static Func<IAttackTarget, bool> megaOne;

		[CompilerGenerated]
		private static Func<IAttackTarget, bool> megaTwo;
		
		
		private static bool TryStartPeaceTalks(Faction faction)
		{
			int tile;
			if (!JecsToolsFactionDialogMaker.TryFindTile(out tile))
			{
				return false;
			}
			PeaceTalks peaceTalks = (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.PeaceTalks);
			peaceTalks.Tile = tile;
			peaceTalks.SetFaction(faction);
			peaceTalks.GetComponent<TimeoutComp>().StartTimeout(900000);
			Find.WorldObjects.Add(peaceTalks);
			var def = IncidentDef.Named("QuestPeaceTalks");
			string text = string.Format(def.letterText.AdjustedFor(faction.leader), faction.def.leaderTitle, faction.Name, 15).CapitalizeFirst();
			Find.LetterStack.ReceiveLetter(def.letterLabel, text, def.letterDef, peaceTalks, null);
			return true;
		}

		private static bool TryFindTile(out int tile) => TileFinder.TryFindNewSiteTile(out tile, 5, 15, false, false, -1);

		private static bool PeaceTalksExist(Faction faction)
		{
			var peaceTalks = Find.WorldObjects.PeaceTalks;
			for (int i = 0; i < peaceTalks.Count; i++)
				if (peaceTalks[i].Faction == faction)
					return true;
			return false;
		}
	}
}
