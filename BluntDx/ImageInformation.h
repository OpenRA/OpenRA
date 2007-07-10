#pragma once

namespace BluntDirectX { namespace Direct3D
{
	public value class ImageInformation
	{
	private:
		ImageInformation( int width, int height, int depth, bool isCube )
			: Width( width ), Height( height ), Depth( depth ), IsCube( isCube ) {}

	public:
		int Width;
		int Height;
		int Depth;
		bool IsCube;

		static ImageInformation Create( Stream^ stream )
		{
			D3DXIMAGE_INFO info;

			stream->Position = 0;
			array<unsigned char>^ data = gcnew array<unsigned char>( (int)stream->Length );
			stream->Read( data, 0, data->Length );

			HRESULT hr;

			pin_ptr<unsigned char> p = &data[0];

			if (FAILED( hr = D3DXGetImageInfoFromFileInMemory( p, data->Length, &info )))
				throw gcnew InvalidOperationException( "image information load failed" );

			return ImageInformation( info.Width, info.Height, info.Depth, 
				info.ResourceType == D3DRTYPE_CUBETEXTURE );
		}
	};
}}