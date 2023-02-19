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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Attach this to the player actor. Required for WithProductionIconOverlay trait on actors to work.")]
	public class ProductionIconOverlayManagerInfo : TraitInfo, Requires<TechTreeInfo>, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Type of the overlay. Prerequisites from WithProductionIconOverlay traits with matching types determine when this overlay will be enabled.")]
		public readonly string Type = null;

		[FieldLoader.Require]
		[Desc("Image used for the overlay.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for the overlay (cannot be animated).")]
		public readonly string Sequence = null;

		[PaletteReference]
		[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
		public readonly string Palette = "chrome";

		public virtual void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (rules.Actors[SystemActors.Player].TraitInfos<ProductionIconOverlayManagerInfo>().Any(piom => piom != this && piom.Type == Type))
				throw new YamlException($"Multiple 'ProductionIconOverlayManager's with type '{Type}' exist.");
		}

		public override object Create(ActorInitializer init) { return new ProductionIconOverlayManager(init, this); }
	}

	public class ProductionIconOverlayManager : ITechTreeElement, IProductionIconOverlay
	{
		readonly Actor self;
		readonly Sprite sprite;
		readonly ProductionIconOverlayManagerInfo info;

		// HACK: TechTree doesn't associate Watcher.Key with the registering ITechTreeElement.
		// So in a situation where multiple ITechTreeElements register Watchers with the same Key,
		// and one removes its Watcher, all other ITechTreeElements' Watchers get removed too.
		// This makes sure that the keys are unique with respect to the registering ITechTreeElement.
		readonly string prefix;

		readonly Dictionary<ActorInfo, bool> overlayActive = new Dictionary<ActorInfo, bool>();

		public ProductionIconOverlayManager(ActorInitializer init, ProductionIconOverlayManagerInfo info)
		{
			self = init.Self;

			var anim = new Animation(self.World, info.Image);
			anim.Play(info.Sequence);
			sprite = anim.Image;

			this.info = info;

			prefix = info.Type + ".";
			var ttc = self.Trait<TechTree>();

			foreach (var a in self.World.Map.Rules.Actors.Values)
			{
				foreach (var wpio in a.TraitInfos<WithProductionIconOverlayInfo>().Where(wpio => wpio.Types.Contains(info.Type)))
					ttc.Add(MakeKey(a.Name), wpio.Prerequisites, 0, this);
			}
		}

		Sprite IProductionIconOverlay.Sprite => sprite;
		string IProductionIconOverlay.Palette => info.Palette;

		float2 IProductionIconOverlay.Offset(float2 iconSize)
		{
			var x = (sprite.Size.X - iconSize.X) / 2;
			var y = (sprite.Size.Y - iconSize.Y) / 2;
			return new float2(x, y);
		}

		bool IProductionIconOverlay.IsOverlayActive(ActorInfo ai)
		{
			if (!overlayActive.TryGetValue(ai, out var isActive))
				return false;

			return isActive;
		}

		string MakeKey(string name)
		{
			return prefix + name;
		}

		string GetName(string key)
		{
			return key.Substring(prefix.Length);
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
