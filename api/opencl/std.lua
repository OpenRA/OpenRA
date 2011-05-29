local funcstring = 
[[
get_work_dim() Returns the number of dimensions in use
get_global_size(uint dimindx) Returns the number of global work-items specified for dimension identified by dimindx
get_global_id(uint dimindx) Returns the unique global work-item ID value for dimension identified by dimindx
get_local_size(uint dimindx) Returns the number of local work-items specified in dimension identified by dimindx
get_local_id(uint dimindx) Returns the unique local work-item ID i.e. a work-item within a specific work-group for dimension identified by dimindx.
get_num_groups(uint dimindx) Returns the number of work-groups that will execute a kernel for dimension identified by dimindx
get_group_id(uint dimindx) Returns the work-group ID
acos(gentype) Arc cosine function
acosh(gentype) Inverse hyperbolic cosine
acospi(gentype) Compute acos (x) / PI
asin(gentype) Arc sine function
asinh(gentype) Inverse hyperbolic sine
asinpi(gentype x) Compute asin (x) / PI
atan(gentype y_over_x) Arc tangent function
atan2(gentype y, gentype x) Arc tangent of y / x
atanh(gentype) Hyperbolic arc tangent.
atanpi(gentype x) Compute atan (x) / PI
atan2pi(gentype y, gentype x) Compute atan2 (y, x) / PI
cbrt(gentype) Compute cube-root
ceil(gentype) Round to integral value using the round to +ve infinity rounding mode
copysign(gentype x, gentype y) Returns x with its sign changed to match the sign of y
cos(gentype) Compute cosine
cosh(gentype) Compute hyperbolic consine
cospi(gentype x) Compute cos (PI*x)
erfc(gentype) Complementary error function
erf(gentype) Error function encountered in integrating the normal distribution
exp(gentype x) Compute the base- e exponential of x
exp2(gentype) Exponential base 2 function
exp10(gentype) Exponential base 10 function
expm1(gentype x) Compute e^x - 1.0
fabs(gentype) Compute absolute value of a floating-point number
fdim(gentype x, gentype y) x - y if x > y, +0 if x is less than or equal to y
floor(gentype) Round to integral value using the round to –ve infinity rounding mode
fma(gentype a, gentype b, gentype c) Returns the correctly rounded floating-point representation of the sum of c with the infinitely precise product of a and b
fmax(gentype x, gentype y) Returns y if x < y, otherwise it returns x
fmin(gentype x, gentype y) Returns y if y < x, otherwise it returns x
fmod(gentype x, gentype y) Modulus. Returns x – y * trunc (x/y)
fract(gentype x, gentype *iptr) Returns fmin( x – floor (x), 0x1.fffffep-1f ).
frexp(gentype x, intn *exp) Extract mantissa and exponent from x
hypot(gentype x, gentype y) Compute the value of the square root of x2+y2
ilogb(gentype x) Return the exponent as an integer value
ldexp(gentype x, intn n) Multiply x by 2 to the power n
lgamma(gentype x) Returns the natural logarithm of the absolute value of the gamma function
lgamma_r(gentype x, intn *signp) Returns the natural logarithm of the absolute value of the gamma function
log(gentype) Compute natural logarithm
log2(gentype) Compute a base 2 logarithm
log10(gentype) Compute a base 10 logarithm
log1p(gentype x) Compute loge(1.0 + x)
logb(gentype x) Compute the exponent of x, which is the integral part of logr|x|
mad(gentype a, gentype b, gentype c) Approximates a * b + c.
modf(gentype x, gentype *iptr) Decompose a floating-point number
nan(uintn nancode) Returns a quiet NaN
nextafter(gentype x, gentype y) Computes the next representable single-precision floating-point value following x in the direction of y.
pow(gentype x, gentype y) Compute x to the power y
pown(gentype x, intn y) Compute x to the power y, where y is an integer
powr(gentype x, gentype y) Compute x to the power y, where x is >= 0
remainder(gentype x, gentype y) r = x - n*y, where n is the integer nearest the exact value of x/y
remquo(gentype x, gentype y, intn *quo) r = x - n*y, where n is the integer nearest the exact value of x/y
rint(gentype) Round to integral value (using round to nearest even rounding mode)
rootn(gentype x, intn y) Compute x to the power 1/y
round(gentype x) Return the integral value nearest to x rounding halfway cases away from zero
rsqrt(gentype) Compute inverse square root
sin(gentype) Compute sine
sincos(gentype x, gentype *cosval) Compute sine and cosine of x
sinh(gentype) Compute hyperbolic sine.
sinpi(gentype x) Compute sin (PI*x)
sqrt(gentype) Compute square root
tan(gentype) Compute tangent
tanh(gentype) Compute hyperbolic tangent
tanpi(gentype x) Compute tan (PI*x)
tgamma(gentype) Compute the gamma function
trunc(gentype) Round to integral value using the round to zero
abs(gentype x) Returns |x|
abs_diff(gentype x, gentype y) Returns |x – y| without modulo overflow
add_sat(gentype x, gentype y) Returns x + y and saturates the result
hadd(gentype x, gentype y) Returns (x + y) >> 1
rhadd(gentype x, gentype y) Returns (x + y + 1) >> 1
clz(gentype x) Returns the number of leading 0-bits in x, starting at the most significant bit position.
mad_hi(gentype a, gentype b, gentype c) Returns mul_hi(a, b) + c
mad_sat(gentype a, gentype b, gentype c) Returns a * b + c and saturates the result
max(gentype x, gentype y) Returns y if x < y, otherwise it returns x
min(gentype x, gentype y) Returns y if y < x, otherwise it returns x
mul_hi(gentype x, gentype y) Computes x * y and returns the high half of the product of x and y
rotate(gentype v, gentype i)
sub_sat(gentype x, gentype y) Returns x - y and saturates the result
upsample(charn hi, ucharn lo) result[i] = ((short)hi[i] << 8) | lo[i]
mad24(gentype x, gentype y, gentype z)
mul24(gentype x, gentype y)
clamp(gentype x, gentype minval, gentype maxval) Returns fmin(fmax(x, minval), maxval)
degrees(gentype radians) Converts radians to degrees
max(gentype x, gentype y)
min(gentype x, gentype y)
mix(gentype x, gentype y, gentype a) Returns the linear blend of x&y: x + (y – x) * a
radians(gentype degrees) Converts degrees to radians
step(gentype edge, gentype x) Returns 0.0 if x < edge, otherwise it returns 1.0
smoothstep(genType edge0, genType edge1, genType x)
sign(gentype x)
cross(float4 p0, float4 p1) Returns the cross product of p0.xyz and p1.xyz.
dot(gentype p0, gentype p1) Compute dot product
distance(gentype p0, gentype p1) Returns the distance between p0 and p1
length(gentype p) Return the length of vecto
normalize(gentype p) Returns a vector in the same direction as p but with length of 1.
fast_distance(gentype p0, gentype p1) Returns fast_length(p0 – p1).
fast_length(gentype p) Returns the length of vector
fast_normalize(gentype p) Returns a vector in the same direction as p but with length of 1.
read_imagef(image2d_t image, sampler_t sampler, int2 coord)
read_imagei(image2d_t image, sampler_t sampler, int2 coord)
read_imageui(image2d_t image, sampler_t sampler, int2 coord)
write_imagef(image2d_t image, int2 coord, float4 color)
write_imagei(image2d_t image, int2 coord, int4 color)
write_imageui(image2d_t image, int2 coord, unsigned int4 color)
get_image_width(image2d_t image)
get_image_width(image3d_t image)
get_image_height(image2d_t image)
get_image_height(image3d_t image)
get_image_channel_data_type(image2d_t image)
get_image_channel_data_type(image3d_t image)
get_image_channel_order(image2d_t image)
get_image_channel_order(image3d_t image)
get_image_dim(image2d_t image)
get_image_dim(image3d_t image)
barrier(cl_mem_fence_flags flags) All work-items in a work-group executing the kernel must execute this function before any are allowed to continue execution beyond the barrier.
mem_fence(cl_mem_fence_flags flags) Orders loads and stores of a work-item executing a kernel.
read_mem_fence(cl_mem_fence_flags flags) Read memory barrier that orders only loads.
write_mem_fence(cl_mem_fence_flags flags) Write memory barrier that orders only stores.
async_work_group_copy(gentype *dst, const gentype *src, size_t num_elements, event_t event) Perform an async copy of num_elements gentype elements from src to dst.
wait_group_events(int num_events, event_t *event_list) Wait for events that identify the async_work_group_copy operations to complete.
prefetch(const __global gentype *p, size_t num_elements) Prefetch num_elements * sizeof(gentype) bytes into the global cache.
vload2(size_t offset, const type *p) Read vector data from memory
vload4(size_t offset, const type *p) Read vector data from memory
vload8(size_t offset, const type *p) Read vector data from memory
vload16(size_t offset, const type *p) Read vector data from memory
vstore2(type2 data, size_t offset, type *p) Write vector data to memory
vstore4(type4 data, size_t offset, type *p) Write vector data to memory
vstore8(type8 data, size_t offset, type *p) Write vector data to memory
vstore16(type16 data, size_t offset, type *p) Write vector data to memory
]]

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
}


local convtypes = [[bool char uchar short ushort int uint long ulong float double]]
local convout = {}
for i in convtypes:gmatch("([%w_]+)") do
	local suffix = {"","_rte","_rtz","_rtp","_rtn"}
	for k,t in ipairs(suffix) do
		table.insert(convout,"convert_"..i..t)
		table.insert(convout,"convert_"..i.."_sat"..t)
		local vectors = {2,4,8,16}
		for n,v in ipairs(vectors) do
			table.insert(convout,"convert_"..i..v..t)
			table.insert(convout,"convert_"..i..v.."_sat"..t)
		end
	end
end
convout = table.concat(convout, " ")

local astypes = [[int uint uchar ushort float double size_t ptrdiff_t intptr_t uintptr_t
		long ulong char short unsigned 
		float2 float4 float8 float16
		double2 double4 double8 double16
		char2 char4 char8 char16
		uchar2 uchar4 uchar8 uchar16
		short2 short4 short8 short16
		ushort2 ushort4 ushort8 ushort16
		int2 int4 int8 int16
		uint2 uint4 uint8 uint16
		long2 long4 long8 long16
		ulong2 ulong4 ulong8 ulong16]]

local astypeout = {}
for i in astypes:gmatch("([%w_]+)") do
	table.insert(astypeout, "as_"..i)
end
astypeout = table.concat(astypeout, " ")

local keyw = astypeout.." "..convout.." "..[[
		int uint uchar ushort half float bool double size_t ptrdiff_t intptr_t uintptr_t void
		long ulong char short unsigned 
		half2 half4 half8 half16
		float2 float4 float8 float16
		double2 double4 double8 double16
		char2 char4 char8 char16
		uchar2 uchar4 uchar8 uchar16
		short2 short4 short8 short16
		ushort2 ushort4 ushort8 ushort16
		int2 int4 int8 int16
		uint2 uint4 uint8 uint16
		long2 long4 long8 long16
		ulong2 ulong4 ulong8 ulong16
		image2d_t image3d_t sampler_t event_t cl_image_format
		
		struct typedef void const
		return switch case for do while if else break continue volatile
		CLK_A CLK_R CLK_RG CLK_RGB CLK_RGBA CLK_ARGB CLK_BGRA CLK_INTENSITY CLK_LUMINANCE
		
		MAXFLOAT HUGE_VALF INFINITY NAN
		CLK_LOCAL_MEM_FENCE CLK_GLOBAL_MEM_FENCE 
		CLK_SNORM_INT8
		CLK_SNORM_INT16
		CLK_UNORM_INT8
		CLK_UNORM_INT16
		CLK_UNORM_SHORT_565
		CLK_UNORM_SHORT_555
		CLK_UNORM_SHORT_101010
		CLK_SIGNED_INT8
		CLK_SIGNED_INT16
		CLK_SIGNED_INT32
		CLK_UNSIGNED_INT8
		CLK_UNSIGNED_INT16
		CLK_UNSIGNED_INT32
		CLK_HALF_FLOAT
		CLK_FLOAT
		__FILE__ __LINE__ __OPENCL_VERSION__ __ENDIAN_LITTLE__ 
		__ROUNDING_MODE__ __IMAGE_SUPPORT__ __FAST_RELAXED_MATH__
		
		__kernel kernel __attribute__ __read_only __write_only read_only write_only
		__constant constant __local local __global global __private private
		vec_type_hint work_group_size_hint reqd_work_group_size
		aligned packed endian host device
		
		async_work_group_copy wait_group_events prefetch 
		clamp min max degrees radians sign smoothstep step mix
		mem_fence read_mem_fence write_mem_fence
		cross prod distance dot length normalize fast_distance fast_length fast_normalize
		read_image write_image get_image_width get_image_height get_image_depth
		get_image_channel_data_type get_image_channel_order
		get_image_dim
		abs abs_diff add_sat clz hadd mad24 mad_hi mad_sat
		mul24 mul_hi rhadd rotate sub_sat upsample
		read_imagei write_imagei read_imageui write_imageui
		read_imagef write_imagef 
		
		isequal isnotequal isgreater isgreaterequal isless islessequal islessgreater
		isfinite isinf isnan isnormal isordered isunordered signbit any all bitselect select
		
		acos acosh acospi asin asinh asinpi atan atan2 atanh atanpi atan2pi
		cbrt ceil copysign cos half_cos native_cos cosh cospi half_divide native_divide
		erf erfc exp half_exp native_exp exp2 half_exp2 native_exp2 exp10 half_exp10 native_exp10
		expm1 fabs fdim floor fma fmax fmin fmod fract frexp hypot ilogb
		ldexp lgamma lgamma_r log half_log native_log log2 half_log2 native_log2
		log10 half_log10 native_log10 log1p logb mad modf nan nextafter
		pow pown powr half_powr native_powr half_recip native_recip
		remainder remquo rint round rootn rsqrt half_rsqrt native_rsqrt
		sin half_sin native_sin sincos sinh sinpi sqrt half_sqrt native_sqrt
		tan half_tan native_tan tanh tanpi tgamma trunc
		
		barrier 
		vload2 vload4 vload8 vload16
		vload_half vload_half2 vload_half4 vload_half8 vload_half16 vloada_half4 vloada_half8 vloada_half16
		vstore2 vstore4 vstore8 vstore16
		vstore_half vstore_half2 vstore_half4 vstore_half8 vstore_half16 vstorea_half4 vstorea_half8 vstorea_half16
		get_global_id get_global_size get_group_id get_local_id get_local_size get_num_groups get_work_dim 
]]

-- keywords - shouldn't be left out
for w in keyw:gmatch("([a-zA-Z_0-9]+)") do
	api[w] = {type="keyword"}
end

return api