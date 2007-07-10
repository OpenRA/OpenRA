#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "dinput8.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")

#pragma unmanaged

#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0500

#define INITGUID
#define DIRECTINPUT_VERSION 0x0800

#include <windows.h>
#include <dinput.h>

#pragma managed

#include <vcclr.h>

#pragma once

#using "System.Drawing.dll"
#using "System.Windows.Forms.dll"

using namespace System;
using namespace System::Windows::Forms;
using namespace System::IO;
using namespace System::Drawing;

#include "Utilities.h"

#include "InputManager.h"
#include "KeyboardState.h"
#include "MouseState.h"
#include "JoystickState.h"
#include "InputDevice.h"