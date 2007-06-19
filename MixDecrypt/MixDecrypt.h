// MixDecrypt.h
#pragma once

#pragma unmanaged
#include "mix_decode.h"
#pragma managed

using namespace System;

namespace MixDecrypt {

	public ref class MixDecrypt
	{
	public:
		static array<Byte>^ BlowfishKey( array<Byte>^ src )
		{
			array<Byte>^ dest = gcnew array<Byte>( 56 );
			pin_ptr<Byte> s = &src[0];
			pin_ptr<Byte> d = &dest[0];

			get_blowfish_key( s, d );

			return dest;
		}
	};
}
