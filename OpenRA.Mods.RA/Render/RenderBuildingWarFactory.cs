#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderWarFactoryInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public object Create(ActorInitializer init) { return new RenderWarFactory(init.self); }
	}

	class RenderWarFactory : INotifyBuildComplete, INotifyDamage, ITick, INotifyProduction, INotifySold
	{
		public Animation roof;
		[Sync]
		bool isOpen;

		string GetPrefix(Actor self)
		{
			return self.GetExtendedDamageState() <= ExtendedDamageState.Half ? "damaged-" : "";
		}

		public RenderWarFactory(Actor self)
		{
			roof = new Animation(self.traits.Get<RenderSimple>().GetImage(self));
		}

		public void BuildingComplete( Actor self )
		{
			roof.Play( GetPrefix(self) + "idle-top" );
			self.traits.Get<RenderSimple>().anims.Add( "roof", new RenderSimple.AnimationWithOffset( roof ) { ZOffset = 2 } );
		}

		public void Tick(Actor self)
		{
			if (isOpen && !self.World.WorldActor.traits.Get<UnitInfluence>()
				.GetUnitsAt(((1f/Game.CellSize) * self.CenterLocation).ToInt2()).Any())
			{
				isOpen = false;
				roof.PlayBackwardsThen(GetPrefix(self) + "build-top", () => roof.Play(GetPrefix(self) + "idle-top"));
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!e.ExtendedDamageStateChanged) return;
			
			switch( e.ExtendedDamageState )
			{
				case ExtendedDamageState.ThreeQuarter: case ExtendedDamageState.Normal: case ExtendedDamageState.Undamaged:
					roof.ReplaceAnim(roof.CurrentSequence.Name.Replace("damaged-",""));
					break;
				case ExtendedDamageState.Half: case ExtendedDamageState.Quarter:
					roof.ReplaceAnim("damaged-" + roof.CurrentSequence.Name);
					break;
			}
		}

		public void UnitProduced(Actor self, Actor other)
		{
			roof.PlayThen(GetPrefix(self) + "build-top", () => isOpen = true);
		}

		public void Selling( Actor self )
		{
			self.traits.Get<RenderSimple>().anims.Remove( "roof" );
		}

		public void Sold( Actor self ) { }
	}
}
