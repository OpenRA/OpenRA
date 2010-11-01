using OpenRA.FileFormats.Graphics;

namespace OpenRA.Renderer.Null
{
	class NullVertexBuffer<T> : IVertexBuffer<T>
	{
		public void Bind()
		{
			
		}

		public void SetData(T[] vertices, int length)
		{

		}
	}
}
