using IjwFramework.Types;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class BuildingInfluenceMap
	{
		bool[,] blocked = new bool[128, 128];
		Actor[,] influence = new Actor[128, 128];
		static readonly Pair<Actor, float> NoClaim = Pair.New((Actor)null, float.MaxValue);

		public BuildingInfluenceMap()
		{
			Game.world.ActorAdded +=
				a => { if (a.traits.Contains<Building>()) 
					ChangeInfluence(a, a.traits.Get<Building>(), true); };
			Game.world.ActorRemoved +=
				a => { if (a.traits.Contains<Building>()) 
					ChangeInfluence(a, a.traits.Get<Building>(), false); };
		}

		void ChangeInfluence(Actor a, Building building, bool isAdd)
		{
			foreach (var u in Footprint.UnpathableTiles(building.unitInfo, a.Location))
				if (IsValid(u))
					blocked[u.X, u.Y] = isAdd;

			foreach (var u in Footprint.Tiles(building.unitInfo, a.Location, false))
				if (IsValid(u))
					influence[u.X, u.Y] = isAdd ? a : null;
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
