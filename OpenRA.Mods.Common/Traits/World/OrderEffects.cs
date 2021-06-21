#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
    [Desc("Renders an effect at the order target locations.")]
    public class OrderEffectsInfo : TraitInfo
    {
        [Desc("The image to use.")]
        [FieldLoader.Require]
        public readonly string TerrainFlashImage;

        [Desc("The sequence to use.")]
        [FieldLoader.Require]
        public readonly string TerrainFlashSequence;

        [Desc("The palette to use.")]
        public readonly string TerrainFlashPalette;

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
            if (target.Type == TargetType.Actor)
            {
                world.AddFrameEndTask(w => w.Add(new FlashTarget(target.Actor)));
                return true;
            }

            if (target.Type == TargetType.FrozenActor)
            {
                target.FrozenActor.Flash();
                return true;
            }

            if (target.Type == TargetType.Terrain)
            {
                world.AddFrameEndTask(w => w.Add(new SpriteAnnotation(target.CenterPosition, world, info.TerrainFlashImage, info.TerrainFlashSequence, info.TerrainFlashPalette)));
                return true;
            }

            return false;
        }
    }
}
