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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class DebugAircraftFacingInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftFacing(init.self); }
	}
	public class DebugAircraftFacing
	{
		readonly Actor self;
		public DebugAircraftFacing(Actor self){this.self = self;}
		[Sync] public int foo { get { return self.Trait<Aircraft>().Facing; } }
	}
	
	public class DebugAircraftSubPxXInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftSubPxX(init.self); }
	}
	public class DebugAircraftSubPxX
	{
		readonly Actor self;
		public DebugAircraftSubPxX(Actor self){this.self = self;}
		[Sync] public int foo { get { return self.Trait<Aircraft>().SubPxPosition.X; } }
	}
	
	public class DebugAircraftSubPxYInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftSubPxY(init.self); }
	}
	public class DebugAircraftSubPxY
	{
		readonly Actor self;
		public DebugAircraftSubPxY(Actor self){this.self = self;}
		[Sync] public int foo { get { return self.Trait<Aircraft>().SubPxPosition.Y; } }
	}
	
	public class DebugAircraftAltitudeInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftAltitude(init.self); }
	}
	public class DebugAircraftAltitude
	{
		readonly Actor self;
		public DebugAircraftAltitude(Actor self){this.self = self;}
		[Sync] public int Facing { get { return self.Trait<Aircraft>().Altitude; } }
	}
	
	public class AircraftInfo : ITraitInfo
	{
		public readonly int CruiseAltitude = 30;
		[ActorReference]
		public readonly string[] RepairBuildings = { "fix" };
		[ActorReference]
		public readonly string[] RearmBuildings = { "hpad", "afld" };
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;
		public readonly string[] LandableTerrainTypes = { };

		public virtual object Create( ActorInitializer init ) { return new Aircraft( init , this ); }
	}

	public class Aircraft : IMove, IFacing, IOccupySpace
	{
		protected readonly Actor self;
		[Sync]
		public int Facing { get; set; }
		[Sync]
		public int Altitude { get; set; }
		[Sync]
		public int2 SubPxPosition;
		public int2 PxPosition { get { return new int2( SubPxPosition.X / 1024, SubPxPosition.Y / 1024 ); } }
		public int2 TopLeft { get { return Util.CellContaining( PxPosition ); } }

		readonly AircraftInfo Info;

		public Aircraft( ActorInitializer init , AircraftInfo info)
		{
			this.self = init.self;
			if( init.Contains<LocationInit>() )
				this.SubPxPosition = 1024 * Util.CenterOfCell( init.Get<LocationInit, int2>() );
			
			this.Facing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : info.InitialFacing;
			this.Altitude = init.Contains<AltitudeInit>() ? init.Get<AltitudeInit,int>() : 0;
			Info = info;
		}

		public int ROT { get { return Info.ROT; } }
		
		public int InitialFacing { get { return Info.InitialFacing; } }

		public void SetPosition(Actor self, int2 cell)
		{
			SetPxPosition( self, Util.CenterOfCell( cell ) );
		}

		public void SetPxPosition( Actor self, int2 px )
		{
			SubPxPosition = px * 1024;
		}

		public void AdjustPxPosition(Actor self, int2 px) { SetPxPosition(self, px); }

		public bool AircraftCanEnter(Actor a)
		{
			if( self.Owner != a.Owner ) return false;
			return Info.RearmBuildings.Contains( a.Info.Name )
				|| Info.RepairBuildings.Contains( a.Info.Name );
		}

		public bool CanEnterCell(int2 location) { return true; }

		public int MovementSpeed
		{
			get
			{
				decimal ret = Info.Speed;
				foreach( var t in self.TraitsImplementing<ISpeedModifier>() )
					ret *= t.GetSpeedModifier();
				return (int)ret;
			}
		}
		
		int2[] noCells = new int2[] { };
		public IEnumerable<int2> OccupiedCells() { return noCells; }

		public void TickMove( int speed, int facing )
		{
			var rawspeed = speed * 7 / (32 * 1024);
			SubPxPosition += rawspeed * -SubPxVector[facing];
		}

		public bool CanLand(int2 cell)
		{
			if (!self.World.Map.IsInMap(cell))
				return false;

			if (self.World.WorldActor.Trait<UnitInfluence>().AnyUnitsAt(cell))
				return false;

			var type = self.World.GetTerrainType(cell);
			return Info.LandableTerrainTypes.Contains(type);
		}
		
		public static readonly int2[] SubPxVector = 
		{
			new int2( 0, 1024 ),
			new int2( 25, 1023 ),
			new int2( 50, 1022 ),
			new int2( 75, 1021 ),
			new int2( 100, 1019 ),
			new int2( 125, 1016 ),
			new int2( 150, 1012 ),
			new int2( 175, 1008 ),
			new int2( 199, 1004 ),
			new int2( 224, 999 ),
			new int2( 248, 993 ),
			new int2( 273, 986 ),
			new int2( 297, 979 ),
			new int2( 321, 972 ),
			new int2( 344, 964 ),
			new int2( 368, 955 ),
			new int2( 391, 946 ),
			new int2( 414, 936 ),
			new int2( 437, 925 ),
			new int2( 460, 914 ),
			new int2( 482, 903 ),
			new int2( 504, 890 ),
			new int2( 526, 878 ),
			new int2( 547, 865 ),
			new int2( 568, 851 ),
			new int2( 589, 837 ),
			new int2( 609, 822 ),
			new int2( 629, 807 ),
			new int2( 649, 791 ),
			new int2( 668, 775 ),
			new int2( 687, 758 ),
			new int2( 706, 741 ),
			new int2( 724, 724 ),
			new int2( 741, 706 ),
			new int2( 758, 687 ),
			new int2( 775, 668 ),
			new int2( 791, 649 ),
			new int2( 807, 629 ),
			new int2( 822, 609 ),
			new int2( 837, 589 ),
			new int2( 851, 568 ),
			new int2( 865, 547 ),
			new int2( 878, 526 ),
			new int2( 890, 504 ),
			new int2( 903, 482 ),
			new int2( 914, 460 ),
			new int2( 925, 437 ),
			new int2( 936, 414 ),
			new int2( 946, 391 ),
			new int2( 955, 368 ),
			new int2( 964, 344 ),
			new int2( 972, 321 ),
			new int2( 979, 297 ),
			new int2( 986, 273 ),
			new int2( 993, 248 ),
			new int2( 999, 224 ),
			new int2( 1004, 199 ),
			new int2( 1008, 175 ),
			new int2( 1012, 150 ),
			new int2( 1016, 125 ),
			new int2( 1019, 100 ),
			new int2( 1021, 75 ),
			new int2( 1022, 50 ),
			new int2( 1023, 25 ),
			new int2( 1024, 0 ),
			new int2( 1023, -25 ),
			new int2( 1022, -50 ),
			new int2( 1021, -75 ),
			new int2( 1019, -100 ),
			new int2( 1016, -125 ),
			new int2( 1012, -150 ),
			new int2( 1008, -175 ),
			new int2( 1004, -199 ),
			new int2( 999, -224 ),
			new int2( 993, -248 ),
			new int2( 986, -273 ),
			new int2( 979, -297 ),
			new int2( 972, -321 ),
			new int2( 964, -344 ),
			new int2( 955, -368 ),
			new int2( 946, -391 ),
			new int2( 936, -414 ),
			new int2( 925, -437 ),
			new int2( 914, -460 ),
			new int2( 903, -482 ),
			new int2( 890, -504 ),
			new int2( 878, -526 ),
			new int2( 865, -547 ),
			new int2( 851, -568 ),
			new int2( 837, -589 ),
			new int2( 822, -609 ),
			new int2( 807, -629 ),
			new int2( 791, -649 ),
			new int2( 775, -668 ),
			new int2( 758, -687 ),
			new int2( 741, -706 ),
			new int2( 724, -724 ),
			new int2( 706, -741 ),
			new int2( 687, -758 ),
			new int2( 668, -775 ),
			new int2( 649, -791 ),
			new int2( 629, -807 ),
			new int2( 609, -822 ),
			new int2( 589, -837 ),
			new int2( 568, -851 ),
			new int2( 547, -865 ),
			new int2( 526, -878 ),
			new int2( 504, -890 ),
			new int2( 482, -903 ),
			new int2( 460, -914 ),
			new int2( 437, -925 ),
			new int2( 414, -936 ),
			new int2( 391, -946 ),
			new int2( 368, -955 ),
			new int2( 344, -964 ),
			new int2( 321, -972 ),
			new int2( 297, -979 ),
			new int2( 273, -986 ),
			new int2( 248, -993 ),
			new int2( 224, -999 ),
			new int2( 199, -1004 ),
			new int2( 175, -1008 ),
			new int2( 150, -1012 ),
			new int2( 125, -1016 ),
			new int2( 100, -1019 ),
			new int2( 75, -1021 ),
			new int2( 50, -1022 ),
			new int2( 25, -1023 ),
			new int2( 0, -1024 ),
			new int2( -25, -1023 ),
			new int2( -50, -1022 ),
			new int2( -75, -1021 ),
			new int2( -100, -1019 ),
			new int2( -125, -1016 ),
			new int2( -150, -1012 ),
			new int2( -175, -1008 ),
			new int2( -199, -1004 ),
			new int2( -224, -999 ),
			new int2( -248, -993 ),
			new int2( -273, -986 ),
			new int2( -297, -979 ),
			new int2( -321, -972 ),
			new int2( -344, -964 ),
			new int2( -368, -955 ),
			new int2( -391, -946 ),
			new int2( -414, -936 ),
			new int2( -437, -925 ),
			new int2( -460, -914 ),
			new int2( -482, -903 ),
			new int2( -504, -890 ),
			new int2( -526, -878 ),
			new int2( -547, -865 ),
			new int2( -568, -851 ),
			new int2( -589, -837 ),
			new int2( -609, -822 ),
			new int2( -629, -807 ),
			new int2( -649, -791 ),
			new int2( -668, -775 ),
			new int2( -687, -758 ),
			new int2( -706, -741 ),
			new int2( -724, -724 ),
			new int2( -741, -706 ),
			new int2( -758, -687 ),
			new int2( -775, -668 ),
			new int2( -791, -649 ),
			new int2( -807, -629 ),
			new int2( -822, -609 ),
			new int2( -837, -589 ),
			new int2( -851, -568 ),
			new int2( -865, -547 ),
			new int2( -878, -526 ),
			new int2( -890, -504 ),
			new int2( -903, -482 ),
			new int2( -914, -460 ),
			new int2( -925, -437 ),
			new int2( -936, -414 ),
			new int2( -946, -391 ),
			new int2( -955, -368 ),
			new int2( -964, -344 ),
			new int2( -972, -321 ),
			new int2( -979, -297 ),
			new int2( -986, -273 ),
			new int2( -993, -248 ),
			new int2( -999, -224 ),
			new int2( -1004, -199 ),
			new int2( -1008, -175 ),
			new int2( -1012, -150 ),
			new int2( -1016, -125 ),
			new int2( -1019, -100 ),
			new int2( -1021, -75 ),
			new int2( -1022, -50 ),
			new int2( -1023, -25 ),
			new int2( -1024, 0 ),
			new int2( -1023, 25 ),
			new int2( -1022, 50 ),
			new int2( -1021, 75 ),
			new int2( -1019, 100 ),
			new int2( -1016, 125 ),
			new int2( -1012, 150 ),
			new int2( -1008, 175 ),
			new int2( -1004, 199 ),
			new int2( -999, 224 ),
			new int2( -993, 248 ),
			new int2( -986, 273 ),
			new int2( -979, 297 ),
			new int2( -972, 321 ),
			new int2( -964, 344 ),
			new int2( -955, 368 ),
			new int2( -946, 391 ),
			new int2( -936, 414 ),
			new int2( -925, 437 ),
			new int2( -914, 460 ),
			new int2( -903, 482 ),
			new int2( -890, 504 ),
			new int2( -878, 526 ),
			new int2( -865, 547 ),
			new int2( -851, 568 ),
			new int2( -837, 589 ),
			new int2( -822, 609 ),
			new int2( -807, 629 ),
			new int2( -791, 649 ),
			new int2( -775, 668 ),
			new int2( -758, 687 ),
			new int2( -741, 706 ),
			new int2( -724, 724 ),
			new int2( -706, 741 ),
			new int2( -687, 758 ),
			new int2( -668, 775 ),
			new int2( -649, 791 ),
			new int2( -629, 807 ),
			new int2( -609, 822 ),
			new int2( -589, 837 ),
			new int2( -568, 851 ),
			new int2( -547, 865 ),
			new int2( -526, 878 ),
			new int2( -504, 890 ),
			new int2( -482, 903 ),
			new int2( -460, 914 ),
			new int2( -437, 925 ),
			new int2( -414, 936 ),
			new int2( -391, 946 ),
			new int2( -368, 955 ),
			new int2( -344, 964 ),
			new int2( -321, 972 ),
			new int2( -297, 979 ),
			new int2( -273, 986 ),
			new int2( -248, 993 ),
			new int2( -224, 999 ),
			new int2( -199, 1004 ),
			new int2( -175, 1008 ),
			new int2( -150, 1012 ),
			new int2( -125, 1016 ),
			new int2( -100, 1019 ),
			new int2( -75, 1021 ),
			new int2( -50, 1022 ),
			new int2( -25, 1023 )
		};
	}
}
