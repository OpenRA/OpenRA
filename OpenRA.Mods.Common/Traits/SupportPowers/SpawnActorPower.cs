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

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns an actor that stays for a limited amount of time.")]
	public class SpawnActorPowerInfo : SupportPowerInfo
	{
		[ActorReference, FieldLoader.Require]
		[Desc("Actor to spawn.")]
		public readonly string Actor = null;

		[Desc("Amount of time to keep the actor alive in ticks. Value < 0 means this actor will not remove itself.")]
		public readonly int LifeTime = 250;

		public readonly string DeploySound = null;

		public readonly string EffectImage = null;
		[SequenceReference("EffectImage")] public readonly string EffectSequence = "idle";
		[PaletteReference] public readonly string EffectPalette = null;

		public override object Create(ActorInitializer init) { return new SpawnActorPower(init.Self, this); }
	}

	public class SpawnActorPower : SupportPower
	{
		public SpawnActorPower(Actor self, SpawnActorPowerInfo info) : base(self, info) { }
		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var info = Info as SpawnActorPowerInfo;

			if (info.Actor != null)
			{
				self.World.AddFrameEndTask(w =>
				{
					var location = self.World.Map.CenterOfCell(order.TargetLocation);

					Game.Sound.Play(info.DeploySound, location);

					if (!string.IsNullOrEmpty(info.EffectSequence) && !string.IsNullOrEmpty(info.EffectPalette))
						w.Add(new SpriteEffect(location, w, info.EffectImage, info.EffectSequence, info.EffectPalette));

					var actor = w.CreateActor(info.Actor, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(self.Owner),
					});

					if (info.LifeTime > -1)
					{
						actor.QueueActivity(new Wait(info.LifeTime));
						actor.QueueActivity(new RemoveSelf());
					}
				});
			}
		}
	}
}
