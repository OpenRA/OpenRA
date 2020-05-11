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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Attach this to the player actor. When attached, enables all actors possessing the ProducibleWithLevel ",
		"trait to have their production queue icons render with an overlay defined in this trait. ",
		"The icon change occurs when ProducibleWithLevel.Prerequisites are met.")]
	public class VeteranProductionIconOverlayInfo : TraitInfo, Requires<TechTreeInfo>
	{
		[FieldLoader.Require]
		[Desc("Image used for the overlay.")]
		public readonly string Image = null;

		[SequenceReference("Image")]
		[Desc("Sequence used for the overlay (cannot be animated).")]
		public readonly string Sequence = null;

		[PaletteReference]
		[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
		public readonly string Palette = "chrome";

		public override object Create(ActorInitializer init) { return new VeteranProductionIconOverlay(init, this); }
	}

	public class VeteranProductionIconOverlay : ITechTreeElement, IProductionIconOverlay
	{
		// HACK: TechTree doesn't associate Watcher.Key with the registering ITechTreeElement.
		// So in a situation where multiple ITechTreeElements register Watchers with the same Key,
		// and one removes its Watcher, all other ITechTreeElements' Watchers get removed too.
		// This makes sure that the keys are unique with respect to the registering ITechTreeElement.
		const string Prefix = "ProductionIconOverlay.";

		readonly Actor self;
		readonly Sprite sprite;
		readonly VeteranProductionIconOverlayInfo info;

		Dictionary<ActorInfo, bool> overlayActive = new Dictionary<ActorInfo, bool>();

		public VeteranProductionIconOverlay(ActorInitializer init, VeteranProductionIconOverlayInfo info)
		{
			self = init.Self;

			var anim = new Animation(self.World, info.Image);
			anim.Play(info.Sequence);
			sprite = anim.Image;

			this.info = info;

			var ttc = self.Trait<TechTree>();

			foreach (var a in self.World.Map.Rules.Actors.Values)
			{
				var uwc = a.TraitInfoOrDefault<ProducibleWithLevelInfo>();
				if (uwc != null)
					ttc.Add(MakeKey(a.Name), uwc.Prerequisites, 0, this);
			}
		}

		Sprite IProductionIconOverlay.Sprite { get { return sprite; } }
		string IProductionIconOverlay.Palette { get { return info.Palette; } }
		float2 IProductionIconOverlay.Offset(float2 iconSize)
		{
			var x = (sprite.Size.X - iconSize.X) / 2;
			var y = (sprite.Size.Y - iconSize.Y) / 2;
			return new float2(x, y);
		}

		bool IProductionIconOverlay.IsOverlayActive(ActorInfo ai)
		{
			bool isActive;
			if (!overlayActive.TryGetValue(ai, out isActive))
				return false;

			return isActive;
		}

		static string MakeKey(string name)
		{
			return Prefix + name;
		}

		static string GetName(string key)
		{
			return key.Substring(Prefix.Length);
		}

		public void PrerequisitesAvailable(string key)
		{
			var ai = self.World.Map.Rules.Actors[GetName(key)];
			overlayActive[ai] = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			var ai = self.World.Map.Rules.Actors[GetName(key)];
			overlayActive[ai] = false;
		}

		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
