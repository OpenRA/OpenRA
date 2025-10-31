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
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Renders sonic blasts")]
	public class SonicBlastRendererInfo : TraitInfo
	{
		[Desc("Diameter of the sonic effect circle.")]
		public readonly int Size = 16;

		[Desc("Amount to scale the visuals within the effect circle.")]
		public readonly float Zoom = 2.5f;

		public override object Create(ActorInitializer init) { return new SonicBlastRenderer(this); }
	}

	public sealed class SonicBlastRenderer : IRenderPostProcessPass, INotifyActorDisposing
	{
		public readonly SonicBlastRendererInfo Info;

		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<RenderPostProcessPassTexturedVertex> buffer;
		readonly List<float3> positions = [];

		public SonicBlastRenderer(SonicBlastRendererInfo info)
		{
			Info = info;
			renderer = Game.Renderer;
			shader = renderer.CreateShader(new RenderPostProcessPassTexturedShaderBindings("sonic"));

			var r = 0.5f * info.Size;
			shader.SetVec("Scale", r * (1f / info.Zoom - 1));
			var vertices = new RenderPostProcessPassTexturedVertex[]
			{
				new(-r, -r, -1, -1),
				new(r, -r, 1, -1),
				new(r, r, 1, 1),
				new(r, r, 1, 1),
				new(-r, r, -1, 1),
				new(-r, -r, -1, -1)
			};

			buffer = renderer.CreateVertexBuffer(vertices, false);
		}

		public void Draw(float3 pos)
		{
			positions.Add(pos);
		}

		PostProcessPassType IRenderPostProcessPass.Type => PostProcessPassType.AfterWorld;
		bool IRenderPostProcessPass.Enabled => positions.Count > 0;

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
			shader.PrepareRender();
			foreach (var pos in positions)
			{
				shader.SetVec("Pos", pos.X, pos.Y);
				renderer.DrawBatch(buffer, shader, 0, 6, PrimitiveType.TriangleList);
			}

			positions.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			buffer.Dispose();
		}
	}
}
