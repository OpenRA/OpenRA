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

namespace OpenRA.Widgets
{
	public class MapPreviewWidget : Widget
	{
		public Func<Map> Map = () => null;
		public Func<Dictionary<int2, Color>> SpawnColors = () => new Dictionary<int2, Color>();
		public Action<MouseInput> OnMouseDown = _ => {};
		public Action<int, int2> OnTooltip = (_, __) => { };
		public bool IgnoreMouseInput = false;
		public bool ShowSpawnPoints = true;

		public MapPreviewWidget() : base() { }

		protected MapPreviewWidget(MapPreviewWidget other)
			: base(other)
		{
			lastMap = other.lastMap;
			Map = other.Map;
			SpawnColors = other.SpawnColors;
			ShowSpawnPoints = other.ShowSpawnPoints;
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

		public int2 ConvertToPreview(int2 point)
		{
			var map = Map();
			return new int2(MapRect.X + (int)(PreviewScale*(point.X - map.Bounds.Left)) , MapRect.Y + (int)(PreviewScale*(point.Y - map.Bounds.Top)));
		}

		Sheet mapChooserSheet;
		Sprite mapChooserSprite;
		Map lastMap;
		Rectangle MapRect;
		float PreviewScale = 0;

		public override void Draw()
		{
			var map = Map();
			if (map == null)
				return;

			// Preview unavailable
			if (!Loaded)
			{
				GeneratePreview();
				return;
			}

			if (lastMap != map)
			{
				lastMap = map;

				// Update image data
				Bitmap preview;
				lock (syncRoot)
					preview = Previews[map.Uid];

				if (mapChooserSheet == null || mapChooserSheet.Size.Width != preview.Width || mapChooserSheet.Size.Height != preview.Height)
					mapChooserSheet = new Sheet(new Size(preview.Width, preview.Height));

				mapChooserSheet.Texture.SetData(preview);
				mapChooserSprite = new Sprite(mapChooserSheet, new Rectangle(0, 0, map.Bounds.Width, map.Bounds.Height), TextureChannel.Alpha);
			}

			// Update map rect
			PreviewScale = Math.Min(RenderBounds.Width * 1.0f / map.Bounds.Width, RenderBounds.Height * 1.0f / map.Bounds.Height);
			var size = Math.Max(map.Bounds.Width, map.Bounds.Height);
			var dw = (int)(PreviewScale * (size - map.Bounds.Width)) / 2;
			var dh = (int)(PreviewScale * (size - map.Bounds.Height)) / 2;
			MapRect = new Rectangle(RenderBounds.X + dw, RenderBounds.Y + dh, (int)(map.Bounds.Width * PreviewScale), (int)(map.Bounds.Height * PreviewScale));

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(mapChooserSprite,
				new float2(MapRect.Location),
				new float2(MapRect.Size));

			if (ShowSpawnPoints)
			{
				var colors = SpawnColors();

				var spawnPoints = map.GetSpawnPoints().ToList();
				foreach (var p in spawnPoints)
				{
					var owned = colors.ContainsKey(p);
					var pos = ConvertToPreview(p);
					var sprite = ChromeProvider.GetImage("spawnpoints", owned ? "owned" : "unowned");
					var offset = new int2(-sprite.bounds.Width/2, -sprite.bounds.Height/2);

					if (owned)
						WidgetUtils.FillRectWithColor(new Rectangle(pos.X + offset.X + 2, pos.Y + offset.Y + 2, 12, 12), colors[p]);

					Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos + offset);

					if ((pos - Viewport.LastMousePos).LengthSquared < 64)
					{
						OnTooltip(spawnPoints.IndexOf(p) + 1, pos);
					}
				}
			}
		}

		// Async map preview generation bits
		enum PreviewStatus { Invalid, Uncached, Generating, Cached }
		static Thread previewLoaderThread;
		static object syncRoot = new object();
		static Queue<string> cacheUids = new Queue<string>();
		static readonly Dictionary<string, Bitmap> Previews = new Dictionary<string, Bitmap>();

		void LoadAsyncInternal()
		{
			for (;;)
			{
				string uid;
				lock (syncRoot)
				{
					if (cacheUids.Count == 0)
						break;
					uid = cacheUids.Peek();
				}

				var bitmap = Minimap.RenderMapPreview(Game.modData.AvailableMaps[uid]);
				lock (syncRoot)
				{
					// TODO: We should add previews to a sheet here (with multiple previews per sheet)
					Previews.Add(uid, bitmap);
					cacheUids.Dequeue();
				}
			}
		}

		void GeneratePreview()
		{
			var m = Map();
			if (m == null)
				return;

			var status = Status(m);
			if (status == PreviewStatus.Uncached)
				lock (syncRoot)
					cacheUids.Enqueue(m.Uid);

			if (previewLoaderThread == null || !previewLoaderThread.IsAlive)
			{
				previewLoaderThread = new Thread(LoadAsyncInternal);
				previewLoaderThread.Priority = ThreadPriority.Lowest;
				previewLoaderThread.Start();
			}
		}

		static PreviewStatus Status(Map m)
		{
			if (m == null)
				return PreviewStatus.Invalid;

			lock (syncRoot)
			{
				if (Previews.ContainsKey(m.Uid))
					return PreviewStatus.Cached;

				if (cacheUids.Contains(m.Uid))
					return PreviewStatus.Generating;
			}
			return PreviewStatus.Uncached;
		}

		public bool Loaded { get { return Status(Map()) == PreviewStatus.Cached; } }
	}
}
