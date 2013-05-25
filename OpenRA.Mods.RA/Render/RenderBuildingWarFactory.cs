﻿#region Copyright & License Information
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
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingWarFactoryInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingWarFactory( init, this ); }

		/* get around unverifiability */
		IEnumerable<IRenderable> BaseBuildingPreview(ActorInfo building, PaletteReference pr)
		{
			return base.RenderPreview(building, pr);
		}

		public override IEnumerable<IRenderable> RenderPreview(ActorInfo building, PaletteReference pr)
		{
			var p = BaseBuildingPreview(building, pr);
			foreach (var r in p)
				yield return r;

			var anim = new Animation(RenderSimple.GetImage(building), () => 0);
			anim.PlayRepeating("idle-top");
			yield return new SpriteRenderable(anim.Image, WPos.Zero + Origin, 0, pr, 1f);
		}
	}

	class RenderBuildingWarFactory : RenderBuilding, INotifyBuildComplete, ITick, INotifyProduction, INotifySold, ISync
	{
		Animation roof;
		[Sync] bool isOpen;
		[Sync] CPos openExit;
		bool buildComplete;

		public RenderBuildingWarFactory(ActorInitializer init, RenderBuildingInfo info)
			: base(init, info)
		{
			roof = new Animation(GetImage(init.self));

			var bi = init.self.Info.Traits.Get<BuildingInfo>();
			anims.Add("roof", new AnimationWithOffset(roof, null,
				() => !buildComplete, FootprintUtils.CenterOffset(bi).Y));
		}

		public void BuildingComplete( Actor self )
		{
			roof.Play(NormalizeSequence(self,
				self.GetDamageState() > DamageState.Heavy ? "damaged-idle-top" : "idle-top"));
			buildComplete = true;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
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

		public void Selling(Actor self) { anims.Remove("roof"); }
		public void Sold(Actor self) { }
	}
}
