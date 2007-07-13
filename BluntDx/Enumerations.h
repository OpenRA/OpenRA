#pragma once

namespace BluntDirectX { namespace Direct3D
{
	[Flags]
	public enum class Surfaces
	{
		None = 0,
		Color = D3DCLEAR_TARGET,
		Depth = D3DCLEAR_ZBUFFER,
	};

	[Flags]
	public enum class VertexFormat
	{
		None = 0,
		Position = D3DFVF_XYZ,
		Normal = D3DFVF_NORMAL,
		Texture = D3DFVF_TEX1,
		Texture2 = D3DFVF_TEX2,
	};

	public enum class PrimitiveType
	{
		PointList = D3DPT_POINTLIST,
		TriangleList = D3DPT_TRIANGLELIST,
	};
}}