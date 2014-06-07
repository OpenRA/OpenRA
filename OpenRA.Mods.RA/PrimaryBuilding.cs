#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	static class PrimaryExts
	{
		public static bool IsPrimaryBuilding(this Actor a)
		{
			var pb = a.TraitOrDefault<PrimaryBuilding>();
			return pb != null && pb.IsPrimary;
		}
	}

	[Desc("Used together with ClassicProductionQueue.")]
	class PrimaryBuildingInfo : TraitInfo<PrimaryBuilding> { }

	class PrimaryBuilding : IIssueOrder, IResolveOrder, ITags, IPostRender
	{
		bool isPrimary = false;
		public bool IsPrimary { get { return isPrimary; } }

		public IEnumerable<TagType> GetTags()
		{
			yield return isPrimary ? TagType.Primary : TagType.None;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("PrimaryProducer", 1); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "PrimaryProducer")
				return new Order(order.OrderID, self, false);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PrimaryProducer")
				SetPrimaryProducer(self, !isPrimary);
		}

		public void SetPrimaryProducer(Actor self, bool state)
		{
			if (state == false)
			{
				isPrimary = false;
				return;
			}

			// TODO: THIS IS SHIT
			// Cancel existing primaries
			foreach (var p in self.Info.Traits.Get<ProductionInfo>().Produces)
				foreach (var b in self.World
					.ActorsWithTrait<PrimaryBuilding>()
					.Where(a => a.Actor.Owner == self.Owner)
					.Where(x => x.Trait.IsPrimary
						&& x.Actor.Info.Traits.Get<ProductionInfo>().Produces.Contains(p)))
					b.Trait.SetPrimaryProducer(b.Actor, false);

			isPrimary = true;

			Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", "PrimaryBuildingSelected", self.Owner.Country.Race);
		}

		// Draw Primary tag
		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if(isPrimary)
			{
				var pos = wr.ScreenPxPosition(self.CenterPosition);
				var bounds = self.Bounds.Value;
				bounds.Offset(pos.X, pos.Y);

				int2 basePosition = new int2((bounds.Left + bounds.Right) / 2, bounds.Top);

				var tagImages = new Animation(self.World, "pips");
				var pal = wr.Palette("chrome");
				var tagxyOffset = new int2(0, 7);

				// Special tag position for airfield
				if (self.Info.Name == "afld")
				{
					if (Game.Settings.Graphics.PixelDouble)
					{
						tagxyOffset.X = 35;
						tagxyOffset.Y = 72;
					} 
					else
					{
						tagxyOffset.X = -16;
						tagxyOffset.Y = 15;
					}
				}

				var tagBase = wr.Viewport.WorldToViewPx(basePosition);

				if (this.GetTags().Contains(TagType.Primary))
				{
					tagImages.PlayRepeating("tag-primary");
					var tagPos = tagBase + tagxyOffset - (0.5f * tagImages.Image.size).ToInt2();
					Game.Renderer.SpriteRenderer.DrawSprite(tagImages.Image, tagPos, pal);
				}
			}
		}
	}
}
