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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	class GpsDotEffect : IEffect, IEffectAnnotation
	{
		readonly Actor actor;
		readonly GpsDotInfo info;
		readonly Animation anim;

		readonly PlayerDictionary<DotState> dotStates;
		readonly IVisibilityModifier[] visibilityModifiers;

		class DotState
		{
			public readonly GpsWatcher Watcher;
			public readonly bool FrozenActorWithRenderables;
			public bool Visible;
			public DotState(Actor a, GpsWatcher watcher, FrozenActorLayer frozenLayer)
			{
				Watcher = watcher;
				if (frozenLayer != null)
				{
					var frozenActor = frozenLayer.FromID(a.ActorID);
					FrozenActorWithRenderables = frozenActor != null && frozenActor.HasRenderables;
				}
			}
		}

		public GpsDotEffect(Actor actor, GpsDotInfo info)
		{
			this.actor = actor;
			this.info = info;
			anim = new Animation(actor.World, info.Image);
			anim.PlayRepeating(info.String);

			visibilityModifiers = actor.TraitsImplementing<IVisibilityModifier>().ToArray();

			dotStates = new PlayerDictionary<DotState>(actor.World,
				p => new DotState(actor, p.PlayerActor.Trait<GpsWatcher>(), p.FrozenActorLayer));
		}

		bool ShouldRender(DotState state, Player toPlayer)
		{
			// Hide the indicator if a frozen actor portrait is visible
			if (state.FrozenActorWithRenderables)
				return false;

			// Hide the indicator if no watchers are available
			if (!state.Watcher.Granted && !state.Watcher.GrantedAllies)
				return false;

			// Hide the indicator if the unit appears to be owned by an allied player
			var owner = actor.EffectiveOwner?.Owner;
			if (owner != null && toPlayer.IsAlliedWith(owner))
				return false;

			// Hide the indicator behind shroud
			var visibility = toPlayer.Shroud.GetVisibility(actor.CenterPosition);
			if (!visibility.HasFlag(Shroud.CellVisibility.Explored))
				return false;

			// Hide for visible
			if (visibility.HasFlag(Shroud.CellVisibility.Visible))
				return false;

			// Hide indicator if the actor wouldn't otherwise be visible if there wasn't fog
			foreach (var visibilityModifier in visibilityModifiers)
				if (!visibilityModifier.IsVisible(actor, toPlayer))
					return false;

			return true;
		}

		void IEffect.Tick(World world)
		{
			for (var playerIndex = 0; playerIndex < dotStates.Count; playerIndex++)
			{
				var state = dotStates[playerIndex];
				state.Visible = ShouldRender(state, world.Players[playerIndex]);
			}
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr)
		{
			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (actor.World.RenderPlayer == null || !dotStates[actor.World.RenderPlayer].Visible)
				return SpriteRenderable.None;

			var effectiveOwner = actor.EffectiveOwner != null && actor.EffectiveOwner.Owner != null ?
				actor.EffectiveOwner.Owner : actor.Owner;

			var palette = wr.Palette(info.IndicatorPalettePrefix + effectiveOwner.InternalName);
			var screenPos = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(actor.CenterPosition));
			return anim.RenderUI(wr, screenPos, WVec.Zero, 0, palette);
		}
	}
}
