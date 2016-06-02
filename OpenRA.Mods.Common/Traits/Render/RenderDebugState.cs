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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.AI;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays the actor's type and ID above the actor.")]
	class RenderDebugStateInfo : ITraitInfo
	{
		public readonly string Font = "TinyBold";

		public object Create(ActorInitializer init) { return new RenderDebugState(init.Self, this); }
	}

	class RenderDebugState : INotifyAddedToWorld, INotifyOwnerChanged, IPostRenderSelection
	{
		readonly DeveloperMode devMode;
		readonly SpriteFont font;
		readonly Actor self;
		readonly WVec offset;
		readonly HackyAI ai;

		Color color;
		string tagString;

		public RenderDebugState(Actor self, RenderDebugStateInfo info)
		{
			var buildingInfo = self.Info.TraitInfoOrDefault<BuildingInfo>();
			var yOffset = buildingInfo == null ? 1 : buildingInfo.Dimensions.Y;
			offset = new WVec(0, 512 * yOffset, 0);

			this.self = self;
			color = GetColor();
			font = Game.Renderer.Fonts[info.Font];

			var localPlayer = self.World.LocalPlayer;
			devMode = localPlayer != null ? localPlayer.PlayerActor.Trait<DeveloperMode>() : null;
			ai = self.Owner.PlayerActor.TraitsImplementing<HackyAI>().FirstOrDefault(x => x.IsEnabled);
		}

		public void AddedToWorld(Actor self)
		{
			tagString = self.ToString();
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			color = GetColor();
		}

		Color GetColor()
		{
			return self.EffectiveOwner != null ? self.EffectiveOwner.Owner.Color.RGB : self.Owner.Color.RGB;
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (devMode == null || !devMode.ShowActorTags)
				yield break;

			yield return new TextRenderable(font, self.CenterPosition - offset, 0, color, tagString);

			// Get the actor's activity.
			var activity = self.GetCurrentActivity();
			if (activity != null)
			{
				var activityName = activity.GetType().ToString().Split('.').Last();
				yield return new TextRenderable(font, self.CenterPosition, 0, color, activityName);
			}

			// Get the AI squad that this actor belongs to.
			if (!self.Owner.IsBot)
				yield break;

			if (ai == null)
				yield break;

			var squads = ai.Squads;
			var squad = squads.FirstOrDefault(x => x.Units.Contains(self));
			if (squad == null)
				yield break;

			var aiSquadInfo = "{0}, {1}".F(squad.Type, squad.TargetActor);
			yield return new TextRenderable(font, self.CenterPosition + offset, 0, color, aiSquadInfo);
		}
	}
}
