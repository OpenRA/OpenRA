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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public abstract class SelectionDecorationsBaseInfo : TraitInfo
	{
		public readonly Color SelectionBoxColor = Color.White;
	}

	public abstract class SelectionDecorationsBase : ISelectionDecorations, IRenderAnnotations, INotifyCreated
	{
		Dictionary<DecorationPosition, IDecoration[]> decorations;
		Dictionary<DecorationPosition, IDecoration[]> selectedDecorations;

		protected readonly SelectionDecorationsBaseInfo info;

		public SelectionDecorationsBase(SelectionDecorationsBaseInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var groupedDecorations = new Dictionary<DecorationPosition, List<IDecoration>>();
			var groupedSelectionDecorations = new Dictionary<DecorationPosition, List<IDecoration>>();
			foreach (var d in self.TraitsImplementing<IDecoration>())
			{
				groupedSelectionDecorations.GetOrAdd(d.Position).Add(d);
				if (!d.RequiresSelection)
					groupedDecorations.GetOrAdd(d.Position).Add(d);
			}

			decorations = groupedDecorations.ToDictionary(
				d => d.Key,
				d => d.Value.ToArray());

			selectedDecorations = groupedSelectionDecorations.ToDictionary(
				d => d.Key,
				d => d.Value.ToArray());
		}

		IEnumerable<WPos> ActivityTargetPath(Actor self)
		{
			if (!self.IsInWorld || self.IsDead)
				yield break;

			var activity = self.CurrentActivity;
			if (activity != null)
			{
				var targets = activity.GetTargets(self);
				yield return self.CenterPosition;

				foreach (var t in targets.Where(t => t.Type != TargetType.Invalid))
					yield return t.CenterPosition;
			}
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (self.World.FogObscures(self))
				return Enumerable.Empty<IRenderable>();

			return DrawDecorations(self, wr);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return true; } }

		IEnumerable<IRenderable> DrawDecorations(Actor self, WorldRenderer wr)
		{
			var selected = self.World.Selection.Contains(self);
			var rollover = self.World.Selection.RolloverContains(self);
			var regularWorld = self.World.Type == WorldType.Regular;
			var statusBars = Game.Settings.Game.StatusBars;

			// Health bars are shown when:
			//  * actor is selected / in active drag rectangle / under the mouse
			//  * status bar preference is set to "always show"
			//  * status bar preference is set to "when damaged" and actor is damaged
			var displayHealth = selected || rollover || (regularWorld && statusBars == StatusBarsType.AlwaysShow)
				|| (regularWorld && statusBars == StatusBarsType.DamageShow && self.GetDamageState() != DamageState.Undamaged);

			// Extra bars are shown when:
			//  * actor is selected / in active drag rectangle / under the mouse
			//  * status bar preference is set to "always show" or "when damaged"
			var displayExtra = selected || rollover || (regularWorld && statusBars != StatusBarsType.Standard);

			if (selected)
				foreach (var r in RenderSelectionBox(self, wr, info.SelectionBoxColor))
					yield return r;

			if (displayHealth || displayExtra)
				foreach (var r in RenderSelectionBars(self, wr, displayHealth, displayExtra))
					yield return r;

			if (selected && self.World.LocalPlayer != null && self.World.LocalPlayer.PlayerActor.Trait<DeveloperMode>().PathDebug)
				yield return new TargetLineRenderable(ActivityTargetPath(self), Color.Green);

			// Hide decorations for spectators that zoom out further than the normal minimum level
			// This avoids graphical glitches with pip rows and icons overlapping the selection box
			if (wr.Viewport.Zoom < wr.Viewport.MinZoom)
				yield break;

			var renderDecorations = self.World.Selection.Contains(self) ? selectedDecorations : decorations;
			foreach (var kv in renderDecorations)
			{
				var pos = GetDecorationPosition(self, wr, kv.Key);
				foreach (var r in kv.Value)
					foreach (var rr in r.RenderDecoration(self, wr, pos))
						yield return rr;
			}
		}

		IEnumerable<IRenderable> ISelectionDecorations.RenderSelectionAnnotations(Actor self, WorldRenderer worldRenderer, Color color)
		{
			return RenderSelectionBox(self, worldRenderer, color);
		}

		protected abstract int2 GetDecorationPosition(Actor self, WorldRenderer wr, DecorationPosition pos);
		protected abstract IEnumerable<IRenderable> RenderSelectionBox(Actor self, WorldRenderer wr, Color color);
		protected abstract IEnumerable<IRenderable> RenderSelectionBars(Actor self, WorldRenderer wr, bool displayHealth, bool displayExtra);
	}
}
