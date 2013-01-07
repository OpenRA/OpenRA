-- author: Srdjan Marković

return {
  ads = {
    type = "library",
    description = "The Corona Advertising library.",
    childs = {
       hide = {
        type = "function",
        description = "Removes the currently shown ad from the screen and prevents new ads from being retrieved until ads.show() has been called again.",
        args = '',
        returns = ''
      },
      init = {
        type = "function",
        description = "Initialize the ads service library by specifying the name of the Ad network service provider and the application identifier.",
        args = '',
        returns = ''
      },
      show = {
        type = "function",
        description = "Begin showing the ads at the given screen location and the given refresh period.",
        args = '',
        returns = ''
      },
    },
  },
  analytics = {
    type = "library",
    description = "The Corona analytics library lets you easily log interesting events in your application.",
    childs = {
      init = {
        type = "function",
        description = "Initializes the analytics library.",
        args = '',
        returns = ''
      },
      logEvent = {
        type = "function",
        description = "Reports a custom event defined by a string value.",
        args = '',
        returns = ''
      },
    },
  },
  audio = {
    type = "library",
    description = "The Corona Audio system gives you access to advanced OpenAL features.",
    childs = {
      dispose = {
        type = "function",
        description = "Releases audio memory associated with the handle.",
        args = '( audioHandle )',
        returns = ''
      },
       fade = {
        type = "function",
        description = "Fades a playing sound in a specified amount to a specified volume.",
        args = '( [ { [channel=c: Number] [, time=t: Number] [, volume=v: Number ] } ] )',
        returns = 'Number'
      },
      fadeOut = {
        type = "function",
        description = "Stops a playing sound in a specified amount of time and fades to min volume while doing it.",
        args = '( [ { [ channel=c: Number ] [ , time=t: Number ] } ] )',
        returns = 'Number'
      },
      findFreeChannel = {
        type = "function",
        description = "Will look for an available channel for playback.",
        args = '( [ startChannel: Number ] )',
        returns = 'Number'
      },
      getDuration = {
        type = "function",
        description = "Returns the total time in milliseconds of the audio resource. If the total length cannot be determined, -1 will be returned.",
        args = '( audioHandle )',
        returns = 'Number'
      },
      getMaxVolume = {
        type = "function",
        description = "Gets the max volume for a specific channel. NOTE: There is no max volume for the master volume.",
        args = '( { channel=c: Number } )',
        returns = 'Number'
      },
      getMinVolume = {
        type = "function",
        description = "Gets the min volume for a specific channel. NOTE: There is no min volume for the master volume.",
        args = '( { channel=c: Number } )',
        returns = 'Number'
      },
      getVolume = {
        type = "function",
        description = "Gets the volume either for a specific channel or gets the master volume.",
        args = '( [ { channel=c: Number } ] )',
        returns = 'Number'
      },
      isChannelActive = {
        type = "function",
        description = "Returns true if the specified channel is currently playing or paused; false if otherwise.",
        args = '( channel=c: Number )',
        returns = 'Boolean'
      },
      isChannelPaused = {
        type = "function",
        description = "Returns true if the specified channel is currently paused; false if not.",
        args = '( channel=c: Number )',
        returns = 'Boolean'
      },
      isChannelPlaying = {
        type = "function",
        description = "Returns true if the specified channel is currently playing; false if otherwise.",
        args = '( channel=c: Number )',
        returns = 'Boolean'
      },
      loadSound = {
        type = "function",
        description = "Loads an entire file completely into memory and returns a reference to the audio data.",
        args = '( audiofileName: String [, baseDir: Constant ] )',
        returns = 'Object: audio handle'
      },
      loadStream = {
        type = "function",
        description = "Loads (opens) a file to be read as a stream.",
        args = '( audiofileName: String [, baseDir: Constant ] )',
        returns = 'Object: audio handle'
      },
      pause = {
        type = "function",
        description = "Pauses playback on a channel (or all channels if no channels are specified). Has no effect on channels that aren't playing.",
        args = '( [ channel: Number ] )',
        returns = 'Number'
      },
      play = {
        type = "function",
        description = "Plays the audio specified by the audio handle on a channel.",
        args = '( audioHandle: Object [, options: Table ] )',
        returns = 'Number'
      },
      reserveChannels = {
        type = "function",
        description = "Allows you to reserve a certain number of channels so they won't be automatically assigned to play on.",
        args = '( channels: Number )',
        returns = 'Number'
      },
      resume = {
        type = "function",
        description = "Resumes playback on a channel that is paused (or all channels if no channel is specified).",
        args = '( [ channel: Number ] )',
        returns = 'Number'
      },
      rewind = {
        type = "function",
        description = "Rewinds audio to the beginning position on either an active channel or directly on the audio handle (rewinds all channels if no arguments are specified).",
        args = '( [, audioHandle: Object ] [, options: Table ] )',
        returns = 'Boolean'
      },
      seek = {
        type = "function",
        description = "Seeks to a time position on either an active channel or directly on the audio handle.",
        args = '( time: Number [, audioHandle: Object ] [, options: Table ] )',
        returns = 'Boolean'
      },
      setMaxVolume = {
        type = "function",
        description = "Clamps the max volume to the set value.",
        args = '( volume: Number, options: Table )',
        returns = 'Boolean'
      },
      setMinVolume = {
        type = "function",
        description = "Clamps the min volume to the set value.",
        args = '( volume: Number, options: Table )',
        returns = 'Boolean'
      },
      setVolume = {
        type = "function",
        description = "Sets the volume either for a specific channel or sets the master volume.",
        args = '( volume: Number [, options: Table ] )',
        returns = 'Boolean'
      },
      stop = {
        type = "function",
        description = "Stops playback on a channel and clears the channel so it can be played on again (or all channels if no channel is specified).",
        args = '( [ channel: Number ] )',
        returns = 'Number'
      },
      stopWithDelay = {
        type = "function",
        description = "Stops the playing a currently playing sound at the specified amount of time.",
        args = '( duration: Number [, options: Table ] )',
        returns = 'Number'
      },
    },
  },
  crypto = {
    type = "library",
    description = "Corona provides routines for calculating common message digests (hashes) and hash-based message authentication codes (HMAC).",
    childs = {
       digest = {
        type = "function",
        description = "Generates the message digest (the hash) of the input string.",
        args = '( algorithm: Constant, data: String [, raw: Boolean] )',
        returns = '[TYPE][api.type.TYPE]'
      },
      hmac = {
        type = "function",
        description = "Computes HMAC (Key-Hashing for Message Authentication Code) of the string and returns it.",
        args = '( algorithm: Constant, data: String, key: String [, raw: Boolean] )',
        returns = '[TYPE][api.type.TYPE]'
      },
      md4 = {
        type = "Constant",
        description = "Constant used to specify the MD4 algorithm (Message-Digest algorithm 4).",
        args = '',
        returns = ''
      },
      md5 = {
        type = "Constant",
        description = "Constant used to specify the MD5 algorithm (Message-Digest algorithm 5).",
        args = '',
        returns = ''
      },
      sha1 = {
        type = "Constant",
        description = "Constant used to specify the SHA-1 algorithm.",
        args = '',
        returns = ''
      },
      sha224 = {
        type = "Constant",
        description = "Constant used to specify the SHA-224 algorithm.",
        args = '',
        returns = ''
      },
      sha256 = {
        type = "Constant",
        description = "Constant used to specify the SHA-256 algorithm.",
        args = '',
        returns = ''
      },
      sha384 = {
        type = "Constant",
        description = "Constant used to specify the SHA-384 algorithm.",
        args = '',
        returns = ''
      },
      sha512 = {
        type = "Constant",
        description = "Constant used to specify the SHA-512 algorithm.",
        args = '',
        returns = ''
      },
    },
  },
  display = {
    type = "library",
    description = "Display library",
    childs = {
      capture = {
        type = "function",
        description = "This function is the same as display.save(), but it returns a display object instead of saving to a file by default.",
        args = '( DisplayObject [, saveToPhotoLibraryFlag: Boolean ] )',
        returns = '( DisplayObject )'
      },
      captureBounds = {
        type = "function",
        description = "Captures a portion of the screen and returns it as a new DisplayObject positioned at the top-left corner of the screen.",
        args = '( screenBounds: Table [, saveToAlbum: Boolean ] )',
        returns = '( DisplayObject )'
      },
      captureScreen = {
        type = "function",
        description = "Captures the contents of the screen and returns it as a new DisplayObject positioned so that the top-left of the screen is at the origin.",
        args = '(saveToAlbum: Boolean)',
        returns = '( DisplayObject )'
      },
      getCurrentStage = {
        type = "function",
        description = "Returns a reference to the current stage object, which is the root group for all display objects and groups.",
        args = '',
        returns = '( DisplayObject: Group )'
      },
      loadRemoteImage = {
        type = "function",
        description = "This a convenience method, similar to network.download(), which returns a DisplayObject containing the image, as well as saving the image to a file.",
        args = '( url: String, method: String, listener: ListenerFunc [, params: Table], destFilename: String [, baseDir: Constant] [, x: Number, y: Number] )',
        returns = '( DisplayObject: Group )'
      },
      newCircle = {
        type = "function",
        description = "Creates a circle with radius radius centered at specified coordinates (xCenter, yCenter).",
        args = '( [parentGroup: Group,] xCenter: Number, yCenter: Number, radius: Number )',
        returns = '( DisplayObject: circle/vector )'
      },
      newEmbossedText = {
        type = "function",
        description = "Creates text with an embossed (inset) effect.",
        args = '( [parentGroup: Group,] string: String, left: Number, top: Number, [width: Number, height: Number,] font: String, size: Number)',
        returns = '( DisplayObject: Group )'
      },
      newGroup = {
        type = "function",
        description = "Creates a group in which you can add and remove child display objects.",
        args = '',
        returns = '( GroupObject )'
      },
      newImage = {
        type = "function",
        description = "Displays an image on the screen from a file.",
        args = '( [parentGroup: Group,] filename: String [,baseDirectory: Constant] [,left: Number,top: Number] [,isFullResolution: Boolean])',
        returns = '( DisplayObject )'
      },
      newImageRect = {
        type = "function",
        description = "Displays an image on the screen from a file. NOTE: For SpriteSheet-based images use: display.newImageRect( [parentGroup,] imageSheet, frameIndex, width, height )",
        args = '( [parentGroup: Group,] filename: String, [baseDirectory: Constant] width: Number, height: Number )',
        returns = '( DisplayObject )'
      },
      newLine = {
        type = "function",
        description = "Draw a line from one point to another. Optionally, append points to the end of the line.",
        args = '( [parent: Group,] x1: Number, y1: Number, x2: Number, y2: Number )',
        returns = '( DisplayObject: line/vector )'
      },
      newRect = {
        type = "function",
        description = "Creates a rectangle vector DisplayObject with the top-left corner position specified by left and top arguments.",
        args = '( [parent: Group,] left: Number, top: Number, width: Number, height: Number )',
        returns = '( DisplayObject: rect/vector )'
      },
      newRoundedRect = {
        type = "function",
        description = "Creates a rounded rectangle vector DisplayObject with the top-left corner position specified by left and top arguments.",
        args = '( [parent: Group,] left: Number, top: Number, width: Number, height: Number, cornerRadius: Number )',
        returns = '( DisplayObject: rect/vector )'
      },
      newSprite = {
        type = "function",
        description = "Creates a sprite.",
        args = '( [parent: Group,] imageSheet: ImageSheet, sequenceData: Table )',
        returns = '( spriteObject )'
      },
      newText = {
        type = "function",
        description = "Creates a text object with its top-left corner at (left, top).",
        args = '( [parent: Group,] string: String, left: Number, top: Number, [width: Number, height: Number,] font: String, size: Number )',
        returns = '( DisplayObject: text/vector )'
      },
      remove = {
        type = "function",
        description = "Removes a group or object if not nil.",
        args = '( Object: DisplayObject )',
        returns = ''
      },
      save = {
        type = "function",
        description = "Renders the DisplayObject referenced by first argument into a JPEG image and saves it as a new file.",
        args = '( DisplayObject, filename: String [, baseDirectory: Constant] )',
        returns = ''
      },
      setDefault = {
        type = "function",
        description = "Set default colors for display objects. Colors default to white if not set.",
        args = '( key: String, r: Number, g: Number, b: Number )',
        returns = ''
      },
      setStatusBar = {
        type = "function",
        description = "Hides or changes the appearance of the status bar on certain devices.",
        args = '( mode: Constant )',
        returns = ''
      },
      contentWidth = {
        type = "number",
        description = "A read-only property representing the original width of the content in pixels.",
        args = '',
        returns = ''
      },
      contentHeight = {
        type = "number",
        description = "A read-only property representing the original height of the content in pixels.",
        args = '',
        returns = ''
      },
      viewableContentWidth = {
        type = "number",
        description = "A read-only property that contains the width of the viewable screen area in content coordinates.",
        args = '',
        returns = ''
      },
      viewableContentHeight = {
        type = "number",
        description = "A read-only property that contains the height of the viewable screen area in content coordinates.",
        args = '',
        returns = ''
      },
      statusBarHeight = {
        type = "number",
        description = "A read-only property representing the height of the status bar in content pixels on iOS devices.",
        args = '',
        returns = ''
      },
      fps = {
        type = "number",
        description = "Current framerate of the running application.",
        args = '',
        returns = ''
      },
      currentStage = {
        type = "stageObject",
        description = "A reference to the current stage object, which is the root group for all display objects and groups.",
        args = '',
        returns = ''
      },
      screenOriginX = {
        type = "number",
        description = "Returns the x-distance from the left of the reference screen to the left of the current screen, in reference screen units.",
        args = '',
        returns = ''
      },
      screenOriginY = {
        type = "number",
        description = "Returns the y-distance from the top of the reference screen to the top of the current screen, in reference screen units.",
        args = '',
        returns = ''
      },
      contentScaleX = {
        type = "number",
        description = "The ratio between content pixel and screen pixel width.",
        args = '',
        returns = ''
      },
      contentScaleY = {
        type = "number",
        description = "The ratio between content pixel and screen pixel height.",
        args = '',
        returns = ''
      },
      contentCenterX = {
        type = "number",
        description = "Equivalent to display.contentWidth * 0.5.",
        args = '',
        returns = ''
      },
      contentCenterY = {
        type = "number",
        description = "Equivalent to display.contentHeight * 0.5.",
        args = '',
        returns = ''
      },
    },
  },
  easing = {
    type = "library",
    description = "Easing functions provide a simple way of interpolating between two values to achieve smooth animations.",
    childs = {
       inExpo = {
        type = "function",
        description = "Starts animation from zero velocity and then accelerates motion as it executes.",
        args = '',
        returns = ''
      },
      inOutExpo = {
        type = "function",
        description = "Starts animation from zero velocity, accelerates, then decelerate to a zero velocity.",
        args = '',
        returns = ''
      },
      inOutQuad = {
        type = "function",
        description = "Starts animation from zero velocity, accelerates, then decelerate to a zero velocity.",
        args = '',
        returns = ''
      },
      inQuad = {
        type = "function",
        description = "Starts animation from zero velocity and then accelerates motion as it executes.",
        args = '',
        returns = ''
      },
      linear = {
        type = "function",
        description = "Performs linear interpolation. This is the default.",
        args = '',
        returns = ''
      },
      outExpo = {
        type = "function",
        description = "Starts animation at a high velocity and decelerates towards zero.",
        args = '',
        returns = ''
      },
      outQuad = {
        type = "function",
        description = "Starts animation at a high velocity and decelerates towards zero.",
        args = '',
        returns = ''
      },
    },
  },
  facebook = {
    type = "library",
    description = "The facebook library provides access to Facebook Connect, a set of web API's for accessing the Facebook social network.",
    childs = {
       login = {
        type = "function",
        description = "Prompts the user to login to Facebook.",
        args = '( appId: String, listener: ListenerFunc [, permissions: Array ] )',
        returns = ''
      },
      logout = {
        type = "function",
        description = "Logs the application out of the user's Facebook session.",
        args = '',
        returns = '[TYPE][api.type.TYPE]'
      },
      request = {
        type = "function",
        description = "Get or post data to the logged in Facebook account.",
        args = '( path: String [, httpMethod: String, params: Table ] )',
        returns = '[TYPE][api.type.TYPE]'
      },
      showDialog = {
        type = "function",
        description = "Displays a Facebook UI dialog for publishing posts to a user's stream using only a few lines of code.",
        args = '( action: String [, params: Table ] )',
        returns = '[TYPE][api.type.TYPE]'
      },
    },
  },
  gameNetwork = {
    type = "library",
    description = "Game Network allows access to 3rd party libraries that enables social gaming features such as public leaderboards and achievements.",
    childs = {
       init = {
        type = "function",
        description = "Initializes an app with the parameters (e.g., product key, secret, display name, etc.) required by the game net work provider.",
        args = '( providerName: String [, params ...] )',
        returns = ''
      },
      request = {
        type = "function",
        description = "Send or request information to/from the game network provider.",
        args = '( command: String [, params ...] )',
        returns = ''
      },
      show = {
        type = "function",
        description = "Shows (displays) information from game network provider on the screen.",
        args = '( name: String [, data: String ] )',
        returns = '[TYPE][api.type.TYPE]'
      },
    },
  },
  graphics = {
    type = "library",
    description = "",
    childs = {
       newGradient = {
        type = "function",
        description = "Creates a gradient object that adds horizontal/vertical linear gradients to rectangle and text objects.",
        args = '( color1: Table, color2: Table [ , direction: String ] )',
        returns = 'Object: gradient'
      },
      newImageSheet = {
        type = "function",
        description = "ImageSheet objects allow you to load multiple graphics from a single image file (also known as a spritesheet).",
        args = '( filename: String, [ baseDir: Constant, ] options: Table )',
        returns = 'ImageSheet'
      },
      newMask = {
        type = "function",
        description = "Creates a bit mask from an image file.",
        args = '( filename: String [, baseDir: Constant ] )',
        returns = 'Mask'
      },
    },
  },
  io = {
    type = "library",
    description = "Standard Lua library to create, write, and read files.",
    childs = {
       close = {
        type = "function",
        description = "Closes an open file handle.",
        args = '( [ file: Object ] )',
        returns = ''
      },
      flush = {
        type = "function",
        description = "Flushes the default output file.",
        args = '',
        returns = ''
      },
      input = {
        type = "function",
        description = "Sets the standard input file.",
        args = '( [ file: String or object ] )',
        returns = 'Object: file handle'
      },
      lines = {
        type = "function",
        description = "Opens the given file name in read mode and returns an iterator function that, each time it is called, returns a new line from the file.",
        args = '( filename: String )',
        returns = 'function: iterator'
      },
      open = {
        type = "function",
        description = "This function opens a file for reading or writing, in the string (default) or binary mode.",
        args = '( filename_path: String, [, mode: String ] )',
        returns = 'Object: file handle'
      },
      output = {
        type = "function",
        description = "Sets the standard output file.",
        args = '( [ file: String or object ] )',
        returns = 'Object: file handle'
      },
      read = {
        type = "function",
        description = "Reads the file set by io.input(), according to the given formats, which specify what to read.",
        args = '( [ fmt1: String ] [, fmt2: String ] [, ...] )',
        returns = 'String, Number, or nil'
      },
      tmpfile = {
        type = "function",
        description = "Opens a temporary file for reading and writing and returns a handle to it.",
        args = '',
        returns = 'Object: file handle'
      },
      type = {
        type = "function",
        description = "Checks whether obj is a valid file handle.",
        args = '( obj: Object )',
        returns = 'String'
      },
      write = {
        type = "function",
        description = "Writes the value of each of its arguments to the file.",
        args = '( arg1: String or number [, arg2: String or Number ] [, ...] )',
        returns = ''
      },
    },
  },
  json = {
    type = "library",
    description = "The json library allows you serialize and deserialize Lua tables into JSON (JavaScript Object Notation) and vice-versa.",
    childs = {
       decode = {
        type = "function",
        description = "Decodes the JSON encoded data structure, and returns a Lua object (table) with the appropriate data.",
        args = '( data: String )',
        returns = 'Table'
      },
      encode = {
        type = "function",
        description = "Returns the Lua object (table) as a JSON-encoded string.",
        args = '( t: Table )',
        returns = 'String'
      },
      null = {
        type = "function",
        description = "Returns a unique value that will be encoded as a null in a JSON encoding.",
        args = '',
        returns = 'Constant'
      },
    },
  },
  media = {
    type = "library",
    description = "The media library provides access to the multimedia features of the device: audio, video, camera and photo library.",
    RemoteSource = {
       decode = {
        type = "Constant",
        description = "Used to indicate that a file path is to be interpreted as a url to a remote server.",
        args = '',
        returns = ''
      },
      newEventSound = {
        type = "function",
        description = "Loads the event sound (1-3 seconds) from a sound file and returns an event sound id that can be passed to media.playEventSound().",
        args = '( filename: String [, baseDir: Constant ] )',
        returns = '[Reference][api.type.Reference]'
      },
      newRecording = {
        type = "function",
        description = "Create an object for audio recording.",
        args = '( [ path: String ] )',
        returns = 'recording'
      },
      pauseSound = {
        type = "function",
        description = "Pauses playback of the extended sound currently opened by the previous call to media.playSound().",
        args = '',
        returns = '[TYPE][api.type.TYPE]'
      },
      playEventSound = {
        type = "function",
        description = "Plays an event sound (1-3 seconds).",
        args = '( sound: String [, baseDir: Constant ] [, completionListener: function ] )',
        returns = ''
      },
      playSound = {
        type = "function",
        description = 'Plays an extended sound (as opposed to an "event sound", 1-3 seconds duration), or resumes play of a paused extended sound. You can only have one such sound file open at a time.',
        args = '( soundfile: String [, baseDir: Constant ] [, onComplete: function | loop: Boolean] )',
        returns = ''
      },
      playVideo = {
        type = "function",
        description = "Plays the video at the specified path (both local and remote) in a device-specific popup Video media player.",
        args = '( path: String [, baseSource: Constant ], showControls: Boolean, listener: ListenerFunc )',
        returns = ''
      },
      save = {
        type = "function",
        description = "Adds specified file to photo library.",
        args = '( filename: String [, baseDir: Constant ] )',
        returns = ''
      },
      setSoundVolume = {
        type = "function",
        description = "Adjusts the playback volume of an extended sound (media.playSound()).",
        args = '( vol: Number )',
        returns = '[TYPE][api.type.TYPE]'
      },
      show = {
        type = "function",
        description = "Opens a platform-specific interface to the device's camera or photo library.",
        args = '( mediaSource: Constant [, listener: ListenerFunc ] [, file: Table ] )',
        returns = ''
      },
      stopSound = {
        type = "function",
        description = "Stops playback of the extended sound currently opened by the previous call to media.playSound().",
        args = '',
        returns = ''
      },
    },
  },
  native = {
    type = "library",
    description = "The native library wraps platform-specific UI elements. These are rendered by the OS, not the Corona engine.",
    RemoteSource = {
      cancelAlert = {
        type = "function",
        description = "Dismisses an alert box programmatically.",
        args = '( alert: [Reference][api.type.Reference] )',
        returns = ''
      },
      cancelWebPopup = {
        type = "function",
        description = "Dismisses the currently displaying web pop-up.",
        args = '',
        returns = ''
      },
      getFontNames = {
        type = "function",
        description = "Returns an array of the available native fonts.",
        args = '',
        returns = 'array'
      },
      getProperty = {
        type = "function",
        description = "Gets the value of a platform-specific property.",
        args = '( key: String )',
        returns = 'number or string'
      },
      newFont = {
        type = "function",
        description = "Creates a font object that you can use to specify fonts in native text fields and text boxes.",
        args = '( name:string [, size: Number ] )',
        returns = '[Reference][api.type.Reference]'
      },
      newMapView = {
        type = "function",
        description = "Renders a MapView within the specified boundaries and returns a display object wrapper.",
        args = '( left: Number, top: Number, width: Number, height: Number )',
        returns = 'map'
      },
      newTextBox = {
        type = "function",
        description = "Creates a scrollable, multi-line text box for displaying text-based content.",
        args = '( left: Number, top: Number, width: Number, height: Number [, listener: ListenerFunc ] )',
        returns = 'textBox'
      },
      newTextField = {
        type = "function",
        description = "Native textfields are only available in device builds, the Xcode Simulator, and in the Corona Mac Simulator.",
        args = '( left: Number, top: Number, width: Number, height: Number [, listener: ListenerFunc ] )',
        returns = 'textField'
      },
      newVideo = {
        type = "function",
        description = "Returns a video object that can be moved and rotated.",
        args = '( left: Number, top: Number, width: Number, height: Number )',
        returns = 'video'
      },
      newWebView = {
        type = "function",
        description = "Loads a remote web page in a webView container.",
        args = '( left: Number, top: Number, width: Number, height: Number )',
        returns = 'webView'
      },
      requestExit = {
        type = "function",
        description = "Closes the application window on Android gracefully without terminating the process.",
        args = '',
        returns = ''
      },
      setActivityIndicator = {
        type = "function",
        description = "Displays or hides a platform-specific activity indicator.",
        args = '( state: Boolean )',
        returns = ''
      },
      setKeyboardFocus = {
        type = "function",
        description = "Sets keyboard focus on a textField and (where appropriate) shows or hides the keyboard. Pass nil to remove focus and dismiss (hide) the keyboard.",
        args = '( textField: Object )',
        returns = ''
      },
      setProperty = {
        type = "function",
        description = "Sets a platform specific property.",
        args = '( key: String, value: Number or string )',
        returns = ''
      },
      showAlert = {
        type = "function",
        description = "Displays a popup alert box with one or more buttons, using a native alert control.",
        args = '( title: String, message: String [, { buttonLabels: Table } [, listener: ListenerFunc ] ] )',
        returns = 'Object: id of alert'
      },
      showPopup = {
        type = "function",
        description = "Displays the operating system's default popup window for a specified service.",
        args = '( name: String [, options: Table ] )',
        returns = 'Boolean'
      },
      showWebPopup = {
        type = "function",
        description = "Creates a web popup that loads a local or remote web page.",
        args = '( [ x: Number, y: Number, width: Number, heigh: Numbert, ] url: String [, options: Table] )',
        returns = ''
      },
    },
  },
  network = {
    type = "library",
    description = "",
    childs = {
      canDetectNetworkStatusChanges = {
        type = "bool",
        description = "Returns true if network status APIs are supported on the current platform.",
        args = '',
        returns = ''
      },
      download = {
        type = "function",
        description = "This API is similar to the asynchronous network.request() except that it downloads the response to a local file that you specify, rather than cacheing it in memory.",
        args = '( url: String, method: String, listener: ListenerFunc [, params: Table], destFilename: String [, baseDir: Constant ] )',
        returns = ''
      },
      request = {
        type = "function",
        description = "Makes an asynchronous HTTP or HTTPS request to a URL.",
        args = '( url: String, method: String, listener: ListenerFunc [, params: Table] )',
        returns = ''
      },
      setStatusListener = {
        type = "function",
        description = "Starts monitoring a host for its network reachability status.",
        args = '( hostURL: String, listener: ListenerFunc )',
        returns = ''
      },
    },
  },
  os = {
    type = "library",
    description = "This standard Lua library provides functions for dealing with system time and date and other OS-related functions.",
    childs = {
      clock = {
        type = "function",
        description = "Returns an approximation of the amount in seconds of CPU time used by the program.",
        args = '',
        returns = 'Number'
      },
      date = {
        type = "function",
        description = "Returns a string or a table containing date and time, formatted according to the given string format.",
        args = '( [format: String [, time: Number ] ] )',
        returns = 'String or Table'
      },
      difftime = {
        type = "function",
        description = "Returns the number of seconds from time t1 to time t2. In POSIX, Windows, and some other systems, this value is exactly t2-t1.",
        args = '( t1: Number, t2: Number )',
        returns = 'Number'
      },
      execute = {
        type = "function",
        description = "Passes a string to the operating system for execution and returns a system-dependent status code.",
        args = '( cmd: String )',
        returns = 'Number'
      },
      exit = {
        type = "function",
        description = "Calls the C function exit(), with an optional code, to terminate the host program. The default value for code is the success code.",
        args = '( [ exit: Number ] )',
        returns = ''
      },
      remove = {
        type = "function",
        description = "Deletes a file or directory.",
        args = '( file: String )',
        returns = 'String and Boolean'
      },
      rename = {
        type = "function",
        description = "Renames a file or directory.",
        args = '( oldname: String, newname: String )',
        returns = 'String and Boolean'
      },
      time = {
        type = "function",
        description = "Returns the current time when called without arguments, or a time representing the date and time specified by the given table.",
        args = '( [ table ] )',
        returns = 'Number'
      },
    },
  },
  package = {
    type = "library",
    description = "Corona supports Lua's module functionality for creating and loading external libraries. You can create your own libraries and call them from your application.",
    childs = {
      module = {
        type = "function",
        description = "Deprecated!",
        args = '( name: String [, ...] )',
        returns = ''
      },
      require = {
        type = "function",
        description = "Loads the given module.",
        args = '( moduleName: String )',
        returns = 'library'
      },
      loaded = {
        type = "table",
        description = "A table used by requireto control which modules are already loaded.",
        args = '',
        returns = ''
      },
      loaders = {
        type = "table",
        description = "A table used by require to control how to load modules.",
        args = '',
        returns = ''
      },
      seeall = {
        type = "table",
        description = "Deprecated!",
        args = '',
        returns = ''
      },
    },
  },
  physics = {
    type = "library",
    description = "Corona's Physics library (Box2D)",
    childs = {
      addBody = {
        type = "function",
        description = "Allows you to turn any Corona display object into a simulated physical object with one line of code, including the assignment of physical properties.",
        args = '(Object: DisplayObject, [ bodyType: String,] { density = d: Number, friction = f: Number, bounce = b: Number [,radius = r: Number or shape = s: Array ] [,filter = f: Table ]})',
        returns = 'Boolean'
      },
      fromMKS = {
        type = "function",
        description = "Convenience function for converting from MKS units to Corona units.",
        args = '( unitName: String, value: Number )',
        returns = 'Number'
      },
      getGravity = {
        type = "function",
        description = "Returns the x,y components of the global gravity vector, in units of m/s2.",
        args = '',
        returns = 'Number'
      },
      getMKS = {
        type = "function",
        description = "Get the MKS value of the physics simulation for specific keys.",
        args = '( key: String )',
        returns = 'Number'
      },
      newJoint = {
        type = "function",
        description = "Used to assemble more complex game objects from multiple rigid bodes.",
        args = '( jointType: String, ... )',
        returns = 'Joint'
      },
      pause = {
        type = "function",
        description = "Pause the physics engine.",
        args = '',
        returns = ''
      },
      removeBody = {
        type = "function",
        description = "Removes a physics body from a display object without destroying the entire object.",
        args = '( Object: DisplayObject )',
        returns = 'Boolean'
      },
      setContinuous = {
        type = "function",
        description = "Set continuous collision detection.",
        args = '( enabled: Boolean )',
        returns = ''
      },
      setDrawMode = {
        type = "function",
        description = "Selects one of three possible rendering modes for the physics engine.",
        args = '( mode: String )',
        returns = ''
      },
      setGravity = {
        type = "function",
        description = "Sets the x,y components of the global gravity vector, in units of m/s2 (e.g. the horizontal and vertical 'pull' of gravity).",
        args = '( gx: Number, gy: Number )',
        returns = ''
      },
      setMKS = {
        type = "function",
        description = "Set the MKS (meters, kilograms, and seconds) value of the physics simulation for specific keys.",
        args = '( key: String, value: Number )',
        returns = '[TYPE][api.type.TYPE]'
      },
      setPositionIterations = {
        type = "function",
        description = "Sets the accuracy of the engine's position calculations. The default value is 8.",
        args = '( value: Number )',
        returns = '[TYPE][api.type.TYPE]'
      },
      setScale = {
        type = "function",
        description = "Sets the internal pixels-per-meter ratio that is used in converting between onscreen Corona coordinates and simulated physics coordinates.",
        args = '( value: Number )',
        returns = '[TYPE][api.type.TYPE]'
      },
      setTimeStep = {
        type = "function",
        description = "Set physics 'time step' to switch from frame-based to (approximate) time-based physics simulation and vice-versa.",
        args = '( dt: Number )',
        returns = ''
      },
      setVelocityIterations = {
        type = "function",
        description = "Sets the accuracy of the engine's velocity calculations. The default value is 3.",
        args = '( value: Number )',
        returns = ''
      },
      start = {
        type = "function",
        description = "This function start the physics simulation and should be called before any other physics functions.",
        args = '( noSleep: Boolean )',
        returns = ''
      },
      stop = {
        type = "function",
        description = "Stops the physics engine.",
        args = '()',
        returns = 'Boolean'
      },
      toMKS = {
        type = "function",
        description = "Convenience function for converting from Corona units to MKS units.",
        args = '( unitName: String, value: Number )',
        returns = 'Number'
      },
    },
  },
  store = {
    type = "library",
    description = "This feature allows you to support In-App Purchases.",
    childs = {
      canMakePurchases = {
        type = "function",
        description = "Check if iOS device settings allow purchases.",
        args = '',
        returns = 'Boolean'
      },
      finishTransaction = {
        type = "function",
        description = "Notifies the App Store that a transaction is complete.",
        args = '( transaction: [Reference][api.type.Reference] )',
        returns = ''
      },
      init = {
        type = "function",
        description = "Activates in-app purchases.",
        args = '( listener: ListenerFunc )',
        returns = '[TYPE][api.type.TYPE]'
      },
      loadProducts = {
        type = "function",
        description = "Retrieves information about items available for sale.",
        args = '( productIdentifiers: Array, listener: ListenerFunc )',
        returns = ''
      },
      purchase = {
        type = "function",
        description = "Initiates a purchase transaction on a provided list of products.",
        args = '( productList: Array )',
        returns = ''
      },
      restore = {
        type = "function",
        description = "Users who wipe the information on a device or buy a new device, may wish to restore previously purchased items without paying for them again.",
        args = '',
        returns = '[TYPE][api.type.TYPE]'
      },
    },
  },
  storyboard = {
    type = "library",
    description = "Storyboard is the officially supported, built-in solution to scene (e.g. 'screens') creation and management in Corona SDK.",
    childs = {
      disableAutoPurge = {
        type = "bool",
        description = "By default, storyboard will automatically purge (e.g. remove the scene's display group, while leaving the actual module in memory) the least recently used scene whenever the OS receives a low memory warning.",
        args = '',
        returns = 'Boolean'
      },
      isDebug = {
        type = "bool",
        description = "Toggles 'Storyboard Debug Mode', which will print useful debugging information to the Corona Terminal in certain situations if set to true.",
        args = '',
        returns = ''
      },
      purgeOnSceneChange = {
        type = "bool",
        description = "If set to true, whenever a scene change is completed, all scenes (except for the newly active scene) will be automatically purged.",
        args = '',
        returns = ''
      },
      stage = {
        type = "displayObject",
        description = "This is a reference to the top-level storyboard display group that all scene views are inserted into.",
        args = '',
        returns = 'DisplayObject'
      },
      getCurrentSceneName = {
        type = "function",
        description = "Returns the current scene name as a string, which can be used with storyboard.gotoScene(), storyboard.removeScene(), and storyboard.purgeScene() functions.",
        args = '',
        returns = 'String'
      },
      getPrevious = {
        type = "function",
        description = "Gets the name of the previously active scene and returns it as a string.",
        args = '',
        returns = 'String'
      },
      getScene = {
        type = "function",
        description = "Returns the specified scene object (as returned from storyboard.newScene()).",
        args = '( sceneName: String )',
        returns = 'Table'
      },
      gotoScene = {
        type = "function",
        description = "Used to transition to a specific scene.",
        args = '( sceneName: String [, options: Table ] )',
        returns = ''
      },
      hideOverlay = {
        type = "function",
        description = "This function will hide/remove the current overlay scene (if one is currently being displayed).",
        args = '( [ purgeOnly: Boolean, effect: String, effectTime: Number ] )',
        returns = ''
      },
      loadScene = {
        type = "function",
        description = "Loads specified scene, behind the currently active scene and hidden, without initiating a scene transition.",
        args = '( sceneName: String [, doNotLoadView: Boolean, params: Table ] )',
        returns = ''
      },
      newScene = {
        type = "function",
        description = "Used to create new scene objects to be used with the Storyboard API (see that page for a scene tempalate module).",
        args = '( [ sceneName: String ] )',
        returns = 'Table'
      },
      printMemUsage = {
        type = "function",
        description = "Will print Lua memory and texture memory usage information in the terminal, but only if storyboard.isDebug is set to true.",
        args = '',
        returns = ''
      },
      purgeAll = {
        type = "function",
        description = "Will purge all scenes (except for the one that is currently active).",
        args = '',
        returns = ''
      },
      purgeScene = {
        type = "function",
        description = "Unloads the specified scene's scene.view property, which is a group that contains all of the scene's display objects.",
        args = '( sceneName: String )',
        returns = ''
      },
      reloadScene = {
        type = "function",
        description = "Reloads the currently loaded scene.",
        args = '',
        returns = ''
      },
      removeAll = {
        type = "function",
        description = "Will purge and remove all scenes (except for the currently active scene).",
        args = '',
        returns = ''
      },
      removeScene = {
        type = "function",
        description = "Purges the specified scene, and then completely unloads the scene's associated module (if there is one).",
        args = '( sceneName: String )',
        returns = ''
      },
      showOverlay = {
        type = "function",
        description = "Load a scene above the currently active scene, leaving the currently active scene in-tact.",
        args = '( sceneName: String [, options: Table ] )',
        returns = ''
      },
    },
  },
  system = {
    type = "library",
    description = "The System functions return information about the system (get device information, current orientation, etc.) and control system functions (enabling Multitouch, controlling the idle time, Accelerometer, GPS, etc.)",
    childs = {
      CachesDirectory = {
        type = "Constant",
        description = "Used with system.pathForFile() to create a path for storing and retrieving files that are available across application launches.",
        args = '',
        returns = ''
      },
      DocumentsDirectory = {
        type = "Constant",
        description = "Used with system.pathForFile() to create a path for storing and retrieving files that need to persist between application sessions. The path is '/Documents'.",
        args = '',
        returns = ''
      },
      ResourceDirectory = {
        type = "Constant",
        description = "Used with system.pathForFile() to create a path for retrieving files where all the application assets exist (e.g., image and sound files). This often called the 'app bundle'.",
        args = '',
        returns = ''
      },
      TemporaryDirectory = {
        type = "Constant",
        description = "Used with system.pathForFile() to create a path for storing and retrieving files that only need to persist while the application is running. The path is '/tmp'.",
        args = '',
        returns = ''
      },
      activate = {
        type = "function",
        description = "Activates a system level feature, such as multitouch. Use system.deactivate() to disable a feature.",
        args = '( feature: String )',
        returns = ''
      },
      cancelNotification = {
        type = "function",
        description = "Removes the specified notification from the scheduler, status bar, or notification center.",
        args = '( [ notificationId ] )',
        returns = ''
      },
      deactivate = {
        type = "function",
        description = "Deactivates a system level feature, such as multitouch.",
        args = '',
        returns = ''
      },
      getInfo = {
        type = "function",
        description = "Returns information about the system on which the application is running.",
        args = '( property: String )',
        returns = 'Any'
      },
      getIdleTimer = {
        type = "function",
        description = "Returns whether the application idle timer is enabled.",
        args = '',
        returns = 'Boolean'
      },
      getPreference = {
        type = "function",
        description = "Returns a preference value as a string.",
        args = '( category: String, name: String )',
        returns = 'String'
      },
      getTimer = {
        type = "function",
        description = "Returns time in milliseconds since application launch.",
        args = '',
        returns = 'Number'
      },
      hasEventSource = {
        type = "function",
        description = "Returns whether the system delivers events corresponding to eventName.",
        args = '( eventName: String )',
        returns = 'Boolean'
      },
      openURL = {
        type = "function",
        description = "Open a web page in the browser; create an email; or call a phone number.",
        args = '( url: String )',
        returns = ''
      },
      pathForFile = {
        type = "function",
        description = "Generates an absolute path using system-defined directories as the base (see System-defined directories below).",
        args = '( filename: String [, baseDirectory: Constant ] )',
        returns = 'String'
      },
      scheduleNotification = {
        type = "function",
        description = "Schedule a local notification event to be delivered in the future.",
        args = '( secondsFromNow: Number [, options: Table ] )',
        returns = 'Object: notification ID'
      },
      setAccelerometerInterval = {
        type = "function",
        description = "Sets the frequency of accelerometer events.",
        args = '( frequency: Number )',
        returns = ''
      },
      setGyroscopeInterval = {
        type = "function",
        description = "Sets the frequency of gyroscope events in Hertz.",
        args = '( frequency: Number )',
        returns = ''
      },
      setIdleTimer = {
        type = "function",
        description = "Controls whether the idle timer is enabled.",
        args = '( enabled: Boolean )',
        returns = ''
      },
      setLocationAccuracy = {
        type = "function",
        description = "Sets the desired accuracy of location (GPS) events to distance in meters.",
        args = '( distance: Number )',
        returns = ''
      },
      setLocationThreshold = {
        type = "function",
        description = "Sets how much distance in meters must be travelled until the next location (GPS) event is sent.",
        args = '( distance: Number )',
        returns = ''
      },
      setTapDelay = {
        type = "function",
        description = "The delay time between when a tap is detected and when the tap event is delivered. By default, this time is 0.",
        args = '( delayTime: Number )',
        returns = ''
      },
      vibrate = {
        type = "function",
        description = "Vibrates the phone. On the Corona simulator this will sound a system beep.",
        args = '',
        returns = ''
      },
    },
  },
  timer = {
    type = "library",
    description = "Timer functions allow calling a function some time in the future rather than immediately.",
    childs = {
      cancel = {
        type = "function",
        description = "Cancels a timer operation initiated with timer.performWithDelay(). This function returns two numbers: time remaining and number of iterations that were left.",
        args = '( timerId: Object )',
        returns = 'Numbers'
      },
      pause = {
        type = "function",
        description = "Pauses a timer started with timer.performWithDelay(). It returns a Number that represents the amount of time remaining in the timer.",
        args = '( timerId: Object )',
        returns = 'Number'
      },
      performWithDelay = {
        type = "function",
        description = "Call a specified function after a delay. This function returns an Object that can be used with other timer.* functions.",
        args = '( delay: Number, listener: ListenerFunc [, iterations: Number ] )',
        returns = 'Object'
      },
      resume = {
        type = "function",
        description = "Resumes a timer that was paused with timer.pause(). It returns a Number that represents the amount of time remaining in the timer.",
        args = '( timerId: Object )',
        returns = 'Number'
      },
    },
  },
  transition = {
    type = "library",
    description = "Transitions library.",
    childs = {
      cancel = {
        type = "function",
        description = "Cancels a timer operation initiated with timer.performWithDelay(). This function returns two numbers: time remaining and number of iterations that were left.",
        args = '( tweenReference: [Reference][api.type.Reference] )',
        returns = ''
      },
      dissolve = {
        type = "function",
        description = "Performs a dissolve transition between two images.",
        args = '( src: DisplayObject, dst: DisplayObject, duration: Number, delayDuration: Number )',
        returns = '[TYPE][api.type.TYPE]'
      },
      from = {
        type = "function",
        description = "Similar to transition.to() except the starting property values are specified in the function's parameter table and the final values are the corresponding property values in target prior to the call.",
        args = '( target: Table, params: Table )',
        returns = '[Reference][api.type.Reference]'
      },
      to = {
        type = "function",
        description = "Animates a DisplayObject's properties over time using easing transitions. Use this to move, rotate, fade, etc. an object over a specific period of time.",
        args = '( target: Table, params: Table )',
        returns = '[Reference][api.type.Reference]'
      },
    },
  },
  widget = {
    type = "library",
    description = "Widgets library.",
    childs = {
      newButton = {
        type = "function",
        description = "Creates a ButtonWidget object that supports onPress, onRelease, onDrag events (or an onEvent to handle all phases using a single listener).",
        args = '( options: Table )',
        returns = 'ButtonWidget'
      },
      newPickerWheel = {
        type = "function",
        description = "The pickerWheel widget supports custom columns and the ability to extract values from column rows that are within the 'selection area'.",
        args = '( [ options: Table ] )',
        returns = 'PickerWidget'
      },
      newScrollView = {
        type = "function",
        description = "This function allows you to create scrolling content areas via the ScrollViewWidget.",
        args = '( [ options: Table ] )',
        returns = 'ScrollViewWidget'
      },
      newSegmentedControl = {
        type = "function",
        description = "Creates a customizable Segmented Control.",
        args = '( [ options: Table ] )',
        returns = 'SegmentedControlWidget'
      },
      newSegmentedControl = {
        type = "function",
        description = "Slider user-interface widget that allows you to slide a handle within a numerical range of 0-100.",
        args = '( [ options: Table ] )',
        returns = 'SliderWidget'
      },
      newSpinner = {
        type = "function",
        description = "Creates a customizable Spinner, otherwise known as an activity indicator.",
        args = '( [ options: Table ] )',
        returns = 'SpinnerWidget'
      },
      newStepper = {
        type = "function",
        description = "Creates a customizable Stepper.",
        args = '( [ options: Table ] )',
        returns = 'StepperWidget'
      },
      newSwitch = {
        type = "function",
        description = "Creates a customizable Switch.",
        args = '( [ options: Table ] )',
        returns = 'SwitchWidget'
      },
      newTabBar = {
        type = "function",
        description = "Creates a customizable bar with tab buttons.",
        args = '( [ options: Table ] )',
        returns = 'TabBarWidget'
      },
      newTableView = {
        type = "function",
        description = "This function allows you to create scrolling lists of data via the TableViewWidget.",
        args = '( [ options: Table ] )',
        returns = 'TableView'
      },
      setTheme = {
        type = "function",
        description = "Used to set the theme you want the widget library to use when creating widgets.",
        args = '( themeFile: String )',
        returns = ''
      },
    },
  },
}