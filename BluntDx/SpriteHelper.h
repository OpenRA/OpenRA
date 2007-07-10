#pragma once

namespace BluntDirectX { namespace Direct3D
{
	public ref class SpriteHelper
	{
	internal:
		ID3DXSprite* sprite;

	public:
		SpriteHelper( GraphicsDevice^ device )
		{
			HRESULT hr;
			ID3DXSprite* s;

			if (FAILED(
				hr = D3DXCreateSprite( device->device, &s )))
				ThrowHelper::Hr( hr );

			sprite = s;
		}

		void SetTransform( float sx, float sy, float tx, float ty )
		{
			D3DXMATRIX m;
			memset( &m, 0, sizeof(m));
			m._11 = sx; m._22 = sy; m._33 = 1; m._44 = 1;
			m._41 = tx; m._42 = ty;

			sprite->SetTransform(&m);
		}

		void Begin()
		{
			sprite->Begin( D3DXSPRITE_ALPHABLEND );
		}

		void End()
		{
			sprite->End();
		}

		void Flush()
		{
			sprite->Flush();
		}

		void Draw( Texture^ texture, int left, int top, int width, int height, int color )
		{
			RECT r = { left, top, width + left, height + top };
			sprite->Draw( (IDirect3DTexture9*)texture->texture, &r, NULL, NULL, color );
		}

		void Draw( Texture^ texture, int color )
		{
			sprite->Draw( (IDirect3DTexture9*)texture->texture, NULL, NULL, NULL, color );
		}

		~SpriteHelper()
		{
			safe_release( sprite );
		}
	};
}}
