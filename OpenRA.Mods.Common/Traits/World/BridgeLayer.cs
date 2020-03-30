#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	interface IBridgeSegment
	{
		void Repair(Actor repairer);
		void Demolish(Actor saboteur);

		string Type { get; }
		DamageState DamageState { get; }
		CVec[] NeighbourOffsets { get; }
		bool Valid { get; }
		CPos Location { get; }
	}

	class BridgeLayerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new BridgeLayer(init.World); }
	}

	class BridgeLayer
	{
		readonly CellLayer<Actor> bridges;

		public BridgeLayer(World world)
		{
			bridges = new CellLayer<Actor>(world.Map);
		}

		public Actor this[CPos cell] { get { return bridges[cell]; } }

		public void Add(Actor b)
		{
			var buildingInfo = b.Info.TraitInfo<BuildingInfo>();
			foreach (var c in buildingInfo.PathableTiles(b.Location))
				bridges[c] = b;
		}

		public void Remove(Actor b)
		{
			var buildingInfo = b.Info.TraitInfo<BuildingInfo>();
			foreach (var c in buildingInfo.PathableTiles(b.Location))
				if (bridges[c] == b)
					bridges[c] = null;
		}
	}
}
