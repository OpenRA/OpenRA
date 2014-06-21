#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class WithProductionOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "production-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithProductionOverlay(init.self, this); }
	}

	public class WithProductionOverlay : INotifyDamageStateChanged, ITick, INotifyBuildComplete, INotifySold
	{
		Animation overlay;
		ProductionQueue queue;
		bool buildComplete;

		bool IsProducing
		{
			get { return queue != null && queue.CurrentItem() != null && !queue.CurrentPaused; }
		}

		public WithProductionOverlay(Actor self, WithProductionOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();

			buildComplete = !self.HasTrait<Building>(); // always render instantly for units

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.PlayRepeating(info.Sequence);
			rs.Add("production_overlay_{0}".F(info.Sequence),
				new AnimationWithOffset(overlay,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => !IsProducing || !buildComplete),
				info.Palette, info.IsPlayerPalette);
		}

		public void Tick(Actor self)
		{
			// search for the queue here once so we don't rely on order of trait initialization
			if (queue == null)
			{
				var production = self.TraitOrDefault<Production>();

				var perBuildingQueues = self.TraitsImplementing<ProductionQueue>();
				queue = perBuildingQueues.FirstOrDefault(q => q.Enabled && production.Info.Produces.Contains(q.Info.Type));

				if (queue == null)
				{
					var perPlayerQueues = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>();
					queue = perPlayerQueues.FirstOrDefault(q => q.Enabled && production.Info.Produces.Contains(q.Info.Type));
				}

				if (queue == null)
					throw new InvalidOperationException("Can't find production queues.");
			}
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}