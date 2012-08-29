--[[// tw tweakbar | AntTweakBar UI
      enum { TW_VERSION = 114 }

      typedef enum ETwType {
	 TW_TYPE_UNDEF,
	 TW_TYPE_BOOLCPP,
	 TW_TYPE_BOOL8,
	 TW_TYPE_BOOL16,
	 TW_TYPE_BOOL32,
	 TW_TYPE_CHAR,
	 TW_TYPE_INT8,
	 TW_TYPE_UINT8,
	 TW_TYPE_INT16,
	 TW_TYPE_UINT16,
	 TW_TYPE_INT32,
	 TW_TYPE_UINT32,
	 TW_TYPE_FLOAT,
	 TW_TYPE_DOUBLE,
	 TW_TYPE_COLOR32,    // 32 bits color. Order is RGBA if API is OpenGL or Direct3D10, and inversed if API is Direct3D9 (can be modified by defining 'colorOrder=...', see doc)
	 TW_TYPE_COLOR3F,    // 3 floats color. Order is RGB.
	 TW_TYPE_COLOR4F,    // 4 floats color. Order is RGBA.
	 TW_TYPE_CDSTRING,   // Null-terminated C Dynamic String (pointer to an array of char dynamically allocated with malloc/realloc/strdup)
	 TW_TYPE__TEMP1,     // 
	 TW_TYPE_QUAT4F,     // 4 floats encoding a quaternion {qx,qy,qz,qs}
	 TW_TYPE_QUAT4D,     // 4 doubles encoding a quaternion {qx,qy,qz,qs}
	 TW_TYPE_DIR3F,      // direction vector represented by 3 floats
	 TW_TYPE_DIR3D,      // direction vector represented by 3 doubles
	 TW_TYPE_CSSTRING_LEN0   = 0x30000000,
	 TW_TYPE_CSSTRING_LEN256 = 0x30000000 + 256,
      } TwType;

      typedef struct CTwEnumVal  {
	 int           Value;
	 const char *  Label;
      } TwEnumVal;

      typedef struct CTwStructMember  {
	 const char *  Name;
	 TwType        Type;
	 size_t        Offset;
	 const char *  DefString;
      } TwStructMember;
      
      typedef enum ETwParamValueType {
	 TW_PARAM_INT32,
	 TW_PARAM_FLOAT,
	 TW_PARAM_DOUBLE,
	 TW_PARAM_CSTRING // Null-terminated array of char (ie, c-string)
      } TwParamValueType;
      
      typedef enum ETwGraphAPI {
	 TW_OPENGL     = 1,
	 TW_DIRECT3D9  = 2,
	 TW_DIRECT3D10 = 3,
	 TW_DIRECT3D11 = 4
      } TwGraphAPI;
      
      typedef enum ETwKeyModifier {
	 TW_KMOD_NONE  = 0x0000,   // same codes as SDL keysym.mod
	 TW_KMOD_SHIFT = 0x0003,
	 TW_KMOD_CTRL  = 0x00c0,
	 TW_KMOD_ALT   = 0x0100,
	 TW_KMOD_META  = 0x0c00
      } TwKeyModifier;
       
      typedef enum EKeySpecial {
	 TW_KEY_BACKSPACE  = '\b',
	 TW_KEY_TAB        = '\t',
	 TW_KEY_CLEAR      = 0x0c,
	 TW_KEY_RETURN     = '\r',
	 TW_KEY_PAUSE      = 0x13,
	 TW_KEY_ESCAPE     = 0x1b,
	 TW_KEY_SPACE      = ' ',
	 TW_KEY_DELETE     = 0x7f,
	 TW_KEY_UP         = 273,
	 TW_KEY_DOWN,
	 TW_KEY_RIGHT,
	 TW_KEY_LEFT,
	 TW_KEY_INSERT,
	 TW_KEY_HOME,
	 TW_KEY_END,
	 TW_KEY_PAGE_UP,
	 TW_KEY_PAGE_DOWN,
	 TW_KEY_F1,
	 TW_KEY_F2,
	 TW_KEY_F3,
	 TW_KEY_F4,
	 TW_KEY_F5,
	 TW_KEY_F6,
	 TW_KEY_F7,
	 TW_KEY_F8,
	 TW_KEY_F9,
	 TW_KEY_F10,
	 TW_KEY_F11,
	 TW_KEY_F12,
	 TW_KEY_F13,
	 TW_KEY_F14,
	 TW_KEY_F15,
	 TW_KEY_LAST
      } TwKeySpecial;

      typedef enum ETwMouseAction {
	 TW_MOUSE_RELEASED,
	 TW_MOUSE_PRESSED  
      } TwMouseAction;
      
      typedef enum ETwMouseButtonID {
	 TW_MOUSE_LEFT   = 1,
	 TW_MOUSE_MIDDLE = 2,
	 TW_MOUSE_RIGHT  = 3 
      } TwMouseButtonID;

      typedef void (*TwSetVarCallback)       ( const void *value, void *clientData );
      typedef void (*TwGetVarCallback)       (       void *value, void *clientData );
      typedef void (*TwButtonCallback)       (                    void *clientData );
      typedef void (*TwSummaryCallback)      ( char *summaryString, size_t summaryMaxLength, const void *value, void *clientData );
      typedef void (*TwCopyCDStringToClient) ( char **destinationClientStringPtr, const char *sourceString );
      typedef void (*TwErrorHandler)         ( const char *errorMessage );
      typedef void (*TwGLUTmousebuttonfun)   ( int glutButton, int glutState, int mouseX, int mouseY );
      typedef void (*TwGLUTmousemotionfun)   ( int mouseX, int mouseY );
      typedef void (*TwGLUTkeyboardfun)      ( unsigned char glutKey, int mouseX, int mouseY );
      typedef void (*TwGLUTspecialfun)       ( int glutKey, int mouseX, int mouseY );
      
      typedef struct CTwBar TwBar;

      TwBar*      TwNewBar(                    const char *barName );
      int         TwDeleteBar(                 TwBar *bar          );
      int         TwDeleteAllBars(                                 );
      int         TwSetTopBar(                 const TwBar *bar    );
      TwBar*      TwGetTopBar(                                     );
      int         TwSetBottomBar(              const TwBar *bar    );
      TwBar*      TwGetBottomBar(                                  );
      const char* TwGetBarName(                TwBar *bar          );
      int         TwGetBarCount(                                   );
      TwBar*      TwGetBarByIndex(             int barIndex        );
      TwBar*      TwGetBarByName(              const char *barName );
      int         TwRefreshBar(                TwBar *bar          );
      int         TwAddVarRW(                  TwBar *bar, const char *name, TwType type, void *var, const char *def );
      int         TwAddVarRO(                  TwBar *bar, const char *name, TwType type, const void *var, const char *def );
      int         TwAddVarCB(                  TwBar *bar, const char *name, TwType type, TwSetVarCallback setCallback, TwGetVarCallback getCallback, void *clientData, const char *def );
      int         TwAddButton(                 TwBar *bar, const char *name, TwButtonCallback callback, void *clientData, const char *def );
      int         TwAddSeparator(              TwBar *bar, const char *name, const char *def );
      int         TwRemoveVar(                 TwBar *bar, const char *name );
      int         TwRemoveAllVars(             TwBar *bar );
      int         TwDefine(                    const char *def );
      TwType      TwDefineEnum(                const char *name, const TwEnumVal *enumValues, unsigned int nbValues );
      TwType      TwDefineEnumFromString(      const char *name, const char *enumString );
      TwType      TwDefineStruct(              const char *name, const TwStructMember *structMembers, unsigned int nbMembers, size_t structSize, TwSummaryCallback summaryCallback, void *summaryClientData );
      void        TwCopyCDStringToClientFunc(  TwCopyCDStringToClient copyCDStringFunc );
      void        TwCopyCDStringToLibrary(     char **destinationLibraryStringPtr, const char *sourceClientString );
      int         TwGetParam(                  TwBar *bar, const char *varName, const char *paramName, TwParamValueType paramValueType, unsigned int outValueMaxCount,      void *outValues );
      int         TwSetParam(                  TwBar *bar, const char *varName, const char *paramName, TwParamValueType paramValueType, unsigned int     inValueCount, const void *inValues );
      int         TwInit(                      TwGraphAPI graphAPI, void *device );
      int         TwTerminate();
      int         TwDraw();
      int         TwWindowSize(                int width, int height);
      int         TwSetCurrentWindow(          int windowID); // multi-windows support
      int         TwGetCurrentWindow();
      int         TwWindowExists(              int windowID);
      int         TwKeyPressed(                int key, int modifiers);
      int         TwKeyTest(                   int key, int modifiers);
      int         TwMouseButton(               TwMouseAction action, TwMouseButtonID button);
      int         TwMouseMotion(               int mouseX, int mouseY);
      int         TwMouseWheel(                int pos);
      const char* TwGetLastError(              );
      void        TwHandleErrors(              TwErrorHandler errorHandler);
      int         TwEventSDL(                  const void *sdlEvent, unsigned char sdlMajorVersion, unsigned char sdlMinorVersion);
      int         TwEventSDL12(                const void *sdlEvent);
      int         TwEventSDL13(                const void *sdlEvent);
      int         TwEventMouseButtonGLFW(      int glfwButton, int glfwAction );
      int         TwEventKeyGLFW(              int glfwKey,    int glfwAction );
      int         TwEventCharGLFW(             int glfwChar,   int glfwAction );
      int         TwEventMouseButtonGLUT(      int glutButton, int glutState, int mouseX, int mouseY);
      int         TwEventMouseMotionGLUT(      int mouseX,     int mouseY);
      int         TwEventKeyboardGLUT(         unsigned char glutKey, int mouseX, int mouseY );
      int         TwEventSpecialGLUT(          int           glutKey, int mouseX, int mouseY );
      int         TwGLUTModifiersFunc(         int (*glutGetModifiersFunc)(void) );
      int         TwEventSFML(                 const void *sfmlEvent, unsigned char sfmlMajorVersion, unsigned char sfmlMinorVersion);
]]
  --auto-generated api from ffi headers
  local api =
         {
         ["TW_TYPE_UNDEF"] = { type ='value', },
       ["TW_TYPE_BOOLCPP"] = { type ='value', },
       ["TW_TYPE_BOOL8"] = { type ='value', },
       ["TW_TYPE_BOOL16"] = { type ='value', },
       ["TW_TYPE_BOOL32"] = { type ='value', },
       ["TW_TYPE_CHAR"] = { type ='value', },
       ["TW_TYPE_INT8"] = { type ='value', },
       ["TW_TYPE_UINT8"] = { type ='value', },
       ["TW_TYPE_INT16"] = { type ='value', },
       ["TW_TYPE_UINT16"] = { type ='value', },
       ["TW_TYPE_INT32"] = { type ='value', },
       ["TW_TYPE_UINT32"] = { type ='value', },
       ["TW_TYPE_FLOAT"] = { type ='value', },
       ["TW_TYPE_DOUBLE"] = { type ='value', },
       ["TW_TYPE_COLOR32"] = { type ='value', },
       ["TW_TYPE_COLOR3F"] = { type ='value', },
       ["TW_TYPE_COLOR4F"] = { type ='value', },
       ["TW_TYPE_CDSTRING"] = { type ='value', },
       ["TW_TYPE__TEMP1"] = { type ='value', },
       ["TW_TYPE_QUAT4F"] = { type ='value', },
       ["TW_TYPE_QUAT4D"] = { type ='value', },
       ["TW_TYPE_DIR3F"] = { type ='value', },
       ["TW_TYPE_DIR3D"] = { type ='value', },
       ["TW_TYPE_CSSTRING_LEN0"] = { type ='value', },
       ["TW_TYPE_CSSTRING_LEN256"] = { type ='value', },
       ["TW_PARAM_INT32"] = { type ='value', },
       ["TW_PARAM_FLOAT"] = { type ='value', },
       ["TW_PARAM_DOUBLE"] = { type ='value', },
       ["TW_PARAM_CSTRING"] = { type ='value', },
       ["TW_OPENGL"] = { type ='value', },
       ["TW_DIRECT3D9"] = { type ='value', },
       ["TW_DIRECT3D10"] = { type ='value', },
       ["TW_DIRECT3D11"] = { type ='value', },
       ["TW_KMOD_NONE"] = { type ='value', },
       ["TW_KMOD_SHIFT"] = { type ='value', },
       ["TW_KMOD_CTRL"] = { type ='value', },
       ["TW_KMOD_ALT"] = { type ='value', },
       ["TW_KMOD_META"] = { type ='value', },
       ["TW_KEY_BACKSPACE"] = { type ='value', },
       ["TW_KEY_TAB"] = { type ='value', },
       ["TW_KEY_CLEAR"] = { type ='value', },
       ["TW_KEY_RETURN"] = { type ='value', },
       ["TW_KEY_PAUSE"] = { type ='value', },
       ["TW_KEY_ESCAPE"] = { type ='value', },
       ["TW_KEY_SPACE"] = { type ='value', },
       ["TW_KEY_DELETE"] = { type ='value', },
       ["TW_KEY_UP"] = { type ='value', },
       ["TW_KEY_DOWN"] = { type ='value', },
       ["TW_KEY_RIGHT"] = { type ='value', },
       ["TW_KEY_LEFT"] = { type ='value', },
       ["TW_KEY_INSERT"] = { type ='value', },
       ["TW_KEY_HOME"] = { type ='value', },
       ["TW_KEY_END"] = { type ='value', },
       ["TW_KEY_PAGE_UP"] = { type ='value', },
       ["TW_KEY_PAGE_DOWN"] = { type ='value', },
       ["TW_KEY_F1"] = { type ='value', },
       ["TW_KEY_F2"] = { type ='value', },
       ["TW_KEY_F3"] = { type ='value', },
       ["TW_KEY_F4"] = { type ='value', },
       ["TW_KEY_F5"] = { type ='value', },
       ["TW_KEY_F6"] = { type ='value', },
       ["TW_KEY_F7"] = { type ='value', },
       ["TW_KEY_F8"] = { type ='value', },
       ["TW_KEY_F9"] = { type ='value', },
       ["TW_KEY_F10"] = { type ='value', },
       ["TW_KEY_F11"] = { type ='value', },
       ["TW_KEY_F12"] = { type ='value', },
       ["TW_KEY_F13"] = { type ='value', },
       ["TW_KEY_F14"] = { type ='value', },
       ["TW_KEY_F15"] = { type ='value', },
       ["TW_KEY_LAST"] = { type ='value', },
       ["TW_MOUSE_RELEASED"] = { type ='value', },
       ["TW_MOUSE_PRESSED"] = { type ='value', },
       ["TW_MOUSE_LEFT"] = { type ='value', },
       ["TW_MOUSE_MIDDLE"] = { type ='value', },
       ["TW_MOUSE_RIGHT"] = { type ='value', },
       ["TwNewBar"] = { type ='function',
          description = "",
          returns = "(TwBar*)",
          valuetype = nil,
          args = "(const char *barName)", },
       ["TwDeleteBar"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar)", },
       ["TwDeleteAllBars"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "()", },
       ["TwSetTopBar"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const TwBar *bar)", },
       ["TwGetTopBar"] = { type ='function',
          description = "",
          returns = "(TwBar*)",
          valuetype = nil,
          args = "()", },
       ["TwSetBottomBar"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const TwBar *bar)", },
       ["TwGetBottomBar"] = { type ='function',
          description = "",
          returns = "(TwBar*)",
          valuetype = nil,
          args = "()", },
       ["TwGetBarName"] = { type ='function',
          description = "",
          returns = "(const char*)",
          valuetype = "string",
          args = "(TwBar *bar)", },
       ["TwGetBarCount"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "()", },
       ["TwGetBarByIndex"] = { type ='function',
          description = "",
          returns = "(TwBar*)",
          valuetype = nil,
          args = "(int barIndex)", },
       ["TwGetBarByName"] = { type ='function',
          description = "",
          returns = "(TwBar*)",
          valuetype = nil,
          args = "(const char *barName)", },
       ["TwRefreshBar"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar)", },
       ["TwAddVarRW"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *name, TwType type, void *var, const char *def)", },
       ["TwAddVarRO"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *name, TwType type, const void *var, const char *def)", },
       ["TwAddVarCB"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *name, TwType type, TwSetVarCallback setCallback, TwGetVarCallback getCallback, void *clientData, const char *def)", },
       ["TwAddButton"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *name, TwButtonCallback callback, void *clientData, const char *def)", },
       ["TwAddSeparator"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *name, const char *def)", },
       ["TwRemoveVar"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *name)", },
       ["TwRemoveAllVars"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar)", },
       ["TwDefine"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const char *def)", },
       ["TwDefineEnum"] = { type ='function',
          description = "",
          returns = "(TwType)",
          valuetype = nil,
          args = "(const char *name, const TwEnumVal *enumValues, unsigned int nbValues)", },
       ["TwDefineEnumFromString"] = { type ='function',
          description = "",
          returns = "(TwType)",
          valuetype = nil,
          args = "(const char *name, const char *enumString)", },
       ["TwDefineStruct"] = { type ='function',
          description = "",
          returns = "(TwType)",
          valuetype = nil,
          args = "(const char *name, const TwStructMember *structMembers, unsigned int nbMembers, size_t structSize, TwSummaryCallback summaryCallback, void *summaryClientData)", },
       ["TwCopyCDStringToClientFunc"] = { type ='function',
          description = "",
          returns = "(void)",
          valuetype = nil,
          args = "(TwCopyCDStringToClient copyCDStringFunc)", },
       ["TwCopyCDStringToLibrary"] = { type ='function',
          description = "",
          returns = "(void)",
          valuetype = nil,
          args = "(char **destinationLibraryStringPtr, const char *sourceClientString)", },
       ["TwGetParam"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *varName, const char *paramName, TwParamValueType paramValueType, unsigned int outValueMaxCount,      void *outValues)", },
       ["TwSetParam"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwBar *bar, const char *varName, const char *paramName, TwParamValueType paramValueType, unsigned int     inValueCount, const void *inValues)", },
       ["TwInit"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwGraphAPI graphAPI, void *device)", },
       ["TwTerminate"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "()", },
       ["TwDraw"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "()", },
       ["TwWindowSize"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int width, int height)", },
       ["TwSetCurrentWindow"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int windowID)", },
       ["TwGetCurrentWindow"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "()", },
       ["TwWindowExists"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int windowID)", },
       ["TwKeyPressed"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int key, int modifiers)", },
       ["TwKeyTest"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int key, int modifiers)", },
       ["TwMouseButton"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(TwMouseAction action, TwMouseButtonID button)", },
       ["TwMouseMotion"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int mouseX, int mouseY)", },
       ["TwMouseWheel"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int pos)", },
       ["TwGetLastError"] = { type ='function',
          description = "",
          returns = "(const char*)",
          valuetype = "string",
          args = "()", },
       ["TwHandleErrors"] = { type ='function',
          description = "",
          returns = "(void)",
          valuetype = nil,
          args = "(TwErrorHandler errorHandler)", },
       ["TwEventSDL"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const void *sdlEvent, unsigned char sdlMajorVersion, unsigned char sdlMinorVersion)", },
       ["TwEventSDL12"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const void *sdlEvent)", },
       ["TwEventSDL13"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const void *sdlEvent)", },
       ["TwEventMouseButtonGLFW"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int glfwButton, int glfwAction)", },
       ["TwEventKeyGLFW"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int glfwKey,    int glfwAction)", },
       ["TwEventCharGLFW"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int glfwChar,   int glfwAction)", },
       ["TwEventMouseButtonGLUT"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int glutButton, int glutState, int mouseX, int mouseY)", },
       ["TwEventMouseMotionGLUT"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int mouseX,     int mouseY)", },
       ["TwEventKeyboardGLUT"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(unsigned char glutKey, int mouseX, int mouseY)", },
       ["TwEventSpecialGLUT"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(int           glutKey, int mouseX, int mouseY)", },
       ["glutGetModifiersFunc"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(void))", },
       ["TwEventSFML"] = { type ='function',
          description = "",
          returns = "(int)",
          valuetype = nil,
          args = "(const void *sfmlEvent, unsigned char sfmlMajorVersion, unsigned char sfmlMinorVersion)", },
       ["TwEnumVal"] = { type ='class',
          description = "",
          childs =         {
          ["Value"] = { type ='value', description = "int          ", valuetype = nil, },
        ["Label"] = { type ='value', description = "const char * ", valuetype = "string", },
                }
         },
       ["TwStructMember"] = { type ='class',
          description = "",
          childs =         {
          ["Name"] = { type ='value', description = "const char * ", valuetype = "string", },
        ["Type"] = { type ='value', description = "TwType       ", valuetype = nil, },
        ["Offset"] = { type ='value', description = "size_t       ", valuetype = nil, },
        ["DefString"] = { type ='value', description = "const char * ", valuetype = "string", },
                }
         },
               }
  return {
        tw = {
      type = 'lib',
      description = "AntTweakBar UI",
      childs = api,
    },
        tweakbar = {
      type = 'lib',
      description = "AntTweakBar UI",
      childs = api,
    },
      }
  