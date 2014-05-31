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
	public class SpawnOccupant
	{
		public readonly HSLColor Color;
		public readonly int ClientIndex;
		public readonly string PlayerName;
		public readonly int Team;
		public readonly string Country;
		public readonly int SpawnPoint;

		public SpawnOccupant()
		{
		}
		public SpawnOccupant(Session.Client client)
		{
			Color = client.Color;
			ClientIndex = client.Index;
			PlayerName = client.Name;
			Team = client.Team;
			Country = client.Country;
			SpawnPoint = client.SpawnPoint;
		}
		public SpawnOccupant(GameInformation.Player player)
		{
			Color = player.Color;
			ClientIndex = player.ClientIndex;
			PlayerName = player.Name;
			Team = player.Team;
			Country = player.FactionId;
			SpawnPoint = player.SpawnPoint;
		}
	}

	public class MapPreviewWidget : Widget
	{
		public Func<MapPreview> Preview = () => null;
		public Func<Dictionary<CPos, SpawnOccupant>> SpawnOccupants = () => new Dictionary<CPos, SpawnOccupant>();
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
			SpawnOccupants = other.SpawnOccupants;
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
			minimap = preview.GetMinimap();
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
				var colors = SpawnOccupants().ToDictionary(c => c.Key, c => c.Value.Color.RGB);

				var spawnPoints = preview.SpawnPoints;
				foreach (var p in spawnPoints)
				{
					var owned = colors.ContainsKey(p);
					var pos = ConvertToPreview(p);
					var sprite = ChromeProvider.GetImage("lobby-bits", owned ? "spawn-claimed" : "spawn-unclaimed");
					var offset = new int2(sprite.bounds.Width, sprite.bounds.Height) / 2;

					if (owned)
						WidgetUtils.FillEllipseWithColor(new Rectangle(pos.X - offset.X + 1, pos.Y - offset.Y + 1, sprite.bounds.Width - 2, sprite.bounds.Height - 2), colors[p]);

					Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos - offset);
					var fonts = Game.Renderer.Fonts[ChromeMetrics.Get<string>("SpawnFont")];
					var number = Convert.ToChar('A' + spawnPoints.IndexOf(p)).ToString();
					offset = fonts.Measure(number) / 2;
					offset.Y += 1; // Does not center well vertically for some reason
					fonts.DrawTextWithContrast(number, pos - offset, ChromeMetrics.Get<Color>("SpawnColor"), ChromeMetrics.Get<Color>("SpawnContrastColor"), 1);

					if (((pos - Viewport.LastMousePos).ToFloat2() * offset.ToFloat2()).LengthSquared < 1)
						TooltipSpawnIndex = spawnPoints.IndexOf(p) + 1;
				}
			}
		}

		public bool Loaded { get { return minimap != null; } }
	}
}
