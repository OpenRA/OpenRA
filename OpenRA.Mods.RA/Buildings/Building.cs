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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class BuildingInfo : ITraitInfo
	{
		public readonly int Power = 0;
		public readonly bool BaseNormal = true;
		public readonly bool WaterBound = false;
		public readonly int Adjacent = 2;
		public readonly bool Capturable = false;
		public readonly string Footprint = "x";
		public readonly int2 Dimensions = new int2(1, 1);
		public readonly bool Unsellable = false;
		public readonly float RefundPercent = 0.5f;
		
		public readonly string[] BuildSounds = {"placbldg.aud", "build5.aud"};
		public readonly string[] SellSounds = {"cashturn.aud"};
		public readonly string DamagedSound = "kaboom1.aud";
		public readonly string DestroyedSound = "kaboom22.aud";

		public object Create(ActorInitializer init) { return new Building(init); }

		public bool IsCloseEnoughToBase(World world, Player p, string buildingName, int2 topLeft)
		{
			var buildingMaxBounds = Dimensions;
			if( Rules.Info[ buildingName ].Traits.Contains<BibInfo>() )
				buildingMaxBounds.Y += 1;

			var scanStart = world.ClampToWorld( topLeft - new int2( Adjacent, Adjacent ) );
			var scanEnd = world.ClampToWorld( topLeft + buildingMaxBounds + new int2( Adjacent, Adjacent ) );

			var nearnessCandidates = new List<int2>();

			for( int y = scanStart.Y ; y < scanEnd.Y ; y++ )
			{
				for( int x = scanStart.X ; x < scanEnd.X ; x++ )
				{
					var at = world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt( new int2( x, y ) );
					if( at != null && at.Owner.Stances[ p ] == Stance.Ally && at.Info.Traits.Get<BuildingInfo>().BaseNormal )
						nearnessCandidates.Add( new int2( x, y ) );
				}
			}
			var buildingTiles = FootprintUtils.Tiles( buildingName, this, topLeft ).ToList();
			return nearnessCandidates
				.Any( a => buildingTiles
					.Any( b => Math.Abs( a.X - b.X ) <= Adjacent
							&& Math.Abs( a.Y - b.Y ) <= Adjacent ) );
		}

		public void DrawBuildingGrid( WorldRenderer wr, World world, string name )
		{
			var position = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
			var topLeft = position - FootprintUtils.AdjustForBuildingSize( this );

			var cells = new Dictionary<int2, bool>();
			// Linebuild for walls.
			// Assumes a 1x1 footprint; weird things will happen for other footprints
			if (Rules.Info[name].Traits.Contains<LineBuildInfo>())
			{
				foreach( var t in BuildingUtils.GetLineBuildCells( world, topLeft, name, this ) )
					cells.Add( t, IsCloseEnoughToBase( world, world.LocalPlayer, name, t ) );
			}
			else
			{
				var res = world.WorldActor.Trait<ResourceLayer>();
				var isCloseEnough = IsCloseEnoughToBase(world, world.LocalPlayer, name, topLeft);
				foreach (var t in FootprintUtils.Tiles(name, this, topLeft))
					cells.Add( t, isCloseEnough && world.IsCellBuildable(t, WaterBound) && res.GetResource(t) == null );
			}
			wr.uiOverlay.DrawGrid( wr, cells );
		}
	}

	public class Building : INotifyDamage, IResolveOrder, IOccupySpace
	{
		readonly Actor self;
		public readonly BuildingInfo Info;
		[Sync]
		readonly int2 topLeft;

		readonly PowerManager PlayerPower;

		public int2 PxPosition { get { return ( 2 * topLeft + Info.Dimensions ) * Game.CellSize / 2; } }

		public Building(ActorInitializer init)
		{
			this.self = init.self;
			this.topLeft = init.Get<LocationInit,int2>();
			this.Info = self.Info.Traits.Get<BuildingInfo>();
			this.PlayerPower = init.self.Owner.PlayerActor.Trait<PowerManager>();
		}
		
		public int GetPowerUsage()
		{
			if (Info.Power <= 0)
				return Info.Power;
			
			var health = self.TraitOrDefault<Health>();
			return health != null ? (Info.Power * health.HP / health.MaxHP) : Info.Power;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			// Power plants lose power with damage
			if (Info.Power > 0)
				PlayerPower.UpdateActor(self, GetPowerUsage());
			
			if (e.DamageState == DamageState.Dead)
			{
				self.World.WorldActor.Trait<ScreenShaker>().AddEffect(10, self.CenterLocation, 1);
				Sound.Play(Info.DestroyedSound, self.CenterLocation);
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Sell")
			{
				self.CancelActivity();
				self.QueueActivity(new Sell());
			}
		}
		
		public int2 TopLeft
		{
			get { return topLeft; }
		}

		public IEnumerable<int2> OccupiedCells()
		{
			return FootprintUtils.UnpathableTiles( self.Info.Name, Info, TopLeft );
		}
	}
}
