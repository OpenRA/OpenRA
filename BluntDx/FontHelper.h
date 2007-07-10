#pragma once

namespace BluntDirectX { namespace Direct3D
{
	public ref class FontHelper
	{
	private:
		static int MakeLogicalFontHeight( int emSize )
		{
			int result;
			HDC hdc = GetDC(NULL);
			result = -MulDiv( emSize, GetDeviceCaps( hdc, LOGPIXELSY ), 72 );
			ReleaseDC( NULL, hdc );

			return result;
		}

	internal:
		ID3DXFont* font;

	public:
		FontHelper( GraphicsDevice^ device, String^ face, int emSize, bool bold )
		{
			HRESULT hr;
			ID3DXFont* f;

			pin_ptr<const wchar_t> uface = PtrToStringChars(face);

			int height = MakeLogicalFontHeight( emSize );

			if (FAILED(
				hr = D3DXCreateFont( device->device, height, 0, bold ? FW_BOLD : FW_NORMAL, 1, false, 
				DEFAULT_CHARSET, OUT_STRING_PRECIS, ANTIALIASED_QUALITY, DEFAULT_PITCH | FF_DONTCARE,
				uface, &f)))
				ThrowHelper::Hr(hr);

			font = f;
		}

		void Draw( SpriteHelper^ sprite, String^ text, int x, int y, int color )
		{
			pin_ptr<const wchar_t> utext = PtrToStringChars(text);
			RECT r = {x, y, x+1, y+1};
			font->DrawText( sprite->sprite, utext, -1, &r, DT_LEFT | DT_NOCLIP, color );
		}

		Size MeasureText( SpriteHelper^ sprite, String^ text )
		{
			pin_ptr<const wchar_t> utext = PtrToStringChars(text);
			RECT r = {0,0,1,1};
			int height = font->DrawText( sprite->sprite, utext, -1, &r, DT_LEFT | DT_CALCRECT, 0 );

			return Size( r.right - r.left, height );
		}
	};
}}