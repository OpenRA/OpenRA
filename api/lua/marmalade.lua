-- Copyright 2013-14 Paul Kulchenko, ZeroBrane LLC

return {
  ads = {
    type = "class",
    childs = {
      init = {
        type = "method",
        description = "Initialise the ad system, must be called before the ad system can be used.",
        args = "()",
        returns = "()",
      },
      isAvailable = {
        type = "method",
        description = "Checks availability of ads.",
        args = "()",
        returns = "(boolean)",
      },
      newAd = {
        type = "method",
        description = "Collects and displays a new ad",
        args = "(yPos: number, height: number, adProvider: string, adType: string, adId: string)",
        returns = "()",
      },
      show = {
        type = "method",
        description = "Shows or hides the ad view\nExample::\nif ads:isAvailable() then\nads:newAd(director.displayHeight, 70, \"leadbolt\", \"banner\", \"158316107\")\nelse\ndbg.log(\"Ads are not available\")\nend",
        args = "(visibility: boolean)",
        returns = "()",
      },
    },
  },
  analytics = {
    type = "class",
    childs = {
      endSession = {
        type = "method",
        description = "Ends a Flurry analytics session.",
        args = "()",
        returns = "()",
      },
      isAvailable = {
        type = "method",
        description = "Checks availability of analytics.",
        args = "()",
        returns = "(boolean)",
      },
      logError = {
        type = "method",
        description = "Logs the named error with the supplied message.",
        args = "(name: string, message: string)",
        returns = "()",
      },
      logEvent = {
        type = "method",
        description = "Logs the named event, with optional table of parameters.\nLimitations::\n- Maximum logEvent parameter key length is 255 characters\n- Maximum logEvent parameter value length is 255 characters\n- Maximum logEvent parameters is 100\nExample::\nif analytics:isAvailable() then\nanalytics:startSession(\"YOUR_API_KEY_GOES_HERE\")\nanalytics:logEvent(\"Game Started\")\nanalytics:logEvent(\"Game Menu\", {option=\"Start\", data=\"Selected\", time=\"18:10\"})\nelse\ndbg.log(\"Analytics not available\")\nend",
        args = "(name: string, params: table)",
        returns = "()",
      },
      startSession = {
        type = "method",
        description = "Starts a Flurry analytics session.",
        args = "(apiKey: string)",
        returns = "()",
      },
    },
  },
  animation = {
    type = "class",
    childs = {
      addFrame = {
        type = "method",
        description = "Adds a frame from an atlas to the current animation.",
        args = "(frame: number, atlas: object)",
        returns = "()",
      },
      addFrameByName = {
        type = "method",
        description = "Adds a frame from an atlas to the current animation.",
        args = "(name: string, atlas: object)",
        returns = "()",
      },
      destroy = {
        type = "method",
        description = "Destroy the Animation object.",
        args = "()",
        returns = "()",
      },
      getDuration = {
        type = "method",
        description = "Gets the playing duration of the animation.",
        args = "()",
        returns = "(number)",
      },
      setDelay = {
        type = "method",
        description = "Sets the amount of time, in seconds, each frame of the animation should\nbe displayed for.",
        args = "(delay: number)",
        returns = "()",
      },
    },
  },
  atlas = {
    type = "class",
    childs = {
      addSpriteFrame = {
        type = "method",
        description = "Adds a sprite frame to the atlas.",
        args = "(x: number, y: number, w: number, h: number, rotated: boolean, ox: number, oy: number, sw: number, sh: number)",
        returns = "()",
      },
      destroy = {
        type = "method",
        description = "This function has been removed: atlases are destroyed when they go out of Lua scope, i.e. no Lua\nreferences to them remain.",
        args = "()",
        returns = "()",
      },
      getAtlasTextureSize = {
        type = "method",
        description = "This function has been removed: use getTextureSize() instead.",
        args = "()",
        returns = "()",
      },
      getTextureSize = {
        type = "method",
        description = "Gets the size of the texture as a number pair (w, h).\n:return w (number): The width of the texture (bitmap) in pixels (texels).\n:return h (number): The height of the texture (bitmap) in pixels (texels).",
        args = "()",
        returns = "()",
      },
      initFromFile = {
        type = "method",
        description = "Initialises the atlas from a texture (bitmap) file, or a .plist file.  This operation\nresults in the creation of one or more sprite frames. If you load from a texture file,\nit will create a single sprite frame covering the entire texture.",
        args = "(filename: string)",
        returns = "(boolean)",
      },
      initTexture = {
        type = "method",
        description = "Initialises the atlas' internal texture.  This operation doesn't create\nany sprite frames - the app is expected to add them manually.",
        args = "(filename: string)",
        returns = "(boolean)",
      },
      setBlendFunc = {
        type = "method",
        description = "Used to set the blending function for the atlas texture. For more information, see the OpenGL ES specification page here:\nhttp://www.khronos.org/opengles/sdk/docs/man/xhtml/glBlendFunc.xml\nBoth inputs can take the following values:\n- \"GL_ZERO\"\n- \"GL_ONE\"\n- \"GL_SRC_COLOR\"\n- \"GL_ONE_MINUS_SRC_COLOR\"\n- \"GL_DST_COLOR\"\n- \"GL_ONE_MINUS_DST_COLOR\"\n- \"GL_SRC_ALPHA\"\n- \"GL_ONE_MINUS_SRC_ALPHA\"\n- \"GL_DST_ALPHA\"\n- \"GL_ONE_MINUS_DST_ALPHA\"\n- \"GL_CONSTANT_COLOR\"\n- \"GL_ONE_MINUS_CONSTANT_COLOR\"\n- \"GL_CONSTANT_ALPHA\"\n- \"GL_ONE_MINUS_CONSTANT_ALPHA\"\n- \"GL_SRC_ALPHA_SATURATE\"",
        args = "(blendSrc: string, blendDst: string)",
        returns = "()",
      },
      setTextureParams = {
        type = "method",
        description = "Used to set the minification and magnifaction filtering modes for the atlas texture, and also the texel wrapping\nmodes. For more information, see the OpenGL ES specification page here:\nhttp://www.khronos.org/opengles/sdk/docs/man/xhtml/glTexParameter.xml\n- \"GL_NEAREST_MIPMAP_NEAREST\"\n- \"GL_LINEAR_MIPMAP_NEAREST\"\n- \"GL_NEAREST_MIPMAP_LINEAR\"\n- \"GL_LINEAR_MIPMAP_LINEAR\"\n- \"GL_NEAREST\"\n- \"GL_LINEAR\"\n- \"GL_NEAREST\"\n- \"GL_LINEAR\"\n- \"GL_REPEAT\"\n- \"GL_CLAMP_TO_EDGE\"\n- \"GL_MIRRORED_REPEAT\"\n- \"GL_REPEAT\"\n- \"GL_CLAMP_TO_EDGE\"\n- \"GL_MIRRORED_REPEAT\"\nThese settings will affect all sprites and other nodes that reference this atlas.",
        args = "(minFilter: string, magFilter: string, wrapS: string, wrapT: string)",
        returns = "()",
      },
    },
  },
  audio = {
    type = "class",
    childs = {
      isStreamPlaying = {
        type = "method",
        description = "Determine whether there is currently a stream playing.\nExample::\nif audio:isStreamPlaying() do\ndbg.print(\"Stream is playing\")\nend",
        args = "()",
        returns = "(boolean)",
      },
      loadSound = {
        type = "method",
        description = "Preload a sound file into memory.\nExample::\naudio:loadSound(\"sound/SplashC.pcm\")",
        args = "(filename: string)",
        returns = "()",
      },
      loadStream = {
        type = "method",
        description = "Preload a stream file into memory.\nExample::\naudio:loadStream(\"sound/Loop1.mp3\")",
        args = "(filename: string)",
        returns = "()",
      },
      pauseStream = {
        type = "method",
        description = "Pause any playing stream. Can be restarted with audio:resumeStream().\nExample::\naudio:stopStream()",
        args = "()",
        returns = "()",
      },
      playSound = {
        type = "method",
        description = "Play a sound from a file.\nExample::\nlocal sID = audio:playSound(\"sound/SplashC.pcm\", true)",
        args = "(filename: string [, looped: boolean])",
        returns = "(number)",
      },
      playStream = {
        type = "method",
        description = "Play a stream from a file.\nExample::\naudio:playStreamWithLoop(\"sound/Loop1.mp3\", true)",
        args = "(filename: string [, looped: boolean])",
        returns = "()",
      },
      resumeStream = {
        type = "method",
        description = "Resume any playing stream, provided it was previously paused using audio:pauseStream().\nExample::\naudio:resumeStream()",
        args = "()",
        returns = "()",
      },
      rewindStream = {
        type = "method",
        description = "Rewind the playing (or paused) stream to the start.\nExample::\naudio:resumeStream()",
        args = "()",
        returns = "()",
      },
      stopSound = {
        type = "method",
        description = "Stop the specified sound.\nExample::\nlocal sID = audio:playSound(\"sound/SplashC.pcm\", true)\naudio:stopSound(sID)",
        args = "(id: number)",
        returns = "()",
      },
      stopStream = {
        type = "method",
        description = "Stop any playing stream.\nExample::\naudio:stopStream()",
        args = "()",
        returns = "()",
      },
      unloadSound = {
        type = "method",
        description = "Free the sound file from memory.\nExample::\naudio:unloadSound(\"sound/SplashC.pcm\")",
        args = "(filename: string)",
        returns = "()",
      },
    },
  },
  rect = {
    type = "class",
    childs = {
      h = {
        type = "value",
        description = "The height coordinate of the rectangle.",
      },
      w = {
        type = "value",
        description = "The width of the rectangle.",
      },
      x = {
        type = "value",
        description = "The x coordinate of the rectangle.",
      },
      y = {
        type = "value",
        description = "The y coordinate of the rectangle.",
      },
    },
  },
  vec2 = {
    type = "class",
    childs = {
      x = {
        type = "value",
        description = "The x coordinate of the 2D vector.",
      },
      y = {
        type = "value",
        description = "The y coordinate of the 2D vector.",
      },
    },
  },
  billing = {
    type = "class",
    childs = {
      finishTransaction = {
        type = "method",
        description = "Finishes / finalises a transaction. \nWhen a purchase request is made it is not finalised until this function is called. This gives \nthe developer the opportunity to validate the transaction / download content before notifying \nthe store that the purchase was successfully completed. If the app exits before the purchase has \nbeen finished, the system will inform the app of the purchase again in the future. Not required \nfor the BlackBerry platform.",
        args = "()",
        returns = "(boolean)",
      },
      init = {
        type = "method",
        description = "Initialises the billing system.",
        args = "()",
        returns = "(boolean)",
      },
      isAvailable = {
        type = "method",
        description = "Checks availability of in-app purchasing.",
        args = "()",
        returns = "(boolean)",
      },
      purchaseProduct = {
        type = "method",
        description = "Purchases a product.\nUpon successfull purchase the receiptAvailable event will be raised providing access to product \ninformation. The receiptAvailable event table is defined as follows:\n- productId: Product identifier\n- transactionId: Transaction identifier\n- date: Date of purchase\n- receipt: Transaction receipt\n- finaliseData: Data used to finalise the transaction\n- restored: true if item was restored, false if item was purchased\nIf an error occurs then the billingError event will be raised providing information about the \nerror that occurred. The billingError event table is defined as follows:\n- productId: Product identifier\n- error: The error that occurred\nExample::\nfunction billingEvent(event)\nif (event.type == \"billingError\") then\n-- An error occurred\nelseif (event.type == \"receiptAvailable\") then\n-- Receipt is available\nbilling:finishTransaction(event.finaliseData)\nend\nend\nif (billing:isAvailable()) then\nif (billing:init()) then\nsystem:addEventListener(\"billing\", billingEvent)\nbilling:purchaseProduct(\"my product id\")\nend\nend",
        args = "(productId: string)",
        returns = "(boolean)",
      },
      queryProduct = {
        type = "method",
        description = "Queries product information.\nPlease note the following platform restrictions:\n- Android: Product query is not available\n- BlackBerry: Only product price details are available\nWhen the product information becomes available the infoAvailable event will be raised providing \ninformation about the product. The infoAvailable event table is defined as follows:\n- productId: Product identifier\n- title: The title of the product\n- description: The localised description of the product\n- price: The localised price of the product\nIf an error occurs then the billingError event will be raised providing information about the \nerror that occurred. The billingError event table is defined as follows:\n- productId: Product identifier\n- error: The error that occurred\nExample::\nfunction billingEvent(event)\nif (event.type == \"billingError\") then\n-- An error occurred\nelseif (event.type == \"infoAvailable\") then\n-- Product info is available\nend\nend\nif (billing:isAvailable()) then\nif (billing:init()) then\nsystem:addEventListener(\"billing\", billingEvent)\nbilling:queryProduct(\"my product id\")\nend\nend",
        args = "(productId: string)",
        returns = "(boolean)",
      },
      restoreTransactions = {
        type = "method",
        description = "Restores previous purchased products. Supported only on the iOS platform.\nFor each product that is restored the receiptAvailable event will be raised providing access to \nproduct information. The receiptAvailable event table is defined as follows:\n- productId: Product identifier\n- transactionId: Transaction identifier\n- date: Date of purchase\n- receip: Transaction receipt\n- finaliseData:- Data used to finalise the transaction\n- restored: true if item was restored, false if item was purchased\nIf an error occurs then the billingError event will be raised providing information about the \nerror that occurred. The billingError event table is defined as follows:\n- productId: Product identifier\n- error: The error that occurred",
        args = "()",
        returns = "(boolean)",
      },
      setTestMode = {
        type = "method",
        description = "Specifies live or test mode (BlackBerry only). \nTest mode will allow the return of test responses, whilst live mode will carry out valid in-app \npurchases.\nErrors\n======\nBilling errors descriptions:\n- BILLING_ERROR_CLIENT_INVALID			- The client is invalid\n- BILLING_ERROR_PAYMENT_CANCELLED		- Payment was cancelled\n- BILLING_ERROR_PAYMENT_INVALID		- Payment request is invalid\n- BILLING_ERROR_PAYMENT_NOT_ALLOWED	- Payment is prohibited by the device\n- BILLING_ERROR_PURCHASE_UNKNOWN		- Purchase failed for unknown reason\n- BILLING_ERROR_PURCHASE_DISABLED		- Purchasing is disabled\n- BILLING_ERROR_NO_CONNECTION			- No connection to store available\n- BILLING_ERROR_RESTORE_FAILED			- Product restore failed\n- BILLING_ERROR_UNKNOWN_PRODUCT		- Product was not found in the store\n- BILLING_ERROR_DEVELOPER_ERROR		- The application making the request is not properly signed\n- BILLING_ERROR_UNAVAILABLE			- The billing extension is not available\n- BILLING_ERROR_FAILED					- General failure\n- BILLING_ERROR_UNKNOWN_ERROR			- An unknown error has occurred\nProduct Refunds\n===============\nThe billing API does not directly force a product refund, however should one occur the refundAvailable \nevent will be raised. The refundAvailable event table is defined as follows:\n- productId: Product identifier\n- finaliseData: Data used to finalise the transaction\nNote that finishTransaction() should be called to inform the store that the refund was completed.\nRefunds are only supported by the Android (Google Play) platform.\nAndroid Public Key\n==================\nFor the Android platform you must set your public key in the app.icf file as shown below::\n[BILLING]\nandroidPublicKey1=\"Part of Android public key\"\nandroidPublicKey2=\"Part of Android public key\"\nandroidPublicKey3=\"Part of Android public key\"\nandroidPublicKey4=\"Part of Android public key\"\nandroidPublicKey5=\"Part of Android public key\"\nNote that the key is split across up to 5 settings, each setting can carry a max of 127 characters. \nThe complete key will be a concatenation of all 5 settings.",
        args = "(productId: boolean)",
        returns = "()",
      },
      terminate = {
        type = "method",
        description = "Terminates the billing system.",
        args = "()",
        returns = "()",
      },
    },
  },
  browser = {
    type = "class",
    childs = {
      isAvailable = {
        type = "method",
        description = "Checks if the device supports the browser functionality.",
        args = "()",
        returns = "(boolean)",
      },
      launchURL = {
        type = "method",
        description = "Perform a system call to open the system predefined browser to the specified URL.\nExample::\nbrowser:launchURL(\"http://www.madewithmarmalade.com\")",
        args = "(url: string, exit: boolean)",
        returns = "(boolean)",
      },
    },
  },
  circle = {
    type = "class",
    childs = {
      radius = {
        type = "value",
        description = "The radius of the circle.",
      },
    },
    inherits = "vector",
  },
  color = {
    type = "class",
    childs = {
      a = {
        type = "value",
        description = "The alpha component of the color (0-255).",
      },
      b = {
        type = "value",
        description = "The blue component of the color (0-255).",
      },
      g = {
        type = "value",
        description = "The green component of the color (0-255).",
      },
      r = {
        type = "value",
        description = "The red component of the color (0-255).",
      },
    },
  },
  compass = {
    type = "class",
    childs = {
      getHeadingDegrees = {
        type = "method",
        description = "Get the current compass heading, in degrees (0..359).\nThe value is relative to the top of device in the current OS orientation. \nNorth, East, South and West are represented by 0, 90, 180 and 270 respectively.",
        args = "()",
        returns = "(number)",
      },
      getHeadingVector = {
        type = "method",
        description = "Gets the current compass 3d vector and heading.\nIf this function fails, the result returned is simply a boolean with value \"false\".\nIf this function succeeds, the result returned is a quartet (heading,x,y,z) of number values, followed by a boolean with value \"true\".\n:returns heading (number): The current heading of the compass vector, in degrees east of north, relative to the current orientation of the device. This is normally calculated by the OS based the x,y,z components below.\n:returns x (number): Component of earth's magnetic field vector measured along the device's x axis, in microteslas.\n:returns y (number): Component of earth's magnetic field vector measured along the device's y axis, in microteslas.\n:returns z (number): Component of earth's magnetic field vector measured along the device's z axis, in microteslas.",
        args = "()",
        returns = "()",
      },
      hasStarted = {
        type = "method",
        description = "",
        args = "()",
        returns = "(boolean)",
      },
      isSupported = {
        type = "method",
        description = "",
        args = "()",
        returns = "(boolean)",
      },
      start = {
        type = "method",
        description = "Starts the compass service. Because the compass sensor consumes battery, it is required to be explictly started using\nthis function.",
        args = "()",
        returns = "(boolean)",
      },
      stop = {
        type = "method",
        description = "Stops the compass service.",
        args = "()",
        returns = "()",
      },
    },
  },
  crypto = {
    type = "class",
    childs = {
      base64Decode = {
        type = "method",
        description = "Performs base64 decoding.",
        args = "(data: string)",
        returns = "(string)",
      },
      base64Encode = {
        type = "method",
        description = "Performs base64 encoding.",
        args = "(data: string)",
        returns = "(string)",
      },
      digestSha1 = {
        type = "method",
        description = "Compute the message digest and return a base64-encoded string.",
        args = "(msg: string)",
        returns = "(string)",
      },
      getSupportedAlgorithmNames = {
        type = "method",
        description = "Gets a list of supported digest algorithm names.",
        args = "()",
        returns = "(table)",
      },
    },
  },
  dbg = {
    type = "class",
    childs = {
      assert = {
        type = "value",
        description = "Evaluates a condition, and terminates processing only if it evaluates to false.\nThe condition argument is followed by further arguments which are evaluated and printed only on a false result.\n:param ... (string): Arbitrary number of optional strings to print.\nExample::\ndbg.assert(type(myVariable) == \"number\", \"Variable is of unsupported type: \", type(myVariable))",
      },
      print = {
        type = "value",
        description = "Prints an arbitrary number of strings to the console output.\n:param ... (string): Arbitrary number of optional strings to print.\nExample::\n-- Assume within some loop with index i\ndbg.print(\"The value of index: \", i, \" is OK\")",
      },
      printTable = {
        type = "value",
        description = "Prints a table to the console log, using spaces to indent nested tables.\nUsefully, circular references are ignored, so any referenced sub-table is only printed once.\nExample::\nlocal testTable = { x=1, y=2, color={r=100, g=200, b=100}, childTable=testTable }\ndbg.printTable(testTable)",
      },
    },
  },
  table = {
    type = "class",
    childs = {
      hasIndex = {
        type = "value",
        description = "Check if a table has a specified *key*. Any key type is acceptable, not just a numerical index.\nExample::\nlocal t = { x=0, y=1 }\nlocal r = table.hasIndex(t, \"x\")\ndbg.print(\"Table has key? \", r)",
      },
      hasValue = {
        type = "value",
        description = "Check if any index of the table has a specified value.\nExample::\nlocal t = { x=\"foo\", y=\"bar\" }\nlocal r = table.hasValue(t, \"bar\")\ndbg.print(\"Table has value? \", r)",
      },
      setValuesFromTable = {
        type = "value",
        description = "Set (key, value) pairs on table A, from a series of (key, value) \"setter\" pairs on table B.\nExample::\nlocal t = { x=\"foo\", y=\"bar\" }\nlocal setters = { x=\"bar\", y=\"foo\", z=\"newvalue\" }\ntable.setValuesFromTable(t, setters)\ndbg.printTable(t)",
      },
    },
  },
  device = {
    type = "class",
    childs = {
      disableVibration = {
        type = "method",
        description = "Disables the vibration.",
        args = "()",
        returns = "(boolean)",
      },
      enableVibration = {
        type = "method",
        description = "Enables the vibration.",
        args = "()",
        returns = "(boolean)",
      },
      getInfo = {
        type = "method",
        description = "Get the specified device state. Valid inputs are:\n* name (string) - The name given to the current device by the user, if the OS supports such a concept. For example: \"John's Smartphone\".\n* | deviceID (string) - A platform-specific device type ID as a string.\n| This can be used as a way to identify the exact handset model in use.\n* platform (string) - The device's operating system. Supported values are: IPHONE, ANDROID, QNX, WINDOWS, OSX.\n* platformVersion (string) - Version information about the underlying OS. The full OS name and version is reported. e.g. \"Windows XP SP2\".\n* architecture (string) - The CPU architecture of the devic, for example \"ARM7\".\n* memoryTotal (number) - The physical RAM installed on the device, in KB.\n* memoryFree (number) - The free physical RAM available to the operating system, in KB. Note that this is NOT the same as the memory available to the app.\n* | language (string) - The current language in use on the device, for example \"ENGLISH\".\n| The full list of valid language strings can be found by inspecting the device.languages table, e.g. using dbg.printTable(device.languages).\n* | locale (string) - The current device locale as a language-country code pair using the ISO 639 and ISO 3166 formats respectively.\n| For example, if the device is set to English (UK) it will return \"en_GB\". If the device does not support providing a locale, it will return the empty string.\n* timezone (string) - The device's current timezone as three or more alphabetical characters. e.g. GMT, PST.\n* phoneNumber (string) - The device's phone number as a string. On devices without a valid phone number, this will return the empty string.\n* batteryLevel (number) - The battery level as a percentage (0-100). If this functionality is not supported, the value 100 is returned.\n* | mainsPower (boolean) - True if device is connected to a power cable, otherwise false (if it is running on battery power).\n| If this functionality is not supported, it returns false.\n* chipset (string) - Some identifier for the device chipset.\n* | fpu (string) - The hardware floating-point type on the chipset.\n| Possible values are \"NONE\", \"VFP\", \"VFPV3\" or \"NEON\".\n* | imsi (string) - The IMSI number of the SIM card currently inserted in the device.\n| This is a string of 15 digits which uniquely identifies the SIM card as well as the country and carrier from which is was obtained.\n| Platforms which do not support this feature (such as iOS) will return the empty string (\"\").\n| Some platforms may return a partial IMSI.\n* silentMode (boolean) - True only if the device is in a silent mode or profile whereby all sound output is muted.\n* numCPUCores (number) - The number of CPU cores on the device.",
        args = "(info: type)",
        returns = "(various)",
      },
      getVibrationThreshold = {
        type = "method",
        description = "Gets the vibration threshold.\nThe threshold is the level above which vibrations will be acted upon. Calls to vibrate below this level will be ignored.",
        args = "()",
        returns = "(number)",
      },
      isVibrationAvailable = {
        type = "method",
        description = "Gets the availability of the vibration functionality on the device.",
        args = "()",
        returns = "(boolean)",
      },
      isVibrationEnabled = {
        type = "method",
        description = "",
        args = "()",
        returns = "(boolean)",
      },
      setBacklightAlways = {
        type = "method",
        description = "Provides some control over the backlight. The OS usually likes to dim the backlight after a \ncertain amount of inactivity, but by passing true into this function you\ncan force the backlight to stay on.",
        args = "(enable: boolean)",
        returns = "()",
      },
      setVibrationThreshold = {
        type = "method",
        description = "Sets the vibration threshold.\nThe threshold is the level above which vibrations will be acted upon. Calls to vibrate below this level will be ignored.\n:param (number): The vibration threshold (in the range 0..255).",
        args = "()",
        returns = "(boolean)",
      },
      stopVibrate = {
        type = "method",
        description = "Stops any existing vibration.",
        args = "()",
        returns = "()",
      },
      vibrate = {
        type = "method",
        description = "Start the device vibration.",
        args = "(level: number, ms: number)",
        returns = "(boolean)",
      },
    },
  },
  director = {
    type = "class",
    childs = {
      addNodesToScene = {
        type = "value",
        description = "Whether or not the Director should automatically add newly-created display objects to the current scene.\nThe default value is true.",
      },
      addScene = {
        type = "method",
        description = "Add a Scene object to the Director. The added Scene object will become the current scene.\nTypically this function is not called directly: instead, the app calls director:createScene() which both creates the new Scene\nobject and adds it to the Director.",
        args = "(scene: object)",
        returns = "()",
      },
      cleanupTextures = {
        type = "method",
        description = "Frees the OpenGL ES memory associated with any unused texture objects.\nIf you are running out of memory because of texture resources, you should:\n- Ensure you've cleared references to all associated Atlas objects, e.g. with myAtlas=nil\n- Force a garbage collection with collectgarbage(\"collect\")\n- Call director:cleanupTextures()\n----------\n**Properties**.",
        args = "()",
        returns = "()",
      },
      createAnimation = {
        type = "method",
        description = "Create an animation, specifying arbitrary input values.\nSee the Objects reference for details of all properties and functions on the Animation object.",
        valuetype = "animation",
        args = "(values: table)",
        returns = "(animation)",
      },
      createAtlas = {
        type = "method",
        description = "Create a texture atlas. Two different input types are permitted:\n| ``director:createAtlas(filename)``\n| ``director:createAtlas(values)``\nSee the Objects reference for details of all properties and functions on the Atlas object.",
        valuetype = "atlas",
        args = "(filename: string, values: table)",
        returns = "(atlas)",
      },
      createBBox = {
        type = "method",
        description = "Create a bounding box object, from x and y bounds. The object simply copies these values into the properties\nxMin, xMax, yMin, yMax respectively.\n----------\n**Scene functions**.",
        valuetype = "box",
        args = "(xMin: number, xMax: number, yMin: number, yMax: number)",
        returns = "(box)",
      },
      createCircle = {
        type = "method",
        description = "Create a Circle node. Two different input types are permitted:\n| ``director:createCircle(x, y, radius)``\n| ``director:createCircle(values)``\nSee the Objects reference for details of all properties and functions on the Circle object.",
        valuetype = "circle",
        args = "(x: number, y: number, radius: number, values: table)",
        returns = "(circle)",
      },
      createFont = {
        type = "method",
        description = "Create a Font object. Two different input types are permitted:\n| ``director:createFont(filename)``\n| ``director:createFont(values)``\nSee the Objects reference for details of all properties and functions on the Font object.",
        valuetype = "font",
        args = "(filename: string, values: table)",
        returns = "(font)",
      },
      createLabel = {
        type = "method",
        description = "Create a Label node. Two different input types are permitted:\n| ``director:createLabel(x, y, text, font)``\n| ``director:createLabel(values)``\nSee the Objects reference for details of all properties and functions on the Label object.",
        valuetype = "label",
        args = "(x: number, y: number, text: string, font: object or string, values: table)",
        returns = "(label)",
      },
      createLines = {
        type = "method",
        description = "Create a Lines node. Two different input types are permitted:\n| ``director:createLines(x, y, coords)``\n| ``director:createLines(values)``\nSee the Objects reference for details of all properties and functions on the Lines object.",
        valuetype = "lines",
        args = "(x: number, y: number, coords: table, values: table)",
        returns = "(lines)",
      },
      createNode = {
        type = "method",
        description = "Create a Node object, specifying a table of arbitrary input values.\nSee the Objects reference for details of all properties and functions on the Node object.",
        valuetype = "node",
        args = "(values: table)",
        returns = "(node)",
      },
      createParticles = {
        type = "method",
        description = "Create a Particles node. Three different input types are permitted:\n| ``director:createParticles(plist)``\n| ``director:createParticles(numParticles)``\n| ``director:createParticles(values)``\nThis file is output from the ParticleDesigner tool, or similar.\nThe app should then set up all other member variables explicitly.\nwritable properties of the Particles object. Additionally, the user \ncan specify the 'source' property, which can be a full pathname to a \ntexture, or an existing Atlas object. For example::\n-- Specify texture from filename\nlocal p1 = director:createParticles( { totalParticles=500, source=\"particles/fire.png\" } )\n-- Specify texture from Atlas object\nlocal atlas = director:createAtlas(\"textures/beachball.png\")\nlocal p2 = director:createParticles( { totalParticles=500, source=atlas } )\nSee the Objects reference for details of all properties and functions on the Particles object.",
        valuetype = "particles",
        args = "(plist: string, numParticles: number, values: table)",
        returns = "(particles)",
      },
      createRectangle = {
        type = "method",
        description = "Create a Rectangle node. Two different input types are permitted:\n| ``director:createRectangle(x, y, w, h)``\n| ``director:createRectangle(values)``\nSee the Objects reference for details of all properties and functions on the Rectangle object.",
        valuetype = "rectangle",
        args = "(x: number, y: number, w: number, h: number, values: table)",
        returns = "(rectangle)",
      },
      createScene = {
        type = "method",
        description = "Create a scene node, and set it to be the director's current scene.\nNote that no transition occurs from any previous scene, and no scene events are thrown.",
        valuetype = "scene",
        args = "()",
        returns = "(scene)",
      },
      createSprite = {
        type = "method",
        description = "Create a Sprite node. Two different input types are permitted:\n| ``director:createSprite(x, y, source)``\n| ``director:createSprite(values)``\nSee the Objects reference for details of all properties and functions on the Sprite object.",
        valuetype = "sprite",
        args = "(x: number, y: number, source: number or object, values: table)",
        returns = "(sprite)",
      },
      displayCenterX = {
        type = "value",
        description = "A convenience value, this is simply displayWidth / 2.",
      },
      displayCenterY = {
        type = "value",
        description = "A convenience value, this is simply displayHeight / 2.",
      },
      displayHeight = {
        type = "value",
        description = "The height of the display, in pixels, as far as the app is concerned.\nThis is usually the physical height of the screen resolution if running in portrait orientation, or the physical\nwidth of the screen resolution if running in landscape orientation.\nIf the Director is subject to any display scaling, then this may not be the case.",
      },
      displayWidth = {
        type = "value",
        description = "The width of the display, in pixels, as far as the app is concerned.\nThis is usually the physical width of the screen resolution if running in portrait orientation, or the physical\nheight of the screen resolution if running in landscape orientation.\nIf the Director is subject to any display scaling, then this may not be the case.",
      },
      getCurrentScene = {
        type = "method",
        description = "Get the current Scene object, which may be the Director's global scene.",
        args = "()",
        returns = "(object)",
      },
      isAlphaInherited = {
        type = "value",
        description = "Whether or not node alpha (and strokeAlpha) should be inherited (propogated) through the scene graph.\nThe default value is true.",
      },
      moveToScene = {
        type = "method",
        description = "Move to a new scene. The new scene receives the following events:\n- setUp - Called immediately, only if the new scene is not already set up.\n- enterPreTransition - Called BEFORE any transition, immediately after the check for sending of the setUp event.\n- enterPostTransition - Called AFTER any transition has completed.\nThe old scene receives the following events:\n- exitPreTransition - Called BEFORE any transition, immediately after the enterPreTransition event is sent to the new scene.\n- tearDown - Called AFTER any transition has completed, if the scene is currently set up.\n- exitPostTransition - Called AFTER any transition has completed, immediately after the tearDown event.\nThe options table can take the following properties:\n- transitionType (string) - Valid values are:\n- \"rotoZoom\"\n- \"jumpZoom\"\n- \"moveInR\"\n- \"moveInT\"\n- \"moveInB\"\n- \"slideInL\"\n- \"slideInR\"\n- \"slideInB\"\n- \"slideInT\"\n- \"shrinkGrow\"\n- \"flipX\"\n- \"flipY\"\n- \"flipAngular\"\n- \"zoomFlipX\"\n- \"zoomFlipY\"\n- \"zoomFlipAngular\"\n- \"crossFade\"\n- \"turnOffTiles\"\n- \"splitCols\"\n- \"splitRows\"\n- \"fadeTR\"\n- \"fadeBL\"\n- \"fadeUp\"\n- \"fadeDown\"\n- \"progressRadialCCW\"\n- \"progressRadialCW\"\n- \"progressHorizontal\"\n- \"progressVertical\"\n- \"progressInOut\"\n- \"progressOutIn\"\n- \"fade\"\n- \"pageTurn\"\nYou'll have to experiment with them to find out what they really do!        \n- transitionTime (number) - The duration of the transition, in seconds.\n----------\n**Node functions**.",
        args = "(newScene: object [, options: table])",
        returns = "()",
      },
      removeNode = {
        type = "method",
        description = "Remove a node from its scene, by setting its parent to nil. This does not destroy the node. This function\nis equivalent to calling n:removeFromParent().",
        args = "(n: object)",
        returns = "()",
      },
      setCurrentScene = {
        type = "method",
        description = "Set the current Scene object.\nBy default, the Director is set to add newly-created objects to the current scene.\nSetting the current Scene object to nil will tell the Director to revert to its global Scene.",
        args = "(scene: object)",
        returns = "()",
      },
      setNodesColor = {
        type = "method",
        description = "Set the default color for all newly created nodes.\n----------\n**Global functions**.",
        args = "(r: number, g: number, b: number)",
        returns = "()",
      },
    },
  },
  facebook = {
    type = "class",
    childs = {
      isAvailable = {
        type = "method",
        description = "Checks availability of the Facebook API.",
        args = "()",
        returns = "(boolean)",
      },
      login = {
        type = "method",
        description = "Log in to Facebook.\nDisplays the Facebook login dialog.  If the user has already put in\ntheir Facebook details then this dialog may not be displayed.",
        args = "()",
        returns = "()",
      },
      logout = {
        type = "method",
        description = "Log out of Facebook.",
        args = "()",
        returns = "()",
      },
      postUpdate = {
        type = "method",
        description = "Posts a status update on Facebook.",
        args = "(update: string)",
        returns = "(boolean)",
      },
      request = {
        type = "method",
        description = "Sends a Facebook graph request.\n:param graph (string) The graph request to send\n:param params (table) A paired list of parameters to send with the graph request.\nSends a Facebook method request.\n:param method (string) The method request to send\n:param type (string) The type parameter to use send\n:param params (table) A paired list of parameters to send with the method request.",
        args = "()",
        returns = "(boolean, boolean)",
      },
      showDialog = {
        type = "method",
        description = "Opens a Facebook dialog.\n:param action (string) The dialog action to take.\n:param params (table) A paired list of parameters to set into the dialog.",
        args = "()",
        returns = "(boolean)",
      },
    },
  },
  font = {
    type = "class",
    childs = {
      destroy = {
        type = "method",
        description = "Destroy the Font object.",
        args = "()",
        returns = "()",
      },
      filename = {
        type = "value",
        description = "The full pathname that the font was loaded from.",
      },
      height = {
        type = "value",
        description = "The height, in pixels, of the font.",
      },
      initFromFntFile = {
        type = "method",
        description = "Creates a Font object from a font resource description held in a .fnt file.\nThis function parses the contents of the .fnt file and stores the characters and kerning\ndefined within. It also loads the associated texture (bitmap) file for the font.",
        args = "(filename: string)",
        returns = "(boolean)",
      },
    },
  },
  json = {
    type = "class",
    childs = {
      decode = {
        type = "method",
        description = "Decodes a JSON string and returns the decoded value as a Lua table or other variable type.",
        args = "(s: string [, startPos: number])",
        returns = "(any)",
      },
      encode = {
        type = "method",
        description = "Encodes a Lua table, or other variable type, as a JSON string.",
        args = "(t: any)",
        returns = "(string)",
      },
      null = {
        type = "method",
        description = "The null function allows one to specify a null value in an associative array (which is otherwise\ndiscarded if you set the value with 'nil' in Lua).",
        valuetype = "null",
        args = "()",
        returns = "(null)",
      },
    },
  },
  label = {
    type = "class",
    childs = {
      font = {
        type = "value",
        description = "The font the label uses to render its text.",
      },
      hAlignment = {
        type = "value",
        description = "The horizontal alignment of the label text, within its text box.\nPossible options are:\n- 'left'\n- 'center' or 'centre'\n- 'right'",
      },
      hText = {
        type = "value",
        description = "The height of the minimal bounding box around the rendered text.",
      },
      text = {
        type = "value",
        description = "The text to be displayed.",
      },
      textTouchableBorder = {
        type = "value",
        description = "If this value is 0, then the touchable area of the Label is the \"text box\" area, defined by x,y,w,h.\nOtherwise, the touchable area of the Label is the minimal bounding box around the rendered text itself,\nplus a border of this width in pixels. The default value is 4.",
      },
      vAlignment = {
        type = "value",
        description = "The vertical alignment of the label text, within its text box.\nPossible options are:\n- 'top'\n- 'middle'\n- 'bottom'",
      },
      wText = {
        type = "value",
        description = "The width of the minimal bounding box around the rendered text.",
      },
      xText = {
        type = "value",
        description = "The x coordinate (left) of the minimal bounding box around the rendered text.",
      },
      yText = {
        type = "value",
        description = "The y coordinate (bottom) of the minimal bounding box around the rendered text.",
      },
    },
    inherits = "node",
  },
  lines = {
    type = "class",
    childs = {
      append = {
        type = "method",
        description = "Append an array of points to the lines object.\nThe array is assumed to be (x,y) pairs, so must have an even number of entries.\nExample::\nlocal l = director:createLines(0, 0, {0,0,  100,0})\nl:append( { 100,100,  0,100} )",
        args = "(coords: table)",
        returns = "()",
      },
    },
    inherits = "vector",
  },
  location = {
    type = "class",
    childs = {
      getLocationType = {
        type = "method",
        description = "Gets a description of the type of the started location service, or an error on failure.\nValid responses are:\n- \"No Location service found\"\n- \"Connected to GPS device\"\n- \"Connected to Wi-fi location service\"\n- \"Connected to Cell Tower location service\"\n- \"Connected to combination of GPS and Wi fi\"\n- \"Connected to an unknown location device\"",
        args = "()",
        returns = "(string)",
      },
      hasStarted = {
        type = "method",
        description = "",
        args = "()",
        returns = "(boolean)",
      },
      isReadingAvailable = {
        type = "method",
        description = "",
        args = "()",
        returns = "(boolean)",
      },
      start = {
        type = "method",
        description = "Starts the location service. Because the location sensor consumes battery, it is required to be explictly started using\nthis function.",
        args = "()",
        returns = "(boolean)",
      },
      stop = {
        type = "method",
        description = "Stops the location service.",
        args = "()",
        returns = "()",
      },
    },
  },
  node = {
    type = "class",
    childs = {
      addChild = {
        type = "method",
        description = "Add the specified node to this node as a child.\nIf the specified node already has a parent, it is cleanly detached from its parent first,\nbefore being added as a child to this node.",
        args = "(nc: object)",
        returns = "()",
      },
      addEventListener = {
        type = "method",
        description = "Add an event listener to the node.\nExample::\n-- Example of a function listener\nlocal myNode = director:createSprite(0, 0, \"textures/beachball.png\")\nlocal touchFunction = function(event)\n{\n-- Do something on every touch event\n}\nmyNode:addEventListener(\"touch\", touchFunction)\n-- Example of a table listener\nlocal myNode = director:createSprite(0, 0, \"textures/beachball.png\")\nfunction myNode:touch(event)\n{\n-- Do something on every touch event\n}\nmyNode:addEventListener(\"touch\", myNode)",
        args = "(name: string or table, funcortable: function or table)",
        returns = "()",
      },
      addTimer = {
        type = "method",
        description = "Add a timed event to this node.",
        valuetype = "timer",
        args = "(funcortable: function or table, period: number, iterations: number, delay: number)",
        returns = "(timer)",
      },
      alpha = {
        type = "value",
        description = "The alpha value, or opacity, of the node, in the range (0..1).\nFor example, 1 is fully opaque, whilst 0 is fully transparent.\nThe default value is 1.\nThis value is multiplied by the \"a\" component of node.color, in order to calculate the actual rendered\nalpha value of the node.",
      },
      children = {
        type = "value",
        description = "The list (table) of children attached to this node. The table can be queried for its length, and can be iterated over, but must\nnot be manipulated in any other way. Attempting to insert or remove elements from the table will result in undefined and almost\ncertainly undesirable behaviour.\nExample of usage::\nfor i,v in ipairs(node.children) do\ndbg.print(\"Child name: \" .. (v or \"<none>\"))\nend",
      },
      clipH = {
        type = "value",
        description = "Specifies the height (in display coordinates) of a clipping rectangle outside of which the node `and all of its children` will not be displayed.\nThe clipping region is only active if clipW and clipH differ from their default 0 values.",
      },
      clipW = {
        type = "value",
        description = "Specifies the width (in display coordinates) of a clipping rectangle outside of which the node `and all of its children` will not be displayed.\nThe clipping region is only active if clipW and clipH differ from their default 0 values.",
      },
      clipX = {
        type = "value",
        description = "Specifies the x value (in display coordinates) of a clipping rectangle outside of which the node `and all of its children` will not be displayed.\nThe clipping region is only active if clipW and clipH differ from their default 0 values.",
      },
      clipY = {
        type = "value",
        description = "Specifies the y value (in display coordinates) of a clipping rectangle outside of which the node `and all of its children` will not be displayed.\nThe clipping region is only active if clipW and clipH differ from their default 0 values.",
      },
      color = {
        type = "value",
        description = "The color of the node. Each component is in the range (0..255).\nFor example, color.r = color.g = color.b = 255 is white, whilst color.r = 255, color.g = color.b = 0 is red.\nThe default value is white.\nExample::\nnode.color = {255, 0, 0} -- set explicitly red\nnode.color = color.black -- set to black using pre-defined color constant",
      },
      debugDraw = {
        type = "value",
        description = "True only if we wish to display the bounding rectangle as a box, for debugging purposes.",
      },
      debugDrawColor = {
        type = "value",
        description = "If debugDraw = true, this sets the color to draw with.",
      },
      getNumChildren = {
        type = "method",
        description = "Get the number of children of this node.",
        args = "()",
        returns = "(number)",
      },
      getParent = {
        type = "method",
        description = "Get this node's parent.",
        args = "()",
        returns = "(object)",
      },
      getPointInLocalSpace = {
        type = "method",
        description = "Given a point in the world (scene) space, return a point in the node's local space.\nNote that for this function to behave as expected, the node's local transform must\nbe up-to-date. For example, if trying to call this function directly after creating a node,\nyou should call Node:sync() to update the local transform first.\n:return: Returns an x,y pair for the point in local (node) space.",
        args = "(x: number, y: number)",
        returns = "()",
      },
      getPointInWorldSpace = {
        type = "method",
        description = "Given a point in the node's local space, return a point in the world (scene) space.\nNote that for this function to behave as expected, the node's local transform must\nbe up-to-date. For example, if trying to call this function directly after creating a node,\nyou should call Node:sync() to update the local transform first.\n:return: Returns an x,y pair for the point in world (scene) space.",
        args = "(x: number, y: number)",
        returns = "()",
      },
      getTimersTimeScale = {
        type = "method",
        description = "Get the time scaling factor currently applied to all timers on this node.",
        args = "()",
        returns = "(number)",
      },
      getTweensTimeScale = {
        type = "method",
        description = "Get the time scaling factor currently applied to all tweens on this node.",
        args = "()",
        returns = "(number)",
      },
      h = {
        type = "value",
        description = "The height of the node. This value is only relevant to certain object types:\n- For Label objects, it refers to the height of the text box being used for text alignment.\n- For Rectangle objects, it refers to the height of the rectangle.\n- For Sprite objects, it refers to the height of the texture or texture region being used by the Sprite, and should be considered readonly.\n- For other types of display object, it is generally ignored. \nFor details of how touch areas are defined, see the API documentation for each specific display object type.",
      },
      isChild = {
        type = "method",
        description = "Determine whether the specified node is a child of this node.",
        args = "(nc: object)",
        returns = "(boolean)",
      },
      isPointInside = {
        type = "method",
        description = "Check if a point, in display coordinates, is inside the bounding region of this node.\nThe node's global transform is taken into account, as well as its shape (rectangular, circular, etc.)",
        args = "(x: number, y: number)",
        returns = "(boolean)",
      },
      isTouchable = {
        type = "value",
        description = "True if the node is to receive touch events.\nThe default value is true.\nNote that invisible nodes can still receive touch events, provided this value is true.",
      },
      isVisible = {
        type = "value",
        description = "True if the node is to be displayed, otherwise false.\nThe default value is true.",
      },
      name = {
        type = "value",
        description = "A user-defined string for the Node object. The app can choose not to use this, or to set it to\nsomething representing the node type, or to set it uniquely for the node instance.",
      },
      pauseTimers = {
        type = "method",
        description = "Pause all timers attached to this node.",
        args = "()",
        returns = "()",
      },
      pauseTweens = {
        type = "method",
        description = "Pause all tweens attached to this node.",
        args = "()",
        returns = "()",
      },
      physics = {
        type = "value",
        description = "The physics properties of the node.\nA nil value implies that the node is not added to the physics simulation.\nSee the Physics API reference, and the guide \"Physics Overview\" for more information on physics.",
      },
      removeChild = {
        type = "method",
        description = "Remove the specified child node from this node.\nIf the node is not a child of this node, a failure message is displayed.",
        args = "(nc: object)",
        returns = "()",
      },
      removeEventListener = {
        type = "method",
        description = "Remove an event listener from this node.",
        args = "(name: string or table, funcortable: function or table)",
        returns = "()",
      },
      removeFromParent = {
        type = "method",
        description = "Remove a node from its parent, and therefore from any scene it belongs to.\nIf the node has no parent, a failure message is displayed.",
        args = "()",
        returns = "()",
      },
      resumeTimers = {
        type = "method",
        description = "Resume all timers attached to this node. Note that if any individual timer has been explicitly paused with timer:pause()\nthen it will continue to be paused.",
        args = "()",
        returns = "()",
      },
      resumeTweens = {
        type = "method",
        description = "Resume all tweens attached to this node.",
        args = "()",
        returns = "()",
      },
      rotate = {
        type = "method",
        description = "Rotate the node by a specified angle.\nThis function is equivalent to:\nnode.rotation = node.rotation + angle",
        args = "(angle: number)",
        returns = "()",
      },
      rotation = {
        type = "value",
        description = "The rotation of the node, in degrees.\nPositive values rotate in a CW direction.",
      },
      scale = {
        type = "method",
        description = "Scale a node by a specified amount, specifying the x and y scales to multiply by.\nThis function is equivalent to:\nnode.xScale = node.xScale * sx; node.yScale = node.yScale * sy\nIf sy is not specified, then the object is scaled by sx along both axes.",
        args = "(sx: number, sy: number)",
        returns = "()",
      },
      setParent = {
        type = "method",
        description = "Set this node's parent to be the specified node.\nIf this node already has a parent, it is cleanly detached from that node first,\nbefore being added as a child to the specified node.",
        args = "(np: object)",
        returns = "()",
      },
      setTimersTimeScale = {
        type = "method",
        description = "Set a time scaling factor to apply when updating all timers for this node.\nFor example a value of 2 would cause all timers to run at double speed.",
        args = "(f: number)",
        returns = "()",
      },
      setTweensTimeScale = {
        type = "method",
        description = "Set a time scaling factor to apply when updating all tweens for this node.\nFor example a value of 2 would cause all tweens to run at double speed.",
        args = "(f: number)",
        returns = "()",
      },
      sync = {
        type = "method",
        description = "Synchronises the Cocos2d-x data with the Quick data for this Node. In most scenarios you do\nnot need to explicitly call this function - it is done automatically across the scene, as\npart of the Director's update.",
        args = "()",
        returns = "()",
      },
      timers = {
        type = "value",
        description = "The list (table) of timers attached to this node. The table can be queried for its length, and can be iterated over, but must\nnot be manipulated in any other way. Attempting to insert or remove elements from the table will result in undefined and almost\ncertainly undesirable behaviour.",
      },
      translate = {
        type = "method",
        description = "Translate a node by a specified amount, specifying the distances to translate along\neach axis.\nThis function is equivalent to:\nnode.x = node.x + dx; node.y = node.y + dy",
        args = "(dx: number, dy: number)",
        returns = "()",
      },
      tweens = {
        type = "value",
        description = "The list (table) of tweens attached to this node. The table can be queried for its length, and can be iterated over, but must\nnot be manipulated in any other way. Attempting to insert or remove elements from the table will result in undefined and almost\ncertainly undesirable behaviour.",
      },
      w = {
        type = "value",
        description = "The width of the node. This value is only relevant to certain object types:\n- For Label objects, it refers to the width of the text box being used for text alignment.\n- For Rectangle objects, it refers to the width of the rectangle.\n- For Sprite objects, it refers to the width of the texture or texture region being used by the Sprite, and should be considered readonly.\n- For other types of display object, it is generally ignored.\nFor details of how touch areas are defined, see the API documentation for each specific display object type.",
      },
      x = {
        type = "value",
        description = "The x coordinate of the node, relative to any parent node.\nIf the node's parent is a Scene, then this value can be considered to be in \"display coordinates\" (global coordinates).\nOtherwise, this value can be considered as within the local space of the parent node.\nThe default value is 0.",
      },
      xAnchor = {
        type = "value",
        description = "The x coordinate of the node's anchor point, as a proportion of the node's width.\nA value of 0 refers to the left edge of the node's bounding box, whilst a value of 1 refers to the right edge.\nThe default value is 0.",
      },
      xScale = {
        type = "value",
        description = "The scaling factor of the node, along the x axis.\nThe default value is 1.",
      },
      xSkew = {
        type = "value",
        description = "The x skew angle of the node in degrees.\nThis angle describes the shear distortion in the x direction.\nThus, it is the angle between the y axis and the left edge of the node's bounding box.\nThe default angle is 0. Positive values distort the node in a CW direction.",
      },
      y = {
        type = "value",
        description = "The y coordinate of the node, relative to any parent node.\nIf the node's parent is a Scene, then this value can be considered to be in \"display coordinates\" (global coordinates).\nOtherwise, this value can be considered as within the local space of the parent node.\nThe default value is 0.",
      },
      yAnchor = {
        type = "value",
        description = "The y coordinate of the node's anchor point, as a proportion of the node's height.\nA value of 0 refers to the bottom edge of the node's bounding box, whilst a value of 1 refers to the top edge.\nThe default value is 0.",
      },
      yScale = {
        type = "value",
        description = "The scaling factor of the node, along the y axis.\nThe default value is 1.",
      },
      ySkew = {
        type = "value",
        description = "The y skew angle of the node in degrees.\nThis angle describes the shear distortion in the y direction.\nThus, it is the angle between the y axis and the bottom edge of the node's bounding box.\nThe default angle is 0. Positive values distort the node in a CCW direction.",
      },
      zOrder = {
        type = "value",
        description = "This value controls how nodes are layered relative to one another.\nnodes with higher values appear in front of nodes with lower values.\nSee the guide \"Drawing to the Display\" for more information on layering of objects.",
      },
    },
  },
  particles = {
    type = "class",
    childs = {
      alphaModifiesColor = {
        type = "value",
        description = "True only if the particle alpha value should be multiplied into the particle color components.",
      },
      angle = {
        type = "value",
        description = "The initial rotation of each particle, in degrees. The property 'angleVar' specifies a random \nvariance that will be added to this value. The default value is 0.",
      },
      angleVar = {
        type = "value",
        description = "The random variance, in degrees, to add to the initial rotation of each particle. The rotation of each\nparticle will be a random value in the range [angle - angleVar, angle + angleVar]. The default\nvalue is 0.",
      },
      duration = {
        type = "value",
        description = "The total lifetime of the particle system, in seconds. If this value is 'particles.durationInfinity'\nthen the particle system remains alive forever (or until stopped). The default value is\n'particles.durationInfinity'.",
      },
      emitterMode = {
        type = "value",
        description = "Must be either 'particles.modeGravity' (if the particle system is to operate in 'gravity' mode)\nor 'particles.modeRadial' (if the particle system is to operate in 'radial' mode). The default\nvalue is 'particles.modeGravity'.",
      },
      emitterRate = {
        type = "value",
        description = "The number of new particles to generate each second. If we call this value 'r', then the time\nperiod between the generation of new particles is (1 / r). The default value is 1.",
      },
      endColor = {
        type = "value",
        description = "The final color of each particle. The property 'endColorVar' specifies a random \nvariance that will be added to this value.\nThe input object is a table specifying r, g, b, a values. For example::\nlocal p = director:createParticles(200)\n-- Set particles to end at half brightness\np.endColor = color.grey -- same as {0x80, 0x80, 0x80, 0xff}\n-- Modify the r component\np.endColor.r = 0xff\nThe default value is grey, i.e. {0x80, 0x80, 0x80, 0xff}.",
      },
      endColorVar = {
        type = "value",
        description = "The random variance to add to the final color of each particle. The final color of each\nparticle will be a random value in the range [endColor - endColorVar, endColor + endColorVar].\nThe input object is a table specifying r, g, b, a values. For example::\nlocal p = director:createParticles(200)\n-- Set particles to have variance in half the full range\np.endColorVar = {0x80, 0x80, 0x80, 0x00}\n-- Modify the r component\np.endColorVar.r = 0x40\nThe default value is zero, i.e. {0x00, 0x00, 0x00, 0x00}.",
      },
      endSize = {
        type = "value",
        description = "The final size (radius) of each particle, in pixels. The property 'endSizeVar' specifies a random \nvariance that will be added to this value. The default value is 8.",
      },
      endSizeVar = {
        type = "value",
        description = "The random variance, in pixels, to add to the final size of each particle. The final size of each\nparticle will be a random value in the range [endSize - endSizeVar, endSize + endSizeVar]. The\ndefault value is 0.",
      },
      endSpin = {
        type = "value",
        description = "The final spin (angular velocity) of each particle, in degrees per second.\nThe property 'endSpinVar' specifies a random variance that will be added to this value.\nThe default value is 0.",
      },
      endSpinVar = {
        type = "value",
        description = "The random variance, in degrees, to add to the final spin of each particle. The final spin\nof each particle will be a random value in the range [endSpin - endSpinVar, endSpin + endSpinVar].\nThe default value is 0.",
      },
      isActive = {
        type = "method",
        description = "Returns true only if there are any particles still alive.",
        args = "()",
        returns = "(boolean)",
      },
      isFull = {
        type = "method",
        description = "Returns true only if the number of living particles is equal to the total number of particles\npermitted by the particle system ('totalParticles').",
        args = "()",
        returns = "(boolean)",
      },
      life = {
        type = "value",
        description = "The total lifetime of each particle, in seconds. The property 'lifeVar' specifies a random \nvariance that will be added to this value. The default value is 1.",
      },
      lifeVar = {
        type = "value",
        description = "The random variance, in seconds, to add to the total lifetime of each particle. The lifetime of each\nparticle will be a random value in the range [life - lifeVar, life + lifeVar]. The default value\nis 0.",
      },
      modeGravity = {
        type = "value",
        description = "FOR TYPE 'GRAVITY' ONLY.\nThe gravity vector (force) to apply to all particles. The input object is a table \nspecifying x, y values. For example::\nlocal p = director:createParticles(200)\n-- Set particles to appear at screen center\np.modeGravity.gravity = { 0, -90 }\n-- Modify y value\np.modeGravity.gravity.y = -45\nThe default value is {0, 0}.\n(number)\nFOR TYPE 'GRAVITY' ONLY.\nThe initial speed, in pixels per second, of each particle.\nThe property 'speedVar' specifies a random variance that will be added to this value.\nThe default value is 0.\n(number)\nFOR TYPE 'GRAVITY' ONLY.\nThe random variance to add to the initial speed of each particle. The initial speed\nof each particle will be a random value in the range [speed - speedVar, speed + speedVar].\nThe default value is 0.\n(number)\nFOR TYPE 'GRAVITY' ONLY.\nThe initial tangential acceleration of each particle.\nThe property 'tangentialAccelVar' specifies a random variance that will be added to this value.\nThe default value is 0.\n(number)\nFOR TYPE 'GRAVITY' ONLY.\nThe random variance to add to the initial tangential acceleration of each particle.\nThe initial tangential acceleration of each particle will be a random value in the range\n[tangentialAccel - tangentialAccelVar, tangentialAccel + tangentialAccelVar].\nThe default value is 0.\n(number)\nFOR TYPE 'GRAVITY' ONLY.\nThe initial radial acceleration of each particle.\nThe property 'radialAccelVar' specifies a random variance that will be added to this value.\nThe default value is 0.\n(number)\nFOR TYPE 'GRAVITY' ONLY.\nThe random variance to add to the initial radial acceleration of each particle.\nThe initial radial acceleration of each particle will be a random value in the range\n[radialAccel - radialAccelVar, radialAccel + radialAccelVar].\nThe default value is 0.",
      },
      modeRadial = {
        type = "value",
        description = "FOR TYPE 'RADIAL' ONLY.\nThe initial radius of each particle.\nThe property 'startRadiusVar' specifies a random variance that will be added to this value.\nThe default value is 0.\n(number)\nFOR TYPE 'RADIAL' ONLY.\nThe random variance to add to the initial radius of each particle.\nThe initial radius of each particle will be a random value in the range\n[startRadius - startRadiusVar, startRadius + startRadiusVar].\nThe default value is 0.\n(number)\nFOR TYPE 'RADIAL' ONLY.\nThe final radius of each particle.\nThe property 'endRadiusVar' specifies a random variance that will be added to this value.\nThe default value is 0.\n(number)\nFOR TYPE 'RADIAL' ONLY.\nThe random variance to add to the final radius of each particle.\nThe final radius of each particle will be a random value in the range\n[endRadius - endRadiusVar, endRadius + endRadiusVar].\nThe default value is 0.\n(number)\nFOR TYPE 'RADIAL' ONLY.\nThe initial angular velocity (in degrees per second) of each particle.\nThe property 'rotatePerSecondVar' specifies a random variance that will be added to this value.\nThe default value is 0.\n(number)\nFOR TYPE 'RADIAL' ONLY.\nThe random variance to add to the initial angular velocity (in degrees per second) of each particle.\nThe initial angular velocity of each particle will be a random value in the range\n[rotatePerSecond - rotatePerSecondVar, rotatePerSecond + rotatePerSecondVar].\nThe default value is 0.",
      },
      reset = {
        type = "method",
        description = "Resets the generator. This kills all living particles, but enables the generation of new\nparticles.",
        args = "()",
        returns = "()",
      },
      sourcePos = {
        type = "value",
        description = "The point, within the particle system's local coordinate system, at which new particles are\ngenerated. The property 'sourcePosVar' specifies a random variance that will be added to this\nvalue.\nThe input object is a table specifying x, y values. For example::\nlocal p = director:createParticles(200)\n-- Set particles to appear at screen center\np.sourcePos = { director.displayCenterX, director.displayCenterY }    \n-- Modify x coordinate\np.sourcePos.x = 100\nThe default value is {0, 0}.",
      },
      sourcePosVar = {
        type = "value",
        description = "The random variance to add to the point at which new particles are\ngenerated. The random variance should be considered as a rectangle, centred on 'sourcePos', \nwith side half-lengths specified by this table.\nThe input object is a table specifying x, y values. For example::\nlocal p = director:createParticles(200)\n-- Set particles to appear at screen center\np.sourcePos = { director.displayCenterX, director.displayCenterY }    \n-- Set random variance for start positions: +/- 50 along the local x axis, and +/- 100\n-- along the local y axis.\np.sourcePosVar = { 50, 100 }\n-- Modify the x variance\np.sourcePosVar.x = 75\nThe default value is {0, 0}.",
      },
      startColor = {
        type = "value",
        description = "The intial color of each particle. The property 'startColorVar' specifies a random \nvariance that will be added to this value.\nThe input object is a table specifying r, g, b, a values. For example::\nlocal p = director:createParticles(200)\n-- Set particles to appear at half brightness\np.startColor = color.grey -- same as {0x80, 0x80, 0x80, 0xff}\n-- Modify the r component\np.startColor.r = 0xff\nThe default value is grey, i.e. {0x80, 0x80, 0x80, 0xff}.",
      },
      startColorVar = {
        type = "value",
        description = "The random variance to add to the initial color of each particle. The initial color of each\nparticle will be a random value in the range [startColor - startColorVar, startColor + startColorVar].\nThe input object is a table specifying r, g, b, a values. For example::\nlocal p = director:createParticles(200)\n-- Set particles to have variance in half the full range\np.startColorVar = {0x80, 0x80, 0x80, 0x00}\n-- Modify the r component\np.startColor.r = 0x40\nThe default value is zero, i.e. {0x00, 0x00, 0x00, 0x00}.",
      },
      startSize = {
        type = "value",
        description = "The initial size (radius) of each particle, in pixels. The property 'startSizeVar' specifies a random \nvariance that will be added to this value. The default value is 2.",
      },
      startSizeVar = {
        type = "value",
        description = "The random variance, in pixels, to add to the initial size of each particle. The initial size of each\nparticle will be a random value in the range [startSize - startSizeVar, startSize + startSizeVar].\nThe default value is 0.",
      },
      startSpin = {
        type = "value",
        description = "The initial spin (angular velocity) of each particle, in degrees per second.\nThe property 'startSpinVar' specifies a random variance that will be added to this value.\nThe default value is 0.",
      },
      startSpinVar = {
        type = "value",
        description = "The random variance, in degrees, to add to the initial spin of each particle. The initial spin\nof each particle will be a random value in the range [startSpin - startSpinVar, startSpin + startSpinVar].\nThe default value is 0.",
      },
      stop = {
        type = "method",
        description = "Stops the generator. No new particles will be emitted. Particles already alive will\ncontinue to behave as normal.",
        args = "()",
        returns = "()",
      },
    },
    inherits = "node",
  },
  contact = {
    type = "class",
    childs = {
      getFriction = {
        type = "method",
        description = "Gets the friction value on the contact.",
        args = "()",
        returns = "(number)",
      },
      getRestitution = {
        type = "method",
        description = "Gets the restitution value on the contact.",
        args = "()",
        returns = "(number)",
      },
      isEnabled = {
        type = "method",
        description = "Returns true if the contact is enabled, otherwise false.",
        args = "()",
        returns = "(boolean)",
      },
      isTouching = {
        type = "method",
        description = "Returns true if the contact is touching, otherwise false.",
        args = "()",
        returns = "(boolean)",
      },
      setEnabled = {
        type = "method",
        description = "Sets whether or not the contact is enabled.",
        args = "(b: boolean)",
        returns = "()",
      },
      setFriction = {
        type = "method",
        description = "Sets the friction value on the contact.",
        args = "(f: number)",
        returns = "()",
      },
      setRestitution = {
        type = "method",
        description = "Sets the restitution value on the contact.",
        args = "(r: number)",
        returns = "()",
      },
    },
  },
  nodeprops = {
    type = "class",
    childs = {
      applyAngularImpulse = {
        type = "method",
        description = "Applies an angular impulse to the rigid body.",
        args = "(a: number)",
        returns = "()",
      },
      applyForce = {
        type = "method",
        description = "Apply a force at a world point. If the force is not\napplied at the center of mass, it will generate a torque and\naffect the angular velocity. This wakes up the body.",
        args = "(fx: number, fy: number, px: number, py: number)",
        returns = "()",
      },
      applyLinearImpulse = {
        type = "method",
        description = "Apply an impulse at a point. This immediately modifies the velocity.\nIt also modifies the angular velocity if the point of application\nis not at the center of mass. This wakes up the body.\nThe difference between a 'force' and 'impulse' is as follows: the force acts a little bit each timestep to move the body up, \nand then gravity acts to push it back down again, in a continual up down up down struggle. The impulse, on the other hand, \ndoes all its work before gravity gets a chance to interfere.",
        args = "(ix: number, iy: number, px: number, py: number)",
        returns = "()",
      },
      applyTorque = {
        type = "method",
        description = "Apply a torque. This affects the angular velocity\nwithout affecting the linear velocity of the center of mass.\nThis wakes up the body.",
        args = "(torque: number)",
        returns = "()",
      },
      debugDraw = {
        type = "value",
        description = "True only if we wish to display the body shape, for debugging purposes.",
      },
      debugDrawColor = {
        type = "value",
        description = "If debugDraw = true, this sets the color to draw with.",
      },
      density = {
        type = "value",
        description = "The density of the rigid body.\nDefault value is 10.",
      },
      friction = {
        type = "value",
        description = "The frictional value of the rigid body.\nDefault value is 0.5.",
      },
      getAngularDamping = {
        type = "method",
        description = "Get the angular damping of the body.",
        args = "()",
        returns = "(number)",
      },
      getAngularVelocity = {
        type = "method",
        description = "Gets the current angular velocity.",
        args = "()",
        returns = "(number)",
      },
      getGravityScale = {
        type = "method",
        description = "Get the gravity scaling value of the body.",
        args = "()",
        returns = "(number)",
      },
      getInertia = {
        type = "method",
        description = "Get the rotational inertia of the body about the local origin.",
        args = "()",
        returns = "(number)",
      },
      getLinearDamping = {
        type = "method",
        description = "Get the linear damping of the body.",
        args = "()",
        returns = "(number)",
      },
      getLinearVelocityFromLocalPoint = {
        type = "method",
        description = "Get the world velocity of a local point.\n:return vx, vy (number, number): The linear velocity vector expressed in world coordinates.",
        args = "(lx: number, ly: number)",
        returns = "()",
      },
      getLinearVelocityFromWorldPoint = {
        type = "method",
        description = "Get the world linear velocity of a world point attached to this body.\n:return vx, vy (number, number): The linear velocity vector expressed in world coordinates.",
        args = "(wx: number, wy: number)",
        returns = "()",
      },
      getLocalPoint = {
        type = "method",
        description = "Gets a local point relative to the body's origin given a world point.\n:return wx, wy (number, number): The same point expressed in local coordinates.",
        args = "(lx: number, ly: number)",
        returns = "()",
      },
      getLocalVector = {
        type = "method",
        description = "Gets a local vector given a world vector.\n:return lx, ly (number, number): The same vector expressed in local coordinates.",
        args = "(wx: number, wy: number)",
        returns = "()",
      },
      getMass = {
        type = "method",
        description = "Get the total mass of the body.\n:return (number) The mass, usually in kilograms (kg).",
        args = "()",
        returns = "()",
      },
      getWorldPoint = {
        type = "method",
        description = "Get the world coordinates of a point given the local coordinates.\n:return wx, wy (number, number): The same point expressed in world coordinates.",
        args = "(lx: number, ly: number)",
        returns = "()",
      },
      getWorldVector = {
        type = "method",
        description = "Get the world coordinates of a vector given the local coordinates.\n:return wx, wy (number, number): The same vector expressed in world coordinates.",
        args = "(lx: number, ly: number)",
        returns = "()",
      },
      radius = {
        type = "value",
        description = "The radius of the rigid body.\nIf this is > 0, the body is assumed to be circular, otherwise it is assumed to be rectangular.\n(Note that, if points have been added using the 'shape' property, then the body has a different shape specified by the \npoints themselves).\nDefault value is 0.",
      },
      restitution = {
        type = "value",
        description = "The restitution, or \"bounciness\" of the rigid body.\nDefault value is 0.5.",
      },
      setAngularDamping = {
        type = "method",
        description = "Set the new angular damping of the body.",
        args = "(d: number)",
        returns = "()",
      },
      setAngularVelocity = {
        type = "method",
        description = "Sets a new angular velocity.",
        args = "(omega: number)",
        returns = "()",
      },
      setGravityScale = {
        type = "method",
        description = "Set the new gravity scaling value of the body.",
        args = "(s: number)",
        returns = "()",
      },
      setLinearDamping = {
        type = "method",
        description = "Set the new linear damping of the body.",
        args = "(d: number)",
        returns = "()",
      },
      type = {
        type = "value",
        description = "The type of the body: 'static', 'dynamic', or 'kinematic'.\nDefault value is 'dynamic'.",
      },
    },
  },
  physics = {
    type = "class",
    childs = {
      addNode = {
        type = "method",
        description = "Add a node to the physics simulation, and potentially set physics properties of the node.\nIf the node is already part of the simulation, we simply set the physics properties.\nOtherwise, we add the node to the simulation, and set any specified physics properties.",
        args = "(n: object [, values: table])",
        returns = "()",
      },
      createDistanceJoint = {
        type = "method",
        description = "Creates a distance joint that constrains the two attached bodies to maintain a constant distance defined\nby the two anchor points. Two different input types are permitted:\n| ``physics:createDistanceJoint(nodeA, nodeB, x1, y1, x2, y2, collideConnected)``\n| ``physics:createDistanceJoint(values)``\nSupported properties, beyond those of the default constructor, include:\n- length (number) - The distance to maintain between the joints\n- frequency (number) - The frequency of any oscillation\n- dampingRatio (number) - The damping ratio of any oscillation",
        valuetype = "jointDistance",
        args = "(nodeA: object, nodeB: object [, x1: number] [, y1: number] [, x2: number] [, y2: number] [, collideConnected: boolean], values: table)",
        returns = "(jointDistance)",
      },
      createFrictionJoint = {
        type = "method",
        description = "Creates a friction joint: a special kind of revolute / prismatic joint that resists motion, and provides\n2D translational and angular friction. Two different input types are permitted:\n| ``physics:createFrictionJoint(nodeA, nodeB, collideConnected)``\n| ``physics:createFrictionJoint(values)``",
        valuetype = "jointFriction",
        args = "(nodeA: object, nodeB: object [, collideConnected: boolean], values: table)",
        returns = "(jointFriction)",
      },
      createGearJoint = {
        type = "method",
        description = "Creates a gear joint that can only connect revolute and/or prismatic joints.\nLike the pulley ratio, you can specify a gear ratio. \nHowever, in this case the gear ratio can be negative. \nAlso keep in mind that when one joint is a revolute joint (angular) and the other joint is prismatic (translation), \nthen the gear ratio will have units of length or one over length.\nCaution: Deleting one of the connected joints automatically deletes this joint.\nCaution: The \"nodeB\" of both the connected joints must not be the same, and must be non-static.\nTwo different input types are permitted:\n| ``physics:createGearJoint(jointA, jointB, collideConnected)``\n| ``physics:createGearJoint(values)``",
        valuetype = "jointGear",
        args = "(jointA: object, jointB: object [, ratio: number] [, collideConnected: boolean], values: table)",
        returns = "(jointGear)",
      },
      createPrismaticJoint = {
        type = "method",
        description = "Creates a prismatic (piston) joint. Two different input types are permitted:\n| ``physics:createPrismaticJoint(nodeA, nodeB, x, y, localAxisX, localAxisY, collideConnected)``\n| ``physics:createPrismaticJoint(values)``",
        valuetype = "jointPrismatic",
        args = "(nodeA: object, nodeB: object, x: number, y: number, localAxisX: number, localAxisY: number [, collideConnected: boolean], values: table)",
        returns = "(jointPrismatic)",
      },
      createPulleyJoint = {
        type = "method",
        description = "Creates a pulley joint that attaches two bodies with an imaginary rope whose length remains constant: if one body is pulled down, the other one will move up.\nTwo different input types are permitted:\n| ``physics:createPulleyJoint(nodeA, nodeB, x1, y1, x2, y2, collideConnected)``\n| ``physics:createPulleyJoint(values)``",
        valuetype = "jointPulley",
        args = "(nodeA: object, nodeB: object, groundAnchorAX: number, groundAnchorAY: number, groundAnchorBX: number, groundAnchorBY: number [, anchorAX: number] [, anchorAY: number] [, anchorBX: number] [, anchorBY: number] [, ratio: number] [, collideConnected: boolean], values: table)",
        returns = "(jointPulley)",
      },
      createRevoluteJoint = {
        type = "method",
        description = "Creates a revolute (pivot) joint that constrains the two attached bodies to rotate about a point.\nTwo different input types are permitted:\n| ``physics:createRevoluteJoint(nodeA, nodeB, x, y, collideConnected)``\n| ``physics:createRevoluteJoint(values)``",
        valuetype = "jointRevolute",
        args = "(nodeA: object, nodeB: object, x: number, y: number [, collideConnected: boolean], values: table)",
        returns = "(jointRevolute)",
      },
      createRopeJoint = {
        type = "method",
        description = "Creates a rope joint that restricts the maximum distance between two points. This can be useful to prevent chains of bodies from stretching, even under high load.\nTwo different input types are permitted:\n| ``physics:createRopeJoint(nodeA, nodeB, x1, y1, x2, y2, collideConnected)``\n| ``physics:createRopeJoint(values)``",
        valuetype = "jointRope",
        args = "(nodeA: object, nodeB: object [, anchorAX: number] [, anchorAY: number] [, anchorBX: number] [, anchorBY: number], values: table)",
        returns = "(jointRope)",
      },
      createTouchJoint = {
        type = "method",
        description = "Creates a \"touch\" (mouse) joint that attaches a body to the world through a spring.\nTwo different input types are permitted:\n| ``physics:createTouchJoint(nodeA, dampingRatio, frequency, maxForce)``\n| ``physics:createTouchJoint(values)``",
        valuetype = "jointTouch",
        args = "(nodeA: object [, dampingRatio: number] [, frequency: number] [, maxForce: number], values: table)",
        returns = "(jointTouch)",
      },
      createWeldJoint = {
        type = "method",
        description = "Creates a weld joint that literaly welds the two attached body in a point.\nTwo different input types are permitted:\n| ``physics:createWeldJoint(nodeA, nodeB, x, y, collideConnected)``\n| ``physics:createWeldJoint(values)``",
        valuetype = "jointWeld",
        args = "(nodeA: object, nodeB: object [, x: number] [, y: number] [, collideConnected: boolean], values: table)",
        returns = "(jointWeld)",
      },
      createWheelJoint = {
        type = "method",
        description = "Creates a wheel joint that combines a piston and a pivot joint. Two different input types are permitted:\n| ``physics:createWheelJoint(nodeA, nodeB, localAxisX, localAxisY, ax, ay, bx, by, collideConnected)``\n| ``physics:createWheelJoint(values)``",
        valuetype = "jointWheel",
        args = "(nodeA: object, nodeB: object, localAxisX: number, localAxisY: number [, ax: number] [, ay: number] [, bx: number] [, by: number] [, collideConnected: boolean], values: table)",
        returns = "(jointWheel)",
      },
      debugDraw = {
        type = "value",
        description = "True only if we wish to turn on debug drawing for the physics simulation.\nDebug drawing will draw an outline of the body shape for all display objects in the simulation.\nSelective objects can be excluded from the debug drawing, by setting their node.physics.debugDraw = false\nEach object's debug drawing color can also be set, using node.physics.debugDrawColor.\nBy default, the debug drawing color matches the shape type: blue for circles, red for rectangles,\nyellow for polygons (which is, of course, the right way to do things: http://www.staff.science.uu.nl/~kreve101/composable-art/colorshapes.html ).",
      },
      getGravity = {
        type = "method",
        description = "Get the gravity vector, in display coordinates.\nThe values are returned as a number pair (x, y).\n:return x (number): The x coordinate of the gravity vector, in display coordinates.\n:return y (number): The y coordinate of the gravity vector, in display coordinates.",
        args = "()",
        returns = "()",
      },
      getTimeScale = {
        type = "method",
        description = "Get the time scaling factor currently applied to the physics simulation.",
        args = "()",
        returns = "(number)",
      },
      pause = {
        type = "method",
        description = "Pause the physics simulation.",
        args = "()",
        returns = "()",
      },
      removeNode = {
        type = "method",
        description = "Remove a node from the physics simulation. This will also destroy any joints attached to the node.\nNote that this will NOT remove/destroy the node (display object) itself; it will simply remove it from the\nphysics simulation.\nMore importantly, if a node is destroyed (garbage collected) then it will NOT automatically\nremove its associated body from the physics simulation; i.e. the app is responsible for calling physics:removeNode()\nbefore the node is destroyed (garbage collected).\nAll physics properties are lost. If the node is not currently part of the simulation, the function has no effect.",
        args = "(n: object)",
        returns = "()",
      },
      resume = {
        type = "method",
        description = "Resume the physics simulation.",
        args = "()",
        returns = "()",
      },
      setAllowSleeping = {
        type = "method",
        description = "Set whether or not the simulation should allow bodies to 'go to sleep' if nothing is happening to them, \nfor efficiency. If this is set to true, bodies will sleep when they come to rest, and are excluded from \nthe simulation until something happens to 'wake' them again. This could be a collision from another body, \nor a force explicitly applied to the body.\nNote that if the app changes the world gravity vector, this does not count as an explicit force in this\ncontext. To allow stationary bodies to respond to a changing gravity vector, you should disallow sleeping.",
        args = "(allow: boolean)",
        returns = "()",
      },
      setGravity = {
        type = "method",
        description = "Set the gravity vector, in display coordinates.",
        args = "(x: number, y: number)",
        returns = "()",
      },
      setIterations = {
        type = "method",
        description = "Set the number of position and velocity iterations used by the physics simulation step.",
        args = "(pos: number, vel: number)",
        returns = "()",
      },
      setScale = {
        type = "method",
        description = "Set the scaling factor that converts from display coordinates to physics simulation coordinates.",
        args = "(scale: number)",
        returns = "()",
      },
      setTimeScale = {
        type = "method",
        description = "Set a time scaling factor to apply when updating the physics simulation.\nFor example a value of 2 would cause the simulation to run at double speed.",
        args = "(f: number)",
        returns = "()",
      },
    },
  },
  joint = {
    type = "class",
    childs = {
      destroy = {
        type = "method",
        description = "Destroys this joint, and detach it from the connected rigid bodies (nodes).\nNote that this may cause the connected bodies to begin colliding. \nJointDistance Properties\n========================\n* :c:member:`jointdistance.length`\n* :c:member:`jointdistance.frequency`\n* :c:member:`jointdistance.dampingRatio`",
        args = "()",
        returns = "()",
      },
      getAnchorA = {
        type = "method",
        description = "Gets the anchor point of node A, in world (display) coordinates.\nThe point is returned as a number pair (x, y).\n:return x (number): The x coordinate (in display coordinates).\n:return y (number): The y coordinate (in display coordinates).",
        args = "()",
        returns = "()",
      },
      getAnchorB = {
        type = "method",
        description = "Gets the anchor point of node B, in world (display) coordinates.\nThe point is returned as a number pair (x, y).\n:return x (number): The x coordinate (in display coordinates).\n:return y (number): The y coordinate (in display coordinates).",
        args = "()",
        returns = "()",
      },
      getNodeA = {
        type = "method",
        description = "Gets the \"node A\" to which this joint is attached.",
        args = "()",
        returns = "(object)",
      },
      getNodeB = {
        type = "method",
        description = "Gets the \"node B\" to which this joint is attached.",
        args = "()",
        returns = "(object)",
      },
      getReactionForce = {
        type = "method",
        description = "Gets the reaction force (in Newtons) on node B at the joint anchor.\nThe force vector is returned as a number pair (x, y).\n:return x (number): The x value.\n:return y (number): The y value.",
        args = "()",
        returns = "()",
      },
      getReactionTorque = {
        type = "method",
        description = "Gets the reaction torque (in Newtons * meters) on node B at the joint anchor.",
        args = "()",
        returns = "(number)",
      },
      isActive = {
        type = "method",
        description = "Gets the active status of this joint. The active status is actually defined by the status of the connected body: \nif both are active the joint is considered active.",
        args = "()",
        returns = "(boolean)",
      },
      isCollideConnected = {
        type = "method",
        description = "Gets the collide connected flag: if true, the attached bodies may collide.",
        args = "()",
        returns = "(boolean)",
      },
    },
  },
  jointDistance = {
    type = "class",
    childs = {
      dampingRatio = {
        type = "value",
        description = "The damping ratio of any springiness. 0 = no damping, 1 = critical damping.\nAlong with frequency, determines the springiness of the joint.\nJointRevolute Properties\n========================\n* :c:member:`jointrevolute.lowerAngle`\n* :c:member:`jointrevolute.upperAngle`\n* :c:member:`jointrevolute.limitEnabled`\n* :c:member:`jointrevolute.maxMotorTorque`\n* :c:member:`jointrevolute.motorSpeed`\n* :c:member:`jointrevolute.motorEnabled`\n* :c:member:`jointrevolute.motorTorque`\n* :c:member:`jointrevolute.jointSpeed`\n* :c:member:`jointrevolute.jointAngle`",
      },
      frequency = {
        type = "value",
        description = "The mass-spring-damper frequency in Hertz. Along with dampingRatio, determines the springiness of the joint.\nA value of 0 disables springiness.",
      },
      length = {
        type = "value",
        description = "The natural length of the joint, in display coordinates.",
      },
    },
    inherits = "joint",
  },
  jointFriction = {
    type = "class",
    childs = {
      maxForce = {
        type = "value",
        description = "The maximum friction force, in Newtons.",
      },
      maxTorque = {
        type = "value",
        description = "The maximum friction torque, in Newton-metres.\nJointWeld Properties\n========================\n* :c:member:`jointweld.frequency`\n* :c:member:`jointweld.dampingRatio`",
      },
    },
    inherits = "joint",
  },
  jointGear = {
    type = "class",
    childs = {
      joint1 = {
        type = "value",
        description = "The 1st revolute/prismatic joint attached to the gear joint.",
      },
      joint2 = {
        type = "value",
        description = "The 2nd revolute/prismatic joint attached to the gear joint.\nJointRope Properties\n========================\n* :c:member:`jointrope.maxLength`",
      },
      ratio = {
        type = "value",
        description = "The gear ratio.",
      },
    },
    inherits = "joint",
  },
  jointPrismatic = {
    type = "class",
    childs = {
      jointSpeed = {
        type = "value",
        description = "Gets the current joint speed (display units per second).",
      },
      jointTranslation = {
        type = "value",
        description = "Gets the current joint offset (distance, in display units).\nJointFriction Properties\n========================\n* :c:member:`jointfriction.maxForce`\n* :c:member:`jointfriction.maxTorque`",
      },
      limitEnabled = {
        type = "value",
        description = "If true, lowerTranslation and upperTranslation define the distance range within which the joint is limited. The joint translation is\nrelative to the initially specified position of the two bodies - it starts at 0. Therefore, the lowerTranslation limit should be\nnegative, and the upperTranslation limit should be positive.",
      },
      lowerTranslation = {
        type = "value",
        description = "The lower distance limit for the joint (should be negative). Only effective if limitEnabled is true.",
      },
      maxMotorTorque = {
        type = "value",
        description = "The maximum motor torque used to achieve the desired motor speed. Only effective if motorEnabled is true.",
      },
      motorEnabled = {
        type = "value",
        description = "If true, the joint's motor is enabled.",
      },
      motorForce = {
        type = "value",
        description = "Gets the current motor force.",
      },
      motorSpeed = {
        type = "value",
        description = "The desired motor speed (angular velocity).",
      },
      upperTranslation = {
        type = "value",
        description = "The upper distance limit for the joint (should be positive). Only effective if limitEnabled is true.",
      },
    },
    inherits = "joint",
  },
  jointPulley = {
    type = "class",
    childs = {
      lengthA = {
        type = "value",
        description = "The reference length for the segment attached to node A.",
      },
      lengthB = {
        type = "value",
        description = "The reference length for the segment attached to node B.\nJointTouch Functions\n========================\n* :func:`jointtouch:setTarget()`",
      },
      ratio = {
        type = "value",
        description = "The pulley ratio, used to simulate a block-and-tackle.",
      },
    },
    inherits = "joint",
  },
  jointRevolute = {
    type = "class",
    childs = {
      jointAngle = {
        type = "value",
        description = "Gets the current joint speed (angular velocity).\nJointPrismatic Properties\n=========================\n* :c:member:`jointprismatic.limitEnabled`\n* :c:member:`jointprismatic.lowerTranslation`\n* :c:member:`jointprismatic.upperTranslation`\n* :c:member:`jointprismatic.motorEnabled`\n* :c:member:`jointprismatic.maxMotorTorque`\n* :c:member:`jointprismatic.motorSpeed`\n* :c:member:`jointprismatic.motorForce`\n* :c:member:`jointprismatic.jointSpeed`\n* :c:member:`jointprismatic.jointTranslation`",
      },
      jointSpeed = {
        type = "value",
        description = "Gets the current motor speed (angular velocity).",
      },
      limitEnabled = {
        type = "value",
        description = "If true, lowerAngle and upperAngle define the angle range within which the joint is limited.",
      },
      lowerAngle = {
        type = "value",
        description = "The lower angle for the joint limit (degrees). Only effective if limitEnabled is true.",
      },
      maxMotorTorque = {
        type = "value",
        description = "The maximum motor torque used to achieve the desired motor speed. Only effective if motorEnabled is true.",
      },
      motorEnabled = {
        type = "value",
        description = "If true, the joint's motor is enabled.",
      },
      motorSpeed = {
        type = "value",
        description = "The desired motor speed (angular velocity). Only effective if motorEnabled is true.",
      },
      motorTorque = {
        type = "value",
        description = "Gets the current motor torque.",
      },
      upperAngle = {
        type = "value",
        description = "The upper angle for the joint limit (degrees). Only effective if limitEnabled is true.",
      },
    },
    inherits = "joint",
  },
  jointRope = {
    type = "class",
    childs = {
      maxLength = {
        type = "value",
        description = "The maximum length of the rope.\nJointWheel Properties\n========================\n* :c:member:`jointwheel.motorEnabled`\n* :c:member:`jointwheel.motorSpeed`\n* :c:member:`jointwheel.maxMotorTorque`\n* :c:member:`jointwheel.springFrequency`\n* :c:member:`jointwheel.springDampingRatio`\n* :c:member:`jointwheel.motorTorque`\n* :c:member:`jointwheel.jointSpeed`\n* :c:member:`jointwheel.jointTranslation`",
      },
    },
    inherits = "joint",
  },
  jointTouch = {
    type = "class",
    childs = {
      dampingRatio = {
        type = "value",
        description = "The damping ratio. 0 = no damping, 1 = critical damping. Default value is 0.7.",
      },
      frequency = {
        type = "value",
        description = "The response speed. Default value is 5.",
      },
      maxForce = {
        type = "value",
        description = "The maximum constraint force that can be exerted\nto move the candidate body. Usually you will express\nas some multiple of the weight (multiplier * mass * gravity). Default value is 0.\nJointGear Properties\n========================\n* :c:member:`jointgear.ratio`\n* :c:member:`jointgear.joint1`\n* :c:member:`jointgear.joint2`",
      },
      setTarget = {
        type = "method",
        description = "Sets the target point for the touch joint.\nJointTouch Properties\n========================\n* :c:member:`jointtouch.dampingRatio`\n* :c:member:`jointtouch.frequency`\n* :c:member:`jointtouch.maxForce`",
        args = "(x: number, y: number)",
        returns = "()",
      },
    },
    inherits = "joint",
  },
  jointWeld = {
    type = "class",
    childs = {
      dampingRatio = {
        type = "value",
        description = "The damping ratio. 0 = no damping, 1 = critical damping.\nJointPulley Properties\n========================\n* :c:member:`jointpulley.ratio`\n* :c:member:`jointpulley.lengthA`\n* :c:member:`jointpulley.lengthB`",
      },
      frequency = {
        type = "value",
        description = "The mass-spring-damper frequency in Hertz. Rotation only. Disable softness with a value of 0.",
      },
    },
    inherits = "joint",
  },
  jointWheel = {
    type = "class",
    childs = {
      jointSpeed = {
        type = "value",
        description = "The speed (angular velocity) of the motor.",
      },
      jointTranslation = {
        type = "value",
        description = "The translation (distance) of the joint; i.e. the offset along the specified axis.",
      },
      maxMotorTorque = {
        type = "value",
        description = "The maximum motor torque, usually in Newton-metres.",
      },
      motorEnabled = {
        type = "value",
        description = "True to enable the joint motor. Default is false.",
      },
      motorSpeed = {
        type = "value",
        description = "The desired motor speed (angular velocity).",
      },
      motorTorque = {
        type = "value",
        description = "The torque of the motor, in Newton-metres.",
      },
      springDampingRatio = {
        type = "value",
        description = "Suspension damping ratio, one indicates critical damping. Default value is 0.7.",
      },
      springFrequency = {
        type = "value",
        description = "Suspension frequency, zero indicates no suspension. Default value is 2.",
      },
    },
    inherits = "joint",
  },
  scene = {
    type = "class",
    childs = {
      releaseAnimation = {
        type = "method",
        description = "Clears the scene's references to the specified Animation object. If the app maintains any additional\nreferences to this objects then it will persist, otherwise it will become ready for garbage \ncollection.",
        args = "(animation: object)",
        returns = "()",
      },
      releaseAtlas = {
        type = "method",
        description = "Clears the scene's references to the specified Atlas object. If the app maintains any additional\nreferences to this objects then it will persist, otherwise it will become ready for garbage \ncollection.",
        args = "(atlas: object)",
        returns = "()",
      },
      releaseFont = {
        type = "method",
        description = "Clears the scene's references to the specified Font object. If the app maintains any additional\nreferences to this objects then it will persist, otherwise it will become ready for garbage \ncollection.",
        args = "(font: object)",
        returns = "()",
      },
      releaseResources = {
        type = "method",
        description = "Clears the scene's references to all owned Atlas, Animation and Font objects. If the app \nmaintains additional references to any specific objects of these types then they will persist, \notherwise the objects will become ready for garbage collection.",
        args = "()",
        returns = "()",
      },
    },
    inherits = "node",
  },
  sprite = {
    type = "class",
    childs = {
      blendMode = {
        type = "value",
        description = "The blend mode to use when rendering.\nAvailable options are:\n* \"normal\" - Standard alpha blended, assuming the texture has pre-multiplied alpha (src = GL_ONE, dest = GL_ONE_MINUS_SRC_ALPHA)\n* \"add\" - Standard additive (src = GL_ONE, dest = GL_ONE)\n* \"multiply\" - Standard multiply (src = GL_ZERO, dest = GL_SRC_COLOR)\n* \"screen\" - (src = GL_ONE, dest = GL_ONE_MINUS_SRC_COLOR)\nThe default value is \"normal\".",
      },
      debugDrawTextureRegion = {
        type = "value",
        description = "True only if we wish to display the texture region as box, for debugging purposes",
      },
      getAtlas = {
        type = "method",
        description = "Gets the atlas object currently referenced by this sprite. If the sprite has an animation, it returns the atlas\nassociated with the current animation frame.",
        args = "()",
        returns = "(object)",
      },
      pause = {
        type = "method",
        description = "Pause any current animation.",
        args = "()",
        returns = "()",
      },
      play = {
        type = "method",
        description = "Play the current assigned animation.",
        args = "([n: table] [, startFrame: number] [, loopCount: number])",
        returns = "()",
      },
      setAnimation = {
        type = "method",
        description = "Sets an animation resource to use with this sprite. Typically, an animation resource contains multiple sprite frames, \nas rectangular texture regions within the bitmap.",
        args = "(anim: object)",
        returns = "()",
      },
      setFrame = {
        type = "method",
        description = "Sets the current animation frame to display (if using an animation).",
        args = "(frame: number)",
        returns = "()",
      },
      timeScale = {
        type = "value",
        description = "A scaling factor to apply to the animation rate.\nThe default value is 1.\nfloat timeScale;",
      },
      uvRect = {
        type = "value",
        description = "Specifies the UV range ('texture window') to apply to the sprite. \nThe Rect object values are interpreted as follows:\n- x (number) - The U offset from the top-left of the sprite's image frame\n- y (number) - The V offset from the top-left of the sprite's image frame\n- w (number) - The U span across the sprite\n- h (number) - The V span across the sprite\nAll values should be considered as proportional to the size of the sprite's current image \nframe (which is generally the size of the sprite's bitmap, unless it is using an animation \nor explicit atlas). The default value is {x=0, y=0, w=1, y=1}, i.e. the sprite is mapped to \nthe entire span of the image frame.\nNote that if any edge of the specified UV rectangle falls outside the sprite frame, then \nthe UV wrapping mode of the sprite's Atlas object comes into play. Generally speaking, \nif you want to exploit UV wrapping, you must set the UV wrapping mode explicitly. \nFor example::\nlocal s = director:createSprite(0, 0, \"textures/testbox64.png\")\n-- Ensure texture is set to repeat (wrap)\ns:getAtlas():setTextureParams(\"GL_LINEAR\", \"GL_LINEAR\", \"GL_REPEAT\", \"GL_REPEAT\")\n-- Repeat image twice along each axis\ns.uvRect = {0, 0, 2, 2}\nNote that the sprite's w and h values correspond to the size of the sprite frame multiplied \nby the u and v span respectively. Hence in the example above, if the bitmap is 64x64 pixels, \nthe w and h of the sprite will both be 128.\nNote also that many GPUs can only wrap textures if the bitmap dimensions are powers of 2. \nQuick will throw a warning if you attempt to wrap a texture that does not have power-of-2 \ndimensions.\nuvRect values can be tweened to create interesting UV animation effects. For example::\nlocal s = director:createSprite(0, 0, \"textures/testbox64.png\")\ns:getAtlas():setTextureParams(\"GL_LINEAR\", \"GL_LINEAR\", \"GL_REPEAT\", \"GL_REPEAT\")\nlocal tw = tween:to(s, {mode=\"repeat\", time=1, uvRect={x=1})\nThe box will scroll to the left indefinitely (we could use tw:cancel() to stop it).",
      },
      xFlip = {
        type = "value",
        description = "True only if the sprite should be flipped along its local x axis.",
      },
      yFlip = {
        type = "value",
        description = "True only if the sprite should be flipped along its local y axis.",
      },
    },
    inherits = "node",
  },
  system = {
    type = "class",
    childs = {
      addEventListener = {
        type = "method",
        description = "Add a global event listener.\nwith an index named <event name> that is a listener function.\nFor example, the listener could be a Lua table containing a \nfunction called \"touch\", and if the table is registered as a\nlistener, the function will be called on touch events.\nExample::\n-- Example of a function listener\nlocal myNode = director:createSprite(0, 0, \"textures/beachball.png\")\nlocal updateFunction = function(event)\n-- Do something on every update event\nend\nsystem:addEventListener(\"update\", updateFunction)\n-- Example of a table listener\nlocal myNode = director:createSprite(0, 0, \"textures/beachball.png\")\nfunction myNode:update(event)\n-- Do something on every update event\nend\nsystem:addEventListener(\"update\", myNode)",
        args = "(name: string or table, funcortable: function or table)",
        returns = "()",
      },
      addTimer = {
        type = "method",
        description = "Add a global timed event.",
        valuetype = "timer",
        args = "(funcortable: function or table, period: number, iterations: number, delay: number)",
        returns = "(timer)",
      },
      debugTime = {
        type = "value",
        description = "If the user sets this property, it will be used as the return value for system:getTime().",
      },
      deltaTime = {
        type = "value",
        description = "The elapsed time, in seconds, since the last app frame (i.e. since the last \"update\" event). During\nnormal app behaviour, this value is reasonably consistent; for example if the app is running at 25\nframes per second, this value will be around 0.04 seconds. Note that if the app pauses to perform some\nheavy processing (for example, when tearing down or setting up a new scene) then at the next app\nupdate, this value can temporarily spike to a large value.",
      },
      gameTime = {
        type = "value",
        description = "The elapsed time, in seconds, since the start of the app. It's preferable for the app to call\nsystem:getTime(), which returns the same value unless system.debugTime is set, in which case it\nreturns system.debugTime.",
      },
      getFilePath = {
        type = "method",
        description = "Get the absolute path for a file. We specify the relative path, and indicate whether the file is in the app's\nlocal storage area (which is read-only), or the device's shared storage area (which is read/write).\nis assumed, so the return value will simply be the path to the root of the area specified by \"type\".",
        args = "(type: string, relPath: string)",
        returns = "(string)",
      },
      getFocus = {
        type = "method",
        description = "Gets the explicit \"focus\" object for touch events.",
        args = "()",
        returns = "(object)",
      },
      getTime = {
        type = "method",
        description = "Return the total elapsed time of the app, in seconds.\nIf system.debugTime is not nil, we instead return the value of system.debugTime. This provides\na useful hook for debugging.",
        args = "()",
        returns = "(number)",
      },
      getTimersTimeScale = {
        type = "method",
        description = "Get the time scaling factor currently applied to all system timers.",
        args = "()",
        returns = "(number)",
      },
      getVersionString = {
        type = "method",
        description = "Returns the current version number of the Quick engine. This version number\nis increased for each formal Quick release, and is typically of the form\nmajor.minor, or major.minor.subminor; for example \"1.1\".",
        args = "()",
        returns = "(string)",
      },
      pauseTimers = {
        type = "method",
        description = "Pause all system timers. Note that the system \"update\" event is NOT affected by this function; it will continue to fire.",
        args = "()",
        returns = "()",
      },
      quit = {
        type = "method",
        description = "Quit the app.",
        args = "()",
        returns = "()",
      },
      removeEventListener = {
        type = "method",
        description = "Remove a global event listener.",
        args = "(name: string or table, funcortable: function or table)",
        returns = "()",
      },
      resumeTimers = {
        type = "method",
        description = "Resume all system timers. Note that if any individual timer has been explicitly paused with timer:pause()\nthen it will continue to be paused.",
        args = "()",
        returns = "()",
      },
      sendEvent = {
        type = "method",
        description = "Send a global (user-defined) event. The event can be listened for in the same way as Quick system events.\nExample::\n-- Set up a listener function\nlocal fooFunction = function(event)\n-- Do something on every \"foo\" event\ndbg.print(\"Type is \", event.type)\ndbg.print(\"Size is \", event.size)\nend\nsystem:addEventListener(\"foo\", fooFunction)\n-- Throw user event\nsystem:sendEvent(\"foo\", { type=\"bar\", size=3 } )",
        args = "(name: string, values: table)",
        returns = "()",
      },
      setFocus = {
        type = "method",
        description = "Sets an explicit \"focus\" object for touch events. If not nil, the specified object will be the first\nto receive touch events, regardless of whether the event intercepts the object bounds. Events are then\npropogated to other objects as usual. If nil, the default behaviour is resumed.",
        args = "(n: object)",
        returns = "()",
      },
      setFrameRateLimit = {
        type = "method",
        description = "Sets the target frame rate limit in frames per second.",
        args = "(fps: number)",
        returns = "()",
      },
      setTimersTimeScale = {
        type = "method",
        description = "Set a time scaling factor to apply when updating all system timers.\nFor example a value of 2 would cause all timers to run at double speed.",
        args = "(f: number)",
        returns = "()",
      },
    },
  },
  tiledmap = {
    type = "class",
    childs = {
      getLayerNamed = {
        type = "method",
        description = "Return the TiledMapLayer object of the specified name, or nil if\nno layer exists with that name.",
        args = "(layername: string)",
        returns = "(object)",
      },
      getObjectGroupNamed = {
        type = "method",
        description = "Return the TiledMapObjectGroup object of the specified name, or nil if\nno object group exists with that name.",
        args = "(groupname: string)",
        returns = "(object)",
      },
      getProperty = {
        type = "method",
        description = "Return the value for the specific property name. Properties can be set\non the tiled map in the Tiled GUI app.\nNote that properties set on the tiled map and separate from any\nproperties set on tiled map layers, on object groups, or on\nindividual objects.\nspecified property is not found.",
        args = "(propname: string)",
        returns = "(string)",
      },
      mapOrientation = {
        type = "value",
        description = "The map orientation. Values must be one of:\n- tiledMap.orientationOrtho (orthogonal)\n- tiledMap.orientationHex (hexagonal)\n- tiledMap.orientationIso (isometric)",
      },
      mapSize = {
        type = "value",
        description = "The size of the tiled map, in tiles.\nThe value is a Vec2 object: the x property represents the tiled map\nwidth, and the y property represents the tiled map height.",
      },
      tileSize = {
        type = "value",
        description = "The size of each tile, in pixels.\nThe value is a Vec2 object: the x property represents the tile\nwidth, and the y property represents the tile height.",
      },
    },
    inherits = "node",
  },
  tiledmaplayer = {
    type = "class",
    childs = {
      getGIDAtGridRef = {
        type = "method",
        description = "Returns the Global ID (GID) at a given tile coordinate (grid reference),\nfollowed by the tile flags.\nA GID value of 0 means that the tile is empty.\nThis method requires that tiledmaplayer.releaseMap() has NOT previously\nbeen called.\n:return gid (number): The Global ID (GID) of the tile.\n:return flags (number): Any flags associated with the tile. Flags indicate if the tile is flipped around any axis,\nand can be an OR'd combination of:\n- tiledMap.horizontal\n- tiledMap.vertical\n- tiledMap.diagonal\nExample::\nlocal tm = director:createTiledMap(\"tiledmaps/desert-test.tmx\")\nlocal tl = tm.children[1]\nlocal gid, flags = tl:getGIDAtGridRef(0, 0)",
        args = "(x: number, y: number)",
        returns = "()",
      },
      getPosAtGridRef = {
        type = "method",
        description = "Returns the position in pixels of the bottom-left of the area at a \ngiven tile coordinate (grid reference).\nExample::\nlocal tm = director:createTiledMap(\"tiledmaps/desert-test.tmx\")\nlocal tl = tm.children[1]\nlocal px, py = tl:getPosAtGridRef(1, 1)",
        args = "(x: number, y: number)",
        returns = "(number, number)",
      },
      getProperty = {
        type = "method",
        description = "Return the value for the specific property name. Properties can be set\non the tiled map layer in the Tiled GUI app.\nNote that properties set on tiled map layers are separate from any\nproperties set on the tiled map object itself, on object groups, or on\nindividual objects.\nspecified property is not found.",
        args = "(propname: string)",
        returns = "(string)",
      },
      layerSize = {
        type = "value",
        description = "The size of the tiled map layer, in tiles.\nThe value is a Vec2 object: the x property represents the tiled map layer\nwidth, and the y property represents the tiled map height.",
      },
      mapOrientation = {
        type = "value",
        description = "The map layer orientation. Values must be one of:\n- tiledMap.orientationOrtho (orthogonal)\n- tiledMap.orientationHex (hexagonal)\n- tiledMap.orientationIso (isometric)",
      },
      mapTileSize = {
        type = "value",
        description = "The size of each tile, in pixels.\nThe value is a Vec2 object: the x property represents the tile\nwidth, and the y property represents the tile height.",
      },
      releaseMap = {
        type = "method",
        description = "Free the memory associated with the tile positions. Unless you want to \nknow at runtime the pixel positions of the tiles, you can safely call \nthis method and free some memory.\nIf you are going to call tiledmaplayer.getGIDAtGridRef() or\ntiledmaplayer.getPosAtGridRef() then you must NOT call this function.",
        args = "()",
        returns = "()",
      },
      removeTileAtGridRef = {
        type = "method",
        description = "Removes a tile at given tile coordinate (grid reference). The area at\nthe given tile coordinate will become empty.",
        args = "(x: number, y: number)",
        returns = "()",
      },
      setGIDAtGridRef = {
        type = "method",
        description = "Sets the tile Global ID (GID) at a given tile coordinate (grid reference).\nIf a tile is already present at this grid reference, it will be replaced with a tile of the specified GID.\ncan be an OR'd combination of:\n- tiledMap.horizontal\n- tiledMap.vertical\n- tiledMap.diagonal",
        args = "(x: number, y: number, gid: number, flags: number)",
        returns = "()",
      },
      setupTiles = {
        type = "method",
        description = "Creates the tile objects for the tiled map layer. This function\nis called automatically when a tiled map is created from a PLIST file,\nand should not normally be called explicitly by the app.",
        args = "()",
        returns = "()",
      },
    },
    inherits = "node",
  },
  tiledmapobject = {
    type = "class",
    childs = {
      getProperty = {
        type = "method",
        description = "Return the value for the specific property name. Properties can be set\non objects in the Tiled GUI app.\nNote that properties set on objects are separate from any\nproperties set on object groups, on tiled map layers, or on the tiled \nmap object itself.\nspecified property is not found.",
        args = "(propname: string)",
        returns = "(string)",
      },
    },
  },
  tiledmapobjectgroup = {
    type = "class",
    childs = {
      getProperty = {
        type = "method",
        description = "Return the value for the specific property name. Properties can be set\non object groups in the Tiled GUI app.\nNote that properties set on object groups are separate from any\nproperties set on individual objects, on tiled map layers, or on the \ntiled map object itself.\nspecified property is not found.",
        args = "(propname: string)",
        returns = "(string)",
      },
    },
  },
  timer = {
    type = "class",
    childs = {
      cancel = {
        type = "method",
        description = "Cancel the timer.",
        args = "()",
        returns = "()",
      },
      pause = {
        type = "method",
        description = "Pause the timer.",
        args = "()",
        returns = "()",
      },
      resume = {
        type = "method",
        description = "Resume the timer.",
        args = "()",
        returns = "()",
      },
    },
  },
  ease = {
    type = "class",
    childs = {
      backIn = {
        type = "method",
        description = "Easing function providing an \"overshoot\" and spring-back curve.\nThe easing is \"soft\" at the start of the tween, and \"hard\" at the end.",
        args = "(time: number)",
        returns = "(number)",
      },
      backInOut = {
        type = "method",
        description = "Easing function providing an \"overshoot\" and spring-back curve.\nThe easing is \"soft\" at both the start and end of the tween.",
        args = "(time: number)",
        returns = "(number)",
      },
      backOut = {
        type = "method",
        description = "Easing function providing an \"overshoot\" and spring-back curve.\nThe easing is \"hard\" at the start of the tween, and \"soft\" at the end.",
        args = "(time: number)",
        returns = "(number)",
      },
      bounceIn = {
        type = "method",
        description = "Easing function providing a damped bouncing curve.\nThe easing is \"soft\" at the start of the tween, and \"hard\" at the end.",
        args = "(time: number)",
        returns = "(number)",
      },
      bounceInOut = {
        type = "method",
        description = "Easing function providing a damped bouncing curve.\nThe easing is \"soft\" at both the start and end of the tween.",
        args = "(time: number)",
        returns = "(number)",
      },
      bounceOut = {
        type = "method",
        description = "Easing function providing a damped bouncing curve.\nThe easing is \"hard\" at the start of the tween, and \"soft\" at the end.",
        args = "(time: number)",
        returns = "(number)",
      },
      elasticIn = {
        type = "method",
        description = "Easing function providing a damped oscillation curve.\nThe value field specifies the period of the oscillation wave, and defaults to 2 * PI.\nThe easing is \"soft\" at the start of the tween, and \"hard\" at the end.",
        args = "(time: number, period: number)",
        returns = "(number)",
      },
      elasticInOut = {
        type = "method",
        description = "Easing function providing a damped oscillation curve.\nThe value field specifies the period of the oscillation wave, and defaults to 2 * PI.\nThe easing is \"soft\" at both the start and end of the tween.",
        args = "(time: number, period: number)",
        returns = "(number)",
      },
      elasticOut = {
        type = "method",
        description = "Easing function providing a damped oscillation curve.\nThe value field specifies the period of the oscillation wave, and defaults to 2 * PI.\nThe easing is \"hard\" at the start of the tween, and \"soft\" at the end.",
        args = "(time: number, period: number)",
        returns = "(number)",
      },
      expIn = {
        type = "method",
        description = "Easing function providing a curve based on raising a value to the power of 10 * time.\nThe value defaults to 2.\nThe easing is \"soft\" at the start of the tween, and \"hard\" at the end.",
        args = "(time: number, value: number)",
        returns = "(number)",
      },
      expInOut = {
        type = "method",
        description = "Easing function providing a curve based on raising a value to the power of 10 * time.\nThe value defaults to 2.\nThe easing is \"soft\" at both the start and end of the tween.",
        args = "(time: number, value: number)",
        returns = "(number)",
      },
      expOut = {
        type = "method",
        description = "Easing function providing a curve based on raising a value to the power of 10 * time.\nThe value defaults to 2.\nThe easing is \"hard\" at the start of the tween, and \"soft\" at the end.",
        args = "(time: number, value: number)",
        returns = "(number)",
      },
      linear = {
        type = "method",
        description = "Easing function providing a linear curve (straight line).",
        args = "(time: number)",
        returns = "(number)",
      },
      one = {
        type = "method",
        description = "Easing function providing a straight line, with fixed value of 1.\nWhen combined with a mode of 'mirror', this can be used to provide a square wave.",
        args = "(time: number)",
        returns = "(number)",
      },
      powIn = {
        type = "method",
        description = "Easing function providing a curve based on raising the time value to a specified power.\nThe value field specifies the power, and defaults to 2 (i.e. a squared function).\nThe easing is \"soft\" at the start of the tween, and \"hard\" at the end.",
        args = "(time: number, power: number)",
        returns = "(number)",
      },
      powInOut = {
        type = "method",
        description = "Easing function providing a curve based on raising the time value to a specified power.\nThe value field specifies the power, and defaults to 2 (i.e. a squared function).\nThe easing is \"soft\" at both the start and end of the tween.",
        args = "(time: number, power: number)",
        returns = "(number)",
      },
      powOut = {
        type = "method",
        description = "Easing function providing a curve based on raising the time value to a specified power.\nThe value field specifies the power, and defaults to 2 (i.e. a squared function).\nThe easing is \"hard\" at the start of the tween, and \"soft\" at the end.",
        args = "(time: number, power: number)",
        returns = "(number)",
      },
      sineIn = {
        type = "method",
        description = "Easing function providing a sine curve raised to a specified power.\nThe value field specifies the power, and defaults to 1 (i.e. a standard sine function).\nThe easing is \"soft\" at the start of the tween, and \"hard\" at the end.",
        args = "(time: number, power: number)",
        returns = "(number)",
      },
      sineInOut = {
        type = "method",
        description = "Easing function providing a sine curve raised to a specified power.\nThe value field specifies the power, and defaults to 1 (i.e. a standard sine function).\nThe easing is \"soft\" at the both the start and end of the tween.",
        args = "(time: number, power: number)",
        returns = "(number)",
      },
      sineOut = {
        type = "method",
        description = "Easing function providing a sine curve raised to a specified power.\nThe value field specifies the power, and defaults to 1 (i.e. a standard sine function).\nThe easing is \"hard\" at the start of the tween, and \"soft\" at the end.",
        args = "(time: number, power: number)",
        returns = "(number)",
      },
      zero = {
        type = "method",
        description = "Easing function providing a straight line, with fixed value of 0.\nWhen combined with a mode of 'mirror', this can be used to provide a square wave.",
        args = "(time: number)",
        returns = "(number)",
      },
    },
  },
  tween = {
    type = "class",
    childs = {
      cancel = {
        type = "method",
        description = "Cancel a tween instance.",
        args = "(t: object)",
        returns = "()",
      },
      dissolve = {
        type = "method",
        description = "Perform a specific type of tween, namely a \"dissolve\" between two Sprite nodes, where the \"source\" node is faded out\nat the same time as the \"destination\" node is faded in.",
        args = "(source: object, destination: object, time: number, delay: number)",
        returns = "(object)",
      },
      from = {
        type = "method",
        description = "Tween a series of node properties over time.\nThe behaviour is identical to tween:to(), with the exception that the values are interpolated FROM the specified values\nTO the current values.",
        args = "(target: object, params: table)",
        returns = "(object)",
      },
      getElapsedTime = {
        type = "method",
        description = "Get the amount of time elapsed on the tween. This begins increasing right after the tween \nis created.",
        args = "()",
        returns = "(number)",
      },
      getNumCycles = {
        type = "method",
        description = "Return the number of cycles that have elapsed on the tween. \nIf mode==\"clamp\", this is 0 until the tween completes, then 1.\nIf mode== \"mirror\" or \"repeat\", this increases by 1 for each completed cycle.",
        args = "()",
        returns = "(number)",
      },
      isAnimating = {
        type = "method",
        description = "Returns true only if the tween has started animating values, i.e. after any 'delay' has \nbeen passed.",
        args = "()",
        returns = "(boolean)",
      },
      to = {
        type = "method",
        description = "Tween a series of node properties over time.\nThe final property values are specified in the params table. To customize the\ntween, you can optionally specify the following non-animating properties in params:\n* params.time (number) - The duration of the tween, in seconds. Default value is 0.5. The duration refers\nto the length of a 'cycle'; see the 'mode' parameter below.\n* params.delay (number) - Any delay in seconds before the tween begins. Default value is 0. If this value is\ngreater than 0, then this period must elapse before any properties start tweening. The 'onStart' callback\nis called only when this period has elapsed.\n* params.delta (boolean) - Whether or not to interpret the specified values as absolute, or relative to the\ncurrent values. The default is false, meaning absolute values. If true, the 'current' values are those\nat the point at which any 'delay' period has elapsed.\n* params.mode (string) - Can be 'clamp', 'repeat' or 'mirror'. The default value is 'clamp'.\n* \"clamp\" - The interpolation value moves from 0 (after any 'delay') to 1 \n(after a further 'time'), whereby any 'onComplete' callback is fired. The function \n'isComplete()' will return true only after this point. The interpolation stays at 1 after \nthis point.\n* \"repeat\" - The interpolation value moves from 0 (after any 'delay') to 1 (after a further \n'time'), whereby any 'onComplete' callback is fired. The interpolation value then starts \nagain at 0, and moves to 1 after a further 'time'; i.e. it continually cycles with a \nperiod of 'time'. The 'onComplete' callback fires after each period. The function \n'isComplete()' NEVER returns true.\n* \"mirror\" - Like \"repeat\", except that the interpolation value alternately ramps up to 1 \nand back down to 0 for each pair of cycles (as opposed to ramping to 1 and then \nimmediately jumping back to 0). For each odd-numbered cycle, the interpolation value \nis (1-r), where r is the value that would be generated from the corresponding \"repeat\" \nmode. The 'onComplete' callback fires after each period. The function 'isComplete()' \nNEVER returns true.\n* params.easing (function) - The tween easing function. Default value is ease.linear. Easing functions\nallow the properties to be animated in a non-linear fashion, for example to slow down at the start or\nend of the animation period. A full list of easing functions is provided above.\n* params.easingValue (number) - The tween easing value. Depending on the easing function being used,\nthis value can affect the 'strength' of the function, for example the degree to which the animation\nspeeds up or slows down at the start or end of the period. Default value depends on the easing function.\n* params.onStart (function or table) - A function or table listener called before the tween \nbegins. Table listeners must have an 'onStart' method. When invoked, the listener function is\npassed the tween's owning node as an input. The 'onStart' listener is called only once any 'delay'\nperiod has elapsed.\n* params.onComplete (function or table) - A function or table listener called after the \ntween completes. Table listeners must have an 'onComplete' method. When invoked, the listener function is\npassed the tween's owning node as an input. The 'onComplete' listener is called at the end of each\ntween 'cycle': if mode is 'clamp', there is only a single cycle, otherwise cycles repeat indefinitely with\nthe period specified by 'time'.\nExample::\nlocal mySprite = director:createSprite(0, 0, \"textures/beachball.png\")\n-- Animate x and alpha properties, over 1 second, after a delay of 0.5 seconds, with the \"powIn\" easing function\ntween:to(mySprite, { time=1, easing=ease.powIn, delay=0.5, x=100, alpha=0 } )\nDifferent 'modes' can be combined with different easing functions to create standard waves. \nFor example:\n* mode=\"repeat\", easing=ease.linear	-- sawtooth wave\n* mode=\"mirror\", easing=ease.linear	-- triangle wave\n* mode=\"mirror\", easing=ease.zero	-- square wave\nAny target value specified in the tween parameters must exist on the target object. Values can \nbe of type 'number' or 'table'. If of type 'table', the table must include values of type \n'number' and exist on the target object.\nExamples:\n* x=10 (type number)\n* xScale=2 (type number)\n* color={r=0} (type table, containing type number)\n* color={r=0, g=255} (type table, containing type number)\n* uvRect={x=1} (type table, containing type number)",
        args = "(target: object, params: table)",
        returns = "(object)",
      },
    },
  },
  vector = {
    type = "class",
    childs = {
      strokeAlpha = {
        type = "value",
        description = "The alpha value (opacity) to use for the the shape outline, in the range 0..1.\nThe default value is 1 (opaque).",
      },
      strokeColor = {
        type = "value",
        description = "The color to use if outlining the shape.\nThe default color is white.",
      },
      strokeWidth = {
        type = "value",
        description = "The width, in display coordinates, of the shape outline.\nIf 0, no outline is drawn. The default value is 1.",
      },
    },
    inherits = "node",
  },
  video = {
    type = "class",
    childs = {
      getSupportedVideoCodecsList = {
        type = "method",
        description = "Gets an array of codec names that are supported by the device.",
        args = "()",
        returns = "(table)",
      },
      getSupportedVideoCodecsTable = {
        type = "method",
        description = "Gets a table of allowed video codecs. For each (key, value) pair\nin the table, the key is a codec name (string), and the value is\na boolean which is true only if the codec is supported on the\ndevice.",
        args = "()",
        returns = "(table)",
      },
      getVideoPosition = {
        type = "method",
        description = "Gets the current video playback position, in milliseconds from the start.",
        args = "()",
        returns = "(number)",
      },
      getVideoState = {
        type = "method",
        description = "Returns a string decribing the current playback state. Possible values are\n\"playing\", \"stopped\", \"failed\" or \"paused\".",
        args = "()",
        returns = "(string)",
      },
      getVolume = {
        type = "method",
        description = "Gets the volume in the range (0..1), where 0 is silent\nand 1 is the maximum volume.",
        args = "()",
        returns = "(number)",
      },
      isVideoCodecSupported = {
        type = "method",
        description = "Checks if a video codec is supported.",
        args = "(codecname: string)",
        returns = "(boolean)",
      },
      isVideoPlaying = {
        type = "method",
        description = "",
        args = "()",
        returns = "(boolean)",
      },
      pauseVideo = {
        type = "method",
        description = "Pause any currently-playing video.",
        args = "()",
        returns = "()",
      },
      playVideo = {
        type = "method",
        description = "Play a video file, into a rectangular display region, perhaps specifying a number of times to repeat as a loop.",
        args = "(filename: string [, repeats: number] [, x: number] [, y: number] [, w: number] [, h: number])",
        returns = "(boolean)",
      },
      resumeVideo = {
        type = "method",
        description = "Resume any video that was previously paused.",
        args = "()",
        returns = "(boolean)",
      },
      setVolume = {
        type = "method",
        description = "Sets the video volume in the range (0..1), where 0 is silent\nand 1 is the maximum volume.",
        args = "(volume: number)",
        returns = "(boolean)",
      },
      stopVideo = {
        type = "method",
        description = "Stop any currently-playing video.",
        args = "()",
        returns = "()",
      },
      volumeDown = {
        type = "method",
        description = "Decreases the volume of the current video by few units.",
        args = "()",
        returns = "(boolean)",
      },
      volumeUp = {
        type = "method",
        description = "Increases the volume of the current video by few units.",
        args = "()",
        returns = "(boolean)",
      },
    },
  },
  webview = {
    type = "class",
    childs = {
      destroy = {
        type = "method",
        description = "Destroys the web view.",
        args = "()",
        returns = "()",
      },
      h = {
        type = "value",
        description = "The height of the web view (in pixels). The default value is the current display height.",
      },
      isVisible = {
        type = "value",
        description = "The visible state of the web view. Default is true.",
      },
      url = {
        type = "value",
        description = "The URL to navigate to.",
      },
      w = {
        type = "value",
        description = "The width of the web view (in pixels). The default value is the current display width.",
      },
      x = {
        type = "value",
        description = "The x coordinate (in pixels) of the top-left of the web view. The default value is 0.",
      },
      y = {
        type = "value",
        description = "The y coordinate (in pixels) of the top-left of the web view. The default value is 0.",
      },
    },
  },
}
