#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public sealed class ThreatProfile
	{
		public int Air;
		public int Armor;
		public int Infantry;
		public int Naval;
		public int Buildings;
		public int TicksSinceLastSighting = int.MaxValue;
		public CPos? LastKnownBaseLocation;
		public CPos? LastContactLocation;
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Tracks visible enemy threats for Adaptive AI.")]
	public sealed class BotIntelModuleInfo : ConditionalTraitInfo
	{
		[ActorReference]
		public readonly FrozenSet<string> AirUnitTypes = FrozenSet<string>.Empty;

		[ActorReference]
		public readonly FrozenSet<string> ArmorUnitTypes = FrozenSet<string>.Empty;

		[ActorReference]
		public readonly FrozenSet<string> InfantryUnitTypes = FrozenSet<string>.Empty;

		[ActorReference]
		public readonly FrozenSet<string> NavalUnitTypes = FrozenSet<string>.Empty;

		[ActorReference]
		public readonly FrozenSet<string> BaseBuildingTypes = FrozenSet<string>.Empty;

		[Desc("Delay (in ticks) between full intel scans.")]
		public readonly int ScanInterval = 25;

		public override object Create(ActorInitializer init) { return new BotIntelModule(init.Self, this); }
	}

	public sealed class BotIntelModule : ConditionalTrait<BotIntelModuleInfo>, IBotTick, IBotRespondToAttack
	{
		readonly World world;
		readonly Player player;

		readonly Dictionary<Player, ThreatProfile> threats = [];
		int scanInterval;

		public BotIntelModule(Actor self, BotIntelModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		public ThreatProfile GetThreat(Player enemy)
		{
			if (enemy == null)
				return new ThreatProfile();

			if (!threats.TryGetValue(enemy, out var profile))
			{
				profile = new ThreatProfile();
				threats[enemy] = profile;
			}

			return profile;
		}

		public ThreatProfile GetPrimaryThreat()
		{
			return threats.Values.OrderByDescending(t => t.Air + t.Armor + t.Infantry + t.Buildings).FirstOrDefault()
				?? new ThreatProfile();
		}

		public int TicksSinceLastSighting
		{
			get
			{
				if (threats.Count == 0)
					return int.MaxValue;

				return threats.Values.Min(t => t.TicksSinceLastSighting);
			}
		}

		public CPos? GetLastKnownEnemyBase()
		{
			foreach (var profile in threats.Values)
			{
				if (profile.LastKnownBaseLocation.HasValue)
					return profile.LastKnownBaseLocation;
			}

			return null;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--scanInterval > 0)
			{
				foreach (var profile in threats.Values)
					profile.TicksSinceLastSighting++;
				return;
			}

			scanInterval = Info.ScanInterval;
			ScanVisibleIntel();
		}

		void ScanVisibleIntel()
		{
			var seenThisScan = new HashSet<Player>();
			foreach (var enemy in world.Players.Where(p => p.RelationshipWith(player) == PlayerRelationship.Enemy))
			{
				var profile = GetThreat(enemy);
				profile.Air = 0;
				profile.Armor = 0;
				profile.Infantry = 0;
				profile.Naval = 0;
				profile.Buildings = 0;

				foreach (var a in world.Actors.Where(a => a.Owner == enemy))
				{
					if (!AdaptiveAIUtils.IsVisibleEnemy(a, player) || a.OccupiesSpace == null)
						continue;

					seenThisScan.Add(enemy);
					profile.TicksSinceLastSighting = 0;
					profile.LastContactLocation = a.Location;
					ClassifyActor(a, profile);
				}

				if (player.FrozenActorLayer == null)
					continue;

				var scanOrigin = GetBaseLocation();
				foreach (var fa in player.FrozenActorLayer.FrozenActorsInCircle(world, world.Map.CenterOfCell(scanOrigin), WDist.FromCells(64)))
				{
					if (fa.Owner != enemy || !AdaptiveAIUtils.IsKnownEnemyFrozen(fa, player))
						continue;

					if (Info.BaseBuildingTypes.Contains(fa.Info.Name))
					{
						profile.Buildings++;
						profile.LastKnownBaseLocation = world.Map.CellContaining(fa.CenterPosition);
					}
				}
			}

			foreach (var kvp in threats)
			{
				if (!seenThisScan.Contains(kvp.Key))
					kvp.Value.TicksSinceLastSighting += Info.ScanInterval;
			}
		}

		void ClassifyActor(Actor a, ThreatProfile profile)
		{
			var name = a.Info.Name;
			if (Info.AirUnitTypes.Contains(name))
				profile.Air++;
			else if (Info.ArmorUnitTypes.Contains(name))
				profile.Armor++;
			else if (Info.NavalUnitTypes.Contains(name))
				profile.Naval++;
			else if (Info.InfantryUnitTypes.Contains(name))
				profile.Infantry++;
			else if (Info.BaseBuildingTypes.Contains(name))
			{
				profile.Buildings++;
				if (a.OccupiesSpace != null)
					profile.LastKnownBaseLocation = a.Location;
			}
		}

		CPos GetBaseLocation()
		{
			var fact = world.ActorsHavingTrait<Building>()
				.FirstOrDefault(a => a.Owner == player && Info.BaseBuildingTypes.Contains(a.Info.Name) && a.OccupiesSpace != null);
			return fact?.Location ?? default;
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor actor, AttackInfo e)
		{
			if (e.Attacker == null || !AdaptiveAIUtils.IsVisibleEnemy(e.Attacker, player))
				return;

			var profile = GetThreat(e.Attacker.Owner);
			profile.TicksSinceLastSighting = 0;
			if (e.Attacker.OccupiesSpace != null)
				profile.LastContactLocation = e.Attacker.Location;
			ClassifyActor(e.Attacker, profile);
		}
	}
}
