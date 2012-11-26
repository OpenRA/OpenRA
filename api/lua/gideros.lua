-- Copyright 2011-12 Paul Kulchenko, ZeroBrane LLC

-- converted from Resources/gideros_annot.api.
-- the conversion script is at the bottom of this file.

return {
 Accelerometer = {
  childs = {
   getAcceleration = {
    args = "()",
    description = "Returns the 3-axis acceleration measured by the accelerometer",
    returns = "()",
    type = "function"
   },
   isAvailable = {
    args = "()",
    description = "Does the accelerometer available?",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "Creates a new Accelerometer object",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "Starts accelerometer updates",
    returns = "()",
    type = "function"
   },
   stop = {
    args = "()",
    description = "Starts accelerometer updates",
    returns = "()",
    type = "function"
   }
  }
 },
 AlertDialog = {
  childs = {
   hide = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   show = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   }
  }
 },
 Application = {
  childs = {
   CENTER = {
    type = "value"
   },
   CROP = {
    type = "value"
   },
   FIT_HEIGHT = {
    type = "value"
   },
   FIT_WIDTH = {
    type = "value"
   },
   LANDSCAPE_LEFT = {
    type = "value"
   },
   LANDSCAPE_RIGHT = {
    type = "value"
   },
   LETTERBOX = {
    type = "value"
   },
   NO_SCALE = {
    type = "value"
   },
   PIXEL_PERFECT = {
    type = "value"
   },
   PORTRAIT = {
    type = "value"
   },
   PORTRAIT_UPSIDE_DOWN = {
    type = "value"
   },
   STRETCH = {
    type = "value"
   }
  }
 },
 Bitmap = {
  childs = {
   getAnchorPoint = {
    args = "()",
    description = "Returns the x and y coordinates of the anchor point",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(texture)",
    description = "creates a new Bitmap object",
    returns = "()",
    type = "function"
   },
   setAnchorPoint = {
    args = "(x, y)",
    description = "Sets the anchor point",
    returns = "()",
    type = "function"
   },
   setTexture = {
    args = "(texture)",
    description = "Sets the texture",
    returns = "()",
    type = "function"
   },
   setTextureRegion = {
    args = "(textureRegion)",
    description = "Sets the texture region",
    returns = "()",
    type = "function"
   }
  }
 },
 Core = {
  childs = {
   class = {
    args = "([base])",
    description = "",
    returns = "()",
    type = "function"
   }
  }
 },
 Event = {
  childs = {
   ADDED_TO_STAGE = {
    type = "value"
   },
   APPLICATION_EXIT = {
    type = "value"
   },
   APPLICATION_RESUME = {
    type = "value"
   },
   APPLICATION_START = {
    type = "value"
   },
   APPLICATION_SUSPEND = {
    type = "value"
   },
   BEGIN_CONTACT = {
    type = "value"
   },
   COMPLETE = {
    type = "value"
   },
   END_CONTACT = {
    type = "value"
   },
   ENTER_FRAME = {
    type = "value"
   },
   ERROR = {
    type = "value"
   },
   KEY_DOWN = {
    type = "value"
   },
   KEY_UP = {
    type = "value"
   },
   MOUSE_DOWN = {
    type = "value"
   },
   MOUSE_MOVE = {
    type = "value"
   },
   MOUSE_UP = {
    type = "value"
   },
   POST_SOLVE = {
    type = "value"
   },
   PRE_SOLVE = {
    type = "value"
   },
   PROGRESS = {
    type = "value"
   },
   REMOVED_FROM_STAGE = {
    type = "value"
   },
   REQUEST_PRODUCTS_COMPLETE = {
    type = "value"
   },
   RESTORE_TRANSACTIONS_COMPLETE = {
    type = "value"
   },
   TIMER = {
    type = "value"
   },
   TIMER_COMPLETE = {
    type = "value"
   },
   TOUCHES_BEGIN = {
    type = "value"
   },
   TOUCHES_CANCEL = {
    type = "value"
   },
   TOUCHES_END = {
    type = "value"
   },
   TOUCHES_MOVE = {
    type = "value"
   },
   TRANSACTION = {
    type = "value"
   },
   getTarget = {
    args = "()",
    description = "Returns the element on which the event listener was registered",
    returns = "()",
    type = "function"
   },
   getType = {
    args = "()",
    description = "Returns the type of Event",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(type)",
    description = "creates a new Event object",
    returns = "()",
    type = "function"
   },
   stopPropagation = {
    args = "()",
    description = "Stops the propagation of the current event in the scene tree hierarchy",
    returns = "()",
    type = "function"
   }
  }
 },
 EventDispatcher = {
  childs = {
   addEventListener = {
    args = "(type, listener [, data])",
    description = "Registers a listener function",
    returns = "()",
    type = "function"
   },
   dispatchEvent = {
    args = "(event)",
    description = "Dispatches an event",
    returns = "()",
    type = "function"
   },
   hasEventListener = {
    args = "(type)",
    description = "Checks if the EventDispatcher object has a event listener",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "creates a new EventDispatcher object",
    returns = "()",
    type = "function"
   },
   removeEventListener = {
    args = "(type, listener [, data])",
    description = "Removes a listener function",
    returns = "()",
    type = "function"
   }
  }
 },
 Font = {
  childs = {
   new = {
    args = "(txtfile, imagefile [, filtering])",
    description = "creates a new Font object",
    returns = "()",
    type = "function"
   }
  }
 },
 FontBase = {
  childs = {}
 },
 Geolocation = {
  childs = {
   getAccuracy = {
    args = "()",
    description = "Returns the previously set desired accuracy",
    returns = "()",
    type = "function"
   },
   getThreshold = {
    args = "()",
    description = "Returns the previously set minimum distance threshold",
    returns = "()",
    type = "function"
   },
   isAvailable = {
    args = "()",
    description = "Does this device have the capability to determine current location?",
    returns = "()",
    type = "function"
   },
   isHeadingAvailable = {
    args = "()",
    description = "Does this device have the capability to determine heading?",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "Creates a new Geolocation object",
    returns = "()",
    type = "function"
   },
   setAccuracy = {
    args = "(accuracy)",
    description = "Sets the desired accuracy (in meters) of the location data",
    returns = "()",
    type = "function"
   },
   setThreshold = {
    args = "(threshold)",
    description = "Sets the minimum distance (in meters) threshold",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "Starts the generation of updates that report the current location and heading",
    returns = "()",
    type = "function"
   },
   startUpdatingHeading = {
    args = "()",
    description = "Starts the generation of updates that report the heading",
    returns = "()",
    type = "function"
   },
   startUpdatingLocation = {
    args = "()",
    description = "Starts the generation of updates that report the current location",
    returns = "()",
    type = "function"
   },
   stop = {
    args = "()",
    description = "Stops the generation of updates that report the current location and heading",
    returns = "()",
    type = "function"
   },
   stopUpdatingHeading = {
    args = "()",
    description = "Stops the generation of updates that report the heading",
    returns = "()",
    type = "function"
   },
   stopUpdatingLocation = {
    args = "()",
    description = "Stops the generation of updates that report the current location",
    returns = "()",
    type = "function"
   }
  }
 },
 Gyroscope = {
  childs = {
   getRotationRate = {
    args = "()",
    description = "Returns the rotation rate in radians per second",
    returns = "()",
    type = "function"
   },
   isAvailable = {
    args = "()",
    description = "Does the gyroscope available?",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "Creates a new Gyroscope object",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "Starts gyroscope updates",
    returns = "()",
    type = "function"
   },
   stop = {
    args = "()",
    description = "Stops gyroscope updates",
    returns = "()",
    type = "function"
   }
  }
 },
 KeyCode = {
  childs = {
   BACK = {
    type = "value"
   },
   CENTER = {
    type = "value"
   },
   DOWN = {
    type = "value"
   },
   L1 = {
    type = "value"
   },
   LEFT = {
    type = "value"
   },
   MENU = {
    type = "value"
   },
   R1 = {
    type = "value"
   },
   RIGHT = {
    type = "value"
   },
   SEARCH = {
    type = "value"
   },
   SELECT = {
    type = "value"
   },
   START = {
    type = "value"
   },
   UP = {
    type = "value"
   },
   X = {
    type = "value"
   }
  }
 },
 Matrix = {
  childs = {
   getElements = {
    args = "()",
    description = "Returns the elements of this matrix instance",
    returns = "()",
    type = "function"
   },
   getM11 = {
    args = "()",
    description = "Returns the value of the m11 component",
    returns = "()",
    type = "function"
   },
   getM12 = {
    args = "()",
    description = "Returns the value of the m12 component",
    returns = "()",
    type = "function"
   },
   getM21 = {
    args = "()",
    description = "Returns the value of the m21 component",
    returns = "()",
    type = "function"
   },
   getM22 = {
    args = "()",
    description = "Returns the value of the m22 component",
    returns = "()",
    type = "function"
   },
   getTx = {
    args = "()",
    description = "Returns the value of the tx component",
    returns = "()",
    type = "function"
   },
   getTy = {
    args = "()",
    description = "Returns the value of the ty component",
    returns = "()",
    type = "function"
   },
   new = {
    args = "([m11 [, m12 [, m21 [, m22 [, tx [, ty]]]]]])",
    description = "creates a new Matrix object",
    returns = "()",
    type = "function"
   },
   setElements = {
    args = "([m11 [, m12 [, m21 [, m22 [, tx [, ty]]]]]])",
    description = "Sets all 6 elements of this matrix instance",
    returns = "()",
    type = "function"
   },
   setM11 = {
    args = "(m11)",
    description = "Sets the value of the m11 component",
    returns = "()",
    type = "function"
   },
   setM12 = {
    args = "(m22)",
    description = "Sets the value of the m22 component",
    returns = "()",
    type = "function"
   },
   setTx = {
    args = "(tx)",
    description = "Sets the value of the tx component",
    returns = "()",
    type = "function"
   },
   setTy = {
    args = "(ty)",
    description = "Sets the value of the ty component",
    returns = "()",
    type = "function"
   }
  }
 },
 MovieClip = {
  childs = {
   clearAction = {
    args = "(frame)",
    description = "Clears the action at the specified frame",
    returns = "()",
    type = "function"
   },
   gotoAndPlay = {
    args = "(frame)",
    description = "Goes to the specified frame and starts playing",
    returns = "()",
    type = "function"
   },
   gotoAndStop = {
    args = "(frame)",
    description = "Goes to the specified frame and stops",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(timeline)",
    description = "creates a new MovieClip object",
    returns = "()",
    type = "function"
   },
   play = {
    args = "()",
    description = "Starts playing the movie clip",
    returns = "()",
    type = "function"
   },
   setGotoAction = {
    args = "(frame, destframe)",
    description = "Sets a \"go to\" action to the specified frame",
    returns = "()",
    type = "function"
   },
   setStopAction = {
    args = "(frame)",
    description = "Sets a \"stop\" action to the specified frame",
    returns = "()",
    type = "function"
   },
   stop = {
    args = "()",
    description = "Stops playing the movie clip",
    returns = "()",
    type = "function"
   }
  }
 },
 Shape = {
  childs = {
   EVEN_ODD = {
    type = "value"
   },
   NON_ZERO = {
    type = "value"
   },
   beginPath = {
    args = "()",
    description = "Resets the current path",
    returns = "()",
    type = "function"
   },
   clear = {
    args = "()",
    description = "Clears the graphics that were drawn to this Shape object, and resets fill and line style settings",
    returns = "()",
    type = "function"
   },
   closePath = {
    args = "()",
    description = "Marks the current subpath as closed, and starts a new subpath with a point the same as the start and end of the newly closed subpath",
    returns = "()",
    type = "function"
   },
   endPath = {
    args = "()",
    description = "Ends the current path and draws the geometry by using the specified line and fill styles",
    returns = "()",
    type = "function"
   },
   lineTo = {
    args = "(x, y)",
    description = "Adds the given point to the current subpath, connected to the previous one by a straight line.",
    returns = "()",
    type = "function"
   },
   moveTo = {
    args = "()",
    description = "Creates a new subpath with the given point",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "creates a new Shape object",
    returns = "()",
    type = "function"
   },
   setFillStyle = {
    args = "(type, ...)",
    description = "Sets the fill style that Shape object uses for subsequent drawings",
    returns = "()",
    type = "function"
   },
   setLineStyle = {
    args = "(width, color, alpha)",
    description = "Sets the line style that Shape object uses for subsequent drawings",
    returns = "()",
    type = "function"
   }
  }
 },
 Sound = {
  childs = {
   new = {
    args = "(filename)",
    description = "creates a new Sound object",
    returns = "()",
    type = "function"
   },
   play = {
    args = "(startTime, loops)",
    description = "Creates a new SoundChannel object to play the sound",
    returns = "()",
    type = "function"
   }
  }
 },
 SoundChannel = {
  childs = {
   getPosition = {
    args = "()",
    description = "Returns the position of the current playback",
    returns = "()",
    type = "function"
   },
   getVolume = {
    args = "()",
    description = "Returns the current volume of the sound channel",
    returns = "()",
    type = "function"
   },
   setVolume = {
    args = "(volume)",
    description = "Sets the volume of the sound channel",
    returns = "()",
    type = "function"
   },
   stop = {
    args = "()",
    description = "Stops the sound playing in the channel",
    returns = "()",
    type = "function"
   }
  }
 },
 Sprite = {
  childs = {
   ADD = {
    type = "value"
   },
   ALPHA = {
    type = "value"
   },
   MULTIPLY = {
    type = "value"
   },
   NO_ALPHA = {
    type = "value"
   },
   addChild = {
    args = "(child)",
    description = "Adds a sprite as a child",
    returns = "()",
    type = "function"
   },
   addChildAt = {
    args = "(child, index)",
    description = "Add a sprite as a child at the index position specified",
    returns = "()",
    type = "function"
   },
   clearBlendMode = {
    args = "()",
    description = "Clears the blending mode",
    returns = "()",
    type = "function"
   },
   contains = {
    args = "(child)",
    description = "Determines whether the specified sprite is contained in the subtree of this sprite",
    returns = "()",
    type = "function"
   },
   get = {
    args = "(param)",
    description = "Gets the specified property by its name",
    returns = "()",
    type = "function"
   },
   getAlpha = {
    args = "()",
    description = "Returns the alpha transparency of this sprite",
    returns = "()",
    type = "function"
   },
   getBounds = {
    args = "(targetSprite)",
    description = "Returns the bounds as it appears in another sprite’s coordinate system",
    returns = "()",
    type = "function"
   },
   getChildAt = {
    args = "(index)",
    description = "Returns the child sprite that exists at the specified index",
    returns = "()",
    type = "function"
   },
   getChildIndex = {
    args = "(sprite)",
    description = "Returns the index of the specified child sprite",
    returns = "()",
    type = "function"
   },
   getColorTransform = {
    args = "()",
    description = "Returns the red, green, blue and alpha channel multipliers",
    returns = "()",
    type = "function"
   },
   getHeight = {
    args = "()",
    description = "Returns the height",
    returns = "()",
    type = "function"
   },
   getMatrix = {
    args = "()",
    description = "Returns the transformation matrix of the sprite",
    returns = "()",
    type = "function"
   },
   getNumChildren = {
    args = "()",
    description = "Returns the number of children of this sprite",
    returns = "()",
    type = "function"
   },
   getParent = {
    args = "()",
    description = "Returns the parent sprite",
    returns = "()",
    type = "function"
   },
   getPosition = {
    args = "()",
    description = "Gets the x,y coordinates of the sprite",
    returns = "()",
    type = "function"
   },
   getRotation = {
    args = "()",
    description = "Returns the rotation of the sprite in degrees",
    returns = "()",
    type = "function"
   },
   getScale = {
    args = "()",
    description = "Returns the horizontal and vertical scales of the sprite",
    returns = "()",
    type = "function"
   },
   getScaleX = {
    args = "()",
    description = "Returns the horizontal scale of the sprite",
    returns = "()",
    type = "function"
   },
   getScaleY = {
    args = "()",
    description = "Returns the vertical scale of the sprite",
    returns = "()",
    type = "function"
   },
   getWidth = {
    args = "()",
    description = "Returns the width",
    returns = "()",
    type = "function"
   },
   getX = {
    args = "()",
    description = "Returns the x coordinate of the sprite",
    returns = "()",
    type = "function"
   },
   getY = {
    args = "()",
    description = "Returns the y coordinate of the sprite",
    returns = "()",
    type = "function"
   },
   globalToLocal = {
    args = "(x, y)",
    description = "Converts the x,y coordinates from the global to the sprite’s (local) coordinates",
    returns = "()",
    type = "function"
   },
   hitTestPoint = {
    args = "(x, y)",
    description = "Checks the given coordinates is in bounds of the sprite",
    returns = "()",
    type = "function"
   },
   isVisible = {
    args = "()",
    description = "Returns the visibility of sprite",
    returns = "()",
    type = "function"
   },
   localToGlobal = {
    args = "(x, y)",
    description = "Converts the x,y coordinates from the sprites’s (local) coordinates to the global coordinates",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "creates a new Sprite object",
    returns = "()",
    type = "function"
   },
   removeChild = {
    args = "(child)",
    description = "Removes the child sprite",
    returns = "()",
    type = "function"
   },
   removeChildAt = {
    args = "(index)",
    description = "Removes the child sprite at the specifed index",
    returns = "()",
    type = "function"
   },
   removeFromParent = {
    args = "()",
    description = "If the sprite has a parent, removes the sprite from the child list of its parent sprite.",
    returns = "()",
    type = "function"
   },
   set = {
    args = "(param, value)",
    description = "Sets the specified property by its name",
    returns = "()",
    type = "function"
   },
   setAlpha = {
    args = "(alpha)",
    description = "Sets the alpha transparency of this sprite",
    returns = "()",
    type = "function"
   },
   setBlendMode = {
    args = "(blendMode)",
    description = "Sets the blend mode of the sprite",
    returns = "()",
    type = "function"
   },
   setColorTransform = {
    args = "(redMultiplier, greenMultiplier, blueMultiplier, alphaMultiplier)",
    description = "Sets the red, green, blue and alpha channel multipliers",
    returns = "()",
    type = "function"
   },
   setMatrix = {
    args = "(matrix)",
    description = "Sets the transformation matrix of the sprite",
    returns = "()",
    type = "function"
   },
   setPosition = {
    args = "(x, y)",
    description = "Sets the x,y coordinates of the sprite",
    returns = "()",
    type = "function"
   },
   setRotation = {
    args = "(rotation)",
    description = "Sets the rotation of the sprite in degrees",
    returns = "()",
    type = "function"
   },
   setScale = {
    args = "(scaleX [, scaleY])",
    description = "Sets the horizontal and vertical scales of the sprite",
    returns = "()",
    type = "function"
   },
   setScaleX = {
    args = "(scaleX)",
    description = "Sets the horizontal scale of the sprite",
    returns = "()",
    type = "function"
   },
   setScaleY = {
    args = "(scaleY)",
    description = "Sets the vertical scale of the sprite",
    returns = "()",
    type = "function"
   },
   setVisible = {
    args = "(visible)",
    description = "Sets the visibility of sprite",
    returns = "()",
    type = "function"
   },
   setX = {
    args = "(x)",
    description = "Sets the x coordinate of the sprite",
    returns = "()",
    type = "function"
   },
   setY = {
    args = "(y)",
    description = "Sets the y coordinate of the sprite",
    returns = "()",
    type = "function"
   }
  }
 },
 Stage = {
  childs = {}
 },
 StoreKit = {
  childs = {
   FAILED = {
    type = "value"
   },
   PURCHASED = {
    type = "value"
   },
   RESTORED = {
    type = "value"
   },
   canMakePayments = {
    args = "()",
    description = "Returns whether the user is allowed to make payments",
    returns = "()",
    type = "function"
   },
   finishTransaction = {
    args = "(transaction)",
    description = "Completes a pending transaction",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "Creates a new StoreKit object",
    returns = "()",
    type = "function"
   },
   purchase = {
    args = "(productIdentifier [, quantity])",
    description = "Process a payment request",
    returns = "()",
    type = "function"
   },
   requestProducts = {
    args = "(productIdentifiers)",
    description = "Retrieve localized information about a list of products",
    returns = "()",
    type = "function"
   },
   restoreCompletedTransactions = {
    args = "()",
    description = "Restore previously completed purchases",
    returns = "()",
    type = "function"
   }
  }
 },
 TTFont = {
  childs = {
   new = {
    args = "(filename, size)",
    description = "creates a new TTFont object",
    returns = "()",
    type = "function"
   }
  }
 },
 TextField = {
  childs = {
   getLetterSpacing = {
    args = "()",
    description = "Returns the letter-spacing property which is used to increase or decrease the space between characters in a text",
    returns = "()",
    type = "function"
   },
   getText = {
    args = "()",
    description = "Returns the text displayed",
    returns = "()",
    type = "function"
   },
   getTextColor = {
    args = "()",
    description = "Returns the color of the text in a text field in hexadecimal format",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(font, text)",
    description = "creates a new TextField object with the specified font and text",
    returns = "()",
    type = "function"
   },
   setLetterSpacing = {
    args = "(spacing)",
    description = "Sets the letter-spacing property which is used to increase or decrease the space between characters in a text",
    returns = "()",
    type = "function"
   },
   setText = {
    args = "(text)",
    description = "Sets the text to be displayed",
    returns = "()",
    type = "function"
   },
   setTextColor = {
    args = "(color)",
    description = "Sets the color of the text in a text field in hexadecimal format",
    returns = "()",
    type = "function"
   }
  }
 },
 TextInputDialog = {
  childs = {
   getInputType = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   hide = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   isSecureInput = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   setInputType = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   setSecureInput = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   },
   show = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   }
  }
 },
 Texture = {
  childs = {
   new = {
    args = "(filename [, filtering [, options]])",
    description = "creates a new Texture object",
    returns = "()",
    type = "function"
   }
  }
 },
 TextureBase = {
  childs = {
   getHeight = {
    args = "()",
    description = "Returns the height of the texture in pixels",
    returns = "()",
    type = "function"
   },
   getWidth = {
    args = "()",
    description = "Returns the width of the texture in pixels",
    returns = "()",
    type = "function"
   }
  }
 },
 TexturePack = {
  childs = {
   getTextureRegion = {
    args = "(texturename)",
    description = "Returns the texture region of texture pack",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(txtfile, imagefile)",
    description = "creates a new TexturePack object",
    returns = "()",
    type = "function"
   }
  }
 },
 TextureRegion = {
  childs = {
   getRegion = {
    args = "()",
    description = "Returns the coordinates of the region",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(texture [, x, y, width, height])",
    description = "creates a new TextureRegion object",
    returns = "()",
    type = "function"
   },
   setRegion = {
    args = "(x, y, width, height)",
    description = "Sets the coordinates of the region",
    returns = "()",
    type = "function"
   }
  }
 },
 TileMap = {
  childs = {
   clearTile = {
    args = "(x, y)",
    description = "Set an empty tile at given indices",
    returns = "()",
    type = "function"
   },
   getTile = {
    args = "(x, y)",
    description = "Returns the index of the tile",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(width, height, texture, tilewidth, tileheight [, spacingx, spacingy, marginx, marginy])",
    description = "creates a new TileMap instance",
    returns = "()",
    type = "function"
   },
   setTile = {
    args = "(x, y, tx, ty)",
    description = "Sets the index of the tile",
    returns = "()",
    type = "function"
   },
   shift = {
    args = "(dx, dy)",
    description = "Shifts the tile map",
    returns = "()",
    type = "function"
   }
  }
 },
 Timer = {
  childs = {
   delayedCall = {
    args = "(delay, func [, data])",
    description = "delayed call a function after a set amount of time",
    returns = "()",
    type = "function"
   },
   getCurrentCount = {
    args = "()",
    description = "Returns the current trigger count of the timer",
    returns = "()",
    type = "function"
   },
   getDelay = {
    args = "()",
    description = "Returns the time interval between timer events in milliseconds",
    returns = "()",
    type = "function"
   },
   getRepeatCount = {
    args = "()",
    description = "Returns the number of repetitions the timer will make",
    returns = "()",
    type = "function"
   },
   isRunning = {
    args = "()",
    description = "Returns the current running status of timer",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(delay, repeatCount)",
    description = "creates a new Timer object",
    returns = "()",
    type = "function"
   },
   pauseAll = {
    args = "()",
    description = "pause all timers",
    returns = "()",
    type = "function"
   },
   reset = {
    args = "()",
    description = "Stops the timer and sets the currentCount property to 0",
    returns = "()",
    type = "function"
   },
   resumeAll = {
    args = "()",
    description = "resume all timers",
    returns = "()",
    type = "function"
   },
   setDelay = {
    args = "(delay)",
    description = "Sets the time interval between timer events in milliseconds",
    returns = "()",
    type = "function"
   },
   setRepeatCount = {
    args = "(repeatCount)",
    description = "Sets the number of repetitions the timer will make",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "Starts the timer",
    returns = "()",
    type = "function"
   },
   stop = {
    args = "()",
    description = "Stops the timer",
    returns = "()",
    type = "function"
   },
   stopAll = {
    args = "()",
    description = "stop all timers",
    returns = "()",
    type = "function"
   }
  }
 },
 UrlLoader = {
  childs = {
   DELETE = {
    type = "value"
   },
   GET = {
    type = "value"
   },
   POST = {
    type = "value"
   },
   PUT = {
    type = "value"
   },
   close = {
    args = "()",
    description = "Terminates the current loading operation",
    returns = "()",
    type = "function"
   },
   load = {
    args = "(url [, method [, body]])",
    description = "Loads data from the specified URL",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(url [, method [, body]])",
    description = "creates a new UrlLoader object",
    returns = "()",
    type = "function"
   }
  }
 },
 application = {
  childs = {
   exit = {
    args = "()",
    description = "Terminates the application",
    returns = "()",
    type = "function"
   },
   getBackgroundColor = {
    args = "()",
    description = "Returns the background color in hexadecimal format",
    returns = "()",
    type = "function"
   },
   getContentHeight = {
    args = "()",
    description = "Returns content height",
    returns = "()",
    type = "function"
   },
   getContentWidth = {
    args = "()",
    description = "Returns content width",
    returns = "()",
    type = "function"
   },
   getDeviceHeight = {
    args = "()",
    description = "Returns the physical height of the device in pixels",
    returns = "()",
    type = "function"
   },
   getDeviceInfo = {
    args = "()",
    description = "Returns information about device",
    returns = "()",
    type = "function"
   },
   getDeviceWidth = {
    args = "()",
    description = "Returns the physical width of the device in pixels",
    returns = "()",
    type = "function"
   },
   getFps = {
    args = "()",
    description = "Returns the frame rate of the application",
    returns = "()",
    type = "function"
   },
   getLanguage = {
    args = "()",
    description = "Returns the user language",
    returns = "()",
    type = "function"
   },
   getLocale = {
    args = "()",
    description = "Returns the device locale",
    returns = "()",
    type = "function"
   },
   getLogicalHeight = {
    args = "()",
    description = "Returns the logical height of the application",
    returns = "()",
    type = "function"
   },
   getLogicalScaleX = {
    args = "()",
    description = "Returns the scaling of automatic screen scaling on the x-axis",
    returns = "()",
    type = "function"
   },
   getLogicalScaleY = {
    args = "()",
    description = "Returns the scaling of automatic screen scaling on the y-axis",
    returns = "()",
    type = "function"
   },
   getLogicalTranslateX = {
    args = "()",
    description = "Returns the translation of automatic screen scaling on the x-axis ",
    returns = "()",
    type = "function"
   },
   getLogicalTranslateY = {
    args = "()",
    description = "Returns the translation of automatic screen scaling on the y-axis",
    returns = "()",
    type = "function"
   },
   getLogicalWidth = {
    args = "()",
    description = "Returns the logical width of the application",
    returns = "()",
    type = "function"
   },
   getOrientation = {
    args = "()",
    description = "Returns the orientation of the application",
    returns = "()",
    type = "function"
   },
   getScaleMode = {
    args = "()",
    description = "Returns the automatic scale mode of the application",
    returns = "()",
    type = "function"
   },
   openUrl = {
    args = "()",
    description = "Opens the given URL in the appropriate application",
    returns = "()",
    type = "function"
   },
   setBackgroundColor = {
    args = "(color)",
    description = "Sets the background color in hexadecimal format",
    returns = "()",
    type = "function"
   },
   setFps = {
    args = "(fps)",
    description = "Sets the frame rate of the application",
    returns = "()",
    type = "function"
   },
   setKeepAwake = {
    args = "(keepAwake)",
    description = "Enables/disables screen dimming and device sleeping",
    returns = "()",
    type = "function"
   },
   setLogicalDimensions = {
    args = "(width, height)",
    description = "Sets the logical dimensions of the application",
    returns = "()",
    type = "function"
   },
   setOrientation = {
    args = "(orientation)",
    description = "Sets the orientation of the application",
    returns = "()",
    type = "function"
   },
   setScaleMode = {
    args = "(scaleMode)",
    description = "Sets the automatic scale mode of the application",
    returns = "()",
    type = "function"
   },
   vibrate = {
    args = "()",
    description = "Vibrates the device",
    returns = "()",
    type = "function"
   }
  }
 },
 b2 = {
  childs = {
   Body = {
    childs = {
     applyAngularImpulse = {
      args = "(impulse)",
      description = "Applies an angular impulse",
      returns = "()",
      type = "function"
     },
     applyForce = {
      args = "(forcex, forcey, pointx, pointy)",
      description = "Applies a force at a world point",
      returns = "()",
      type = "function"
     },
     applyLinearImpulse = {
      args = "(impulsex, impulsey, pointx, pointy)",
      description = "Applies an impulse at a point",
      returns = "()",
      type = "function"
     },
     applyTorque = {
      args = "(torque)",
      description = "Applies a torque",
      returns = "()",
      type = "function"
     },
     createFixture = {
      args = "(fixtureDef)",
      description = "Creates a fixture and attach it to this body",
      returns = "()",
      type = "function"
     },
     destroyFixture = {
      args = "(fixture)",
      description = "Destroys a fixture",
      returns = "()",
      type = "function"
     },
     getAngle = {
      args = "()",
      description = "Returns the current world rotation angle in radians",
      returns = "()",
      type = "function"
     },
     getAngularDamping = {
      args = "()",
      description = "Returns the angular damping of the body",
      returns = "()",
      type = "function"
     },
     getAngularVelocity = {
      args = "()",
      description = "Returns the angular velocity",
      returns = "()",
      type = "function"
     },
     getGravityScale = {
      args = "()",
      description = "Returns the gravity scale of the body",
      returns = "()",
      type = "function"
     },
     getInertia = {
      args = "()",
      description = "Returns the rotational inertia of the body about the local origin in kg-m^2",
      returns = "()",
      type = "function"
     },
     getLinearDamping = {
      args = "()",
      description = "Returns the linear damping of the body",
      returns = "()",
      type = "function"
     },
     getLinearVelocity = {
      args = "()",
      description = "Returns the linear velocity of the center of mass",
      returns = "()",
      type = "function"
     },
     getLocalCenter = {
      args = "()",
      description = "Returns the local position of the center of mass",
      returns = "()",
      type = "function"
     },
     getMass = {
      args = "()",
      description = "Returns the total mass of the body in kilograms (kg)",
      returns = "()",
      type = "function"
     },
     getPosition = {
      args = "()",
      description = "Returns the world body origin position",
      returns = "()",
      type = "function"
     },
     getWorldCenter = {
      args = "()",
      description = "Returns the world position of the center of mass",
      returns = "()",
      type = "function"
     },
     isActive = {
      args = "()",
      description = "Returns the active state of the body",
      returns = "()",
      type = "function"
     },
     isAwake = {
      args = "()",
      description = "Returns the sleeping state of the body",
      returns = "()",
      type = "function"
     },
     setActive = {
      args = "(flag)",
      description = "Sets the active state of the body",
      returns = "()",
      type = "function"
     },
     setAngularDamping = {
      args = "(angularDamping)",
      description = "Sets the angular damping of the body",
      returns = "()",
      type = "function"
     },
     setAngularVelocity = {
      args = "(omega)",
      description = "Sets the angular velocity",
      returns = "()",
      type = "function"
     },
     setAwake = {
      args = "(awake)",
      description = "Sets the sleep state of the body",
      returns = "()",
      type = "function"
     },
     setGravityScale = {
      args = "(scale)",
      description = "Sets the gravity scale of the body",
      returns = "()",
      type = "function"
     },
     setLinearDamping = {
      args = "(linearDamping)",
      description = "Sets the linear damping of the body ",
      returns = "()",
      type = "function"
     },
     setLinearVelocity = {
      args = "(x, y)",
      description = "Sets the linear velocity of the center of mass",
      returns = "()",
      type = "function"
     },
     setPosition = {
      args = "(x, y)",
      description = "Sets the world body origin position",
      returns = "()",
      type = "function"
     }
    }
   },
   ChainShape = {
    childs = {
     createChain = {
      args = "(vertices)",
      description = "Creates a chain with isolated end vertices",
      returns = "()",
      type = "function"
     },
     createLoop = {
      args = "(vertices)",
      description = "Creates a loop",
      returns = "()",
      type = "function"
     },
     instance = {
      args = "()",
      description = "",
      returns = "()",
      type = "function"
     }
    }
   },
   CircleShape = {
    childs = {
     new = {
      args = "([centerx, centery, radius])",
      description = "creates a new b2.CircleShape instance",
      returns = "()",
      type = "function"
     },
     set = {
      args = "(centerx, centery, radius)",
      description = "Sets the center point and radius",
      returns = "()",
      type = "function"
     }
    }
   },
   DebugDraw = {
    childs = {
     appendFlags = {
      args = "(flags)",
      description = "Append flags to the current flags",
      returns = "()",
      type = "function"
     },
     clearFlags = {
      args = "(flags)",
      description = "Clear flags from the current flags",
      returns = "()",
      type = "function"
     },
     getFlags = {
      args = "()",
      description = "Returns the debug drawing flags",
      returns = "()",
      type = "function"
     },
     new = {
      args = "()",
      description = "creates a new b2.DebugDraw instance",
      returns = "()",
      type = "function"
     },
     setFlags = {
      args = "(flags)",
      description = "Sets the debug drawing flags",
      returns = "()",
      type = "function"
     }
    }
   },
   DistanceJoint = {
    childs = {
     getDampingRatio = {
      args = "()",
      description = "Returns the damping ratio",
      returns = "()",
      type = "function"
     },
     getFrequency = {
      args = "()",
      description = "Returns the mass-spring-damper frequency in Hertz",
      returns = "()",
      type = "function"
     },
     getLength = {
      args = "()",
      description = "Returns the length of this distance joint in meters",
      returns = "()",
      type = "function"
     },
     setDampingRatio = {
      args = "(ratio)",
      description = "Sets the damping ratio (0 = no damping, 1 = critical damping)",
      returns = "()",
      type = "function"
     },
     setFrequency = {
      args = "(frequency)",
      description = "Sets the mass-spring-damper frequency in Hertz",
      returns = "()",
      type = "function"
     },
     setLength = {
      args = "(length)",
      description = "Sets the natural joint length in meters",
      returns = "()",
      type = "function"
     }
    }
   },
   EdgeShape = {
    childs = {
     new = {
      args = "([v1x, v1y, v2x, v2y])",
      description = "creates a new b2.EdgeShape instance",
      returns = "()",
      type = "function"
     },
     set = {
      args = "(v1x, v1y, v2x, v2y)",
      description = "Sets the two vertices",
      returns = "()",
      type = "function"
     }
    }
   },
   Fixture = {
    childs = {
     getBody = {
      args = "()",
      description = "Returns the parent body of this fixture",
      returns = "()",
      type = "function"
     },
     getFilterData = {
      args = "()",
      description = "Returns the contact filtering data",
      returns = "()",
      type = "function"
     },
     isSensor = {
      args = "()",
      description = "Is this fixture a sensor (non-solid)?",
      returns = "()",
      type = "function"
     },
     setFilterData = {
      args = "(filterData)",
      description = "Sets the contact filtering data",
      returns = "()",
      type = "function"
     },
     setSensor = {
      args = "(sensor)",
      description = "Sets if this fixture is a sensor",
      returns = "()",
      type = "function"
     }
    }
   },
   FrictionJoint = {
    childs = {
     getMaxForce = {
      args = "()",
      description = "Returns the maximum friction force in N",
      returns = "()",
      type = "function"
     },
     getMaxTorque = {
      args = "()",
      description = "Returns the maximum friction torque in N*m",
      returns = "()",
      type = "function"
     },
     setMaxForce = {
      args = "(force)",
      description = "Sets the maximum friction force in N",
      returns = "()",
      type = "function"
     },
     setMaxTorque = {
      args = "(torque)",
      description = "Sets the maximum friction torque in N*m",
      returns = "()",
      type = "function"
     }
    }
   },
   GearJoint = {
    childs = {
     getRatio = {
      args = "()",
      description = "Returns the gear ratio",
      returns = "()",
      type = "function"
     },
     setRatio = {
      args = "(ratio)",
      description = "Sets the gear ratio",
      returns = "()",
      type = "function"
     }
    }
   },
   Joint = {
    childs = {
     getAnchorA = {
      args = "()",
      description = "Returns the anchor point on bodyA in world coordinates",
      returns = "()",
      type = "function"
     },
     getAnchorB = {
      args = "()",
      description = "Returns the anchor point on bodyB in world coordinates",
      returns = "()",
      type = "function"
     },
     getBodyA = {
      args = "()",
      description = "Returns the first body attached to this joint",
      returns = "()",
      type = "function"
     },
     getBodyB = {
      args = "()",
      description = "Returns the second body attached to this joint",
      returns = "()",
      type = "function"
     },
     getReactionForce = {
      args = "(inv_dt)",
      description = "Returns the reaction force on bodyB at the joint anchor",
      returns = "()",
      type = "function"
     },
     getReactionTorque = {
      args = "(inv_dt)",
      description = "Returns the reaction torque on bodyB",
      returns = "()",
      type = "function"
     },
     getType = {
      args = "()",
      description = "Returns a value that represents the type",
      returns = "()",
      type = "function"
     },
     isActive = {
      args = "()",
      description = "Is active?",
      returns = "()",
      type = "function"
     }
    }
   },
   MouseJoint = {
    childs = {
     getDampingRatio = {
      args = "()",
      description = "Returns the damping ratio",
      returns = "()",
      type = "function"
     },
     getFrequency = {
      args = "()",
      description = "Returns the response frequency in Hertz",
      returns = "()",
      type = "function"
     },
     getMaxForce = {
      args = "()",
      description = "Returns the maximum force in N",
      returns = "()",
      type = "function"
     },
     getTarget = {
      args = "()",
      description = "Returns the x and y coordinates of the target point",
      returns = "()",
      type = "function"
     },
     setDampingRatio = {
      args = "(ratio)",
      description = "Sets the damping ratio (0 = no damping, 1 = critical damping)",
      returns = "()",
      type = "function"
     },
     setFrequency = {
      args = "(frequency)",
      description = "Sets the response frequency in Hertz",
      returns = "()",
      type = "function"
     },
     setMaxForce = {
      args = "(force)",
      description = "Sets the maximum force in N",
      returns = "()",
      type = "function"
     },
     setTarget = {
      args = "(x, y)",
      description = "Updates the target point",
      returns = "()",
      type = "function"
     }
    }
   },
   PolygonShape = {
    childs = {
     new = {
      args = "()",
      description = "creates a new b2.PolygonShape instance",
      returns = "()",
      type = "function"
     },
     set = {
      args = "(vertices)",
      description = "Sets vertices",
      returns = "()",
      type = "function"
     },
     setAsBox = {
      args = "(hx, hy [, centerx, centery, angle])",
      description = "Set vertices to represent an oriented box",
      returns = "()",
      type = "function"
     }
    }
   },
   PrismaticJoint = {
    childs = {
     enableLimit = {
      args = "(flag)",
      description = "Enables or disables the joint limit",
      returns = "()",
      type = "function"
     },
     enableMotor = {
      args = "(flag)",
      description = "Enables or disables the joint motor",
      returns = "()",
      type = "function"
     },
     getJointSpeed = {
      args = "()",
      description = "Returns the current joint translation speed in meters per second",
      returns = "()",
      type = "function"
     },
     getJointTranslation = {
      args = "()",
      description = "Returns the current joint translation in meters",
      returns = "()",
      type = "function"
     },
     getLimits = {
      args = "()",
      description = "Returns the lower and upper joint limits in meters",
      returns = "()",
      type = "function"
     },
     getMotorForce = {
      args = "(inv_dt)",
      description = "Returns the current motor force given the inverse time step",
      returns = "()",
      type = "function"
     },
     getMotorSpeed = {
      args = "()",
      description = "Returns the motor speed in meters per second",
      returns = "()",
      type = "function"
     },
     isLimitEnabled = {
      args = "()",
      description = "Is the joint limit enabled?",
      returns = "()",
      type = "function"
     },
     isMotorEnabled = {
      args = "()",
      description = "Is the joint motor enabled?",
      returns = "()",
      type = "function"
     },
     setLimits = {
      args = "(lower, upper)",
      description = "Sets the joint limits in meters",
      returns = "()",
      type = "function"
     },
     setMaxMotorForce = {
      args = "(force)",
      description = "Sets the maximum motor force in N",
      returns = "()",
      type = "function"
     },
     setMotorSpeed = {
      args = "(speed)",
      description = "Sets the motor speed in meters per second",
      returns = "()",
      type = "function"
     }
    }
   },
   PulleyJoint = {
    childs = {
     getGroundAnchorA = {
      args = "()",
      description = "Returns the x and y coordinates of the first ground anchor",
      returns = "()",
      type = "function"
     },
     getGroundAnchorB = {
      args = "()",
      description = "Returns the x and y coordinates of the second ground anchor",
      returns = "()",
      type = "function"
     },
     getLengthA = {
      args = "()",
      description = "Returns the current length of the segment attached to bodyA",
      returns = "()",
      type = "function"
     },
     getLengthB = {
      args = "()",
      description = "Returns the current length of the segment attached to bodyB",
      returns = "()",
      type = "function"
     },
     getRatio = {
      args = "()",
      description = "Returns the joint ratio",
      returns = "()",
      type = "function"
     }
    }
   },
   RevoluteJoint = {
    childs = {
     enableLimit = {
      args = "(flag)",
      description = "Enables or disables the joint limit",
      returns = "()",
      type = "function"
     },
     enableMotor = {
      args = "(flag)",
      description = "Enables or disables the joint motor",
      returns = "()",
      type = "function"
     },
     getJointAngle = {
      args = "()",
      description = "Returns the current joint angle in radians",
      returns = "()",
      type = "function"
     },
     getJointSpeed = {
      args = "()",
      description = "Returns the current joint angle speed in radians per second",
      returns = "()",
      type = "function"
     },
     getLimits = {
      args = "()",
      description = "Returns the lower and upper joint limit in radians",
      returns = "()",
      type = "function"
     },
     getMotorSpeed = {
      args = "()",
      description = "Returns the motor speed in radians per second",
      returns = "()",
      type = "function"
     },
     getMotorTorque = {
      args = "(inv_dt)",
      description = "Returns the current motor torque given the inverse time step",
      returns = "()",
      type = "function"
     },
     isLimitEnabled = {
      args = "()",
      description = "Is the joint limit enabled?",
      returns = "()",
      type = "function"
     },
     isMotorEnabled = {
      args = "()",
      description = "Is the joint motor enabled?",
      returns = "()",
      type = "function"
     },
     setLimits = {
      args = "(lower, upper)",
      description = "Sets the joint limits in radians",
      returns = "()",
      type = "function"
     },
     setMaxMotorTorque = {
      args = "(torque)",
      description = "Sets the maximum motor torque in N-m",
      returns = "()",
      type = "function"
     },
     setMotorSpeed = {
      args = "(speed)",
      description = "Sets the motor speed in radians per second",
      returns = "()",
      type = "function"
     }
    }
   },
   WeldJoint = {
    childs = {
     getDampingRatio = {
      args = "()",
      description = "Returns damping ratio",
      returns = "()",
      type = "function"
     },
     getFrequency = {
      args = "()",
      description = "Returns frequency in Hz",
      returns = "()",
      type = "function"
     },
     setDampingRatio = {
      args = "(damping)",
      description = "Sets damping ratio",
      returns = "()",
      type = "function"
     },
     setFrequency = {
      args = "(frequency)",
      description = "Sets frequency in Hz",
      returns = "()",
      type = "function"
     }
    }
   },
   WheelJoint = {
    childs = {
     enableMotor = {
      args = "(flag)",
      description = "Enables or disables the joint motor",
      returns = "()",
      type = "function"
     },
     getJointSpeed = {
      args = "()",
      description = "Returns the current joint translation speed in meters per second. ",
      returns = "()",
      type = "function"
     },
     getJointTranslation = {
      args = "()",
      description = "Returns the current joint translation in meters. ",
      returns = "()",
      type = "function"
     },
     getMaxMotorTorque = {
      args = "()",
      description = "Returns the maximum motor force in N-m",
      returns = "()",
      type = "function"
     },
     getMotorSpeed = {
      args = "()",
      description = "Returns the motor speed in radians per second",
      returns = "()",
      type = "function"
     },
     getSpringDampingRatio = {
      args = "()",
      description = "Returns the spring damping ratio",
      returns = "()",
      type = "function"
     },
     getSpringFrequencyHz = {
      args = "()",
      description = "Returns the spring frequency in Hertz",
      returns = "()",
      type = "function"
     },
     isMotorEnabled = {
      args = "()",
      description = "Is the joint motor enabled?",
      returns = "()",
      type = "function"
     },
     setMaxMotorTorque = {
      args = "(torque)",
      description = "Sets the maximum motor force in N-m",
      returns = "()",
      type = "function"
     },
     setMotorSpeed = {
      args = "(speed)",
      description = "Sets the motor speed in radians per second",
      returns = "()",
      type = "function"
     },
     setSpringDampingRatio = {
      args = "(ratio)",
      description = "Sets the spring damping ratio",
      returns = "()",
      type = "function"
     },
     setSpringFrequencyHz = {
      args = "(frequency)",
      description = "Sets the spring frequency in Hertz (0 = disable the spring)",
      returns = "()",
      type = "function"
     }
    }
   },
   World = {
    childs = {
     clearForces = {
      args = "()",
      description = "Call this after you are done with time steps to clear the forces",
      returns = "()",
      type = "function"
     },
     createBody = {
      args = "(bodyDef)",
      description = "Creates a rigid body given a definition",
      returns = "()",
      type = "function"
     },
     createJoint = {
      args = "(jointDef)",
      description = "Creates a joint given a definition",
      returns = "()",
      type = "function"
     },
     destroyBody = {
      args = "(body)",
      description = "Destroys a rigid body",
      returns = "()",
      type = "function"
     },
     destroyJoint = {
      args = "(joint)",
      description = "Destroys a joint",
      returns = "()",
      type = "function"
     },
     getGravity = {
      args = "()",
      description = "Returns the gravity vector",
      returns = "()",
      type = "function"
     },
     new = {
      args = "(gravityx, gravityy [, doSleep])",
      description = "creates a new b2.World object",
      returns = "()",
      type = "function"
     },
     queryAABB = {
      args = "(lowerx, lowery, upperx, uppery)",
      description = "Query the world for all fixtures that potentially overlap the provided AABB",
      returns = "()",
      type = "function"
     },
     rayCast = {
      args = "(x1, y1, x2, y2, listener [, data])",
      description = "Raycast the world for all fixtures in the path of the ray",
      returns = "()",
      type = "function"
     },
     setDebugDraw = {
      args = "(debugDraw)",
      description = "Registers a b2.DebugDraw instance for debug drawing",
      returns = "()",
      type = "function"
     },
     setGravity = {
      args = "(gravityx, gravityy)",
      description = "Sets the gravity vector",
      returns = "()",
      type = "function"
     },
     step = {
      args = "(timeStep, velocityIterations, positionIterations)",
      description = "Takes a time step",
      returns = "()",
      type = "function"
     }
    }
   },
   createDistanceJointDef = {
    args = "(bodyA, bodyB, anchorAx, anchorAy, anchorBx, anchorBy)",
    description = "creates and returns a distance joint definition table",
    returns = "()",
    type = "function"
   },
   createFrictionJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory)",
    description = "creates and returns a friction joint definition table",
    returns = "()",
    type = "function"
   },
   createGearJointDef = {
    args = "(bodyA, bodyB, joint1, joint2, ratio)",
    description = "creates and returns a gear joint definition table",
    returns = "()",
    type = "function"
   },
   createMouseJointDef = {
    args = "(bodyA, bodyB, targetx, targety, maxForce, frequencyHz, dampingRatio)",
    description = "creates and returns a mouse joint definition table",
    returns = "()",
    type = "function"
   },
   createPrismaticJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory, axisx, axisy)",
    description = "creates and returns a prismatic joint definition table",
    returns = "()",
    type = "function"
   },
   createPulleyJointDef = {
    args = "(bodyA, bodyB, groundAnchorAx, groundAnchorAy, groundAnchorBx, groundAnchorBy, anchorAx, anchorAy, anchorBx, anchorBy, ratio)",
    description = "creates and returns a pulley joint definition table",
    returns = "()",
    type = "function"
   },
   createRevoluteJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory)",
    description = "creates and returns a revolute joint definition table",
    returns = "()",
    type = "function"
   },
   createWeldJointDef = {
    args = "(bodyA, bodyB, anchorAx, anchorAy, anchorBx, anchorBy)",
    description = "creates and returns a weld joint definition table",
    returns = "()",
    type = "function"
   },
   createWheelJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory, axisx, axisy)",
    description = "creates and returns a wheel joint definition table",
    returns = "()",
    type = "function"
   },
   getScale = {
    args = "()",
    description = "returns the global pixels to meters scale",
    returns = "()",
    type = "function"
   },
   setScale = {
    args = "(scale)",
    description = "sets the global pixels to meters scale",
    returns = "()",
    type = "function"
   }
  }
 },
 flurry = {
  childs = {
   endTimedEvent = {
    args = "(eventName [, parameters])",
    description = "ends Flurry timed event",
    returns = "()",
    type = "function"
   },
   isAvailable = {
    args = "()",
    description = "returns true if Flurry is available",
    returns = "()",
    type = "function"
   },
   logEvent = {
    args = "(eventName [, parameters [, timed]])",
    description = "logs Flurry event",
    returns = "()",
    type = "function"
   },
   startSession = {
    args = "(apiKey)",
    description = "starts the Flurry session with your API key",
    returns = "()",
    type = "function"
   }
  }
 }
}

--[[
  Conversion script for Resources/gideros_annot.api
  Run the script as: lua gideros-conv.lua <gideros_annot.api >gideros.lua

  Event
  Event.new(type) creates a new Event object
  Event.ENTER_FRAME
  getType() Event - returns the type of Event
  getTarget() Event - returns the element on which the event listener was registered

  Manual fixes
  - removed standard Lua functions
  - added Core at the beginning
  - moved Event.* constants together

  Limitations
  - only handles two levels of class hierarchy (as in b2.Body.*)

  Notes
  + b2.* and flurry.* don't have any headers (assume those)
  + there some duplicates, like Stage and stage (ignore lowecase ones)
  + remove "CLASS - " from the description
  + b2.World and many others have several levels
  + create different methods for Application and application
  + missing new() methods for some classes (+geolocation, +gyroscope, +accelerometer, +storekit)
  + application, stage, world are global variables

------------------------>> cut here <<-----------------------------

local class = ""
local t = {}
while true do
  local s = io.read()
  if not s then break end
  local newclass = s:match('^([A-Z]%w+)$') or s:match('^(b2%.%w+)$')
    or s:match('^([%.%w]+)%.')
  if newclass and class:lower() ~= newclass:lower() then
    class = newclass
    if not class:match('%.') then t[class] = t[class] or {childs = {}} end
  end
  s = s:gsub('^'..class..'%.', ""):gsub('^'..class:lower()..'%:', "")
  local const = s:match('^([A-Z_0-9]+)$')
  local fun, args, desc = s:match('(%w+)(%b())%s*(.*)%s*$')
  if not fun then fun = s:match('([a-z]%w+)%s*$') end
  if s:lower() == class:lower() then
    -- do nothing; it's either class or its duplicate
  elseif const then
    t[class].childs[const] = {type = "value"}
  elseif fun then
    desc = (desc or ""):gsub(class..' %- (%w)', string.upper)
    local t, class = t, class
    local c1, c2 = class:match('^(%w+)%.(%w+)$')
    if c1 and c2 then
      t[c1] = t[c1] or {childs = {}}
      t = t[c1].childs
      class = c2
      t[class] = t[class] or {childs = {}}
    end
    t[class].childs[fun] = {
      type = "function",
      args = args or "()",
      description = desc,
      returns = "()",
    }
  else
    print("Unrecognized string: "..s)
  end
end

-- several manual tweaks --

-- move functions from "Application" to "application" as there is a global
-- variable with that name.

t.application = t.application or {childs = {}}
for key, value in pairs(t.Application.childs) do
  if value.type == "function" then
    t.application.childs[key] = value
    t.Application.childs[key] = nil
  end
end

-- "stage" and "world" are also global variables, but what are their methods?

-- add missing new() methods
for _, class in ipairs{'Geolocation', 'Gyroscope', 'Accelerometer', 'StoreKit'} do
  if t[class] and t[class].childs then
    t[class].childs.new = {
      type = "function",
      args = "()",
      description = "Creates a new " .. class .." object",
      returns = "()",
    }
  else
    print("Can't find class object for class " .. class)
  end
end

print('return ' .. (require 'mobdebug').line(t, {indent = ' ', comment = false}))

------------------------>> cut here <<-----------------------------]]
