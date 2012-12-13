-- Copyright 2011-12 Paul Kulchenko, ZeroBrane LLC

-- converted from love_api.lua (https://love2d.org/forums/viewtopic.php?f=3&t=1796&start=30)
-- (as of Nov 1, 2012)
-- the conversion script is at the bottom of this file
-- manually removed "linear clamped", "exponent clamped", and "inverse clamped"
-- values as those can't be entered through auto-complete.

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
      inverse = {
       description = "Inverse distance attenuation.",
       type = "value"
      },
      linear = {
       description = "Linear attenuation.",
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
      getDirection = {
       args = "()",
       description = "Gets the direction of the Source.",
       returns = "(x: number, y: number, z: number)",
       type = "function"
      },
      getDistance = {
       args = "()",
       description = "Returns the reference and maximum distance of the source.",
       returns = "(ref: number, max: number)",
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
       args = "(offset: number, unit: TimeUnit)",
       description = "Sets the currently playing position of the Source.",
       returns = "()",
       type = "function"
      },
      setDirection = {
       args = "(x: number, y: number, z: number)",
       description = "Sets the direction vector of the Source. A zero vector makes the source non-directional.",
       returns = "()",
       type = "function"
      },
      setDistance = {
       args = "(ref: number, max: number)",
       description = "Sets the reference and maximum distance of the source.",
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
       description = "Sets the velocity of the Source.\n\nThis does not change the position of the Source, but lets the application know how it has to calculate the doppler effect.",
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
    getDistanceModel = {
     args = "()",
     description = "Returns the distance attenuation model.",
     returns = "(model: DistanceModel)",
     type = "function"
    },
    getNumSources = {
     args = "()",
     description = "Gets the current number of simulatenous playing sources.",
     returns = "(numSources: number)",
     type = "function"
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
     args = "(filename: string, type: SourceType)",
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
    clear = {
     args = "()",
     description = "Clears the event queue.",
     returns = "()",
     type = "function"
    },
    poll = {
     args = "()",
     description = "Returns an iterator for messages in the event queue.",
     returns = "(i: function)",
     type = "function"
    },
    pump = {
     args = "()",
     description = "Pump events into the event queue. This is a low-level function, and is usually not called explicitly, but implicitly by love.event.poll() or love.event.wait().",
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
     description = "Like love.event.poll(), but blocks until there is an event in the queue.",
     returns = "(e: Event, a: mixed, b: mixed, c: mixed, d: mixed)",
     type = "function"
    }
   },
   description = "Manages events, like keypresses.",
   type = "lib"
  },
  filesystem = {
   childs = {
    File = {
     childs = {
      close = {
       args = "()",
       description = "Closes a file.",
       returns = "(success: boolean)",
       type = "function"
      },
      eof = {
       args = "()",
       description = "If the end-of-file has been reached",
       returns = "(eof: boolean)",
       type = "function"
      },
      getSize = {
       args = "()",
       description = "Returns the file size.",
       returns = "(size: number)",
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
       returns = "(contents: string)",
       type = "function"
      },
      seek = {
       args = "(position: number)",
       description = "Seek to a position in a file.",
       returns = "(success: boolean)",
       type = "function"
      },
      write = {
       args = "(data: string)",
       description = "Write data to a file.",
       returns = "(success: boolean)",
       type = "function"
      }
     },
     description = "Represents a file on the filesystem.",
     type = "lib"
    },
    FileData = {
     childs = {
      getExtension = {
       args = "()",
       description = "Gets the extension of the FileData.",
       returns = "(ext: string)",
       type = "function"
      },
      getFilename = {
       args = "()",
       description = "Gets the filename of the FileData.",
       returns = "(name: string)",
       type = "function"
      }
     },
     description = "Represents a file on the filesystem.",
     type = "lib"
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
    enumerate = {
     args = "(dir: string)",
     description = "Returns a table with the names of files and subdirectories in the directory in an undefined order.\n\nNote that this directory is relative to the love folder/archive being run. Absolute paths will not work.",
     returns = "(files: table)",
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
    getLastModified = {
     args = "(filename: string)",
     description = "Gets the last modification time of a file.",
     returns = "(modtime: number, errormsg: string)",
     type = "function"
    },
    getSaveDirectory = {
     args = "()",
     description = "Gets the full path to the designated save directory. This can be useful if you want to use the standard io library (or something else) to read or write in the save directory.",
     returns = "(dir: string)",
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
     returns = "(cwd: string)",
     type = "function"
    },
    init = {
     args = "()",
     description = "Initializes love.filesystem, will be called internally, so should not be used explicitly.",
     returns = "()",
     type = "function"
    },
    isDirectory = {
     args = "(filename: string)",
     description = "Check whether something is a directory.",
     returns = "(is_dir: boolean)",
     type = "function"
    },
    isFile = {
     args = "(filename: string)",
     description = "Check whether something is a file.",
     returns = "(is_file: boolean)",
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
    mkdir = {
     args = "(name: string)",
     description = "Creates a directory.",
     returns = "(ok: boolean)",
     type = "function"
    },
    newFile = {
     args = "(filename: string)",
     description = "Creates a new File object. It needs to be opened before it can be accessed.",
     returns = "(file: File)",
     type = "function"
    },
    newFileData = {
     args = "(contents: string, name: string, decoder: FileDecoder)",
     description = "Creates a new FileData object.",
     returns = "(data: FileData)",
     type = "function"
    },
    read = {
     args = "(name: string, size: number)",
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
     args = "(name: string)",
     description = "Sets the write directory for your game. Note that you can only set the name of the folder to store your files in, not the location.",
     returns = "()",
     type = "function"
    },
    setSource = {
     args = "()",
     description = "Sets the source of the game, where the code is present, can only be called once, done automatically.",
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
    newFontData = {
     args = "(rasterizer: Rasterizer)",
     description = "Creates a new FontData.",
     returns = "(fontData: FontData)",
     type = "function"
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
      clear = {
       args = "(red: number, green: number, blue: number, alpha: number)",
       description = "Clears content of a Canvas.\n\nWhen called without arguments, the Canvas will be cleared with color rgba = {0,0,0,0}, i.e. it will be fully transparent. If called with color parameters (be it numbers or a color table), the alpha component may be omitted in which case it defaults to 255 (fully opaque).",
       returns = "()",
       type = "function"
      },
      getFilter = {
       args = "()",
       description = "Gets the filter mode of the Canvas.",
       returns = "(min: FilterMode, mag: FilterMode)",
       type = "function"
      },
      getImageData = {
       args = "()",
       description = "Returns the image data stored in the Canvas. Think of it as taking a screenshot of the hidden screen that is the Canvas.",
       returns = "(data: ImageData)",
       type = "function"
      },
      getWrap = {
       args = "()",
       description = "Gets the wrapping properties of a Canvas.\n\nThis functions returns the currently set horizontal and vertical wrapping modes for the Canvas.",
       returns = "(horiz: WrapMode, vert: WrapMode)",
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
       args = "(horiz: WrapMode, vert: WrapMode)",
       description = "Sets the wrapping properties of a Canvas.\n\n This function sets the way the edges of a Canvas are treated if it is scaled or rotated. If the WrapMode is set to \"clamp\", the edge will not be interpolated. If set to \"repeat\", the edge will be interpolated with the pixels on the opposing side of the framebuffer.",
       returns = "()",
       type = "function"
      }
     },
     description = "A Canvas is used for off-screen rendering. Think of it as an invisible screen that you can draw to, but that will not be visible until you draw it to the actual visible screen. It is also known as \"render to texture\".\n\nBy drawing things that do not change position often (such as background items) to the Canvas, and then drawing the entire Canvas instead of each item, you can reduce the number of draw operations performed each frame.",
     type = "lib"
    },
    ColorMode = {
     childs = {
      modulate = {
       description = "Images (etc) will be affected by the current color.",
       type = "value"
      },
      replace = {
       description = "Replace color mode. Images (etc) will not be affected by current color.",
       type = "value"
      }
     },
     description = "class constants",
     type = "class"
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
      canvas = {
       description = "Support for Canvas.",
       type = "value"
      },
      npot = {
       description = "Support for textures with non-power-of-two textures.",
       type = "value"
      },
      pixeleffect = {
       description = "Support for PixelEffect.",
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
      getFilter = {
       args = "()",
       description = "Gets the filter mode for an image.",
       returns = "(min: FilterMode, mag: FilterMode)",
       type = "function"
      },
      getHeight = {
       args = "()",
       description = "Returns the height of the Image.",
       returns = "(h: number)",
       type = "function"
      },
      getWidth = {
       args = "()",
       description = "Returns the width of the Image.",
       returns = "(w: number)",
       type = "function"
      },
      getWrap = {
       args = "()",
       description = "Gets the wrapping properties of an Image.\n\nThis functions returns the currently set horizontal and vertical wrapping modes for the image.",
       returns = "(horiz: WrapMode, vert: WrapMode)",
       type = "function"
      },
      setFilter = {
       args = "(min: FilterMode, mag: FilterMode)",
       description = "Sets the filter mode for an image.",
       returns = "()",
       type = "function"
      },
      setWrap = {
       args = "(horiz: WrapMode, vert: WrapMode)",
       description = "Sets the wrapping properties of an Image.\n\nThis function sets the way an Image is repeated when it is drawn with a Quad that is larger than the image's extent. An image may be clamped or set to repeat in both horizontal and vertical directions. Clamped images appear only once, but repeated ones repeat as many times as there is room in the Quad.\n\nN.B. If you use a Quad that is larger than the image extent and do not use repeated tiling, there may be an unwanted visual effect of the image stretching all the way to fill the Quad. If this is the case, setting Image:getWrap(\"repeat\", \"repeat\") for all the images to be repeated, and using Quads of appropriate size will result in the best visual appearance.",
       returns = "()",
       type = "function"
      }
     },
     description = "Drawable image type.",
     type = "lib"
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
    ParticleSystem = {
     childs = {
      count = {
       args = "()",
       description = "Gets the amount of particles that are currently in the system.",
       returns = "(count: number)",
       type = "function"
      },
      getDirection = {
       args = "()",
       description = "Gets the direction of the particle emitter (in radians).",
       returns = "(direction: number)",
       type = "function"
      },
      getOffsetX = {
       args = "()",
       description = "Get the x coordinate of the particle rotation offset.",
       returns = "(xOffset: number)",
       type = "function"
      },
      getOffsetY = {
       args = "()",
       description = "Get the y coordinate of the particle rotation offset.",
       returns = "(yOffset: number)",
       type = "function"
      },
      getSpread = {
       args = "()",
       description = "Gets the amount of directional spread of the particle emitter (in radians).",
       returns = "(spread: number)",
       type = "function"
      },
      getX = {
       args = "()",
       description = "Gets the x-coordinate of the particle emitter's position.",
       returns = "(x: number)",
       type = "function"
      },
      getY = {
       args = "()",
       description = "Gets the y-coordinate of the particle emitter's position.",
       returns = "(y: number)",
       type = "function"
      },
      isActive = {
       args = "()",
       description = "Checks whether the particle system is actively emitting particles.",
       returns = "(active: boolean)",
       type = "function"
      },
      isEmpty = {
       args = "()",
       description = "Checks whether the particle system is empty of particles.",
       returns = "(empty: boolean)",
       type = "function"
      },
      isFull = {
       args = "()",
       description = "Checks whether the particle system is full of particles.",
       returns = "(full: boolean)",
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
      setGravity = {
       args = "(min: number, max: number)",
       description = "Sets the gravity affecting the particles (acceleration along the y-axis). Every particle created will have a gravity between min and max.",
       returns = "()",
       type = "function"
      },
      setLifetime = {
       args = "(life: number)",
       description = "Sets how long the particle system should emit particles (if -1 then it emits particles forever).",
       returns = "()",
       type = "function"
      },
      setOffset = {
       args = "(x: number, y: number)",
       description = "Set the offset position which the particle sprite is rotated around. If this function is not used, the particles rotate around their center.",
       returns = "()",
       type = "function"
      },
      setParticleLife = {
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
       args = "(min: number, max: number, variation: number)",
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
      setSprite = {
       args = "(sprite: Image)",
       description = "Sets the image which is to be emitted.",
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
    PixelEffect = {
     childs = {
      getWarnings = {
       args = "()",
       description = "Returns any warning messages from compiling the pixel effect code. This can be used for debugging your pixel effects if there's anything the graphics hardware doesn't like.",
       returns = "(warnings: string)",
       type = "function"
      },
      send = {
       args = "(name: string, number: number)",
       description = "Sends one or more values to a pixel effect using the specified name.\n\nThis function allows certain aspects of a pixel effect to be controlled by Lua code.",
       returns = "()",
       type = "function"
      }
     },
     description = "A PixelEffect is used for advanced hardware-accelerated pixel manipulation. These effects are written in a language based on GLSL (OpenGL Shading Language) with a few things simplified for easier coding.\n\nPotential uses for pixel effects include HDR/bloom, motion blur, grayscale/invert/sepia/any kind of color effect, reflection/refraction, distortions, and much more!",
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
     childs = {
      flip = {
       args = "(x: boolean, y: boolean)",
       description = "Flips this quad horizontally, vertically, or both.",
       returns = "()",
       type = "function"
      },
      getViewport = {
       args = "()",
       description = "Gets the current viewport of this Quad.",
       returns = "(x: number, y: number, w: number, h: number)",
       type = "function"
      },
      setViewport = {
       args = "(x: number, y: number, w: number, h: number)",
       description = "Sets the texture coordinates according to a viewport.",
       returns = "()",
       type = "function"
      }
     },
     description = "A quadrilateral with texture coordinate information.\n\nQuads can be used to select part of a texture to draw. In this way, one large texture atlas can be loaded, and then split up into sub-images.",
     type = "lib"
    },
    SpriteBatch = {
     childs = {
      add = {
       args = "(x: number, y: number, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
       description = "Add a sprite to the batch.",
       returns = "(id: number)",
       type = "function"
      },
      addq = {
       args = "()",
       description = "Add a Quad to the batch.",
       returns = "(id: number)",
       type = "function"
      },
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
      getImage = {
       args = "()",
       description = "Returns the image used by the SpriteBatch.",
       returns = "(image: Image)",
       type = "function"
      },
      set = {
       args = "()",
       description = "Changes a sprite in the batch. This requires the identifier returned by add and addq.",
       returns = "(id: number, x: number, y: number, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
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
      setq = {
       args = "()",
       description = "Changes a sprite with a quad in the batch. This requires the identifier returned by add and addq.",
       returns = "(id: number)",
       type = "function"
      },
      unbind = {
       args = "()",
       description = "Unbinds the SpriteBatch.",
       returns = "()",
       type = "function"
      }
     },
     description = "Using a single image, draw any number of identical copies of the image using a single call to love.graphics.draw(). This can be used, for example, to draw repeating copies of a single background image.\n\nA SpriteBatch can be even more useful when the underlying image is a Texture Atlas (a single image file containing many independent images); by adding Quads to the batch, different sub-images from within the atlas can be drawn.",
     type = "lib"
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
    arc = {
     args = "(mode: DrawMode, x: number, y: number, radius: number, angle1: number, angle2: number, segments: number)",
     description = "Draws an arc.",
     returns = "()",
     type = "function"
    },
    checkMode = {
     args = "(width: number, height: number, fullscreen: boolean)",
     description = "Checks if a display mode is supported.",
     returns = "(supported: boolean)",
     type = "function"
    },
    circle = {
     args = "(mode: DrawMode, x: number, y: number, radius: number, segments: number)",
     description = "Draws a circle.",
     returns = "()",
     type = "function"
    },
    clear = {
     args = "()",
     description = "Clears the screen to background color.\n\nThis function is called automatically before love.draw in the default love.run function. See the example in love.run for a typical use of this function.\n\nNote that the scissor area bounds the cleared region.",
     returns = "()",
     type = "function"
    },
    draw = {
     args = "(drawable: Drawable, x: number, y: number, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
     description = "Draws objects on screen. Drawable objects are loaded images, but may be other kinds of Drawable objects, such as a ParticleSystem.\n\nIn addition to simple drawing, the draw() function can rotate and scale the object at the same time, as well as offset the image (for example, to center the image at the chosen coordinates).\n\nlove.graphics.draw() anchors from the top left corner by default.\n\nYou can specify a negative value for sx or sy to flip the drawable horizontally or vertically.\n\nThe pivotal point is (x, y) on the screen and (ox, oy) in the internal coordinate system of the drawable object, before rotation and scaling. The object is scaled by (sx, sy), then rotated by r around the pivotal point.\n\nThe default ColorMode blends the current drawing color into the image, so you will often want to invoke love.graphics.setColorMode(\"replace\") before drawing images, to ensure that the drawn image matches the source image file.\n\nThe origin offset values are most often used to shift the images up and left by half of its height and width, so that (effectively) the specified x and y coordinates are where the center of the image will end up.",
     returns = "()",
     type = "function"
    },
    drawq = {
     args = "(image: Image, quad: Quad, x: number, y: number, r: number, sx: number, sy: number, ox: number, oy: number, kx: number, ky: number)",
     description = "Draw a Quad with the specified Image on screen.",
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
    getCaption = {
     args = "()",
     description = "Gets the window caption.",
     returns = "(caption: string)",
     type = "function"
    },
    getColor = {
     args = "()",
     description = "Gets the current color.",
     returns = "(r: number, g: number, b: number, a: number)",
     type = "function"
    },
    getColorMode = {
     args = "()",
     description = "Gets the color mode (which controls how images are affected by the current color).",
     returns = "(mode: ColorMode)",
     type = "function"
    },
    getDefaultImageFilter = {
     args = "()",
     description = "Returns the default scaling filters.",
     returns = "(min: FilterMode, mag: FilterMode)",
     type = "function"
    },
    getFont = {
     args = "()",
     description = "Gets the current Font object.",
     returns = "(font: Font)",
     type = "function"
    },
    getHeight = {
     args = "()",
     description = "Gets the height of the window.",
     returns = "(height: number)",
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
    getMaxPointSize = {
     args = "()",
     description = "Gets the max supported point size.",
     returns = "(size: number)",
     type = "function"
    },
    getMode = {
     args = "()",
     description = "Returns the current display mode.",
     returns = "(width: number, height: number, fullscreen: boolean, vsync: boolean, fsaa: number)",
     type = "function"
    },
    getModes = {
     args = "()",
     description = "Gets a list of supported fullscreen modes.",
     returns = "(modes: table)",
     type = "function"
    },
    getPixelEffect = {
     args = "()",
     description = "Returns the current PixelEffect. Returns nil if none is set.",
     returns = "(pixeleffect: PixelEffect)",
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
    getScissor = {
     args = "()",
     description = "Gets the current scissor box.",
     returns = "(x: number, y: number, width: number, height: number)",
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
    isCreated = {
     args = "()",
     description = "Checks if the window has been created.",
     returns = "(created: boolean)",
     type = "function"
    },
    isSupported = {
     args = "(supportN: GraphicsFeature)",
     description = "Checks if certain graphics functions can be used.\n\nOlder and low-end systems do not always support all graphics extensions.",
     returns = "(isSupported: boolean)",
     type = "function"
    },
    line = {
     args = "(x1: number, y1: number, x2: number, y2: number, ...: number)",
     description = "Draws lines between points.",
     returns = "()",
     type = "function"
    },
    newCanvas = {
     args = "()",
     description = "Creates a new Canvas object for offscreen rendering.",
     returns = "(canvas: Canvas)",
     type = "function"
    },
    newFont = {
     args = "(filename: string, size: number)",
     description = "Creates a new Font.",
     returns = "(font: Font)",
     type = "function"
    },
    newImage = {
     args = "(filename: string)",
     description = "Creates a new Image from a filepath or an opened File or an ImageData.",
     returns = "(image: Image)",
     type = "function"
    },
    newImageFont = {
     args = "(image: Image, glyphs: string)",
     description = "Creates a new font by loading a specifically formatted image. There can be up to 256 glyphs.\n\nExpects ISO 8859-1 encoding for the glyphs string.",
     returns = "(font: Font)",
     type = "function"
    },
    newParticleSystem = {
     args = "(image: Image, buffer: number)",
     description = "Creates a new ParticleSystem.",
     returns = "(system: ParticleSystem)",
     type = "function"
    },
    newPixelEffect = {
     args = "(code: string)",
     description = "Creates a new PixelEffect object for hardware-accelerated pixel level effects.\n\nA PixelEffect contains at least one function, named effect, which is the effect itself, but it can contain additional functions.\n\nvec4 effect( vec4 color, Image texture, vec2 texture_coords, vec2 pixel_coords )",
     returns = "(pixeleffect: PixelEffect)",
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
    newSpriteBatch = {
     args = "(image: Image, size: number)",
     description = "Creates a new SpriteBatch object.",
     returns = "(spriteBatch: SpriteBatch)",
     type = "function"
    },
    newStencil = {
     args = "(stencilFunction: function)",
     description = "Creates a new stencil.",
     returns = "(myStencil: function)",
     type = "function"
    },
    point = {
     args = "(x: number, y: number)",
     description = "Draws a point.\n\nThe pixel grid is actually offset to the center of each pixel. So to get clean pixels drawn use 0.5 + integer increments.",
     returns = "()",
     type = "function"
    },
    polygon = {
     args = "(mode: DrawMode, ...: number)",
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
     description = "Draws text on screen. If no Font is set, one will be created and set (once) if needed.\n\nWhen using translation and scaling functions while drawing text, this function assumes the scale occurs first. If you don't script with this in mind, the text won't be in the right position, or possibly even on screen.\n\nDrawing uses the current color, but only if the ColorMode is \"modulate\" (which is the default). If your text is displayed as white, it is probably because the color mode is \"replace\" (which is useful when drawing sprites). Change the color model to \"modulate\" before drawing.\n\nlove.graphics.print stops at the first '\000' (null) character. This can bite you if you are appending keystrokes to form your string, as some of those are multi-byte unicode characters which will likely contain null bytes.",
     returns = "()",
     type = "function"
    },
    printf = {
     args = "(text: string, x: number, y: number, limit: number, align: AlignMode)",
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
    quad = {
     args = "(mode: DrawMode, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, x4: number, y4: number)",
     description = "Draws a quadrilateral shape.",
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
     description = "Resets the current graphics settings.\n\nCalling reset makes the current drawing color white, the current background color black, the window title empty and removes any scissor settings. It sets the BlendMode to alpha and ColorMode to modulate. It also sets both the point and line drawing modes to smooth and their sizes to 1.0.",
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
     args = "(r: number, g: number, b: number, a: number)",
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
     args = "(canvas: Canvas)",
     description = "Captures drawing operations to a Canvas",
     returns = "()",
     type = "function"
    },
    setCaption = {
     args = "(caption: string)",
     description = "Sets the window caption.",
     returns = "()",
     type = "function"
    },
    setColor = {
     args = "(red: number, green: number, blue: number, alpha: number)",
     description = "Sets the color used for drawing.",
     returns = "()",
     type = "function"
    },
    setColorMode = {
     args = "(mode: ColorMode)",
     description = "Sets the color mode (which controls how images are affected by the current color).",
     returns = "()",
     type = "function"
    },
    setDefaultImageFilter = {
     args = "(min: FilterMode, mag: FilterMode)",
     description = "Sets the default scaling filters.",
     returns = "()",
     type = "function"
    },
    setFont = {
     args = "(font: Font)",
     description = "Set an already-loaded Font as the current font or create and load a new one from the file and size.\n\nIt's recommended that Font objects are created with love.graphics.newFont in the loading stage and then passed to this function in the drawing stage.",
     returns = "()",
     type = "function"
    },
    setIcon = {
     args = "(drawable: Drawable)",
     description = "Set window icon. This feature is not completely supported on Windows (apparently an SDL bug, not a LÖVE bug).\n\nThe icon should be a 32x32px png image.",
     returns = "()",
     type = "function"
    },
    setInvertedStencil = {
     args = "(stencilFunction: function)",
     description = "Defines an inverted stencil for the drawing operations or releases the active one.\n\nIt's the same as love.graphics.setStencil with the mask inverted.\n\nCalling the function without arguments releases the active stencil.",
     returns = "()",
     type = "function"
    },
    setLine = {
     args = "(width: number, style: LineStyle)",
     description = "Sets the line width and style.",
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
    setMode = {
     args = "(width: number, height: number, fullscreen: boolean, vsync: boolean, fsaa: number)",
     description = "Changes the display mode.\n\nIf width and height are both 0, setMode will use the resolution of the desktop.\n\nUnsets certain settings on images, like WrapMode.",
     returns = "(success: boolean)",
     type = "function"
    },
    setNewFont = {
     args = "(size: number)",
     description = "Creates and sets a new font.",
     returns = "(font: Font)",
     type = "function"
    },
    setPixelEffect = {
     args = "(pixeleffect: PixelEffect)",
     description = "Sets or resets a PixelEffect as the current pixel effect. All drawing operations until the next love.graphics.setPixelEffect will be drawn using the PixelEffect object specified.",
     returns = "()",
     type = "function"
    },
    setPoint = {
     args = "(size: number, style: PointStyle)",
     description = "Sets the point size and style.",
     returns = "()",
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
    setStencil = {
     args = "(stencilFunction: function)",
     description = "Defines or releases a stencil for the drawing operations.\n\nThe passed function draws to the stencil instead of the screen, creating an image with transparent and opaque pixels. While active, it is used to test where pixels will be drawn or discarded.\n\nCalling the function without arguments releases the active stencil.",
     returns = "()",
     type = "function"
    },
    shear = {
     args = "(kx: number, ky: number)",
     description = "Shears the coordinate system.",
     returns = "()",
     type = "function"
    },
    toggleFullscreen = {
     args = "()",
     description = "Toggles fullscreen.",
     returns = "(success: boolean)",
     type = "function"
    },
    translate = {
     args = "(dx: number, dy: number)",
     description = "Translates the coordinate system in two dimensions.\n\nWhen this function is called with two numbers, dx, and dy, all the following drawing operations take effect as if their x and y coordinates were x+dx and y+dy.\n\nScale and translate are not commutative operations, therefore, calling them in different orders will change the outcome.\n\nThis change lasts until love.draw exits or else a love.graphics.pop reverts to a previous love.graphics.push.\n\nTranslating using whole numbers will prevent tearing/blurring of images and fonts draw after translating.",
     returns = "()",
     type = "function"
    },
    triangle = {
     args = "(mode: DrawMode, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)",
     description = "Draws a triangle.",
     returns = "()",
     type = "function"
    }
   },
   description = "The primary responsibility for the love.graphics module is the drawing of lines, shapes, text, Images and other Drawable objects onto the screen. Its secondary responsibilities include loading external files (including Images and Fonts) into memory, creating specialized objects (such as ParticleSystems or Framebuffers) and managing screen geometry.\n\nLÖVE's coordinate system is rooted in the upper-left corner of the screen, which is at location (0, 0). The x-axis is horizontal: larger values are further to the right. The y-axis is vertical: larger values are further towards the bottom.\n\nIn many cases, you draw images or shapes in terms of their upper-left corner (See the picture above).\n\nMany of the functions are used to manipulate the graphics coordinate system, which is essentially the the way coordinates are mapped to the display. You can change the position, scale, and even rotation in this way.",
   type = "class"
  },
  image = {
   childs = {
    ImageData = {
     childs = {
      encode = {
       args = "(outFile: string)",
       description = "Encodes the ImageData and writes it to the save directory.",
       returns = "()",
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
      getString = {
       args = "()",
       description = "Gets the full ImageData as a string.",
       returns = "(pixels: string)",
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
      gif = {
       description = "GIF image format.",
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
    newImageData = {
     args = "(width: number, height: number)",
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
    JoystickConstant = {
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
    close = {
     args = "(joystick: number)",
     description = "Closes a joystick, i.e. stop using it for generating events and in query functions.",
     returns = "()",
     type = "function"
    },
    getAxes = {
     args = "(joystick: number)",
     description = "Returns the position of each axis.",
     returns = "(axisDir1: number, axisDir2: number, axisDirN: number)",
     type = "function"
    },
    getAxis = {
     args = "(joystick: number, axis: number)",
     description = "Returns the direction of the axis.",
     returns = "(direction: number)",
     type = "function"
    },
    getBall = {
     args = "(joystick: number, ball: number)",
     description = "Returns the change in ball position.",
     returns = "(dx: number, dy: number)",
     type = "function"
    },
    getHat = {
     args = "(joystick: number, hat: number)",
     description = "Returns the direction of a hat.",
     returns = "(direction: JoystickConstant)",
     type = "function"
    },
    getName = {
     args = "(joystick: number)",
     description = "Returns the name of a joystick.",
     returns = "(name: string)",
     type = "function"
    },
    getNumAxes = {
     args = "(joystick: number)",
     description = "Returns the number of axes on the joystick.",
     returns = "(axes: number)",
     type = "function"
    },
    getNumBalls = {
     args = "(joystick: number)",
     description = "Returns the number of balls on the joystick.",
     returns = "(balls: number)",
     type = "function"
    },
    getNumButtons = {
     args = "(joystick: number)",
     description = "Returns the number of buttons on the joystick.",
     returns = "(buttons: number)",
     type = "function"
    },
    getNumHats = {
     args = "(joystick: number)",
     description = "Returns the number of hats on the joystick.",
     returns = "(hats: number)",
     type = "function"
    },
    getNumJoysticks = {
     args = "()",
     description = "Returns how many joysticks are available.",
     returns = "(joysticks: number)",
     type = "function"
    },
    isDown = {
     args = "(joystick: number, button: number)",
     description = "Checks if a button on a joystick is pressed.",
     returns = "(down: boolean)",
     type = "function"
    },
    isOpen = {
     args = "(joystick: number)",
     description = "Checks if the joystick is open.",
     returns = "(open: boolean)",
     type = "function"
    },
    open = {
     args = "(joystick: number)",
     description = "Opens up a joystick to be used, i.e. makes it ready to use. By default joysticks that are available at the start of your game will be opened.",
     returns = "(open: boolean)",
     type = "function"
    }
   },
   description = "Provides an interface to the user's joystick.",
   type = "lib"
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
    getKeyRepeat = {
     args = "()",
     description = "Returns the delay and interval of key repeating.",
     returns = "(delay: number, interval: number)",
     type = "function"
    },
    isDown = {
     args = "(key: KeyConstant)",
     description = "Checks whether a certain key is down. Not to be confused with love.keypressed or love.keyreleased.",
     returns = "(down: boolean)",
     type = "function"
    },
    setKeyRepeat = {
     args = "(delay: number, interval: number)",
     description = "Enables key repeating and sets the delay and interval.",
     returns = "()",
     type = "function"
    }
   },
   description = "Provides an interface to the user's keyboard.",
   type = "lib"
  },
  keypressed = {
   args = "(key: KeyConstant, unicode: number)",
   description = "Callback function triggered when a key is pressed.",
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
  mouse = {
   childs = {
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
     args = "(button: MouseConstant)",
     description = "Checks whether a certain mouse button is down. This function does not detect mousewheel scrolling; you must use the love.mousepressed callback for that.",
     returns = "(down: boolean)",
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
    setGrab = {
     args = "(grab: boolean)",
     description = "Grabs the mouse and confines it to the window.",
     returns = "()",
     type = "function"
    },
    setPosition = {
     args = "(x: number, y: number)",
     description = "Sets the current position of the mouse.",
     returns = "()",
     type = "function"
    },
    setVisible = {
     args = "(visible: boolean)",
     description = "Sets the current visibility of the cursor.",
     returns = "()",
     type = "function"
    }
   },
   description = "Provides an interface to the user's mouse.",
   type = "lib"
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
      applyAngularImpulse = {
       args = "(impulse: number)",
       description = "Applies an angular impulse to a body. This makes a single, instantaneous addition to the body momentum.\n\nA body with with a larger mass will react less. The reaction does not depend on the timestep, and is equivalent to applying a force continuously for 1 second. Impulses are best used to give a single push to a body. For a continuous push to a body it is better to use Body:applyForce.",
       returns = "()",
       type = "function"
      },
      applyForce = {
       args = "(fx: number, fy: number)",
       description = "Apply force to a Body.\n\nA force pushes a body in a direction. A body with with a larger mass will react less. The reaction also depends on how long a force is applied: since the force acts continuously over the entire timestep, a short timestep will only push the body for a short time. Thus forces are best used for many timesteps to give a continuous push to a body (like gravity). For a single push that is independent of timestep, it is better to use Body:applyImpulse.\n\nIf the position to apply the force is not given, it will act on the center of mass of the body. The part of the force not directed towards the center of mass will cause the body to spin (and depends on the rotational inertia).\n\nNote that the force components and position must be given in world coordinates.",
       returns = "()",
       type = "function"
      },
      applyLinearImpulse = {
       args = "(ix: number, iy: number)",
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
      getAllowSleeping = {
       args = "()",
       description = "Return whether a body is allowed to sleep.\n\nA sleeping body is much more efficient to simulate than when awake.",
       returns = "(status: boolean)",
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
      isSleeping = {
       args = "()",
       description = "Get the sleeping status of a body.\n\nA sleeping body is much more efficient to simulate than when awake.\n\nIf sleeping is allowed, a body that has come to rest will sleep.",
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
      putToSleep = {
       args = "()",
       description = "Put the body to sleep.\n\nA sleeping body is much more efficient to simulate than when awake.\n\nThe body will wake up if another body collides with it, if a joint or contact attached to it is destroyed, or if Body:wakeUp is called.",
       returns = "()",
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
      setAllowSleeping = {
       args = "(permission: boolean)",
       description = "Set the sleep behaviour of a body.\n\nA sleeping body is much more efficient to simulate than when awake.\n\nIf sleeping is allowed, a body that has come to rest will sleep.",
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
       args = "(isFixed: boolean)",
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
       description = "Sets the mass properties directly.\n\nIf you're not sure what all this stuff means, you can use Body:setMassFromShapes after adding shapes instead.\n\nThe first two parameters will be the local coordinates of the Body's center of mass.\n\nThe third parameter is the mass, in kilograms.\n\nThe last parameter is the rotational inertia.",
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
      },
      wakeUp = {
       args = "()",
       description = "Wake a sleeping body up.\n\nA sleeping body is much more efficient to simulate than when awake\n\nA sleeping body will also wake up if another body collides with it or if a joint or contact attached to it is destroyed.",
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
      getChildEdge = {
       args = "(index: number)",
       description = "Returns a child of the shape as an EdgeShape.",
       returns = "(EdgeShape: number)",
       type = "function"
      },
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
      setPrevVertex = {
       args = "(x: number, y: number)",
       description = "Sets a vertex that establishes a connection to the previous shape.\n\nThis can help prevent unwanted collisions when a flat shape slides along the edge and moves over to the new shape.",
       returns = "()",
       type = "function"
      }
     },
     description = "A ChainShape consists of multiple line segments. It can be used to create the boundaries of your terrain. The shape does not have volume and can only collide with PolygonShape and CircleShape.\n\nUnlike the PolygonShape, the ChainShape does not have a vertices limit or has to form a convex shape, but self intersections are not supported. ",
     type = "lib"
    },
    CircleShape = {
     childs = {
      getLocalCenter = {
       args = "()",
       description = "Get the center of the circle in local coordinates.",
       returns = "(lx: number, ly: number)",
       type = "function"
      },
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
      getFriction = {
       args = "()",
       description = "Get the friction between two shapes that are in contact.",
       returns = "(friction: number)",
       type = "function"
      },
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
      getSeparation = {
       args = "()",
       description = "Get the separation between two shapes that are in contact.\n\nThe return value of this function is always zero or negative, with a negative value indicating overlap between the two shapes.",
       returns = "(distance: number)",
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
      getDamping = {
       args = "()",
       description = "Gets the damping ratio.",
       returns = "(ratio: number)",
       type = "function"
      },
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
      destroy = {
       args = "()",
       description = "Destroys the fixture",
       returns = "()",
       type = "function"
      },
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
       description = "Casts a ray against the shape of the fixture and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned.\n\nThe ray starts on the first point of the input line and goes towards the second point of the line. The fourth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.\n\nThe world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.\n\nThere is a bug in 0.8.0 where the normal vector returned by this function gets scaled by love.physics.getMeter.",
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
      getMaxForce = {
       args = "()",
       description = "Gets the maximum friction force in Newtons.",
       returns = "(force: number)",
       type = "function"
      },
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
     childs = {
      getRatio = {
       args = "()",
       description = "Get the ratio of a gear joint.",
       returns = "(ratio: number)",
       type = "function"
      },
      setRatio = {
       args = "(ratio: number)",
       description = "Set the ratio of a gear joint.",
       returns = "()",
       type = "function"
      }
     },
     description = "Keeps bodies together in such a way that they act like gears.",
     type = "lib"
    },
    Joint = {
     childs = {
      destroy = {
       args = "()",
       description = "Explicitly destroys the Joint. When you don't have time to wait for garbage collection, this function may be used to free the object immediately, but note that an error will occur if you attempt to use the object after calling this function.",
       returns = "()",
       type = "function"
      },
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
      getDampingRatio = {
       args = "()",
       description = "Returns the damping ratio.",
       returns = "(ratio: number)",
       type = "function"
      },
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
      enableLimit = {
       args = "(enable: boolean)",
       description = "Enables or disables the limits of the joint.",
       returns = "()",
       type = "function"
      },
      enableMotor = {
       args = "(enable: boolean)",
       description = "Starts or stops the joint motor.",
       returns = "()",
       type = "function"
      },
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
      isLimitEnabled = {
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
      getConstant = {
       args = "()",
       description = "Get the total length of the rope.",
       returns = "(length: number)",
       type = "function"
      },
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
      enableLimits = {
       args = "(enable: boolean)",
       description = "Enables or disables the joint limits.",
       returns = "()",
       type = "function"
      },
      enableMotor = {
       args = "(enable: boolean)",
       description = "Starts or stops the joint motor.",
       returns = "()",
       type = "function"
      },
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
      isLimitsEnabled = {
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
      setLimitsEnabled = {
       args = "(enable: boolean)",
       description = "Enables/disables the joint limit.",
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
      setMotorSpeed = {
       args = "(s: number)",
       description = "Sets the motor speed.",
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
      computeAABB = {
       args = "(tx: number, ty: number, tr: number, childIndex: number)",
       description = "Returns the points of the bounding box for the transformed shape.",
       returns = "(topLeftX: number, topLeftY: number, bottomRightX: number, bottomRightY: number)",
       type = "function"
      },
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
      getRestitution = {
       args = "()",
       description = "Gets the restitution of this shape.",
       returns = "(restitution: number)",
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
       description = "Casts a ray against the shape and returns the surface normal vector and the line position where the ray hit. If the ray missed the shape, nil will be returned. The Shape can be transformed to get it into the desired position.\n\nThe ray starts on the first point of the input line and goes towards the second point of the line. The fourth argument is the maximum distance the ray is going to travel as a scale factor of the input line length.\n\nThe world position of the impact can be calculated by multiplying the line vector with the third return value and adding it to the line starting point.\n\nThere is a bug in 0.8.0 where the normal vector returned by this function gets scaled by love.physics.getMeter.",
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
      setRestitution = {
       args = "(restitution: number)",
       description = "Sets the restitution of the shape. Restitution indicates the \"bounciness\" of the shape. High restitution can be used to model stuff like a rubber ball, while low restitution can be used for \"dull\" objects, like a bag of sand.\n\nA shape with a restitution of 0 will still bounce a little if the shape it collides with has a restitution higher than 0.",
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
      getDampingRatio = {
       args = "()",
       description = "Returns the damping ratio of the joint.",
       returns = "(ratio: number)",
       type = "function"
      },
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
       args = "()",
       description = "Sets a new frequency.",
       returns = "(freq: number)",
       type = "function"
      }
     },
     description = "A WeldJoint essentially glues two bodies together.",
     type = "lib"
    },
    World = {
     childs = {
      destroy = {
       args = "()",
       description = "Destroys the world, taking all bodies, joints, fixtures and their shapes with it.\n\nAn error will occur if you attempt to use any of the destroyed objects after calling this function.",
       returns = "()",
       type = "function"
      },
      getAllowSleeping = {
       args = "()",
       description = "Returns the sleep behaviour of the world.",
       returns = "(allowSleep: boolean)",
       type = "function"
      },
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
      isAllowSleep = {
       args = "()",
       description = "Get the sleep behaviour of the world.\n\nA sleeping body is much more efficient to simulate than when awake.\n\nIf sleeping is allowed, any body that has come to rest will sleep.",
       returns = "(permission: boolean)",
       type = "function"
      },
      isLocked = {
       args = "()",
       description = "Returns if the world is updating its state.\n\nThis will return true inside the callbacks from World:setCallbacks.",
       returns = "(locked: boolean)",
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
      setAllowSleeping = {
       args = "(allowSleep: boolean)",
       description = "Set the sleep behaviour of the world.\n\nA sleeping body is much more efficient to simulate than when awake.\n\nIf sleeping is allowed, any body that has come to rest will sleep.",
       returns = "()",
       type = "function"
      },
      setCallbacks = {
       args = "(add: function, persist: function, remove: function, result: function)",
       description = "Set functions to be called when shapes collide.\n\nFour Lua functions can be given as arguments. The value nil can be given for events that are uninteresting.\n\nWhen called, each function will be passed three arguments. The first two arguments (one for each shape) will pass data that has been set with Shape:setData (or nil). The third argument passes the Contact between the two shapes.\n\nUsing Shape:destroy when there is an active remove callback can lead to a crash. It is possible to work around this issue by only destroying object that are not in active contact with anything.",
       returns = "()",
       type = "function"
      },
      setGravity = {
       args = "(x: number, y: number)",
       description = "Set the gravity of the world.",
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
    getDistance = {
     args = "(fixture1: Fixture, fixture2: Fixture)",
     description = "Returns the two closest points between two fixtures and their distance.",
     returns = "(distance: number, x1: number, y1: number, x2: number, y2: number)",
     type = "function"
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
     description = "Create a new CircleShape at (x,y) in local coordinates.\n\nlove.physics.newCircleShape() anchors from the center of the shape by default.",
     returns = "(shape: CircleShape)",
     type = "function"
    },
    newDistanceJoint = {
     args = "(body1: Body, body2: Body, x1: number, y1: number, x2: number, y2: number, collideConnected: boolean)",
     description = "Create a distance joint between two bodies.\n\nThis joint constrains the distance between two points on two bodies to be constant. These two points are specified in world coordinates and the two bodies are assumed to be in place when this joint is created. The first anchor point is connected to the first body and the second to the second body, and the points define the length of the distance joint.",
     returns = "(joint: Joint)",
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
     description = "Shorthand for creating rectangluar PolygonShapes.\n\nThe rectangle will be created at (x,y) in local coordinates.\n\nlove.physics.newRectangleShape() anchors from the center of the shape by default.",
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
     description = "Sets the meter scale factor.\n\nAll coordinates in the physics module are divided by this number, creating a convenient way to draw the objects directly to the screen without the need for graphics transformations.\n\nIt is recommended to create shapes no larger than 10 times the scale. This is important because Box2D is tuned to work well with shape sizes from 0.1 to 10 meters.",
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
      getBits = {
       args = "()",
       description = "Returns the number of bits per sample.",
       returns = "(bitSize: number)",
       type = "function"
      },
      getChannels = {
       args = "()",
       description = "Returns the number of channels in the stream.",
       returns = "(channels: number)",
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
      getBits = {
       args = "()",
       description = "Returns the number of bits per sample.",
       returns = "(bitSize: number)",
       type = "function"
      },
      getChannels = {
       args = "()",
       description = "Returns the number of channels in the stream.",
       returns = "(channels: number)",
       type = "function"
      },
      getSample = {
       args = "(i: number)",
       description = "Gets the sample at the specified position.",
       returns = "(sample: number)",
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
    newDecoder = {
     args = "(file: File, buffer: number)",
     description = "Attempts to find a decoder for the encoded sound data in the specified file.",
     returns = "(decoder: Decoder)",
     type = "function"
    },
    newSoundData = {
     args = "(decoder: Decoder)",
     description = "Creates new SoundData from a Decoder or file. It's also possible to create SoundData with a custom sample rate, channel and bit depth.\n\nThe sound data will be decoded to the memory in a raw format. It is recommended to create only short sounds like effects, as a 3 minute song uses 30 MB of memory this way.",
     returns = "(soundData: SoundData)",
     type = "function"
    }
   },
   description = "This module is responsible for decoding sound files. It can't play the sounds, see love.audio for that.",
   type = "class"
  },
  thread = {
   childs = {
    Thread = {
     childs = {
      demand = {
       args = "(name: string)",
       description = "Receive a message from a thread. Wait for the message to exist before returning. (Can return nil in case of an error in the thread.)",
       returns = "(value: value)",
       type = "function"
      },
      get = {
       args = "(name: string)",
       description = "Get a value (cross-threads). Returns nil when a message is not in the message box.",
       returns = "(value: mixed)",
       type = "function"
      },
      getKeys = {
       args = "()",
       description = "Returns the names of all messages in a table.",
       returns = "(msgNames: table)",
       type = "function"
      },
      getName = {
       args = "()",
       description = "Get the name of a thread.",
       returns = "(name: string)",
       type = "function"
      },
      peek = {
       args = "(name: string)",
       description = "Receive a message from a thread, but leave it in the message box.",
       returns = "(value: value)",
       type = "function"
      },
      set = {
       args = "(name: string, value: mixed)",
       description = "Set a value (cross-threads).",
       returns = "()",
       type = "function"
      },
      start = {
       args = "()",
       description = "Starts the thread.",
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
     description = "A Thread is a chunk of code that can run in parallel with other threads.\n\nThreads will place all Lua errors in \"error\". To retrieve the error, call thread:receive('error') in the main thread.",
     type = "lib"
    },
    getThread = {
     args = "(name: string)",
     description = "Look for a thread and get its object.",
     returns = "(thread: Thread)",
     type = "function"
    },
    getThreads = {
     args = "()",
     description = "Get all threads.",
     returns = "(threads: table)",
     type = "function"
    },
    newThread = {
     args = "(name: string, filename: string)",
     description = "Creates a new Thread from a File or Data object.",
     returns = "(thread: Thread)",
     type = "function"
    }
   },
   description = "Allows you to work with threads.\n\nThreads are separate Lua environments, running in parallel to the main code. As their code runs separately, they can be used to compute complex operations without adversely affecting the frame rate of the main thread. However, as they are separate environments, they cannot access the variables and functions of the main thread, and communication between threads is limited\n\nWhen a Thread is started, it only loads the love.thread module. Every other module has to be loaded with require.\n\nThe love.graphics module has several restrictions and therefore should only be used in the main thread.",
   type = "class"
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
     description = "Returns the current frames per second.\n\nDisplaying the FPS with love.graphics.print or love.graphics.setCaption can have an impact on this value. Keep this in mind while benchmarking your game.",
     returns = "(fps: number)",
     type = "function"
    },
    getMicroTime = {
     args = "()",
     description = "Returns the value of a timer with an unspecified starting time. The time is accurate to the microsecond, and is limited to 24 hours.",
     returns = "(t: number)",
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
  }
 },
 description = "Love2d modules, functions, and callbacks.",
 type = "lib"
}

do return {love = love} end

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

print((require 'mobdebug').line(convert(love), {indent = ' ', comment = false}))
