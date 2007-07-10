#pragma once

#define safe_release(x) if(x) { (x)->Release(); x = NULL; }

namespace BluntDirectX 
{
	ref class ThrowHelper
	{
	public:
		static void Hr( HRESULT hr )
		{
			throw gcnew InvalidOperationException( String::Format("COM error in DirectX, hr={0}", hr) );
		}
	};
}