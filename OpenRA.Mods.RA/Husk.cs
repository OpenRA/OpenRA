#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA
{
	class HuskInfo : ITraitInfo, IFacingInfo
	{
		public object Create( ActorInitializer init ) { return new Husk( init ); }
	}

	class Husk : IOccupySpace, IFacing, ISync
	{
		[Sync]
		int2 location;

		[Sync]
		public int2 PxPosition { get; set; }

		[Sync]
		public int Facing { get; set; }
		public int ROT { get { return 0; } }
		public int InitialFacing { get { return 128; } }

		public Husk(ActorInitializer init)
		{
			var self = init.self;
			location = init.Get<LocationInit,int2>();
			PxPosition = init.Get<CenterLocationInit, int2>();
			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : 128;

			var speed = init.Contains<HuskSpeedInit>() ? init.Get<HuskSpeedInit,int>() : 0;
			if (speed > 0)
			{
				var to = Util.CenterOfCell(location);
				var length = (int)((to - PxPosition).Length * 3 / speed);
				self.QueueActivity(new DragHusk(PxPosition, to, length, this));
			}
		}

		public int2 TopLeft { get { return location; } }

		public IEnumerable<Pair<int2, SubCell>> OccupiedCells() { yield return Pair.New(TopLeft, SubCell.FullCell); }

		class DragHusk : Activity
		{
			Husk husk;
			int2 endLocation;
			int2 startLocation;
			int length;

			public DragHusk(int2 start, int2 end, int length, Husk husk)
			{
				startLocation = start;
				endLocation = end;
				this.length = length;
				this.husk = husk;
			}

			int ticks = 0;
			public override Activity Tick( Actor self )
			{
				if (ticks >= length || length <= 1)
				{
					husk.PxPosition = endLocation;
					return NextActivity;
				}

				husk.PxPosition = int2.Lerp(startLocation, endLocation, ticks++, length - 1);
				return this;
			}

			public override IEnumerable<Target> GetTargets( Actor self ) { yield break; }
			public override void Cancel( Actor self ) { }
		}
	}

	public class HuskSpeedInit : IActorInit<int>
	{
		[FieldFromYamlKey] public readonly int value = 0;
		public HuskSpeedInit() { }
		public HuskSpeedInit( int init ) { value = init; }
		public int Value( World world ) { return value; }
	}
}
