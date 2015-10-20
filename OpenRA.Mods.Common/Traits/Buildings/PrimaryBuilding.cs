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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
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
	public class PrimaryBuildingInfo : ITraitInfo
	{
		public readonly string Image = "pips";

		[SequenceReference("Image")] public readonly string TagSequence = "tag-primary";

		[PaletteReference] public readonly string Palette = "chrome";

		public object Create(ActorInitializer init) { return new PrimaryBuilding(init.Self, this); }
	}

	public class PrimaryBuilding : IIssueOrder, IResolveOrder, IPostRenderSelection
	{
		readonly PrimaryBuildingInfo info;
		readonly Actor self;

		bool isPrimary = false;
		public bool IsPrimary { get { return isPrimary; } }

		public PrimaryBuilding(Actor self, PrimaryBuildingInfo info)
		{
			this.self = self;
			this.info = info;
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
			foreach (var p in self.Info.TraitInfo<ProductionInfo>().Produces)
			{
				var productionType = p;		// benign closure hazard
				foreach (var b in self.World
					.ActorsWithTrait<PrimaryBuilding>()
					.Where(a =>
						a.Actor.Owner == self.Owner &&
						a.Trait.IsPrimary &&
						a.Actor.Info.TraitInfo<ProductionInfo>().Produces.Contains(productionType)))
					b.Trait.SetPrimaryProducer(b.Actor, false);
			}

			isPrimary = true;

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", "PrimaryBuildingSelected", self.Owner.Faction.InternalName);
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (!isPrimary)
				yield break;

			var tagImages = new Animation(wr.World, info.Image);
			var pal = wr.Palette(info.Palette);
			var tagxyOffset = new int2(0, 6);
			tagImages.PlayRepeating(info.TagSequence);
			var b = self.VisualBounds;
			var center = wr.ScreenPxPosition(self.CenterPosition);
			var tm = wr.Viewport.WorldToViewPx(center + new int2((b.Left + b.Right) / 2, b.Top));
			var pos = tm + tagxyOffset - (0.5f * tagImages.Image.Size).ToInt2();
			yield return new UISpriteRenderable(tagImages.Image, pos, 0, pal, 1f);
		}
	}
}
