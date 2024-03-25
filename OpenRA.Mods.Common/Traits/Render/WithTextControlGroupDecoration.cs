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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders Ctrl groups using typeface.")]
	public class WithTextControlGroupDecorationInfo : TraitInfo, IRulesetLoaded
	{
		public readonly string Font = "TinyBold";

		[Desc("Display in this color when not using the player color.")]
		public readonly Color Color = Color.White;

		[Desc("Use the player color of the current owner.")]
		public readonly bool UsePlayerColor = false;

		[Desc("Position in the actor's selection box to draw the decoration.")]
		public readonly string Position = "TopLeft";

		[Desc("Offset text center position from the selection box edge.")]
		public readonly int2 Margin = int2.Zero;

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (!Game.ModData.Manifest.Get<Fonts>().FontList.ContainsKey(Font))
				throw new YamlException($"Font '{Font}' is not listed in the mod.yaml's Fonts section");
		}

		public override object Create(ActorInitializer init) { return new WithTextControlGroupDecoration(init.Self, this); }
	}

	public class WithTextControlGroupDecoration : IDecoration
	{
		readonly WithTextControlGroupDecorationInfo info;
		readonly SpriteFont font;
		readonly CachedTransform<int, string> label;

		public WithTextControlGroupDecoration(Actor self, WithTextControlGroupDecorationInfo info)
		{
			this.info = info;
			font = Game.Renderer.Fonts[info.Font];
			label = new CachedTransform<int, string>(g => self.World.ControlGroups.Groups[g]);
		}

		bool IDecoration.RequiresSelection => true;

		IEnumerable<IRenderable> IDecoration.RenderDecoration(Actor self, WorldRenderer wr, ISelectionDecorations container)
		{
			var group = self.World.ControlGroups.GetControlGroupForActor(self);
			if (group == null)
				return Enumerable.Empty<IRenderable>();

			var text = label.Update(group.Value);
			var screenPos = container.GetDecorationOrigin(self, wr, info.Position, info.Margin);
			return new IRenderable[]
			{
				new UITextRenderable(font, self.CenterPosition, screenPos, 0, info.UsePlayerColor ? self.OwnerColor() : info.Color, text)
			};
		}
	}
}
