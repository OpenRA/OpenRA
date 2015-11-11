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
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("This structure can be infiltrated causing resources to be stolen.")]
	class InfiltrateForResourcesInfo : ITraitInfo
	{
		[Desc("Resource to steal.")]
		[FieldLoader.Require]
		public readonly string ResourceType;

		public readonly int Percentage = 50;
		public readonly int Minimum = 500;
		public readonly string SoundToVictim = "credit1.aud";

		public object Create(ActorInitializer init) { return new InfiltrateForResources(this); }
	}

	class InfiltrateForResources : INotifyInfiltrated
	{
		readonly InfiltrateForResourcesInfo info;

		public InfiltrateForResources(InfiltrateForResourcesInfo info) { this.info = info; }

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			var targetResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var spyResources = infiltrator.Owner.PlayerActor.Trait<PlayerResources>();

			var toTake = (targetResources.Cash + targetResources.Resources) * info.Percentage / 100;
			var toGive = Math.Max(toTake, info.Minimum);

			targetResources.TakeResource(info.ResourceType, toTake);
			spyResources.GiveResource(info.ResourceType, toGive);

			Game.Sound.PlayToPlayer(self.Owner, info.SoundToVictim);

			self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, infiltrator.Owner.Color.RGB, FloatingText.FormatCashTick(toGive), 30)));
		}
	}
}
