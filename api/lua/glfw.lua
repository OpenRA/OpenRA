--[[// glfw | GLFW window manager
enum {
/*************************************************************************
 * GLFW version
 *************************************************************************/

  GLFW_VERSION_MAJOR    =2,
  GLFW_VERSION_MINOR    =7,
  GLFW_VERSION_REVISION =2,


/*************************************************************************
 * Input handling definitions
 *************************************************************************/

/* Key and button state/action definitions */
  GLFW_RELEASE            =0,
  GLFW_PRESS              =1,
  
  GLFW_TRUE  = 1,
  GLFW_FALSE = 0,

/* Keyboard key definitions: 8-bit ISO-8859-1 (Latin 1) encoding is used
 * for printable keys (such as A-Z, 0-9 etc), and values above 256
 * represent special (non-printable) keys (e.g. F1, Page Up etc).
 */
  GLFW_KEY_UNKNOWN      =-1,
  GLFW_KEY_SPACE        =32,
  GLFW_KEY_APOSTROPHE        = 39, 
  GLFW_KEY_COMMA             = 44, 
  GLFW_KEY_MINUS             = 45, 
  GLFW_KEY_PERIOD            = 46, 
  GLFW_KEY_SLASH             = 47, 
  GLFW_KEY_0                 = 48,
  GLFW_KEY_1                 = 49,
  GLFW_KEY_2                 = 50,
  GLFW_KEY_3                 = 51,
  GLFW_KEY_4                 = 52,
  GLFW_KEY_5                 = 53,
  GLFW_KEY_6                 = 54,
  GLFW_KEY_7                 = 55,
  GLFW_KEY_8                 = 56,
  GLFW_KEY_9                 = 57,
  GLFW_KEY_SEMICOLON         = 59, 
  GLFW_KEY_EQUAL             = 61, 
  GLFW_KEY_A                 = 65,
  GLFW_KEY_B                 = 66,
  GLFW_KEY_C                 = 67,
  GLFW_KEY_D                 = 68,
  GLFW_KEY_E                 = 69,
  GLFW_KEY_F                 = 70,
  GLFW_KEY_G                 = 71,
  GLFW_KEY_H                 = 72,
  GLFW_KEY_I                 = 73,
  GLFW_KEY_J                 = 74,
  GLFW_KEY_K                 = 75,
  GLFW_KEY_L                 = 76,
  GLFW_KEY_M                 = 77,
  GLFW_KEY_N                 = 78,
  GLFW_KEY_O                 = 79,
  GLFW_KEY_P                 = 80,
  GLFW_KEY_Q                 = 81,
  GLFW_KEY_R                 = 82,
  GLFW_KEY_S                 = 83,
  GLFW_KEY_T                 = 84,
  GLFW_KEY_U                 = 85,
  GLFW_KEY_V                 = 86,
  GLFW_KEY_W                 = 87,
  GLFW_KEY_X                 = 88,
  GLFW_KEY_Y                 = 89,
  GLFW_KEY_Z                 = 90,
  GLFW_KEY_LEFT_BRACKET      = 91, 
  GLFW_KEY_BACKSLASH         = 92,
  GLFW_KEY_RIGHT_BRACKET     = 93, 
  GLFW_KEY_GRAVE_ACCENT      = 96,
  GLFW_KEY_WORLD_1           = 161,
  GLFW_KEY_WORLD_2           = 162,

  GLFW_KEY_SPECIAL      =256,
  GLFW_KEY_ESC          =(GLFW_KEY_SPECIAL+1),
  GLFW_KEY_F1           =(GLFW_KEY_SPECIAL+2),
  GLFW_KEY_F2           =(GLFW_KEY_SPECIAL+3),
  GLFW_KEY_F3           =(GLFW_KEY_SPECIAL+4),
  GLFW_KEY_F4           =(GLFW_KEY_SPECIAL+5),
  GLFW_KEY_F5           =(GLFW_KEY_SPECIAL+6),
  GLFW_KEY_F6           =(GLFW_KEY_SPECIAL+7),
  GLFW_KEY_F7           =(GLFW_KEY_SPECIAL+8),
  GLFW_KEY_F8           =(GLFW_KEY_SPECIAL+9),
  GLFW_KEY_F9           =(GLFW_KEY_SPECIAL+10),
  GLFW_KEY_F10          =(GLFW_KEY_SPECIAL+11),
  GLFW_KEY_F11          =(GLFW_KEY_SPECIAL+12),
  GLFW_KEY_F12          =(GLFW_KEY_SPECIAL+13),
  GLFW_KEY_F13          =(GLFW_KEY_SPECIAL+14),
  GLFW_KEY_F14          =(GLFW_KEY_SPECIAL+15),
  GLFW_KEY_F15          =(GLFW_KEY_SPECIAL+16),
  GLFW_KEY_F16          =(GLFW_KEY_SPECIAL+17),
  GLFW_KEY_F17          =(GLFW_KEY_SPECIAL+18),
  GLFW_KEY_F18          =(GLFW_KEY_SPECIAL+19),
  GLFW_KEY_F19          =(GLFW_KEY_SPECIAL+20),
  GLFW_KEY_F20          =(GLFW_KEY_SPECIAL+21),
  GLFW_KEY_F21          =(GLFW_KEY_SPECIAL+22),
  GLFW_KEY_F22          =(GLFW_KEY_SPECIAL+23),
  GLFW_KEY_F23          =(GLFW_KEY_SPECIAL+24),
  GLFW_KEY_F24          =(GLFW_KEY_SPECIAL+25),
  GLFW_KEY_F25          =(GLFW_KEY_SPECIAL+26),
  GLFW_KEY_UP           =(GLFW_KEY_SPECIAL+27),
  GLFW_KEY_DOWN         =(GLFW_KEY_SPECIAL+28),
  GLFW_KEY_LEFT         =(GLFW_KEY_SPECIAL+29),
  GLFW_KEY_RIGHT        =(GLFW_KEY_SPECIAL+30),
  GLFW_KEY_LSHIFT       =(GLFW_KEY_SPECIAL+31),
  GLFW_KEY_RSHIFT       =(GLFW_KEY_SPECIAL+32),
  GLFW_KEY_LCTRL        =(GLFW_KEY_SPECIAL+33),
  GLFW_KEY_RCTRL        =(GLFW_KEY_SPECIAL+34),
  GLFW_KEY_LALT         =(GLFW_KEY_SPECIAL+35),
  GLFW_KEY_RALT         =(GLFW_KEY_SPECIAL+36),
  GLFW_KEY_TAB          =(GLFW_KEY_SPECIAL+37),
  GLFW_KEY_ENTER        =(GLFW_KEY_SPECIAL+38),
  GLFW_KEY_BACKSPACE    =(GLFW_KEY_SPECIAL+39),
  GLFW_KEY_INSERT       =(GLFW_KEY_SPECIAL+40),
  GLFW_KEY_DEL          =(GLFW_KEY_SPECIAL+41),
  GLFW_KEY_PAGEUP       =(GLFW_KEY_SPECIAL+42),
  GLFW_KEY_PAGEDOWN     =(GLFW_KEY_SPECIAL+43),
  GLFW_KEY_HOME         =(GLFW_KEY_SPECIAL+44),
  GLFW_KEY_END          =(GLFW_KEY_SPECIAL+45),
  GLFW_KEY_KP_0         =(GLFW_KEY_SPECIAL+46),
  GLFW_KEY_KP_1         =(GLFW_KEY_SPECIAL+47),
  GLFW_KEY_KP_2         =(GLFW_KEY_SPECIAL+48),
  GLFW_KEY_KP_3         =(GLFW_KEY_SPECIAL+49),
  GLFW_KEY_KP_4         =(GLFW_KEY_SPECIAL+50),
  GLFW_KEY_KP_5         =(GLFW_KEY_SPECIAL+51),
  GLFW_KEY_KP_6         =(GLFW_KEY_SPECIAL+52),
  GLFW_KEY_KP_7         =(GLFW_KEY_SPECIAL+53),
  GLFW_KEY_KP_8         =(GLFW_KEY_SPECIAL+54),
  GLFW_KEY_KP_9         =(GLFW_KEY_SPECIAL+55),
  GLFW_KEY_KP_DIVIDE    =(GLFW_KEY_SPECIAL+56),
  GLFW_KEY_KP_MULTIPLY  =(GLFW_KEY_SPECIAL+57),
  GLFW_KEY_KP_SUBTRACT  =(GLFW_KEY_SPECIAL+58),
  GLFW_KEY_KP_ADD       =(GLFW_KEY_SPECIAL+59),
  GLFW_KEY_KP_DECIMAL   =(GLFW_KEY_SPECIAL+60),
  GLFW_KEY_KP_EQUAL     =(GLFW_KEY_SPECIAL+61),
  GLFW_KEY_KP_ENTER     =(GLFW_KEY_SPECIAL+62),
  GLFW_KEY_KP_NUM_LOCK  =(GLFW_KEY_SPECIAL+63),
  GLFW_KEY_CAPS_LOCK    =(GLFW_KEY_SPECIAL+64),
  GLFW_KEY_SCROLL_LOCK  =(GLFW_KEY_SPECIAL+65),
  GLFW_KEY_PAUSE        =(GLFW_KEY_SPECIAL+66),
  GLFW_KEY_LSUPER       =(GLFW_KEY_SPECIAL+67),
  GLFW_KEY_RSUPER       =(GLFW_KEY_SPECIAL+68),
  GLFW_KEY_MENU         =(GLFW_KEY_SPECIAL+69),
  GLFW_KEY_LAST         =GLFW_KEY_MENU,

/* Mouse button definitions */
  GLFW_MOUSE_BUTTON_1      =0,
  GLFW_MOUSE_BUTTON_2      =1,
  GLFW_MOUSE_BUTTON_3      =2,
  GLFW_MOUSE_BUTTON_4      =3,
  GLFW_MOUSE_BUTTON_5      =4,
  GLFW_MOUSE_BUTTON_6      =5,
  GLFW_MOUSE_BUTTON_7      =6,
  GLFW_MOUSE_BUTTON_8      =7,
  GLFW_MOUSE_BUTTON_LAST   =GLFW_MOUSE_BUTTON_8,

/* Mouse button aliases */
  GLFW_MOUSE_BUTTON_LEFT   =GLFW_MOUSE_BUTTON_1,
  GLFW_MOUSE_BUTTON_RIGHT  =GLFW_MOUSE_BUTTON_2,
  GLFW_MOUSE_BUTTON_MIDDLE =GLFW_MOUSE_BUTTON_3,


/* Joystick identifiers */
  GLFW_JOYSTICK_1          =0,
  GLFW_JOYSTICK_2          =1,
  GLFW_JOYSTICK_3          =2,
  GLFW_JOYSTICK_4          =3,
  GLFW_JOYSTICK_5          =4,
  GLFW_JOYSTICK_6          =5,
  GLFW_JOYSTICK_7          =6,
  GLFW_JOYSTICK_8          =7,
  GLFW_JOYSTICK_9          =8,
  GLFW_JOYSTICK_10         =9,
  GLFW_JOYSTICK_11         =10,
  GLFW_JOYSTICK_12         =11,
  GLFW_JOYSTICK_13         =12,
  GLFW_JOYSTICK_14         =13,
  GLFW_JOYSTICK_15         =14,
  GLFW_JOYSTICK_16         =15,
  GLFW_JOYSTICK_LAST       =GLFW_JOYSTICK_16,


/*************************************************************************
 * Other definitions
 *************************************************************************/

/* glfwOpenWindow modes */
  GLFW_WINDOW               =0x00010001,
  GLFW_FULLSCREEN           =0x00010002,

/* glfwGetWindowParam tokens */
  GLFW_OPENED               =0x00020001,
  GLFW_ACTIVE               =0x00020002,
  GLFW_ICONIFIED            =0x00020003,
  GLFW_ACCELERATED          =0x00020004,
  GLFW_RED_BITS             =0x00020005,
  GLFW_GREEN_BITS           =0x00020006,
  GLFW_BLUE_BITS            =0x00020007,
  GLFW_ALPHA_BITS           =0x00020008,
  GLFW_DEPTH_BITS           =0x00020009,
  GLFW_STENCIL_BITS         =0x0002000A,

/* The following constants are used for both glfwGetWindowParam
 * and glfwOpenWindowHint
 */
  GLFW_REFRESH_RATE         =0x0002000B,
  GLFW_ACCUM_RED_BITS       =0x0002000C,
  GLFW_ACCUM_GREEN_BITS     =0x0002000D,
  GLFW_ACCUM_BLUE_BITS      =0x0002000E,
  GLFW_ACCUM_ALPHA_BITS     =0x0002000F,
  GLFW_AUX_BUFFERS          =0x00020010,
  GLFW_STEREO               =0x00020011,
  GLFW_WINDOW_NO_RESIZE     =0x00020012,
  GLFW_FSAA_SAMPLES         =0x00020013,
  GLFW_OPENGL_VERSION_MAJOR =0x00020014,
  GLFW_OPENGL_VERSION_MINOR =0x00020015,
  GLFW_OPENGL_FORWARD_COMPAT =0x00020016,
  GLFW_OPENGL_DEBUG_CONTEXT =0x00020017,
  GLFW_OPENGL_PROFILE       =0x00020018,

/* GLFW_OPENGL_PROFILE tokens */
  GLFW_OPENGL_CORE_PROFILE  =0x00050001,
  GLFW_OPENGL_COMPAT_PROFILE =0x00050002,

/* glfwEnable/glfwDisable tokens */
  GLFW_MOUSE_CURSOR         =0x00030001,
  GLFW_STICKY_KEYS          =0x00030002,
  GLFW_STICKY_MOUSE_BUTTONS =0x00030003,
  GLFW_SYSTEM_KEYS          =0x00030004,
  GLFW_KEY_REPEAT           =0x00030005,
  GLFW_AUTO_POLL_EVENTS     =0x00030006,

/* glfwWaitThread wait modes */
  GLFW_WAIT                 =0x00040001,
  GLFW_NOWAIT               =0x00040002,

/* glfwGetJoystickParam tokens */
  GLFW_PRESENT              =0x00050001,
  GLFW_AXES                 =0x00050002,
  GLFW_BUTTONS              =0x00050003,

/* glfwReadImage/glfwLoadTexture2D flags */
  GLFW_NO_RESCALE_BIT       =0x00000001 /* Only for glfwReadImage */,
  GLFW_ORIGIN_UL_BIT        =0x00000002,
  GLFW_BUILD_MIPMAPS_BIT    =0x00000004 /* Only for glfwLoadTexture2D */,
  GLFW_ALPHA_MAP_BIT        =0x00000008,

/* Time spans longer than this (seconds) are considered to be infinity */
};

const float GLFW_INFINITY =100000.0;

/* The video mode structure used by glfwGetVideoModes() */
typedef struct {
    int Width, Height;
    int RedBits, BlueBits, GreenBits;
} GLFWvidmode;

/* Image/texture information */
typedef struct {
    int Width, Height;
    int Format;
    int BytesPerPixel;
    unsigned char *Data;
} GLFWimage;

/* Thread ID */
typedef int GLFWthread;

/* Mutex object */
typedef void * GLFWmutex;

/* Condition variable object */
typedef void * GLFWcond;

/* Function pointer types */
typedef void (GLFWCALL * GLFWwindowsizefun)(int,int);
typedef int  (GLFWCALL * GLFWwindowclosefun)(void);
typedef void (GLFWCALL * GLFWwindowrefreshfun)(void);
typedef void (GLFWCALL * GLFWmousebuttonfun)(int,int);
typedef void (GLFWCALL * GLFWmouseposfun)(int,int);
typedef void (GLFWCALL * GLFWmousewheelfun)(int);
typedef void (GLFWCALL * GLFWkeyfun)(int,int);
typedef void (GLFWCALL * GLFWcharfun)(int,int);
typedef void (GLFWCALL * GLFWthreadfun)(void *);


/*************************************************************************
 * Prototypes
 *************************************************************************/

/* GLFW initialization, termination and version querying */
int  glfwInit( void );
void glfwTerminate( void );
void glfwGetVersion( int *major, int *minor, int *rev );

/* Window handling */
int  glfwOpenWindow( int width, int height, int redbits, int greenbits, int bluebits, int alphabits, int depthbits, int stencilbits, int mode );
void glfwOpenWindowHint( int target, int hint );
void glfwCloseWindow( void );
void glfwSetWindowTitle( const char *title );
void glfwGetWindowSize( int *width, int *height );
void glfwSetWindowSize( int width, int height );
void glfwSetWindowPos( int x, int y );
void glfwIconifyWindow( void );
void glfwRestoreWindow( void );
void glfwSwapBuffers( void );
void glfwSwapInterval( int interval );
int  glfwGetWindowParam( int param );
void glfwSetWindowSizeCallback( GLFWwindowsizefun cbfun );
void glfwSetWindowCloseCallback( GLFWwindowclosefun cbfun );
void glfwSetWindowRefreshCallback( GLFWwindowrefreshfun cbfun );

/* Video mode functions */
int  glfwGetVideoModes( GLFWvidmode *list, int maxcount );
void glfwGetDesktopMode( GLFWvidmode *mode );

/* Input handling */
void glfwPollEvents( void );
void glfwWaitEvents( void );
int  glfwGetKey( int key );
int  glfwGetMouseButton( int button );
void glfwGetMousePos( int *xpos, int *ypos );
void glfwSetMousePos( int xpos, int ypos );
int  glfwGetMouseWheel( void );
void glfwSetMouseWheel( int pos );
void glfwSetKeyCallback( GLFWkeyfun cbfun );
void glfwSetCharCallback( GLFWcharfun cbfun );
void glfwSetMouseButtonCallback( GLFWmousebuttonfun cbfun );
void glfwSetMousePosCallback( GLFWmouseposfun cbfun );
void glfwSetMouseWheelCallback( GLFWmousewheelfun cbfun );

/* Joystick input */
int glfwGetJoystickParam( int joy, int param );
int glfwGetJoystickPos( int joy, float *pos, int numaxes );
int glfwGetJoystickButtons( int joy, unsigned char *buttons, int numbuttons );

/* Time */
double glfwGetTime( void );
void   glfwSetTime( double time );
void   glfwSleep( double time );

/* Extension support */
int   glfwExtensionSupported( const char *extension );
void* glfwGetProcAddress( const char *procname );
void  glfwGetGLVersion( int *major, int *minor, int *rev );

/* Threading support */
GLFWthread glfwCreateThread( GLFWthreadfun fun, void *arg );
void glfwDestroyThread( GLFWthread ID );
int  glfwWaitThread( GLFWthread ID, int waitmode );
GLFWthread glfwGetThreadID( void );
GLFWmutex glfwCreateMutex( void );
void glfwDestroyMutex( GLFWmutex mutex );
void glfwLockMutex( GLFWmutex mutex );
void glfwUnlockMutex( GLFWmutex mutex );
GLFWcond glfwCreateCond( void );
void glfwDestroyCond( GLFWcond cond );
void glfwWaitCond( GLFWcond cond, GLFWmutex mutex, double timeout );
void glfwSignalCond( GLFWcond cond );
void glfwBroadcastCond( GLFWcond cond );
int  glfwGetNumberOfProcessors( void );

/* Enable/disable functions */
void glfwEnable( int token );
void glfwDisable( int token );

/* Image/texture I/O support */
int  glfwReadImage( const char *name, GLFWimage *img, int flags );
int  glfwReadMemoryImage( const void *data, long size, GLFWimage *img, int flags );
void glfwFreeImage( GLFWimage *img );
int  glfwLoadTexture2D( const char *name, int flags );
int  glfwLoadMemoryTexture2D( const void *data, long size, int flags );
int  glfwLoadTextureImage2D( GLFWimage *img, int flags );
]]
  --auto-generated api from ffi headers
  local api =
         {
         ["GLFW_VERSION_MAJOR"] = { type ='value', },
       ["GLFW_VERSION_MINOR"] = { type ='value', },
       ["GLFW_VERSION_REVISION"] = { type ='value', },
       ["GLFW_RELEASE"] = { type ='value', },
       ["GLFW_PRESS"] = { type ='value', },
       ["GLFW_KEY_UNKNOWN"] = { type ='value', },
       ["GLFW_KEY_SPACE"] = { type ='value', },
       ["GLFW_KEY_APOSTROPHE"] = { type ='value', },
       ["GLFW_KEY_COMMA"] = { type ='value', },
       ["GLFW_KEY_MINUS"] = { type ='value', },
       ["GLFW_KEY_PERIOD"] = { type ='value', },
       ["GLFW_KEY_SLASH"] = { type ='value', },
       ["GLFW_KEY_0"] = { type ='value', },
       ["GLFW_KEY_1"] = { type ='value', },
       ["GLFW_KEY_2"] = { type ='value', },
       ["GLFW_KEY_3"] = { type ='value', },
       ["GLFW_KEY_4"] = { type ='value', },
       ["GLFW_KEY_5"] = { type ='value', },
       ["GLFW_KEY_6"] = { type ='value', },
       ["GLFW_KEY_7"] = { type ='value', },
       ["GLFW_KEY_8"] = { type ='value', },
       ["GLFW_KEY_9"] = { type ='value', },
       ["GLFW_KEY_SEMICOLON"] = { type ='value', },
       ["GLFW_KEY_EQUAL"] = { type ='value', },
       ["GLFW_KEY_A"] = { type ='value', },
       ["GLFW_KEY_B"] = { type ='value', },
       ["GLFW_KEY_C"] = { type ='value', },
       ["GLFW_KEY_D"] = { type ='value', },
       ["GLFW_KEY_E"] = { type ='value', },
       ["GLFW_KEY_F"] = { type ='value', },
       ["GLFW_KEY_G"] = { type ='value', },
       ["GLFW_KEY_H"] = { type ='value', },
       ["GLFW_KEY_I"] = { type ='value', },
       ["GLFW_KEY_J"] = { type ='value', },
       ["GLFW_KEY_K"] = { type ='value', },
       ["GLFW_KEY_L"] = { type ='value', },
       ["GLFW_KEY_M"] = { type ='value', },
       ["GLFW_KEY_N"] = { type ='value', },
       ["GLFW_KEY_O"] = { type ='value', },
       ["GLFW_KEY_P"] = { type ='value', },
       ["GLFW_KEY_Q"] = { type ='value', },
       ["GLFW_KEY_R"] = { type ='value', },
       ["GLFW_KEY_S"] = { type ='value', },
       ["GLFW_KEY_T"] = { type ='value', },
       ["GLFW_KEY_U"] = { type ='value', },
       ["GLFW_KEY_V"] = { type ='value', },
       ["GLFW_KEY_W"] = { type ='value', },
       ["GLFW_KEY_X"] = { type ='value', },
       ["GLFW_KEY_Y"] = { type ='value', },
       ["GLFW_KEY_Z"] = { type ='value', },
       ["GLFW_KEY_LEFT_BRACKET"] = { type ='value', },
       ["GLFW_KEY_BACKSLASH"] = { type ='value', },
       ["GLFW_KEY_RIGHT_BRACKET"] = { type ='value', },
       ["GLFW_KEY_GRAVE_ACCENT"] = { type ='value', },
       ["GLFW_KEY_WORLD_1"] = { type ='value', },
       ["GLFW_KEY_WORLD_2"] = { type ='value', },
       ["GLFW_KEY_SPECIAL"] = { type ='value', },
       ["GLFW_KEY_ESC"] = { type ='value', },
       ["GLFW_KEY_F1"] = { type ='value', },
       ["GLFW_KEY_F2"] = { type ='value', },
       ["GLFW_KEY_F3"] = { type ='value', },
       ["GLFW_KEY_F4"] = { type ='value', },
       ["GLFW_KEY_F5"] = { type ='value', },
       ["GLFW_KEY_F6"] = { type ='value', },
       ["GLFW_KEY_F7"] = { type ='value', },
       ["GLFW_KEY_F8"] = { type ='value', },
       ["GLFW_KEY_F9"] = { type ='value', },
       ["GLFW_KEY_F10"] = { type ='value', },
       ["GLFW_KEY_F11"] = { type ='value', },
       ["GLFW_KEY_F12"] = { type ='value', },
       ["GLFW_KEY_F13"] = { type ='value', },
       ["GLFW_KEY_F14"] = { type ='value', },
       ["GLFW_KEY_F15"] = { type ='value', },
       ["GLFW_KEY_F16"] = { type ='value', },
       ["GLFW_KEY_F17"] = { type ='value', },
       ["GLFW_KEY_F18"] = { type ='value', },
       ["GLFW_KEY_F19"] = { type ='value', },
       ["GLFW_KEY_F20"] = { type ='value', },
       ["GLFW_KEY_F21"] = { type ='value', },
       ["GLFW_KEY_F22"] = { type ='value', },
       ["GLFW_KEY_F23"] = { type ='value', },
       ["GLFW_KEY_F24"] = { type ='value', },
       ["GLFW_KEY_F25"] = { type ='value', },
       ["GLFW_KEY_UP"] = { type ='value', },
       ["GLFW_KEY_DOWN"] = { type ='value', },
       ["GLFW_KEY_LEFT"] = { type ='value', },
       ["GLFW_KEY_RIGHT"] = { type ='value', },
       ["GLFW_KEY_LSHIFT"] = { type ='value', },
       ["GLFW_KEY_RSHIFT"] = { type ='value', },
       ["GLFW_KEY_LCTRL"] = { type ='value', },
       ["GLFW_KEY_RCTRL"] = { type ='value', },
       ["GLFW_KEY_LALT"] = { type ='value', },
       ["GLFW_KEY_RALT"] = { type ='value', },
       ["GLFW_KEY_TAB"] = { type ='value', },
       ["GLFW_KEY_ENTER"] = { type ='value', },
       ["GLFW_KEY_BACKSPACE"] = { type ='value', },
       ["GLFW_KEY_INSERT"] = { type ='value', },
       ["GLFW_KEY_DEL"] = { type ='value', },
       ["GLFW_KEY_PAGEUP"] = { type ='value', },
       ["GLFW_KEY_PAGEDOWN"] = { type ='value', },
       ["GLFW_KEY_HOME"] = { type ='value', },
       ["GLFW_KEY_END"] = { type ='value', },
       ["GLFW_KEY_KP_0"] = { type ='value', },
       ["GLFW_KEY_KP_1"] = { type ='value', },
       ["GLFW_KEY_KP_2"] = { type ='value', },
       ["GLFW_KEY_KP_3"] = { type ='value', },
       ["GLFW_KEY_KP_4"] = { type ='value', },
       ["GLFW_KEY_KP_5"] = { type ='value', },
       ["GLFW_KEY_KP_6"] = { type ='value', },
       ["GLFW_KEY_KP_7"] = { type ='value', },
       ["GLFW_KEY_KP_8"] = { type ='value', },
       ["GLFW_KEY_KP_9"] = { type ='value', },
       ["GLFW_KEY_KP_DIVIDE"] = { type ='value', },
       ["GLFW_KEY_KP_MULTIPLY"] = { type ='value', },
       ["GLFW_KEY_KP_SUBTRACT"] = { type ='value', },
       ["GLFW_KEY_KP_ADD"] = { type ='value', },
       ["GLFW_KEY_KP_DECIMAL"] = { type ='value', },
       ["GLFW_KEY_KP_EQUAL"] = { type ='value', },
       ["GLFW_KEY_KP_ENTER"] = { type ='value', },
       ["GLFW_KEY_KP_NUM_LOCK"] = { type ='value', },
       ["GLFW_KEY_CAPS_LOCK"] = { type ='value', },
       ["GLFW_KEY_SCROLL_LOCK"] = { type ='value', },
       ["GLFW_KEY_PAUSE"] = { type ='value', },
       ["GLFW_KEY_LSUPER"] = { type ='value', },
       ["GLFW_KEY_RSUPER"] = { type ='value', },
       ["GLFW_KEY_MENU"] = { type ='value', },
       ["GLFW_KEY_LAST"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_1"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_2"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_3"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_4"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_5"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_6"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_7"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_8"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_LAST"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_LEFT"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_RIGHT"] = { type ='value', },
       ["GLFW_MOUSE_BUTTON_MIDDLE"] = { type ='value', },
       ["GLFW_JOYSTICK_1"] = { type ='value', },
       ["GLFW_JOYSTICK_2"] = { type ='value', },
       ["GLFW_JOYSTICK_3"] = { type ='value', },
       ["GLFW_JOYSTICK_4"] = { type ='value', },
       ["GLFW_JOYSTICK_5"] = { type ='value', },
       ["GLFW_JOYSTICK_6"] = { type ='value', },
       ["GLFW_JOYSTICK_7"] = { type ='value', },
       ["GLFW_JOYSTICK_8"] = { type ='value', },
       ["GLFW_JOYSTICK_9"] = { type ='value', },
       ["GLFW_JOYSTICK_10"] = { type ='value', },
       ["GLFW_JOYSTICK_11"] = { type ='value', },
       ["GLFW_JOYSTICK_12"] = { type ='value', },
       ["GLFW_JOYSTICK_13"] = { type ='value', },
       ["GLFW_JOYSTICK_14"] = { type ='value', },
       ["GLFW_JOYSTICK_15"] = { type ='value', },
       ["GLFW_JOYSTICK_16"] = { type ='value', },
       ["GLFW_JOYSTICK_LAST"] = { type ='value', },
       ["GLFW_WINDOW"] = { type ='value', },
       ["GLFW_FULLSCREEN"] = { type ='value', },
       ["GLFW_OPENED"] = { type ='value', },
       ["GLFW_ACTIVE"] = { type ='value', },
       ["GLFW_ICONIFIED"] = { type ='value', },
       ["GLFW_ACCELERATED"] = { type ='value', },
       ["GLFW_RED_BITS"] = { type ='value', },
       ["GLFW_GREEN_BITS"] = { type ='value', },
       ["GLFW_BLUE_BITS"] = { type ='value', },
       ["GLFW_ALPHA_BITS"] = { type ='value', },
       ["GLFW_DEPTH_BITS"] = { type ='value', },
       ["GLFW_STENCIL_BITS"] = { type ='value', },
       ["GLFW_REFRESH_RATE"] = { type ='value', },
       ["GLFW_ACCUM_RED_BITS"] = { type ='value', },
       ["GLFW_ACCUM_GREEN_BITS"] = { type ='value', },
       ["GLFW_ACCUM_BLUE_BITS"] = { type ='value', },
       ["GLFW_ACCUM_ALPHA_BITS"] = { type ='value', },
       ["GLFW_AUX_BUFFERS"] = { type ='value', },
       ["GLFW_STEREO"] = { type ='value', },
       ["GLFW_WINDOW_NO_RESIZE"] = { type ='value', },
       ["GLFW_FSAA_SAMPLES"] = { type ='value', },
       ["GLFW_OPENGL_VERSION_MAJOR"] = { type ='value', },
       ["GLFW_OPENGL_VERSION_MINOR"] = { type ='value', },
       ["GLFW_OPENGL_FORWARD_COMPAT"] = { type ='value', },
       ["GLFW_OPENGL_DEBUG_CONTEXT"] = { type ='value', },
       ["GLFW_OPENGL_PROFILE"] = { type ='value', },
       ["GLFW_OPENGL_CORE_PROFILE"] = { type ='value', },
       ["GLFW_OPENGL_COMPAT_PROFILE"] = { type ='value', },
       ["GLFW_MOUSE_CURSOR"] = { type ='value', },
       ["GLFW_STICKY_KEYS"] = { type ='value', },
       ["GLFW_STICKY_MOUSE_BUTTONS"] = { type ='value', },
       ["GLFW_SYSTEM_KEYS"] = { type ='value', },
       ["GLFW_KEY_REPEAT"] = { type ='value', },
       ["GLFW_AUTO_POLL_EVENTS"] = { type ='value', },
       ["GLFW_WAIT"] = { type ='value', },
       ["GLFW_NOWAIT"] = { type ='value', },
       ["GLFW_PRESENT"] = { type ='value', },
       ["GLFW_AXES"] = { type ='value', },
       ["GLFW_BUTTONS"] = { type ='value', },
       ["GLFW_NO_RESCALE_BIT"] = { type ='value', },
       ["GLFW_ORIGIN_UL_BIT"] = { type ='value', },
       ["GLFW_BUILD_MIPMAPS_BIT"] = { type ='value', },
       ["GLFW_ALPHA_MAP_BIT"] = { type ='value', },
       ["glfwInit"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(void)", },
       ["glfwTerminate"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(void)", },
       ["glfwGetVersion"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int *major, int *minor, int *rev)", },
       ["glfwOpenWindow"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int width, int height, int redbits, int greenbits, int bluebits, int alphabits, int depthbits, int stencilbits, int mode)", },
       ["glfwOpenWindowHint"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int target, int hint)", },
       ["glfwCloseWindow"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(void)", },
       ["glfwSetWindowTitle"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(const char *title)", },
       ["glfwGetWindowSize"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int *width, int *height)", },
       ["glfwSetWindowSize"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int width, int height)", },
       ["glfwSetWindowPos"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int x, int y)", },
       ["glfwIconifyWindow"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(void)", },
       ["glfwRestoreWindow"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(void)", },
       ["glfwSwapBuffers"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(void)", },
       ["glfwSwapInterval"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int interval)", },
       ["glfwGetWindowParam"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int param)", },
       ["glfwSetWindowSizeCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWwindowsizefun cbfun)", },
       ["glfwSetWindowCloseCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWwindowclosefun cbfun)", },
       ["glfwSetWindowRefreshCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWwindowrefreshfun cbfun)", },
       ["glfwGetVideoModes"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(GLFWvidmode *list, int maxcount)", },
       ["glfwGetDesktopMode"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWvidmode *mode)", },
       ["glfwPollEvents"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(void)", },
       ["glfwWaitEvents"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(void)", },
       ["glfwGetKey"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int key)", },
       ["glfwGetMouseButton"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int button)", },
       ["glfwGetMousePos"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int *xpos, int *ypos)", },
       ["glfwSetMousePos"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int xpos, int ypos)", },
       ["glfwGetMouseWheel"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(void)", },
       ["glfwSetMouseWheel"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int pos)", },
       ["glfwSetKeyCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWkeyfun cbfun)", },
       ["glfwSetCharCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWcharfun cbfun)", },
       ["glfwSetMouseButtonCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWmousebuttonfun cbfun)", },
       ["glfwSetMousePosCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWmouseposfun cbfun)", },
       ["glfwSetMouseWheelCallback"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWmousewheelfun cbfun)", },
       ["glfwGetJoystickParam"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int joy, int param)", },
       ["glfwGetJoystickPos"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int joy, float *pos, int numaxes)", },
       ["glfwGetJoystickButtons"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int joy, unsigned char *buttons, int numbuttons)", },
       ["glfwGetTime"] = { type ='function',
          description = "",
          returns = "(double)",
          valuetype = nil,
          args = "(void)", },
       ["glfwSetTime"] = { type ='function',
          description = "",
          returns = "(void)",
          valuetype = nil,
          args = "(double time)", },
       ["glfwSleep"] = { type ='function',
          description = "",
          returns = "(void)",
          valuetype = nil,
          args = "(double time)", },
       ["glfwExtensionSupported"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const char *extension)", },
       ["glfwGetProcAddress"] = { type ='function',
          description = "",
          returns = "(void*)",
          valuetype = nil,
          args = "(const char *procname)", },
       ["glfwGetGLVersion"] = { type ='function',
          description = "",
          returns = "(void)",
          valuetype = nil,
          args = "(int *major, int *minor, int *rev)", },
       ["glfwCreateThread"] = { type ='function',
          description = "",
          returns = "(GLFWthread)",
          valuetype = nil,
          args = "(GLFWthreadfun fun, void *arg)", },
       ["glfwDestroyThread"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWthread ID)", },
       ["glfwWaitThread"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(GLFWthread ID, int waitmode)", },
       ["glfwGetThreadID"] = { type ='function',
          description = "",
          returns = "(GLFWthread)",
          valuetype = nil,
          args = "(void)", },
       ["glfwCreateMutex"] = { type ='function',
          description = "",
          returns = "(GLFWmutex)",
          valuetype = nil,
          args = "(void)", },
       ["glfwDestroyMutex"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWmutex mutex)", },
       ["glfwLockMutex"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWmutex mutex)", },
       ["glfwUnlockMutex"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWmutex mutex)", },
       ["glfwCreateCond"] = { type ='function',
          description = "",
          returns = "(GLFWcond)",
          valuetype = nil,
          args = "(void)", },
       ["glfwDestroyCond"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWcond cond)", },
       ["glfwWaitCond"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWcond cond, GLFWmutex mutex, double timeout)", },
       ["glfwSignalCond"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWcond cond)", },
       ["glfwBroadcastCond"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWcond cond)", },
       ["glfwGetNumberOfProcessors"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(void)", },
       ["glfwEnable"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int token)", },
       ["glfwDisable"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(int token)", },
       ["glfwReadImage"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const char *name, GLFWimage *img, int flags)", },
       ["glfwReadMemoryImage"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const void *data, long size, GLFWimage *img, int flags)", },
       ["glfwFreeImage"] = { type ='function',
          description = "",
          returns = "()",
          valuetype = nil,
          args = "(GLFWimage *img)", },
       ["glfwLoadTexture2D"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const char *name, int flags)", },
       ["glfwLoadMemoryTexture2D"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const void *data, long size, int flags)", },
       ["glfwLoadTextureImage2D"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(GLFWimage *img, int flags)", },
       ["GLFWvidmode"] = { type ='class',
          description = "",
          childs =         {
          ["Width"] = { type ='value', description = "int", valuetype = nil, },
        ["Height"] = { type ='value', description = "int", valuetype = nil, },
        ["RedBits"] = { type ='value', description = "int", valuetype = nil, },
        ["BlueBits"] = { type ='value', description = "int", valuetype = nil, },
        ["GreenBits"] = { type ='value', description = "int", valuetype = nil, },
                }
         },
       ["GLFWimage"] = { type ='class',
          description = "",
          childs =         {
          ["Width"] = { type ='value', description = "int", valuetype = nil, },
        ["Height"] = { type ='value', description = "int", valuetype = nil, },
        ["Format"] = { type ='value', description = "int", valuetype = nil, },
        ["BytesPerPixel"] = { type ='value', description = "int", valuetype = nil, },
                }
         },
               }
  return {
        glfw = {
      type = 'lib',
      description = "GLFW window manager",
      childs = api,
    },
      }
  