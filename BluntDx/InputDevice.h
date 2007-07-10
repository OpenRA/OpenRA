#pragma once

namespace BluntDirectX { namespace DirectInput
{
	[Flags]
	public enum class CooperativeLevel
	{
		OnlyWhenFocused = 0x1,
		Exclusive = 0x2,
		DisableWindowsKey = 0x4,
	};

	public enum class DeviceType
	{
		Keyboard,
		Mouse,
		Joystick,
	};

	public ref class DiInputDevice
	{
	private:
		DiInputDevice( IDirectInputDevice8* device ) 
			: device(device), devtype( DeviceType::Joystick ) {}

	internal:
		IDirectInputDevice8* device;
		DeviceType devtype;

	public:
		static property Guid SystemKeyboard
		{
			Guid get()
			{
				return *(Guid*)&GUID_SysKeyboard;
			}
		}

		static property Guid SystemMouse
		{
			Guid get()
			{
				return *(Guid*)&GUID_SysMouse;
			}
		}

		DiInputDevice( InputManager^ manager, Guid guid, DeviceType devtype ) : devtype( devtype )
		{
			HRESULT hr;
			IDirectInputDevice8* _device;

			GUID* p = (GUID*)&guid;

			if (FAILED( hr = manager->di->CreateDevice( *p, &_device, NULL )))
				ThrowHelper::Hr(hr);

			device = _device;
		}

		void SetCooperativeLevel( Control^ host, CooperativeLevel level)
		{
			int _level = (int)level;
			DWORD flags = 0;
			HRESULT hr;

			flags |= ((_level & (int)CooperativeLevel::OnlyWhenFocused) ? DISCL_FOREGROUND : DISCL_BACKGROUND);
			flags |= ((_level & (int)CooperativeLevel::Exclusive) ? DISCL_EXCLUSIVE : DISCL_NONEXCLUSIVE);
			if (_level & (int)CooperativeLevel::DisableWindowsKey)
				flags |= DISCL_NOWINKEY;

			if (FAILED( hr = device->SetCooperativeLevel( (HWND)host->Handle.ToInt32(), flags ) ))
				ThrowHelper::Hr(hr);

			if (devtype == DeviceType::Keyboard)
				device->SetDataFormat( &c_dfDIKeyboard );
			else if (devtype == DeviceType::Mouse)
				device->SetDataFormat( &c_dfDIMouse );
		}

		void Acquire()
		{
			HRESULT hr;
			if (FAILED(hr = device->Acquire()))
				ThrowHelper::Hr(hr);
		}

		void Unacquire()
		{
			HRESULT hr;
			if (FAILED(hr = device->Unacquire()))
				ThrowHelper::Hr(hr);
		}

		void Poll()
		{
			HRESULT hr;
			if (FAILED(hr = device->Poll()))
				ThrowHelper::Hr(hr);
		}

		KeyboardState^ GetKeyboardState()
		{
			char buffer[256];
			HRESULT hr;

			if (FAILED(hr = device->GetDeviceState(sizeof(buffer), (void*)buffer)))
				ThrowHelper::Hr(hr);

			array<bool>^ result = gcnew array<bool>(256);

			for( int i=0; i<256; i++ )
				result[i] = (buffer[i] & 0x80) != 0;

			return gcnew KeyboardState(result);
		}

		MouseState^ GetMouseState()
		{
			MouseData mouse;
			HRESULT hr;

			if (FAILED(hr = device->GetDeviceState(sizeof(mouse), (void*)&mouse)))
				ThrowHelper::Hr(hr);

			return gcnew MouseState(mouse);
		}

		JoystickState^ GetJoystickState()
		{
			DIJOYSTATE state;
			HRESULT hr;

			if (FAILED(hr = device->GetDeviceState(sizeof(state), (void*)&state)))
				ThrowHelper::Hr(hr);

			return gcnew JoystickState( &state );
		}

		~DiInputDevice()
		{
			safe_release( device );
		}

		static DiInputDevice^ GetJoystick( InputManager^ manager )
		{
			IDirectInputDevice8* dev = CreateAndConfigureJoystick( manager->di );
			if (dev == nullptr)
				return nullptr;

			return gcnew DiInputDevice( dev );
		}
	};
}}