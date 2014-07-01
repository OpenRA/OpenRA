#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Displays an overlay when the building is being repaired by the player.")]
	public class WithRepairOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public readonly bool PauseOnLowPower = false;

		public object Create(ActorInitializer init) { return new WithRepairOverlay(init.self, this); }
	}

	public class WithRepairOverlay : INotifyDamageStateChanged, INotifyBuildComplete, INotifySold, INotifyRepair
	{
		Animation overlay;
		bool buildComplete;

		public WithRepairOverlay(Actor self, WithRepairOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();
			var disabled = self.TraitsImplementing<IDisable>();

			buildComplete = !self.HasTrait<Building>(); // always render instantly for units
			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);
			rs.Add("repair_{0}".F(info.Sequence),
				new AnimationWithOffset(overlay,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => !buildComplete,
					() => info.PauseOnLowPower && disabled.Any(d => d.Disabled),
					p => WithTurret.ZOffsetFromCenter(self, p, 1)),
				info.Palette, info.IsPlayerPalette);
		}

		public void BuildingComplete(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(120, () =>
				buildComplete = true)));
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

		public void Repairing(Actor self, Actor host)
		{
			overlay.Play(overlay.CurrentSequence.Name);
		}
	}
}