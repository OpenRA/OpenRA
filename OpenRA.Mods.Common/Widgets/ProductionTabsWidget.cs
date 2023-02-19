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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProductionTab
	{
		public string Name;
		public ProductionQueue Queue;
	}

	public class ProductionTabGroup
	{
		public List<ProductionTab> Tabs = new List<ProductionTab>();
		public string Group;
		public int NextQueueName = 1;
		public bool Alert { get { return Tabs.Any(t => t.Queue.AllQueued().Any(i => i.Done)); } }

		public void Update(IEnumerable<ProductionQueue> allQueues)
		{
			var queues = allQueues.Where(q => q.Info.Group == Group).ToList();
			var tabs = new List<ProductionTab>();
			var largestUsedName = 0;

			// Remove stale queues
			foreach (var t in Tabs)
			{
				if (!queues.Contains(t.Queue))
					continue;

				tabs.Add(t);
				queues.Remove(t.Queue);
				largestUsedName = Math.Max(int.Parse(t.Name), largestUsedName);
			}

			NextQueueName = largestUsedName + 1;

			// Add new queues
			foreach (var queue in queues)
				tabs.Add(new ProductionTab()
				{
					Name = NextQueueName++.ToString(),
					Queue = queue
				});
			Tabs = tabs;
		}
	}

	public class ProductionTabsWidget : Widget
	{
		readonly World world;

		public readonly string PaletteWidget = null;
		public readonly string TypesContainer = null;
		public readonly string BackgroundContainer = null;

		public readonly int TabWidth = 30;
		public readonly int ArrowWidth = 20;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");

		public readonly HotkeyReference PreviousProductionTabKey = new HotkeyReference();
		public readonly HotkeyReference NextProductionTabKey = new HotkeyReference();

		public readonly Dictionary<string, ProductionTabGroup> Groups;

		public string ArrowButton = "button";
		public string TabButton = "button";

		public string Background = "panel-black";
		public string Decorations = "scrollpanel-decorations";
		public readonly string DecorationScrollLeft = "left";
		public readonly string DecorationScrollRight = "right";
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getLeftArrowImage;
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getRightArrowImage;

		int contentWidth = 0;
		float listOffset = 0;
		bool leftPressed = false;
		bool rightPressed = false;
		SpriteFont font;
		Rectangle leftButtonRect;
		Rectangle rightButtonRect;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;
		string queueGroup;

		[ObjectCreator.UseCtor]
		public ProductionTabsWidget(World world)
		{
			this.world = world;

			Groups = world.Map.Rules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>())
				.Select(q => q.Group).Distinct().ToDictionary(g => g, g => new ProductionTabGroup() { Group = g });

			// Only visible if the production palette has icons to display
			IsVisible = () => queueGroup != null && Groups[queueGroup].Tabs.Count > 0;

			paletteWidget = Exts.Lazy(() => Ui.Root.Get<ProductionPaletteWidget>(PaletteWidget));
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			var rb = RenderBounds;
			leftButtonRect = new Rectangle(rb.X, rb.Y, ArrowWidth, rb.Height);
			rightButtonRect = new Rectangle(rb.Right - ArrowWidth, rb.Y, ArrowWidth, rb.Height);
			font = Game.Renderer.Fonts["TinyBold"];

			getLeftArrowImage = WidgetUtils.GetCachedStatefulImage(Decorations, DecorationScrollLeft);
			getRightArrowImage = WidgetUtils.GetCachedStatefulImage(Decorations, DecorationScrollRight);
		}

		public bool SelectNextTab(bool reverse)
		{
			if (queueGroup == null)
				return true;

			// Prioritize alerted queues
			var queues = Groups[queueGroup].Tabs.Select(t => t.Queue)
					.OrderByDescending(q => q.AllQueued().Any(i => i.Done) ? 1 : 0)
					.ToList();

			if (reverse) queues.Reverse();

			CurrentQueue = queues.SkipWhile(q => q != CurrentQueue)
				.Skip(1).FirstOrDefault() ?? queues.FirstOrDefault();

			return true;
		}

		public void PickUpCompletedBuilding()
		{
			// This is called from ProductionTabsLogic
			paletteWidget.Value.PickUpCompletedBuilding();
		}

		public string QueueGroup
		{
			get => queueGroup;

			set
			{
				listOffset = 0;
				queueGroup = value;
				SelectNextTab(false);
			}
		}

		public ProductionQueue CurrentQueue
		{
			get => paletteWidget.Value.CurrentQueue;

			set
			{
				paletteWidget.Value.CurrentQueue = value;
				queueGroup = value?.Info.Group;

				// TODO: Scroll tabs so selected queue is visible
			}
		}

		public override void Draw()
		{
			var tabs = Groups[queueGroup].Tabs.Where(t => t.Queue.BuildableItems().Any());

			if (!tabs.Any())
				return;

			var rb = RenderBounds;

			var leftDisabled = listOffset >= 0;
			var leftHover = Ui.MouseOverWidget == this && leftButtonRect.Contains(Viewport.LastMousePos);
			var rightDisabled = listOffset <= Bounds.Width - rightButtonRect.Width - leftButtonRect.Width - contentWidth;
			var rightHover = Ui.MouseOverWidget == this && rightButtonRect.Contains(Viewport.LastMousePos);

			WidgetUtils.DrawPanel(Background, rb);
			ButtonWidget.DrawBackground(ArrowButton, leftButtonRect, leftDisabled, leftPressed, leftHover, false);
			ButtonWidget.DrawBackground(ArrowButton, rightButtonRect, rightDisabled, rightPressed, rightHover, false);

			var leftArrowImage = getLeftArrowImage.Update((leftDisabled, leftPressed, leftHover, false, false));
			WidgetUtils.DrawSprite(leftArrowImage,
				new float2(leftButtonRect.Left + (int)((leftButtonRect.Width - leftArrowImage.Size.X) / 2), leftButtonRect.Top + (int)((leftButtonRect.Height - leftArrowImage.Size.Y) / 2)));

			var rightArrowImage = getRightArrowImage.Update((rightDisabled, rightPressed, rightHover, false, false));
			WidgetUtils.DrawSprite(rightArrowImage,
				new float2(rightButtonRect.Left + (int)((rightButtonRect.Width - rightArrowImage.Size.X) / 2), rightButtonRect.Top + (int)((rightButtonRect.Height - rightArrowImage.Size.Y) / 2)));

			// Draw tab buttons
			Game.Renderer.EnableScissor(new Rectangle(leftButtonRect.Right, rb.Y + 1, rightButtonRect.Left - leftButtonRect.Right - 1, rb.Height));
			var origin = new int2(leftButtonRect.Right - 1 + (int)listOffset, leftButtonRect.Y);
			contentWidth = 0;

			foreach (var tab in tabs)
			{
				var rect = new Rectangle(origin.X + contentWidth, origin.Y, TabWidth, rb.Height);
				var hover = !leftHover && !rightHover && Ui.MouseOverWidget == this && rect.Contains(Viewport.LastMousePos);
				var highlighted = tab.Queue == CurrentQueue;
				ButtonWidget.DrawBackground(TabButton, rect, false, false, hover, highlighted);
				contentWidth += TabWidth - 1;

				var textSize = font.Measure(tab.Name);
				var position = new int2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2);
				font.DrawTextWithContrast(tab.Name, position, tab.Queue.AllQueued().Any(i => i.Done) ? Color.Gold : Color.White, Color.Black, 1);
			}

			Game.Renderer.DisableScissor();
		}

		void Scroll(int amount)
		{
			listOffset += amount * Game.Settings.Game.UIScrollSpeed;
			listOffset = Math.Min(0, Math.Max(Bounds.Width - rightButtonRect.Width - leftButtonRect.Width - contentWidth, listOffset));
		}

		// Is added to world.ActorAdded by the SidebarLogic handler
		public void ActorChanged(Actor a)
		{
			if (a.Info.HasTraitInfo<ProductionQueueInfo>())
			{
				var allQueues = a.World.ActorsWithTrait<ProductionQueue>()
					.Where(p => p.Actor.Owner == p.Actor.World.LocalPlayer && p.Actor.IsInWorld && p.Trait.Enabled)
					.Select(p => p.Trait).ToList();

				foreach (var g in Groups.Values)
					g.Update(allQueues);

				if (queueGroup == null)
					return;

				// Queue destroyed, was last of type: switch to a new group
				if (Groups[queueGroup].Tabs.Count == 0)
					QueueGroup = Groups.Where(g => g.Value.Tabs.Count > 0)
						.Select(g => g.Key).FirstOrDefault();

				// Queue destroyed, others of same type: switch to another tab
				else if (!Groups[queueGroup].Tabs.Select(t => t.Queue).Contains(CurrentQueue))
					SelectNextTab(false);
			}
		}

		public override void Tick()
		{
			if (leftPressed) Scroll(1);
			if (rightPressed) Scroll(-1);
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			leftPressed = rightPressed = false;
			return base.YieldMouseFocus(mi);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll)
			{
				Scroll(mi.Delta.Y);
				return true;
			}

			if (mi.Button != MouseButton.Left)
				return true;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return true;

			if (!HasMouseFocus)
				return true;

			if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
				return YieldMouseFocus(mi);

			leftPressed = leftButtonRect.Contains(mi.Location);
			rightPressed = rightButtonRect.Contains(mi.Location);
			var leftDisabled = listOffset >= 0;
			var rightDisabled = listOffset <= Bounds.Width - rightButtonRect.Width - leftButtonRect.Width - contentWidth;

			if (leftPressed || rightPressed)
			{
				if ((leftPressed && !leftDisabled) || (rightPressed && !rightDisabled))
					Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
				else
					Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickDisabledSound, null);
			}

			// Check production tabs
			var offsetloc = mi.Location - new int2(leftButtonRect.Right - 1 + (int)listOffset, leftButtonRect.Y);
			if (offsetloc.X > 0 && offsetloc.X < contentWidth)
			{
				CurrentQueue = Groups[queueGroup].Tabs[offsetloc.X / (TabWidth - 1)].Queue;
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
			}

			return true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			if (PreviousProductionTabKey.IsActivatedBy(e))
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
				return SelectNextTab(true);
			}

			if (NextProductionTabKey.IsActivatedBy(e))
			{
				Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
				return SelectNextTab(false);
			}

			return false;
		}
	}
}
