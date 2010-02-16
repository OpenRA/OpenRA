#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class RenderWarFactoryInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public object Create(Actor self) { return new RenderWarFactory(self); }
	}

	class RenderWarFactory : INotifyBuildComplete, INotifyDamage, ITick, INotifyProduction, INotifySold
	{
		public Animation roof;
		[Sync]
		bool isOpen;

		string GetPrefix(Actor self)
		{
			return self.GetDamageState() == DamageState.Half ? "damaged-" : "";
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
			var b = self.GetBounds(false);
			if (isOpen && !self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(((1/24f) * self.CenterLocation).ToInt2()).Any())
			{
				isOpen = false;
				roof.PlayBackwardsThen(GetPrefix(self) + "build-top", () => roof.Play(GetPrefix(self) + "idle-top"));
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;
			switch (e.DamageState)
			{
				case DamageState.Normal:
					roof.ReplaceAnim(roof.CurrentSequence.Name.Replace("damaged-",""));
					break;
				case DamageState.Half:
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
