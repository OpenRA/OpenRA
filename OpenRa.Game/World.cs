using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
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

		public void Draw(Renderer renderer)
		{
			int sprites = 0;
			List<Vertex> vertices = new List<Vertex>();
			List<ushort> indices = new List<ushort>();
			Sheet sheet = null;

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

					sheet = image.sheet;

					if (++sprites >= spritesPerBatch)
					{
						DrawBatch(vertices, indices, renderer, sheet);

						vertices = new List<Vertex>();
						indices = new List<ushort>();
						sprites = 0;
					}
				}
			}

			if (sprites > 0)
				DrawBatch(vertices, indices, renderer, sheet);
		}

		void DrawBatch(List<Vertex> vertices, List<ushort> indices, Renderer renderer, Sheet sheet)
		{
			vb.SetData(vertices.ToArray());
			ib.SetData(indices.ToArray());

			renderer.DrawWithShader(ShaderQuality.High,
				delegate
				{
					renderer.DrawBatch(vb, ib,
						new Range<int>(0, vertices.Count),
						new Range<int>(0, indices.Count),
						sheet.texture);
				});
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
