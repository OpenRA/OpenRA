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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingWarFactoryInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingWarFactory( init, this ); }

		/* get around unverifiability */
		IEnumerable<IRenderable> BaseBuildingPreview(World world, ActorInfo building, PaletteReference pr)
		{
			return base.RenderPreview(world, building, pr);
		}

		public override IEnumerable<IRenderable> RenderPreview(World world, ActorInfo building, PaletteReference pr)
		{
			var p = BaseBuildingPreview(world, building, pr);
			var anim = new Animation(world, RenderSprites.GetImage(building), () => 0);
			anim.PlayRepeating("idle-top");

			return p.Concat(anim.Render(WPos.Zero, WVec.Zero, 0, pr, Scale));
		}
	}

	class RenderBuildingWarFactory : RenderBuilding, INotifyBuildComplete, ITickRender, INotifyProduction, INotifySold, ISync
	{
		Animation roof;
		[Sync] bool isOpen;
		[Sync] CPos openExit;
		bool buildComplete;

		public RenderBuildingWarFactory(ActorInitializer init, RenderBuildingInfo info)
			: base(init, info)
		{
			roof = new Animation(init.world, GetImage(init.self));
			var bi = init.self.Info.Traits.Get<BuildingInfo>();

			// Additional 512 units move from center -> top of cell
			var offset = FootprintUtils.CenterOffset(init.world, bi).Y + 512;
			Add("roof", new AnimationWithOffset(roof, null,
				() => !buildComplete, offset));
		}

		public void BuildingComplete( Actor self )
		{
			roof.Play(NormalizeSequence(self,
				self.GetDamageState() > DamageState.Heavy ? "damaged-idle-top" : "idle-top"));
			buildComplete = true;
		}

		public override void TickRender(WorldRenderer wr, Actor self)
		{
			base.TickRender(wr, self);
			if (isOpen && !self.World.ActorMap.GetUnitsAt(openExit).Any( a => a != self ))
			{
				isOpen = false;
				roof.PlayBackwardsThen(NormalizeSequence(self, "build-top"),
					() => roof.Play(NormalizeSequence(self, "idle-top")));
			}
		}

		public override void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (roof.CurrentSequence != null)
			{
				if (e.DamageState >= DamageState.Heavy)
					roof.ReplaceAnim("damaged-" + roof.CurrentSequence.Name);
				else
					roof.ReplaceAnim(roof.CurrentSequence.Name.Replace("damaged-", ""));
			}

			base.DamageStateChanged(self, e);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			roof.PlayThen(NormalizeSequence(self, "build-top"), () => { isOpen = true; openExit = exit; });
		}

		public void Selling(Actor self) { Remove("roof"); }
		public void Sold(Actor self) { }
	}
}
