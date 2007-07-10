using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenRa.Game
{
	class MainWindow : Form
	{
		readonly GraphicsDevice device;
		readonly Map map;
		readonly TileSet tileSet;
		
		Palette pal;
		Package TileMix;
		string TileSuffix;

		const string mapName = "scm12ea.ini";
		const string shaderName = "diffuse.fx";

		Dictionary<TileReference, SheetRectangle<Sheet>> tileMapping =
			new Dictionary<TileReference, SheetRectangle<Sheet>>();

		FvfVertexBuffer<Vertex> vertexBuffer;

		Dictionary<Sheet, IndexBuffer> drawBatches = new Dictionary<Sheet, IndexBuffer>();

		Effect effect;
		IntPtr texture, scroll;
		SpriteHelper spriteHelper;
		FontHelper fontHelper;

		void LoadTextures()
		{
			List<Sheet> tempSheets = new List<Sheet>();

			Provider<Sheet> sheetProvider = delegate
			{
				Sheet t = new Sheet( new Bitmap(256, 256));
				tempSheets.Add(t);
				return t;
			};

			TileSheetBuilder<Sheet> builder = new TileSheetBuilder<Sheet>( new Size(256,256), sheetProvider );

			for( int i = 0; i < 128; i++ )
				for (int j = 0; j < 128; j++)
				{
					TileReference tileRef = map.MapTiles[i, j];

					if (!tileMapping.ContainsKey(tileRef))
					{
						SheetRectangle<Sheet> rect = builder.AddImage(new Size(24, 24));
						Bitmap srcImage = tileSet.tiles[ tileRef.tile ].GetTile( tileRef.image );
						using (Graphics g = Graphics.FromImage(rect.sheet.bitmap))
							g.DrawImage(srcImage, rect.origin);

						tileMapping.Add(tileRef, rect);
					}
				}

			foreach (Sheet s in tempSheets)
				s.LoadTexture(device);
		}

		float U(SheetRectangle<Sheet> s, float u)
		{
			float u0 = (float)(s.origin.X + 0.5f) / (float)s.sheet.bitmap.Width;
			float u1 = (float)(s.origin.X + s.size.Width) / (float)s.sheet.bitmap.Width;

			return (u > 0) ? u1 : u0;// (1 - u) * u0 + u * u1;
		}

		float V(SheetRectangle<Sheet> s, float v)
		{
			float v0 = (float)(s.origin.Y + 0.5f) / (float)s.sheet.bitmap.Height;
			float v1 = (float)(s.origin.Y + s.size.Height) / (float)s.sheet.bitmap.Height;

			return (v > 0) ? v1 : v0;// return (1 - v) * v0 + v * v1;
		}

		void LoadVertexBuffer()
		{
			Dictionary<Sheet, List<ushort>> indexMap = new Dictionary<Sheet, List<ushort>>();
			Vertex[] vertices = new Vertex[4 * 128 * 128];

			for( int i = map.XOffset; i < map.XOffset+ map.Width; i++ )
				for (int j = map.YOffset; j < map.YOffset + map.Height; j++)
				{
					SheetRectangle<Sheet> tile = tileMapping[map.MapTiles[i, j]];

					ushort offset = (ushort)(4 * (i * 128 + j));

					vertices[offset] = new Vertex(24 * i, 24 * j, 0, U(tile, 0), V(tile, 0));
					vertices[offset + 1] = new Vertex(24 + 24 * i, 24 * j, 0, U(tile, 1), V(tile, 0));
					vertices[offset + 2] = new Vertex(24 * i, 24 + 24 * j, 0, U(tile, 0), V(tile, 1));
					vertices[offset + 3] = new Vertex(24 + 24 * i, 24 + 24 * j, 0, U(tile, 1), V(tile, 1));

					List<ushort> indexList;
					if (!indexMap.TryGetValue(tile.sheet, out indexList))
						indexMap.Add(tile.sheet, indexList = new List<ushort>());

					indexList.Add(offset);
					indexList.Add((ushort)(offset + 1));
					indexList.Add((ushort)(offset + 2));

					indexList.Add((ushort)(offset + 1));
					indexList.Add((ushort)(offset + 3));
					indexList.Add((ushort)(offset + 2));
				}

			vertexBuffer = new FvfVertexBuffer<Vertex>(device, vertices.Length, Vertex.Format);
			vertexBuffer.SetData(vertices);

			foreach (KeyValuePair<Sheet, List<ushort>> p in indexMap)
			{
				IndexBuffer indexBuffer = new IndexBuffer(device, p.Value.Count);
				indexBuffer.SetData(p.Value.ToArray());
				drawBatches.Add(p.Key, indexBuffer);
			}
		}

		public MainWindow()
		{
			ClientSize = new Size(640, 480);

			Visible = true;

			//device = GraphicsDevice.Create(this, ClientSize.Width, ClientSize.Height, true, false);

			device = GraphicsDevice.Create(this, 1280, 800, false, false);

			IniFile mapFile = new IniFile(File.OpenRead("../../../" + mapName));
			map = new Map(mapFile);

			Text = string.Format("OpenRA - {0} - {1}", map.Title, mapName);

			tileSet = LoadTileSet(map);

			LoadTextures();
			LoadVertexBuffer();

			effect = new Effect(device, File.OpenRead("../../../" + shaderName));
			texture = effect.GetHandle("DiffuseTexture");
			scroll = effect.GetHandle("Scroll");

			spriteHelper = new SpriteHelper(device);
			fontHelper = new FontHelper(device, "Tahoma", 10, false);

			Clock.Reset();
		}

		internal void Run()
		{
			while (Created && Visible)
			{
				Frame();
				Application.DoEvents();
			}
		}

		PointF scrollPos = new PointF(1, 5);
		PointF oldPos;
		int x1,y1;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			x1 = e.X;
			y1 = e.Y;
			oldPos = scrollPos;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.Button != 0)
			{
				int dx = x1 - e.X;
				int dy = y1 - e.Y;
				scrollPos = oldPos;
				scrollPos.X += (float)dx / (ClientSize.Width / 2);
				scrollPos.Y += (float)dy / (ClientSize.Height / 2);
			}
		}

		static T First<T>(IEnumerable<T> src)
		{
			IEnumerator<T> enumerator = src.GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current : default(T);
		}

		static T Nth<T>(IEnumerable<T> src, int n)
		{
			IEnumerator<T> enumerator = src.GetEnumerator();
			bool ok = false;
			while (n-- > 0)
				ok = enumerator.MoveNext();

			return ok ? enumerator.Current : default(T);
		}

		int n = 1;

		void Frame()
		{
			Clock.StartFrame();
			device.Begin();
			device.Clear( 0, Surfaces.Color );

			vertexBuffer.Bind(0);

			effect.Quality = ShaderQuality.Low;
			effect.Begin();
			effect.BeginPass(0);

			effect.SetValue(scroll, scrollPos);

			KeyValuePair<Sheet, IndexBuffer> batch = Nth(drawBatches, n);
			//foreach (KeyValuePair<Sheet, IndexBuffer> batch in drawBatches)
			{
				effect.SetTexture(texture, batch.Key.texture);
				effect.Commit();

				batch.Value.Bind();

				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 
					vertexBuffer.Size, batch.Value.Size / 3);
			}

			effect.EndPass();
			effect.End();

			spriteHelper.Begin();
			fontHelper.Draw(spriteHelper, "fps: " + Clock.FrameRate, 0, 0, Color.White.ToArgb());
			spriteHelper.End();

			device.End();
			device.Present();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.KeyCode == Keys.C)
				Clock.Reset();

			if (e.KeyCode == Keys.A)
				n++;

			if (e.KeyCode == Keys.Z)
				if (--n < 1)
					n = 1;
		}

		TileSet LoadTileSet(Map currentMap)
		{
			string theaterName = currentMap.Theater;
			if (theaterName.Length > 8)
				theaterName = theaterName.Substring(0, 8);

			pal = new Palette(File.OpenRead("../../../" + theaterName + ".pal"));
			TileMix = new Package("../../../" + theaterName + ".mix");
			TileSuffix = "." + theaterName.Substring(0, 3);

			return new TileSet(TileMix, TileSuffix, pal);
		}
	}
}
