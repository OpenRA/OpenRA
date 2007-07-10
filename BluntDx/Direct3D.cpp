#pragma comment(lib, "d3d9.lib")
#pragma comment(lib, "d3dx9.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")

#pragma unmanaged

#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0500

#include <windows.h>
#include <d3d9.h>
#include <d3dx9.h>

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

#include "Enumerations.h"
#include "GraphicsDevice.h"
#include "ImageInformation.h"
#include "Texture.h"
#include "SpriteHelper.h"
#include "FontHelper.h"
#include "Mesh.h"
#include "Effect.h"
#include "VertexBuffer.h"