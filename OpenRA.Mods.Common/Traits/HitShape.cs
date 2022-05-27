#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.HitShapes;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Shape of actor for targeting and damage calculations.")]
	public class HitShapeInfo : ConditionalTraitInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Name of turret this shape is linked to. Leave empty to link shape to body.")]
		public readonly string Turret = null;

		[Desc("Create a targetable position for each offset listed here (relative to CenterPosition).")]
		public readonly WVec[] TargetableOffsets = { WVec.Zero };

		[Desc("Create a targetable position at the center of each occupied cell. Stacks with TargetableOffsets.")]
		public readonly bool UseTargetableCellsOffsets = false;

		[Desc("Defines which Armor types apply when the actor receives damage to this HitShape.",
			"If none specified, all armor types the actor has are valid.")]
		public readonly BitSet<ArmorType> ArmorTypes = default(BitSet<ArmorType>);

		[FieldLoader.LoadUsing(nameof(LoadShape))]
		[Desc("Engine comes with support for `Circle`, `Capsule`, `Polygon` and `Rectangle`. Defaults to `Circle` when left empty.")]
		public readonly IHitShape Type;

		static object LoadShape(MiniYaml yaml)
		{
			IHitShape ret;

			var shapeNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Type");
			var shape = shapeNode != null ? shapeNode.Value.Value : string.Empty;

			if (!string.IsNullOrEmpty(shape))
			{
				try
				{
					ret = Game.CreateObject<IHitShape>(shape + "Shape");
					FieldLoader.Load(ret, shapeNode.Value);
				}
				catch (YamlException e)
				{
					throw new YamlException($"HitShape {shape}: {e.Message}");
				}
			}
			else
				ret = new CircleShape();

			ret.Initialize();
			return ret;
		}

		public override object Create(ActorInitializer init) { return new HitShape(init.Self, this); }
	}

	public class HitShape : ConditionalTrait<HitShapeInfo>, ITargetablePositions
	{
		readonly BodyOrientation orientation;
		ITargetableCells targetableCells;
		Turreted turret;

		((CPos Cell, SubCell SubCell)[] targetableCells,
			WPos? selfCenterPosition,
			WRot? selfOrientation,
			WRot? turretLocalOrientation,
			WVec? turretOffset) cacheInput;

		WPos[] cachedTargetablePositions;

		public HitShape(Actor self, HitShapeInfo info)
			: base(info)
		{
			orientation = self.Trait<BodyOrientation>();
		}

		protected override void Created(Actor self)
		{
			targetableCells = self.TraitOrDefault<ITargetableCells>();
			turret = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == Info.Turret);

			base.Created(self);
		}

		IEnumerable<WPos> ITargetablePositions.TargetablePositions(Actor self)
		{
			if (IsTraitDisabled)
				return Enumerable.Empty<WPos>();

			// Check for changes in inputs that affect the result of the TargetablePositions method.
			// If the inputs have not changed we can cache and reuse the result for later calls.
			// i.e. we are treating the method as a pure function.
			var newCacheInput = (
				Info.UseTargetableCellsOffsets ? targetableCells?.TargetableCells() : null,
				Info.TargetableOffsets.Length > 0 ? self.CenterPosition : (WPos?)null,
				Info.TargetableOffsets.Length > 0 ? self.Orientation : (WRot?)null,
				Info.TargetableOffsets.Length > 0 ? turret?.LocalOrientation : null,
				Info.TargetableOffsets.Length > 0 ? turret?.Offset : null);
			if (cachedTargetablePositions == null ||
				cacheInput != newCacheInput)
			{
				cachedTargetablePositions = TargetablePositions(self).ToArray();
				cacheInput = newCacheInput;
			}

			return cachedTargetablePositions;
		}

		IEnumerable<WPos> TargetablePositions(Actor self)
		{
			if (Info.UseTargetableCellsOffsets && targetableCells != null)
				foreach (var c in targetableCells.TargetableCells())
					yield return self.World.Map.CenterOfCell(c.Cell);

			foreach (var o in Info.TargetableOffsets)
			{
				var offset = CalculateTargetableOffset(self, o);
				yield return self.CenterPosition + offset;
			}
		}

		WVec CalculateTargetableOffset(Actor self, in WVec offset)
		{
			var localOffset = offset;
			var quantizedBodyOrientation = orientation.QuantizeOrientation(self.Orientation);

			if (turret != null)
			{
				localOffset = localOffset.Rotate(turret.LocalOrientation);
				localOffset += turret.Offset;
			}

			return orientation.LocalToWorld(localOffset.Rotate(quantizedBodyOrientation));
		}

		public WDist DistanceFromEdge(Actor self, WPos pos)
		{
			var origin = turret != null ? self.CenterPosition + turret.Position(self) : self.CenterPosition;
			var orientation = turret != null ? turret.WorldOrientation : self.Orientation;
			return Info.Type.DistanceFromEdge(pos, origin, orientation);
		}

		public IEnumerable<IRenderable> RenderDebugAnnotations(Actor self)
		{
			var targetPosHLine = new WVec(0, 128, 0);
			var targetPosVLine = new WVec(128, 0, 0);
			var targetPosColor = IsTraitDisabled ? Color.Gainsboro : Color.Lime;
			foreach (var p in TargetablePositions(self))
			{
				yield return new LineAnnotationRenderable(p - targetPosHLine, p + targetPosHLine, 1, targetPosColor);
				yield return new LineAnnotationRenderable(p - targetPosVLine, p + targetPosVLine, 1, targetPosColor);
			}
		}

		public IEnumerable<IRenderable> RenderDebugOverlay(Actor self, WorldRenderer wr)
		{
			var origin = turret != null ? self.CenterPosition + turret.Position(self) : self.CenterPosition;
			var orientation = turret != null ? turret.WorldOrientation : self.Orientation;
			return Info.Type.RenderDebugOverlay(this, wr, origin, orientation);
		}
	}
}
