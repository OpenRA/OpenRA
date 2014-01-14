-- Copyright 2011-12 Paul Kulchenko, ZeroBrane LLC

-- converted from love_api.lua (http://love2d.org/forums/viewtopic.php?f=3&t=1796&start=50#p158650)
-- (API for love 0.9.0; as of Jan 13, 2014)
-- the conversion script is at the bottom of this file

local love = {
 childs = {
  audio = {
   childs = {
    DistanceModel = {
     childs = {
      exponent = {
       description = "Exponential attenuation.",
       type = "value"
      },
      ["exponent clamped"] = {
       description = "Exponential attenuation. Gain is clamped.",
       type = "value"
      },
      inverse = {
       description = "Inverse distance attenuation.",
       type = "value"
      },
      ["inverse clamped"] = {
       description = "Inverse distance attenuation. Gain is clamped.",
       type = "value"
      },
      linear = {
       description = "Linear attenuation.",
       type = "value"
      },
      ["linear clamped"] = {
       description = "Linear attenuation. Gain is clamped.",
       type = "value"
      },
      none = {
       description = "Sources do not get attenuated.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    Source = {
     childs = {
      getChannels = {
       args = "()",
       description = "Gets the number of channels in the Source. Only 1-channel (mono) Sources can use directional and positional effects.",
       returns = "(channels: number)",
       type = "function"
      },
      getCone = {
       args = "()",
       description = "Gets the Source's directional volume cones. Together with Source:setDirection, the cone angles allow for the Source's volume to vary depending on its direction.",
       returns = "(innerAngle: number, outerAngle: number, outerVolume: number)",
       type = "function"
      },
      getDirection = {
       args = "()",
       description = "Gets the direction of the Source.",
       returns = "(x: number, y: number, z: number)",
       type = "function"
      },
      getPitch = {
       args = "()",
       description = "Gets the current pitch of the Source.",
       returns = "(pitch: number)",
       type = "function"
      },
      getPosition = {
       args = "()",
       description = "Gets the position of the Source.",
       returns = "(x: number, y: number, z: number)",
       type = "function"
      },
      getRolloff = {
       args = "()",
       description = "Returns the rolloff factor of the source.",
       returns = "(rolloff: number)",
       type = "function"
      },
      getVelocity = {
       args = "()",
       description = "Gets the velocity of the Source.",
       returns = "(x: number, y: number, z: number)",
       type = "function"
      },
      getVolume = {
       args = "()",
       description = "Gets the current volume of the Source.",
       returns = "(volume: number)",
       type = "function"
      },
      getVolumeLimits = {
       args = "()",
       description = "Returns the volume limits of the source.",
       returns = "(min: number, max: number)",
       type = "function"
      },
      isLooping = {
       args = "()",
       description = "Returns whether the Source will loop.",
       returns = "(loop: boolean)",
       type = "function"
      },
      isPaused = {
       args = "()",
       description = "Returns whether the Source is paused.",
       returns = "(paused: boolean)",
       type = "function"
      },
      isPlaying = {
       args = "()",
       description = "Returns whether the Source is playing.",
       returns = "(playing: boolean)",
       type = "function"
      },
      isStatic = {
       args = "()",
       description = "Returns whether the Source is static.\n\nSee SourceType.static definition to have more informations.",
       returns = "(static: boolean)",
       type = "function"
      },
      isStopped = {
       args = "()",
       description = "Returns whether the Source is stopped.",
       returns = "(stopped: boolean)",
       type = "function"
      },
      pause = {
       args = "()",
       description = "Pauses the Source.",
       returns = "()",
       type = "function"
      },
      play = {
       args = "()",
       description = "Starts playing the Source.",
       returns = "()",
       type = "function"
      },
      resume = {
       args = "()",
       description = "Resumes a paused Source.",
       returns = "()",
       type = "function"
      },
      rewind = {
       args = "()",
       description = "Rewinds a Source.",
       returns = "()",
       type = "function"
      },
      seek = {
       args = "(position: number, unit: TimeUnit)",
       description = "Sets the playing position of the Source.",
       returns = "()",
       type = "function"
      },
      setAttenuationDistances = {
       args = "(ref: number, max: number)",
       description = "Sets the reference and maximum distance of the source.",
       returns = "()",
       type = "function"
      },
      setCone = {
       args = "(innerAngle: number, outerAngle: number, outerVolume: number)",
       description = "Sets the Source's directional volume cones. Together with Source:setDirection, the cone angles allow for the Source's volume to vary depending on its direction.",
       returns = "()",
       type = "function"
      },
      setDirection = {
       args = "(x: number, y: number, z: number)",
       description = "Sets the direction vector of the Source. A zero vector makes the source non-directional.",
       returns = "()",
       type = "function"
      },
      setLooping = {
       args = "(loop: boolean)",
       description = "Sets whether the Source should loop.",
       returns = "()",
       type = "function"
      },
      setPitch = {
       args = "(pitch: number)",
       description = "Sets the pitch of the Source.",
       returns = "()",
       type = "function"
      },
      setPosition = {
       args = "(x: number, y: number, z: number)",
       description = "Sets the position of the Source.",
       returns = "()",
       type = "function"
      },
      setRolloff = {
       args = "(rolloff: number)",
       description = "Sets the rolloff factor.",
       returns = "()",
       type = "function"
      },
      setVelocity = {
       args = "(x: number, y: number, z: number)",
       description = "Sets the velocity of the Source.\n\nThis does not change the position of the Source, but is used to calculate the doppler effect.",
       returns = "()",
       type = "function"
      },
      setVolume = {
       args = "(volume: number)",
       description = "Sets the volume of the Source.",
       returns = "()",
       type = "function"
      },
      setVolumeLimits = {
       args = "(min: number, max: number)",
       description = "Sets the volume limits of the source. The limits have to be numbers from 0 to 1.",
       returns = "()",
       type = "function"
      },
      stop = {
       args = "()",
       description = "Stops a Source.",
       returns = "()",
       type = "function"
      },
      tell = {
       args = "(unit: TimeUnit)",
       description = "Gets the currently playing position of the Source.",
       returns = "(position: number)",
       type = "function"
      }
     },
     description = "A Source represents audio you can play back. You can do interesting things with Sources, like set the volume, pitch, and its position relative to the listener.",
     type = "lib"
    },
    SourceType = {
     childs = {
      static = {
       description = "Decode the entire sound at once.",
       type = "value"
      },
      stream = {
       description = "Stream the sound; decode it gradually.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    TimeUnit = {
     childs = {
      samples = {
       description = "Audio samples.",
       type = "value"
      },
      seconds = {
       description = "Regular seconds.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    getOrientation = {
     args = "()",
     description = "Returns the orientation of the listener.",
     returns = "(fx: number, fy: number, fz: number, ux: number, uy: number, uz: number)",
     type = "function"
    },
    getPosition = {
     args = "()",
     description = "Returns the position of the listener.",
     returns = "(x: number, y: number, z: number)",
     type = "function"
    },
    getSourceCount = {
     args = "()",
     description = "Returns the number of sources which are currently playing or paused.",
     returns = "(numSources: number)",
     type = "function"
    },
    getVelocity = {
     args = "()",
     description = "Returns the velocity of the listener.",
     returns = "(x: number, y: number, z: number)",
     type = "function"
    },
    getVolume = {
     args = "()",
     description = "Returns the master volume.",
     returns = "(volume: number)",
     type = "function"
    },
    newSource = {
     args = "(file: File, type: SourceType)",
     description = "Creates a new Source from a file, SoundData, or Decoder. Sources created from SoundData are always static.",
     returns = "(source: Source)",
     type = "function"
    },
    pause = {
     args = "(source: Source)",
     description = "Pauses all audio",
     returns = "()",
     type = "function"
    },
    play = {
     args = "(source: Source)",
     description = "Plays the specified Source.",
     returns = "()",
     type = "function"
    },
    resume = {
     args = "(source: Source)",
     description = "Resumes all audio",
     returns = "()",
     type = "function"
    },
    rewind = {
     args = "(source: Source)",
     description = "Rewinds all playing audio.",
     returns = "()",
     type = "function"
    },
    setDistanceModel = {
     args = "(model: DistanceModel)",
     description = "Sets the distance attenuation model.",
     returns = "()",
     type = "function"
    },
    setOrientation = {
     args = "(fx: number, fy: number, fz: number, ux: number, uy: number, uz: number)",
     description = "Sets the orientation of the listener.",
     returns = "()",
     type = "function"
    },
    setPosition = {
     args = "(x: number, y: number, z: number)",
     description = "Sets the position of the listener, which determines how sounds play.",
     returns = "()",
     type = "function"
    },
    setVelocity = {
     args = "(x: number, y: number, z: number)",
     description = "Sets the velocity of the listener.",
     returns = "()",
     type = "function"
    },
    setVolume = {
     args = "(volume: number)",
     description = "Sets the master volume.",
     returns = "()",
     type = "function"
    },
    stop = {
     args = "(source: Source)",
     description = "Stops all playing audio.",
     returns = "()",
     type = "function"
    }
   },
   description = "Provides an interface to create noise with the user's speakers.",
   type = "class"
  },
  draw = {
   args = "()",
   description = "Callback function used to draw on the screen every frame.",
   returns = "()",
   type = "function"
  },
  errhand = {
   args = "(msg: string)",
   description = "The error handler, used to display error messages.",
   returns = "()",
   type = "function"
  },
  event = {
   childs = {
    Event = {
     childs = {
      focus = {
       description = "Window focus gained or lost",
       type = "value"
      },
      joystickpressed = {
       description = "Joystick pressed",
       type = "value"
      },
      joystickreleased = {
       description = "Joystick released",
       type = "value"
      },
      keypressed = {
       description = "Key pressed",
       type = "value"
      },
      keyreleased = {
       description = "Key released",
       type = "value"
      },
      mousepressed = {
       description = "Mouse pressed",
       type = "value"
      },
      mousereleased = {
       description = "Mouse released",
       type = "value"
      },
      quit = {
       description = "Quit",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    poll = {
     args = "()",
     description = "Returns an iterator for messages in the event queue.",
     returns = "(i: function)",
     type = "function"
    },
    pump = {
     args = "()",
     description = "Pump events into the event queue. This is a low-level function, and is usually not called by the user, but by love.run. Note that this does need to be called for any OS to think you're still running, and if you want to handle OS-generated events at all (think callbacks). love.event.pump can only be called from the main thread, but afterwards, the rest of love.event can be used from any other thread.",
     returns = "()",
     type = "function"
    },
    push = {
     args = "(e: Event, a: mixed, b: mixed, c: mixed, d: mixed)",
     description = "Adds an event to the event queue.",
     returns = "()",
     type = "function"
    },
    quit = {
     args = "()",
     description = "Adds the quit event to the queue.\n\nThe quit event is a signal for the event handler to close LÖVE. It's possible to abort the exit process with the love.quit callback.",
     returns = "()",
     type = "function"
    },
    wait = {
     args = "()",
     description = "Like love.event.poll but blocks until there is an event in the queue.",
     returns = "(e: Event, a: mixed, b: mixed, c: mixed, d: mixed)",
     type = "function"
    }
   },
   description = "Manages events, like keypresses.",
   type = "lib"
  },
  filesystem = {
   childs = {
    BufferMode = {
     childs = {
      full = {
       description = "Full buffering. Write and append operations are always buffered until the buffer size limit is reached.",
       type = "value"
      },
      line = {
       description = "Line buffering. Write and append operations are buffered until a newline is output or the buffer size limit is reached.",
       type = "value"
      },
      none = {
       description = "No buffering. The result of write and append operations appears immediately.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    File = {
     childs = {
      eof = {
       args = "()",
       description = "If the end-of-file has been reached",
       returns = "(eof: boolean)",
       type = "function"
      },
      flush = {
       args = "()",
       description = "Flushes any buffered written data in the file to the disk.",
       returns = "(success: boolean, err: string)",
       type = "function"
      },
      getBuffer = {
       args = "()",
       description = "Gets the buffer mode of a file.",
       returns = "(mode: BufferMode, size: number)",
       type = "function"
      },
      getMode = {
       args = "()",
       description = "Gets the FileMode the file has been opened with.",
       returns = "(mode: FileMode)",
       type = "function"
      },
      getSize = {
       args = "()",
       description = "Returns the file size.",
       returns = "(size: number)",
       type = "function"
      },
      isOpen = {
       args = "()",
       description = "Gets whether the file is open.",
       returns = "(open: boolean)",
       type = "function"
      },
      lines = {
       args = "()",
       description = "Iterate over all the lines in a file",
       returns = "(iterator: function)",
       type = "function"
      },
      open = {
       args = "(mode: FileMode)",
       description = "Open the file for write, read or append.\n\nIf you are getting the error message \"Could not set write directory\", try setting the save directory. This is done either with love.filesystem.setIdentity or by setting the identity field in love.conf.",
       returns = "(ok: boolean)",
       type = "function"
      },
      read = {
       args = "(bytes: number)",
       description = "Read a number of bytes from a file.",
       returns = "(contents: string, size: number)",
       type = "function"
      },
      seek = {
       args = "(position: number)",
       description = "Seek to a position in a file.",
       returns = "(success: boolean)",
       type = "function"
      },
      setBuffer = {
       args = "(mode: BufferMode, size: number)",
       description = "Sets the buffer mode for a file opened for writing or appending. Files with buffering enabled will not write data to the disk until the buffer size limit is reached, depending on the buffer mode.",
       returns = "(success: boolean, errorstr: string)",
       type = "function"
      },
      write = {
       args = "(data: string, size: number)",
       description = "Write data to a file.",
       returns = "(success: boolean)",
       type = "function"
      }
     },
     description = "Represents a file on the filesystem.",
     type = "lib"
    },
    FileData = {
     args = "()",
     description = "Data representing the contents of a file.",
     returns = "()",
     type = "function"
    },
    FileDecoder = {
     childs = {
      base64 = {
       description = "The data is base64-encoded.",
       type = "value"
      },
      file = {
       description = "The data is unencoded.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    FileMode = {
     childs = {
      a = {
       description = "Open a file for append.",
       type = "value"
      },
      c = {
       description = "Do not open a file (represents a closed file.)",
       type = "value"
      },
      r = {
       description = "Open a file for read.",
       type = "value"
      },
      w = {
       description = "Open a file for write.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    createDirectory = {
     args = "(name: string)",
     description = "Creates a directory.",
     returns = "(ok: boolean)",
     type = "function"
    },
    exists = {
     args = "(filename: string)",
     description = "Check whether a file or directory exists.",
     returns = "(e: boolean)",
     type = "function"
    },
    getAppdataDirectory = {
     args = "()",
     description = "Returns the application data directory (could be the same as getUserDirectory)",
     returns = "(path: string)",
     type = "function"
    },
    getDirectoryItems = {
     args = "(dir: string)",
     description = "Returns a table with the names of files and subdirectories in the directory in an undefined order.",
     returns = "(items: table)",
     type = "function"
    },
    getIdentity = {
     args = "(name: string)",
     description = "Gets the write directory name for your game. Note that this only returns the name of the folder to store your files in, not the full location.",
     returns = "()",
     type = "function"
    },
    getLastModified = {
     args = "(filename: string)",
     description = "Gets the last modification time of a file.",
     returns = "(modtime: number, errormsg: string)",
     type = "function"
    },
    getSaveDirectory = {
     args = "()",
     description = "Gets the full path to the designated save directory. This can be useful if you want to use the standard io library (or something else) to read or write in the save directory.",
     returns = "(path: string)",
     type = "function"
    },
    getSize = {
     args = "(filename: string)",
     description = "Gets the size in bytes of a file.",
     returns = "(size: number, errormsg: string)",
     type = "function"
    },
    getUserDirectory = {
     args = "()",
     description = "Returns the path of the user's directory.",
     returns = "(path: string)",
     type = "function"
    },
    getWorkingDirectory = {
     args = "()",
     description = "Gets the current working directory.",
     returns = "(path: string)",
     type = "function"
    },
    isDirectory = {
     args = "(path: string)",
     description = "Check whether something is a directory.",
     returns = "(is_dir: boolean)",
     type = "function"
    },
    isFile = {
     args = "(path: string)",
     description = "Check whether something is a file.",
     returns = "(is_file: boolean)",
     type = "function"
    },
    isFused = {
     args = "()",
     description = "Gets whether the game is in fused mode or not.\n\nIf a game is in fused mode, its save directory will be directly in the Appdata directory instead of Appdata/LOVE/. The game will also be able to load C Lua dynamic libraries which are located in the save directory.\n\nA game is in fused mode if the source .love has been fused to the executable (see Game Distribution), or if \"--fused\" has been given as a command-line argument when starting the game.",
     returns = "(fused: boolean)",
     type = "function"
    },
    lines = {
     args = "(name: string)",
     description = "Iterate over the lines in a file.",
     returns = "(iterator: function)",
     type = "function"
    },
    load = {
     args = "(name: string)",
     description = "Load a file (but not run it).",
     returns = "(chunk: function)",
     type = "function"
    },
    newFile = {
     args = "(filename: string, mode: FileMode)",
     description = "Creates a new File object. It needs to be opened before it can be accessed.",
     returns = "(file: File, errorstr: string)",
     type = "function"
    },
    newFileData = {
     args = "(contents: string, name: string, decoder: FileDecoder)",
     description = "Creates a new FileData object.",
     returns = "(data: FileData)",
     type = "function"
    },
    read = {
     args = "(name: string, bytes: number)",
     description = "Read the contents of a file.",
     returns = "(contents: string, size: number)",
     type = "function"
    },
    remove = {
     args = "(name: string)",
     description = "Removes a file or directory.",
     returns = "(ok: boolean)",
     type = "function"
    },
    setIdentity = {
     args = "(name: string, searchorder: SearchOrder)",
     description = "Sets the write directory for your game. Note that you can only set the name of the folder to store your files in, not the location.",
     returns = "()",
     type = "function"
    },
    write = {
     args = "(name: string, data: string, size: number)",
     description = "Write data to a file.\n\nIf you are getting the error message \"Could not set write directory\", try setting the save directory. This is done either with love.filesystem.setIdentity or by setting the identity field in love.conf.",
     returns = "(success: boolean)",
     type = "function"
    }
   },
   description = "Provides an interface to the user's filesystem.",
   type = "class"
  },
  focus = {
   args = "(f: boolean)",
   description = "Callback function triggered when window receives or loses focus.",
   returns = "()",
   type = "function"
  },
  font = {
   childs = {
    FontData = {
     description = "A FontData represents a font, containing the font Rasterizer and its glyphs.",
     type = "value"
    },
    GlyphData = {
     description = "A GlyphData represents a drawable symbol of a font Rasterizer.",
     type = "value"
    },
    Rasterizer = {
     description = "A Rasterizer handles font rendering, containing the font data (image or TrueType font font) and drawable glyphs.",
     type = "value"
    },
    newGlyphData = {
     args = "(rasterizer: Rasterizer, glyph: number)",
     description = "Creates a new GlyphData.",
     returns = "(glyphData: GlyphData)",
     type = "function"
    },
    newRasterizer = {
     args = "(imageData: ImageData, glyphs: string)",
     description = "Creates a new Rasterizer.",
     returns = "(rasterizer: Rasterizer)",
     type = "function"
    }
   },
   description = "Allows you to work with fonts.",
   type = "class"
  },
  gamepadaxis = {
   args = "(joystick: Joystick, axis: GamepadAxis)",
   description = "Called when a Joystick's virtual gamepad axis is moved.",
   returns = "()",
   type = "function"
  },
  gamepadpressed = {
   args = "(joystick: Joystick, button: GamepadButton)",
   description = "Called when a Joystick's virtual gamepad button is pressed.",
   returns = "()",
   type = "function"
  },
  gamepadreleased = {
   args = "(joystick: Joystick, button: GamepadButton)",
   description = "Called when a Joystick's virtual gamepad button is released.",
   returns = "()",
   type = "function"
  },
  graphics = {
   childs = {
    AlignMode = {
     childs = {
      center = {
       description = "Align text center.",
       type = "value"
      },
      left = {
       description = "Align text left.",
       type = "value"
      },
      right = {
       description = "Align text right.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    AreaSpreadDistribution = {
     childs = {
      none = {
       description = "No distribution - area spread is disabled.",
       type = "value"
      },
      normal = {
       description = "Normal (gaussian) distribution.",
       type = "value"
      },
      uniform = {
       description = "Uniform distribution.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    BlendMode = {
     childs = {
      additive = {
       description = "Additive blend mode.",
       type = "value"
      },
      alpha = {
       description = "Alpha blend mode (\"normal\").",
       type = "value"
      },
      multiplicative = {
       description = "Multiply blend mode.",
       type = "value"
      },
      premultiplied = {
       description = "Premultiplied blend mode.",
       type = "value"
      },
      replace = {
       description = "Replace blend mode.",
       type = "value"
      },
      subtractive = {
       description = "Subtractive blend mode.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    Canvas = {
     childs = {
      getDimensions = {
       args = "()",
       description = "Gets the width and height of the Canvas.",
       returns = "(width: number, height: number)",
       type = "function"
      },
      getFilter = {
       args = "()",
       description = "Gets the filter mode of the Canvas.",
       returns = "(min: FilterMode, mag: FilterMode)",
       type = "function"
      },
      getHeight = {
       args = "()",
       description = "Gets the height of the Canvas.",
       returns = "(height: number)",
       type = "function"
      },
      getImageData = {
       args = "()",
       description = "Returns the image data stored in the Canvas. Think of it as taking a screenshot of the hidden screen that is the Canvas.",
       returns = "(data: ImageData)",
       type = "function"
      },
      getPixel = {
       args = "(x: number, y: number)",
       description = "Gets the pixel at the specified position from a Canvas.\n\nValid x and y values start at 0 and go up to canvas width and height minus 1.",
       returns = "(r: number, g: number, b: number, a: number)",
       type = "function"
      },
      getWidth = {
       args = "()",
       description = "Gets the width of the Canvas.",
       returns = "(width: number)",
       type = "function"
      },
      getWrap = {
       args = "()",
       description = "Gets the wrapping properties of a Canvas.\n\nThis functions returns the currently set horizontal and vertical wrapping modes for the Canvas.",
       returns = "(horizontal: WrapMode, vertical: WrapMode)",
       type = "function"
      },
      renderTo = {
       args = "(func: function)",
       description = "Render to the Canvas using a function.",
       returns = "()",
       type = "function"
      },
      setFilter = {
       args = "(min: FilterMode, mag: FilterMode)",
       description = "Sets the filter of the Canvas.",
       returns = "()",
       type = "function"
      },
      setWrap = {
       args = "(horizontal: WrapMode, vertical: WrapMode)",
       description = "Sets the wrapping properties of a Canvas.\n\nThis function sets the way the edges of a Canvas are treated if it is scaled or rotated. If the WrapMode is set to \"clamp\", the edge will not be interpolated. If set to \"repeat\", the edge will be interpolated with the pixels on the opposing side of the framebuffer.",
       returns = "()",
       type = "function"
      }
     },
     description = "A Canvas is used for off-screen rendering. Think of it as an invisible screen that you can draw to, but that will not be visible until you draw it to the actual visible screen. It is also known as \"render to texture\".\n\nBy drawing things that do not change position often (such as background items) to the Canvas, and then drawing the entire Canvas instead of each item, you can reduce the number of draw operations performed each frame.",
     type = "lib"
    },
    DrawMode = {
     childs = {
      fill = {
       description = "Draw filled shape.",
       type = "value"
      },
      line = {
       description = "Draw outlined shape.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    FilterMode = {
     childs = {
      linear = {
       description = "Scale image with linear interpolation.",
       type = "value"
      },
      nearest = {
       description = "Scale image with nearest neighbor interpolation.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    Font = {
     childs = {
      getBaseline = {
       args = "()",
       description = "Gets the baseline of the Font. Most scripts share the notion of a baseline: an imaginary horizontal line on which characters rest. In some scripts, parts of glyphs lie below the baseline.",
       returns = "(baseline: number)",
       type = "function"
      },
      getDescent = {
       args = "()",
       description = "Gets the descent of the Font. The descent spans the distance between the baseline and the lowest descending glyph in a typeface.",
       returns = "(descent: number)",
       type = "function"
      },
      getFilter = {
       args = "()",
       description = "Gets the filter mode for a font.",
       returns = "(min: FilterMode, mag: FilterMode, anisotropy: number)",
       type = "function"
      },
      getHeight = {
       args = "()",
       description = "Gets the height of the Font. The height of the font is the size including any spacing; the height which it will need.",
       returns = "(height: number)",
       type = "function"
      },
      getLineHeight = {
       args = "()",
       description = "Gets the line height. This will be the value previously set by Font:setLineHeight, or 1.0 by default.",
       returns = "(height: number)",
       type = "function"
      },
      getWidth = {
       args = "(line: string)",
       description = "Determines the horizontal size a line of text needs. Does not support line-breaks.",
       returns = "(width: number)",
       type = "function"
      },
      getWrap = {
       args = "(text: string, width: number)",
       description = "Returns how many lines text would be wrapped to. This function accounts for newlines correctly (i.e. '\\n')",
       returns = "(width: number, lines: number)",
       type = "function"
      },
      hasGlyph = {
       args = "(codepoint: number)",
       description = "Gets whether the font can render a particular character.",
       returns = "(hasglyph: boolean)",
       type = "function"
      },
      setFilter = {
       args = "(min: FilterMode, mag: FilterMode, anisotropy: number)",
       description = "Sets the filter mode for a font.",
       returns = "()",
       type = "function"
      },
      setLineHeight = {
       args = "(height: number)",
       description = "Sets the line height. When rendering the font in lines the actual height will be determined by the line height multiplied by the height of the font. The default is 1.0.",
       returns = "()",
       type = "function"
      }
     },
     description = "Defines the shape of characters than can be drawn onto the screen.",
     type = "lib"
    },
    GraphicsFeature = {
     childs = {
      bc5 = {
       description = "Support for BC4 and BC5 compressed images.",
       type = "value"
      },
      canvas = {
       description = "Support for Canvas.",
       type = "value"
      },
      dxt = {
       description = "Support for DXT compressed images (see CompressedFormat.)",
       type = "value"
      },
      hdrcanvas = {
       description = "Support for HDR Canvas.",
       type = "value"
      },
      mipmap = {
       description = "Support for Mipmaps.",
       type = "value"
      },
      multicanvas = {
       description = "Support for simultaneous rendering to at least 4 canvases at once, with love.graphics.setCanvas.",
       type = "value"
      },
      npot = {
       description = "Support for textures with non-power-of-two textures.",
       type = "value"
      },
      shader = {
       description = "Support for Shader.",
       type = "value"
      },
      subtractive = {
       description = "Support for the subtractive blend mode.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    Image = {
     childs = {
      getDimensions = {
       args = "()",
       description = "Gets the width and height of the Image.",
       returns = "(width: number, height: number)",
       type = "function"
      },
      getFilter = {
       args = "()",
       description = "Gets the filter mode for an image.",
       returns = "(min: FilterMode, mag: FilterMode)",
       type = "function"
      },
      getHeight = {
       args = "()",
       description = "Gets the height of the Image.",
       returns = "(height: number)",
       type = "function"
      },
      getMipmapFilter = {
       args = "()",
       description = "Gets the mipmap filter mode for an Image.",
       returns = "(mode: FilterMode, sharpness: number)",
       type = "function"
      },
      getWidth = {
       args = "()",
       description = "Gets the width of the Image.",
       returns = "(width: number)",
       type = "function"
      },
      getWrap = {
       args = "()",
       description = "Gets the wrapping properties of an Image.\n\nThis functions returns the currently set horizontal and vertical wrapping modes for the image.",
       returns = "(horizontal: WrapMode, vertical: WrapMode)",
       type = "function"
      },
      refresh = {
       args = "()",
       description = "Reloads the Image's contents from the ImageData or CompressedData used to create the image.",
       returns = "()",
       type = "function"
      },
      setFilter = {
       args = "(min: FilterMode, mag: FilterMode)",
       description = "Sets the filter mode for an image.",
       returns = "()",
       type = "function"
      },
      setMipmapFilter = {
       args = "(filtermode: FilterMode, sharpness: number)",
       description = "Sets the mipmap filter mode for an Image.\n\nMipmapping is useful when drawing an image at a reduced scale. It can improve performance and reduce aliasing issues.\n\nAutomatically creates mipmaps for the Image if none exist yet. If the image is compressed and its CompressedData has mipmap data included, it will use that.\n\nDisables mipmap filtering when called without arguments.",
       returns = "()",
       type = "function"
      },
      setWrap = {
       args = "(horizontal: WrapMode, vertical: WrapMode)",
       description = "Sets the wrapping properties of an Image.\n\nThis function sets the way an Image is repeated when it is drawn with a Quad that is larger than the image's extent. An image may be clamped or set to repeat in both horizontal and vertical directions. Clamped images appear only once, but repeated ones repeat as many times as there is room in the Quad.\n\nIf you use a Quad that is larger than the image extent and do not use repeated tiling, there may be an unwanted visual effect of the image stretching all the way to fill the Quad. If this is the case, setting Image:getWrap(\"repeat\", \"repeat\") for all the images to be repeated, and using Quad of appropriate size will result in the best visual appearance.",
       returns = "()",
       type = "function"
      }
     },
     description = "Drawable image type.",
     type = "lib"
    },
    LineJoin = {
     childs = {
      bevel = {
       description = "Bevel style.",
       type = "value"
      },
      miter = {
       description = "Miter style.",
       type = "value"
      },
      none = {
       description = "None style.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    LineStyle = {
     childs = {
      rough = {
       description = "Draw rough lines.",
       type = "value"
      },
      smooth = {
       description = "Draw smooth lines.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    Mesh = {
     childs = {
      getVertex = {
       args = "(i: number)",
       description = "Returns vertex information from the Mesh.",
       returns = "(x: number, y: number, u: number, v: number, r: number, g: number, b: number, a: number)",
       type = "function"
      },
      getVertexCount = {
       args = "()",
       description = "Returns the total number of vertices in the Mesh.",
       returns = "(num: number)",
       type = "function"
      },
      getVertexMap = {
       args = "()",
       description = "Gets the Mesh's vertex map.\n\n If no vertex map has been set previously (either in love.graphics.newMesh or with Mesh:setVertexMap), then this function will return the default vertex map: {1, 2, 3, ..., Mesh:getVertexCount()}.",
       returns = "(vertex_map: table)",
       type = "function"
      },
      hasVertexColors = {
       args = "(on: boolean)",
       description = "Retrieves if the per-vertex colors are used when rendering instead of the constant color (constant color being love.graphics.setColor or SpriteBatch:setColor)\n\nThe per-vertex colors are automatically enabled by default when making a new Mesh or when doing Mesh:setVertex, but only if at least one vertex color is not the default (255,255,255,255).",
       returns = "()",
       type = "function"
      },
      setVertex = {
       args = "(i: number, x: number, y: number, u: number, v: number, r: number, g: number, b: number, a: number)",
       description = "Sets the vertex information for a Mesh.",
       returns = "()",
       type = "function"
      },
      setVertexColors = {
       args = "(on: boolean)",
       description = "Sets if the per-vertex colors are used when rendering instead of the constant color (constant color being love.graphics.setColor or SpriteBatch:setColor)\n\nThe per-vertex colors are automatically enabled by default when making a new Mesh or when doing Mesh:setVertex, but only if at least one vertex color is not the default (255,255,255,255).",
       returns = "()",
       type = "function"
      },
      setVertexMap = {
       args = "(vi1: number, vi2: number, vi3: number)",
       description = "Sets the vertex map for a Mesh. The vertex map describes the order in which the vertices are used when the Mesh is drawn.\n\nThe vertex map allows you to re-order or reuse vertices when drawing without changing the actual vertex parameters or duplicating vertices. It is especially useful when combined with different Mesh draw modes.",
       returns = "()",
       type = "function"
      }
     },
     description = "",
     type = "lib"
    },
    MeshDrawMode = {
     childs = {
      fan = {
       description = "The vertices create a \"fan\" shape with the first vertex acting as the hub point. Can be easily used to draw simple convex polygons.",
       type = "value"
      },
      points = {
       description = "The vertices are drawn as unconnected points (see love.graphics.setPointSize.)",
       type = "value"
      },
      strip = {
       description = "The vertices create a series of connected triangles using vertices v1, v2, v3, then v3, v2, v4 (note the order), then v3, v4, v5 and so on.",
       type = "value"
      },
      triangles = {
       description = "The vertices create unconnected triangles.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    ParticleInsertMode = {
     childs = {
      bottom = {
       description = "Particles are inserted at the bottom of the ParticleSystem's list of particles.",
       type = "value"
      },
      random = {
       description = "Particles are inserted at random positions in the ParticleSystem's list of particles.",
       type = "value"
      },
      top = {
       description = "Particles are inserted at the top of the ParticleSystem's list of particles.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    ParticleSystem = {
     childs = {
      getAreaSpread = {
       args = "()",
       description = "Gets the area-based spawn parameters for the particles.",
       returns = "(distribution: AreaSpreadDistribution, dx: number, dy: number)",
       type = "function"
      },
      getBufferSize = {
       args = "()",
       description = "Gets the size of the buffer (the max allowed amount of particles in the system).",
       returns = "(buffer: number)",
       type = "function"
      },
      getColors = {
       args = "()",
       description = "Gets a series of colors to apply to the particle sprite. The particle system will interpolate between each color evenly over the particle's lifetime. Color modulation needs to be activated for this function to have any effect.\n\nArguments are passed in groups of four, representing the components of the desired RGBA value. At least one color must be specified. A maximum of eight may be used.",
       returns = "(r1: number, g1: number, b1: number, a1: number, r2: number, g2: number, b2: number, a2: number, ...: number)",
       type = "function"
      },
      getCount = {
       args = "()",
       description = "Gets the amount of particles that are currently in the system.",
       returns = "(count: number)",
       type = "function"
      },
      getDirection = {
       args = "()",
       description = "Gets the direction the particles will be emitted in.",
       returns = "(direction: number)",
       type = "function"
      },
      getEmissionRate = {
       args = "()",
       description = "Gets the amount of particles emitted per second.",
       returns = "(rate: number)",
       type = "function"
      },
      getEmitterLifetime = {
       args = "()",
       description = "Gets how long the particle system should emit particles (if -1 then it emits particles forever).",
       returns = "(life: number)",
       type = "function"
      },
      getImage = {
       args = "()",
       description = "Gets the image which is to be emitted.",
       returns = "(image: Image)",
       type = "function"
      },
      getInsertMode = {
       args = "()",
       description = "Gets the mode to use when the ParticleSystem adds new particles.",
       returns = "(mode: ParticleInsertMode)",
       type = "function"
      },
      getLinearAcceleration = {
       args = "()",
       description = "Gets the linear acceleration (acceleration along the x and y axes) for particles.\n\nEvery particle created will accelerate along the x and y axes between xmin,ymin and xmax,ymax.",
       returns = "(xmin: number, ymin: number, xmax: number, ymax: number)",
       type = "function"
      },
      getOffset = {
       args = "()",
       description = "Get the offget position which the particle sprite is rotated around. If this function is not used, the particles rotate around their center.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getParticleLifetime = {
       args = "()",
       description = "Gets the life of the particles.",
       returns = "(min: number, max: number)",
       type = "function"
      },
      getPosition = {
       args = "()",
       description = "Gets the position of the emitter.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getRadialAcceleration = {
       args = "()",
       description = "Get the radial acceleration (away from the emitter).",
       returns = "(min: number, max: number)",
       type = "function"
      },
      getRotation = {
       args = "()",
       description = "Gets the rotation of the image upon particle creation (in radians).",
       returns = "(min: number, max: number)",
       type = "function"
      },
      getSizeVariation = {
       args = "()",
       description = "Gets the degree of variation (0 meaning no variation and 1 meaning full variation between start and end).",
       returns = "(variation: number)",
       type = "function"
      },
      getSizes = {
       args = "()",
       description = "Gets a series of sizes by which to scale a particle sprite. 1.0 is normal size. The particle system will interpolate between each size evenly over the particle's lifetime.\n\nAt least one size must be specified. A maximum of eight may be used.",
       returns = "(size1: number, size2: number, ...: number)",
       type = "function"
      },
      getSpeed = {
       args = "()",
       description = "Gets the speed of the particles.",
       returns = "(min: number, max: number)",
       type = "function"
      },
      getSpin = {
       args = "()",
       description = "Gets the spin of the sprite.",
       returns = "(min: number, max: number)",
       type = "function"
      },
      getSpinVariation = {
       args = "()",
       description = "Gets the degree of variation (0 meaning no variation and 1 meaning full variation between start and end).",
       returns = "(variation: number)",
       type = "function"
      },
      getSpread = {
       args = "()",
       description = "Gets the amount of spread for the system.",
       returns = "(spread: number)",
       type = "function"
      },
      getTangentialAcceleration = {
       args = "()",
       description = "Gets the tangential acceleration (acceleration perpendicular to the particle's direction).",
       returns = "(min: number, max: number)",
       type = "function"
      },
      isActive = {
       args = "()",
       description = "Checks whether the particle system is actively emitting particles.",
       returns = "(active: boolean)",
       type = "function"
      },
      isPaused = {
       args = "()",
       description = "Checks whether the particle system is paused.",
       returns = "(paused: boolean)",
       type = "function"
      },
      isStopped = {
       args = "()",
       description = "Checks whether the particle system is stopped.",
       returns = "(stopped: boolean)",
       type = "function"
      },
      pause = {
       args = "()",
       description = "Pauses the particle emitter.",
       returns = "()",
       type = "function"
      },
      reset = {
       args = "()",
       description = "Resets the particle emitter, removing any existing particles and resetting the lifetime counter.",
       returns = "()",
       type = "function"
      },
      setAreaSpread = {
       args = "(distribution: AreaSpreadDistribution, dx: number, dy: number)",
       description = "Sets area-based spawn parameters for the particles. Newly created particles will spawn in an area around the emitter based on the parameters to this function.",
       returns = "()",
       type = "function"
      },
      setBufferSize = {
       args = "(buffer: number)",
       description = "Sets the size of the buffer (the max allowed amount of particles in the system).",
       returns = "()",
       type = "function"
      },
      setColors = {
       args = "(r1: number, g1: number, b1: number, a1: number, r2: number, g2: number, b2: number, a2: number, ...: number)",
       description = "Sets a series of colors to apply to the particle sprite. The particle system will interpolate between each color evenly over the particle's lifetime. Color modulation needs to be activated for this function to have any effect.\n\nArguments are passed in groups of four, representing the components of the desired RGBA value. At least one color must be specified. A maximum of eight may be used.",
       returns = "()",
       type = "function"
      },
      setDirection = {
       args = "(direction: number)",
       description = "Sets the direction the particles will be emitted in.",
       returns = "()",
       type = "function"
      },
      setEmissionRate = {
       args = "(rate: number)",
       description = "Sets the amount of particles emitted per second.",
       returns = "()",
       type = "function"
      },
      setEmitterLifetime = {
       args = "(life: number)",
       description = "Sets how long the particle system should emit particles (if -1 then it emits particles forever).",
       returns = "()",
       type = "function"
      },
      setImage = {
       args = "(image: Image)",
       description = "Sets the image which is to be emitted.",
       returns = "()",
       type = "function"
      },
      setInsertMode = {
       args = "(mode: ParticleInsertMode)",
       description = "Sets the mode to use when the ParticleSystem adds new particles.",
       returns = "()",
       type = "function"
      },
      setLinearAcceleration = {
       args = "(xmin: number, ymin: number, xmax: number, ymax: number)",
       description = "Sets the linear acceleration (acceleration along the x and y axes) for particles.\n\nEvery particle created will accelerate along the x and y axes between xmin,ymin and xmax,ymax.",
       returns = "()",
       type = "function"
      },
      setOffset = {
       args = "(x: number, y: number)",
       description = "Set the offset position which the particle sprite is rotated around. If this function is not used, the particles rotate around their center.",
       returns = "()",
       type = "function"
      },
      setParticleLifetime = {
       args = "(min: number, max: number)",
       description = "Sets the life of the particles.",
       returns = "()",
       type = "function"
      },
      setPosition = {
       args = "(x: number, y: number)",
       description = "Sets the position of the emitter.",
       returns = "()",
       type = "function"
      },
      setRadialAcceleration = {
       args = "(min: number, max: number)",
       description = "Set the radial acceleration (away from the emitter).",
       returns = "()",
       type = "function"
      },
      setRotation = {
       args = "(min: number, max: number)",
       description = "Sets the rotation of the image upon particle creation (in radians).",
       returns = "()",
       type = "function"
      },
      setSizeVariation = {
       args = "(variation: number)",
       description = "Sets the degree of variation (0 meaning no variation and 1 meaning full variation between start and end).",
       returns = "()",
       type = "function"
      },
      setSizes = {
       args = "(size1: number, size2: number, ...: number)",
       description = "Sets a series of sizes by which to scale a particle sprite. 1.0 is normal size. The particle system will interpolate between each size evenly over the particle's lifetime.\n\nAt least one size must be specified. A maximum of eight may be used.",
       returns = "()",
       type = "function"
      },
      setSpeed = {
       args = "(min: number, max: number)",
       description = "Sets the speed of the particles.",
       returns = "()",
       type = "function"
      },
      setSpin = {
       args = "(min: number, max: number)",
       description = "Sets the spin of the sprite.",
       returns = "()",
       type = "function"
      },
      setSpinVariation = {
       args = "(variation: number)",
       description = "Sets the degree of variation (0 meaning no variation and 1 meaning full variation between start and end).",
       returns = "()",
       type = "function"
      },
      setSpread = {
       args = "(spread: number)",
       description = "Sets the amount of spread for the system.",
       returns = "()",
       type = "function"
      },
      setTangentialAcceleration = {
       args = "(min: number, max: number)",
       description = "Sets the tangential acceleration (acceleration perpendicular to the particle's direction).",
       returns = "()",
       type = "function"
      },
      start = {
       args = "()",
       description = "Starts the particle emitter.",
       returns = "()",
       type = "function"
      },
      stop = {
       args = "()",
       description = "Stops the particle emitter, resetting the lifetime counter.",
       returns = "()",
       type = "function"
      },
      update = {
       args = "(dt: number)",
       description = "Updates the particle system; moving, creating and killing particles.",
       returns = "()",
       type = "function"
      }
     },
     description = "Used to create cool effects, like fire. The particle systems are created and drawn on the screen using functions in love.graphics. They also need to be updated in the update(dt) callback for you to see any changes in the particles emitted.",
     type = "lib"
    },
    PointStyle = {
     childs = {
      rough = {
       description = "Draw rough points.",
       type = "value"
      },
      smooth = {
       description = "Draw smooth points.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    Quad = {
     args = "()",
     description = "A quadrilateral (a polygon with four sides and four corners) with texture coordinate information.\n\nQuads can be used to select part of a texture to draw. In this way, one large texture atlas can be loaded, and then split up into sub-images.",
     returns = "()",
     type = "function"
    },
    Shader = {
     args = "()",
     description = "A Shader is used for advanced hardware-accelerated pixel or vertex manipulation. These effects are written in a language based on GLSL (OpenGL Shading Language) with a few things simplified for easier coding.\n\nPotential uses for pixel effects include HDR/bloom, motion blur, grayscale/invert/sepia/any kind of color effect, reflection/refraction, distortions, and much more!",
     returns = "()",
     type = "function"
    },
    SpriteBatch = {
     childs = {
      bind = {
       args = "()",
       description = "Binds the SpriteBatch to the memory.\n\nBinding a SpriteBatch before updating its content can improve the performance as it doesn't push each update to the graphics card separately. Don't forget to unbind the SpriteBatch or the updates won't show up.",
       returns = "()",
       type = "function"
      },
      clear = {
       args = "()",
       description = "Removes all sprites from the buffer.",
       returns = "()",
       type = "function"
      },
      getBufferSize = {
       args = "()",
       description = "Gets the maximum number of sprites the SpriteBatch can hold.",
       returns = "(size: number)",
       type = "function"
      },
      getColor = {
       args = "(r: number, g: number, b: number, a: number)",
       description = "Gets the color that will be used for the next add and set operations.\n\nIf no color has been set with SpriteBatch:setColor or the current SpriteBatch color has been cleared, this method will return nil.",
       returns = "()",
       type = "function"
      },
      getCount = {
       args = "()",
       description = "Gets the amount of sprites currently in the SpriteBatch.",
       returns = "(count: number)",
       type = "function"
      },
      getImage = {
       args = "()",
       description = "Returns the image used by the SpriteBatch.",
       returns = "(image: Image)",
       type = "function"
      },
      set = {
       args = "(id: number, quad: Quad, x: number, y: number, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
       description = "Changes a sprite in the batch. This requires the identifier returned by add and addq.",
       returns = "()",
       type = "function"
      },
      setBufferSize = {
       args = "(size: number)",
       description = "Sets the maximum number of sprites the SpriteBatch can hold. Existing sprites in the batch (up to the new maximum) will not be cleared when this function is called.",
       returns = "()",
       type = "function"
      },
      setColor = {
       args = "(r: number, g: number, b: number, a: number)",
       description = "Sets the color that will be used for the next add and set operations. Calling the function without arguments will clear the color.\n\nThe global color set with love.graphics.setColor will not work on the SpriteBatch if any of the sprites has its own color.",
       returns = "()",
       type = "function"
      },
      setImage = {
       args = "(image: Image)",
       description = "Replaces the image used for the sprites.",
       returns = "()",
       type = "function"
      },
      unbind = {
       args = "()",
       description = "Unbinds the SpriteBatch.",
       returns = "()",
       type = "function"
      }
     },
     description = "Using a single image, draw any number of identical copies of the image using a single call to love.graphics.draw. This can be used, for example, to draw repeating copies of a single background image.\n\nA SpriteBatch can be even more useful when the underlying image is a Texture Atlas (a single image file containing many independent images); by adding Quad to the batch, different sub-images from within the atlas can be drawn.",
     type = "lib"
    },
    SpriteBatchUsage = {
     childs = {
      dynamic = {
       description = "The SpriteBatch data will change repeatedly during its lifetime.",
       type = "value"
      },
      static = {
       description = "The SpriteBatch will not be modified after initial sprites are added.",
       type = "value"
      },
      stream = {
       description = "The SpriteBatch data will always change between draws.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    TextureMode = {
     childs = {
      hdr = {
       description = "The HDR texture format: floating point 16 bits per channel (64 bpp) RGBA.",
       type = "value"
      },
      normal = {
       description = "The default texture format: 8 bits per channel (32 bpp) RGBA.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    WrapMode = {
     childs = {
      clamp = {
       description = "Clamp the image. Appears only once.",
       type = "value"
      },
      ["repeat"] = {
       description = "Repeat the image. Fills the whole available extent.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    circle = {
     args = "(mode: DrawMode, x: number, y: number, radius: number, segments: number)",
     description = "Draws a circle.",
     returns = "()",
     type = "function"
    },
    clear = {
     args = "()",
     description = "Clears the screen to background color and restores the default coordinate system.\n\nThis function is called automatically before love.draw in the default love.run function. See the example in love.run for a typical use of this function.\n\nNote that the scissor area bounds the cleared region.",
     returns = "()",
     type = "function"
    },
    draw = {
     args = "(image: Image, quad: Quad, x: number, y: number, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
     description = "Draws objects on screen. Drawable objects are loaded images, but may be other kinds of Drawable objects, such as a ParticleSystem.\n\nIn addition to simple drawing, this function can rotate and scale the object at the same time, as well as offset the image (for example, to center the image at the chosen coordinates).\n\nlove.graphics.draw anchors from the top left corner by default.\n\nYou can specify a negative value for sx or sy to flip the drawable horizontally or vertically.\n\nThe pivotal point is (x, y) on the screen and (ox, oy) in the internal coordinate system of the drawable object, before rotation and scaling. The object is scaled by (sx, sy), then rotated by r around the pivotal point.\n\nThe origin offset values are most often used to shift the images up and left by half of its height and width, so that (effectively) the specified x and y coordinates are where the center of the image will end up.",
     returns = "()",
     type = "function"
    },
    getBackgroundColor = {
     args = "()",
     description = "Gets the current background color.",
     returns = "(r: number, g: number, b: number, a: number)",
     type = "function"
    },
    getBlendMode = {
     args = "()",
     description = "Gets the blending mode.",
     returns = "(mode: BlendMode)",
     type = "function"
    },
    getCanvas = {
     args = "()",
     description = "Gets the current target Canvas.",
     returns = "(canvas: Canvas)",
     type = "function"
    },
    getColor = {
     args = "()",
     description = "Gets the current color.",
     returns = "(r: number, g: number, b: number, a: number)",
     type = "function"
    },
    getColorMask = {
     args = "()",
     description = "Gets the active color components used when drawing. Normally all 4 components are active unless love.graphics.setColorMask has been used.\n\nThe color mask determines whether individual components of the colors of drawn objects will affect the color of the screen. They affect love.graphics.clear and Canvas:clear as well.",
     returns = "(r: boolean, g: boolean, b: boolean, a: boolean)",
     type = "function"
    },
    getDefaultFilter = {
     args = "()",
     description = "Returns the default scaling filters used with Images, Canvases, and Fonts.",
     returns = "(min: FilterMode, mag: FilterMode, anisotropy: number)",
     type = "function"
    },
    getDimensions = {
     args = "()",
     description = "Gets the width and height of the window.",
     returns = "(width: number, height: number)",
     type = "function"
    },
    getFont = {
     args = "()",
     description = "Gets the current Font object.",
     returns = "(font: Font)",
     type = "function"
    },
    getFullscreenModes = {
     args = "()",
     description = "Gets a list of supported fullscreen modes.",
     returns = "(modes: table)",
     type = "function"
    },
    getHeight = {
     args = "()",
     description = "Gets the height of the window.",
     returns = "(height: number)",
     type = "function"
    },
    getLineJoin = {
     args = "()",
     description = "Gets the line join style.",
     returns = "(join: LineJoin)",
     type = "function"
    },
    getLineStyle = {
     args = "()",
     description = "Gets the line style.",
     returns = "(style: LineStyle)",
     type = "function"
    },
    getLineWidth = {
     args = "()",
     description = "Gets the current line width.",
     returns = "(width: number)",
     type = "function"
    },
    getMaxImageSize = {
     args = "()",
     description = "Gets the max supported width or height of Images and Canvases.\n\nAttempting to create an Image with a width *or* height greater than this number will create a checkerboard-patterned image instead. Doing the same for a canvas will result in an error.\n\nThe returned number depends on the system running the code. It is safe to assume it will never be less than 1024 and will almost always be 2048 or greater.",
     returns = "(size: number)",
     type = "function"
    },
    getMaxPointSize = {
     args = "()",
     description = "Gets the max supported point size.",
     returns = "(size: number)",
     type = "function"
    },
    getPointSize = {
     args = "()",
     description = "Gets the point size.",
     returns = "(size: number)",
     type = "function"
    },
    getPointStyle = {
     args = "()",
     description = "Gets the current point style.",
     returns = "(style: PointStyle)",
     type = "function"
    },
    getRendererInfo = {
     args = "()",
     description = "Gets information about the system's video card and drivers.",
     returns = "(name: string, version: string, vendor: string, device: string)",
     type = "function"
    },
    getScissor = {
     args = "()",
     description = "Gets the current scissor box.",
     returns = "(x: number, y: number, width: number, height: number)",
     type = "function"
    },
    getShader = {
     args = "()",
     description = "Returns the current Shader. Returns nil if none is set.",
     returns = "(shader: Shader)",
     type = "function"
    },
    getWidth = {
     args = "()",
     description = "Gets the width of the window.",
     returns = "(width: number)",
     type = "function"
    },
    isSupported = {
     args = "(supportN: GraphicsFeature)",
     description = "Checks if certain graphics functions can be used.\n\nOlder and low-end systems do not always support all graphics extensions.",
     returns = "(isSupported: boolean)",
     type = "function"
    },
    line = {
     args = "(points: table)",
     description = "Draws lines between points.",
     returns = "()",
     type = "function"
    },
    newCanvas = {
     args = "(width: number, height: number, texture_type: TextureMode)",
     description = "Creates a new Canvas object for offscreen rendering.",
     returns = "(canvas: Canvas)",
     type = "function"
    },
    newFont = {
     args = "(file: File, size: number)",
     description = "Creates a new Font.",
     returns = "(font: Font)",
     type = "function"
    },
    newImage = {
     args = "(file: File)",
     description = "Creates a new Image from a filepath or an opened File or an ImageData.",
     returns = "(image: Image)",
     type = "function"
    },
    newImageFont = {
     args = "(file: File, glyphs: string)",
     description = "Creates a new font by loading a specifically formatted image. There can be up to 256 glyphs.\n\nExpects ISO 8859-1 encoding for the glyphs string.",
     returns = "(font: Font)",
     type = "function"
    },
    newMesh = {
     args = "(vertices: table, image: Image, mode: MeshDrawMode)",
     description = "Creates a new Mesh.",
     returns = "(mesh: Mesh)",
     type = "function"
    },
    newParticleSystem = {
     args = "(image: Image, buffer: number)",
     description = "Creates a new ParticleSystem.",
     returns = "(system: ParticleSystem)",
     type = "function"
    },
    newQuad = {
     args = "(x: number, y: number, width: number, height: number, sw: number, sh: number)",
     description = "Creates a new Quad.\n\nThe purpose of a Quad is to describe the result of the following transformation on any drawable object. The object is first scaled to dimensions sw * sh. The Quad then describes the rectangular area of dimensions width * height whose upper left corner is at position (x, y) inside the scaled object.",
     returns = "(quad: Quad)",
     type = "function"
    },
    newScreenshot = {
     args = "()",
     description = "Creates a screenshot and returns the image data.",
     returns = "(screenshot: ImageData)",
     type = "function"
    },
    newShader = {
     args = "(pixelcode: string, vertexcode: string)",
     description = "Creates a new Shader object for hardware-accelerated vertex and pixel effects. A Shader contains either vertex shader code, pixel shader code, or both.\n\nVertex shader code must contain at least one function, named position, which is the function that will produce transformed vertex positions of drawn objects in screen-space.\n\nPixel shader code must contain at least one function, named effect, which is the function that will produce the color which is blended onto the screen for each pixel a drawn object touches.",
     returns = "(shader: Shader)",
     type = "function"
    },
    newSpriteBatch = {
     args = "(image: Image, size: number, usage: SpriteBatchUsage)",
     description = "Creates a new SpriteBatch object.",
     returns = "(spriteBatch: SpriteBatch)",
     type = "function"
    },
    origin = {
     args = "()",
     description = "Resets the current coordinate transformation.\n\nThis function is always used to reverse any previous calls to love.graphics.rotate, love.graphics.scale, love.graphics.shear or love.graphics.translate. It returns the current transformation state to its defaults.",
     returns = "()",
     type = "function"
    },
    point = {
     args = "(x: number, y: number)",
     description = "Draws a point.\n\nThe pixel grid is actually offset to the center of each pixel. So to get clean pixels drawn use 0.5 + integer increments.",
     returns = "()",
     type = "function"
    },
    polygon = {
     args = "(mode: DrawMode, vertices: table)",
     description = "Draw a polygon.\n\nFollowing the mode argument, this function can accept multiple numeric arguments or a single table of numeric arguments. In either case the arguments are interpreted as alternating x and y coordinates of the polygon's vertices.\n\nWhen in fill mode, the polygon must be convex and simple or rendering artifacts may occur.",
     returns = "()",
     type = "function"
    },
    pop = {
     args = "()",
     description = "Pops the current coordinate transformation from the transformation stack.\n\nThis function is always used to reverse a previous push operation. It returns the current transformation state to what it was before the last preceding push. For an example, see the description of love.graphics.push.",
     returns = "()",
     type = "function"
    },
    present = {
     args = "()",
     description = "Displays the results of drawing operations on the screen.\n\nThis function is used when writing your own love.run function. It presents all the results of your drawing operations on the screen. See the example in love.run for a typical use of this function.",
     returns = "()",
     type = "function"
    },
    print = {
     args = "(text: string, x: number, y: number, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
     description = "Draws text on screen. If no Font is set, one will be created and set (once) if needed.\n\nWhen using translation and scaling functions while drawing text, this function assumes the scale occurs first. If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.\n\nlove.graphics.print stops at the first '\0' (null) character. This can bite you if you are appending keystrokes to form your string, as some of those are multi-byte unicode characters which will likely contain null bytes.",
     returns = "()",
     type = "function"
    },
    printf = {
     args = "(text: string, x: number, y: number, limit: number, align: AlignMode, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
     description = "Draws formatted text, with word wrap and alignment.\n\nSee additional notes in love.graphics.print.",
     returns = "()",
     type = "function"
    },
    push = {
     args = "()",
     description = "Copies and pushes the current coordinate transformation to the transformation stack.\n\nThis function is always used to prepare for a corresponding pop operation later. It stores the current coordinate transformation state into the transformation stack and keeps it active. Later changes to the transformation can be undone by using the pop operation, which returns the coordinate transform to the state it was in before calling push.",
     returns = "()",
     type = "function"
    },
    rectangle = {
     args = "(mode: DrawMode, x: number, y: number, width: number, height: number)",
     description = "Draws a rectangle.",
     returns = "()",
     type = "function"
    },
    reset = {
     args = "()",
     description = "Resets the current graphics settings.\n\nCalling reset makes the current drawing color white, the current background color black, resets any active Canvas or Shader, and removes any scissor settings. It sets the BlendMode to alpha. It also sets both the point and line drawing modes to smooth and their sizes to 1.0.",
     returns = "()",
     type = "function"
    },
    rotate = {
     args = "(angle: number)",
     description = "Rotates the coordinate system in two dimensions.\n\nCalling this function affects all future drawing operations by rotating the coordinate system around the origin by the given amount of radians. This change lasts until love.draw exits.",
     returns = "()",
     type = "function"
    },
    scale = {
     args = "(sx: number, sy: number)",
     description = "Scales the coordinate system in two dimensions.\n\nBy default the coordinate system in LÖVE corresponds to the display pixels in horizontal and vertical directions one-to-one, and the x-axis increases towards the right while the y-axis increases downwards. Scaling the coordinate system changes this relation.\n\nAfter scaling by sx and sy, all coordinates are treated as if they were multiplied by sx and sy. Every result of a drawing operation is also correspondingly scaled, so scaling by (2, 2) for example would mean making everything twice as large in both x- and y-directions. Scaling by a negative value flips the coordinate system in the corresponding direction, which also means everything will be drawn flipped or upside down, or both. Scaling by zero is not a useful operation.\n\nScale and translate are not commutative operations, therefore, calling them in different orders will change the outcome.\n\nScaling lasts until love.draw exits.",
     returns = "()",
     type = "function"
    },
    setBackgroundColor = {
     args = "(rgba: table)",
     description = "Sets the background color.",
     returns = "()",
     type = "function"
    },
    setBlendMode = {
     args = "(mode: BlendMode)",
     description = "Sets the blending mode.",
     returns = "()",
     type = "function"
    },
    setCanvas = {
     args = "(canvas: Canvas, ...: Canvas)",
     description = "Sets the render target to one or more Canvases. All drawing operations until the next love.graphics.setCanvas call will be redirected to the specified canvases and not shown on the screen.\n\nAll canvas arguments must have the same widths and heights and the same texture type. Normally the same thing will be drawn on each canvas, but that can be changed if a pixel shader is used with the \"effects\" function instead of the regular effect.\n\nNot all computers support Canvases, and not all computers which support Canvases will support multiple render targets. Use love.graphics.isSupported to check.\n\nnWhen called without arguments, the render target is reset to the screen.",
     returns = "()",
     type = "function"
    },
    setColor = {
     args = "(rgba: table)",
     description = "Sets the color used for drawing.",
     returns = "()",
     type = "function"
    },
    setColorMask = {
     args = "(red: boolean, green: boolean, blue: boolean, alpha: boolean)",
     description = "Sets the color mask. Enables or disables specific color components when rendering and clearing the screen. For example, if red is set to false, no further changes will be made to the red component of any pixels.\n\nEnables all color components when called without arguments.",
     returns = "()",
     type = "function"
    },
    setDefaultFilter = {
     args = "(min: FilterMode, mag: FilterMode, anisotropy: number)",
     description = "Sets the default scaling filters used with Images, Canvases, and Fonts.\n\nThis function does not apply retroactively to loaded images.",
     returns = "()",
     type = "function"
    },
    setFont = {
     args = "(font: Font)",
     description = "Set an already-loaded Font as the current font or create and load a new one from the file and size.\n\nIt's recommended that Font objects are created with love.graphics.newFont in the loading stage and then passed to this function in the drawing stage.",
     returns = "()",
     type = "function"
    },
    setInvertedStencil = {
     args = "(stencilFunction: function)",
     description = "Defines an inverted stencil for the drawing operations or releases the active one.\n\nIt's the same as love.graphics.setStencil with the mask inverted.\n\nCalling the function without arguments releases the active stencil.",
     returns = "()",
     type = "function"
    },
    setLineJoin = {
     args = "(join: LineJoin)",
     description = "Sets the line join style.",
     returns = "()",
     type = "function"
    },
    setLineStyle = {
     args = "(style: LineStyle)",
     description = "Sets the line style.",
     returns = "()",
     type = "function"
    },
    setLineWidth = {
     args = "(width: number)",
     description = "Sets the line width.",
     returns = "()",
     type = "function"
    },
    setNewFont = {
     args = "(filename: string, size: number)",
     description = "Creates and sets a new font.",
     returns = "(font: Font)",
     type = "function"
    },
    setPointSize = {
     args = "(size: number)",
     description = "Sets the point size.",
     returns = "()",
     type = "function"
    },
    setPointStyle = {
     args = "(style: PointStyle)",
     description = "Sets the point style.",
     returns = "()",
     type = "function"
    },
    setScissor = {
     args = "(x: number, y: number, width: number, height: number)",
     description = "Sets or disables scissor.\n\nThe scissor limits the drawing area to a specified rectangle. This affects all graphics calls, including love.graphics.clear.",
     returns = "()",
     type = "function"
    },
    setShader = {
     args = "(shader: Shader)",
     description = "Sets or resets a Shader as the current pixel effect or vertex shaders. All drawing operations until the next love.graphics.setShader will be drawn using the Shader object specified.\n\nDisables the shaders when called without arguments.",
     returns = "()",
     type = "function"
    },
    setStencil = {
     args = "(stencilFunction: function)",
     description = "Defines or releases a stencil for the drawing operations.\n\nThe passed function draws to the stencil instead of the screen, creating an image with transparent and opaque pixels. While active, it is used to test where pixels will be drawn or discarded.\n\nCalling the function without arguments releases the active stencil.\n\nWhen called without arguments, the active stencil is released.",
     returns = "()",
     type = "function"
    },
    shear = {
     args = "(kx: number, ky: number)",
     description = "Shears the coordinate system.",
     returns = "()",
     type = "function"
    },
    translate = {
     args = "(dx: number, dy: number)",
     description = "Translates the coordinate system in two dimensions.\n\nWhen this function is called with two numbers, dx, and dy, all the following drawing operations take effect as if their x and y coordinates were x+dx and y+dy.\n\nScale and translate are not commutative operations, therefore, calling them in different orders will change the outcome.\n\nThis change lasts until love.graphics.clear is called (which is called automatically before love.draw in the default love.run function), or a love.graphics.pop reverts to a previous coordinate system state.\n\nTranslating using whole numbers will prevent tearing/blurring of images and fonts draw after translating.",
     returns = "()",
     type = "function"
    }
   },
   description = "The primary responsibility for the love.graphics module is the drawing of lines, shapes, text, Images and other Drawable objects onto the screen. Its secondary responsibilities include loading external files (including Images and Fonts) into memory, creating specialized objects (such as ParticleSystems or Framebuffers) and managing screen geometry.\n\nLÖVE's coordinate system is rooted in the upper-left corner of the screen, which is at location (0, 0). The x-axis is horizontal: larger values are further to the right. The y-axis is vertical: larger values are further towards the bottom.\n\nIn many cases, you draw images or shapes in terms of their upper-left corner (See the picture above).\n\nMany of the functions are used to manipulate the graphics coordinate system, which is essentially the way coordinates are mapped to the display. You can change the position, scale, and even rotation in this way.",
   type = "class"
  },
  image = {
   childs = {
    CompressedData = {
     childs = {
      getHeight = {
       args = "(level: number)",
       description = "Gets the height of the CompressedData.",
       returns = "(height: number)",
       type = "function"
      },
      getMipmapCount = {
       args = "(mipmaps: number)",
       description = "Gets the number of mipmap levels in the CompressedData. The base mipmap level (original image) is included in the count.\n\nMipmap filtering cannot be activated for an Image created from a CompressedData which does not have enough mipmap levels to go down to 1x1. For example, a 256x256 image created from a CompressedData should have 8 mipmap levels or Image:setMipmapFilter will error. Most tools which can create compressed textures are able to automatically generate mipmaps for them in the same file.",
       returns = "()",
       type = "function"
      },
      getWidth = {
       args = "(level: number)",
       description = "Gets the width of the CompressedData.",
       returns = "(width: number)",
       type = "function"
      }
     },
     description = "Represents compressed image data designed to stay compressed in RAM. CompressedData encompasses standard compressed formats such as DXT1, DXT5, and BC5 / 3Dc.\n\nYou can't draw CompressedData directly to the screen. See Image for that.",
     type = "lib"
    },
    ImageData = {
     childs = {
      getDimensions = {
       args = "()",
       description = "Gets the width and height of the ImageData.",
       returns = "(width: number, height: number)",
       type = "function"
      },
      getHeight = {
       args = "()",
       description = "Gets the height of the ImageData.",
       returns = "(height: number)",
       type = "function"
      },
      getPixel = {
       args = "(x: number, y: number)",
       description = "Gets the pixel at the specified position.\n\nValid x and y values start at 0 and go up to image width and height minus 1.",
       returns = "(r: number, g: number, b: number, a: number)",
       type = "function"
      },
      getWidth = {
       args = "()",
       description = "Gets the width of the ImageData.",
       returns = "(width: number)",
       type = "function"
      },
      mapPixel = {
       args = "(pixelFunction: function)",
       description = "Transform an image by applying a function to every pixel.\n\nThis function is a higher order function. It takes another function as a parameter, and calls it once for each pixel in the ImageData.\n\nThe function parameter is called with six parameters for each pixel in turn. The parameters are numbers that represent the x and y coordinates of the pixel and its red, green, blue and alpha values. The function parameter can return up to four number values, which become the new r, g, b and a values of the pixel. If the function returns fewer values, the remaining components are set to 0.",
       returns = "()",
       type = "function"
      },
      paste = {
       args = "(source: ImageData, dx: number, dy: number, sx: number, sy: number, sw: number, sh: number)",
       description = "Paste into ImageData from another source ImageData.",
       returns = "()",
       type = "function"
      },
      setPixel = {
       args = "(x: number, y: number, r: number, g: number, b: number, a: number)",
       description = "Sets the color of a pixel.\n\nValid x and y values start at 0 and go up to image width and height minus 1.",
       returns = "()",
       type = "function"
      }
     },
     description = "Raw (decoded) image data.\n\nYou can't draw ImageData directly to screen. See Image for that.",
     type = "lib"
    },
    ImageFormat = {
     childs = {
      bmp = {
       description = "BMP image format.",
       type = "value"
      },
      jpg = {
       description = "JPG image format.",
       type = "value"
      },
      png = {
       description = "PNG image format.",
       type = "value"
      },
      tga = {
       description = "Targa image format.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    newCompressedData = {
     args = "(fileData: FileData)",
     description = "Create a new CompressedData object from a compressed image file. LÖVE currently supports DDS files compressed with the DXT1, DXT5, and BC5 / 3Dc formats.",
     returns = "(compressedData: CompressedData)",
     type = "function"
    },
    newImageData = {
     args = "(filename: string)",
     description = "Create a new ImageData object.",
     returns = "(imageData: ImageData)",
     type = "function"
    }
   },
   description = "Provides an interface to decode encoded image data.",
   type = "class"
  },
  joystick = {
   childs = {
    GamepadAxis = {
     childs = {
      leftx = {
       description = "The x-axis of the left thumbstick.",
       type = "value"
      },
      lefty = {
       description = "The y-axis of the left thumbstick.",
       type = "value"
      },
      rightx = {
       description = "The x-axis of the right thumbstick.",
       type = "value"
      },
      righty = {
       description = "The y-axis of the right thumbstick.",
       type = "value"
      },
      triggerleft = {
       description = "Left analog trigger.",
       type = "value"
      },
      triggerright = {
       description = "Right analog trigger.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    GamepadButton = {
     childs = {
      a = {
       description = "Bottom face button (A).",
       type = "value"
      },
      b = {
       description = "Right face button (B).",
       type = "value"
      },
      back = {
       description = "Back button.",
       type = "value"
      },
      dpdown = {
       description = "D-pad down.",
       type = "value"
      },
      dpleft = {
       description = "D-pad left.",
       type = "value"
      },
      dpright = {
       description = "D-pad right.",
       type = "value"
      },
      dpup = {
       description = "D-pad up.",
       type = "value"
      },
      guide = {
       description = "Guide button.",
       type = "value"
      },
      leftshoulder = {
       description = "Left bumper.",
       type = "value"
      },
      leftstick = {
       description = "Left stick click button.",
       type = "value"
      },
      rightshoulder = {
       description = "Right bumper.",
       type = "value"
      },
      rightstick = {
       description = "Right stick click button.",
       type = "value"
      },
      start = {
       description = "Start button.",
       type = "value"
      },
      x = {
       description = "Left face button (X).",
       type = "value"
      },
      y = {
       description = "Top face button (Y).",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    Joystick = {
     childs = {
      getAxis = {
       args = "(axis: number)",
       description = "Gets the direction of an axis.",
       returns = "(direction: number)",
       type = "function"
      },
      getAxisCount = {
       args = "()",
       description = "Gets the number of axes on the joystick.",
       returns = "(axes: number)",
       type = "function"
      },
      getButtonCount = {
       args = "()",
       description = "Gets the number of buttons on the joystick.",
       returns = "(buttons: number)",
       type = "function"
      },
      getGUID = {
       args = "()",
       description = "Gets a stable GUID unique to the type of the physical joystick which does not change over time. For example, all Sony Dualshock 3 controllers in OS X have the same GUID. The value is platform-dependent.",
       returns = "(guid: string)",
       type = "function"
      },
      getGamepadAxis = {
       args = "(axis: GamepadAxis)",
       description = "Gets the direction of a virtual gamepad axis. If the Joystick isn't recognized as a gamepad or isn't connected, this function will always return 0.",
       returns = "(direction: number)",
       type = "function"
      },
      getGamepadMapping = {
       args = "(button: GamepadAxis)",
       description = "Gets the button, axis or hat that a virtual gamepad input is bound to.",
       returns = "(inputtype: JoystickInputType, inputindex: number, hatdirection: JoystickHat)",
       type = "function"
      },
      getHat = {
       args = "(hat: number)",
       description = "Gets the direction of a hat.",
       returns = "(direction: JoystickHat)",
       type = "function"
      },
      getHatCount = {
       args = "()",
       description = "Gets the number of hats on the joystick.",
       returns = "(hats: number)",
       type = "function"
      },
      getID = {
       args = "()",
       description = "Gets the joystick's unique identifier. The identifier will remain the same for the life of the game, even when the Joystick is disconnected and reconnected, but it will change when the game is re-launched.",
       returns = "(id: number, instanceid: number)",
       type = "function"
      },
      getName = {
       args = "()",
       description = "Gets the name of the joystick.",
       returns = "(name: string)",
       type = "function"
      },
      isConnected = {
       args = "()",
       description = "Gets whether the Joystick is connected.",
       returns = "(connected: boolean)",
       type = "function"
      },
      isDown = {
       args = "(...: number)",
       description = "Checks if a button on the Joystick is pressed.",
       returns = "(anyDown: boolean)",
       type = "function"
      },
      isGamepad = {
       args = "()",
       description = "Gets whether the Joystick is recognized as a gamepad. If this is the case, the Joystick's buttons and axes can be used in a standardized manner across different operating systems and joystick models via Joystick:getGamepadAxis and related functions.\n\nLÖVE automatically recognizes most popular controllers with a similar layout to the Xbox 360 controller as gamepads, but you can add more with love.joystick.setGamepadMapping.",
       returns = "(isgamepad: boolean)",
       type = "function"
      },
      isGamepadDown = {
       args = "(...: GamepadButton)",
       description = "Checks if a virtual gamepad button on the Joystick is pressed. If the Joystick is not recognized as a Gamepad or isn't connected, then this function will always return false.",
       returns = "(anyDown: boolean)",
       type = "function"
      }
     },
     description = "Represents a physical joystick.",
     type = "lib"
    },
    JoystickHat = {
     childs = {
      c = {
       description = "Centered",
       type = "value"
      },
      d = {
       description = "Down",
       type = "value"
      },
      l = {
       description = "Left",
       type = "value"
      },
      ld = {
       description = "Left+Down",
       type = "value"
      },
      lu = {
       description = "Left+Up",
       type = "value"
      },
      r = {
       description = "Right",
       type = "value"
      },
      rd = {
       description = "Right+Down",
       type = "value"
      },
      ru = {
       description = "Right+Up",
       type = "value"
      },
      u = {
       description = "Up",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    JoystickInputType = {
     childs = {
      axis = {
       description = "Analog axis.",
       type = "value"
      },
      button = {
       description = "Button.",
       type = "value"
      },
      hat = {
       description = "8-direction hat value.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    getJoystickCount = {
     args = "()",
     description = "Gets the number of connected joysticks.",
     returns = "(joystickcount: number)",
     type = "function"
    },
    getJoysticks = {
     args = "()",
     description = "Gets a list of connected Joysticks.",
     returns = "(joysticks: table)",
     type = "function"
    }
   },
   description = "Provides an interface to the user's joystick.",
   type = "class"
  },
  joystickadded = {
   args = "(joystick: Joystick)",
   description = "Called when a Joystick is connected.\n\nThis callback is also triggered after love.load for every Joystick which was already connected when the game started up.",
   returns = "()",
   type = "function"
  },
  joystickaxis = {
   args = "(joystick: Joystick, axis: number, value: number)",
   description = "Called when a joystick axis moves.",
   returns = "()",
   type = "function"
  },
  joystickhat = {
   args = "(joystick: Joystick, hat: number, direction: JoystickHat)",
   description = "Called when a joystick hat direction changes.",
   returns = "()",
   type = "function"
  },
  joystickpressed = {
   args = "(joystick: number, button: number)",
   description = "Called when a joystick button is pressed.",
   returns = "()",
   type = "function"
  },
  joystickreleased = {
   args = "(joystick: number, button: number)",
   description = "Called when a joystick button is released.",
   returns = "()",
   type = "function"
  },
  joystickremoved = {
   args = "(joystick: Joystick)",
   description = "Called when a Joystick is disconnected.",
   returns = "()",
   type = "function"
  },
  keyboard = {
   childs = {
    KeyConstant = {
     childs = {
      ["!"] = {
       description = "Exclamation mark key",
       type = "value"
      },
      ["\""] = {
       description = "Double quote key",
       type = "value"
      },
      ["#"] = {
       description = "Hash key",
       type = "value"
      },
      ["$"] = {
       description = "Dollar key",
       type = "value"
      },
      ["&"] = {
       description = "Ampersand key",
       type = "value"
      },
      ["'"] = {
       description = "Single quote key",
       type = "value"
      },
      ["("] = {
       description = "Left parenthesis key",
       type = "value"
      },
      ["(space)"] = {
       description = "Space key",
       notes = "Replace (space) with the actual space character",
       type = "value"
      },
      [")"] = {
       description = "Right parenthesis key",
       type = "value"
      },
      ["*"] = {
       description = "Asterisk key",
       type = "value"
      },
      ["+"] = {
       description = "Plus key",
       type = "value"
      },
      [","] = {
       description = "Comma key",
       type = "value"
      },
      ["-"] = {
       description = "Hyphen-minus key",
       type = "value"
      },
      ["."] = {
       description = "Full stop key",
       type = "value"
      },
      ["/"] = {
       description = "Slash key",
       type = "value"
      },
      ["0"] = {
       description = "The zero key",
       type = "value"
      },
      ["1"] = {
       description = "The one key",
       type = "value"
      },
      ["2"] = {
       description = "The two key",
       type = "value"
      },
      ["3"] = {
       description = "The three key",
       type = "value"
      },
      ["4"] = {
       description = "The four key",
       type = "value"
      },
      ["5"] = {
       description = "The five key",
       type = "value"
      },
      ["6"] = {
       description = "The six key",
       type = "value"
      },
      ["7"] = {
       description = "The seven key",
       type = "value"
      },
      ["8"] = {
       description = "The eight key",
       type = "value"
      },
      ["9"] = {
       description = "The nine key",
       type = "value"
      },
      [":"] = {
       description = "Colon key",
       type = "value"
      },
      [";"] = {
       description = "Semicolon key",
       type = "value"
      },
      ["<"] = {
       description = "Less-than key",
       type = "value"
      },
      ["="] = {
       description = "Equal key",
       type = "value"
      },
      [">"] = {
       description = "Greater-than key",
       type = "value"
      },
      ["?"] = {
       description = "Question mark key",
       type = "value"
      },
      ["@"] = {
       description = "At sign key",
       type = "value"
      },
      ["["] = {
       description = "Left square bracket key",
       type = "value"
      },
      ["\\"] = {
       description = "Backslash key",
       type = "value"
      },
      ["]"] = {
       description = "Right square bracket key",
       type = "value"
      },
      ["^"] = {
       description = "Caret key",
       type = "value"
      },
      _ = {
       description = "Underscore key",
       type = "value"
      },
      ["`"] = {
       description = "Grave accent key",
       notes = "Also known as the \"Back tick\" key",
       type = "value"
      },
      a = {
       description = "The A key",
       type = "value"
      },
      b = {
       description = "The B key",
       type = "value"
      },
      backspace = {
       description = "Backspace key",
       type = "value"
      },
      ["break"] = {
       description = "Break key",
       type = "value"
      },
      c = {
       description = "The C key",
       type = "value"
      },
      capslock = {
       description = "Caps-lock key",
       notes = "Caps-on is a key press. Caps-off is a key release.",
       type = "value"
      },
      clear = {
       description = "Clear key",
       type = "value"
      },
      compose = {
       description = "Compose key",
       type = "value"
      },
      d = {
       description = "The D key",
       type = "value"
      },
      delete = {
       description = "Delete key",
       type = "value"
      },
      down = {
       description = "Down cursor key",
       type = "value"
      },
      e = {
       description = "The E key",
       type = "value"
      },
      ["end"] = {
       description = "End key",
       type = "value"
      },
      escape = {
       description = "Escape key",
       type = "value"
      },
      euro = {
       description = "Euro (&euro;) key",
       type = "value"
      },
      f = {
       description = "The F key",
       type = "value"
      },
      f1 = {
       description = "The 1st function key",
       type = "value"
      },
      f2 = {
       description = "The 2nd function key",
       type = "value"
      },
      f3 = {
       description = "The 3rd function key",
       type = "value"
      },
      f4 = {
       description = "The 4th function key",
       type = "value"
      },
      f5 = {
       description = "The 5th function key",
       type = "value"
      },
      f6 = {
       description = "The 6th function key",
       type = "value"
      },
      f7 = {
       description = "The 7th function key",
       type = "value"
      },
      f8 = {
       description = "The 8th function key",
       type = "value"
      },
      f9 = {
       description = "The 9th function key",
       type = "value"
      },
      f10 = {
       description = "The 10th function key",
       type = "value"
      },
      f11 = {
       description = "The 11th function key",
       type = "value"
      },
      f12 = {
       description = "The 12th function key",
       type = "value"
      },
      f13 = {
       description = "The 13th function key",
       type = "value"
      },
      f14 = {
       description = "The 14th function key",
       type = "value"
      },
      f15 = {
       description = "The 15th function key",
       type = "value"
      },
      g = {
       description = "The G key",
       type = "value"
      },
      h = {
       description = "The H key",
       type = "value"
      },
      help = {
       description = "Help key",
       type = "value"
      },
      home = {
       description = "Home key",
       type = "value"
      },
      i = {
       description = "The I key",
       type = "value"
      },
      insert = {
       description = "Insert key",
       type = "value"
      },
      j = {
       description = "The J key",
       type = "value"
      },
      k = {
       description = "The K key",
       type = "value"
      },
      ["kp*"] = {
       description = "The numpad multiplication key",
       type = "value"
      },
      ["kp+"] = {
       description = "The numpad addition key",
       type = "value"
      },
      ["kp-"] = {
       description = "The numpad substraction key",
       type = "value"
      },
      ["kp."] = {
       description = "The numpad decimal point key",
       type = "value"
      },
      ["kp/"] = {
       description = "The numpad division key",
       type = "value"
      },
      kp0 = {
       description = "The numpad zero key",
       type = "value"
      },
      kp1 = {
       description = "The numpad one key",
       type = "value"
      },
      kp2 = {
       description = "The numpad two key",
       type = "value"
      },
      kp3 = {
       description = "The numpad three key",
       type = "value"
      },
      kp4 = {
       description = "The numpad four key",
       type = "value"
      },
      kp5 = {
       description = "The numpad five key",
       type = "value"
      },
      kp6 = {
       description = "The numpad six key",
       type = "value"
      },
      kp7 = {
       description = "The numpad seven key",
       type = "value"
      },
      kp8 = {
       description = "The numpad eight key",
       type = "value"
      },
      kp9 = {
       description = "The numpad nine key",
       type = "value"
      },
      ["kp="] = {
       description = "The numpad equals key",
       type = "value"
      },
      kpenter = {
       description = "The numpad enter key",
       type = "value"
      },
      l = {
       description = "The L key",
       type = "value"
      },
      lalt = {
       description = "Left alt key",
       type = "value"
      },
      lctrl = {
       description = "Left control key",
       type = "value"
      },
      left = {
       description = "Left cursor key",
       type = "value"
      },
      lmeta = {
       description = "Left meta key",
       type = "value"
      },
      lshift = {
       description = "Left shift key",
       type = "value"
      },
      lsuper = {
       description = "Left super key",
       type = "value"
      },
      m = {
       description = "The M key",
       type = "value"
      },
      menu = {
       description = "Menu key",
       type = "value"
      },
      mode = {
       description = "Mode key",
       type = "value"
      },
      n = {
       description = "The N key",
       type = "value"
      },
      numlock = {
       description = "Num-lock key",
       type = "value"
      },
      o = {
       description = "The O key",
       type = "value"
      },
      p = {
       description = "The P key",
       type = "value"
      },
      pagedown = {
       description = "Page down key",
       type = "value"
      },
      pageup = {
       description = "Page up key",
       type = "value"
      },
      pause = {
       description = "Pause key",
       type = "value"
      },
      power = {
       description = "Power key",
       type = "value"
      },
      print = {
       description = "Print key",
       type = "value"
      },
      q = {
       description = "The Q key",
       type = "value"
      },
      r = {
       description = "The R key",
       type = "value"
      },
      ralt = {
       description = "Right alt key",
       type = "value"
      },
      rctrl = {
       description = "Right control key",
       type = "value"
      },
      ["return"] = {
       description = "Return key",
       notes = "Also known as the Enter key",
       type = "value"
      },
      right = {
       description = "Right cursor key",
       type = "value"
      },
      rmeta = {
       description = "Right meta key",
       type = "value"
      },
      rshift = {
       description = "Right shift key",
       type = "value"
      },
      rsuper = {
       description = "Right super key",
       type = "value"
      },
      s = {
       description = "The S key",
       type = "value"
      },
      scrollock = {
       description = "Scroll-lock key",
       type = "value"
      },
      sysreq = {
       description = "System request key",
       type = "value"
      },
      t = {
       description = "The T key",
       type = "value"
      },
      tab = {
       description = "Tab key",
       type = "value"
      },
      u = {
       description = "The U key",
       type = "value"
      },
      undo = {
       description = "Undo key",
       type = "value"
      },
      up = {
       description = "Up cursor key",
       type = "value"
      },
      v = {
       description = "The V key",
       type = "value"
      },
      w = {
       description = "The W key",
       type = "value"
      },
      x = {
       description = "The X key",
       type = "value"
      },
      y = {
       description = "The Y key",
       type = "value"
      },
      z = {
       description = "The Z key",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    hasTextInput = {
     args = "()",
     description = "Gets whether text input events are enabled.",
     returns = "(enabled: boolean)",
     type = "function"
    },
    isDown = {
     args = "(...: KeyConstant)",
     description = "Checks whether a certain key is down. Not to be confused with love.keypressed or love.keyreleased.",
     returns = "(anyDown: boolean)",
     type = "function"
    },
    setKeyRepeat = {
     args = "(enable: boolean)",
     description = "Enables or disables key repeat. It is disabled by default.\n\nThe interval between repeats depends on the user's system settings.",
     returns = "()",
     type = "function"
    },
    setTextInput = {
     args = "(enable: boolean)",
     description = "Enables or disables text input events. It is enabled by default.",
     returns = "()",
     type = "function"
    }
   },
   description = "Provides an interface to the user's keyboard.",
   type = "lib"
  },
  keypressed = {
   args = "(key: KeyConstant, isrepeat: boolean)",
   description = "Callback function triggered when a key is pressed.\n\nKey repeat needs to be enabled with love.keyboard.setKeyRepeat for repeat keypress events to be received.",
   returns = "()",
   type = "function"
  },
  keyreleased = {
   args = "(key: KeyConstant)",
   description = "Callback function triggered when a key is released.",
   returns = "()",
   type = "function"
  },
  load = {
   args = "(arg: table)",
   description = "This function is called exactly once at the beginning of the game.",
   returns = "()",
   type = "function"
  },
  math = {
   childs = {
    BezierCurve = {
     childs = {
      getControlPoint = {
       args = "(i: number)",
       description = "Get coordinates of the i-th control point. Indices start with 1.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getControlPointCount = {
       args = "()",
       description = "Get the number of control points in the Bézier curve.",
       returns = "(count: number)",
       type = "function"
      },
      getDegree = {
       args = "()",
       description = "Get degree of the Bézier curve. The degree is equal to number-of-control-points - 1.",
       returns = "(degree: number)",
       type = "function"
      },
      getDerivative = {
       args = "()",
       description = "Get the derivative of the Bézier curve.\n\nThis function can be used to rotate sprites moving along a curve in the direction of the movement and compute the direction perpendicular to the curve at some parameter t.",
       returns = "(derivative: BezierCurve)",
       type = "function"
      },
      insertControlPoint = {
       args = "(x: number, y: number, i: number)",
       description = "Insert control point after the i-th control point. Indices start with 1. Negative indices wrap around: -1 is the last control point, -2 the one before the last, etc.",
       returns = "()",
       type = "function"
      },
      render = {
       args = "(depth: number)",
       description = "Get a list of coordinates to be used with love.graphics.line.\n\nThis function samples the Bézier curve using recursive subdivision. You can control the recursion depth using the depth parameter.\n\nIf you are just interested to know the position on the curve given a parameter, use BezierCurve:evalulate.",
       returns = "(coordinates: table)",
       type = "function"
      },
      rotate = {
       args = "(angle: number, ox: number, oy: number)",
       description = "Rotate the Bézier curve by an angle.",
       returns = "()",
       type = "function"
      },
      scale = {
       args = "(s: number, ox: number, oy: number)",
       description = "Scale the Bézier curve by a factor.",
       returns = "()",
       type = "function"
      },
      setControlPoint = {
       args = "(i: number, ox: number, oy: number)",
       description = "Set coordinates of the i-th control point. Indices start with 1.",
       returns = "()",
       type = "function"
      },
      translate = {
       args = "(dx: number, dy: number)",
       description = "Move the Bézier curve by an offset.",
       returns = "()",
       type = "function"
      }
     },
     description = "A Bézier curve object that can evaluate and render Bézier curves of arbitrary degree.",
     type = "lib"
    },
    RandomGenerator = {
     childs = {
      random = {
       args = "(max: number)",
       description = "Generates a pseudo random number in a platform independent way.",
       returns = "(number: number)",
       type = "function"
      },
      randomNormal = {
       args = "(stddev: number, mean: number)",
       description = "Get a normally distributed pseudo random number.",
       returns = "(number: number)",
       type = "function"
      },
      setSeed = {
       args = "(low: number, high: number)",
       description = "Sets the seed of the random number generator using the specified integer number.",
       returns = "()",
       type = "function"
      }
     },
     description = "A random number generation object which has its own random state.",
     type = "lib"
    },
    isConvex = {
     args = "(x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, ...: number)",
     description = "Checks whether a polygon is convex.\n\nPolygonShapes in love.physics, some forms of Mesh, and polygons drawn with love.graphics.polygon must be simple convex polygons.",
     returns = "(convex: boolean)",
     type = "function"
    },
    newBezierCurve = {
     args = "(x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, ...: number)",
     description = "Creates a new BezierCurve object.\n\nThe number of vertices in the control polygon determines the degree of the curve, e.g. three vertices define a quadratic (degree 2) Bézier curve, four vertices define a cubic (degree 3) Bézier curve, etc.",
     returns = "(curve: BezierCurve)",
     type = "function"
    },
    newRandomGenerator = {
     args = "(seed: number)",
     description = "Creates a new RandomGenerator object which is completely independent of other RandomGenerator objects and random functions.",
     returns = "(rng: RandomGenerator)",
     type = "function"
    },
    noise = {
     args = "(x: number, y: number)",
     description = "Generates a Simplex noise value in 1-4 dimensions.\n\nSimplex noise is closely related to Perlin noise. It is widely used for procedural content generation.",
     returns = "(value: number)",
     type = "function"
    },
    random = {
     args = "(max: number)",
     description = "Generates a pseudo random number in a platform independent way.",
     returns = "(number: number)",
     type = "function"
    },
    randomNormal = {
     args = "(stddev: number, mean: number)",
     description = "Get a normally distributed pseudo random number.",
     returns = "(number: number)",
     type = "function"
    },
    setRandomSeed = {
     args = "(low: number, high: number)",
     description = "Sets the seed of the random number generator using the specified integer number.",
     returns = "()",
     type = "function"
    },
    triangulate = {
     args = "(x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, ...: number)",
     description = "Triangulate a simple polygon.",
     returns = "(triangles: table)",
     type = "function"
    }
   },
   description = "Provides system-independent mathematical functions.",
   type = "class"
  },
  mouse = {
   childs = {
    Cursor = {
     args = "()",
     description = "Represents a hardware cursor.",
     returns = "()",
     type = "function"
    },
    CursorType = {
     childs = {
      arrow = {
       description = "An arrow pointer.",
       type = "value"
      },
      crosshair = {
       description = "Crosshair symbol.",
       type = "value"
      },
      hand = {
       description = "Hand symbol.",
       type = "value"
      },
      ibeam = {
       description = "An I-beam, normally used when mousing over editable or selectable text.",
       type = "value"
      },
      image = {
       description = "The cursor is using a custom image.",
       type = "value"
      },
      no = {
       description = "Slashed circle or crossbones.",
       type = "value"
      },
      sizeall = {
       description = "Four-pointed arrow pointing up, down, left, and right.",
       type = "value"
      },
      sizenesw = {
       description = "Double arrow pointing to the top-right and bottom-left.",
       type = "value"
      },
      sizens = {
       description = "Double arrow pointing up and down.",
       type = "value"
      },
      sizenwse = {
       description = "Double arrow pointing to the top-left and bottom-right.",
       type = "value"
      },
      sizewe = {
       description = "Double arrow pointing left and right.",
       type = "value"
      },
      wait = {
       description = "Wait graphic.",
       type = "value"
      },
      waitarrow = {
       description = "Small wait cursor with an arrow pointer.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    MouseConstant = {
     childs = {
      l = {
       description = "Left mouse button.",
       type = "value"
      },
      m = {
       description = "Middle mouse button.",
       type = "value"
      },
      r = {
       description = "Right mouse button.",
       type = "value"
      },
      wd = {
       description = "Mouse wheel down.",
       type = "value"
      },
      wu = {
       description = "Mouse wheel up.",
       type = "value"
      },
      x1 = {
       description = "Mouse X1 (also known as button 4).",
       type = "value"
      },
      x2 = {
       description = "Mouse X2 (also known as button 5).",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    getPosition = {
     args = "()",
     description = "Returns the current position of the mouse.",
     returns = "(x: number, y: number)",
     type = "function"
    },
    getX = {
     args = "()",
     description = "Returns the current x position of the mouse.",
     returns = "(x: number)",
     type = "function"
    },
    getY = {
     args = "()",
     description = "Returns the current y position of the mouse.",
     returns = "(y: number)",
     type = "function"
    },
    isDown = {
     args = "(...: MouseConstant)",
     description = "Checks whether a certain mouse button is down. This function does not detect mousewheel scrolling; you must use the love.mousepressed callback for that.",
     returns = "(anyDown: boolean)",
     type = "function"
    },
    isGrabbed = {
     args = "()",
     description = "Checks if the mouse is grabbed.",
     returns = "(grabbed: boolean)",
     type = "function"
    },
    isVisible = {
     args = "()",
     description = "Checks if the cursor is visible.",
     returns = "(visible: boolean)",
     type = "function"
    },
    newCursor = {
     args = "(cursortype: CursorType)",
     description = "Creates a new hardware Cursor object.\n\nHardware cursors are framerate-independent and work the same way as normal operating system cursors. Unlike drawing an image at the mouse's current coordinates, hardware cursors never have visible lag between when the mouse is moved and when the cursor position updates, even at low framerates.\n\nThe hot spot is the point the operating system uses to determine what was clicked and at what position the mouse cursor is. For example, the normal arrow pointer normally has its hot spot at the top left of the image, but a crosshair cursor might have it in the middle.",
     returns = "(cursor: Cursor)",
     type = "function"
    },
    setCursor = {
     args = "(cursor: Cursor)",
     description = "Sets the current mouse cursor.\n\nResets the current mouse cursor to the default when called without arguments.",
     returns = "()",
     type = "function"
    },
    setGrabbed = {
     args = "(grab: boolean)",
     description = "Grabs the mouse and confines it to the window.",
     returns = "()",
     type = "function"
    },
    setPosition = {
     args = "(x: number, y: number)",
     description = "Sets the position of the mouse.",
     returns = "()",
     type = "function"
    },
    setVisible = {
     args = "(visible: boolean)",
     description = "Sets the visibility of the cursor.",
     returns = "()",
     type = "function"
    },
    setX = {
     args = "(x: number)",
     description = "Sets the current X position of the mouse.",
     returns = "()",
     type = "function"
    },
    setY = {
     args = "(y: number)",
     description = "Sets the current Y position of the mouse.",
     returns = "()",
     type = "function"
    }
   },
   description = "Provides an interface to the user's mouse.",
   type = "class"
  },
  mousefocus = {
   args = "(f: boolean)",
   description = "Callback function triggered when window receives or loses mouse focus.",
   returns = "()",
   type = "function"
  },
  mousepressed = {
   args = "(x: number, y: number, button: MouseConstant)",
   description = "Callback function triggered when a mouse button is pressed.",
   returns = "()",
   type = "function"
  },
  mousereleased = {
   args = "(x: number, y: number, button: MouseConstant)",
   description = "Callback function triggered when a mouse button is released.",
   returns = "()",
   type = "function"
  },
  physics = {
   childs = {
    Body = {
     childs = {
      applyForce = {
       args = "(fx: number, fy: number, x: number, y: number)",
       description = "Apply force to a Body.\n\nA force pushes a body in a direction. A body with with a larger mass will react less. The reaction also depends on how long a force is applied: since the force acts continuously over the entire timestep, a short timestep will only push the body for a short time. Thus forces are best used for many timesteps to give a continuous push to a body (like gravity). For a single push that is independent of timestep, it is better to use Body:applyImpulse.\n\nIf the position to apply the force is not given, it will act on the center of mass of the body. The part of the force not directed towards the center of mass will cause the body to spin (and depends on the rotational inertia).\n\nNote that the force components and position must be given in world coordinates.",
       returns = "()",
       type = "function"
      },
      applyLinearImpulse = {
       args = "(ix: number, iy: number, x: number, y: number)",
       description = "Applies an impulse to a body. This makes a single, instantaneous addition to the body momentum.\n\nAn impulse pushes a body in a direction. A body with with a larger mass will react less. The reaction does not depend on the timestep, and is equivalent to applying a force continuously for 1 second. Impulses are best used to give a single push to a body. For a continuous push to a body it is better to use Body:applyForce.\n\nIf the position to apply the impulse is not given, it will act on the center of mass of the body. The part of the impulse not directed towards the center of mass will cause the body to spin (and depends on the rotational inertia).\n\nNote that the impulse components and position must be given in world coordinates.",
       returns = "()",
       type = "function"
      },
      applyTorque = {
       args = "(torque: number)",
       description = "Apply torque to a body.\n\nTorque is like a force that will change the angular velocity (spin) of a body. The effect will depend on the rotational inertia a body has.",
       returns = "()",
       type = "function"
      },
      destroy = {
       args = "()",
       description = "Explicitly destroys the Body. When you don't have time to wait for garbage collection, this function may be used to free the object immediately, but note that an error will occur if you attempt to use the object after calling this function.",
       returns = "()",
       type = "function"
      },
      getAngle = {
       args = "()",
       description = "Get the angle of the body.\n\nThe angle is measured in radians. If you need to transform it to degrees, use math.deg.\n\nA value of 0 radians will mean \"looking to the right\". Although radians increase counter-clockwise, the y-axis points down so it becomes clockwise from our point of view.",
       returns = "(angle: number)",
       type = "function"
      },
      getAngularDamping = {
       args = "()",
       description = "Gets the Angular damping of the Body\n\nThe angular damping is the rate of decrease of the angular velocity over time: A spinning body with no damping and no external forces will continue spinning indefinitely. A spinning body with damping will gradually stop spinning.\n\nDamping is not the same as friction - they can be modelled together. However, only damping is provided by Box2D (and LÖVE).\n\nDamping parameters should be between 0 and infinity, with 0 meaning no damping, and infinity meaning full damping. Normally you will use a damping value between 0 and 0.1.",
       returns = "(damping: number)",
       type = "function"
      },
      getAngularVelocity = {
       args = "()",
       description = "Get the angular velocity of the Body.\n\nThe angular velocity is the rate of change of angle over time.\n\nIt is changed in World:update by applying torques, off centre forces/impulses, and angular damping. It can be set directly with Body:setAngularVelocity.\n\nIf you need the rate of change of position over time, use Body:getLinearVelocity.",
       returns = "(w: number)",
       type = "function"
      },
      getFixtureList = {
       args = "()",
       description = "Returns a table with all fixtures.",
       returns = "(fixtures: table)",
       type = "function"
      },
      getGravityScale = {
       args = "()",
       description = "Returns the gravity scale factor.",
       returns = "(scale: number)",
       type = "function"
      },
      getInertia = {
       args = "()",
       description = "Gets the rotational inertia of the body.\n\nThe rotational inertia is how hard is it to make the body spin. It is set with the 4th argument to Body:setMass, or automatically with Body:setMassFromShapes.",
       returns = "(inertia: number)",
       type = "function"
      },
      getLinearDamping = {
       args = "()",
       description = "Gets the linear damping of the Body.\n\nThe linear damping is the rate of decrease of the linear velocity over time. A moving body with no damping and no external forces will continue moving indefinitely, as is the case in space. A moving body with damping will gradually stop moving.\n\nDamping is not the same as friction - they can be modelled together. However, only damping is provided by Box2D (and LÖVE).",
       returns = "(damping: number)",
       type = "function"
      },
      getLinearVelocity = {
       args = "()",
       description = "Gets the linear velocity of the Body from its center of mass.\n\nThe linear velocity is the rate of change of position over time.\n\nIf you need the rate of change of angle over time, use Body:getAngularVelocity. If you need to get the linear velocity of a point different from the center of mass:\n\nBody:getLinearVelocityFromLocalPoint allows you to specify the point in local coordinates.\nBody:getLinearVelocityFromWorldPoint allows you to specify the point in world coordinates.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getLinearVelocityFromLocalPoint = {
       args = "(x: number, y: number)",
       description = "Get the linear velocity of a point on the body.\n\nThe linear velocity for a point on the body is the velocity of the body center of mass plus the velocity at that point from the body spinning.\n\nThe point on the body must given in local coordinates. Use Body:getLinearVelocityFromWorldPoint to specify this with world coordinates.",
       returns = "(vx: number, vy: number)",
       type = "function"
      },
      getLinearVelocityFromWorldPoint = {
       args = "(x: number, y: number)",
       description = "Get the linear velocity of a point on the body.\n\nThe linear velocity for a point on the body is the velocity of the body center of mass plus the velocity at that point from the body spinning.\n\nThe point on the body must given in world coordinates. Use Body:getLinearVelocityFromLocalPoint to specify this with local coordinates.",
       returns = "(vx: number, vy: number)",
       type = "function"
      },
      getLocalCenter = {
       args = "()",
       description = "Get the center of mass position in local coordinates.\n\nUse Body:getWorldCenter to get the center of mass in world coordinates.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getLocalPoint = {
       args = "(world_x: number, world_y: number)",
       description = "Transform a point from world coordinates to local coordinates.",
       returns = "(local_x: number, local_y: number)",
       type = "function"
      },
      getLocalVector = {
       args = "(world_x: number, world_y: number)",
       description = "Transform a vector from world coordinates to local coordinates.",
       returns = "(local_x: number, local_y: number)",
       type = "function"
      },
      getMass = {
       args = "()",
       description = "Get the mass of the body.",
       returns = "(mass: number)",
       type = "function"
      },
      getMassData = {
       args = "()",
       description = "Returns the mass, its center, and the rotational inertia.",
       returns = "(x: number, y: number, mass: number, inertia: number)",
       type = "function"
      },
      getPosition = {
       args = "()",
       description = "Get the position of the body.\n\nNote that this may not be the center of mass of the body.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getType = {
       args = "()",
       description = "Returns the type of the body.",
       returns = "(type: BodyType)",
       type = "function"
      },
      getWorldCenter = {
       args = "()",
       description = "Get the center of mass position in world coordinates.\n\nUse Body:getLocalCenter to get the center of mass in local coordinates.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getWorldPoint = {
       args = "(local_x: number, local_y: number)",
       description = "Transform a point from local coordinates to world coordinates.",
       returns = "(world_x: number, world_y: number)",
       type = "function"
      },
      getWorldPoints = {
       args = "(x1: number, y1: number, x2: number, y2: number, ...: number)",
       description = "Transforms multiple points from local coordinates to world coordinates.",
       returns = "(x1: number, y1: number, x2: number, y2: number, ...: number)",
       type = "function"
      },
      getWorldVector = {
       args = "(local_x: number, local_y: number)",
       description = "Transform a vector from local coordinates to world coordinates.",
       returns = "(world_x: number, world_y: number)",
       type = "function"
      },
      getX = {
       args = "()",
       description = "Get the x position of the body in world coordinates.",
       returns = "(x: number)",
       type = "function"
      },
      getY = {
       args = "()",
       description = "Get the y position of the body in world coordinates.",
       returns = "(y: number)",
       type = "function"
      },
      isActive = {
       args = "()",
       description = "Returns whether the body is actively used in the simulation.",
       returns = "(status: boolean)",
       type = "function"
      },
      isAwake = {
       args = "()",
       description = "Returns the sleep status of the body.",
       returns = "(status: boolean)",
       type = "function"
      },
      isBullet = {
       args = "()",
       description = "Get the bullet status of a body.\n\nThere are two methods to check for body collisions:\n\nat their location when the world is updated (default)\nusing continuous collision detection (CCD)\n\nThe default method is efficient, but a body moving very quickly may sometimes jump over another body without producing a collision. A body that is set as a bullet will use CCD. This is less efficient, but is guaranteed not to jump when moving quickly.\n\nNote that static bodies (with zero mass) always use CCD, so your walls will not let a fast moving body pass through even if it is not a bullet.",
       returns = "(status: boolean)",
       type = "function"
      },
      isDynamic = {
       args = "()",
       description = "Get the dynamic status of the body.\n\nA static body has no mass and a constant position. It will not react to collisions. Often used for walls.\n\nA dynamic body has mass and can move. It will react to collisions when the world is updated.",
       returns = "(status: boolean)",
       type = "function"
      },
      isFixedRotation = {
       args = "()",
       description = "Returns whether the body rotation is locked.",
       returns = "(fixed: boolean)",
       type = "function"
      },
      isFrozen = {
       args = "()",
       description = "Get the frozen status of the body.\n\nA body becomes frozen when it goes outside the world boundary. A frozen body is no longer changed by World:update.",
       returns = "(status: boolean)",
       type = "function"
      },
      isSleepingAllowed = {
       args = "()",
       description = "Returns the sleeping behaviour of the body.",
       returns = "(status: boolean)",
       type = "function"
      },
      isStatic = {
       args = "()",
       description = "Get the static status of the body.\n\nA static body has no mass and a constant position. It will not react to collisions. Often used for walls.\n\nA dynamic body has mass and can move. It will react to collisions when the world is updated.",
       returns = "(status: boolean)",
       type = "function"
      },
      resetMassData = {
       args = "()",
       description = "Resets the mass of the body by recalculating it from the mass properties of the fixtures.",
       returns = "()",
       type = "function"
      },
      setActive = {
       args = "(active: boolean)",
       description = "Sets whether the body is active in the world.\n\nAn inactive body does not take part in the simulation. It will not move or cause any collisions.",
       returns = "()",
       type = "function"
      },
      setAngle = {
       args = "(angle: number)",
       description = "Set the angle of the body.\n\nThe angle is measured in radians. If you need to transform it from degrees, use math.rad.\n\nA value of 0 radians will mean \"looking to the right\". .Although radians increase counter-clockwise, the y-axis points down so it becomes clockwise from our point of view.\n\nIt is possible to cause a collision with another body by changing its angle.",
       returns = "()",
       type = "function"
      },
      setAngularDamping = {
       args = "(damping: number)",
       description = "Sets the angular damping of a Body.\n\nSee Body:getAngularDamping for a definition of angular damping.\n\nAngular damping can take any value from 0 to infinity. It is recommended to stay between 0 and 0.1, though. Other values will look unrealistic.",
       returns = "()",
       type = "function"
      },
      setAngularVelocity = {
       args = "(w: number)",
       description = "Sets the angular velocity of a Body.\n\nThe angular velocity is the rate of change of angle over time.\n\nThis function will not accumulate anything; any impulses previously applied since the last call to World:update will be lost.",
       returns = "()",
       type = "function"
      },
      setAwake = {
       args = "(awake: boolean)",
       description = "Wakes the body up or puts it to sleep.",
       returns = "()",
       type = "function"
      },
      setBullet = {
       args = "(status: boolean)",
       description = "Set the bullet status of a body.\n\nThere are two methods to check for body collisions:\n\nat their location when the world is updated (default)\nusing continuous collision detection (CCD)\n\nThe default method is efficient, but a body moving very quickly may sometimes jump over another body without producing a collision. A body that is set as a bullet will use CCD. This is less efficient, but is guaranteed not to jump when moving quickly.\n\nNote that static bodies (with zero mass) always use CCD, so your walls will not let a fast moving body pass through even if it is not a bullet.",
       returns = "()",
       type = "function"
      },
      setFixedRotation = {
       args = "(fixed: boolean)",
       description = "Set whether a body has fixed rotation.\n\nBodies with fixed rotation don't vary the speed at which they rotate.",
       returns = "()",
       type = "function"
      },
      setGravityScale = {
       args = "(scale: number)",
       description = "Sets a new gravity scale factor for the body.",
       returns = "()",
       type = "function"
      },
      setInertia = {
       args = "(inertia: number)",
       description = "Set the inertia of a body.\n\nThis value can also be set by the fourth argument of Body:setMass.",
       returns = "()",
       type = "function"
      },
      setLinearDamping = {
       args = "(ld: number)",
       description = "Sets the linear damping of a Body\n\nSee Body:getLinearDamping for a definition of linear damping.\n\nLinear damping can take any value from 0 to infinity. It is recommended to stay between 0 and 0.1, though. Other values will make the objects look \"floaty\".",
       returns = "()",
       type = "function"
      },
      setLinearVelocity = {
       args = "(x: number, y: number)",
       description = "Sets a new linear velocity for the Body.\n\nThis function will not accumulate anything; any impulses previously applied since the last call to World:update will be lost.",
       returns = "()",
       type = "function"
      },
      setMass = {
       args = "(mass: number)",
       description = "Sets the mass in kilograms.",
       returns = "()",
       type = "function"
      },
      setMassData = {
       args = "(x: number, y: number, mass: number, inertia: number)",
       description = "Overrides the calculated mass data.",
       returns = "()",
       type = "function"
      },
      setMassFromShapes = {
       args = "()",
       description = "Sets mass properties from attatched shapes.\n\nIf you feel that finding the correct mass properties is tricky, then this function may be able to help you. After creating the needed shapes on the Body, a call to this function will set the mass properties based on those shapes. Remember to call this function after adding the shapes.\n\nSetting the mass properties this way always results in a realistic (or at least good-looking) simulation, so using it is highly recommended.",
       returns = "()",
       type = "function"
      },
      setPosition = {
       args = "(x: number, y: number)",
       description = "Set the position of the body.\n\nNote that this may not be the center of mass of the body.",
       returns = "()",
       type = "function"
      },
      setSleepingAllowed = {
       args = "(allowed: boolean)",
       description = "Sets the sleeping behaviour of the body.",
       returns = "()",
       type = "function"
      },
      setType = {
       args = "(type: BodyType)",
       description = "Sets a new body type.",
       returns = "()",
       type = "function"
      },
      setX = {
       args = "(x: number)",
       description = "Set the x position of the body.",
       returns = "()",
       type = "function"
      },
      setY = {
       args = "(y: number)",
       description = "Set the y position of the body.",
       returns = "()",
       type = "function"
      }
     },
     description = "Bodies are objects with velocity and position.",
     type = "lib"
    },
    BodyType = {
     childs = {
      dynamic = {
       description = "Dynamic bodies collide with all bodies.",
       type = "value"
      },
      kinematic = {
       description = "Kinematic bodies only collide with dynamic bodies.",
       type = "value"
      },
      static = {
       description = "Static bodies do not move.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    ChainShape = {
     childs = {
      getPoint = {
       args = "(index: number)",
       description = "Returns a point of the shape.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getPoints = {
       args = "()",
       description = "Returns all points of the shape.",
       returns = "(x1: number, y1: number, x2: number, y2: number, ...: number)",
       type = "function"
      },
      getVertexCount = {
       args = "()",
       description = "Returns the number of vertices the shape has.",
       returns = "(count: number)",
       type = "function"
      },
      setNextVertex = {
       args = "(x: number, y: number)",
       description = "Sets a vertex that establishes a connection to the next shape.\n\nThis can help prevent unwanted collisions when a flat shape slides along the edge and moves over to the new shape.",
       returns = "()",
       type = "function"
      },
      setPreviousVertex = {
       args = "(x: number, y: number)",
       description = "Sets a vertex that establishes a connection to the previous shape.\n\nThis can help prevent unwanted collisions when a flat shape slides along the edge and moves over to the new shape.",
       returns = "()",
       type = "function"
      }
     },
     description = "A ChainShape consists of multiple line segments. It can be used to create the boundaries of your terrain. The shape does not have volume and can only collide with PolygonShape and CircleShape.\n\nUnlike the PolygonShape, the ChainShape does not have a vertices limit or has to form a convex shape, but self intersections are not supported.",
     type = "lib"
    },
    CircleShape = {
     childs = {
      getRadius = {
       args = "()",
       description = "Gets the radius of the circle shape.",
       returns = "(radius: number)",
       type = "function"
      },
      getWorldCenter = {
       args = "()",
       description = "Get the center of the circle in world coordinates.",
       returns = "(wx: number, wy: number)",
       type = "function"
      },
      setRadius = {
       args = "(radius: number)",
       description = "Sets the radius of the circle.",
       returns = "()",
       type = "function"
      }
     },
     description = "Circle extends Shape and adds a radius and a local position.",
     type = "lib"
    },
    Contact = {
     childs = {
      getNormal = {
       args = "()",
       description = "Get the normal vector between two shapes that are in contact.\n\nThis function returns the coordinates of a unit vector that points from the first shape to the second.",
       returns = "(nx: number, ny: number)",
       type = "function"
      },
      getPositions = {
       args = "()",
       description = "Returns the contact points of the two colliding fixtures. There can be one or two points.",
       returns = "(x1: number, y1: number, x2: number, y2: number)",
       type = "function"
      },
      getRestitution = {
       args = "()",
       description = "Get the restitution between two shapes that are in contact.",
       returns = "(restitution: number)",
       type = "function"
      },
      isEnabled = {
       args = "()",
       description = "Returns whether the contact is enabled. The collision will be ignored if a contact gets disabled in the post solve callback.",
       returns = "(enabled: boolean)",
       type = "function"
      },
      isTouching = {
       args = "()",
       description = "Returns whether the two colliding fixtures are touching each other.",
       returns = "(touching: boolean)",
       type = "function"
      },
      resetFriction = {
       args = "()",
       description = "Resets the contact friction to the mixture value of both fixtures.",
       returns = "()",
       type = "function"
      },
      resetRestitution = {
       args = "()",
       description = "Resets the contact restitution to the mixture value of both fixtures.",
       returns = "()",
       type = "function"
      },
      setEnabled = {
       args = "(enabled: boolean)",
       description = "Enables or disables the contact.",
       returns = "()",
       type = "function"
      },
      setFriction = {
       args = "(friction: number)",
       description = "Sets the contact friction.",
       returns = "()",
       type = "function"
      },
      setRestitution = {
       args = "(restitution: number)",
       description = "Sets the contact restitution.",
       returns = "()",
       type = "function"
      }
     },
     description = "Contacts are objects created to manage collisions in worlds.",
     type = "lib"
    },
    DistanceJoint = {
     childs = {
      getFrequency = {
       args = "()",
       description = "Gets the response speed.",
       returns = "(Hz: number)",
       type = "function"
      },
      getLength = {
       args = "()",
       description = "Gets the equilibrium distance between the two Bodies.",
       returns = "(l: number)",
       type = "function"
      },
      setDampingRatio = {
       args = "(ratio: number)",
       description = "Sets the damping ratio.",
       returns = "()",
       type = "function"
      },
      setFrequency = {
       args = "(Hz: number)",
       description = "Sets the response speed.",
       returns = "()",
       type = "function"
      },
      setLength = {
       args = "(l: number)",
       description = "Sets the equilibrium distance between the two Bodies.",
       returns = "()",
       type = "function"
      }
     },
     description = "Keeps two bodies at the same distance.",
     type = "lib"
    },
    EdgeShape = {
     args = "()",
     description = "A EdgeShape is a line segment. They can be used to create the boundaries of your terrain. The shape does not have volume and can only collide with PolygonShape and CircleShape.",
     returns = "()",
     type = "function"
    },
    Fixture = {
     childs = {
      getBody = {
       args = "()",
       description = "Returns the body to which the fixture is attached.",
       returns = "(body: Body)",
       type = "function"
      },
      getBoundingBox = {
       args = "(index: number)",
       description = "Returns the points of the fixture bounding box. In case the fixture has multiple childern a 1-based index can be specified.",
       returns = "(topLeftX: number, topLeftY: number, bottomRightX: number, bottomRightY: number)",
       type = "function"
      },
      getCategory = {
       args = "()",
       description = "Returns the categories the fixture belongs to.",
       returns = "(category1: number, category2: number, ...: number)",
       type = "function"
      },
      getDensity = {
       args = "()",
       description = "Returns the density of the fixture.",
       returns = "(density: number)",
       type = "function"
      },
      getFilterData = {
       args = "()",
       description = "Returns the filter data of the fixture. Categories and masks are encoded as the bits of a 16-bit integer.",
       returns = "(categories: number, mask: number, group: number)",
       type = "function"
      },
      getFriction = {
       args = "()",
       description = "Returns the friction of the fixture.",
       returns = "(friction: number)",
       type = "function"
      },
      getGroupIndex = {
       args = "()",
       description = "Returns the group the fixture belongs to. Fixtures with the same group will always collide if the group is positive or never collide if it's negative. The group zero means no group.\n\nThe groups range from -32768 to 32767.",
       returns = "(group: number)",
       type = "function"
      },
      getMask = {
       args = "()",
       description = "Returns the category mask of the fixture.",
       returns = "(mask1: number, mask2: number, ...: number)",
       type = "function"
      },
      getMassData = {
       args = "()",
       description = "Returns the mass, its center and the rotational inertia.",
       returns = "(x: number, y: number, mass: number, inertia: number)",
       type = "function"
      },
      getRestitution = {
       args = "()",
       description = "Returns the restitution of the fixture.",
       returns = "(restitution: number)",
       type = "function"
      },
      getShape = {
       args = "()",
       description = "Returns the shape of the fixture. This shape is a reference to the actual data used in the simulation. It's possible to change its values between timesteps.\n\nDo not call any functions on this shape after the parent fixture has been destroyed. This shape will point to an invalid memory address and likely cause crashes if you interact further with it.",
       returns = "(shape: Shape)",
       type = "function"
      },
      getUserData = {
       args = "()",
       description = "Returns the Lua value associated with this fixture.\n\nUse this function in one thread only.",
       returns = "(value: mixed)",
       type = "function"
      },
      isSensor = {
       args = "()",
       description = "Returns whether the fixture is a sensor.",
       returns = "(sensor: boolean)",
       type = "function"
      },
      rayCast = {
       args = "(x1: number, y1: number, x2: number, y1: number, maxFraction: number, childIndex: number)",
       description = "Casts a ray against the shape of the fixture and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned.\n\nThe ray starts on the first point of the input line and goes towards the second point of the line. The fourth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.\n\nThe childIndex parameter is used to specify which child of a parent shape, such as a ChainShape, will be ray casted. For ChainShapes, the index of 1 is the first edge on the chain. Ray casting a parent shape will only test the child specified so if you want to test every shape of the parent, you must loop through all of its children.\n\nThe world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.\n\nhitx, hity = x1 + (x2 - x1) * fraction, y1 + (y2 - y1) * fraction",
       returns = "(x: number, y: number, fraction: number)",
       type = "function"
      },
      setCategory = {
       args = "(category1: number, category2: number, ...: number)",
       description = "Sets the categories the fixture belongs to. There can be up to 16 categories represented as a number from 1 to 16.",
       returns = "()",
       type = "function"
      },
      setDensity = {
       args = "(density: number)",
       description = "Sets the density of the fixture. Call Body:resetMassData if this needs to take effect immediately.",
       returns = "()",
       type = "function"
      },
      setFilterData = {
       args = "(categories: number, mask: number, group: number)",
       description = "Sets the filter data of the fixture.\n\nGroups, categories, and mask can be used to define the collision behaviour of the fixture.\n\nIf two fixtures are in the same group they either always collide if the group is positive, or never collide if it's negative. Is the group zero or they do not match, then the contact filter checks if the fixtures select a category of the other fixture with their masks. The fixtures do not collide if that's not the case. If they do have each others categories selected, the return value of the custom contact filter will be used. They always collide if none was set.\n\nThere can be up to 16 categories. Categories and masks are encoded as the bits of a 16-bit integer.",
       returns = "()",
       type = "function"
      },
      setFriction = {
       args = "(friction: number)",
       description = "Sets the friction of the fixture.",
       returns = "()",
       type = "function"
      },
      setGroupIndex = {
       args = "(group: number)",
       description = "Sets the group the fixture belongs to. Fixtures with the same group will always collide if the group is positive or never collide if it's negative. The group zero means no group.\n\nThe groups range from -32768 to 32767.",
       returns = "()",
       type = "function"
      },
      setMask = {
       args = "(mask1: number, mask2: number, ...: number)",
       description = "Sets the category mask of the fixture. There can be up to 16 categories represented as a number from 1 to 16.\n\nThis fixture will collide with the fixtures that are in the selected categories if the other fixture also has a category of this fixture selected.",
       returns = "()",
       type = "function"
      },
      setRestitution = {
       args = "(restitution: number)",
       description = "Sets the restitution of the fixture.",
       returns = "()",
       type = "function"
      },
      setSensor = {
       args = "(sensor: boolean)",
       description = "Sets whether the fixture should act as a sensor.\n\nSensor do not produce collisions responses, but the begin and end callbacks will still be called for this fixture.",
       returns = "()",
       type = "function"
      },
      setUserData = {
       args = "(value: mixed)",
       description = "Associates a Lua value with the fixture.\n\nUse this function in one thread only.",
       returns = "()",
       type = "function"
      },
      testPoint = {
       args = "(x: number, y: number)",
       description = "Checks if a point is inside the shape of the fixture.",
       returns = "(isInside: boolean)",
       type = "function"
      }
     },
     description = "Fixtures attach shapes to bodies.",
     type = "lib"
    },
    FrictionJoint = {
     childs = {
      getMaxTorque = {
       args = "()",
       description = "Gets the maximum friction torque in Newton-meters.",
       returns = "(torque: number)",
       type = "function"
      },
      setMaxForce = {
       args = "(maxForce: number)",
       description = "Sets the maximum friction force in Newtons.",
       returns = "()",
       type = "function"
      },
      setMaxTorque = {
       args = "(torque: number)",
       description = "Sets the maximum friction torque in Newton-meters.",
       returns = "()",
       type = "function"
      }
     },
     description = "A FrictionJoint applies friction to a body.",
     type = "lib"
    },
    GearJoint = {
     args = "()",
     description = "Keeps bodies together in such a way that they act like gears.",
     returns = "()",
     type = "function"
    },
    Joint = {
     childs = {
      getAnchors = {
       args = "()",
       description = "Get the anchor points of the joint.",
       returns = "(x1: number, y1: number, x2: number, y2: number)",
       type = "function"
      },
      getCollideConnected = {
       args = "()",
       description = "Gets whether the connected Bodies collide.",
       returns = "(c: boolean)",
       type = "function"
      },
      getReactionForce = {
       args = "()",
       description = "Gets the reaction force on Body 2 at the joint anchor.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getReactionTorque = {
       args = "(invdt: number)",
       description = "Returns the reaction torque on the second body.",
       returns = "(torque: number)",
       type = "function"
      },
      getType = {
       args = "()",
       description = "Gets an string representing the type.",
       returns = "(type: JointType)",
       type = "function"
      },
      setCollideConnected = {
       args = "(collide: boolean)",
       description = "Sets whether the connected Bodies should collide with eachother.",
       returns = "()",
       type = "function"
      }
     },
     description = "Attach multiple bodies together to interact in unique ways.",
     type = "lib"
    },
    JointType = {
     childs = {
      distance = {
       description = "A DistanceJoint.",
       type = "value"
      },
      gear = {
       description = "A GearJoint.",
       type = "value"
      },
      mouse = {
       description = "A MouseJoint.",
       type = "value"
      },
      prismatic = {
       description = "A PrismaticJoint.",
       type = "value"
      },
      pulley = {
       description = "A PulleyJoint.",
       type = "value"
      },
      revolute = {
       description = "A RevoluteJoint.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    MouseJoint = {
     childs = {
      getFrequency = {
       args = "()",
       description = "Returns the frequency.",
       returns = "(freq: number)",
       type = "function"
      },
      getMaxForce = {
       args = "()",
       description = "Gets the highest allowed force.",
       returns = "(f: number)",
       type = "function"
      },
      getTarget = {
       args = "()",
       description = "Gets the target point.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      setDampingRatio = {
       args = "(ratio: number)",
       description = "Sets a new damping ratio.",
       returns = "()",
       type = "function"
      },
      setFrequency = {
       args = "(freq: number)",
       description = "Sets a new frequency.",
       returns = "()",
       type = "function"
      },
      setMaxForce = {
       args = "(f: number)",
       description = "Sets the highest allowed force.",
       returns = "()",
       type = "function"
      },
      setTarget = {
       args = "(x: number, y: number)",
       description = "Sets the target point.",
       returns = "()",
       type = "function"
      }
     },
     description = "For controlling objects with the mouse.",
     type = "lib"
    },
    PolygonShape = {
     args = "()",
     description = "Polygon is a convex polygon with up to 8 sides.",
     returns = "()",
     type = "function"
    },
    PrismaticJoint = {
     childs = {
      getJointSpeed = {
       args = "()",
       description = "Get the current joint angle speed.",
       returns = "(s: number)",
       type = "function"
      },
      getJointTranslation = {
       args = "()",
       description = "Get the current joint translation.",
       returns = "(t: number)",
       type = "function"
      },
      getLimits = {
       args = "()",
       description = "Gets the joint limits.",
       returns = "(lower: number, upper: number)",
       type = "function"
      },
      getLowerLimit = {
       args = "()",
       description = "Gets the lower limit.",
       returns = "(lower: number)",
       type = "function"
      },
      getMaxMotorForce = {
       args = "()",
       description = "Gets the maximum motor force.",
       returns = "(f: number)",
       type = "function"
      },
      getMotorForce = {
       args = "()",
       description = "Get the current motor force.",
       returns = "(f: number)",
       type = "function"
      },
      getMotorSpeed = {
       args = "()",
       description = "Gets the motor speed.",
       returns = "(s: number)",
       type = "function"
      },
      getUpperLimit = {
       args = "()",
       description = "Gets the upper limit.",
       returns = "(upper: number)",
       type = "function"
      },
      hasLimitsEnabled = {
       args = "()",
       description = "Checks whether the limits are enabled.",
       returns = "(enabled: boolean)",
       type = "function"
      },
      isMotorEnabled = {
       args = "()",
       description = "Checks whether the motor is enabled.",
       returns = "(enabled: boolean)",
       type = "function"
      },
      setLimits = {
       args = "(lower: number, upper: number)",
       description = "Sets the limits.",
       returns = "()",
       type = "function"
      },
      setLowerLimit = {
       args = "(lower: number)",
       description = "Sets the lower limit.",
       returns = "()",
       type = "function"
      },
      setMaxMotorForce = {
       args = "(f: number)",
       description = "Set the maximum motor force.",
       returns = "()",
       type = "function"
      },
      setMotorEnabled = {
       args = "(enable: boolean)",
       description = "Starts or stops the joint motor.",
       returns = "()",
       type = "function"
      },
      setMotorSpeed = {
       args = "(s: number)",
       description = "Sets the motor speed.",
       returns = "()",
       type = "function"
      },
      setUpperLimit = {
       args = "(upper: number)",
       description = "Sets the upper limit.",
       returns = "()",
       type = "function"
      }
     },
     description = "Restricts relative motion between Bodies to one shared axis.",
     type = "lib"
    },
    PulleyJoint = {
     childs = {
      getGroundAnchors = {
       args = "()",
       description = "Get the ground anchor positions in world coordinates.",
       returns = "(a1x: number, a1y: number, a2x: number, a2y: number)",
       type = "function"
      },
      getLengthA = {
       args = "()",
       description = "Get the current length of the rope segment attached to the first body.",
       returns = "(length: number)",
       type = "function"
      },
      getLengthB = {
       args = "()",
       description = "Get the current length of the rope segment attached to the second body.",
       returns = "(length: number)",
       type = "function"
      },
      getMaxLengths = {
       args = "()",
       description = "Get the maximum lengths of the rope segments.",
       returns = "(len1: number, len2: number)",
       type = "function"
      },
      getRatio = {
       args = "()",
       description = "Get the pulley ratio.",
       returns = "(ratio: number)",
       type = "function"
      },
      setConstant = {
       args = "(length: number)",
       description = "Set the total length of the rope.\n\nSetting a new length for the rope updates the maximum length values of the joint.",
       returns = "()",
       type = "function"
      },
      setMaxLengths = {
       args = "(max1: number, max2: number)",
       description = "Set the maximum lengths of the rope segments.\n\nThe physics module also imposes maximum values for the rope segments. If the parameters exceed these values, the maximum values are set instead of the requested values.",
       returns = "()",
       type = "function"
      },
      setRatio = {
       args = "(ratio: number)",
       description = "Set the pulley ratio.",
       returns = "()",
       type = "function"
      }
     },
     description = "Allows you to simulate bodies connected through pulleys.",
     type = "lib"
    },
    RevoluteJoint = {
     childs = {
      getJointAngle = {
       args = "()",
       description = "Get the current joint angle.",
       returns = "(angle: number)",
       type = "function"
      },
      getJointSpeed = {
       args = "()",
       description = "Get the current joint angle speed.",
       returns = "(s: number)",
       type = "function"
      },
      getLimits = {
       args = "()",
       description = "Gets the joint limits.",
       returns = "(lower: number, upper: number)",
       type = "function"
      },
      getLowerLimit = {
       args = "()",
       description = "Gets the lower limit.",
       returns = "(lower: number)",
       type = "function"
      },
      getMaxMotorTorque = {
       args = "()",
       description = "Gets the maximum motor force.",
       returns = "(f: number)",
       type = "function"
      },
      getMotorSpeed = {
       args = "()",
       description = "Gets the motor speed.",
       returns = "(s: number)",
       type = "function"
      },
      getMotorTorque = {
       args = "()",
       description = "Get the current motor force.",
       returns = "(f: number)",
       type = "function"
      },
      getUpperLimit = {
       args = "()",
       description = "Gets the upper limit.",
       returns = "(upper: number)",
       type = "function"
      },
      hasLimitsEnabled = {
       args = "()",
       description = "Checks whether limits are enabled.",
       returns = "(enabled: boolean)",
       type = "function"
      },
      isMotorEnabled = {
       args = "()",
       description = "Checks whether the motor is enabled.",
       returns = "(enabled: boolean)",
       type = "function"
      },
      setLimits = {
       args = "(lower: number, upper: number)",
       description = "Sets the limits.",
       returns = "()",
       type = "function"
      },
      setLowerLimit = {
       args = "(lower: number)",
       description = "Sets the lower limit.",
       returns = "()",
       type = "function"
      },
      setMaxMotorTorque = {
       args = "(f: number)",
       description = "Set the maximum motor force.",
       returns = "()",
       type = "function"
      },
      setMotorEnabled = {
       args = "(enable: boolean)",
       description = "Starts or stops the joint motor.",
       returns = "()",
       type = "function"
      },
      setMotorSpeed = {
       args = "(s: number)",
       description = "Sets the motor speed.",
       returns = "()",
       type = "function"
      },
      setUpperLimit = {
       args = "(upper: number)",
       description = "Sets the upper limit.",
       returns = "()",
       type = "function"
      }
     },
     description = "Allow two Bodies to revolve around a shared point.",
     type = "lib"
    },
    RopeJoint = {
     args = "()",
     description = "The RopeJoint enforces a maximum distance between two points on two bodies. It has no other effect.",
     returns = "()",
     type = "function"
    },
    Shape = {
     childs = {
      computeMass = {
       args = "(density: number)",
       description = "Computes the mass properties for the shape with the specified density.",
       returns = "(x: number, y: number, mass: number, inertia: number)",
       type = "function"
      },
      destroy = {
       args = "()",
       description = "Explicitly destroys the Shape. When you don't have time to wait for garbage collection, this function may be used to free the object immediately, but note that an error will occur if you attempt to use the object after calling this function.\nNote that Box2D doesn't allow destroying or creating shapes during collision callbacks.",
       returns = "()",
       type = "function"
      },
      getBody = {
       args = "()",
       description = "Get the Body the shape is attached to.",
       returns = "(body: Body)",
       type = "function"
      },
      getBoundingBox = {
       args = "()",
       description = "Gets the bounding box of the shape. This function can be used in a nested fashion with love.graphics.polygon.\n\nA bounding box is the smallest rectangle that encapsulates the entire polygon.\n\nVertexes are returned starting from the bottom-left in a clockwise fashion (bottom-left, top-left, top-right, bottom-right).",
       returns = "(x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, x4: number, y4: number)",
       type = "function"
      },
      getChildCount = {
       args = "()",
       description = "Returns the number of children the shape has.",
       returns = "(count: number)",
       type = "function"
      },
      getData = {
       args = "()",
       description = "Get the data set with setData.",
       returns = "(v: any)",
       type = "function"
      },
      getDensity = {
       args = "()",
       description = "Gets the density of the Shape.",
       returns = "(density: number)",
       type = "function"
      },
      getFilterData = {
       args = "()",
       description = "Gets the filter data of the Shape.",
       returns = "(categoryBits: number, maskBits: number, groupIndex: number)",
       type = "function"
      },
      getFriction = {
       args = "()",
       description = "Gets the friction of this shape.",
       returns = "(friction: number)",
       type = "function"
      },
      getType = {
       args = "()",
       description = "Gets a string representing the Shape. This function can be useful for conditional debug drawing.",
       returns = "(type: ShapeType)",
       type = "function"
      },
      rayCast = {
       args = "(x1: number, y1: number, x2: number, y2: number, maxFraction: number, tx: number, ty: number, tr: number, childIndex: number)",
       description = "Casts a ray against the shape and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned. The Shape can be transformed to get it into the desired position.\n\nThe ray starts on the first point of the input line and goes towards the second point of the line. The fourth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.\n\nThe childIndex parameter is used to specify which child of a parent shape, such as a ChainShape, will be ray casted. For ChainShapes, the index of 1 is the first edge on the chain. Ray casting a parent shape will only test the child specified so if you want to test every shape of the parent, you must loop through all of its children.\n\nThe world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.\n\nhitx, hity = x1 + (x2 - x1) * fraction, y1 + (y2 - y1) * fraction",
       returns = "(xn: number, yn: number, fraction: number)",
       type = "function"
      },
      setData = {
       args = "(v: any)",
       description = "Set data to be passed to the collision callback.\n\nWhen a shape collides, the value set here will be passed to the collision callback as one of the parameters. Typically, you would want to store a table reference here, but any value can be used.",
       returns = "()",
       type = "function"
      },
      setDensity = {
       args = "(density: number)",
       description = "Sets the density of a Shape. Do this before calling Body:setMassFromShapes.",
       returns = "()",
       type = "function"
      },
      setFilterData = {
       args = "(categoryBits: number, maskBits: number, groupIndex: number)",
       description = "Sets the filter data for a Shape.\n\nCollision filtering is a system for preventing collision between shapes. For example, say you make a character that rides a bicycle. You want the bicycle to collide with the terrain and the character to collide with the terrain, but you don't want the character to collide with the bicycle (because they must overlap). Box2D supports such collision filtering using categories and groups.",
       returns = "()",
       type = "function"
      },
      setFriction = {
       args = "(friction: number)",
       description = "Sets the friction of the shape. Friction determines how shapes react when they \"slide\" along other shapes. Low friction indicates a slippery surface, like ice, while high friction indicates a rough surface, like concrete. Range: 0.0 - 1.0.",
       returns = "()",
       type = "function"
      },
      testPoint = {
       args = "(x: number, y: number)",
       description = "Checks whether a point lies inside the shape. This is particularly useful for mouse interaction with the shapes. By looping through all shapes and testing the mouse position with this function, we can find which shapes the mouse touches.",
       returns = "(hit: boolean)",
       type = "function"
      },
      testSegment = {
       args = "(x1: number, y1: number, x2: number, y2: number)",
       description = "Checks whether a line segment intersects a shape. This function will either return the \"time\" of impact and the surface normal at the point of collision, or nil if the line does not intersect the shape. The \"time\" is a value between 0.0 and 1.0 and can be used to calculate where the collision occured.",
       returns = "(t: number, xn: number, yn: number)",
       type = "function"
      }
     },
     description = "Shapes are objects used to control mass and collisions.\n\nEvery shape is either a circle or a polygon, and is attached to a Body.",
     type = "lib"
    },
    ShapeType = {
     childs = {
      chain = {
       description = "The Shape is a ChainShape.",
       type = "value"
      },
      circle = {
       description = "The Shape is a CircleShape.",
       type = "value"
      },
      edge = {
       description = "The Shape is a EdgeShape.",
       type = "value"
      },
      polygon = {
       description = "The Shape is a PolygonShape.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    WeldJoint = {
     childs = {
      getFrequency = {
       args = "()",
       description = "Returns the frequency.",
       returns = "(freq: number)",
       type = "function"
      },
      setDampingRatio = {
       args = "(ratio: number)",
       description = "The new damping ratio.",
       returns = "()",
       type = "function"
      },
      setFrequency = {
       args = "(freq: number)",
       description = "Sets a new frequency.",
       returns = "()",
       type = "function"
      }
     },
     description = "A WeldJoint essentially glues two bodies together.",
     type = "lib"
    },
    World = {
     childs = {
      getBodyCount = {
       args = "()",
       description = "Get the number of bodies in the world.",
       returns = "(n: number)",
       type = "function"
      },
      getBodyList = {
       args = "()",
       description = "Returns a table with all bodies.",
       returns = "(bodies: table)",
       type = "function"
      },
      getCallbacks = {
       args = "()",
       description = "Returns functions for the callbacks during the world update.",
       returns = "(beginContact: function, endContact: function, preSolve: function, postSolve: function)",
       type = "function"
      },
      getContactCount = {
       args = "()",
       description = "Returns the number of contacts in the world.",
       returns = "(n: number)",
       type = "function"
      },
      getContactFilter = {
       args = "()",
       description = "Returns the function for collision filtering.",
       returns = "(contactFilter: function)",
       type = "function"
      },
      getContactList = {
       args = "()",
       description = "Returns a table with all contacts.",
       returns = "(contacts: table)",
       type = "function"
      },
      getGravity = {
       args = "()",
       description = "Get the gravity of the world.",
       returns = "(x: number, y: number)",
       type = "function"
      },
      getJointCount = {
       args = "()",
       description = "Get the number of joints in the world.",
       returns = "(n: number)",
       type = "function"
      },
      getJointList = {
       args = "()",
       description = "Returns a table with all joints.",
       returns = "(joints: table)",
       type = "function"
      },
      isLocked = {
       args = "()",
       description = "Returns if the world is updating its state.\n\nThis will return true inside the callbacks from World:setCallbacks.",
       returns = "(locked: boolean)",
       type = "function"
      },
      isSleepingAllowed = {
       args = "()",
       description = "Returns the sleep behaviour of the world.",
       returns = "(allowSleep: boolean)",
       type = "function"
      },
      queryBoundingBox = {
       args = "(topLeftX: number, topLeftY: number, bottomRightX: number, bottomRightY: number, callback: function)",
       description = "Calls a function for each fixture inside the specified area.",
       returns = "()",
       type = "function"
      },
      rayCast = {
       args = "(x1: number, y1: number, x2: number, y2: number, callback: function)",
       description = "Casts a ray and calls a function with the fixtures that intersect it. You cannot make any assumptions about the order of the callbacks.\n\nEach time the function gets called, 6 arguments get passed to it. The first is the fixture intersecting the ray. The second and third are the coordinates of the intersection point. The fourth and fifth is the surface normal vector of the shape edge. The sixth argument is the position of the intersection on the ray as a number from 0 to 1 (or even higher if the ray length was changed with the return value).\n\nThe ray can be controlled with the return value. A positive value sets a new ray length where 1 is the default value. A value of 0 terminates the ray. If the callback function returns -1, the intersection gets ignored as if it didn't happen.\n\nThere is a bug in 0.8.0 where the normal vector passed to the callback function gets scaled by love.physics.getMeter.",
       returns = "()",
       type = "function"
      },
      setCallbacks = {
       args = "(beginContact: function, endContact: function, preSolve: function, postSolve: function)",
       description = "Sets functions for the collision callbacks during the world update.\n\nFour Lua functions can be given as arguments. The value nil removes a function.\n\nWhen called, each function will be passed three arguments. The first two arguments are the colliding fixtures and the third argument is the Contact between them. The PostSolve callback additionally gets the normal and tangent impulse for each contact point.",
       returns = "()",
       type = "function"
      },
      setGravity = {
       args = "(x: number, y: number)",
       description = "Set the gravity of the world.",
       returns = "()",
       type = "function"
      },
      setSleepingAllowed = {
       args = "(allowSleep: boolean)",
       description = "Set the sleep behaviour of the world.\n\nA sleeping body is much more efficient to simulate than when awake.\n\nIf sleeping is allowed, any body that has come to rest will sleep.",
       returns = "()",
       type = "function"
      },
      update = {
       args = "(dt: number)",
       description = "Update the state of the world.",
       returns = "()",
       type = "function"
      }
     },
     description = "A world is an object that contains all bodies and joints.",
     type = "lib"
    },
    getMeter = {
     args = "()",
     description = "Get the scale of the world.\n\nThe world scale is the number of pixels per meter. Try to keep your shape sizes less than 10 times this scale.\n\nThis is important because the physics in Box2D is tuned to work well for objects of size 0.1m up to 10m. All physics coordinates are divided by this number for the physics calculations.",
     returns = "(scale: number)",
     type = "function"
    },
    newBody = {
     args = "(world: World, x: number, y: number, type: BodyType)",
     description = "Creates a new body.\n\nThere are three types of bodies. Static bodies do not move, have a infinite mass, and can be used for level boundaries. Dynamic bodies are the main actors in the simulation, they collide with everything. Kinematic bodies do not react to forces and only collide with dynamic bodies.\n\nThe mass of the body gets calculated when a Fixture is attached or removed, but can be changed at any time with Body:setMass or Body:resetMassData.",
     returns = "(body: Body)",
     type = "function"
    },
    newChainShape = {
     args = "(loop: boolean, x1: number, y1: number, x2: number, y2: number)",
     description = "Creates a chain shape.",
     returns = "(shape: ChainShape)",
     type = "function"
    },
    newCircleShape = {
     args = "(body: Body, x: number, y: number, radius: number)",
     description = "Create a new CircleShape at (x,y) in local coordinates.\n\nAnchors from the center of the shape by default.",
     returns = "(shape: CircleShape)",
     type = "function"
    },
    newDistanceJoint = {
     args = "(body1: Body, body2: Body, x1: number, y1: number, x2: number, y2: number, collideConnected: boolean)",
     description = "Create a distance joint between two bodies.\n\nThis joint constrains the distance between two points on two bodies to be constant. These two points are specified in world coordinates and the two bodies are assumed to be in place when this joint is created. The first anchor point is connected to the first body and the second to the second body, and the points define the length of the distance joint.",
     returns = "(joint: DistanceJoint)",
     type = "function"
    },
    newEdgeShape = {
     args = "(x1: number, y1: number, x2: number, y2: number)",
     description = "Creates a edge shape.",
     returns = "(shape: EdgeShape)",
     type = "function"
    },
    newFixture = {
     args = "(body: Body, shape: Shape, density: number)",
     description = "Creates and attaches a Fixture to a body.",
     returns = "(fixture: Fixture)",
     type = "function"
    },
    newFrictionJoint = {
     args = "(body1: Body, body2: Body, x: number, y: number, collideConnected: boolean)",
     description = "Create a friction joint between two bodies. A FrictionJoint applies friction to a body.",
     returns = "(joint: FrictionJoint)",
     type = "function"
    },
    newGearJoint = {
     args = "(joint1: Joint, joint2: Joint, ratio: number, collideConnected: boolean)",
     description = "Create a gear joint connecting two joints.\n\nThe gear joint connects two joints that must be either prismatic or revolute joints. Using this joint requires that the joints it uses connect their respective bodies to the ground and have the ground as the first body. When destroying the bodies and joints you must make sure you destroy the gear joint before the other joints.\n\nThe gear joint has a ratio the determines how the angular or distance values of the connected joints relate to each other. The formula coordinate1 + ratio * coordinate2 always has a constant value that is set when the gear joint is created.",
     returns = "(joint: Joint)",
     type = "function"
    },
    newMouseJoint = {
     args = "(body: Body, x: number, y: number)",
     description = "Create a joint between a body and the mouse.\n\nThis joint actually connects the body to a fixed point in the world. To make it follow the mouse, the fixed point must be updated every timestep (example below).\n\nThe advantage of using a MouseJoint instead of just changing a body position directly is that collisions and reactions to other joints are handled by the physics engine.",
     returns = "(joint: Joint)",
     type = "function"
    },
    newPolygonShape = {
     args = "(body: Body, ...: number)",
     description = "Creates a new PolygonShape.\nThis shape can have 8 vertices at most, and must form a convex shape.",
     returns = "(shape: PolygonShape)",
     type = "function"
    },
    newPrismaticJoint = {
     args = "(body1: Body, body2: Body, x: number, y: number, ax: number, ay: number, collideConnected: boolean)",
     description = "Create a prismatic joints between two bodies.\n\nA prismatic joint constrains two bodies to move relatively to each other on a specified axis. It does not allow for relative rotation. Its definition and operation are similar to a revolute joint, but with translation and force substituted for angle and torque.",
     returns = "(joint: PrismaticJoint)",
     type = "function"
    },
    newPulleyJoint = {
     args = "(body1: Body, body2: Body, gx1: number, gy1: number, gx2: number, gy2: number, x1: number, y1: number, x2: number, y2: number, ratio: number, collideConnected: boolean)",
     description = "Create a pulley joint to join two bodies to each other and the ground.\n\nThe pulley joint simulates a pulley with an optional block and tackle. If the ratio parameter has a value different from one, then the simulated rope extends faster on one side than the other. In a pulley joint the total length of the simulated rope is the constant length1 + ratio * length2, which is set when the pulley joint is created.\n\nPulley joints can behave unpredictably if one side is fully extended. It is recommended that the method setMaxLengths  be used to constrain the maximum lengths each side can attain.",
     returns = "(joint: Joint)",
     type = "function"
    },
    newRectangleShape = {
     args = "(body: Body, x: number, y: number, width: number, height: number, angle: number)",
     description = "Shorthand for creating rectangluar PolygonShapes.\n\nThe rectangle will be created at (x,y) in local coordinates.\n\nAnchors from the center of the shape by default.",
     returns = "(shape: PolygonShape)",
     type = "function"
    },
    newRevoluteJoint = {
     args = "(body1: Body, body2: Body, x: number, y: number, collideConnected: number)",
     description = "Creates a pivot joint between two bodies.\n\nThis joint connects two bodies to a point around which they can pivot.",
     returns = "(joint: Joint)",
     type = "function"
    },
    newRopeJoint = {
     args = "(body1: Body, body2: Body, x1: number, y1: number, x2: number, y2: number, maxLength: number, collideConnected: boolean)",
     description = "Create a joint between two bodies. Its only function is enforcing a max distance between these bodies.",
     returns = "(joint: RopeJoint)",
     type = "function"
    },
    newWeldJoint = {
     args = "(body1: Body, body2: Body, x: number, y: number, collideConnected: boolean)",
     description = "Create a friction joint between two bodies. A WeldJoint essentially glues two bodies together.",
     returns = "(joint: WeldJoint)",
     type = "function"
    },
    newWheelJoint = {
     args = "(body1: Body, body2: Body, x: number, y: number, ax: number, ay: number, collideConnected: boolean)",
     description = "Creates a wheel joint.",
     returns = "(joint: WheelJoint)",
     type = "function"
    },
    newWorld = {
     args = "(xg: number, yg: number, sleep: boolean)",
     description = "Creates a new World.",
     returns = "(world: World)",
     type = "function"
    },
    setMeter = {
     args = "(scale: number)",
     description = "Sets the pixels to meter scale factor.\n\nAll coordinates in the physics module are divided by this number and converted to meters, and it creates a convenient way to draw the objects directly to the screen without the need for graphics transformations.\n\nIt is recommended to create shapes no larger than 10 times the scale. This is important because Box2D is tuned to work well with shape sizes from 0.1 to 10 meters.\n\nlove.physics.setMeter does not apply retroactively to created objects. Created objects retain their meter coordinates but the scale factor will affect their pixel coordinates.",
     returns = "()",
     type = "function"
    }
   },
   description = "Can simulate 2D rigid body physics in a realistic manner. This module is based on Box2D, and this API corresponds to the Box2D API as closely as possible.",
   type = "class"
  },
  quit = {
   args = "()",
   description = "Callback function triggered when the game is closed.",
   returns = "(r: boolean)",
   type = "function"
  },
  resize = {
   args = "(w: number, h: number)",
   description = "Called when the window is resized, for example if the user resizes the window, or if love.window.setMode is called with an unsupported width or height in fullscreen and the window chooses the closest appropriate size.\n\nCalls to love.window.setMode will only trigger this event if the width or height of the window after the call doesn't match the requested width and height. This can happen if a fullscreen mode is requested which doesn't match any supported mode, or if the fullscreen type is 'desktop' and the requested width or height don't match the desktop resolution.",
   returns = "()",
   type = "function"
  },
  run = {
   args = "()",
   description = "The main function, containing the main loop. A sensible default is used when left out.",
   returns = "()",
   type = "function"
  },
  sound = {
   childs = {
    Decoder = {
     childs = {
      getChannels = {
       args = "()",
       description = "Returns the number of channels in the stream.",
       returns = "(channels: number)",
       type = "function"
      },
      getDuration = {
       args = "()",
       description = "Gets the duration of the sound data.",
       returns = "(duration: number)",
       type = "function"
      },
      getSampleRate = {
       args = "()",
       description = "Returns the sample rate of the Decoder.",
       returns = "(rate: number)",
       type = "function"
      }
     },
     description = "An object which can gradually decode a sound file.",
     type = "lib"
    },
    SoundData = {
     childs = {
      getChannels = {
       args = "()",
       description = "Returns the number of channels in the stream.",
       returns = "(channels: number)",
       type = "function"
      },
      getDuration = {
       args = "()",
       description = "Returns the number of channels in the stream.",
       returns = "(duration: number)",
       type = "function"
      },
      getSample = {
       args = "(i: number)",
       description = "Gets the sample at the specified position.",
       returns = "(sample: number)",
       type = "function"
      },
      getSampleCount = {
       args = "()",
       description = "Returns the sample count of the SoundData.",
       returns = "(count: number)",
       type = "function"
      },
      getSampleRate = {
       args = "()",
       description = "Returns the sample rate of the SoundData.",
       returns = "(rate: number)",
       type = "function"
      },
      setSample = {
       args = "(i: number, sample: number)",
       description = "Sets the sample at the specified position.",
       returns = "()",
       type = "function"
      }
     },
     description = "Contains raw audio samples. You can not play SoundData back directly. You must wrap a Source object around it.",
     type = "lib"
    },
    newSoundData = {
     args = "(filename: string)",
     description = "Creates new SoundData from a Decoder or file. It's also possible to create SoundData with a custom sample rate, channel and bit depth.\n\nThe sound data will be decoded to the memory in a raw format. It is recommended to create only short sounds like effects, as a 3 minute song uses 30 MB of memory this way.",
     returns = "(soundData: SoundData)",
     type = "function"
    }
   },
   description = "This module is responsible for decoding sound files. It can't play the sounds, see love.audio for that.",
   type = "class"
  },
  system = {
   childs = {
    PowerState = {
     childs = {
      battery = {
       description = "Not plugged in, running on a battery.",
       type = "value"
      },
      charged = {
       description = "Plugged in, battery is fully charged.",
       type = "value"
      },
      charging = {
       description = "Plugged in, charging battery.",
       type = "value"
      },
      nobattery = {
       description = "Plugged in, no battery available.",
       type = "value"
      },
      unknown = {
       description = "Cannot determine power status.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    getOS = {
     args = "()",
     description = "Gets the current operating system. In general, LÖVE abstracts away the need to know the current operating system, but there are a few cases where it can be useful (especially in combination with os.execute.)",
     returns = "(os_string: string)",
     type = "function"
    },
    getPowerInfo = {
     args = "()",
     description = "Gets information about the system's power supply.",
     returns = "(state: PowerState, percent: number, seconds: number)",
     type = "function"
    },
    getProcessorCount = {
     args = "()",
     description = "Gets the number of CPU cores in the system.\n\nThe number includes the threads reported if technologies such as Intel's Hyper-threading are enabled. For example, on a 4-core CPU with Hyper-threading, this function will return 8.",
     returns = "(cores: number)",
     type = "function"
    },
    setClipboardText = {
     args = "(text: string)",
     description = "Puts text in the clipboard.",
     returns = "()",
     type = "function"
    }
   },
   description = "Provides access to information about the user's system.",
   type = "lib"
  },
  textinput = {
   args = "(text: string)",
   description = "Called when text has been entered by the user. For example if shift-2 is pressed on an American keyboard layout, the text \"@\" will be generated.",
   returns = "()",
   type = "function"
  },
  thread = {
   childs = {
    Channel = {
     childs = {
      demand = {
       args = "()",
       description = "Retrieves the value of a Channel message and removes it from the message queue.\n\nThe value of the message can be a boolean, string, number, LÖVE userdata, or a simple flat table. It waits until a message is in the queue then returns the message value.",
       returns = "(value: value)",
       type = "function"
      },
      getCount = {
       args = "()",
       description = "Retrieves the number of messages in the thread Channel queue.",
       returns = "(count: number)",
       type = "function"
      },
      peek = {
       args = "()",
       description = "Retrieves the value of a Channel message, but leaves it in the queue.\n\nThe value of the message can be a boolean, string, number or a LÖVE userdata. It returns nil if there's no message in the queue.",
       returns = "(value: value)",
       type = "function"
      },
      pop = {
       args = "()",
       description = "Retrieves the value of a Channel message and removes it from the message queue.\n\nThe value of the message can be a boolean, string, number, LÖVE userdata, or a simple flat table. It returns nil if there are no messages in the queue.",
       returns = "(value: value)",
       type = "function"
      },
      push = {
       args = "()",
       description = "Send a message to the thread Channel.\n\nThe value of the message can be a boolean, string, number, LÖVE userdata, or a simple flat table. Foreign userdata (Lua's files, LuaSocket, ENet, ...), functions, and tables inside tables are not supported.",
       returns = "(value: value)",
       type = "function"
      },
      supply = {
       args = "()",
       description = "Send a message to the thread Channel and wait for a thread to accept it.\n\nThe value of the message can be a boolean, string, number, LÖVE userdata, or a simple flat table. Foreign userdata (Lua's files, LuaSocket, ENet, ...), functions, and tables inside tables are not supported.",
       returns = "(value: value)",
       type = "function"
      }
     },
     description = "A channel is a way to send and receive data to and from different threads.",
     type = "lib"
    },
    Thread = {
     childs = {
      start = {
       args = "()",
       description = "Starts the thread.\n\nThreads can be restarted after they have completed their execution.",
       returns = "()",
       type = "function"
      },
      wait = {
       args = "()",
       description = "Wait for a thread to finish. This call will block until the thread finishes.",
       returns = "()",
       type = "function"
      }
     },
     description = "A Thread is a chunk of code that can run in parallel with other threads.\n\nThreads will place all Lua errors in \"error\". To retrieve the error, call Thread:get('error') in the main thread.",
     type = "lib"
    },
    newChannel = {
     args = "()",
     description = "Create a new unnamed thread channel.\n\nOne use for them is to pass new unnamed channels to other threads via Channel:push",
     returns = "(channel: Channel)",
     type = "function"
    },
    newThread = {
     args = "(name: string, file: File)",
     description = "Creates a new Thread from a File or Data object.",
     returns = "(thread: Thread)",
     type = "function"
    }
   },
   description = "Allows you to work with threads.\n\nThreads are separate Lua environments, running in parallel to the main code. As their code runs separately, they can be used to compute complex operations without adversely affecting the frame rate of the main thread. However, as they are separate environments, they cannot access the variables and functions of the main thread, and communication between threads is limited.\n\nAll LOVE objects (userdata) are shared among threads so you'll only have to send their references across threads. You may run into concurrency issues if you manipulate an object on multiple threads at the same time.\n\nWhen a Thread is started, it only loads the love.thread module. Every other module has to be loaded with require.",
   type = "class"
  },
  threaderror = {
   args = "(thread: Thread, errorstr: string)",
   description = "Callback function triggered when a Thread encounters an error.",
   returns = "()",
   type = "function"
  },
  timer = {
   childs = {
    getDelta = {
     args = "()",
     description = "Returns the time between the last two frames.",
     returns = "(dt: number)",
     type = "function"
    },
    getFPS = {
     args = "()",
     description = "Returns the current frames per second.",
     returns = "(fps: number)",
     type = "function"
    },
    getTime = {
     args = "()",
     description = "Returns the value of a timer with an unspecified starting time. This function should only be used to calculate differences between points in time, as the starting time of the timer is unknown.",
     returns = "(time: number)",
     type = "function"
    },
    sleep = {
     args = "(s: number)",
     description = "Sleeps the program for the specified amount of time.",
     returns = "()",
     type = "function"
    },
    step = {
     args = "()",
     description = "Measures the time between two frames. Calling this changes the return value of love.timer.getDelta.",
     returns = "()",
     type = "function"
    }
   },
   description = "Provides an interface to the user's clock.",
   type = "lib"
  },
  update = {
   args = "(dt: number)",
   description = "Callback function triggered when a key is pressed.",
   returns = "()",
   type = "function"
  },
  visible = {
   args = "(v: boolean)",
   description = "Callback function triggered when window is minimized/hidden or unminimized by the user.",
   returns = "()",
   type = "function"
  },
  window = {
   childs = {
    FullscreenType = {
     childs = {
      desktop = {
       description = "Sometimes known as borderless fullscreen windowed mode. A borderless screen-sized window is created which sits on top of all desktop GUI elements (such as the Windows taskbar and the Mac OS X dock.) The window is automatically resized to match the dimensions of the desktop, and its size cannot be changed.",
       type = "value"
      },
      normal = {
       description = "Standard fullscreen mode. Changes the display mode (actual resolution) of the monitor.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
    },
    getFullscreen = {
     args = "()",
     description = "Gets whether the window is fullscreen.",
     returns = "(fullscreen: boolean, fstype: FullscreenType)",
     type = "function"
    },
    getFullscreenModes = {
     args = "()",
     description = "Gets a list of supported fullscreen modes.",
     returns = "(modes: table)",
     type = "function"
    },
    getHeight = {
     args = "()",
     description = "Gets the height of the window.",
     returns = "(height: number)",
     type = "function"
    },
    getIcon = {
     args = "()",
     description = "Gets the window icon.",
     returns = "(imagedata: ImageData)",
     type = "function"
    },
    getMode = {
     args = "()",
     description = "Returns the current display mode.",
     returns = "(width: number, height: number, fullscreen: boolean, vsync: boolean, fsaa: number)",
     type = "function"
    },
    getTitle = {
     args = "()",
     description = "Gets the window title.",
     returns = "(title: string)",
     type = "function"
    },
    getWidth = {
     args = "()",
     description = "Gets the width of the window.",
     returns = "(width: number)",
     type = "function"
    },
    hasFocus = {
     args = "()",
     description = "Checks if the game window has keyboard focus.",
     returns = "(focus: boolean)",
     type = "function"
    },
    hasMouseFocus = {
     args = "()",
     description = "Checks if the game window has mouse focus.",
     returns = "(focus: boolean)",
     type = "function"
    },
    isCreated = {
     args = "()",
     description = "Checks if the window has been created.",
     returns = "(created: boolean)",
     type = "function"
    },
    isVisible = {
     args = "()",
     description = "Checks if the game window is visible.\n\nThe window is considered visible if it's not minimized and the program isn't hidden.",
     returns = "(visible: boolean)",
     type = "function"
    },
    setFullscreen = {
     args = "(fullscreen: boolean, fstype: FullscreenType)",
     description = "Enters or exits fullscreen. The display to use when entering fullscreen is chosen based on which display the window is currently in, if multiple monitors are connected.\n\nIf fullscreen mode is entered and the window size doesn't match one of the monitor's display modes (in normal fullscreen mode) or the window size doesn't match the desktop size (in 'desktop' fullscreen mode), the window will be resized appropriately. The window will revert back to its original size again when fullscreen mode is exited using this function.",
     returns = "(success: boolean)",
     type = "function"
    },
    setIcon = {
     args = "(imagedata: ImageData)",
     description = "Sets the window icon until the game is quit. Not all operating systems support very large icon images.",
     returns = "(success: boolean)",
     type = "function"
    },
    setMode = {
     args = "(width: number, height: number, flags: table)",
     description = "Changes the display mode.\n\nIf width or height is 0, the width or height of the desktop will be used.",
     returns = "(success: boolean)",
     type = "function"
    },
    setTitle = {
     args = "(title: string)",
     description = "Sets the window title.",
     returns = "()",
     type = "function"
    }
   },
   description = "The primary responsibility for the love.graphics module is the drawing of lines, shapes, text, Images and other Drawable objects onto the screen. Its secondary responsibilities include loading external files (including Images and Fonts) into memory, creating specialized objects (such as ParticleSystems or Framebuffers) and managing screen geometry.\n\nLÖVE's coordinate system is rooted in the upper-left corner of the screen, which is at location (0, 0). The x-axis is horizontal: larger values are further to the right. The y-axis is vertical: larger values are further towards the bottom.\n\nIn many cases, you draw images or shapes in terms of their upper-left corner (See the picture above).\n\nMany of the functions are used to manipulate the graphics coordinate system, which is essentially the way coordinates are mapped to the display. You can change the position, scale, and even rotation in this way.",
   type = "lib"
  }
 },
 description = "Love2d modules, functions, and callbacks.",
 type = "lib"
}

do return {love = love} end

-- the following code is used to convert love_api.lua to a proper format
love = dofile('love_api.lua')

-- conversion script
local function convert(l)
  local function merge(...) -- merges tables into one table
    local r = {}
    for _,v in pairs({...}) do
      for _,e in pairs(v) do table.insert(r, e) end
    end
    return r
  end
  local function params(t) -- merges parameters and return results
    local r = {}
    for _,v in ipairs(t) do
      table.insert(r, v.name .. ': ' .. v.type)
    end
    return '(' .. table.concat(r, ", ") .. ')'
  end

  if l.modules then
    l.description = 'Love2d modules, functions, and callbacks.'
    l.type = "lib"
    l.childs = l.modules
    l.types = nil -- don't need types
    for _,v in pairs(l.callbacks or {}) do table.insert(l.childs, v) end
    l.callbacks = nil
    l.modules = nil
  end

  if not l.childs then return end

  for n,v in ipairs(l.childs) do
    if v.functions and #v.functions > 1 and #v.functions[1] == 0 then
      io.stderr:write("alternative signatured ignored for "..v.name..".\n")
      table.remove(v.functions, 1)
    end
    v.childs = merge(v.types, v.functions, v.constants, v.enums)
    if v.name then
      l.childs[v.name] = v
      v.name = nil
    end
    if #v.childs > 0 and v.childs[1] then
      if v.childs[1].returns then
        v.returns = params(v.childs[1].returns)
      end
      if v.childs[1].arguments then
        v.args = params(v.childs[1].arguments)
      end
    end
    local nochildren = #v.childs == 0 or #v.childs == 1 and #v.childs[1] == 0
      or v.returns or v.args
    v.type = nochildren and (v.functions and "function" or "value")
      or v.types and "class"
      or v.constants and "class"
      or v.functions and "lib"
      or "function"
    if v.constants then v.description = "class constants" end
    v.types = nil
    v.functions = nil
    v.constants = nil
    v.enums = nil
    v.supertypes = nil
    v.constructors = nil
    if nochildren then v.childs = nil end
    if v.type == "function" then
      v.args = v.args or '()'
      v.returns = v.returns or '()'
    end
    l.childs[n] = nil
    convert(v)
  end
  return l
end

package.path = package.path .. ';../../lualibs/?/?.lua;../../lualibs/?.lua'
package.cpath = package.cpath .. ';../../bin/clibs/?.dll'
print((require 'mobdebug').line(convert(love), {indent = ' ', comment = false}))
