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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("A dictionary of buildings placed on the map. Attach this to the world actor.")]
	public class BuildingInfluenceInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new BuildingInfluence(init.World); }
	}

	public class BuildingInfluence
	{
		class InfluenceNode
		{
			public InfluenceNode Next;
			public Actor Actor;
		}

		readonly Map map;
		readonly CellLayer<InfluenceNode> influence;

		public BuildingInfluence(World world)
		{
			map = world.Map;

			influence = new CellLayer<InfluenceNode>(map);
		}

		internal void AddInfluence(Actor a, IEnumerable<CPos> cells)
		{
			foreach (var c in cells)
			{
				var uv = c.ToMPos(map);
				if (influence.Contains(uv))
					influence[uv] = new InfluenceNode { Next = influence[uv], Actor = a };
			}
		}

		internal void RemoveInfluence(Actor a, IEnumerable<CPos> cells)
		{
			foreach (var c in cells)
			{
				var uv = c.ToMPos(map);
				if (!influence.Contains(uv))
					continue;

				influence[uv] = RemoveInfluenceInner(influence[uv], a);
			}
		}

		static InfluenceNode RemoveInfluenceInner(InfluenceNode influenceNode, Actor toRemove)
		{
			if (influenceNode == null)
				return null;

			influenceNode.Next = RemoveInfluenceInner(influenceNode.Next, toRemove);
			return influenceNode.Actor == toRemove ? influenceNode.Next : influenceNode;
		}

		public IEnumerable<Actor> GetBuildingsAt(CPos cell)
		{
			var uv = cell.ToMPos(map);
			if (!influence.Contains(uv))
				yield break;

			var node = influence[uv];
			while (node != null)
			{
				yield return node.Actor;
				node = node.Next;
			}
		}

		public bool AnyBuildingAt(CPos cell)
		{
			return influence.Contains(cell) && influence[cell] != null;
		}
	}
}
