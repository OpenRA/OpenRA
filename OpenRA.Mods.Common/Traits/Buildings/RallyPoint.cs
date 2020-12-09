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
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to waypoint units after production or repair is finished.")]
	public class RallyPointInfo : TraitInfo
	{
		public readonly string Image = "rallypoint";

		[Desc("Width (in pixels) of the rallypoint line.")]
		public readonly int LineWidth = 1;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		public readonly string FlagSequence = "flag";

		[SequenceReference(nameof(Image), allowNullImage: true)]
		public readonly string CirclesSequence = "circles";

		[Desc("Cursor to display when rally point can be set.")]
		public readonly string Cursor = "ability";

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom indicator palette name")]
		public readonly string Palette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = true;

		[Desc("A list of 0 or more offsets defining the initial rally point path.")]
		public readonly CVec[] Path = { };

		[NotificationReference("Speech")]
		[Desc("The speech notification to play when setting a new rallypoint.")]
		public readonly string Notification = null;

		public override object Create(ActorInitializer init) { return new RallyPoint(init.Self, this); }
	}

	public class RallyPoint : IIssueOrder, IResolveOrder, INotifyOwnerChanged, INotifyCreated
	{
		const string OrderID = "SetRallyPoint";

		public List<CPos> Path;

		public RallyPointInfo Info;
		public string PaletteName { get; private set; }

		const uint ForceSet = 1;

		public void ResetPath(Actor self)
		{
			Path = Info.Path.Select(p => self.Location + p).ToList();
		}

		public RallyPoint(Actor self, RallyPointInfo info)
		{
			Info = info;
			ResetPath(self);
			PaletteName = info.IsPlayerPalette ? info.Palette + self.Owner.InternalName : info.Palette;
		}

		void INotifyCreated.Created(Actor self)
		{
			self.World.Add(new RallyPointIndicator(self, this));
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (Info.IsPlayerPalette)
				PaletteName = Info.Palette + newOwner.InternalName;

			ResetPath(self);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RallyPointOrderTargeter(Info.Cursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == OrderID)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.Notification, self.Owner.Faction.InternalName);

				return new Order(order.OrderID, self, target, queued)
				{
					SuppressVisualFeedback = true,
					ExtraData = ((RallyPointOrderTargeter)order).ForceSet ? ForceSet : 0
				};
			}

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderID)
				return;

			if (!order.Queued)
				Path.Clear();

			Path.Add(self.World.Map.CellContaining(order.Target.CenterPosition));
		}

		public static bool IsForceSet(Order order)
		{
			return order.OrderString == OrderID && order.ExtraData == ForceSet;
		}

		class RallyPointOrderTargeter : IOrderTargeter
		{
			readonly string cursor;

			public RallyPointOrderTargeter(string cursor)
			{
				this.cursor = cursor;
			}

			public string OrderID { get { return "SetRallyPoint"; } }
			public int OrderPriority { get { return 0; } }
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }
			public bool ForceSet { get; private set; }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, in Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var location = self.World.Map.CellContaining(target.CenterPosition);
				if (self.World.Map.Contains(location))
				{
					cursor = this.cursor;

					// Notify force-set 'RallyPoint' order watchers with Ctrl and only if this is the only building of its type selected
					if (modifiers.HasModifier(TargetModifiers.ForceAttack))
					{
						var selfName = self.Info.Name;
						if (!self.World.Selection.Actors.Any(a => a.Info.Name == selfName && a.ActorID != self.ActorID))
							ForceSet = true;
					}

					return true;
				}

				return false;
			}
		}
	}
}
