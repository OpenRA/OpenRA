#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
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

	public class CombatDebugOverlay : IPostRender, INotifyDamage
	{
		readonly DeveloperMode devMode;

		readonly HealthInfo healthInfo;
		readonly BlocksProjectilesInfo blockInfo;
		Lazy<AttackBase> attack;
		Lazy<BodyOrientation> coords;

		public CombatDebugOverlay(Actor self)
		{
			healthInfo = self.Info.TraitInfoOrDefault<HealthInfo>();
			blockInfo = self.Info.TraitInfoOrDefault<BlocksProjectilesInfo>();
			attack = Exts.Lazy(() => self.TraitOrDefault<AttackBase>());
			coords = Exts.Lazy(() => self.Trait<BodyOrientation>());

			var localPlayer = self.World.LocalPlayer;
			devMode = localPlayer != null ? localPlayer.PlayerActor.Trait<DeveloperMode>() : null;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (devMode == null || !devMode.ShowCombatGeometry)
				return;

			if (healthInfo != null)
				wr.DrawRangeCircle(self.CenterPosition, healthInfo.Radius, Color.Red);

			var wlr = Game.Renderer.WorldLineRenderer;

			if (blockInfo != null)
			{
				var hc = Color.Orange;
				var height = new WVec(0, 0, blockInfo.Height.Length);
				var ha = wr.ScreenPosition(self.CenterPosition);
				var hb = wr.ScreenPosition(self.CenterPosition + height);
				wlr.DrawLine(ha, hb, hc);
				wr.DrawTargetMarker(hc, ha);
				wr.DrawTargetMarker(hc, hb);
			}

			// No armaments to draw
			if (attack.Value == null)
				return;

			var c = Color.White;

			// Fire ports on garrisonable structures
			var garrison = attack.Value as AttackGarrisoned;
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
					wlr.DrawLine(o, a, c);
					wlr.DrawLine(o, b, c);
				}

				return;
			}

			foreach (var a in attack.Value.Armaments)
			{
				foreach (var b in a.Barrels)
				{
					var muzzle = self.CenterPosition + a.MuzzleOffset(self, b);
					var dirOffset = new WVec(0, -224, 0).Rotate(a.MuzzleOrientation(self, b));

					var sm = wr.ScreenPosition(muzzle);
					var sd = wr.ScreenPosition(muzzle + dirOffset);
					wlr.DrawLine(sm, sd, c);
					wr.DrawTargetMarker(c, sm);
				}
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (devMode == null || !devMode.ShowCombatGeometry || e.Damage == 0)
				return;

			if (healthInfo == null)
				return;

			var maxHP = healthInfo.HP > 0 ? healthInfo.HP : 1;
			var damageText = "{0} ({1}%)".F(-e.Damage, e.Damage * 100 / maxHP);

			self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, e.Attacker.Owner.Color.RGB, damageText, 30)));
		}
	}
}
