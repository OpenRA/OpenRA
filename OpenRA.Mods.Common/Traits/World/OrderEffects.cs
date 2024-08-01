#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum ActorFlashType { Overlay, Tint }

	[TraitLocation(SystemActors.World)]
	[Desc("Renders an effect at the order target locations.")]
	public class OrderEffectsInfo : TraitInfo
	{
		[Desc("The image to use.")]
		[FieldLoader.Require]
		public readonly string TerrainFlashImage = null;

		[Desc("The sequence to use.")]
		[FieldLoader.Require]
		public readonly string TerrainFlashSequence = null;

		[Desc("The palette to use.")]
		public readonly string TerrainFlashPalette = null;

		[Desc("The type of effect to apply to targeted (frozen) actors. Accepts values Overlay and Tint.")]
		public readonly ActorFlashType ActorFlashType = ActorFlashType.Overlay;

		[Desc("The overlay color to display when ActorFlashType is Overlay.")]
		public readonly Color ActorFlashOverlayColor = Color.White;

		[Desc("The overlay transparency to display when ActorFlashType is Overlay.")]
		public readonly float ActorFlashOverlayAlpha = 0.5f;

		[Desc("The tint to apply when ActorFlashType is Tint.")]
		public readonly float3 ActorFlashTint = new(1.4f, 1.4f, 1.4f);

		[Desc("Number of times to flash (frozen) actors.")]
		public readonly int ActorFlashCount = 2;

		[Desc("Number of ticks between (frozen) actor flashes.")]
		public readonly int ActorFlashInterval = 2;

		[Desc("Order name(s) this applies for (empty if applies to all).")]
		public readonly List<string> OrderScope = new();

		public override object Create(ActorInitializer init)
		{
			return new OrderEffects(this);
		}
	}

	public class OrderEffects : INotifyOrderIssued
	{
		protected readonly OrderEffectsInfo Info;

		public OrderEffects(OrderEffectsInfo info)
		{
			Info = info;
		}

		protected void FlashActor(World world, Target target)
		{
			if (Info.ActorFlashType == ActorFlashType.Overlay)
				world.AddFrameEndTask(w => w.Add(new FlashTarget(
					target.Actor, Info.ActorFlashOverlayColor, Info.ActorFlashOverlayAlpha,
					Info.ActorFlashCount, Info.ActorFlashInterval)));
			else
				world.AddFrameEndTask(w => w.Add(new FlashTarget(
					target.Actor, Info.ActorFlashTint,
					Info.ActorFlashCount, Info.ActorFlashInterval)));
		}

		protected void FlashFrozenActor(Target target)
		{
			if (Info.ActorFlashType == ActorFlashType.Overlay)
				target.FrozenActor.Flash(Info.ActorFlashOverlayColor, Info.ActorFlashOverlayAlpha);
			else
				target.FrozenActor.Flash(Info.ActorFlashTint);
		}

		protected void FlashTerrain(World world, Target target)
		{
			world.AddFrameEndTask(w => w.Add(new SpriteAnnotation(
						target.CenterPosition, world, Info.TerrainFlashImage, Info.TerrainFlashSequence, Info.TerrainFlashPalette)));
		}

		protected virtual bool OrderIssued(World world, Target target)
		{
			switch (target.Type)
			{
				case TargetType.Actor:
				{
					FlashActor(world, target);
					return true;
				}

				case TargetType.FrozenActor:
				{
					FlashFrozenActor(target);
					return true;
				}

				case TargetType.Terrain:
				{
					FlashTerrain(world, target);
					return true;
				}

				default:
					return false;
			}
		}

		bool INotifyOrderIssued.OrderIssued(World world, string orderString, Target target)
		{
			if (Info.OrderScope.Count > 0 && Info.OrderScope.Contains(orderString, StringComparer.Ordinal))
				return false;

			return OrderIssued(world, target);
		}
	}
}
