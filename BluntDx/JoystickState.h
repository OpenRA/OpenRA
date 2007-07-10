#pragma once

namespace BluntDirectX { namespace DirectInput {

#define joy_axis_limit	1000

#pragma unmanaged

	int CALLBACK EnumDevicesCb( const DIDEVICEINSTANCE* inst, void* data )
	{
		DIDEVICEINSTANCE* pdata = (DIDEVICEINSTANCE*)data;
		memcpy( pdata, inst, sizeof( DIDEVICEINSTANCE ) );
		return DIENUM_STOP;
	}

	int CALLBACK EnumDeviceObjectsCb( const DIDEVICEOBJECTINSTANCE* inst, void* data )
	{
		IDirectInputDevice8* dev = (IDirectInputDevice8*) data;

		DIPROPRANGE p;
		p.diph.dwSize = sizeof(DIPROPRANGE);
		p.diph.dwHeaderSize = sizeof(DIPROPHEADER);
		p.diph.dwHow = DIPH_BYID;
		p.diph.dwObj = inst->dwType;
		p.lMin = -joy_axis_limit;
		p.lMax = joy_axis_limit;

		HRESULT hr;
		if (FAILED(hr = dev->SetProperty( DIPROP_RANGE, &p.diph )))
			return DIENUM_STOP;

		return DIENUM_CONTINUE;
	}

	IDirectInputDevice8* CreateAndConfigureJoystick( IDirectInput8* di )
	{
		DIDEVICEINSTANCE inst;
		memset( &inst, 0, sizeof(inst) );
		di->EnumDevices( DI8DEVCLASS_GAMECTRL, EnumDevicesCb, (void*)&inst, DIEDFL_ATTACHEDONLY );
		if (inst.dwSize == 0)
			return nullptr;

		IDirectInputDevice8* dev;
		HRESULT hr = di->CreateDevice( inst.guidInstance, &dev, NULL );

		if (FAILED(hr))
			return nullptr;

		hr = dev->EnumObjects( EnumDeviceObjectsCb, (void*)dev, DIDFT_AXIS );

		if (FAILED(hr)) {
			dev->Release();
			return nullptr;
		}

		dev->SetDataFormat( &c_dfDIJoystick );
		return dev;
	}

#pragma managed

	public ref class JoystickState
	{
	private:
		DIJOYSTATE* pstate;

	internal:
		JoystickState(DIJOYSTATE* state) 
		{
			DIJOYSTATE* p = new DIJOYSTATE;
			pstate = p;
			memcpy( pstate, state, sizeof( DIJOYSTATE ) );
		}

	public:
		~JoystickState()
		{
			delete pstate;
		}

		property float x
		{
			float get() 
			{
				return (float)pstate->lX / joy_axis_limit;
			}
		}

		property float y
		{
			float get()
			{
				return (float)pstate->lY / joy_axis_limit;
			}
		}

		property float z
		{
			float get()
			{
				return (float)pstate->lZ / joy_axis_limit;
			}
		}

		property float rx
		{
			float get()
			{
				return (float)pstate->lRx / joy_axis_limit;
			}
		}

		property float ry
		{
			float get()
			{
				return (float)pstate->lRy / joy_axis_limit;
			}
		}

		property float rz
		{
			float get()
			{
				return (float)pstate->lRz / joy_axis_limit;
			}
		}

		property float u
		{
			float get()
			{
				return (float)pstate->rglSlider[0] / joy_axis_limit;
			}
		}

		property float v
		{
			float get()
			{
				return (float)pstate->rglSlider[1] / joy_axis_limit;
			}
		}

		property int pov
		{
			int get()
			{
				return pstate->rgdwPOV[0];
			}
		}

		bool button( int button )
		{
			return (pstate->rgbButtons[button] & 0x80) != 0;
		}
	};

}}