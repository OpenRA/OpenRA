using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class Actor
	{
		public PointF location;
		public SheetRectangle<Sheet>[] currentImages;
	}

	class Tree : Actor
	{
		public Tree( TreeReference r, TreeRenderer renderer, Map map )
		{
			location = new PointF(24 * (r.X - map.XOffset), 24 * (r.Y - map.YOffset));
			currentImages = new SheetRectangle<Sheet>[] { renderer.GetImage( r.Image ) };
		}
	}

	class TreeRenderer
	{
		Dictionary<string, SheetRectangle<Sheet>> trees = new Dictionary<string, SheetRectangle<Sheet>>();

		public readonly Sheet sh;

		public TreeRenderer(GraphicsDevice device, Map map, Package package, Palette pal)
		{
			Size pageSize = new Size( 1024, 512 );
			List<Sheet> sheets = new List<Sheet>();

			Provider<Sheet> sheetProvider = delegate
			{
				Sheet sheet = new Sheet(new Bitmap(pageSize.Width, pageSize.Height));
				sheets.Add(sheet);
				return sheet;
			};

			TileSheetBuilder<Sheet> builder = new TileSheetBuilder<Sheet>(pageSize, sheetProvider);

			foreach (TreeReference r in map.Trees)
			{
				if (trees.ContainsKey( r.Image ))
					continue;

				ShpReader reader = new ShpReader(package.GetContent(r.Image + "." + map.Theater.Substring(0,3)));
				Bitmap bitmap = BitmapBuilder.FromBytes(reader[0].Image, reader.Width, reader.Height, pal);

				SheetRectangle<Sheet> rect = builder.AddImage(bitmap.Size);
				using (Graphics g = Graphics.FromImage(rect.sheet.bitmap))
					g.DrawImage(bitmap, rect.origin);

				trees.Add(r.Image, rect);
			}

			foreach (Sheet sheet in sheets)
				sheet.LoadTexture(device);

			sh = sheets[0];
		}

		public SheetRectangle<Sheet> GetImage( string tree )
		{
			return trees[tree];
		}
	}

	class World
	{
		const int spritesPerBatch = 1024;

		List<Actor> actors = new List<Actor>();
		FvfVertexBuffer<Vertex> vb;
		IndexBuffer ib;
		GraphicsDevice device;

		public World(GraphicsDevice device)
		{
			this.device = device;
			this.vb = new FvfVertexBuffer<Vertex>(device, spritesPerBatch * 4, Vertex.Format);
			this.ib = new IndexBuffer(device, spritesPerBatch * 6);
		}

		public void Add(Actor a)
		{
			actors.Add(a);	//todo: protect from concurrent modification
		}

		// assumption: there is only one sheet!
		// some noob needs to fix this!

		// assumption: its not going to hurt, to draw *all* units.
		// in reality, 500 tanks is going to hurt our perf.

		public void Draw()
		{
			int sprites = 0;
			List<Vertex> vertices = new List<Vertex>();
			List<ushort> indices = new List<ushort>();

			foreach (Actor a in actors)
			{
				if (a.currentImages == null)
					continue;

				foreach (SheetRectangle<Sheet> image in a.currentImages)
				{
					int offset = vertices.Count;
					vertices.Add(new Vertex(a.location.X, a.location.Y, 0, U(image, 0), V(image, 0)));
					vertices.Add(new Vertex(a.location.X + image.size.Width, a.location.Y, 0, U(image, 1), V(image, 0)));
					vertices.Add(new Vertex(a.location.X, a.location.Y + image.size.Height, 0, U(image, 0), V(image, 1)));
					vertices.Add(new Vertex(a.location.X + image.size.Width, a.location.Y + image.size.Height, 0, U(image, 1), V(image, 1)));

					indices.Add((ushort)offset);
					indices.Add((ushort)(offset + 1));
					indices.Add((ushort)(offset + 2));

					indices.Add((ushort)(offset + 1));
					indices.Add((ushort)(offset + 3));
					indices.Add((ushort)(offset + 2));

					if (++sprites >= spritesPerBatch)
					{
						DrawBatch(vertices, indices);

						vertices = new List<Vertex>();
						indices = new List<ushort>();
						sprites = 0;
					}
				}
			}

			if (sprites > 0)
				DrawBatch(vertices, indices);
		}

		void DrawBatch(List<Vertex> vertices, List<ushort> indices)
		{
			vb.SetData(vertices.ToArray());
			ib.SetData(indices.ToArray());

			vb.Bind(0);
			ib.Bind();

			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 
				vertices.Count, indices.Count / 3);
		}

		static float U(SheetRectangle<Sheet> s, float u)
		{
			float u0 = (float)(s.origin.X + 0.5f) / (float)s.sheet.bitmap.Width;
			float u1 = (float)(s.origin.X + s.size.Width) / (float)s.sheet.bitmap.Width;

			return (u > 0) ? u1 : u0;// (1 - u) * u0 + u * u1;
		}

		static float V(SheetRectangle<Sheet> s, float v)
		{
			float v0 = (float)(s.origin.Y + 0.5f) / (float)s.sheet.bitmap.Height;
			float v1 = (float)(s.origin.Y + s.size.Height) / (float)s.sheet.bitmap.Height;

			return (v > 0) ? v1 : v0;// return (1 - v) * v0 + v * v1;
		}
	}
}
