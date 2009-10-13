using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class BuildingInfluenceMap
	{
		Actor[,] influence = new Actor[128, 128];

		public BuildingInfluenceMap(World world, Player player)
		{
			world.ActorAdded +=
				a => { if (a.traits.Contains<Traits.Building>() && a.Owner == player) AddInfluence(a); };
			world.ActorRemoved +=
				a => { if (a.traits.Contains<Traits.Building>() && a.Owner == player) RemoveInfluence(a); };
		}

		void AddInfluence(Actor a)
		{
			foreach (var t in Footprint.Tiles(a.unitInfo.Name, a.Location))
				if (IsValid(t))
					influence[t.X, t.Y] = a;
		}

		void RemoveInfluence(Actor a)
		{
			foreach (var t in Footprint.Tiles(a.unitInfo.Name, a.Location))
				if (IsValid(t))
					influence[t.X, t.Y] = null;
		}

		bool IsValid(int2 t)
		{
			return !(t.X < 0 || t.Y < 0 || t.X >= 128 || t.Y >= 128);
		}

		public Actor this[int2 cell] { get { return IsValid(cell) ? influence[cell.X, cell.Y] : null; } }
	}
}
