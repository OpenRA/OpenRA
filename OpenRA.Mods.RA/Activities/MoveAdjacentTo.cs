#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Activities
{
	public class MoveAdjacentTo : Activity
	{
		readonly Actor target;

		public MoveAdjacentTo( Actor target )
		{
			this.target = target;
		}

		public override Activity Tick( Actor self )
		{
			if( IsCanceled || target.Destroyed || !target.IsInWorld) return NextActivity;

			var mobile = self.Trait<Mobile>();

			var cells = target.Trait<IOccupySpace>().OccupiedCells().ToArray();
			if (cells.Length == 0)
				cells = new OpenRA.FileFormats.Pair<int2, SubCell>[] {
					Pair.New(target.Location, SubCell.FullCell) };

			var ps1 = new PathSearch( self.World, mobile.Info, self.Owner )
			{
				checkForBlocked = true,
				heuristic = location => 0,
				inReverse = true
			};

			foreach( var cell in cells )
			{
				ps1.AddInitialCell( cell.First );
				if( ( mobile.toCell - cell.First ).LengthSquared <= 2 )
					return NextActivity;
			}
			ps1.heuristic = PathSearch.DefaultEstimator( mobile.toCell );

			var ps2 = PathSearch.FromPoint( self.World, mobile.Info, self.Owner, mobile.toCell, target.Location, true );
			var ret = self.World.WorldActor.Trait<PathFinder>().FindBidiPath( ps1, ps2 );
			if( ret.Count > 0 )
				ret.RemoveAt( 0 );
			return Util.SequenceActivities( mobile.MoveTo( () => ret ), this );
		}
	}
}
