#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Produce the unit on map edge or under shroud/fog and move into play.")]
	public class ProductionFromMapEdgeInfo : ProductionInfo
	{
		public override object Create(ActorInitializer init) { return new ProductionFromMapEdge(this, init.Self); }
	}

	public class ProductionFromMapEdge : Production
	{
		public ProductionFromMapEdge(ProductionFromMapEdgeInfo info, Actor self)
			: base(info, self) { }

		public override bool Produce(Actor self, ActorInfo producee, string raceVariant)
		{
			// Get a spawn cell
			var location = GetNearestInvisibleCell(self.Owner, self.Location);
			var pos = self.World.Map.CenterOfCell(location);

			var initialFacing = self.World.Map.FacingBetween(location, self.Location, 0);

			// If aircraft, spawn at cruise altitude
			var ai = producee.Traits.WithInterface<AircraftInfo>().FirstOrDefault();
			if (ai != null)
				pos += new WVec(0, 0, ai.CruiseAltitude.Range);

			self.World.AddFrameEndTask(w =>
			{
				var td = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new LocationInit(location),
					new CenterPositionInit(pos),
					new FacingInit(initialFacing)
				};

				if (raceVariant != null)
					td.Add(new RaceInit(raceVariant));

				var newUnit = self.World.CreateActor(producee.Name, td);

				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
					newUnit.QueueActivity(move.MoveIntoWorld(newUnit, self.Location));

				newUnit.SetTargetLine(Target.FromCell(self.World, self.Location), Color.Green, false);

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, self.Location);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit);

				var bi = newUnit.Info.Traits.GetOrDefault<BuildableInfo>();
				if (bi != null && bi.InitialActivity != null)
					newUnit.QueueActivity(Game.CreateObject<Activity>(bi.InitialActivity));

				foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
					t.BuildingComplete(newUnit);
			});

			return true;
		}

		// Gives a (1) cell that is invisible due to fog, or if none (2) a cell that has not been explored yet (shrouded) or as a last resort (3) a cell on map edge.
		public static CPos GetNearestInvisibleCell(Player player, CPos toCell)
		{
			// Get list of players against wich a shroud/fog test should be done
			var checkplayers = player.World.Players
				.Where(p => p.Playable);

			// Please note hack: World.LobbyInfo.GlobalSettings.Fog returns wrong value for shellmap. Therefore skip to edge spawn on shellmap.

			// Get the closest fogged cell
			if (player.Shroud.FogEnabled && player.World.Type == WorldType.Regular)
			{
				var location = player.World.Map.Cells.Where(c => CheckFogged(c, checkplayers) == true).OrderBy(c => (c - toCell).LengthSquared).FirstOrDefault();

				if (location != CPos.Zero)
					return location;
			}

			// Get the closest shrouded cell
			if (player.Shroud.ShroudEnabled && player.World.Type == WorldType.Regular)
			{
				var location = player.World.Map.Cells.Where(c => CheckUnexplored(c, checkplayers) == true).OrderBy(c => (c - toCell).LengthSquared).FirstOrDefault();

				if (location != CPos.Zero)
					return location;
			}

			// Last resort
			return player.World.Map.ChooseRandomEdgeCell(player.World.SharedRandom);
		}

		// True when none of the players can see the cell
		static bool CheckFogged(CPos c, IEnumerable<Player> players)
		{
			return !players.Any(p => p.Shroud.IsVisible(c));
		}

		// True when none of the players have explored the cell
		static bool CheckUnexplored(CPos c, IEnumerable<Player> players)
		{
			return !players.Any(p => p.Shroud.IsExplored(c));
		}
	}
}
