#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Effects;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA
{
	[Desc("Spawns an actor that stays for a limited amount of time.")]
	public class SpawnActorPowerInfo : SupportPowerInfo
	{
		[Desc("Actor to spawn.")]
		public readonly string Actor = null;

		[Desc("Amount of time to keep the actor alive in ticks.")]
		public readonly int LifeTime = 250;

		public readonly string DeploySound = null;

		public readonly string EffectSequence = null;
		public readonly string EffectPalette = null;

		public override object Create(ActorInitializer init) { return new SpawnActorPower(init, this); }
	}

	public class SpawnActorPower : SupportPower
	{
		public SpawnActorPower(ActorInitializer init, SpawnActorPowerInfo info) : base(init, info) { }
		public override void Activate(Order order)
		{
			base.Activate(order);

			var info = Info as SpawnActorPowerInfo;

			if (info.Actor != null)
			{
				Self.World.AddFrameEndTask(w =>
				{
					var location = Self.World.Map.CenterOfCell(order.TargetLocation);

					Sound.Play(info.DeploySound, location);

					if (!string.IsNullOrEmpty(info.EffectSequence) && !string.IsNullOrEmpty(info.EffectPalette))
						w.Add(new SpriteEffect(location, w, info.EffectSequence, info.EffectPalette));

					var actor = w.CreateActor(info.Actor, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(Self.Owner),
					});

					actor.QueueActivity(new Wait(info.LifeTime));
					actor.QueueActivity(new RemoveSelf());
				});
			}
		}
	}
}
