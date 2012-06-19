using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Traits
{
	public sealed class DangerZoneLayerInfo : TraitInfo<DangerZoneLayer>
	{
	}

	public sealed class DangerZoneLayer
	{
		// NOTE: There really only needs to be at most one DangerZone per cell.
		Dictionary<CPos, DangerZone> DangerZones;

		public DangerZoneLayer()
		{
			DangerZones = new Dictionary<CPos, DangerZone>();
		}

		public int DangerCost(Actor self, CPos loc, Func<Actor, DangerZone, bool> shouldAvoid)
		{
			// Check if there's a danger zone at this location:
			DangerZone zone;
			if (!DangerZones.TryGetValue(loc, out zone)) return 0;

			// Has it expired?
			if (self.World.FrameNumber - zone.CreatedFrame > 300)
			{
				// Make GC happy:
				RemoveDangerZone(self.World, zone);
				return 0;
			}

			// Should the unit avoid this zone?
			if (!shouldAvoid(self, zone)) return 0;

			// Penalty is highest at 0 distance from center and falls off exponentially around it:
			int rsqr = (zone.CellRadiusSquared - (zone.CellLocation - loc).LengthSquared);
			return 30 + rsqr * 50;
		}

		public void AddDangerZone(World world, DangerZone zone)
		{
			// Set the DangerZone to all cells in the blast radius:
			foreach (var tile in world.FindTilesInCircle(zone.CellLocation, zone.PixelRadius / Game.CellSize))
			{
				DangerZones[tile] = zone;
			}
		}

		public void RemoveDangerZone(World world, DangerZone zone)
		{
			// We gotta keep the GC happy and avoid storing `DangerZone`s in a Dictionary that won't die.
			foreach (var tile in world.FindTilesInCircle(zone.CellLocation, zone.PixelRadius / Game.CellSize))
			{
				DangerZone tileZone;
				if (DangerZones.TryGetValue(tile, out tileZone))
				{
					// Remove the DangerZone from this tile if it's the one we expect (not if something else overwrote it):
					if (tileZone == zone) DangerZones.Remove(tile);
				}
			}
		}
	}
}
