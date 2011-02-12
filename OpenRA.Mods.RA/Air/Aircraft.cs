#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Air
{
	public class DebugAircraftFacingInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftFacing(init.self); }
	}
	public class DebugAircraftFacing : ISync
	{
		readonly Actor self;
		public DebugAircraftFacing(Actor self){this.self = self;}
		[Sync] public int foo { get { return self.Trait<Aircraft>().Facing; } }
	}
	
	public class DebugAircraftSubPxXInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftSubPxX(init.self); }
	}
	public class DebugAircraftSubPxX : ISync
	{
		readonly Actor self;
		public DebugAircraftSubPxX(Actor self){this.self = self;}
		[Sync] public int foo { get { return self.Trait<Aircraft>().SubPxPosition.X; } }
	}
	
	public class DebugAircraftSubPxYInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftSubPxY(init.self); }
	}
	public class DebugAircraftSubPxY : ISync
	{
		readonly Actor self;
		public DebugAircraftSubPxY(Actor self){this.self = self;}
		[Sync] public int foo { get { return self.Trait<Aircraft>().SubPxPosition.Y; } }
	}
	
	public class DebugAircraftAltitudeInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugAircraftAltitude(init.self); }
	}
	public class DebugAircraftAltitude : ISync
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

	public class Aircraft : IMove, IFacing, IOccupySpace, ISync
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
		
		Pair<int2, SubCell>[] noCells = new Pair<int2, SubCell>[] { };
		public IEnumerable<Pair<int2, SubCell>> OccupiedCells() { return noCells; }

		public void TickMove( int speed, int facing )
		{
			var rawspeed = speed * 7 / (32 * 1024);
			SubPxPosition += rawspeed * -Util.SubPxVector[facing];
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
	}
}
