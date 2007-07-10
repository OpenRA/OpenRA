#pragma once

namespace BluntDirectX { namespace DirectInput
{
	public enum class Key : unsigned char
	{
		A = DIK_A,	B = DIK_B,	C = DIK_C,
		D = DIK_D,	E = DIK_E,	F = DIK_F,
		G = DIK_G,	H = DIK_H,	I = DIK_I,
		J = DIK_J,	K = DIK_K,	L = DIK_L,
		M = DIK_M,	N = DIK_N,	O = DIK_O,
		P = DIK_P,	Q = DIK_Q,	R = DIK_R,
		S = DIK_S,	T = DIK_T,	U = DIK_U,
		V = DIK_V,	W = DIK_W,	X = DIK_X,
		Y = DIK_Y,	Z = DIK_Z,

		D0 = DIK_0, D1 = DIK_1, D2 = DIK_2, D3 = DIK_3, D4 = DIK_4,
		D5 = DIK_5, D6 = DIK_6, D7 = DIK_7, D8 = DIK_8, D9 = DIK_9,

		ArrowUp = DIK_UPARROW,
		ArrowLeft = DIK_LEFTARROW,
		ArrowRight = DIK_RIGHTARROW,
		ArrowDown = DIK_DOWNARROW,

		Space = DIK_SPACE,

		Escape = DIK_ESCAPE,
		Tab = DIK_TAB,
		Enter = DIK_RETURN,

		Tilde = DIK_GRAVE,

		F1 = DIK_F1,
		F2 = DIK_F2,
		F3 = DIK_F3,
		F4 = DIK_F4,
		F5 = DIK_F5,
		F6 = DIK_F6,
		F7 = DIK_F7,
		F8 = DIK_F8,
		F9 = DIK_F9,
		F10 = DIK_F10,
		F11 = DIK_F11,
		F12 = DIK_F12,

		LeftControl = DIK_LCONTROL,
		LeftShift = DIK_LSHIFT,
		LeftAlt = DIK_LALT,

		Home = DIK_HOME,
		End = DIK_END,
		PageUp = DIK_PGUP,
		PageDown = DIK_PGDN,

		RightControl = DIK_RCONTROL,
		RightShift = DIK_RSHIFT,
		RightAlt = DIK_RALT,
	};

	public ref class KeyboardState
	{
	private:
		array<bool>^ data;

	internal:
		KeyboardState( array<bool>^ data ) : data(data) {}

	public:
		property bool default[ Key ]
		{
			bool get( Key key )
			{
				return data[ (int) key ];
			}
		}
	};
}}