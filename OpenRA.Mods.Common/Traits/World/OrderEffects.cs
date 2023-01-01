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
		public readonly float3 ActorFlashTint = new float3(1.4f, 1.4f, 1.4f);

		[Desc("Number of times to flash (frozen) actors.")]
		public readonly int ActorFlashCount = 2;

		[Desc("Number of ticks between (frozen) actor flashes.")]
		public readonly int ActorFlashInterval = 2;

		public override object Create(ActorInitializer init)
		{
			return new OrderEffects(this);
		}
	}

	public class OrderEffects : INotifyOrderIssued
	{
		readonly OrderEffectsInfo info;

		public OrderEffects(OrderEffectsInfo info)
		{
			this.info = info;
		}

		bool INotifyOrderIssued.OrderIssued(World world, Target target)
		{
			switch (target.Type)
			{
				case TargetType.Actor:
				{
					if (info.ActorFlashType == ActorFlashType.Overlay)
						world.AddFrameEndTask(w => w.Add(new FlashTarget(
							target.Actor, info.ActorFlashOverlayColor, info.ActorFlashOverlayAlpha,
							info.ActorFlashCount, info.ActorFlashInterval)));
					else
						world.AddFrameEndTask(w => w.Add(new FlashTarget(
							target.Actor, info.ActorFlashTint,
							info.ActorFlashCount, info.ActorFlashInterval)));

					return true;
				}

				case TargetType.FrozenActor:
				{
					if (info.ActorFlashType == ActorFlashType.Overlay)
						target.FrozenActor.Flash(info.ActorFlashOverlayColor, info.ActorFlashOverlayAlpha);
					else
						target.FrozenActor.Flash(info.ActorFlashTint);

					return true;
				}

				case TargetType.Terrain:
				{
					world.AddFrameEndTask(w => w.Add(new SpriteAnnotation(target.CenterPosition, world, info.TerrainFlashImage, info.TerrainFlashSequence, info.TerrainFlashPalette)));
					return true;
				}

				default:
					return false;
			}
		}
	}
}
