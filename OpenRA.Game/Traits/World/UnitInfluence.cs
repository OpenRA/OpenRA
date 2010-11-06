#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class UnitInfluenceInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new UnitInfluence( init.world ); }
	}

	public class UnitInfluence
	{
		class InfluenceNode
		{
			public InfluenceNode next;
			public Actor actor;
		}

		InfluenceNode[,] influence;
		Map map;

		public UnitInfluence( World world )
		{
			map = world.Map;
			influence = new InfluenceNode[world.Map.MapSize.X, world.Map.MapSize.Y];

			world.ActorRemoved += a => Remove( a, a.TraitOrDefault<IOccupySpace>() );
		}

		public IEnumerable<Actor> GetUnitsAt( int2 a )
		{
			if (!map.IsInMap(a)) yield break;

			for( var i = influence[ a.X, a.Y ] ; i != null ; i = i.next )
				yield return i.actor;
		}

		public bool AnyUnitsAt(int2 a)
		{
			return /*map.IsInMap(a) && */influence[ a.X, a.Y ] != null;
		}

		public void Add( Actor self, IOccupySpace unit )
		{
			foreach( var c in unit.OccupiedCells() )
				influence[ c.X, c.Y ] = new InfluenceNode { next = influence[ c.X, c.Y ], actor = self };
		}

		public void Remove( Actor self, IOccupySpace unit )
		{
			if (unit != null)
				foreach (var c in unit.OccupiedCells())
					RemoveInner( ref influence[ c.X, c.Y ], self );
		}

		void RemoveInner( ref InfluenceNode influenceNode, Actor toRemove )
		{
			if( influenceNode == null )
				return;
			else if( influenceNode.actor == toRemove )
				influenceNode = influenceNode.next;

			RemoveInner( ref influenceNode.next, toRemove );
		}

		public void Update(Actor self, IOccupySpace unit)
		{
			Remove(self, unit);
			if (!self.IsDead()) Add(self, unit);
		}
	}
}
