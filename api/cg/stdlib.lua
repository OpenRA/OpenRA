-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local function fn (description) 
	local description2,returns,args = description:match("(.+)%-%s*(%b())%s*(%b())")
	if not description2 then
		return {type="function",description=description,
			returns="(?)"} 
	end
	return {type="function",description=description2,
		returns=returns:gsub("^%s+",""):gsub("%s+$",""), args = args} 
end

local function val (description)
	return {type="value",description = description}
end
-- docs
local api = {
abs = fn "returns absolute value of scalars and vectors. - (typeN)(typeN)",
acos = fn "returns arccosine of scalars and vectors. - (typeN)(typeN)",
all = fn "returns true if a boolean scalar or all components of a boolean vector are true. - (bool)(boolN)",
any = fn "returns true if a boolean scalar or any component of a boolean vector is true.  - (bool)(boolN)",
asin = fn "returns arcsine of scalars and vectors. - (typeN)(typeN)",
atan = fn "returns arctangent of scalars and vectors. - (typeN)(typeN)",
atan2 = fn "returns the arctangent of y/x. atan2 is well defined for every point other than the origin, even if x equals 0 and y does not equal 0. - (typeN)(typeN y, typeN x)",
ceil = fn "returns smallest integer not less than a scalar or each vector component.  - (typeN)(typeN)",
clamp = fn "returns x clamped to the range [a,b]. - (typeN)(typeN x, a, b)",
clip = fn "conditionally (<0) kill a pixel before output. - ()(typeN)",
cos = fn "returns cosine of scalars and vectors.  - (typeN)(typeN)",
cosh = fn "returns hyperbolic cosine of scalars and vectors. - (typeN)(typeN)",
cross = fn "returns the cross product of two three-component vectors. - (type3)(type3 a, b)",
ddx = fn "returns approximate partial derivative with respect to window-space X. - (typeN)(typeN)",
ddy = fn "returns approximate partial derivative with respect to window-space Y. - (typeN)(typeN)",
degrees = fn "converts values of scalars and vectors from radians to degrees.  - (typeN)(typeN)",
determinant = fn "returns the scalar determinant of a square matrix. - (float)(floatNxN)",
distance = fn "return the Euclidean distance between two points. - (typeN)(typeN a, b)",
dot = fn "returns the scalar dot product of two vectors. - (type)(typeN a, b)",
exp = fn "returns the base-e exponential of scalars and vectors. - (typeN)(typeN)",
exp2 = fn "returns the base-2 exponential of scalars and vectors. - (typeN)(typeN)",
faceforward = fn "returns a normal as-is if a vertex's eye-space position vector points in the opposite direction of a geometric normal, otherwise return the negated version of the normal. - (typeN)(typeN Nperturbated, Incident, Ngeometric)",
floatToIntBits = fn "returns the 32-bit integer representation of an IEEE 754 floating-point scalar or vector - (intN)(floatN)",
floatToRawIntBits = fn "returns the raw 32-bit integer representation of an IEEE 754 floating-point scalar or vector. - (intN)(floatN)",
floor = fn "returns largest integer not greater than a scalar or each vector component. - (typeN)(typeN)",
fmod = fn "returns the remainder of x/y with the same sign as x. - (typeN)(typeN x, y)",
frac = fn "returns the fractional portion of a scalar or each vector component.  - (typeN)(typeN)",
frexp = fn "splits scalars and vectors into normalized fraction and a power of 2. - (typeN)(typeN x, out typeN e)",
fwidth = fn "returns sum of approximate window-space partial derivatives magnitudes. - (typeN)(typeN)",
intBitsToFloat = fn "returns the float value corresponding to a given bit represention.of a scalar int value or vector of int values. - (floatN)(intN)",
isfinite = fn "test whether or not a scalar or each vector component is a finite value. - (boolN)(typeN)",
isinf = fn "test whether or not a scalar or each vector component is infinite. - (boolN)(typeN)",
isnan = fn "test whether or not a scalar or each vector component is not-a-number. - (boolN)(typeN)",
ldexp = fn "returns x times 2 rained to n. - (typeN)(typeN a, n)",
length = fn "return scalar Euclidean length of a vector. - (type)(typeN)",
lerp = fn "lerp - returns linear interpolation of two scalars or vectors based on a weight. - (typeN)(typeN a, b, weight)",
lit = fn "computes lighting coefficients for ambient(x), diffuse(y), and specular(z) lighting contributions (w=1). - (type4)(type NdotL, NdotH, specshiny)",
log = fn "returns the natural logarithm of scalars and vectors. - (typeN)(typeN)",
log10 = fn "returns the base-10 logarithm of scalars and vectors.  - (typeN)(typeN)",
log2 = fn "returns the base-2 logarithm of scalars and vectors. - (typeN)(typeN)",
max = fn "returns the maximum of two scalars or each respective component of two vectors. - (typeN)(typeN a, b)",
min = fn "returns the minimum of two scalars or each respective component of two vectors. - (typeN)(typeN a, b)",
mul = fn "Returns the vector result of multiplying a matrix M by a column vector v; a row vector v by a matrix M; or a matrix A by a second matrix B.  - (typeN)(typeNxN/typeN a, typeN/typeNxN b)",
normalize = fn "Returns the normalized version of a vector, meaning a vector in the same direction as the original vector but with a Euclidean length of one. - (typeN)(typeN)",
pow = fn "returns x to the y-th power of scalars and vectors. - (typeN)(typeN x, y)",
radians = fn "converts values of scalars and vectors from degrees to radians. - (typeN)(typeN)",
reflect = fn "returns the reflectiton vector given an incidence vector and a normal vector. - (typeN)(typeN incidence, normal)",
refract = fn "computes a refraction vector. - (typeN)(typeN incidence, normal, type eta)",
round = fn "returns the rounded value of scalars or vectors. - (typeN)(typeN a)",
rsqrt = fn "returns reciprocal square root of scalars and vectors. 1/sqrt. - (typeN)(typeN)",
saturate = fn "returns x saturated to the range [0,1]. - (typeN)(typeN)",
sign = fn "returns sign (1 or -1) of scalar or each vector component. - (typeN)(typeN)",
sin = fn "returns sine of scalars and vectors. - (typeN)(typeN)",
sincos = fn "returns sine of scalars and vectors. - ()(typeN x, out typeN sin, out typeN cos)",
sinh = fn "returns hyperbolic sine of scalars and vectors. - (typeN)(typeN)",
sqrt = fn "returns square root of scalars and vectors. - (typeN)(typeN)",
step = fn "implement a step function returning either zero or one (a <= b). - (typeN)(typeN a, b)",
tan = fn "returns tangent of scalars and vectors. - (typeN)(typeN)",
tanh = fn "returns hyperbolic tangent of scalars and vectors. - (typeN)(typeN)",
transpose = fn "returns transpose matrix of a matrix. - (typeRxC)(typeCxR)",
trunc = fn "returns largest integer not greater than a scalar or each vector component. - (typeN)(typeN)",

tex1D = fn "performs a texture lookup in a given 1D sampler and, in some cases, a shadow comparison (as .y coord). May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(sampler1D, float/float2 s, |float dx, dy|,[int texeloffset])",
tex1Dbias = fn "performs a texture lookup with bias in a given sampler (as .w).  - (float4)(sampler1D, float4 s, [int texeloffset])",
tex1Dcmpbias = fn "performs a texture lookup with bias and shadow compare in a given sampler (compare as .y, bias as .w).  - (float4)(sampler1D, float4 s, [int texeloffset])",
tex1Dcmplod = fn "performs a texture lookup with a specified level of detail and a shadow compare in a given sampler (compare as .y, lod as .w).  - (float4)(sampler1D, float4 s, [int texeloffset])",
tex1Dfetch = fn "performs an unfiltered texture lookup in a given sampler (lod as .w). - (float4)(sampler1D, int4 s, [int texeloffset])",
tex1Dlod = fn "performs a texture lookup with a specified level of detail in a given sampler (lod as .w) - (float4)(sampler1D, float4 s, [int texeloffset])",
tex1Dproj = fn "performs a texture lookup with projection in a given sampler. May perform a shadow comparison if argument for shadow comparison is provided. (shadow in .y for float3 coord, proj in .y or .z) - (float4)(sampler1D, float2/float3 s, [int texeloff])",
tex1Dsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(sampler1D, int lod)",

tex2D = fn "performs a texture lookup in a given 2D sampler and, in some cases, a shadow comparison (as .z coord). May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(sampler2D, float2/float3 s, |float2 dx, dy|,[int texeloffset])",
tex2Dbias = fn "performs a texture lookup with bias in a given sampler (as .w).  - (float4)(sampler2D, float4 s, [int texeloffset])",
tex2Dcmpbias = fn "performs a texture lookup with bias and shadow compare in a given sampler (compare as .z, bias as .w).  - (float4)(sampler2D, float4 s, [int texeloffset])",
tex2Dcmplod = fn "performs a texture lookup with a specified level of detail and a shadow compare in a given sampler (compare as .y, lod as .w).  - (float4)(sampler2D, float4 s, [int texeloffset])",
tex2Dfetch = fn "performs an unfiltered texture lookup in a given sampler (lod as .w). - (float4)(sampler2D, int4 s, [int texeloffset])",
tex2Dlod = fn "performs a texture lookup with a specified level of detail in a given sampler (lod as .w) - (float4)(sampler2D, float4 s, [int texeloffset])",
tex2Dproj = fn "performs a texture lookup with projection in a given sampler. May perform a shadow comparison if argument for shadow comparison is provided. (shadow in .z for float3 coord, proj in .z or .w) - (float4)(sampler2D, float3/float4 s, [int texeloff])",
tex2Dsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(sampler2D, int lod)",
tex2Dgather = fn "returns 4 texels of a given single channel texture image for a given level of detail. (only gp4 profiles) - (int3)(sampler2D, int lod)",

tex3D = fn "performs a texture lookup in a given 3D sampler. May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(sampler3D, float3 s, {float3 dx, dy},[int texeloffset])",
tex3Dbias = fn "performs a texture lookup with bias in a given sampler (as .w).  - (float4)(sampler3D, float4 s, [int texeloffset])",
tex3Dfetch = fn "performs an unfiltered texture lookup in a given sampler (lod as .w). - (float4)(sampler3D, int4 s, [int texeloffset])",
tex3Dlod = fn "performs a texture lookup with a specified level of detail in a given sampler (lod as .w) - (float4)(sampler3D, float4 s, [int texeloffset])",
tex3Dproj = fn "performs a texture lookup with projection in a given sampler. (proj in .w) - (float4)(sampler3D, float4 s, [int texeloff])",
tex3Dsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(sampler3D, int lod)",

texBUF = fn "performs an unfiltered texture lookup in a given texture buffer sampler. (only gp4 profiles) - (float4)(samplerBUF, int s)",
texBUFsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(samplerBUF, int lod)",

texRBUF = fn "performs a multi-sampled texture lookup in a renderbuffer. (only gp4 profiles) - (float4)(samplerRBUF, int2 s, int sample)",
texRBUFsize = fn "returns the size of a given renderbuffer. (only gp4 profiles) - (int2)(samplerBUF)",

texCUBE = fn "performs a texture lookup in a given CUBE sampler and, in some cases, a shadow comparison (float4 coord). May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(samplerCUBE, float3/float4 s, |float3 dx, dy|)",
texCUBEbias = fn "performs a texture lookup with bias in a given sampler (as .w).  - (float4)(sampler1D, float4 s, [int texeloffset])",
texCUBElod = fn "performs a texture lookup with a specified level of detail in a given sampler (lod as .w) - (float4)(sampler1D, float4 s, [int texeloffset])",
texCUBEproj = fn "performs a texture lookup with projection in a given sampler. (proj in .w) - (float4)(samplerCUBE, float4 s)",
texCUBEsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(sampler1D, int lod)",

texRECT = fn "performs a texture lookup in a given RECT sampler and, in some cases, a shadow comparison (as .z). May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(samplerRECT, float2/float3 s, |float2 dx, dy|, [int texeloff])",
texRECTbias = fn "performs a texture lookup with bias in a given sampler (as .w).  - (float4)(samplerRECT, float4 s, [int texeloffset])",
texRECTfetch = fn "performs an unfiltered texture lookup in a given sampler (lod as .w). - (float4)(samplerRECT, int4 s, [int texeloffset])",
texRECTlod = fn "performs a texture lookup with a specified level of detail in a given sampler (lod as .w) - (float4)(samplerRECT, float4 s, [int texeloffset])",
texRECTproj = fn "performs a texture lookup with projection in a given sampler. May perform a shadow comparison if argument for shadow comparison is provided. (shadow in .z for float3 coord, proj in .z or .w) - (float4)(samplerRECT, float3/float4 s, [int texeloff])",
texRECTsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(samplerRECT, int lod)",

tex1DARRAY = fn "performs a texture lookup in a given 1D sampler array and, in some cases, a shadow comparison (as .z). May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(sampler1DARRAY, float2/float3 s, {float dx, dy},[int texeloffset])",
tex1DARRAYbias = fn "performs a texture lookup with bias in a given sampler (as .w).  - (float4)(sampler1DARRAY, float4 s, [int texeloffset])",
tex1DARRAYcmpbias = fn "performs a texture lookup with bias and shadow compare in a given sampler (layer as .y, compare as .z, bias as .w).  - (float4)(sampler1DARRAY, float4 s, [int texeloffset])",
tex1DARRAYcmplod = fn "performs a texture lookup with a specified level of detail and a shadow compare in a given sampler (compare as .z, lod as .w).  - (float4)(sampler1DARRAY, float4 s, [int texeloffset])",
tex1DARRAYfetch = fn "performs an unfiltered texture lookup in a given sampler (lod as .z). - (float4)(sampler1DARRAY, int3 s, [int texeloffset])",
tex1DARRAYlod = fn "performs a texture lookup with a specified level of detail in a given sampler (lod as .z) - (float4)(sampler1DARRAY, float3 s, [int texeloffset])",
tex1DARRAYproj = fn "performs a texture lookup with projection in a given sampler. May perform a shadow comparison if argument for shadow comparison is provided. (shadow in .z for float3 coord, proj in .z or .w) - (float4)(sampler1DARRAY, float3/float4 s, [int texeloff])",
tex1DARRAYsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(sampler1DARRAY, int lod)",

tex2DARRAY = fn "performs a texture lookup in a given 2D sampler array and, in some cases, a shadow comparison (as .w coord). May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(sampler2DARRAY, float3/float4 s, {float2 dx, dy},[int texeloffset])",
tex2DARRAYbias = fn "performs a texture lookup with bias in a given sampler (as .w).  - (float4)(sampler2DARRAY, float4 s, [int texeloffset])",
tex2DARRAYfetch = fn "performs an unfiltered texture lookup in a given sampler (lod as .w). - (float4)(sampler2DARRAY, int4 s, [int texeloffset])",
tex2DARRAYlod = fn "performs a texture lookup with a specified level of detail in a given sampler (lod as .w) - (float4)(sampler2DARRAY, float4 s, [int texeloffset])",
tex2DARRAYproj = fn "performs a texture lookup with projection in a given sampler. May perform a shadow comparison if argument for shadow comparison is provided. (proj in .w) - (float4)(sampler2DARRAY, float4 s, [int texeloff])",
tex2DARRAYsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(sampler2DARRAY, int lod)",

texCUBEARRAY = fn "performs a texture lookup in a given CUBE sampler array. May also use pre computed derivatives if those are provided. Texeloffset only in gp4 or higher profiles. - (float4)(samplerCUBEARRAY, float4 s, {float3 dx, dy},[int texeloffset])",
texCUBEARRAYsize = fn "returns the size of a given texture image for a given level of detail. (only gp4 profiles) - (int3)(samplerCUBEARRAY, int lod)",

unpack_4ubyte = fn "interprets the single float as 4 normalized unsigned bytes and returns the vector. (only nv/gp4 profiles) - (float4)(float)",
pack_4ubyte = fn "packs the floats into a single storing as normalized unsigned bytes.(only nv/gp4 profiles) - (float)(float4)",
unpack_4byte = fn "interprets the single float as 4 normalized signed bytes and returns the vector. (only nv/gp4 profiles) - (float4)(float)",
pack_4ubyte = fn "packs the floats into a single storing as normalized signed bytes.(only nv/gp4 profiles) - (float)(float4)",
unpack_4ushort = fn "interprets the single float as 2 normalized unsigned shorts and returns the vector. (only nv/gp4 profiles) - (float2)(float)",
pack_4ushort = fn "packs the floats into a single storing as normalized unsigned shorts.(only nv/gp4 profiles) - (float)(float2)",
unpack_2half = fn "interprets the single float as 2 16-bit floats and returns the vector. (only nv/gp4 profiles) - (float2)(float)",
pack_2half = fn "packs the floats into a single storing as 16-bit floats.(only nv/gp4 profiles) - (float)(float2)",
}

local keyw = 
[[int half float float3 float4 float2 float3x3 float3x4 float4x3 float4x4 
float1x2 float2x1 float2x2 float2x3 float3x2 float1x3 float3x1 float4x1 float1x4
float2x4 float4x2 double1x4 double4x4 double4x2 double4x3 double3x4 double2x4 double1x4
double half half2 half3 half4 int2 int3 uint uint2 uint3 uint4
int4 bool bool2 bool3 bool4 string struct typedef
usampler usampler1D usampler2D usampler3D usamplerRECT usamplerCUBE isampler1DARRAY usampler2DARRAY usamplerCUBEARRAY
isampler isampler1D isampler2D isampler3D isamplerRECT isamplerCUBE isampler1DARRAY isampler2DARRAY isamplerCUBEARRAY
usamplerBUF isamplerBUF samplerBUF
sampler sampler1D sampler2D sampler3D samplerRECT samplerCUBE sampler1DARRAY sampler2DARRAY samplerCUBEARRAY
texture texture1D texture2D texture3D textureRECT textureCUBE texture1DARRAY texture2DARRAY textureCUBEARRAY

decl do else extern false for if in inline inout out pass
pixelshader return shared static string technique true
uniform vector vertexshader void volatile while

asm compile const auto break case catch char class const_cast continue default delete
dynamic_cast enum explicit friend goto long mutable namespace new operator private protected
public register reinterpret_case short signed sizeof static_cast switch template this throw
try typename union unsigned using virtual

POSITION PSIZE DIFFUSE SPECULAR TEXCOORD FOG COLOR COLOR0 COLOR1 COLOR2 COLOR3 TEXCOORD0 TEXCOORD1 TEXCOORD2 TEXCOORD3
TEXCOORD4 TEXCOORD5 TEXCOORD6 TEXCOORD7 TEXCOORD8 TEXCOORD9 TEXCOORD10 TEXCOORD11 TEXCOORD12 TEXCOORD13 TEXCOORD14
TEXCOORD15
NORMAL WPOS
ATTR0 ATTR1 ATTR2 ATTR3 ATTR4 ATTR5 ATTR6 ATTR7 ATTR8 ATTR9 ATTR10 ATTR11 ATTR12 ATTR13 ATTR14 ATTR15
TEXUNIT0 TEXUNIT1 TEXUNIT2 TEXUNIT3 TEXUNIT4 TEXUNIT5 TEXUNIT6 TEXUNIT7 TEXUNIT8 TEXUNIT9 TEXUNIT10 TEXUNIT11 TEXUNIT12
TEXUNIT13 TEXUNIT14 TEXUNIT15 

PROJ PROJECTION PROJECTIONMATRIX PROJMATRIX
PROJMATRIXINV PROJINV PROJECTIONINV PROJINVERSE PROJECTIONINVERSE PROJINVMATRIX PROJECTIONINVMATRIX PROJINVERSEMATRIX PROJECTIONINVERSEMATRIX
VIEW VIEWMATRIX VIEWMATRIXINV VIEWINV VIEWINVERSE VIEWINVERSEMATRIX VIEWINVMATRIX
VIEWPROJECTION VIEWPROJ VIEWPROJMATRIX VIEWPROJECTIONMATRIX
WORLD WORLDMATRIX WORLDVIEW WORLDVIEWMATRIX
WORLDVIEWPROJ WORLDVIEWPROJECTION WORLDVIEWPROJMATRIX WORLDVIEWPROJECTIONMATRIX
VIEWPORTSIZE VIEWPORTDIMENSION
VIEWPORTSIZEINV VIEWPORTSIZEINVERSE VIEWPORTDIMENSIONINV VIEWPORTDIMENSIONINVERSE INVERSEVIEWPORTDIMENSIONS
FOGCOLOR FOGDISTANCE CAMERAWORLDPOS CAMERAWORLDDIR

CENTROID FLAT NOPERSPECTIVE FACE PRIMITIVEID VERTEXID

]]

-- keywords - shouldn't be left out
for w in keyw:gmatch("([_%w]+)") do
	api[w] = {type="keyword"}
end

return api


