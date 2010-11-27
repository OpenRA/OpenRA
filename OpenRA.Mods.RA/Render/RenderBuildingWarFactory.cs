#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderWarFactoryInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderWarFactory(init); }
		
		public override IEnumerable<Renderable> BuildingPreview(ActorInfo building, string Tileset)
		{
			foreach (var r in base.BuildingPreview(building, Tileset))
				yield return r;

			var anim = new Animation(RenderSimple.GetImage(building, Tileset), () => 0);
			anim.PlayRepeating("idle-top");
			var rb = building.Traits.Get<RenderBuildingInfo>();
			yield return new Renderable(anim.Image, rb.Origin + 0.5f*anim.Image.size*(1 - Scale), rb.Palette, 0, Scale);
		}
	}

	class RenderWarFactory : RenderBuilding, INotifyBuildComplete, INotifyDamage, ITick, INotifyProduction, INotifySold
	{
		public Animation roof;
		[Sync]
		bool isOpen;
		[Sync]
		int2 openExit;

		public RenderWarFactory(ActorInitializer init)
			: base(init)
		{
			roof = new Animation(GetImage(init.self));
		}

		public void BuildingComplete( Actor self )
		{
			roof.Play( NormalizeSequence(self, "idle-top") );
			self.Trait<RenderSimple>().anims.Add( "roof", new RenderSimple.AnimationWithOffset( roof ) { ZOffset = 24 } );
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (isOpen && !self.World.WorldActor.Trait<UnitInfluence>()
				.GetUnitsAt(openExit).Any( a => a != self ))
			{
				isOpen = false;
				roof.PlayBackwardsThen(NormalizeSequence(self, "build-top"), () => roof.Play(NormalizeSequence(self, "idle-top")));
			}
		}

		public override void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;
			
			if (e.DamageState >= DamageState.Heavy)
				roof.ReplaceAnim("damaged-" + roof.CurrentSequence.Name);
			else
				roof.ReplaceAnim(roof.CurrentSequence.Name.Replace("damaged-",""));
		}

		public void UnitProduced(Actor self, Actor other, int2 exit)
		{
			roof.PlayThen(NormalizeSequence(self, "build-top"), () => { isOpen = true; openExit = exit; });
		}

		public override void Selling( Actor self )
		{
			self.Trait<RenderSimple>().anims.Remove( "roof" );
			base.Selling(self);
		}
	}
}
