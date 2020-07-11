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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays the actor's type and ID above the actor.")]
	class RenderDebugStateInfo : TraitInfo
	{
		public readonly string Font = "TinyBold";

		public override object Create(ActorInitializer init) { return new RenderDebugState(init.Self, this); }
	}

	class RenderDebugState : INotifyAddedToWorld, INotifyOwnerChanged, INotifyCreated, IRenderAnnotationsWhenSelected
	{
		readonly DebugVisualizations debugVis;
		readonly SpriteFont font;
		readonly Actor self;
		readonly WVec offset;
		SquadManagerBotModule ai;

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

			debugVis = self.World.WorldActor.TraitOrDefault<DebugVisualizations>();
		}

		void INotifyCreated.Created(Actor self)
		{
			ai = self.Owner.PlayerActor.TraitsImplementing<SquadManagerBotModule>().FirstOrDefault(Exts.IsTraitEnabled);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			tagString = self.ToString();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			color = GetColor();
		}

		Color GetColor()
		{
			return self.EffectiveOwner != null && self.EffectiveOwner.Disguised ? self.EffectiveOwner.Owner.Color : self.Owner.Color;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (debugVis == null || !debugVis.ActorTags)
				yield break;

			yield return new TextAnnotationRenderable(font, self.CenterPosition - offset, 0, color, tagString);

			// Get the actor's activity.
			var activity = self.CurrentActivity;
			if (activity != null)
				yield return new TextAnnotationRenderable(font, self.CenterPosition, 0, color, activity.DebugLabelComponents().JoinWith("."));

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
			yield return new TextAnnotationRenderable(font, self.CenterPosition + offset, 0, color, aiSquadInfo);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable { get { return true; } }
	}
}
