using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;

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

		// assumption: its not going to hurt, to draw *all* units.
		// in reality, 500 tanks is going to hurt our perf.

		// assumption: we dont skip around between sheets much. otherwise, our perf is going to SUCK.
		// this can be fixed by pooling vertex/index lists, except that breaks z-ordering
		// across sheets.

		// assumption: when people fix these items, they might update the warning comment?

		public void Draw(Renderer renderer)
		{
			int sprites = 0;
			List<Vertex> vertices = new List<Vertex>();
			List<ushort> indices = new List<ushort>();
			Sheet sheet = null;

			foreach (Actor a in actors)
			{
				if (a.CurrentImages == null)
					continue;

				foreach (SheetRectangle<Sheet> image in a.CurrentImages)
				{
					if( image.sheet != sheet && sprites > 0 && sheet != null )
					{
						DrawBatch( vertices, indices, renderer, sheet );

						vertices = new List<Vertex>();
						indices = new List<ushort>();
						sprites = 0;
					}

					sheet = image.sheet;
					Util.CreateQuad(vertices, indices, a.location, image);

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
	}
}
