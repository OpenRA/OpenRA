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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Render chrono vortex")]
	public class ChronoVortexRendererInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new ChronoVortexRenderer(init.Self); }
	}

	public sealed class ChronoVortexRenderer : IRenderPostProcessPass, INotifyActorDisposing
	{
		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<RenderPostProcessPassTexturedVertex> vortexBuffer;
		readonly Sheet vortexSheet;
		readonly List<(float3, int)> vortices = [];

		public ChronoVortexRenderer(Actor self)
		{
			renderer = Game.Renderer;
			shader = renderer.CreateShader(new RenderPostProcessPassTexturedShaderBindings("vortex"));

			vortexSheet = new Sheet(SheetType.BGRA, new Size(512, 512));
			var vertices = new RenderPostProcessPassTexturedVertex[288];

			var data = vortexSheet.GetData();
			var j = 0;
			for (var f = 0; f < 48; f++)
			{
				var row = f / 8;
				var col = f % 8;

				using (var stream = self.World.Map.Open($"hole{f:D04}.lut"))
				{
					for (var y = 0; y < 64; y++)
					{
						var i = 2048 * (64 * row + y) + 256 * col;
						for (var x = 0; x < 64; x++)
						{
							data[i++] = (byte)(stream.ReadUInt8() + 128 - x);
							data[i++] = (byte)(stream.ReadUInt8() + 128 - y);
							data[i++] = stream.ReadUInt8();
							data[i++] = 255;
						}
					}
				}

				var tl = new float2(col, row) / 8;
				var br = new float2(col + 1, row + 1) / 8;
				vertices[j++] = new RenderPostProcessPassTexturedVertex(-32, -32, tl.X, tl.Y);
				vertices[j++] = new RenderPostProcessPassTexturedVertex(32, -32, br.X, tl.Y);
				vertices[j++] = new RenderPostProcessPassTexturedVertex(32, 32, br.X, br.Y);
				vertices[j++] = new RenderPostProcessPassTexturedVertex(32, 32, br.X, br.Y);
				vertices[j++] = new RenderPostProcessPassTexturedVertex(-32, 32, tl.X, br.Y);
				vertices[j++] = new RenderPostProcessPassTexturedVertex(-32, -32, tl.X, tl.Y);
			}

			vortexBuffer = renderer.CreateVertexBuffer(vertices, false);
			vortexSheet.CommitBufferedData();
		}

		public void DrawVortex(float3 pos, int frame)
		{
			vortices.Add((pos, frame));
		}

		PostProcessPassType IRenderPostProcessPass.Type => PostProcessPassType.AfterWorld;
		bool IRenderPostProcessPass.Enabled => vortices.Count > 0;

		void IRenderPostProcessPass.Draw(WorldRenderer wr)
		{
			var scroll = wr.Viewport.TopLeft;
			var size = renderer.WorldFrameBufferSize;
			var width = 2f / (renderer.WorldDownscaleFactor * size.Width);
			var height = 2f / (renderer.WorldDownscaleFactor * size.Height);

			shader.SetVec("Scroll", scroll.X, scroll.Y);
			shader.SetVec("p1", width, height);
			shader.SetVec("p2", -1, -1);
			shader.SetTexture("WorldTexture", Game.Renderer.WorldBufferSnapshot());
			shader.SetTexture("VortexTexture", vortexSheet.GetTexture());
			shader.PrepareRender();
			foreach (var (pos, frame) in vortices)
			{
				shader.SetVec("Pos", pos.X, pos.Y);
				renderer.DrawBatch(vortexBuffer, shader, 6 * frame, 6, PrimitiveType.TriangleList);
			}

			vortices.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			vortexSheet.Dispose();
			vortexBuffer.Dispose();
		}
	}
}
