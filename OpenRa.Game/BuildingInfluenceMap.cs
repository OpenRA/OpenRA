using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using IjwFramework.Types;
using IjwFramework.Collections;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class BuildingInfluenceMap
	{
		bool[,] blocked = new bool[128, 128];
		Actor[,] influence = new Actor[128, 128];
		static readonly Pair<Actor, float> NoClaim = Pair.New((Actor)null, float.MaxValue);

		public BuildingInfluenceMap()
		{
			for (int j = 0; j < 128; j++)
				for (int i = 0; i < 128; i++)
					influence[i, j] = null;

			Game.world.ActorAdded +=
				a => { if (a.traits.Contains<Traits.Building>()) AddInfluence(a, a.traits.Get<Traits.Building>()); };
			Game.world.ActorRemoved +=
				a => { if (a.traits.Contains<Traits.Building>()) RemoveInfluence(a, a.traits.Get<Traits.Building>()); };
		}

		void AddInfluence(Actor a, Traits.Building building)
		{
			foreach (var u in Footprint.UnpathableTiles(building.unitInfo, a.Location))
				if (IsValid(u))
					blocked[u.X, u.Y] = true;

			foreach (var u in Footprint.Tiles(building.unitInfo, a.Location, false))
				if (IsValid(u))
					influence[u.X, u.Y] = a;
		}

		void RemoveInfluence(Actor a, Traits.Building building)
		{
			foreach (var u in Footprint.UnpathableTiles(building.unitInfo, a.Location))
				if (IsValid(u))
					blocked[u.X, u.Y] = false;

			foreach (var u in Footprint.Tiles(building.unitInfo, a.Location, false))
				if (IsValid(u))
					influence[u.X, u.Y] = null;
		}

		bool IsValid(int2 t)
		{
			return !(t.X < 0 || t.Y < 0 || t.X >= 128 || t.Y >= 128);
		}

		public Actor GetBuildingAt(int2 cell)
		{
			if (!IsValid(cell)) return null;
			return influence[cell.X, cell.Y];
		}

		public Actor GetNearestBuilding(int2 cell)
		{
			if (!IsValid(cell)) return null;
			return influence[cell.X, cell.Y];
		}

		public int GetDistanceToBuilding(int2 cell)
		{
			if (!IsValid(cell)) return int.MaxValue;
			return influence[cell.X, cell.Y] == null ? int.MaxValue : 0;
		}

		public bool CanMoveHere(int2 cell)
		{
			return IsValid(cell) && !blocked[cell.X, cell.Y];
		}
	}
}
