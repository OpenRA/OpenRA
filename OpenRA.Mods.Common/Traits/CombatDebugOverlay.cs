#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays fireports, muzzle offsets, and hit areas in developer mode.")]
	public class CombatDebugOverlayInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new CombatDebugOverlay(init.Self); }
	}

	public class CombatDebugOverlay : IPostRender, INotifyDamage, INotifyCreated
	{
		readonly DeveloperMode devMode;
		readonly HealthInfo healthInfo;
		readonly Lazy<BodyOrientation> coords;

		IBlocksProjectiles[] allBlockers;

		public CombatDebugOverlay(Actor self)
		{
			healthInfo = self.Info.TraitInfoOrDefault<HealthInfo>();
			coords = Exts.Lazy(self.Trait<BodyOrientation>);

			var localPlayer = self.World.LocalPlayer;
			devMode = localPlayer != null ? localPlayer.PlayerActor.Trait<DeveloperMode>() : null;
		}

		public void Created(Actor self)
		{
			allBlockers = self.TraitsImplementing<IBlocksProjectiles>().ToArray();
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (devMode == null || !devMode.ShowCombatGeometry)
				return;

			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			var iz = 1 / wr.Viewport.Zoom;

			if (healthInfo != null)
				healthInfo.Shape.DrawCombatOverlay(wr, wcr, self);

			var blockers = allBlockers.Where(Exts.IsTraitEnabled).ToList();
			if (blockers.Count > 0)
			{
				var hc = Color.Orange;
				var height = new WVec(0, 0, blockers.Max(b => b.BlockingHeight.Length));
				var ha = wr.ScreenPosition(self.CenterPosition);
				var hb = wr.ScreenPosition(self.CenterPosition + height);
				wcr.DrawLine(ha, hb, iz, hc);
				TargetLineRenderable.DrawTargetMarker(wr, hc, ha);
				TargetLineRenderable.DrawTargetMarker(wr, hc, hb);
			}

			foreach (var attack in self.TraitsImplementing<AttackBase>().Where(x => !x.IsTraitDisabled))
				DrawArmaments(self, attack, wr, wcr, iz);
		}

		void DrawArmaments(Actor self, AttackBase attack, WorldRenderer wr, RgbaColorRenderer wcr, float iz)
		{
			var c = Color.White;

			// Fire ports on garrisonable structures
			var garrison = attack as AttackGarrisoned;
			if (garrison != null)
			{
				var bodyOrientation = coords.Value.QuantizeOrientation(self, self.Orientation);
				foreach (var p in garrison.Info.Ports)
				{
					var pos = self.CenterPosition + coords.Value.LocalToWorld(p.Offset.Rotate(bodyOrientation));
					var da = coords.Value.LocalToWorld(new WVec(224, 0, 0).Rotate(WRot.FromYaw(p.Yaw + p.Cone)).Rotate(bodyOrientation));
					var db = coords.Value.LocalToWorld(new WVec(224, 0, 0).Rotate(WRot.FromYaw(p.Yaw - p.Cone)).Rotate(bodyOrientation));

					var o = wr.ScreenPosition(pos);
					var a = wr.ScreenPosition(pos + da * 224 / da.Length);
					var b = wr.ScreenPosition(pos + db * 224 / db.Length);
					wcr.DrawLine(o, a, iz, c);
					wcr.DrawLine(o, b, iz, c);
				}

				return;
			}

			foreach (var a in attack.Armaments)
			{
				foreach (var b in a.Barrels)
				{
					var muzzle = self.CenterPosition + a.MuzzleOffset(self, b);
					var dirOffset = new WVec(0, -224, 0).Rotate(a.MuzzleOrientation(self, b));

					var sm = wr.ScreenPosition(muzzle);
					var sd = wr.ScreenPosition(muzzle + dirOffset);
					wcr.DrawLine(sm, sd, iz, c);
					TargetLineRenderable.DrawTargetMarker(wr, c, sm);
				}
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (devMode == null || !devMode.ShowCombatGeometry || e.Damage.Value == 0)
				return;

			if (healthInfo == null)
				return;

			var maxHP = healthInfo.HP > 0 ? healthInfo.HP : 1;
			var damageText = "{0} ({1}%)".F(-e.Damage.Value, e.Damage.Value * 100 / maxHP);

			self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, e.Attacker.Owner.Color.RGB, damageText, 30)));
		}
	}
}
