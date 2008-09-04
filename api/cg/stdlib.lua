-- function helpers
local self = ...
local function key (str)
	self[str] = {type="keyword"}
	return key
end

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

abs = fn "returns absolute value of scalars and vectors. - (typeN)(typeN)"
acos = fn "returns arccosine of scalars and vectors. - (typeN)(typeN)"
all = fn "returns true if a boolean scalar or all components of a boolean vector are true. - (bool)(boolN)"
any = fn "returns true if a boolean scalar or any component of a boolean vector is true.  - (bool)(boolN)"
asin = fn "returns arcsine of scalars and vectors. - (typeN)(typeN)"
atan = fn "returns arctangent of scalars and vectors. - (typeN)(typeN)"
atan2 = fn "returns the arctangent of y/x. atan2 is well defined for every point other than the origin, even if x equals 0 and y does not equal 0. - (typeN)(typeN y, typeN x)"
ceil = fn "returns smallest integer not less than a scalar or each vector component.  - (typeN)(typeN)"
clamp = fn "returns x clamped to the range [a,b]. - (typeN)(typeN x, a, b)"
clip = fn "conditionally (<0) kill a pixel before output. - ()(typeN)"
cos = fn "returns cosine of scalars and vectors.  - (typeN)(typeN)"
cosh = fn "returns hyperbolic cosine of scalars and vectors. - (typeN)(typeN)"
cross = fn "returns the cross product of two three-component vectors. - (type3)(type3 a, b)"
ddx = fn "returns approximate partial derivative with respect to window-space X. - (typeN)(typeN)"
ddy = fn "returns approximate partial derivative with respect to window-space Y. - (typeN)(typeN)"
degrees = fn "converts values of scalars and vectors from radians to degrees.  - (typeN)(typeN)"
determinant = fn "returns the scalar determinant of a square matrix. - (float)(floatNxN)"
distance = fn "return the Euclidean distance between two points. - (typeN)(typeN a, b)"
dot = fn "returns the scalar dot product of two vectors. - (type)(typeN a, b)"
exp = fn "returns the base-e exponential of scalars and vectors. - (typeN)(typeN)"
exp2 = fn "returns the base-2 exponential of scalars and vectors. - (typeN)(typeN)"
faceforward = fn "returns a normal as-is if a vertex's eye-space position vector points in the opposite direction of a geometric normal, otherwise return the negated version of the normal. - (typeN)(typeN Nperturbated, Incident, Ngeometric)"
floatToIntBits = fn "returns the 32-bit integer representation of an IEEE 754 floating-point scalar or vector - (intN)(floatN)"
floatToRawIntBits = fn "returns the raw 32-bit integer representation of an IEEE 754 floating-point scalar or vector. - (intN)(floatN)"
floor = fn "returns largest integer not greater than a scalar or each vector component. - (typeN)(typeN)"
fmod = fn "returns the remainder of x/y with the same sign as x. - (typeN)(typeN x, y)"
frac = fn "returns the fractional portion of a scalar or each vector component.  - (typeN)(typeN)"
frexp = fn "splits scalars and vectors into normalized fraction and a power of 2. - (typeN)(typeN x, out typeN e)"
fwidth = fn "returns sum of approximate window-space partial derivatives magnitudes. - (typeN)(typeN)"
intBitsToFloat = fn "returns the float value corresponding to a given bit represention.of a scalar int value or vector of int values. - (floatN)(intN)"
isfinite = fn "test whether or not a scalar or each vector component is a finite value. - (boolN)(typeN)"
isinf = fn "test whether or not a scalar or each vector component is infinite. - (boolN)(typeN)"
isnan = fn "test whether or not a scalar or each vector component is not-a-number. - (boolN)(typeN)"
ldexp = fn "returns x times 2 rained to n. - (typeN)(typeN a, n)"
length = fn "return scalar Euclidean length of a vector. - (type)(typeN)"
lerp = fn "lerp - returns linear interpolation of two scalars or vectors based on a weight. - (typeN)(typeN a, b, weight)"
lit = fn "computes lighting coefficients for ambient(x), diffuse(y), and specular(z) lighting contributions (w=1). - (type4)(type NdotL, NdotH, specshiny)"
log = fn "returns the natural logarithm of scalars and vectors. - (typeN)(typeN)"
log10 = fn "returns the base-10 logarithm of scalars and vectors.  - (typeN)(typeN)"
log2 = fn "returns the base-2 logarithm of scalars and vectors. - (typeN)(typeN)"
max = fn "returns the maximum of two scalars or each respective component of two vectors. - (typeN)(typeN a, b)"
min = fn "returns the minimum of two scalars or each respective component of two vectors. - (typeN)(typeN a, b)"
mul = fn "Returns the vector result of multiplying a matrix M by a column vector v; a row vector v by a matrix M; or a matrix A by a second matrix B.  - (typeN)(typeNxN/typeN a, typeN/typeNxN b)"
normalize = fn "Returns the normalized version of a vector, meaning a vector in the same direction as the original vector but with a Euclidean length of one. - (typeN)(typeN)"
pow = fn "returns x to the y-th power of scalars and vectors. - (typeN)(typeN x, y)"
radians = fn "converts values of scalars and vectors from degrees to radians. - (typeN)(typeN)"
reflect = fn "returns the reflectiton vector given an incidence vector and a normal vector. - (typeN)(typeN incidence, normal)"
refract = fn "computes a refraction vector. - (typeN)(typeN incidence, normal, type eta)"
round = fn "returns the rounded value of scalars or vectors. - (typeN)(typeN a)"
rsqrt = fn "returns reciprocal square root of scalars and vectors. 1/sqrt. - (typeN)(typeN)"
saturate = fn "returns x saturated to the range [0,1]. - (typeN)(typeN)"
sign = fn "returns sign (1 or -1) of scalar or each vector component. - (typeN)(typeN)"
sin = fn "returns sine of scalars and vectors. - (typeN)(typeN)"
sincos = fn "returns sine of scalars and vectors. - ()(typeN x, out typeN sin, out typeN cos)"
sinh = fn "returns hyperbolic sine of scalars and vectors. - (typeN)(typeN)"
sqrt = fn "returns square root of scalars and vectors. - (typeN)(typeN)"
step = fn "implement a step function returning either zero or one (x >= a). - (typeN)(typeN a, x)"
tan = fn "returns tangent of scalars and vectors. - (typeN)(typeN)"
tanh = fn "returns hyperbolic tangent of scalars and vectors. - (typeN)(typeN)"
tex1D = fn "descr - (typeN)(typeN a, b)"
tex1DARRAY = fn "descr - (typeN)(typeN a, b)"
tex1DARRAYbias = fn "descr - (typeN)(typeN a, b)"
tex1DARRAYcmpbias = fn "descr - (typeN)(typeN a, b)"
tex1DARRAYcmplod = fn "descr - (typeN)(typeN a, b)"
tex1DARRAYfetch = fn "descr - (typeN)(typeN a, b)"
tex1DARRAYlod = fn "descr - (typeN)(typeN a, b)"
tex1DARRAYproj = fn "descr - (typeN)(typeN a, b)"
tex1DARRAYsize = fn "descr - (typeN)(typeN a, b)"
tex1Dbias = fn "descr - (typeN)(typeN a, b)"
tex1Dcmpbias = fn "descr - (typeN)(typeN a, b)"
tex1Dcmplod = fn "descr - (typeN)(typeN a, b)"
tex1Dfetch = fn "descr - (typeN)(typeN a, b)"
tex1Dlod = fn "descr - (typeN)(typeN a, b)"
tex1Dproj = fn "descr - (typeN)(typeN a, b)"
tex1Dsize = fn "descr - (typeN)(typeN a, b)"
tex2D = fn "descr - (typeN)(typeN a, b)"
tex2DARRAY = fn "descr - (typeN)(typeN a, b)"
tex2DARRAYbias = fn "descr - (typeN)(typeN a, b)"
tex2DARRAYfetch = fn "descr - (typeN)(typeN a, b)"
tex2DARRAYlod = fn "descr - (typeN)(typeN a, b)"
tex2DARRAYproj = fn "descr - (typeN)(typeN a, b)"
tex2DARRAYsize = fn "descr - (typeN)(typeN a, b)"
tex2Dbias = fn "descr - (typeN)(typeN a, b)"
tex2Dcmpbias = fn "descr - (typeN)(typeN a, b)"
tex2Dcmplod = fn "descr - (typeN)(typeN a, b)"
tex2Dfetch = fn "descr - (typeN)(typeN a, b)"
tex2Dlod = fn "descr - (typeN)(typeN a, b)"
tex2Dproj = fn "descr - (typeN)(typeN a, b)"
tex2Dsize = fn "descr - (typeN)(typeN a, b)"
tex3D = fn "descr - (typeN)(typeN a, b)"
tex3Dbias = fn "descr - (typeN)(typeN a, b)"
tex3Dfetch = fn "descr - (typeN)(typeN a, b)"
tex3Dlod = fn "descr - (typeN)(typeN a, b)"
tex3Dproj = fn "descr - (typeN)(typeN a, b)"
tex3Dsize = fn "descr - (typeN)(typeN a, b)"
texBUF = fn "descr - (typeN)(typeN a, b)"
texBUFsize = fn "descr - (typeN)(typeN a, b)"
texCUBE = fn "descr - (typeN)(typeN a, b)"
texCUBEARRAY = fn "descr - (typeN)(typeN a, b)"
texCUBEARRAYsize = fn "descr - (typeN)(typeN a, b)"
texCUBEbias = fn "descr - (typeN)(typeN a, b)"
texCUBElod = fn "descr - (typeN)(typeN a, b)"
texCUBEproj = fn "descr - (typeN)(typeN a, b)"
texCUBEsize = fn "descr - (typeN)(typeN a, b)"
texRECT = fn "descr - (typeN)(typeN a, b)"
texRECTbias = fn "descr - (typeN)(typeN a, b)"
texRECTfetch = fn "descr - (typeN)(typeN a, b)"
texRECTlod = fn "descr - (typeN)(typeN a, b)"
texRECTproj = fn "descr - (typeN)(typeN a, b)"
texRECTsize = fn "descr - (typeN)(typeN a, b)"
transpose = fn "returns transpose matrix of a matrix. - (typeRxC)(typeCxR)"
trunc = fn "returns largest integer not greater than a scalar or each vector component. - (typeN)(typeN)"

local keyw = 
[[int half float float3 float4 float2 float3x3 float3x4 float4x3 float4x4 double vector vec matrix
half half2 half3 half4
mat string struct typedef matrix
sampler sampler1D sampler2D sampler3D samplerRECT samplerCUBE 
texture texture1D texture2D texture3D textureRECT textureCUBE

decl do double else extern false for if in inline inout out pass
pixelshader return shared static string technique true
uniform vector vertexshader void volatile while

asm bool compile const auto break case catch char class const_cast continue default delete
dynamic_cast enum explicit friend goto long mutable namespace new operator private protected
public register reinterpret_case short signed sizeof static_cast switch template this throw
try typename union unsigned using virtual

POSITION PSIZE DIFFUSE SPECULAR TEXCOORD FOG COLOR COLOR0 COLOR1 COLOR2 COLOR3 TEXCOORD0 TEXCOORD1 TEXCOORD2 TEXCOORD3
TEXCOORD4 TEXCOORD5 TEXCOORD6 TEXCOORD7 TEXCOORD8 TEXCOORD9 TEXCOORD10 TEXCOORD11 TEXCOORD12 TEXCOORD13 TEXCOORD14
TEXCOORD15
NORMAL
ATTR0 ATTR1 ATTR2 ATTR3 ATTR4 ATTR5 ATTR6 ATTR7 ATTR8 ATTR9 ATTR10 ATTR11 ATTR12 ATTR13 ATTR14 ATTR15
TEXUNIT0 TEXUNIT1 TEXUNIT2 TEXUNIT3 TEXUNIT4 TEXUNIT5 TEXUNIT6 TEXUNIT7 TEXUNIT8 TEXUNIT9 TEXUNIT10 TEXUNIT11 TEXUNIT12
TEXUNIT13 TEXUNIT14 TEXUNIT15 ]]

-- keywords - shouldn't be left out
for w in keyw:gmatch(keyw,"([a-zA-Z_0-9]+)") do
	key(w)
end



