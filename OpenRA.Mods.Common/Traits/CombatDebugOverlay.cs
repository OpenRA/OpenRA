#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays fireports, muzzle offsets, and hit areas in developer mode.")]
	public class CombatDebugOverlayInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new CombatDebugOverlay(init.Self); }
	}

	public class CombatDebugOverlay : IRenderAboveWorld, INotifyDamage, INotifyCreated
	{
		static readonly WVec TargetPosHLine = new WVec(0, 128, 0);
		static readonly WVec TargetPosVLine = new WVec(128, 0, 0);

		readonly DebugVisualizations debugVis;
		readonly IHealthInfo healthInfo;
		readonly Lazy<BodyOrientation> coords;

		HitShape[] shapes;
		IBlocksProjectiles[] allBlockers;

		public CombatDebugOverlay(Actor self)
		{
			healthInfo = self.Info.TraitInfoOrDefault<IHealthInfo>();
			coords = Exts.Lazy(self.Trait<BodyOrientation>);

			debugVis = self.World.WorldActor.TraitOrDefault<DebugVisualizations>();
		}

		void INotifyCreated.Created(Actor self)
		{
			shapes = self.TraitsImplementing<HitShape>().ToArray();
			allBlockers = self.TraitsImplementing<IBlocksProjectiles>().ToArray();
		}

		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			if (debugVis == null || !debugVis.CombatGeometry)
				return;

			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			var iz = 1 / wr.Viewport.Zoom;

			var blockers = allBlockers.Where(Exts.IsTraitEnabled).ToList();
			if (blockers.Count > 0)
			{
				var hc = Color.Orange;
				var height = new WVec(0, 0, blockers.Max(b => b.BlockingHeight.Length));
				var ha = wr.Screen3DPosition(self.CenterPosition);
				var hb = wr.Screen3DPosition(self.CenterPosition + height);
				wcr.DrawLine(ha, hb, iz, hc);
				TargetLineRenderable.DrawTargetMarker(wr, hc, ha);
				TargetLineRenderable.DrawTargetMarker(wr, hc, hb);
			}

			var activeShapes = shapes.Where(Exts.IsTraitEnabled);
			foreach (var s in activeShapes)
				s.Info.Type.DrawCombatOverlay(wr, wcr, self);

			var tc = Color.Lime;
			var positions = Target.FromActor(self).Positions;
			foreach (var p in positions)
			{
				var center = wr.Screen3DPosition(p);
				TargetLineRenderable.DrawTargetMarker(wr, tc, center);
				wcr.DrawLine(wr.Screen3DPosition(p - TargetPosHLine), wr.Screen3DPosition(p + TargetPosHLine), iz, tc);
				wcr.DrawLine(wr.Screen3DPosition(p - TargetPosVLine), wr.Screen3DPosition(p + TargetPosVLine), iz, tc);
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

					var o = wr.Screen3DPosition(pos);
					var a = wr.Screen3DPosition(pos + da * 224 / da.Length);
					var b = wr.Screen3DPosition(pos + db * 224 / db.Length);
					wcr.DrawLine(o, a, iz, c);
					wcr.DrawLine(o, b, iz, c);
				}

				return;
			}

			foreach (var a in attack.Armaments)
			{
				if (a.IsTraitDisabled)
					continue;

				foreach (var b in a.Barrels)
				{
					var muzzle = self.CenterPosition + a.MuzzleOffset(self, b);
					var dirOffset = new WVec(0, -224, 0).Rotate(a.MuzzleOrientation(self, b));

					var sm = wr.Screen3DPosition(muzzle);
					var sd = wr.Screen3DPosition(muzzle + dirOffset);
					wcr.DrawLine(sm, sd, iz, c);
					TargetLineRenderable.DrawTargetMarker(wr, c, sm);
				}
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (debugVis == null || !debugVis.CombatGeometry || e.Damage.Value == 0)
				return;

			if (healthInfo == null)
				return;

			var maxHP = healthInfo.MaxHP > 0 ? healthInfo.MaxHP : 1;
			var damageText = "{0} ({1}%)".F(-e.Damage.Value, e.Damage.Value * 100 / maxHP);

			self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, e.Attacker.Owner.Color, damageText, 30)));
		}
	}
}
