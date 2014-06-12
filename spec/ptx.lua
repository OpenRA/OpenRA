-- author: Christoph Kubisch
---------------------------------------------------------

return {
  exts = {"ptx",},
  lexer = wxstc.wxSTC_LEX_CPP,
  apitype = "ptx",
  sep = ".",
  linecomment = "//",

  isfndef = function(str)
    local l
    local s,e,cap = string.find(str,"^%s*([A-Za-z0-9_]+%s+[A-Za-z0-9_]+%s*%(.+%))")
    if (not s) then
      s,e,cap = string.find(str,"^%s*([A-Za-z0-9_]+%s+[A-Za-z0-9_]+)%s*%(")
    end
    if (cap and (string.find(cap,"^return") or string.find(cap,"else"))) then return end
    return s,e,cap,l
  end,

  lexerstyleconvert = {
    text = {wxstc.wxSTC_C_IDENTIFIER,
      wxstc.wxSTC_C_VERBATIM,
      wxstc.wxSTC_C_REGEX,
      wxstc.wxSTC_C_REGEX,
      wxstc.wxSTC_C_GLOBALCLASS,},

    lexerdef = {wxstc.wxSTC_C_DEFAULT,},
    comment = {wxstc.wxSTC_C_COMMENT,
      wxstc.wxSTC_C_COMMENTLINE,
      wxstc.wxSTC_C_COMMENTDOC,
      wxstc.wxSTC_C_COMMENTLINEDOC,
      wxstc.wxSTC_C_COMMENTDOCKEYWORD,
      wxstc.wxSTC_C_COMMENTDOCKEYWORDERROR,},
    stringtxt = {wxstc.wxSTC_C_STRING,
      wxstc.wxSTC_C_CHARACTER,
      wxstc.wxSTC_C_UUID,},
    stringeol = {wxstc.wxSTC_C_STRINGEOL,},
    preprocessor= {wxstc.wxSTC_C_PREPROCESSOR,},
    operator = {wxstc.wxSTC_C_OPERATOR,},
    number = {wxstc.wxSTC_C_NUMBER,
      wxstc.wxSTC_C_WORD},

    keywords0 = {wxstc.wxSTC_C_WORD,},
    keywords1 = {wxstc.wxSTC_C_WORD2,},
  },

  keywords = {
[[
version
target
address_size

entry
func

branchtargets
calltargets
callprototype

maxnreg
maxntid
reqntid
minnctapersm
maxnctapersm
pragma

section
file
loc

extern
visible

pragma

align
file
maxntid
shared
branchtargets
func
minnctapersm
sreg
callprototype
global
param
target
calltargets
local
pragma
tex
const
loc
reg
version
entry
maxnctapersm
reqntid
visible
extern
maxnreg
section

s8
s16
s32
s64
u8
u16
u32
u64
f16
f32
f64
b8
b16
b32
b64
pred

rn
rz
rm
rp

rni
rzi
rmi
rpi

ca 
cg
cs 
lu
cv

wb
cg
cs
wt

texref
samplerref
surfref

sat
ftz

cc

hi
lo
wide

f4e
b4e
rc8
ecl
ecr
rc16

finite
infinite
number
notanumber
normal
subnormal

approx
full

eq
ne
lt
le
gt
ge

equ
neu
ltu
leu
gtu
geu

num
nan

ls
hs

volatile

v2
v4

L1
L2

1d
2d
3d
a1d
a2d

width
height
depth
channel_data_type
channel_order
normalized_coords

force_unnormalized_coords
filter_mode
addr_mode_0
addr_mode_1
addr_mode_2

trap
clamp
zero

all
any
uni
ballot

sync
arrive
red

cta
gl
sys

and
or
xor
cas
exch
add
inc
dec
min
max

b0
b1
b2
b3
h0
h1
wrap
shr7
shr15

byte 
4byte 
quad 
4byte 
quad

b8 
b32 
b64 
b32 
b64
]],

-- functions

[[
add
sub
add.cc
addc
sub.cc
subc
mul
mad
mul24
mad24
sad
div
rem
abs
neg
min
max
popc
clz
bfind
brev
bfe
bfi
prmt

rcp
sqrt
rsqrt
sin
cos
lg2
ex2
fma

set
setp
selp
slct

and
or
xor
not
cnot
shl
shr

mov
ld
ldu
st
prefetch
prefetchu
isspacep
cvta
cvt

tex
tld4
txq
suld
sust
sured
suq

bra
call
ret
exit

bar
membar
atom
red
vote

vadd
vsub
vabsdiff
vmin
vmax
vshl
vshr
vmad
vset

trap
brkpt
pmevent

%clock
%laneid
%lanemask_gt
%pm0
%pm1
%pm2
%pm3
%clock64
%lanemask_eq
%nctaid
%smid
%ctaid
%lanemask_le
%ntid
%tid
%envreg0
%envreg1
%envreg2
%envreg3
%envreg4
%envreg5
%envreg6
%envreg7
%envreg8
%envreg9
%envreg10
%envreg11
%envreg12
%envreg13
%envreg14
%envreg15
%envreg16
%envreg17
%envreg18
%envreg19
%envreg20
%envreg21
%envreg22
%envreg23
%envreg24
%envreg25
%envreg26
%envreg27
%envreg28
%envreg29
%envreg30
%envreg31
%lanemask_lt
%nsmid
%warpid
%gridid
%lanemask_ge
%nwarpid
WARP_SZ
nearest
linear
wrap
mirror
clamp_ogl
clamp_to_edge
clamp_to_border

sm_20
sm_10
sm_11
sm_12
sm_13
texmode_unified
texmode_independent
map_f64_to_f32
]],

  },
}
