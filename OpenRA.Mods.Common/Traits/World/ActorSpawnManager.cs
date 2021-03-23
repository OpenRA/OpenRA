#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the spawning of specified actor types. Attach this to the world actor.")]
	public class ActorSpawnManagerInfo : ConditionalTraitInfo, Requires<MapCreepsInfo>
	{
		[Desc("Minimum number of actors.")]
		public readonly int Minimum = 0;

		[Desc("Maximum number of actors.")]
		public readonly int Maximum = 4;

		[Desc("Time (in ticks) between actor spawn. Supports 1 or 2 values.\nIf 2 values are provided they are used as a range from which a value is randomly selected.")]
		public readonly int[] SpawnInterval = { 6000 };

		[FieldLoader.Require]
		[ActorReference]
		[Desc("Name of the actor that will be randomly picked to spawn.")]
		public readonly string[] Actors = { };

		public readonly string Owner = "Creeps";

		[Desc("Type of ActorSpawner with which it connects.")]
		public readonly HashSet<string> Types = new HashSet<string>() { };

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (SpawnInterval.Length == 0 || SpawnInterval.Length > 2)
				throw new YamlException("{0}.{1} must be either 1 or 2 values".F(nameof(ActorSpawnManager), nameof(SpawnInterval)));

			if (SpawnInterval.Length == 2 && SpawnInterval[0] >= SpawnInterval[1])
				throw new YamlException("{0}.{1}'s first value must be less than the second value".F(nameof(ActorSpawnManager), nameof(SpawnInterval)));

			if (SpawnInterval.Any(it => it < 0))
				throw new YamlException("{0}.{1}'s value(s) must not be less than 0".F(nameof(ActorSpawnManager), nameof(SpawnInterval)));
		}

		public override object Create(ActorInitializer init) { return new ActorSpawnManager(init.Self, this); }
	}

	public class ActorSpawnManager : ConditionalTrait<ActorSpawnManagerInfo>, ITick
	{
		readonly ActorSpawnManagerInfo info;

		bool enabled;
		int spawnCountdown;
		int actorsPresent;

		public ActorSpawnManager(Actor self, ActorSpawnManagerInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			enabled = self.Trait<MapCreeps>().Enabled;
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !enabled)
				return;

			if (info.Maximum < 1 || actorsPresent >= info.Maximum)
				return;

			if (--spawnCountdown > 0 && actorsPresent >= info.Minimum)
				return;

			var spawnPoint = GetRandomSpawnPoint(self.World, self.World.SharedRandom);

			if (spawnPoint == null)
				return;

			spawnCountdown = Util.RandomDelay(self.World, info.SpawnInterval);

			do
			{
				// Always spawn at least one actor, plus
				// however many needed to reach the minimum.
				SpawnActor(self, spawnPoint);
			}
			while (actorsPresent < info.Minimum);
		}

		WPos SpawnActor(Actor self, Actor spawnPoint)
		{
			self.World.AddFrameEndTask(w => w.CreateActor(info.Actors.Random(self.World.SharedRandom), new TypeDictionary
			{
				new OwnerInit(w.Players.First(x => x.PlayerName == info.Owner)),
				new LocationInit(spawnPoint.Location)
			}));

			actorsPresent++;

			return spawnPoint.CenterPosition;
		}

		Actor GetRandomSpawnPoint(World world, Support.MersenneTwister random)
		{
			var spawnPointActors = world.ActorsWithTrait<ActorSpawner>()
				.Where(x => !x.Trait.IsTraitDisabled && (info.Types.Overlaps(x.Trait.Types) || !x.Trait.Types.Any()))
				.ToArray();

			return spawnPointActors.Any() ? spawnPointActors.Random(random).Actor : null;
		}

		public void DecreaseActorCount()
		{
			actorsPresent--;
		}
	}
}
