#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;

namespace OpenRA.Widgets
{
	public class MapPreviewWidget : Widget
	{
		public Func<MapPreview> Preview = () => null;
		public Func<Dictionary<CPos, Session.Client>> SpawnClients = () => new Dictionary<CPos, Session.Client>();
		public Action<MouseInput> OnMouseDown = _ => {};
		public bool IgnoreMouseInput = false;
		public bool ShowSpawnPoints = true;

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "SPAWN_TOOLTIP";
		Lazy<TooltipContainerWidget> tooltipContainer;
		public int TooltipSpawnIndex = -1;

		Rectangle MapRect;
		float PreviewScale = 0;

		public MapPreviewWidget()
		{
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected MapPreviewWidget(MapPreviewWidget other)
			: base(other)
		{
			Preview = other.Preview;
			SpawnClients = other.SpawnClients;
			ShowSpawnPoints = other.ShowSpawnPoints;
			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override Widget Clone() { return new MapPreviewWidget(this); }

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (IgnoreMouseInput)
				return base.HandleMouseInput(mi);

			if (mi.Event != MouseInputEvent.Down)
				return false;

			OnMouseDown(mi);
			return true;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() {{ "preview", this }});
		}

		public override void MouseExited()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.RemoveTooltip();
		}

		public int2 ConvertToPreview(CPos point)
		{
			var preview = Preview();
			return new int2(MapRect.X + (int)(PreviewScale*(point.X - preview.Bounds.Left)) , MapRect.Y + (int)(PreviewScale*(point.Y - preview.Bounds.Top)));
		}

		Sprite minimap;
		public override void Draw()
		{
			var preview = Preview();
			if (preview == null)
				return;

			// Stash a copy of the minimap to ensure consistency
			// (it may be modified by another thread)
			minimap = preview.Minimap;
			if (minimap == null)
				return;

			// Update map rect
			PreviewScale = Math.Min(RenderBounds.Width / minimap.size.X, RenderBounds.Height / minimap.size.Y);
			var w = (int)(PreviewScale * minimap.size.X);
			var h = (int)(PreviewScale * minimap.size.Y);
			var x = RenderBounds.X + (RenderBounds.Width - w) / 2;
			var y = RenderBounds.Y + (RenderBounds.Height - h) / 2;
			MapRect = new Rectangle(x, y, w, h);

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(minimap, new float2(MapRect.Location), new float2(MapRect.Size));

			TooltipSpawnIndex = -1;
			if (ShowSpawnPoints)
			{
				var colors = SpawnClients().ToDictionary(c => c.Key, c => c.Value.Color.RGB);

				var spawnPoints = preview.SpawnPoints;
				foreach (var p in spawnPoints)
				{
					var owned = colors.ContainsKey(p);
					var pos = ConvertToPreview(p);
					var sprite = ChromeProvider.GetImage("lobby-bits", owned ? "spawn-claimed" : "spawn-unclaimed");
					var offset = new int2(-sprite.bounds.Width/2, -sprite.bounds.Height/2);

					if (owned)
						WidgetUtils.FillRectWithColor(new Rectangle(pos.X + offset.X + 2, pos.Y + offset.Y + 2, 12, 12), colors[p]);

					Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos + offset);
					Game.Renderer.Fonts[ChromeMetrics.Get<string>("SpawnFont")].DrawTextWithContrast(Convert.ToString(spawnPoints.IndexOf(p) + 1), new int2(pos.X + offset.X + 4, pos.Y + offset.Y - 15), ChromeMetrics.Get<Color>("SpawnColor"), ChromeMetrics.Get<Color>("SpawnContrastColor"), 2);

					if ((pos - Viewport.LastMousePos).LengthSquared < 64)
						TooltipSpawnIndex = spawnPoints.IndexOf(p) + 1;
				}
			}
		}

		public bool Loaded { get { return minimap != null; } }
	}
}
