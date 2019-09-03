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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays fireports, muzzle offsets, and hit areas in developer mode.")]
	public class CombatDebugOverlayInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new CombatDebugOverlay(init.Self); }
	}

	public class CombatDebugOverlay : IRenderAboveShroud, INotifyDamage, INotifyCreated
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

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (debugVis == null || !debugVis.CombatGeometry || self.World.FogObscures(self))
				yield break;

			var wcr = Game.Renderer.WorldRgbaColorRenderer;
			var iz = 1 / wr.Viewport.Zoom;

			var blockers = allBlockers.Where(Exts.IsTraitEnabled).ToList();
			if (blockers.Count > 0)
			{
				var height = new WVec(0, 0, blockers.Max(b => b.BlockingHeight.Length));
				yield return new LineAnnotationRenderable(self.CenterPosition, self.CenterPosition + height, 1,  Color.Orange);
			}

			var activeShapes = shapes.Where(Exts.IsTraitEnabled);
			foreach (var s in activeShapes)
				foreach (var r in s.Info.Type.RenderDebugOverlay(wr, self))
					yield return r;

			var positions = Target.FromActor(self).Positions;
			foreach (var p in positions)
			{
				yield return new LineAnnotationRenderable(p - TargetPosHLine, p + TargetPosHLine, 1, Color.Lime);
				yield return new LineAnnotationRenderable(p - TargetPosVLine, p + TargetPosVLine, 1, Color.Lime);
			}

			foreach (var attack in self.TraitsImplementing<AttackBase>().Where(x => !x.IsTraitDisabled))
				foreach (var r in RenderArmaments(self, attack, wr, wcr, iz))
					yield return r;
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return true; } }

		IEnumerable<IRenderable> RenderArmaments(Actor self, AttackBase attack, WorldRenderer wr, RgbaColorRenderer wcr, float iz)
		{
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

					yield return new LineAnnotationRenderable(pos, pos + da * 224 / da.Length, 1, Color.White);
					yield return new LineAnnotationRenderable(pos, pos + db * 224 / da.Length, 1, Color.White);
				}

				yield break;
			}

			foreach (var a in attack.Armaments)
			{
				if (a.IsTraitDisabled)
					continue;

				foreach (var b in a.Barrels)
				{
					var muzzle = self.CenterPosition + a.MuzzleOffset(self, b);
					var dirOffset = new WVec(0, -224, 0).Rotate(a.MuzzleOrientation(self, b));
					yield return new LineAnnotationRenderable(muzzle, muzzle + dirOffset, 1, Color.White);
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
