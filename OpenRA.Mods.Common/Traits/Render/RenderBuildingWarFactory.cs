#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RenderBuildingWarFactoryInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingWarFactory(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			foreach (var orig in base.RenderPreviewSprites(init, rs, image, facings, p))
				yield return orig;

			// Show additional roof overlay
			var anim = new Animation(init.World, image, () => 0);
			anim.PlayRepeating("idle-top");

			var bi = init.Actor.Traits.Get<BuildingInfo>();
			var offset = FootprintUtils.CenterOffset(init.World, bi).Y + 512;
			yield return new SpriteActorPreview(anim, WVec.Zero, offset, p, rs.Scale);
		}
	}

	class RenderBuildingWarFactory : RenderBuilding, INotifyBuildComplete, ITick, INotifyProduction, INotifySold
	{
		readonly Animation roof;
		bool isOpen;
		CPos openExit;
		bool buildComplete;

		public RenderBuildingWarFactory(ActorInitializer init, RenderBuildingInfo info)
			: base(init, info)
		{
			roof = new Animation(init.World, GetImage(init.Self));
			var bi = init.Self.Info.Traits.Get<BuildingInfo>();

			// Additional 512 units move from center -> top of cell
			var offset = FootprintUtils.CenterOffset(init.World, bi).Y + 512;
			Add("roof", new AnimationWithOffset(roof, null,
				() => !buildComplete, offset));
		}

		public override void BuildingComplete(Actor self)
		{
			roof.Play(NormalizeSequence(self,
				self.GetDamageState() > DamageState.Heavy ? "damaged-idle-top" : "idle-top"));
			buildComplete = true;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			if (isOpen && !self.World.ActorMap.GetUnitsAt(openExit).Any(a => a != self))
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
