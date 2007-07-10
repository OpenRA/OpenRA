#pragma once

namespace BluntDirectX { namespace DirectInput
{
	public ref class InputManager
	{
	private:

	internal:
		IDirectInput8* di;

	public:
		InputManager() 
		{
			HINSTANCE hinst = GetModuleHandle(0);
			HRESULT hr;
			IDirectInput8* _di;

			if (FAILED( hr = DirectInput8Create( hinst, DIRECTINPUT_VERSION, IID_IDirectInput8, (void**)&_di, NULL )))
				ThrowHelper::Hr(hr);

			di = _di;
		}

		~InputManager()
		{
			safe_release(di);
		}
	};
}}