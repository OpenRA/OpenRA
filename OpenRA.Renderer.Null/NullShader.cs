using System;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Renderer.Null
{
	public class NullShader : IShader
	{
		public void SetValue(string name, float x, float y)
		{
			
		}

		public void SetValue(string param, ITexture texture)
		{
		}

		public void Commit()
		{
		}

		public void Render(Action a)
		{
		}
	}
}
