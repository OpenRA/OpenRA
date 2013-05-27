using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Orders;
using OpenRA.Support;
using OpenRA.Traits;
using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA
{
	public class DomainIndex
	{
		Rectangle bounds;
		int[,] domains;

		public DomainIndex(World world)
		{
			bounds = world.Map.Bounds;
			domains = new int[(bounds.Width + bounds.X), (bounds.Height + bounds.Y)];

			BuildDomains(world);
		}

		public int GetDomainOf(CPos p)
		{
			return domains[p.X, p.Y];
		}

		public bool IsCrossDomain(CPos p1, CPos p2)
		{
			return GetDomainOf(p1) != GetDomainOf(p2);
		}

		public void SetDomain(CPos p, int domain)
		{
			domains[p.X, p.Y] = domain;
		}

		void BuildDomains(World world)
		{
			Map map = world.Map;

			int i = 1;
			HashSet<CPos> unassigned = new HashSet<CPos>();

			// Fill up our set of yet-unassigned map cells
			for (int x = map.Bounds.Left; x < bounds.Right; x += 1)
			{
				for (int y = bounds.Top; y < bounds.Bottom; y += 1)
				{
					unassigned.Add(new CPos(x, y));
				}
			}

			while (unassigned.Count != 0)
			{
				var start = unassigned.First();
				unassigned.Remove(start);

				// Wander around looking for water transitions
				bool inWater = WorldUtils.GetTerrainInfo(world, start).IsWater;
				Queue<CPos> toProcess = new Queue<CPos>();
				HashSet<CPos> seen = new HashSet<CPos>();
				toProcess.Enqueue(start);

				do
				{
					CPos p = toProcess.Dequeue();
					if (seen.Contains(p)) continue;
					seen.Add(p);

					TerrainTypeInfo cellInfo = WorldUtils.GetTerrainInfo(world, p);
					bool isWater = cellInfo.IsWater;

					// Check if we're still in one contiguous domain
					if (inWater == isWater)
					{
						SetDomain(p, i);
						unassigned.Remove(p);

						// Visit our neighbors, if we haven't already
						foreach (var d in CVec.directions)
						{
							CPos nextPos = p + d;
							if (nextPos.X >= map.Bounds.Left && nextPos.Y >= map.Bounds.Top &&
								nextPos.X < map.Bounds.Right && nextPos.Y < map.Bounds.Bottom)
							{
								if (!seen.Contains(nextPos)) toProcess.Enqueue(nextPos);
							}
						}
					}
				} while (toProcess.Count != 0);

				i += 1;
			}

			Log.Write("debug", "{0}: Found {1} domains", map.Title, i-1);
		}
	}
}
