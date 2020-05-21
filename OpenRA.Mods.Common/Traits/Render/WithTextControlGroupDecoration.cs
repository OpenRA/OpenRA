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
		public readonly DecorationPosition Position = DecorationPosition.TopLeft;

		[Desc("Offset text center position from the selection box edge.")]
		public readonly int2 Margin = int2.Zero;

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (!Game.ModData.Manifest.Get<Fonts>().FontList.ContainsKey(Font))
				throw new YamlException("Font '{0}' is not listed in the mod.yaml's Fonts section".F(Font));
		}

		public override object Create(ActorInitializer init) { return new WithTextControlGroupDecoration(init.Self, this); }
	}

	public class WithTextControlGroupDecoration : IDecoration, INotifyOwnerChanged
	{
		readonly WithTextControlGroupDecorationInfo info;
		readonly SpriteFont font;
		readonly Actor self;
		readonly CachedTransform<int, string> label;

		Color color;

		public WithTextControlGroupDecoration(Actor self, WithTextControlGroupDecorationInfo info)
		{
			this.info = info;
			this.self = self;
			font = Game.Renderer.Fonts[info.Font];
			color = info.UsePlayerColor ? self.Owner.Color : info.Color;
			label = new CachedTransform<int, string>(g => g.ToString());
		}

		DecorationPosition IDecoration.Position { get { return info.Position; } }

		bool IDecoration.Enabled { get { return self.Owner == self.World.LocalPlayer && self.World.Selection.GetControlGroupForActor(self) != null; } }

		bool IDecoration.RequiresSelection { get { return true; } }

		IEnumerable<IRenderable> IDecoration.RenderDecoration(Actor self, WorldRenderer wr, int2 pos)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null)
				return Enumerable.Empty<IRenderable>();

			var text = label.Update(group.Value);
			var screenPos = wr.Viewport.WorldToViewPx(pos) + info.Position.CreateMargin(info.Margin);
			return new IRenderable[]
			{
				new UITextRenderable(font, self.CenterPosition, screenPos, 0, color, text)
			};
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (info.UsePlayerColor)
				color = newOwner.Color;
		}
	}
}
