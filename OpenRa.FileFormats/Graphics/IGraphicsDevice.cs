using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace OpenRa.FileFormats.Graphics
{
	[AttributeUsage( AttributeTargets.Assembly )]
	public class RendererAttribute : Attribute
	{
		public readonly Type Type;

		public RendererAttribute( Type graphicsDeviceType )
		{
			if( !typeof( IGraphicsDevice ).IsAssignableFrom( graphicsDeviceType ) )
				throw new InvalidOperationException( "Incorrect type in RendererAttribute" );
			Type = graphicsDeviceType;
		}
	}

	public interface IGraphicsDevice
	{
		IVertexBuffer<Vertex> CreateVertexBuffer( int length );
		IIndexBuffer CreateIndexBuffer( int length );
		ITexture CreateTexture( Bitmap bitmap );
		IShader CreateShader( Stream stream );

		Size WindowSize { get; }

		void Begin();
		void End();
		void Clear( Color color );
		void Present();

		void DrawIndexedPrimitives( PrimitiveType type, Range<int> vertexRange, Range<int> indexRange );
		void DrawIndexedPrimitives( PrimitiveType type, int vertexPool, int numPrimitives );

		void EnableScissor( int left, int top, int width, int height );
		void DisableScissor();
	}

	public interface IVertexBuffer<T>
	{
		void Bind();
		void SetData( T[] vertices );
	}

	public interface IIndexBuffer
	{
		void Bind();
		void SetData( ushort[] indices );
	}

	public interface IShader
	{
		void SetValue( string name, float x, float y );
		void SetValue( string param, ITexture texture );
		void Commit();
		void Render( Action a );
	}

	public interface ITexture
	{
		void SetData( Bitmap bitmap );
	}

	public interface IFont
	{
		void DrawText( string text, int2 pos, Color c );

		int2 Measure( string text );
	}

    public enum PrimitiveType
    {
        PointList, 
        LineList, 
        TriangleList,
    }

	public struct Range<T>
	{
		public readonly T Start, End;
		public Range( T start, T end ) { Start = start; End = end; }
	}
}
