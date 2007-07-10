#pragma once

namespace BluntDirectX { namespace DirectInput {

	value class MouseData
	{
	public:
		long x;
		long y;
		long z;
		unsigned char left, right, middle, aux;
	};

	public ref class MouseState
	{
	private:
		MouseData d;

	internal:
		MouseState( MouseData d ) : d(d) {}

	public:
		property bool default[ int ]
		{
			bool get( int button )
			{
				unsigned char value = 0;
				switch( button )
				{
				case 0: value = d.left; break;
				case 1: value = d.right; break;
				case 2: value = d.middle; break;
				case 3: value = d.aux; break;
				default:
					throw gcnew System::Exception("you idiot");
				}

				return (value & 0x80) != 0;
			}
		}

		property int X
		{
			int get()
			{
				return (int)d.x;
			}
		}

		property int Y
		{
			int get()
			{
				return (int)d.y;
			}
		}

		property int Z
		{
			int get()
			{
				return (int)d.z;
			}
		}
	};

}}