#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor. When attached, enables all actors possessing the LevelupWhenCreated ",
		"trait to have their production queue icons render with an overlay defined in this trait. ",
		"The icon change occurs when LevelupWhenCreated.Prerequisites are met.")]
	public class VeteranProductionIconOverlayInfo : ITraitInfo, Requires<TechTreeInfo>
	{
		[FieldLoader.Require]
		[Desc("Image used for the overlay.")]
		public readonly string Image = null;

		[Desc("Sequence used for the overlay (cannot be animated).")]
		[SequenceReference("Image")] public readonly string Sequence = null;

		[Desc("Palette to render the sprite in. Reference the world actor's PaletteFrom* traits.")]
		[PaletteReference] public readonly string Palette = "chrome";

		[Desc("Point on the production icon's used as reference for offsetting the overlay. ",
			"Possible values are any combination of Top, VCenter, Bottom and Left, HCenter, Right separated by a comma.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

		[Desc("Pixel offset relative to the icon's reference point.")]
		public readonly int2 Offset = int2.Zero;

		[Desc("Visual scale of the overlay.")]
		public readonly float Scale = 1f;

		public object Create(ActorInitializer init) { return new VeteranProductionIconOverlay(init, this); }
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
				var uwc = a.Traits.GetOrDefault<ProducibleWithLevelInfo>();
				if (uwc != null)
					ttc.Add(MakeKey(a.Name), uwc.Prerequisites, 0, this);
			}
		}

		public Sprite Sprite()
		{
			return sprite;
		}

		public string Palette()
		{
			return info.Palette;
		}

		public float Scale()
		{
			return info.Scale;
		}

		public float2 Offset(float2 iconSize)
		{
			float offsetX = 0, offsetY = 0;
			switch (info.ReferencePoint & (ReferencePoints)3)
			{
				case ReferencePoints.Top:
					offsetY = (-iconSize.Y + sprite.Size.Y) / 2;
					break;
				case ReferencePoints.VCenter:
					break;
				case ReferencePoints.Bottom:
					offsetY = (iconSize.Y - sprite.Size.Y) / 2;
					break;
			}

			switch (info.ReferencePoint & (ReferencePoints)(3 << 2))
			{
				case ReferencePoints.Left:
					offsetX = (-iconSize.X + sprite.Size.X) / 2;
					break;
				case ReferencePoints.HCenter:
					break;
				case ReferencePoints.Right:
					offsetX = (iconSize.X - sprite.Size.X) / 2;
					break;
			}

			return new float2(offsetX, offsetY) + info.Offset;
		}

		public bool IsOverlayActive(ActorInfo ai)
		{
			bool isActive;
			overlayActive.TryGetValue(ai, out isActive);

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

		public void PrerequisitesUnavailable(string key) { }
		public void PrerequisitesItemHidden(string key) { }
		public void PrerequisitesItemVisible(string key) { }
	}
}
