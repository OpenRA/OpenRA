#pragma once

namespace BluntDirectX { namespace Direct3D
{
	public ref class Texture
	{
	private:
		Texture( IDirect3DBaseTexture9* texture ) : texture(texture) {}

	internal:
		IDirect3DBaseTexture9* texture;

	public:
		static Texture^ Create( Stream^ stream, GraphicsDevice^ device )
		{
			stream->Position = 0;
			array<unsigned char>^ data = gcnew array<unsigned char>( (int)stream->Length );
			stream->Read( data, 0, data->Length );

			HRESULT hr;
			IDirect3DBaseTexture9* tex;

			pin_ptr<unsigned char> p = &data[0];

			if (FAILED( hr = D3DXCreateTextureFromFileInMemory( device->device, p, data->Length, (IDirect3DTexture9**)&tex ) ))
				throw gcnew InvalidOperationException("Texture load failed");

			return gcnew Texture( tex );
		}
		
		static Texture^ CreateCube( Stream^ stream, GraphicsDevice^ device )
		{
			stream->Position = 0;
			array<unsigned char>^ data = gcnew array<unsigned char>((int)stream->Length);
			stream->Read( data, 0, data->Length );

			HRESULT hr;
			IDirect3DBaseTexture9* tex;

			pin_ptr<unsigned char> p = &data[0];

			if (FAILED( hr = D3DXCreateCubeTextureFromFileInMemory( device->device, p, data->Length, (IDirect3DCubeTexture9**)&tex ) ))
				throw gcnew InvalidOperationException("Texture load failed");

			return gcnew Texture( tex );
		}
	};
}
}