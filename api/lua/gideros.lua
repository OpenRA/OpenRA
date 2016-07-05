-- Copyright 2011-16 Paul Kulchenko, ZeroBrane LLC

-- converted from http://docs.giderosmobile.com/reference/autocomplete.php;
-- (API for Gideros 2016.06 as of July 4, 2016)
-- also available in <Gideros>/Resources/gideros_annot.api.
-- the conversion script is at the bottom of this file.

-- To process:
-- 1. download the API description and save it as gideros_annot.api
-- 2. run "../../bin/lua gideros.lua <gideros_annot.api >newapi" from ZBS/api/lua folder
-- 3. copy the content of "newapi" file to replace "api" table in gideros.lua
-- 4. launch the IDE and switch to gideros to confirm that it's loading without issues

local api = {
 Accelerometer = {
  childs = {
   getAcceleration = {
    args = "()",
    description = "Returns the 3-axis acceleration measured by the accelerometer",
    returns = "()",
    type = "method"
   },
   isAvailable = {
    args = "()",
    description = "Does the accelerometer available?",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "Creates new Accelerometer instance",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "Starts accelerometer updates",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Starts accelerometer updates",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Ads = {
  childs = {
   enableTesting = {
    args = "()",
    description = "Enable testing ads",
    returns = "()",
    type = "method"
   },
   get = {
    args = "(property)",
    description = "Gets property value of the ad",
    returns = "()",
    type = "method"
   },
   getHeight = {
    args = "()",
    description = "Gets the height of the ad",
    returns = "()",
    type = "method"
   },
   getPosition = {
    args = "()",
    description = "Gets x and y position of the ad",
    returns = "()",
    type = "method"
   },
   getWidth = {
    args = "()",
    description = "Gets width of the ad",
    returns = "()",
    type = "method"
   },
   getX = {
    args = "()",
    description = "Gets x position of the ad",
    returns = "()",
    type = "method"
   },
   getY = {
    args = "()",
    description = "Gets y position of the ad",
    returns = "()",
    type = "method"
   },
   hideAd = {
    args = "()",
    description = "Hides ads",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(adframework)",
    description = "Initializes new ad framework",
    returns = "()",
    type = "function"
   },
   set = {
    args = "(property, value)",
    description = "Sets property value of the ad",
    returns = "()",
    type = "method"
   },
   setAlignment = {
    args = "(horizontal, vertical)",
    description = "Sets alignment of the ad",
    returns = "()",
    type = "method"
   },
   setKey = {
    args = "(...)",
    description = "Set keys for the framework",
    returns = "()",
    type = "method"
   },
   setPosition = {
    args = "(x, y)",
    description = "Sets position of the ad",
    returns = "()",
    type = "method"
   },
   setX = {
    args = "(x)",
    description = "Sets x position of the ad",
    returns = "()",
    type = "method"
   },
   setY = {
    args = "(y)",
    description = "Sets y position of the ad",
    returns = "()",
    type = "method"
   },
   showAd = {
    args = "(...)",
    description = "Display ad",
    returns = "()",
    type = "method"
   }
  },
  inherits = "EventDispatcher",
  type = "class"
 },
 AlertDialog = {
  childs = {
   hide = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(title, message, cancelButton [, button1, button2])",
    description = "",
    returns = "()",
    type = "function"
   },
   show = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Application = {
  childs = {
   LANDSCAPE_LEFT = {
    description = "value \"landscapeLeft\"",
    type = "value"
   },
   LANDSCAPE_RIGHT = {
    description = "value \"landscapeRight\"",
    type = "value"
   },
   PORTRAIT = {
    description = "value \"portrait\"",
    type = "value"
   },
   PORTRAIT_UPSIDE_DOWN = {
    description = "value \"portraitUpsideDown\"",
    type = "value"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Bitmap = {
  childs = {
   getAnchorPoint = {
    args = "()",
    description = "Returns the x and y coordinates of the anchor point",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(texture)",
    description = "Creates a new Bitmap object",
    returns = "()",
    type = "function"
   },
   setAnchorPoint = {
    args = "(x, y)",
    description = "Sets the anchor point",
    returns = "()",
    type = "method"
   },
   setTexture = {
    args = "(texture)",
    description = "Sets the texture",
    returns = "()",
    type = "method"
   },
   setTextureRegion = {
    args = "(textureRegion)",
    description = "Sets the texture region",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 Controller = {
  childs = {
   getControllerName = {
    args = "(id)",
    description = "Gets the name of controller",
    returns = "()",
    type = "method"
   },
   getPlayerCount = {
    args = "()",
    description = "Returns amount of connected controllers",
    returns = "()",
    type = "method"
   },
   getPlayers = {
    args = "()",
    description = "Returns table with controller IDs",
    returns = "()",
    type = "method"
   },
   isAnyAvailable = {
    args = "()",
    description = "Return true if any controller is connected",
    returns = "()",
    type = "method"
   },
   virbate = {
    args = "(ms)",
    description = "Vibrate the controller for provided amount of miliseconds",
    returns = "()",
    type = "method"
   }
  },
  inherits = "EventDispatcher",
  type = "class"
 },
 Core = {
  childs = {
   asyncCall = {
    args = "(task [, parameters])",
    description = "Launch function on separate thread as background task",
    returns = "()",
    type = "function"
   },
   class = {
    args = "([base])",
    description = "Creates and returns new Gideros class",
    returns = "()",
    type = "function"
   },
   frameStatistics = {
    args = "()",
    description = "Return data about frame",
    returns = "()",
    type = "function"
   },
   yield = {
    args = "(state)",
    description = "Yield function running as background task",
    returns = "()",
    type = "function"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Cryptography = {
  childs = {
   aesDecrypt = {
    args = "(ciphertext, key [, iv, paddingType])",
    description = "Decrypt an AES 128 string",
    returns = "()",
    type = "function"
   },
   aesEncrypt = {
    args = "(plaintext, key [, iv, paddingType])",
    description = "Encrypt a string with AES",
    returns = "()",
    type = "function"
   },
   md5 = {
    args = "(input)",
    description = "Compute the MD5 hash of the input string",
    returns = "()",
    type = "function"
   }
  },
  type = "class"
 },
 Event = {
  childs = {
   ADDED_TO_STAGE = {
    description = "value \"addedToStage\"",
    type = "value"
   },
   AD_ACTION_BEGIN = {
    description = "value \"adActionBegin\"",
    type = "value"
   },
   AD_ACTION_END = {
    description = "value \"adActionEnd\"",
    type = "value"
   },
   AD_DISMISSED = {
    description = "value \"adDismissed\"",
    type = "value"
   },
   AD_ERROR = {
    description = "value \"adError\"",
    type = "value"
   },
   AD_FAILED = {
    description = "value \"adFailed\"",
    type = "value"
   },
   AD_RECEIVED = {
    description = "value \"adReceived\"",
    type = "value"
   },
   APPLICATION_BACKGROUND = {
    description = "value \"applicationBackground\"",
    type = "value"
   },
   APPLICATION_EXIT = {
    description = "value \"applicationExit\"",
    type = "value"
   },
   APPLICATION_FOREGROUND = {
    description = "value \"applicationForeground\"",
    type = "value"
   },
   APPLICATION_RESIZE = {
    description = "value \"applicationResize\"",
    type = "value"
   },
   APPLICATION_RESUME = {
    description = "value \"applicationResume\"",
    type = "value"
   },
   APPLICATION_START = {
    description = "value \"applicationStart\"",
    type = "value"
   },
   APPLICATION_SUSPEND = {
    description = "value \"applicationSuspend\"",
    type = "value"
   },
   BANNER_ACTION_BEGIN = {
    description = "value \"bannerActionBegin\"",
    type = "value"
   },
   BANNER_ACTION_FINISHED = {
    description = "value \"bannerActionFinished\"",
    type = "value"
   },
   BANNER_AD_FAILED = {
    description = "value \"bannerAdFailed\"",
    type = "value"
   },
   BANNER_AD_LOADED = {
    description = "value \"bannerAdLoaded\"",
    type = "value"
   },
   BEGIN_CONTACT = {
    description = "value \"beginContact\"",
    type = "value"
   },
   CHECK_BILLING_SUPPORTED_COMPLETE = {
    description = "value \"checkBillingSupportedComplete\"",
    type = "value"
   },
   COMPLETE = {
    description = "value \"complete\"",
    type = "value"
   },
   CONFIRM_NOTIFICATION_COMPLETE = {
    description = "value \"confirmNotificationComplete\"",
    type = "value"
   },
   CONNECTED = {
    description = "value \"connected\"",
    type = "value"
   },
   DATA_AVAILABLE = {
    description = "value \"dataAvailable\"",
    type = "value"
   },
   DIALOG_CANCEL = {
    description = "value \"dialogCancel\"",
    type = "value"
   },
   DIALOG_COMPLETE = {
    description = "value \"dialogComplete\"",
    type = "value"
   },
   DIALOG_ERROR = {
    description = "value \"dialogError\"",
    type = "value"
   },
   DISCONNECTED = {
    description = "value \"disconnected\"",
    type = "value"
   },
   END_CONTACT = {
    description = "value \"endContact\"",
    type = "value"
   },
   ENTER_FRAME = {
    description = "value \"enterFrame\"",
    type = "value"
   },
   ERROR = {
    description = "value \"error\"",
    type = "value"
   },
   HEADING_UPDATE = {
    description = "value \"headingUpdate\"",
    type = "value"
   },
   KEY_DOWN = {
    description = "value \"keyDown\"",
    type = "value"
   },
   KEY_UP = {
    description = "value \"keyUp\"",
    type = "value"
   },
   LEFT_JOYSTICK = {
    description = "value \"leftJoystick\"",
    type = "value"
   },
   LEFT_TRIGGER = {
    description = "value \"leftTrigger\"",
    type = "value"
   },
   LOCAL_NOTIFICATION = {
    description = "value \"localNotification\"",
    type = "value"
   },
   LOCATION_UPDATE = {
    description = "value \"locationUpdate\"",
    type = "value"
   },
   LOGIN_CANCEL = {
    description = "value \"loginCancel\"",
    type = "value"
   },
   LOGIN_COMPLETE = {
    description = "value \"loginComplete\"",
    type = "value"
   },
   LOGIN_ERROR = {
    description = "value \"loginError\"",
    type = "value"
   },
   LOGOUT_COMPLETE = {
    description = "value \"logoutComplete\"",
    type = "value"
   },
   MEMORY_WARNING = {
    description = "value \"memoryWarning\"",
    type = "value"
   },
   MOUSE_DOWN = {
    description = "value \"mouseDown\"",
    type = "value"
   },
   MOUSE_HOVER = {
    description = "value \"mouseHover\"",
    type = "value"
   },
   MOUSE_MOVE = {
    description = "value \"mouseMove\"",
    type = "value"
   },
   MOUSE_UP = {
    description = "value \"mouseUp\"",
    type = "value"
   },
   MOUSE_WHEEL = {
    description = "value \"mouseWheel\"",
    type = "value"
   },
   POST_SOLVE = {
    description = "value \"postSolve\"",
    type = "value"
   },
   PRE_SOLVE = {
    description = "value \"preSolve\"",
    type = "value"
   },
   PROGRESS = {
    description = "value \"progress\"",
    type = "value"
   },
   PURCHASE_STATE_CHANGE = {
    description = "value \"purchaseStateChange\"",
    type = "value"
   },
   PUSH_NOTIFICATION = {
    description = "value \"pushNotification\"",
    type = "value"
   },
   PUSH_REGISTRATION = {
    description = "value \"pushRegistration\"",
    type = "value"
   },
   PUSH_REGISTRATION_ERROR = {
    description = "value \"pushRegistrationError\"",
    type = "value"
   },
   REMOVED_FROM_STAGE = {
    description = "value \"removedFromStage\"",
    type = "value"
   },
   REQUEST_COMPLETE = {
    description = "value \"requestComplete\"",
    type = "value"
   },
   REQUEST_ERROR = {
    description = "value \"requestError\"",
    type = "value"
   },
   REQUEST_PRODUCTS_COMPLETE = {
    description = "value \"requestProductsComplete\"",
    type = "value"
   },
   REQUEST_PURCHASE_COMPLETE = {
    description = "value \"requestPurchaseComplete\"",
    type = "value"
   },
   RESTORE_TRANSACTIONS_COMPLETE = {
    description = "value \"restoreTransactionsComplete\"",
    type = "value"
   },
   RIGHT_JOYSTICK = {
    description = "value \"rightJoystick\"",
    type = "value"
   },
   RIGHT_TRIGGER = {
    description = "value \"rightTrigger\"",
    type = "value"
   },
   TIMER = {
    description = "value \"timer\"",
    type = "value"
   },
   TIMER_COMPLETE = {
    description = "value \"timerComplete\"",
    type = "value"
   },
   TOUCHES_BEGIN = {
    description = "value \"touchesBegin\"",
    type = "value"
   },
   TOUCHES_CANCEL = {
    description = "value \"touchesCancel\"",
    type = "value"
   },
   TOUCHES_END = {
    description = "value \"touchesEnd\"",
    type = "value"
   },
   TOUCHES_MOVE = {
    description = "value \"touchesMove\"",
    type = "value"
   },
   TRANSACTION = {
    description = "value \"transaction\"",
    type = "value"
   },
   getTarget = {
    args = "()",
    description = "Returns the element on which the event listener was registered",
    returns = "()",
    type = "method"
   },
   getType = {
    args = "()",
    description = "Returns the type of Event",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(type)",
    description = "Creates a new Event object",
    returns = "()",
    type = "function"
   },
   stopPropagation = {
    args = "()",
    description = "Stops the propagation of the current event in the scene tree hierarchy",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 EventDispatcher = {
  childs = {
   addEventListener = {
    args = "(type, listener [, data])",
    description = "Registers a listener function",
    returns = "()",
    type = "method"
   },
   dispatchEvent = {
    args = "(event)",
    description = "Dispatches an event",
    returns = "()",
    type = "method"
   },
   hasEventListener = {
    args = "(type)",
    description = "Checks if the EventDispatcher object has a event listener",
    returns = "()",
    type = "method"
   },
   new = {
    args = "()",
    description = "Creates a new EventDispatcher object",
    returns = "()",
    type = "function"
   },
   removeEventListener = {
    args = "(type, listener, data)",
    description = "Removes a listener function",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Facebook = {
  childs = {
   authorize = {
    args = "(permissions)",
    description = "",
    returns = "()",
    type = "method"
   },
   dialog = {
    args = "(action, paramaters)",
    description = "",
    returns = "()",
    type = "method"
   },
   extendAccessToken = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   extendAccessTokenIfNeeded = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   getAccessToken = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   getExpirationDate = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   graphRequest = {
    args = "(graphPath, paramaters, method)",
    description = "",
    returns = "()",
    type = "method"
   },
   isSessionValid = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   logout = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   setAccessToken = {
    args = "(accessToken)",
    description = "",
    returns = "()",
    type = "method"
   },
   setAppId = {
    args = "(appId)",
    description = "",
    returns = "()",
    type = "method"
   },
   setExpirationDate = {
    args = "(expirationDate)",
    description = "",
    returns = "()",
    type = "method"
   },
   shouldExtendAccessToken = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   }
  },
  inherits = "EventDispatcher",
  type = "class"
 },
 Font = {
  childs = {
   getDefault = {
    args = "()",
    description = "Get default font",
    returns = "()",
    type = "function"
   },
   new = {
    args = "(txtfile, imagefile, filtering)",
    description = "Creates a new Font object",
    returns = "()",
    type = "function"
   }
  },
  inherits = "FontBase",
  type = "class"
 },
 FontBase = {
  childs = {
   getAdvanceX = {
    args = "(text, letterSpacing, size)",
    description = "",
    returns = "()",
    type = "method"
   },
   getAscender = {
    args = "()",
    description = "Returns the ascender of the font",
    returns = "()",
    type = "method"
   },
   getBounds = {
    args = "(text)",
    description = "Returns the tight bounding rectangle of the characters in the string specified by text",
    returns = "()",
    type = "method"
   },
   getLineHeight = {
    args = "()",
    description = "Returns the distance from one base line to the next",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
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
    description = "Creates new Geolocation instance",
    returns = "()",
    type = "function"
   },
   setAccuracy = {
    args = "(accuracy)",
    description = "Of the location data",
    returns = "()",
    type = "function"
   },
   setThreshold = {
    args = "(threshold)",
    description = "Threshold",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "Starts the generation of updates that report the current location and heading",
    returns = "()",
    type = "method"
   },
   startUpdatingHeading = {
    args = "()",
    description = "Starts the generation of updates that report the heading",
    returns = "()",
    type = "method"
   },
   startUpdatingLocation = {
    args = "()",
    description = "Starts the generation of updates that report the current location",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Stops the generation of updates that report the current location and heading",
    returns = "()",
    type = "method"
   },
   stopUpdatingHeading = {
    args = "()",
    description = "Stops the generation of updates that report the heading",
    returns = "()",
    type = "method"
   },
   stopUpdatingLocation = {
    args = "()",
    description = "Stops the generation of updates that report the current location",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 GoogleBilling = {
  childs = {
   BILLING_UNAVAILABLE = {
    description = "value \"billingUnavailable\"",
    type = "value"
   },
   CANCELED = {
    description = "value \"canceled\"",
    type = "value"
   },
   DEVELOPER_ERROR = {
    description = "value \"developerError\"",
    type = "value"
   },
   ERROR = {
    description = "value \"error\"",
    type = "value"
   },
   EXPIRED = {
    description = "value \"expired\"",
    type = "value"
   },
   INAPP = {
    description = "value \"inapp\"",
    type = "value"
   },
   ITEM_UNAVAILABLE = {
    description = "value \"itemUnavailable\"",
    type = "value"
   },
   OK = {
    description = "value \"ok\"",
    type = "value"
   },
   PURCHASED = {
    description = "value \"purchased\"",
    type = "value"
   },
   REFUNDED = {
    description = "value \"refunded\"",
    type = "value"
   },
   SERVICE_UNAVAILABLE = {
    description = "value \"serviceUnavailable\"",
    type = "value"
   },
   SUBS = {
    description = "value \"subs\"",
    type = "value"
   },
   USER_CANCELED = {
    description = "value \"userCanceled\"",
    type = "value"
   },
   checkBillingSupported = {
    args = "(productType)",
    description = "",
    returns = "()",
    type = "method"
   },
   confirmNotification = {
    args = "(notificationId)",
    description = "",
    returns = "()",
    type = "method"
   },
   requestPurchase = {
    args = "(productId, productType, developerPayload)",
    description = "",
    returns = "()",
    type = "method"
   },
   restoreTransactions = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   setApiVersion = {
    args = "(apiVersion)",
    description = "",
    returns = "()",
    type = "method"
   },
   setPublicKey = {
    args = "(publicKey)",
    description = "",
    returns = "()",
    type = "method"
   }
  },
  type = "class"
 },
 Gyroscope = {
  childs = {
   getRotationRate = {
    args = "()",
    description = "Returns the rotation rate in radians per second",
    returns = "()",
    type = "method"
   },
   isAvailable = {
    args = "()",
    description = "Does the gyroscope available?",
    returns = "()",
    type = "function"
   },
   new = {
    args = "()",
    description = "Creates new Gyroscope instance",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "Starts gyroscope updates",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Stops gyroscope updates",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 KeyCode = {
  childs = {
   A = {
    description = "value 65",
    type = "value"
   },
   B = {
    description = "value 66",
    type = "value"
   },
   BACK = {
    description = "value 301",
    type = "value"
   },
   C = {
    description = "value 67",
    type = "value"
   },
   CENTER = {
    description = "value 304",
    type = "value"
   },
   D = {
    description = "value 68",
    type = "value"
   },
   DOWN = {
    description = "value 40",
    type = "value"
   },
   E = {
    description = "value 82",
    type = "value"
   },
   F = {
    description = "value 70",
    type = "value"
   },
   G = {
    description = "value 71",
    type = "value"
   },
   H = {
    description = "value 72",
    type = "value"
   },
   I = {
    description = "value 73",
    type = "value"
   },
   J = {
    description = "value 74",
    type = "value"
   },
   K = {
    description = "value 75",
    type = "value"
   },
   L = {
    description = "value 76",
    type = "value"
   },
   L1 = {
    description = "value 307",
    type = "value"
   },
   LEFT = {
    description = "value 37",
    type = "value"
   },
   M = {
    description = "value 77",
    type = "value"
   },
   MENU = {
    description = "value 303",
    type = "value"
   },
   MOUSE_LEFT = {
    description = "value 1",
    type = "value"
   },
   MOUSE_MIDDLE = {
    description = "value 4",
    type = "value"
   },
   MOUSE_NONE = {
    description = "value 0",
    type = "value"
   },
   MOUSE_RIGHT = {
    description = "value 2",
    type = "value"
   },
   N = {
    description = "value 78",
    type = "value"
   },
   NUM_0 = {
    description = "value 48",
    type = "value"
   },
   NUM_1 = {
    description = "value 49",
    type = "value"
   },
   NUM_2 = {
    description = "value 50",
    type = "value"
   },
   NUM_3 = {
    description = "value 51",
    type = "value"
   },
   NUM_4 = {
    description = "value 52",
    type = "value"
   },
   NUM_5 = {
    description = "value 53",
    type = "value"
   },
   NUM_6 = {
    description = "value 54",
    type = "value"
   },
   NUM_7 = {
    description = "value 55",
    type = "value"
   },
   NUM_8 = {
    description = "value 56",
    type = "value"
   },
   NUM_9 = {
    description = "value 57",
    type = "value"
   },
   O = {
    description = "value 79",
    type = "value"
   },
   P = {
    description = "value 80",
    type = "value"
   },
   Q = {
    description = "value 81",
    type = "value"
   },
   R1 = {
    description = "value 308",
    type = "value"
   },
   RIGHT = {
    description = "value 39",
    type = "value"
   },
   S = {
    description = "value 83",
    type = "value"
   },
   SEARCH = {
    description = "value 302",
    type = "value"
   },
   SELECT = {
    description = "value 305",
    type = "value"
   },
   START = {
    description = "value 306",
    type = "value"
   },
   T = {
    description = "value 84",
    type = "value"
   },
   U = {
    description = "value 85",
    type = "value"
   },
   UP = {
    description = "value 38",
    type = "value"
   },
   V = {
    description = "value 86",
    type = "value"
   },
   W = {
    description = "value 87",
    type = "value"
   },
   X = {
    description = "value 88",
    type = "value"
   },
   Y = {
    description = "value 89",
    type = "value"
   },
   Z = {
    description = "value 90",
    type = "value"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Matrix = {
  childs = {
   getAnchorPosition = {
    args = "()",
    description = "Get anchor position from matrix transformation",
    returns = "()",
    type = "method"
   },
   getElements = {
    args = "()",
    description = "Returns the elements of this matrix instance",
    returns = "()",
    type = "method"
   },
   getM11 = {
    args = "()",
    description = "Returns the value of the m11 component",
    returns = "()",
    type = "method"
   },
   getM12 = {
    args = "()",
    description = "Returns the value of the m12 component",
    returns = "()",
    type = "method"
   },
   getM21 = {
    args = "()",
    description = "Returns the value of the m21 component",
    returns = "()",
    type = "method"
   },
   getM22 = {
    args = "()",
    description = "Returns the value of the m22 component",
    returns = "()",
    type = "method"
   },
   getMatrix = {
    args = "()",
    description = "Get all 16 elements of 4x4 matrix",
    returns = "()",
    type = "method"
   },
   getPosition = {
    args = "()",
    description = "Get position from matrix transformation",
    returns = "()",
    type = "method"
   },
   getRotationX = {
    args = "()",
    description = "Get rotation for x axis",
    returns = "()",
    type = "method"
   },
   getRotationY = {
    args = "()",
    description = "Get rotation on y axis",
    returns = "()",
    type = "method"
   },
   getRotationZ = {
    args = "()",
    description = "Get rotation for z axis",
    returns = "()",
    type = "method"
   },
   getScale = {
    args = "()",
    description = "Get scale from matrix transformation",
    returns = "()",
    type = "method"
   },
   getScaleX = {
    args = "()",
    description = "Get scale on x axis",
    returns = "()",
    type = "method"
   },
   getScaleY = {
    args = "()",
    description = "Get scale on y axis",
    returns = "()",
    type = "method"
   },
   getScaleZ = {
    args = "()",
    description = "Get scale on z axis",
    returns = "()",
    type = "method"
   },
   getTx = {
    args = "()",
    description = "Returns the value of the tx component",
    returns = "()",
    type = "method"
   },
   getTy = {
    args = "()",
    description = "Returns the value of the ty component",
    returns = "()",
    type = "method"
   },
   getTz = {
    args = "()",
    description = "Returns the value of the tz component",
    returns = "()",
    type = "method"
   },
   getX = {
    args = "()",
    description = "Get x position",
    returns = "()",
    type = "method"
   },
   getY = {
    args = "()",
    description = "Get y position",
    returns = "()",
    type = "method"
   },
   getZ = {
    args = "()",
    description = "Get z position",
    returns = "()",
    type = "method"
   },
   multiply = {
    args = "(matrix)",
    description = "Multiply current matrix with new one",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(m11, m12, m21, m22, tx, ty)",
    description = "Creates a new Matrix object",
    returns = "()",
    type = "function"
   },
   rotate = {
    args = "(angle, x vector, y vector, z vector)",
    description = "Combine existing rotation with provided",
    returns = "()",
    type = "method"
   },
   scale = {
    args = "(x scale [, y scale, z scale])",
    description = "Combine existing scale with provided scale",
    returns = "()",
    type = "method"
   },
   setAnchorPosition = {
    args = "(x, y [, z])",
    description = "Transform matrix for setting anchor position",
    returns = "()",
    type = "method"
   },
   setElements = {
    args = "(m11, m12, m21, m22, tx, ty)",
    description = "Sets all 6 elements of this matrix instance",
    returns = "()",
    type = "method"
   },
   setM11 = {
    args = "(m11)",
    description = "Sets the value of the m11 component",
    returns = "()",
    type = "method"
   },
   setM12 = {
    args = "(m12)",
    description = "Sets the value of the m22 component",
    returns = "()",
    type = "method"
   },
   setM21 = {
    args = "(m21)",
    description = "",
    returns = "()",
    type = "method"
   },
   setM22 = {
    args = "(m22)",
    description = "",
    returns = "()",
    type = "method"
   },
   setMatrix = {
    args = "([m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44])",
    description = "Set all 16 elements of 4x4 matrix",
    returns = "()",
    type = "method"
   },
   setPosition = {
    args = "(x, y [, z])",
    description = "Transform matrix for setting position",
    returns = "()",
    type = "method"
   },
   setRotationX = {
    args = "(x rotation)",
    description = "Set rotation on x axis",
    returns = "()",
    type = "method"
   },
   setRotationY = {
    args = "(y rotation)",
    description = "Set rotation on y axis",
    returns = "()",
    type = "method"
   },
   setRotationZ = {
    args = "(z rotation)",
    description = "Set rotation on z axis",
    returns = "()",
    type = "method"
   },
   setScale = {
    args = "(x [, y, z])",
    description = "Transform matrix for setting scale",
    returns = "()",
    type = "method"
   },
   setScaleX = {
    args = "(x scale)",
    description = "Set scale on x axis",
    returns = "()",
    type = "method"
   },
   setScaleY = {
    args = "(y scale)",
    description = "Set scale on y axis",
    returns = "()",
    type = "method"
   },
   setScaleZ = {
    args = "(z scale)",
    description = "Set scale on z axis",
    returns = "()",
    type = "method"
   },
   setTx = {
    args = "(tx)",
    description = "Sets the value of the tx component",
    returns = "()",
    type = "method"
   },
   setTy = {
    args = "(ty)",
    description = "Sets the value of the ty component",
    returns = "()",
    type = "method"
   },
   setTz = {
    args = "(tz)",
    description = "Sets the value of the tz component",
    returns = "()",
    type = "method"
   },
   setX = {
    args = "(x)",
    description = "Set x position",
    returns = "()",
    type = "method"
   },
   setY = {
    args = "(y)",
    description = "Set y position",
    returns = "()",
    type = "method"
   },
   setZ = {
    args = "(z)",
    description = "Set z position",
    returns = "()",
    type = "method"
   },
   translate = {
    args = "(x [, y, z])",
    description = "Combine existing translation with provided translation",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Mesh = {
  childs = {
   clearColorArray = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   clearIndexArray = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   clearTexture = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   clearTextureCoordinateArray = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   clearVertexArray = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   getColor = {
    args = "(i)",
    description = "Returns color and alpha of the i-th element from color array",
    returns = "()",
    type = "method"
   },
   getColorArraySize = {
    args = "()",
    description = "Get size of the Color array",
    returns = "()",
    type = "method"
   },
   getIndex = {
    args = "(i)",
    description = "Returns the i-th element from index array",
    returns = "()",
    type = "method"
   },
   getIndexArraySize = {
    args = "()",
    description = "Get size of the Index array",
    returns = "()",
    type = "method"
   },
   getTextureCoordinate = {
    args = "(i)",
    description = "Returns u and v coordinate of the i-th element from texture coordinate array",
    returns = "()",
    type = "method"
   },
   getTextureCoordinateArraySize = {
    args = "()",
    description = "Get size of the Texture Coordinate array",
    returns = "()",
    type = "method"
   },
   getVertex = {
    args = "(i)",
    description = "Returns x and y coordinate of the i-th element from vertex array",
    returns = "()",
    type = "method"
   },
   getVertexArraySize = {
    args = "()",
    description = "Get size of the Vertices array",
    returns = "()",
    type = "method"
   },
   new = {
    args = "([is3d])",
    description = "",
    returns = "()",
    type = "function"
   },
   resizeColorArray = {
    args = "(size)",
    description = "",
    returns = "()",
    type = "method"
   },
   resizeIndexArray = {
    args = "(size)",
    description = "",
    returns = "()",
    type = "method"
   },
   resizeTextureCoordinateArray = {
    args = "(size)",
    description = "",
    returns = "()",
    type = "method"
   },
   resizeVertexArray = {
    args = "(size)",
    description = "",
    returns = "()",
    type = "method"
   },
   setColor = {
    args = "(i, color, alpha)",
    description = "",
    returns = "()",
    type = "method"
   },
   setColorArray = {
    args = "(colors)",
    description = "",
    returns = "()",
    type = "method"
   },
   setColors = {
    args = "(colors)",
    description = "",
    returns = "()",
    type = "method"
   },
   setIndex = {
    args = "(i, index)",
    description = "",
    returns = "()",
    type = "method"
   },
   setIndexArray = {
    args = "(indices)",
    description = "",
    returns = "()",
    type = "method"
   },
   setIndices = {
    args = "(indices)",
    description = "",
    returns = "()",
    type = "method"
   },
   setTexture = {
    args = "(texture)",
    description = "",
    returns = "()",
    type = "method"
   },
   setTextureCoordinate = {
    args = "(i, u, v)",
    description = "",
    returns = "()",
    type = "method"
   },
   setTextureCoordinateArray = {
    args = "(textureCoordinates)",
    description = "",
    returns = "()",
    type = "method"
   },
   setTextureCoordinates = {
    args = "(textureCoordinates)",
    description = "",
    returns = "()",
    type = "method"
   },
   setVertex = {
    args = "(i, x, y)",
    description = "",
    returns = "()",
    type = "method"
   },
   setVertexArray = {
    args = "(vertices)",
    description = "",
    returns = "()",
    type = "method"
   },
   setVertices = {
    args = "(vertices)",
    description = "",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 Microphone = {
  childs = {
   new = {
    args = "(deviceName, sampleRate, numChannels, bitsPerSample)",
    description = "Creates a new Microphone object.",
    returns = "()",
    type = "function"
   },
   setOutputFile = {
    args = "(fileName)",
    description = "Sets the output file",
    returns = "()",
    type = "method"
   },
   start = {
    args = "()",
    description = "Start recording with device.",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Stop recording",
    returns = "()",
    type = "method"
   }
  },
  type = "class"
 },
 MovieClip = {
  childs = {
   clearAction = {
    args = "(frame)",
    description = "Clears the action at the specified frame",
    returns = "()",
    type = "method"
   },
   gotoAndPlay = {
    args = "(frame)",
    description = "Goes to the specified frame and starts playing",
    returns = "()",
    type = "method"
   },
   gotoAndStop = {
    args = "(frame)",
    description = "Goes to the specified frame and stops",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(timeline)",
    description = "Creates a new MovieClip object",
    returns = "()",
    type = "function"
   },
   play = {
    args = "()",
    description = "Starts playing the movie clip",
    returns = "()",
    type = "method"
   },
   setGotoAction = {
    args = "(frame, destframe)",
    description = "Sets a \"go to\" action to the specified frame",
    returns = "()",
    type = "method"
   },
   setStopAction = {
    args = "(frame)",
    description = "Sets a \"stop\" action to the specified frame",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Stops playing the movie clip",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 Notification = {
  childs = {
   DEFAULT_SOUND = {
    description = "value \"default\"",
    type = "value"
   },
   cancel = {
    args = "()",
    description = "Cancel notification",
    returns = "()",
    type = "method"
   },
   dispatchAfter = {
    args = "()",
    description = "Dispatch notification after specified time",
    returns = "()",
    type = "method"
   },
   dispatchNow = {
    args = "()",
    description = "Dispatch notification now",
    returns = "()",
    type = "method"
   },
   dispatchOn = {
    args = "()",
    description = "Dispatch on specified date",
    returns = "()",
    type = "method"
   },
   getId = {
    args = "()",
    description = "Get id of notification",
    returns = "()",
    type = "method"
   },
   getMessage = {
    args = "()",
    description = "Get message of notification",
    returns = "()",
    type = "method"
   },
   getNumber = {
    args = "()",
    description = "Get notification number",
    returns = "()",
    type = "method"
   },
   getSound = {
    args = "()",
    description = "Get sound of notification",
    returns = "()",
    type = "method"
   },
   getTitle = {
    args = "()",
    description = "Get title of notification",
    returns = "()",
    type = "method"
   },
   new = {
    args = "()",
    description = "Creates new notification",
    returns = "()",
    type = "function"
   },
   setNumber = {
    args = "()",
    description = "Set notification number",
    returns = "()",
    type = "method"
   },
   setSound = {
    args = "()",
    description = "Set notification sound",
    returns = "()",
    type = "method"
   },
   setTitle = {
    args = "()",
    description = "Set the title of notification",
    returns = "()",
    type = "method"
   }
  },
  type = "class"
 },
 NotificationManager = {
  childs = {
   cancelAllNotifications = {
    args = "()",
    description = "Cancel scheduled notification",
    returns = "()",
    type = "method"
   },
   cancelNotification = {
    args = "()",
    description = "Cancel specified notification",
    returns = "()",
    type = "method"
   },
   clearLocalNotifications = {
    args = "()",
    description = "Clear local notifications",
    returns = "()",
    type = "method"
   },
   clearPushNotifications = {
    args = "()",
    description = "Clear push notifications",
    returns = "()",
    type = "method"
   },
   getLocalNotifications = {
    args = "()",
    description = "Get local notifications",
    returns = "()",
    type = "method"
   },
   getPushNotifications = {
    args = "()",
    description = "Get push notification",
    returns = "()",
    type = "method"
   },
   getScheduledNotifications = {
    args = "()",
    description = "Get schedule notifications",
    returns = "()",
    type = "method"
   },
   getSharedInstance = {
    args = "()",
    description = "Get NotificationManager instance",
    returns = "()",
    type = "function"
   },
   registerForPushNotifications = {
    args = "()",
    description = "Register for push notifications",
    returns = "()",
    type = "method"
   },
   unregisterForPushNotifications = {
    args = "()",
    description = "Unregister from notifications",
    returns = "()",
    type = "method"
   }
  },
  inherits = "EventDispatcher",
  type = "class"
 },
 Object = {
  childs = {
   getBaseClass = {
    args = "()",
    description = "Returns base class",
    returns = "()",
    type = "method"
   },
   getClass = {
    args = "()",
    description = "Returns class name",
    returns = "()",
    type = "method"
   },
   isInstanceOf = {
    args = "(classname)",
    description = "Checks if instance belongs to class",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Particles = {
  childs = {
   addParticles = {
    args = "(particles)",
    description = "Add particles",
    returns = "()",
    type = "method"
   },
   clearTexture = {
    args = "()",
    description = "Clear texture for all particles",
    returns = "()",
    type = "method"
   },
   getParticleAngle = {
    args = "(i)",
    description = "Get particle angle",
    returns = "()",
    type = "method"
   },
   getParticleColor = {
    args = "(i)",
    description = "Get color and alpha value of particle",
    returns = "()",
    type = "method"
   },
   getParticlePosition = {
    args = "(i)",
    description = "Get position of particle",
    returns = "()",
    type = "method"
   },
   getParticleSize = {
    args = "(i)",
    description = "Get size of particle in pixels",
    returns = "()",
    type = "method"
   },
   getParticleSpeed = {
    args = "(i)",
    description = "Get speed of particle",
    returns = "()",
    type = "method"
   },
   getParticleTtl = {
    args = "(i)",
    description = "Get initial time to live of particle",
    returns = "()",
    type = "method"
   },
   new = {
    args = "()",
    description = "Create new particles group",
    returns = "()",
    type = "function"
   },
   removeParticles = {
    args = "(particle indeces)",
    description = "Remove particles by index in table or as arguments",
    returns = "()",
    type = "method"
   },
   setParticleAngle = {
    args = "(i, angle)",
    description = "Set angle of particle",
    returns = "()",
    type = "method"
   },
   setParticleColor = {
    args = "(i, color [, alpha])",
    description = "Set color of particles",
    returns = "()",
    type = "method"
   },
   setParticlePosition = {
    args = "(i, x, y)",
    description = "Set position of particle",
    returns = "()",
    type = "method"
   },
   setParticleSize = {
    args = "(i, size)",
    description = "Set size of particle",
    returns = "()",
    type = "method"
   },
   setParticleSpeed = {
    args = "(i [, x, y, a, decay])",
    description = "Set speed of particles",
    returns = "()",
    type = "method"
   },
   setParticleTtl = {
    args = "(i, ttl)",
    description = "Set time to live",
    returns = "()",
    type = "method"
   },
   setTexture = {
    args = "(texture)",
    description = "Set texture to all particles",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 Path2D = {
  childs = {
   new = {
    args = "()",
    description = "Creates Path2D object",
    returns = "()",
    type = "function"
   },
   setConvex = {
    args = "(convex)",
    description = "Flag the shape as convex.",
    returns = "()",
    type = "method"
   },
   setFillColor = {
    args = "(color [, alpha])",
    description = "Sets fill color",
    returns = "()",
    type = "method"
   },
   setFontPath = {
    args = "(font, character)",
    description = "Sets the path from the outline of a TTFont character",
    returns = "()",
    type = "method"
   },
   setLineColor = {
    args = "(color [, alpha])",
    description = "Sets line color",
    returns = "()",
    type = "method"
   },
   setLineThickness = {
    args = "(thickness [, feather])",
    description = "Set the thickness of the outline",
    returns = "()",
    type = "method"
   },
   setPath = {
    args = "(commands, coordinates [, coordinates])",
    description = "Set path to draw",
    returns = "()",
    type = "method"
   },
   setSvgPath = {
    args = "(svg_params)",
    description = "Set path with svg properties",
    returns = "()",
    type = "method"
   },
   setTexture = {
    args = "(texture)",
    description = "Sets texture for fill (Not implemented yet)",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 Pixel = {
  childs = {
   getColor = {
    args = "()",
    description = "Gets the color of the Pixe",
    returns = "()",
    type = "method"
   },
   new = {
    args = "([color, alpha, width, height])",
    description = "Create new pixel",
    returns = "()",
    type = "function"
   },
   setColor = {
    args = "([color, alpha])",
    description = "Sets the color of the Pixel",
    returns = "()",
    type = "method"
   },
   setDimensions = {
    args = "(w, h)",
    description = "Sets both width and height of the Pixel.",
    returns = "()",
    type = "method"
   },
   setHeight = {
    args = "(h)",
    description = "Sets the height of the pixel sprite.",
    returns = "()",
    type = "method"
   },
   setWidth = {
    args = "(w)",
    description = "Sets the width of the pixel sprite.",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 RenderTarget = {
  childs = {
   clear = {
    args = "(color, alpha [, x, y, width, height])",
    description = "Clears rendered texture",
    returns = "()",
    type = "method"
   },
   draw = {
    args = "(sprite)",
    description = "Renders provided object",
    returns = "()",
    type = "method"
   },
   getPixel = {
    args = "(x, y)",
    description = "Returns single pixels color and alpha channel",
    returns = "()",
    type = "method"
   },
   getPixels = {
    args = "(x, y, w, h)",
    description = "Returns buffer containing color and alpha data from provided rectangle",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(width, height, filtering)",
    description = "Creates new RenderTarget object",
    returns = "()",
    type = "function"
   }
  },
  inherits = "TextureBase",
  type = "class"
 },
 Shader = {
  childs = {
   CFLOAT = {
    description = "value 1",
    type = "value"
   },
   CFLOAT4 = {
    description = "value 2",
    type = "value"
   },
   CINT = {
    description = "value 0",
    type = "value"
   },
   CMATRIX = {
    description = "value 3",
    type = "value"
   },
   CTEXTURE = {
    description = "value 4",
    type = "value"
   },
   DBYTE = {
    description = "value 0",
    type = "value"
   },
   DFLOAT = {
    description = "value 5",
    type = "value"
   },
   DINT = {
    description = "value 4",
    type = "value"
   },
   DSHORT = {
    description = "value 2",
    type = "value"
   },
   DUBYTE = {
    description = "value 1",
    type = "value"
   },
   DUSHORT = {
    description = "value 3",
    type = "value"
   },
   FLAG_NONE = {
    description = "value 0",
    type = "value"
   },
   FLAG_NO_DEFAULT_HEADER = {
    description = "value 1",
    type = "value"
   },
   SYS_COLOR = {
    description = "value 2",
    type = "value"
   },
   SYS_NONE = {
    description = "value 0",
    type = "value"
   },
   SYS_PARTICLESIZE = {
    description = "value 6",
    type = "value"
   },
   SYS_TEXTUREINFO = {
    description = "value 5",
    type = "value"
   },
   SYS_WIT = {
    description = "value 3",
    type = "value"
   },
   SYS_WORLD = {
    description = "value 4",
    type = "value"
   },
   SYS_WVP = {
    description = "value 1",
    type = "value"
   },
   getEngineVersion = {
    args = "()",
    description = "Get shader version",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(vertex shader, fragment shader, flags, uniform descriptor, attribute descriptor)",
    description = "Create new shader",
    returns = "()",
    type = "function"
   },
   setConstant = {
    args = "(uniform name, data type, mult, data)",
    description = "Change the value of a uniform",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Shape = {
  childs = {
   EVEN_ODD = {
    description = "value \"evenOdd\"",
    type = "value"
   },
   NONE = {
    description = "value \"none\"",
    type = "value"
   },
   NON_ZERO = {
    description = "value \"nonZero\"",
    type = "value"
   },
   SOLID = {
    description = "value \"solid\"",
    type = "value"
   },
   TEXTURE = {
    description = "value \"texture\"",
    type = "value"
   },
   beginPath = {
    args = "(winding)",
    description = "Resets the current path",
    returns = "()",
    type = "method"
   },
   clear = {
    args = "()",
    description = "Clears the graphics that were drawn to this Shape object, and resets fill and line style settings",
    returns = "()",
    type = "method"
   },
   closePath = {
    args = "()",
    description = "Marks the current subpath as closed, and starts a new subpath with a point the same as the start and end of the newly closed subpath",
    returns = "()",
    type = "method"
   },
   endPath = {
    args = "()",
    description = "Ends the current path and draws the geometry by using the specified line and fill styles",
    returns = "()",
    type = "method"
   },
   lineTo = {
    args = "(x, y)",
    description = "Adds the given point to the current subpath, connected to the previous one by a straight line.",
    returns = "()",
    type = "method"
   },
   moveTo = {
    args = "(x, y)",
    description = "Creates a new subpath with the given point",
    returns = "()",
    type = "method"
   },
   new = {
    args = "()",
    description = "Creates a new Shape object",
    returns = "()",
    type = "function"
   },
   setFillStyle = {
    args = "(type, ...)",
    description = "Sets the fill style that Shape object uses for subsequent drawings",
    returns = "()",
    type = "method"
   },
   setLineStyle = {
    args = "(width, color, alpha)",
    description = "Sets the line style that Shape object uses for subsequent drawings",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 Sound = {
  childs = {
   getLength = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(filename)",
    description = "Creates a new Sound object",
    returns = "()",
    type = "function"
   },
   play = {
    args = "(startTime, looping, paused)",
    description = "Creates a new SoundChannel object to play the sound",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 SoundChannel = {
  childs = {
   getPitch = {
    args = "()",
    description = "Returns the current pitch of the sound channel",
    returns = "()",
    type = "method"
   },
   getPosition = {
    args = "()",
    description = "Returns the position of the current playback",
    returns = "()",
    type = "method"
   },
   getVolume = {
    args = "()",
    description = "Returns the current volume of the sound channel",
    returns = "()",
    type = "method"
   },
   isLooping = {
    args = "()",
    description = "Returns the looping state of the channel",
    returns = "()",
    type = "method"
   },
   isPaused = {
    args = "()",
    description = "Returns the paused state of the channel",
    returns = "()",
    type = "method"
   },
   isPlaying = {
    args = "()",
    description = "Returns the playing state for the sound channel",
    returns = "()",
    type = "method"
   },
   setLooping = {
    args = "(looping)",
    description = "Sets the looping state of the channel",
    returns = "()",
    type = "method"
   },
   setPaused = {
    args = "(paused)",
    description = "Sets the paused state of the channel",
    returns = "()",
    type = "method"
   },
   setPitch = {
    args = "(pitch)",
    description = "Sets the pitch of the sound channel",
    returns = "()",
    type = "method"
   },
   setPosition = {
    args = "(position)",
    description = "Sets the position of the current playback",
    returns = "()",
    type = "method"
   },
   setVolume = {
    args = "(volume)",
    description = "Sets the volume of the sound channel",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Stops the sound playing in the channel",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Sprite = {
  childs = {
   ADD = {
    description = "value \"add\"",
    type = "value"
   },
   ALPHA = {
    description = "value \"alpha\"",
    type = "value"
   },
   MULTIPLY = {
    description = "value \"multiply\"",
    type = "value"
   },
   NO_ALPHA = {
    description = "value \"noAlpha\"",
    type = "value"
   },
   SCREEN = {
    description = "value \"screen\"",
    type = "value"
   },
   addChild = {
    args = "(child)",
    description = "Adds a sprite as a child",
    returns = "()",
    type = "method"
   },
   addChildAt = {
    args = "(child, index)",
    description = "Add a sprite as a child at the index position specified",
    returns = "()",
    type = "method"
   },
   clearBlendMode = {
    args = "()",
    description = "Clears the blending mode",
    returns = "()",
    type = "method"
   },
   contains = {
    args = "(child)",
    description = "Determines whether the specified sprite is contained in the subtree of this sprite",
    returns = "()",
    type = "method"
   },
   get = {
    args = "(param)",
    description = "Gets the specified property by its name",
    returns = "()",
    type = "method"
   },
   getAlpha = {
    args = "()",
    description = "Returns the alpha transparency of this sprite",
    returns = "()",
    type = "method"
   },
   getAnchorPosition = {
    args = "()",
    description = "Returns anchor position of Sprite",
    returns = "()",
    type = "method"
   },
   getBounds = {
    args = "(targetSprite)",
    description = "Returns the bounds as it appears in another sprite's coordinate system",
    returns = "()",
    type = "method"
   },
   getChildAt = {
    args = "(index)",
    description = "Returns the child sprite that exists at the specified index",
    returns = "()",
    type = "method"
   },
   getChildIndex = {
    args = "(child)",
    description = "Returns the index of the specified child sprite",
    returns = "()",
    type = "method"
   },
   getColorTransform = {
    args = "()",
    description = "Returns the red, green, blue and alpha channel multipliers",
    returns = "()",
    type = "method"
   },
   getHeight = {
    args = "()",
    description = "Returns the height",
    returns = "()",
    type = "method"
   },
   getMatrix = {
    args = "()",
    description = "Returns the transformation matrix of the sprite",
    returns = "()",
    type = "method"
   },
   getNumChildren = {
    args = "()",
    description = "Returns the number of children of this sprite",
    returns = "()",
    type = "method"
   },
   getParent = {
    args = "()",
    description = "Returns the parent sprite",
    returns = "()",
    type = "method"
   },
   getPosition = {
    args = "()",
    description = "Gets the x,y and z coordinates of the sprite",
    returns = "()",
    type = "method"
   },
   getRotation = {
    args = "()",
    description = "Returns the rotation of the sprite in degrees",
    returns = "()",
    type = "method"
   },
   getRotationX = {
    args = "()",
    description = "Returns the rotation of the sprite around x axis in degrees",
    returns = "()",
    type = "method"
   },
   getRotationY = {
    args = "()",
    description = "Returns the rotation of the sprite around y axis in degrees",
    returns = "()",
    type = "method"
   },
   getScale = {
    args = "()",
    description = "Returns the horizontal, vertical and z scales of the sprite",
    returns = "()",
    type = "method"
   },
   getScaleX = {
    args = "()",
    description = "Returns the horizontal scale of the sprite",
    returns = "()",
    type = "method"
   },
   getScaleY = {
    args = "()",
    description = "Returns the vertical scale of the sprite",
    returns = "()",
    type = "method"
   },
   getScaleZ = {
    args = "()",
    description = "Returns the scale on z axis of the sprite",
    returns = "()",
    type = "method"
   },
   getWidth = {
    args = "()",
    description = "Returns the width",
    returns = "()",
    type = "method"
   },
   getX = {
    args = "()",
    description = "Returns the x coordinate of the sprite",
    returns = "()",
    type = "method"
   },
   getY = {
    args = "()",
    description = "Returns the y coordinate of the sprite",
    returns = "()",
    type = "method"
   },
   getZ = {
    args = "()",
    description = "Returns the z coordinate of the sprite",
    returns = "()",
    type = "method"
   },
   globalToLocal = {
    args = "(x, y)",
    description = "Converts the x,y coordinates from the global to the sprite's (local) coordinates",
    returns = "()",
    type = "method"
   },
   hitTestPoint = {
    args = "(x, y [, shapeFlag])",
    description = "Checks the given coordinates is in bounds of the sprite",
    returns = "()",
    type = "method"
   },
   isVisible = {
    args = "()",
    description = "Returns the visibility of sprite",
    returns = "()",
    type = "method"
   },
   localToGlobal = {
    args = "(x, y)",
    description = "Converts the x,y coordinates from the sprite's (local) coordinates to the global coordinates",
    returns = "()",
    type = "method"
   },
   new = {
    args = "()",
    description = "Creates a new Sprite object",
    returns = "()",
    type = "function"
   },
   removeChild = {
    args = "(child)",
    description = "Removes the child sprite",
    returns = "()",
    type = "method"
   },
   removeChildAt = {
    args = "(index)",
    description = "Removes the child sprite at the specifed index",
    returns = "()",
    type = "method"
   },
   removeFromParent = {
    args = "()",
    description = "If the sprite has a parent, removes the sprite from the child list of its parent sprite.",
    returns = "()",
    type = "method"
   },
   set = {
    args = "(param, value)",
    description = "Sets the specified property by its name",
    returns = "()",
    type = "method"
   },
   setAlpha = {
    args = "(alpha)",
    description = "Sets the alpha transparency of this sprite",
    returns = "()",
    type = "method"
   },
   setAnchorPosition = {
    args = "(anchorX, anchorY [, anchorZ])",
    description = "Set anchor position",
    returns = "()",
    type = "method"
   },
   setBlendMode = {
    args = "(blendMode)",
    description = "Sets the blend mode of the sprite",
    returns = "()",
    type = "method"
   },
   setClip = {
    args = "(x, y, width, height)",
    description = "Clip Sprite contents",
    returns = "()",
    type = "method"
   },
   setColorTransform = {
    args = "(redMultiplier, greenMultiplier, blueMultiplier, alphaMultiplier)",
    description = "Sets the red, green, blue and alpha channel multipliers",
    returns = "()",
    type = "method"
   },
   setMatrix = {
    args = "(matrix)",
    description = "Sets the transformation matrix of the sprite",
    returns = "()",
    type = "method"
   },
   setPosition = {
    args = "(x, y [, z])",
    description = "Sets the x,y and z coordinates of the sprite",
    returns = "()",
    type = "method"
   },
   setRotation = {
    args = "(rotation)",
    description = "Sets the rotation of the sprite in degrees",
    returns = "()",
    type = "method"
   },
   setRotationX = {
    args = "()",
    description = "Sets the rotation of the sprite in degrees around x axis",
    returns = "()",
    type = "method"
   },
   setRotationY = {
    args = "()",
    description = "Sets the rotation of the sprite in degrees around y axis",
    returns = "()",
    type = "method"
   },
   setScale = {
    args = "(scaleX, scaleY, scaleZ)",
    description = "Sets the horizontal, vertical and z axis scales of the sprite",
    returns = "()",
    type = "method"
   },
   setScaleX = {
    args = "(scaleX)",
    description = "Sets the horizontal scale of the sprite",
    returns = "()",
    type = "method"
   },
   setScaleY = {
    args = "(scaleY)",
    description = "Sets the vertical scale of the sprite",
    returns = "()",
    type = "method"
   },
   setScaleZ = {
    args = "(scale)",
    description = "Set scale on z axis",
    returns = "()",
    type = "method"
   },
   setShader = {
    args = "(shader)",
    description = "Set shader for this sprite",
    returns = "()",
    type = "method"
   },
   setVisible = {
    args = "(visible)",
    description = "Sets the visibility of sprite",
    returns = "()",
    type = "method"
   },
   setX = {
    args = "(x)",
    description = "Sets the x coordinate of the sprite",
    returns = "()",
    type = "method"
   },
   setY = {
    args = "(y)",
    description = "Sets the y coordinate of the sprite",
    returns = "()",
    type = "method"
   },
   setZ = {
    args = "(z)",
    description = "Sets the z coordinate of the sprite",
    returns = "()",
    type = "method"
   },
   swapChildren = {
    args = "(child1, child2)",
    description = "Swap two children index places",
    returns = "()",
    type = "method"
   },
   swapChildrenAt = {
    args = "(index1, index2)",
    description = "Swaps two child sprites.",
    returns = "()",
    type = "method"
   }
  },
  inherits = "EventDispatcher",
  type = "class"
 },
 Stage = {
  childs = {},
  inherits = "Sprite",
  type = "class"
 },
 StoreKit = {
  childs = {
   FAILED = {
    description = "value \"failed\"",
    type = "value"
   },
   PURCHASED = {
    description = "value \"purchased\"",
    type = "value"
   },
   RESTORED = {
    description = "value \"restored\"",
    type = "value"
   },
   canMakePayments = {
    args = "()",
    description = "Returns whether the user is allowed to make payments",
    returns = "()",
    type = "method"
   },
   finishTransaction = {
    args = "(transaction)",
    description = "Completes a pending transaction",
    returns = "()",
    type = "method"
   },
   new = {
    args = "()",
    description = "Creates a new StoreKit object",
    returns = "()",
    type = "function"
   },
   purchase = {
    args = "(productIdentifier, quantity)",
    description = "Process a payment request",
    returns = "()",
    type = "method"
   },
   requestProducts = {
    args = "(productIdentifiers)",
    description = "Retrieve localized information about a list of products",
    returns = "()",
    type = "method"
   },
   restoreCompletedTransactions = {
    args = "()",
    description = "Restore previously completed purchases",
    returns = "()",
    type = "method"
   }
  },
  type = "class"
 },
 TTFont = {
  childs = {
   new = {
    args = "(filename, size, text, filtering)",
    description = "Creates a new TTFont object",
    returns = "()",
    type = "function"
   }
  },
  inherits = "FontBase",
  type = "class"
 },
 TextField = {
  childs = {
   getLetterSpacing = {
    args = "()",
    description = "Returns the letter-spacing property which is used to increase or decrease the space between characters in a text",
    returns = "()",
    type = "method"
   },
   getText = {
    args = "()",
    description = "Returns the text displayed",
    returns = "()",
    type = "method"
   },
   getTextColor = {
    args = "()",
    description = "Returns the color of the text in a text field in hexadecimal format",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(font, text)",
    description = "Creates a new TextField object with the specified font and text",
    returns = "()",
    type = "function"
   },
   setLetterSpacing = {
    args = "(spacing)",
    description = "Sets the letter-spacing property which is used to increase or decrease the space between characters in a text",
    returns = "()",
    type = "method"
   },
   setText = {
    args = "(text)",
    description = "Sets the text to be displayed",
    returns = "()",
    type = "method"
   },
   setTextColor = {
    args = "(color)",
    description = "Sets the color of the text in a text field in hexadecimal format",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 TextInputDialog = {
  childs = {
   EMAIL = {
    description = "value \"email\"",
    type = "value"
   },
   NUMBER = {
    description = "value \"number\"",
    type = "value"
   },
   PHONE = {
    description = "value \"phone\"",
    type = "value"
   },
   TEXT = {
    description = "value \"text\"",
    type = "value"
   },
   URL = {
    description = "value \"url\"",
    type = "value"
   },
   getInputType = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   getText = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   isSecureInput = {
    args = "()",
    description = "",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(title, message, text, cancelButton, button1, button2)",
    description = "",
    returns = "()",
    type = "function"
   },
   setInputType = {
    args = "(type)",
    description = "",
    returns = "()",
    type = "method"
   },
   setSecureInput = {
    args = "(secureInput)",
    description = "",
    returns = "()",
    type = "method"
   },
   setText = {
    args = "(text)",
    description = "",
    returns = "()",
    type = "method"
   }
  },
  inherits = "AlertDialog",
  type = "class"
 },
 Texture = {
  childs = {
   new = {
    args = "(filename, filtering, options)",
    description = "Creates a new Texture object",
    returns = "()",
    type = "function"
   }
  },
  inherits = "TextureBase",
  type = "class"
 },
 TextureBase = {
  childs = {
   CLAMP = {
    description = "value \"clamp\"",
    type = "value"
   },
   REPEAT = {
    description = "value \"repeat\"",
    type = "value"
   },
   RGB565 = {
    description = "value \"rgb565\"",
    type = "value"
   },
   RGB888 = {
    description = "value \"rgb888\"",
    type = "value"
   },
   RGBA4444 = {
    description = "value \"rgba4444\"",
    type = "value"
   },
   RGBA5551 = {
    description = "value \"rgba5551\"",
    type = "value"
   },
   RGBA8888 = {
    description = "value \"rgba8888\"",
    type = "value"
   },
   getHeight = {
    args = "()",
    description = "Returns the height of the texture in pixels",
    returns = "()",
    type = "method"
   },
   getWidth = {
    args = "()",
    description = "Returns the width of the texture in pixels",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 TexturePack = {
  childs = {
   getTextureRegion = {
    args = "(texturename)",
    description = "Returns the texture region of texture pack",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(txtfile, imagefile, filtering, options)",
    description = "",
    returns = "()",
    type = "function"
   }
  },
  inherits = "TextureBase",
  type = "class"
 },
 TextureRegion = {
  childs = {
   getRegion = {
    args = "()",
    description = "Returns the coordinates of the region",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(texture, x, y, width, height)",
    description = "",
    returns = "()",
    type = "function"
   },
   setRegion = {
    args = "(x, y, width, height)",
    description = "Sets the coordinates of the region",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 TileMap = {
  childs = {
   FLIP_DIAGONAL = {
    description = "value 1",
    type = "value"
   },
   FLIP_HORIZONTAL = {
    description = "value 4",
    type = "value"
   },
   FLIP_VERTICAL = {
    description = "value 2",
    type = "value"
   },
   clearTile = {
    args = "(x, y)",
    description = "Set an empty tile at given indices",
    returns = "()",
    type = "method"
   },
   getTile = {
    args = "(x, y)",
    description = "Returns the index of the tile",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(width, height, texture, tilewidth, tileheight, spacingx, spacingy, marginx, marginy, displaywidth, displayheight)",
    description = "Creates a new TileMap instance",
    returns = "()",
    type = "function"
   },
   setTile = {
    args = "(x, y, tx, ty, flip)",
    description = "Sets the index of the tile",
    returns = "()",
    type = "method"
   },
   shift = {
    args = "(dx, dy)",
    description = "Shifts the tile map",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 Timer = {
  childs = {
   delayedCall = {
    args = "(delay, func [, data])",
    description = "Delayed call a function after a set amount of time",
    returns = "()",
    type = "function"
   },
   getCurrentCount = {
    args = "()",
    description = "Returns the current trigger count of the timer",
    returns = "()",
    type = "method"
   },
   getDelay = {
    args = "()",
    description = "Returns the time interval between timer events in milliseconds",
    returns = "()",
    type = "method"
   },
   getRepeatCount = {
    args = "()",
    description = "Returns the number of repetitions the timer will make",
    returns = "()",
    type = "method"
   },
   isRunning = {
    args = "()",
    description = "Returns the current running status of timer",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(delay, repeatCount)",
    description = "Creates a new Timer object",
    returns = "()",
    type = "function"
   },
   pauseAll = {
    args = "()",
    description = "Pause all timers",
    returns = "()",
    type = "function"
   },
   reset = {
    args = "()",
    description = "Stops the timer and sets the currentCount property to 0",
    returns = "()",
    type = "method"
   },
   resumeAll = {
    args = "()",
    description = "Resume all timers",
    returns = "()",
    type = "function"
   },
   setDelay = {
    args = "(delay)",
    description = "Sets the time interval between timer events in milliseconds",
    returns = "()",
    type = "method"
   },
   setRepeatCount = {
    args = "(repeatCount)",
    description = "Sets the number of repetitions the timer will make",
    returns = "()",
    type = "method"
   },
   start = {
    args = "()",
    description = "Starts the timer",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Stops the timer",
    returns = "()",
    type = "method"
   },
   stopAll = {
    args = "()",
    description = "Stop all timers",
    returns = "()",
    type = "function"
   }
  },
  inherits = "Object",
  type = "class"
 },
 UrlLoader = {
  childs = {
   DELETE = {
    description = "value \"delete\"",
    type = "value"
   },
   GET = {
    description = "value \"get\"",
    type = "value"
   },
   POST = {
    description = "value \"post\"",
    type = "value"
   },
   PUT = {
    description = "value \"put\"",
    type = "value"
   },
   close = {
    args = "()",
    description = "Terminates the current loading operation",
    returns = "()",
    type = "method"
   },
   ignoreSslErrors = {
    args = "()",
    description = "Ignores SSL certificate related errors",
    returns = "()",
    type = "method"
   },
   load = {
    args = "(url, method, headers, body)",
    description = "Loads data from the specified URL",
    returns = "()",
    type = "method"
   },
   new = {
    args = "(url, method, headers, body)",
    description = "Creates a new UrlLoader object",
    returns = "()",
    type = "function"
   }
  },
  inherits = "Object",
  type = "class"
 },
 Viewport = {
  childs = {
   setContent = {
    args = "(content)",
    description = "",
    returns = "()",
    type = "method"
   },
   setProjection = {
    args = "(matrix)",
    description = "Specify a projection matrix to use when displaying the content. ",
    returns = "()",
    type = "method"
   },
   setTransform = {
    args = "(transform)",
    description = "",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 application = {
  childs = {
   canOpenUrl = {
    args = "(url)",
    description = "Tests if it is possible to open provided url",
    returns = "()",
    type = "method"
   },
   configureFrustum = {
    args = "(fov [, farplane])",
    description = "Configure the projection for 3D perspective.",
    returns = "()",
    type = "method"
   },
   exit = {
    args = "()",
    description = "Terminates the application",
    returns = "()",
    type = "method"
   },
   getApiVersion = {
    args = "()",
    description = "Returns the API version",
    returns = "()",
    type = "method"
   },
   getBackgroundColor = {
    args = "()",
    description = "Returns the background color in hexadecimal format",
    returns = "()",
    type = "method"
   },
   getContentHeight = {
    args = "()",
    description = "Returns content height",
    returns = "()",
    type = "method"
   },
   getContentWidth = {
    args = "()",
    description = "Returns content width",
    returns = "()",
    type = "method"
   },
   getDeviceHeight = {
    args = "()",
    description = "Returns the physical height of the device in pixels",
    returns = "()",
    type = "method"
   },
   getDeviceInfo = {
    args = "()",
    description = "Returns information about device",
    returns = "()",
    type = "method"
   },
   getDeviceOrientation = {
    args = "()",
    description = "Get the device orientation",
    returns = "()",
    type = "method"
   },
   getDeviceWidth = {
    args = "()",
    description = "Returns the physical width of the device in pixels",
    returns = "()",
    type = "method"
   },
   getFps = {
    args = "()",
    description = "Returns the frame rate of the application",
    returns = "()",
    type = "method"
   },
   getLanguage = {
    args = "()",
    description = "Returns the user language",
    returns = "()",
    type = "method"
   },
   getLocale = {
    args = "()",
    description = "Returns the device locale",
    returns = "()",
    type = "method"
   },
   getLogicalHeight = {
    args = "()",
    description = "Returns the logical height of the application",
    returns = "()",
    type = "method"
   },
   getLogicalScaleX = {
    args = "()",
    description = "Returns the scaling of automatic screen scaling on the x-axis",
    returns = "()",
    type = "method"
   },
   getLogicalScaleY = {
    args = "()",
    description = "Returns the scaling of automatic screen scaling on the y-axis",
    returns = "()",
    type = "method"
   },
   getLogicalTranslateX = {
    args = "()",
    description = "Returns the translation of automatic screen scaling on the x-axis",
    returns = "()",
    type = "method"
   },
   getLogicalTranslateY = {
    args = "()",
    description = "Returns the translation of automatic screen scaling on the y-axis",
    returns = "()",
    type = "method"
   },
   getLogicalWidth = {
    args = "()",
    description = "Returns the logical width of the application",
    returns = "()",
    type = "method"
   },
   getOrientation = {
    args = "()",
    description = "Returns the orientation of the application",
    returns = "()",
    type = "method"
   },
   getScaleMode = {
    args = "()",
    description = "Returns the automatic scale mode of the application",
    returns = "()",
    type = "method"
   },
   getScreenDensity = {
    args = "()",
    description = "Returns the screen density in pixels per inch",
    returns = "()",
    type = "method"
   },
   getTextureMemoryUsage = {
    args = "()",
    description = "Returns the texture memory usage in Kbytes",
    returns = "()",
    type = "method"
   },
   isPlayerMode = {
    args = "()",
    description = "Check if app runs on player",
    returns = "()",
    type = "method"
   },
   openUrl = {
    args = "(url)",
    description = "Opens the given URL in the appropriate application",
    returns = "()",
    type = "method"
   },
   setBackgroundColor = {
    args = "(color)",
    description = "Sets the background color in hexadecimal format",
    returns = "()",
    type = "method"
   },
   setFps = {
    args = "(fps)",
    description = "Sets the frame rate of the application",
    returns = "()",
    type = "method"
   },
   setFullScreen = {
    args = "(fullscreen)",
    description = "Full screen or window mode",
    returns = "()",
    type = "method"
   },
   setKeepAwake = {
    args = "(keepAwake)",
    description = "Enables/disables screen dimming and device sleeping",
    returns = "()",
    type = "method"
   },
   setLogicalDimensions = {
    args = "(width, height)",
    description = "Sets the logical dimensions of the application",
    returns = "()",
    type = "method"
   },
   setOrientation = {
    args = "(orientation)",
    description = "Sets the orientation of the application",
    returns = "()",
    type = "method"
   },
   setScaleMode = {
    args = "(scaleMode)",
    description = "Sets the automatic scale mode of the application",
    returns = "()",
    type = "method"
   },
   setWindowSize = {
    args = "(width, height)",
    description = "Sets desktop window to a specific size",
    returns = "()",
    type = "method"
   },
   vibrate = {
    args = "()",
    description = "Vibrates the device",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Object",
  type = "class"
 },
 b2 = {
  childs = {
   Body = {
    childs = {
     applyAngularImpulse = {
      args = "(impulse)",
      description = "Applies an angular impulse",
      returns = "()",
      type = "method"
     },
     applyForce = {
      args = "(forcex, forcey, pointx, pointy)",
      description = "Applies a force at a world point",
      returns = "()",
      type = "method"
     },
     applyLinearImpulse = {
      args = "(impulsex, impulsey, pointx, pointy)",
      description = "Applies an impulse at a point",
      returns = "()",
      type = "method"
     },
     applyTorque = {
      args = "(torque)",
      description = "Applies a torque",
      returns = "()",
      type = "method"
     },
     createFixture = {
      args = "(fixtureDef)",
      description = "Creates a fixture and attach it to this body",
      returns = "()",
      type = "method"
     },
     destroyFixture = {
      args = "(fixture)",
      description = "Destroys a fixture",
      returns = "()",
      type = "method"
     },
     getAngle = {
      args = "()",
      description = "Returns the current world rotation angle in radians",
      returns = "()",
      type = "method"
     },
     getAngularDamping = {
      args = "()",
      description = "Returns the angular damping of the body",
      returns = "()",
      type = "method"
     },
     getAngularVelocity = {
      args = "()",
      description = "Returns the angular velocity",
      returns = "()",
      type = "method"
     },
     getGravityScale = {
      args = "()",
      description = "Returns the gravity scale of the body",
      returns = "()",
      type = "method"
     },
     getInertia = {
      args = "()",
      description = "Returns the rotational inertia of the body about the local origin in kg-m^2",
      returns = "()",
      type = "method"
     },
     getLinearDamping = {
      args = "()",
      description = "Returns the linear damping of the body",
      returns = "()",
      type = "method"
     },
     getLinearVelocity = {
      args = "()",
      description = "Returns the linear velocity of the center of mass",
      returns = "()",
      type = "method"
     },
     getLocalCenter = {
      args = "()",
      description = "Returns the local position of the center of mass",
      returns = "()",
      type = "method"
     },
     getLocalPoint = {
      args = "(x, y)",
      description = "",
      returns = "()",
      type = "method"
     },
     getLocalVector = {
      args = "(x, y)",
      description = "",
      returns = "()",
      type = "method"
     },
     getMass = {
      args = "()",
      description = "Returns the total mass of the body in kilograms (kg)",
      returns = "()",
      type = "method"
     },
     getPosition = {
      args = "()",
      description = "Returns the world body origin position",
      returns = "()",
      type = "method"
     },
     getWorldCenter = {
      args = "()",
      description = "Returns the world position of the center of mass",
      returns = "()",
      type = "method"
     },
     getWorldPoint = {
      args = "(x, y)",
      description = "",
      returns = "()",
      type = "method"
     },
     getWorldVector = {
      args = "(x, y)",
      description = "",
      returns = "()",
      type = "method"
     },
     isActive = {
      args = "()",
      description = "Returns the active state of the body",
      returns = "()",
      type = "method"
     },
     isAwake = {
      args = "()",
      description = "Returns the sleeping state of the body",
      returns = "()",
      type = "method"
     },
     isBullet = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     isFixedRotation = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     isSleepingAllowed = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     setActive = {
      args = "(flag)",
      description = "Sets the active state of the body",
      returns = "()",
      type = "method"
     },
     setAngle = {
      args = "(angle)",
      description = "",
      returns = "()",
      type = "method"
     },
     setAngularDamping = {
      args = "(angularDamping)",
      description = "Sets the angular damping of the body",
      returns = "()",
      type = "method"
     },
     setAngularVelocity = {
      args = "(omega)",
      description = "Sets the angular velocity",
      returns = "()",
      type = "method"
     },
     setAwake = {
      args = "(awake)",
      description = "Sets the sleep state of the body",
      returns = "()",
      type = "method"
     },
     setBullet = {
      args = "(flag)",
      description = "",
      returns = "()",
      type = "method"
     },
     setFixedRotation = {
      args = "(flag)",
      description = "",
      returns = "()",
      type = "method"
     },
     setGravityScale = {
      args = "(scale)",
      description = "Sets the gravity scale of the body",
      returns = "()",
      type = "method"
     },
     setLinearDamping = {
      args = "(linearDamping)",
      description = "Sets the linear damping of the body",
      returns = "()",
      type = "method"
     },
     setLinearVelocity = {
      args = "(x, y)",
      description = "Sets the linear velocity of the center of mass",
      returns = "()",
      type = "method"
     },
     setPosition = {
      args = "(x, y)",
      description = "Sets the world body origin position",
      returns = "()",
      type = "method"
     },
     setSleepingAllowed = {
      args = "(flag)",
      description = "",
      returns = "()",
      type = "method"
     }
    },
    type = "class"
   },
   ChainShape = {
    childs = {
     createChain = {
      args = "(vertices)",
      description = "Creates a chain with isolated end vertices",
      returns = "()",
      type = "method"
     },
     createLoop = {
      args = "(vertices)",
      description = "Creates a loop",
      returns = "()",
      type = "method"
     },
     new = {
      args = "()",
      description = "",
      returns = "()",
      type = "function"
     }
    },
    inherits = "b2.Shape",
    type = "class"
   },
   CircleShape = {
    childs = {
     new = {
      args = "(centerx, centery, radius)",
      description = "",
      returns = "()",
      type = "function"
     },
     set = {
      args = "(centerx, centery, radius)",
      description = "Sets the center point and radius",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Shape",
    type = "class"
   },
   Contact = {
    childs = {
     getChildIndexA = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     getChildIndexB = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     getFixtureA = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     getFixtureB = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     getFriction = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     getManifold = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     getRestitution = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     getWorldManifold = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     isTouching = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     resetFriction = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     resetRestitution = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     setEnabled = {
      args = "(flag)",
      description = "",
      returns = "()",
      type = "method"
     },
     setFriction = {
      args = "(friction)",
      description = "",
      returns = "()",
      type = "method"
     },
     setRestitution = {
      args = "(restitution)",
      description = "",
      returns = "()",
      type = "method"
     }
    },
    type = "class"
   },
   DISTANCE_JOINT = {
    description = "value 3",
    type = "value"
   },
   DYNAMIC_BODY = {
    description = "value 2",
    type = "value"
   },
   DebugDraw = {
    childs = {
     AABB_BIT = {
      description = "value 4",
      type = "value"
     },
     CENTER_OF_MASS_BIT = {
      description = "value 16",
      type = "value"
     },
     JOINT_BIT = {
      description = "value 2",
      type = "value"
     },
     PAIR_BIT = {
      description = "value 8",
      type = "value"
     },
     SHAPE_BIT = {
      description = "value 1",
      type = "value"
     },
     appendFlags = {
      args = "(flags)",
      description = "Append flags to the current flags",
      returns = "()",
      type = "method"
     },
     clearFlags = {
      args = "(flags)",
      description = "Clear flags from the current flags",
      returns = "()",
      type = "method"
     },
     getFlags = {
      args = "()",
      description = "Returns the debug drawing flags",
      returns = "()",
      type = "method"
     },
     new = {
      args = "()",
      description = "",
      returns = "()",
      type = "function"
     },
     setFlags = {
      args = "(flags)",
      description = "Sets the debug drawing flags",
      returns = "()",
      type = "method"
     }
    },
    inherits = "Sprite",
    type = "class"
   },
   DistanceJoint = {
    childs = {
     getDampingRatio = {
      args = "()",
      description = "Returns the damping ratio",
      returns = "()",
      type = "method"
     },
     getFrequency = {
      args = "()",
      description = "Returns the mass-spring-damper frequency in Hertz",
      returns = "()",
      type = "method"
     },
     getLength = {
      args = "()",
      description = "Returns the length of this distance joint in meters",
      returns = "()",
      type = "method"
     },
     setDampingRatio = {
      args = "(ratio)",
      description = "Sets the damping ratio (0 = no damping, 1 = critical damping)",
      returns = "()",
      type = "method"
     },
     setFrequency = {
      args = "(frequency)",
      description = "Sets the mass-spring-damper frequency in Hertz",
      returns = "()",
      type = "method"
     },
     setLength = {
      args = "(length)",
      description = "Sets the natural joint length in meters",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   EdgeShape = {
    childs = {
     new = {
      args = "(v1x, v1y, v2x, v2y)",
      description = "",
      returns = "()",
      type = "function"
     },
     set = {
      args = "(v1x, v1y, v2x, v2y)",
      description = "Sets the two vertices",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Shape",
    type = "class"
   },
   FRICTION_JOINT = {
    description = "value 9",
    type = "value"
   },
   Fixture = {
    childs = {
     getBody = {
      args = "()",
      description = "Returns the parent body of this fixture",
      returns = "()",
      type = "method"
     },
     getFilterData = {
      args = "()",
      description = "Returns the contact filtering data",
      returns = "()",
      type = "method"
     },
     isSensor = {
      args = "()",
      description = "Is this fixture a sensor (non-solid)?",
      returns = "()",
      type = "method"
     },
     setFilterData = {
      args = "(filterData)",
      description = "Sets the contact filtering data",
      returns = "()",
      type = "method"
     },
     setSensor = {
      args = "(sensor)",
      description = "Sets if this fixture is a sensor",
      returns = "()",
      type = "method"
     }
    },
    type = "class"
   },
   FrictionJoint = {
    childs = {
     getMaxForce = {
      args = "()",
      description = "Returns the maximum friction force in N",
      returns = "()",
      type = "method"
     },
     getMaxTorque = {
      args = "()",
      description = "Returns the maximum friction torque in N*m",
      returns = "()",
      type = "method"
     },
     setMaxForce = {
      args = "(force)",
      description = "Sets the maximum friction force in N",
      returns = "()",
      type = "method"
     },
     setMaxTorque = {
      args = "(torque)",
      description = "Sets the maximum friction torque in N*m",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   GEAR_JOINT = {
    description = "value 6",
    type = "value"
   },
   GearJoint = {
    childs = {
     getRatio = {
      args = "()",
      description = "Returns the gear ratio",
      returns = "()",
      type = "method"
     },
     setRatio = {
      args = "(ratio)",
      description = "Sets the gear ratio",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   Joint = {
    childs = {
     getAnchorA = {
      args = "()",
      description = "Returns the anchor point on bodyA in world coordinates",
      returns = "()",
      type = "method"
     },
     getAnchorB = {
      args = "()",
      description = "Returns the anchor point on bodyB in world coordinates",
      returns = "()",
      type = "method"
     },
     getBodyA = {
      args = "()",
      description = "Returns the first body attached to this joint",
      returns = "()",
      type = "method"
     },
     getBodyB = {
      args = "()",
      description = "Returns the second body attached to this joint",
      returns = "()",
      type = "method"
     },
     getReactionForce = {
      args = "(inv_dt)",
      description = "Returns the reaction force on bodyB at the joint anchor",
      returns = "()",
      type = "method"
     },
     getReactionTorque = {
      args = "(inv_dt)",
      description = "Returns the reaction torque on bodyB",
      returns = "()",
      type = "method"
     },
     getType = {
      args = "()",
      description = "Returns a value that represents the type",
      returns = "()",
      type = "method"
     },
     isActive = {
      args = "()",
      description = "Is active?",
      returns = "()",
      type = "method"
     }
    },
    type = "class"
   },
   KINEMATIC_BODY = {
    description = "value 1",
    type = "value"
   },
   MOUSE_JOINT = {
    description = "value 5",
    type = "value"
   },
   Manifold = {
    childs = {
     localNormal = {
      description = "value \"table\"",
      type = "value"
     },
     localPoint = {
      description = "value \"table\"",
      type = "value"
     },
     points = {
      description = "value \"table\"",
      type = "value"
     }
    },
    type = "class"
   },
   MouseJoint = {
    childs = {
     getDampingRatio = {
      args = "()",
      description = "Returns the damping ratio",
      returns = "()",
      type = "method"
     },
     getFrequency = {
      args = "()",
      description = "Returns the response frequency in Hertz",
      returns = "()",
      type = "method"
     },
     getMaxForce = {
      args = "()",
      description = "Returns the maximum force in N",
      returns = "()",
      type = "method"
     },
     getTarget = {
      args = "()",
      description = "Returns the x and y coordinates of the target point",
      returns = "()",
      type = "method"
     },
     setDampingRatio = {
      args = "(ratio)",
      description = "Sets the damping ratio (0 = no damping, 1 = critical damping)",
      returns = "()",
      type = "method"
     },
     setFrequency = {
      args = "(frequency)",
      description = "Sets the response frequency in Hertz",
      returns = "()",
      type = "method"
     },
     setMaxForce = {
      args = "(force)",
      description = "Sets the maximum force in N",
      returns = "()",
      type = "method"
     },
     setTarget = {
      args = "(x, y)",
      description = "Updates the target point",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   PRISMATIC_JOINT = {
    description = "value 2",
    type = "value"
   },
   PULLEY_JOINT = {
    description = "value 4",
    type = "value"
   },
   ParticleSystem = {
    childs = {
     FLAG_COLOR_MIXING = {
      description = "value 256",
      type = "value"
     },
     FLAG_ELASTIC = {
      description = "value 16",
      type = "value"
     },
     FLAG_POWDER = {
      description = "value 64",
      type = "value"
     },
     FLAG_SPRING = {
      description = "value 8",
      type = "value"
     },
     FLAG_TENSILE = {
      description = "value 128",
      type = "value"
     },
     FLAG_VISCOUS = {
      description = "value 32",
      type = "value"
     },
     FLAG_WALL = {
      description = "value 4",
      type = "value"
     },
     FLAG_WATER = {
      description = "value 0",
      type = "value"
     },
     FLAG_ZOMBIE = {
      description = "value 2",
      type = "value"
     },
     createParticle = {
      args = "(particleDef)",
      description = "Create new particle",
      returns = "()",
      type = "method"
     },
     createParticleGroup = {
      args = "(particleGoupDef)",
      description = "Create group of particles",
      returns = "()",
      type = "method"
     },
     destroyParticle = {
      args = "(id)",
      description = "Destroy particle by id",
      returns = "()",
      type = "method"
     },
     setTexture = {
      args = "(texture)",
      description = "Set texture to particles",
      returns = "()",
      type = "method"
     }
    },
    type = "class"
   },
   PolygonShape = {
    childs = {
     new = {
      args = "()",
      description = "",
      returns = "()",
      type = "function"
     },
     set = {
      args = "(vertices)",
      description = "Sets vertices",
      returns = "()",
      type = "method"
     },
     setAsBox = {
      args = "(hx, hy, centerx, centery, angle)",
      description = "Set vertices to represent an oriented box",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Shape",
    type = "class"
   },
   PrismaticJoint = {
    childs = {
     enableLimit = {
      args = "(flag)",
      description = "Enables or disables the joint limit",
      returns = "()",
      type = "method"
     },
     enableMotor = {
      args = "(flag)",
      description = "Enables or disables the joint motor",
      returns = "()",
      type = "method"
     },
     getJointSpeed = {
      args = "()",
      description = "Returns the current joint translation speed in meters per second",
      returns = "()",
      type = "method"
     },
     getJointTranslation = {
      args = "()",
      description = "Returns the current joint translation in meters",
      returns = "()",
      type = "method"
     },
     getLimits = {
      args = "()",
      description = "Returns the lower and upper joint limits in meters",
      returns = "()",
      type = "method"
     },
     getMotorForce = {
      args = "(inv_dt)",
      description = "Returns the current motor force given the inverse time step",
      returns = "()",
      type = "method"
     },
     getMotorSpeed = {
      args = "()",
      description = "Returns the motor speed in meters per second",
      returns = "()",
      type = "method"
     },
     isLimitEnabled = {
      args = "()",
      description = "Is the joint limit enabled?",
      returns = "()",
      type = "method"
     },
     isMotorEnabled = {
      args = "()",
      description = "Is the joint motor enabled?",
      returns = "()",
      type = "method"
     },
     setLimits = {
      args = "(lower, upper)",
      description = "Sets the joint limits in meters",
      returns = "()",
      type = "method"
     },
     setMaxMotorForce = {
      args = "(force)",
      description = "Sets the maximum motor force in N",
      returns = "()",
      type = "method"
     },
     setMotorSpeed = {
      args = "(speed)",
      description = "Sets the motor speed in meters per second",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   PulleyJoint = {
    childs = {
     getGroundAnchorA = {
      args = "()",
      description = "Returns the x and y coordinates of the first ground anchor",
      returns = "()",
      type = "method"
     },
     getGroundAnchorB = {
      args = "()",
      description = "Returns the x and y coordinates of the second ground anchor",
      returns = "()",
      type = "method"
     },
     getLengthA = {
      args = "()",
      description = "Returns the current length of the segment attached to bodyA",
      returns = "()",
      type = "method"
     },
     getLengthB = {
      args = "()",
      description = "Returns the current length of the segment attached to bodyB",
      returns = "()",
      type = "method"
     },
     getRatio = {
      args = "()",
      description = "Returns the joint ratio",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   REVOLUTE_JOINT = {
    description = "value 1",
    type = "value"
   },
   ROPE_JOINT = {
    description = "value 10",
    type = "value"
   },
   RevoluteJoint = {
    childs = {
     enableLimit = {
      args = "(flag)",
      description = "Enables or disables the joint limit",
      returns = "()",
      type = "method"
     },
     enableMotor = {
      args = "(flag)",
      description = "Enables or disables the joint motor",
      returns = "()",
      type = "method"
     },
     getJointAngle = {
      args = "()",
      description = "Returns the current joint angle in radians",
      returns = "()",
      type = "method"
     },
     getJointSpeed = {
      args = "()",
      description = "Returns the current joint angle speed in radians per second",
      returns = "()",
      type = "method"
     },
     getLimits = {
      args = "()",
      description = "Returns the lower and upper joint limit in radians",
      returns = "()",
      type = "method"
     },
     getMotorSpeed = {
      args = "()",
      description = "Returns the motor speed in radians per second",
      returns = "()",
      type = "method"
     },
     getMotorTorque = {
      args = "(inv_dt)",
      description = "Returns the current motor torque given the inverse time step",
      returns = "()",
      type = "method"
     },
     isLimitEnabled = {
      args = "()",
      description = "Is the joint limit enabled?",
      returns = "()",
      type = "method"
     },
     isMotorEnabled = {
      args = "()",
      description = "Is the joint motor enabled?",
      returns = "()",
      type = "method"
     },
     setLimits = {
      args = "(lower, upper)",
      description = "Sets the joint limits in radians",
      returns = "()",
      type = "method"
     },
     setMaxMotorTorque = {
      args = "(torque)",
      description = "Sets the maximum motor torque in N*m",
      returns = "()",
      type = "method"
     },
     setMotorSpeed = {
      args = "(speed)",
      description = "Sets the motor speed in radians per second",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   RopeJoint = {
    childs = {
     getMaxLength = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     setMaxLength = {
      args = "(maxLength)",
      description = "",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   STATIC_BODY = {
    description = "value 0",
    type = "value"
   },
   WELD_JOINT = {
    description = "value 8",
    type = "value"
   },
   WHEEL_JOINT = {
    description = "value 7",
    type = "value"
   },
   WeldJoint = {
    childs = {
     getDampingRatio = {
      args = "()",
      description = "Returns damping ratio",
      returns = "()",
      type = "method"
     },
     getFrequency = {
      args = "()",
      description = "Returns frequency in Hz",
      returns = "()",
      type = "method"
     },
     setDampingRatio = {
      args = "(damping)",
      description = "Sets damping ratio",
      returns = "()",
      type = "method"
     },
     setFrequency = {
      args = "(frequency)",
      description = "Sets frequency in Hz",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   WheelJoint = {
    childs = {
     enableMotor = {
      args = "(flag)",
      description = "Enables or disables the joint motor",
      returns = "()",
      type = "method"
     },
     getJointSpeed = {
      args = "()",
      description = "Returns the current joint translation speed in meters per second.",
      returns = "()",
      type = "method"
     },
     getJointTranslation = {
      args = "()",
      description = "Returns the current joint translation in meters.",
      returns = "()",
      type = "method"
     },
     getMaxMotorTorque = {
      args = "()",
      description = "Returns the maximum motor torque in N*m",
      returns = "()",
      type = "method"
     },
     getMotorSpeed = {
      args = "()",
      description = "Returns the motor speed in radians per second",
      returns = "()",
      type = "method"
     },
     getSpringDampingRatio = {
      args = "()",
      description = "Returns the spring damping ratio",
      returns = "()",
      type = "method"
     },
     getSpringFrequencyHz = {
      args = "()",
      description = "Returns the spring frequency in Hertz",
      returns = "()",
      type = "method"
     },
     isMotorEnabled = {
      args = "()",
      description = "Is the joint motor enabled?",
      returns = "()",
      type = "method"
     },
     setMaxMotorTorque = {
      args = "(torque)",
      description = "Sets the maximum motor torque in N*m",
      returns = "()",
      type = "method"
     },
     setMotorSpeed = {
      args = "(speed)",
      description = "Sets the motor speed in radians per second",
      returns = "()",
      type = "method"
     },
     setSpringDampingRatio = {
      args = "(damping)",
      description = "Sets the spring damping ratio",
      returns = "()",
      type = "method"
     },
     setSpringFrequencyHz = {
      args = "(frequency)",
      description = "Sets the spring frequency in Hertz (0 = disable the spring)",
      returns = "()",
      type = "method"
     }
    },
    inherits = "b2.Joint",
    type = "class"
   },
   World = {
    childs = {
     clearForces = {
      args = "()",
      description = "Call this after you are done with time steps to clear the forces",
      returns = "()",
      type = "method"
     },
     createBody = {
      args = "(bodyDef)",
      description = "Creates a rigid body given a definition",
      returns = "()",
      type = "method"
     },
     createJoint = {
      args = "(jointDef)",
      description = "Creates a joint given a definition",
      returns = "()",
      type = "method"
     },
     createParticleSystem = {
      args = "(particleSysDef)",
      description = "Create particle system",
      returns = "()",
      type = "method"
     },
     destroyBody = {
      args = "(body)",
      description = "Destroys a rigid body",
      returns = "()",
      type = "method"
     },
     destroyJoint = {
      args = "(joint)",
      description = "Destroys a joint",
      returns = "()",
      type = "method"
     },
     getGravity = {
      args = "()",
      description = "Returns the gravity vector",
      returns = "()",
      type = "method"
     },
     new = {
      args = "(gravityx, gravityy, doSleep)",
      description = "",
      returns = "()",
      type = "function"
     },
     queryAABB = {
      args = "(minx, miny, maxx, maxy)",
      description = "Query the world for all fixtures that potentially overlap the provided AABB",
      returns = "()",
      type = "method"
     },
     rayCast = {
      args = "(x1, y1, x2, y2, listener [, data])",
      description = "Raycast the world for all fixtures in the path of the ray",
      returns = "()",
      type = "method"
     },
     setDebugDraw = {
      args = "()",
      description = "Registers a b2.DebugDraw instance for debug drawing",
      returns = "()",
      type = "method"
     },
     setGravity = {
      args = "(gravityx, gravityy)",
      description = "Sets the gravity vector",
      returns = "()",
      type = "method"
     },
     step = {
      args = "(timeStep, velocityIterations, positionIterations)",
      description = "Takes a time step",
      returns = "()",
      type = "method"
     }
    },
    inherits = "EventDispatcher",
    type = "class"
   },
   WorldManifold = {
    childs = {
     normal = {
      description = "value \"table\"",
      type = "value"
     },
     points = {
      description = "value \"table\"",
      type = "value"
     }
    },
    type = "class"
   },
   createDistanceJointDef = {
    args = "(bodyA, bodyB, anchorAx, anchorAy, anchorBx, anchorBy)",
    description = "Creates and returns a distance joint definition table",
    returns = "()",
    type = "function"
   },
   createFrictionJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory)",
    description = "Creates and returns a friction joint definition table",
    returns = "()",
    type = "function"
   },
   createGearJointDef = {
    args = "(bodyA, bodyB, joint1, joint2, ratio)",
    description = "Creates and returns a gear joint definition table",
    returns = "()",
    type = "function"
   },
   createMouseJointDef = {
    args = "(bodyA, bodyB, targetx, targety, maxForce, frequencyHz, dampingRatio)",
    description = "Creates and returns a mouse joint definition table",
    returns = "()",
    type = "function"
   },
   createPrismaticJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory, axisx, axisy)",
    description = "Creates and returns a prismatic joint definition table",
    returns = "()",
    type = "function"
   },
   createPulleyJointDef = {
    args = "(bodyA, bodyB, groundAnchorAx, groundAnchorAy, groundAnchorBx, groundAnchorBy, anchorAx, anchorAy, anchorBx, anchorBy, ratio)",
    description = "Creates and returns a pulley joint definition table",
    returns = "()",
    type = "function"
   },
   createRevoluteJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory)",
    description = "Creates and returns a revolute joint definition table",
    returns = "()",
    type = "function"
   },
   createRopeJointDef = {
    args = "(bodyA, bodyB, anchorAx, anchorAy, anchorBx, anchorBy, maxLength)",
    description = "",
    returns = "()",
    type = "function"
   },
   createWeldJointDef = {
    args = "(bodyA, bodyB, anchorAx, anchorAy, anchorBx, anchorBy)",
    description = "Creates and returns a weld joint definition table",
    returns = "()",
    type = "function"
   },
   createWheelJointDef = {
    args = "(bodyA, bodyB, anchorx, anchory, axisx, axisy)",
    description = "Creates and returns a wheel joint definition table",
    returns = "()",
    type = "function"
   },
   getScale = {
    args = "()",
    description = "Returns the global pixels to meters scale",
    returns = "()",
    type = "function"
   },
   setScale = {
    args = "(scale)",
    description = "Sets the global pixels to meters scale",
    returns = "()",
    type = "function"
   }
  },
  type = "class"
 },
 flurry = {
  childs = {
   endTimedEvent = {
    args = "(eventName, parameters)",
    description = "Ends Flurry timed event",
    returns = "()",
    type = "function"
   },
   isAvailable = {
    args = "()",
    description = "Returns true if Flurry is available",
    returns = "()",
    type = "function"
   },
   logEvent = {
    args = "(eventName, parameters, timed)",
    description = "Logs Flurry event",
    returns = "()",
    type = "function"
   },
   startSession = {
    args = "(apiKey)",
    description = "Starts the Flurry session with your API key",
    returns = "()",
    type = "function"
   }
  },
  type = "class"
 },
 iad = {
  childs = {
   Banner = {
    childs = {
     BOTTOM = {
      description = "value \"bottom\"",
      type = "value"
     },
     LANDSCAPE = {
      description = "value \"landscape\"",
      type = "value"
     },
     PORTRAIT = {
      description = "value \"portrait\"",
      type = "value"
     },
     TOP = {
      description = "value \"top\"",
      type = "value"
     },
     hide = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     isBannerLoaded = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     },
     new = {
      args = "(alignment, orientation)",
      description = "",
      returns = "()",
      type = "function"
     },
     setAlignment = {
      args = "(alignment)",
      description = "",
      returns = "()",
      type = "method"
     },
     show = {
      args = "()",
      description = "",
      returns = "()",
      type = "method"
     }
    },
    inherits = "EventDispatcher",
    type = "class"
   },
   isAvailable = {
    args = "()",
    description = "",
    returns = "()",
    type = "function"
   }
  },
  type = "class"
 },
 json = {
  childs = {
   decode = {
    args = "(jsondata)",
    description = "Returns Lua table from provided json encoded string",
    returns = "()",
    type = "function"
   },
   encode = {
    args = "(data)",
    description = "Returns encoded json string from provided Lua table",
    returns = "()",
    type = "function"
   }
  },
  type = "class"
 },
 stage = {
  childs = {
   setClearColorBuffer = {
    args = "(state)",
    description = "Enable/disable draw call for background color",
    returns = "()",
    type = "method"
   }
  },
  inherits = "Sprite",
  type = "class"
 },
 utf8 = {
  childs = {
   byte = {
    args = "(s [, i, j])",
    description = "Returns the internal numerical codes of the characters",
    returns = "()",
    type = "function"
   },
   char = {
    args = "(code1 [, code2, codeN])",
    description = "Returns a string from integers as characters",
    returns = "()",
    type = "function"
   },
   charpattern = {
    description = "value \"[\\0-\\x7F\\xC2-\\xF4][\\x80-\\xBF]*\"",
    type = "value"
   },
   charpos = {
    args = "(s [, charpos, offset])",
    description = "Convert UTF-8 position to byte offset",
    returns = "()",
    type = "function"
   },
   codepoint = {
    args = "(s [, i, j])",
    description = "Returns the codepoints (as integers) from all characters",
    returns = "()",
    type = "function"
   },
   codes = {
    args = "(s)",
    description = "Returns values so that the construction",
    returns = "()",
    type = "function"
   },
   escape = {
    args = "(s)",
    description = "Escape a str to UTF-8 format string",
    returns = "()",
    type = "function"
   },
   find = {
    args = "(s, pattern [, init, plain])",
    description = "Looks for the first match of pattern in the string s",
    returns = "()",
    type = "function"
   },
   fold = {
    args = "(s)",
    description = "Convert UTF-8 string s to folded case used to compare by ignore case",
    returns = "()",
    type = "function"
   },
   gmatch = {
    args = "(s, pattern)",
    description = "Returns an iterator function",
    returns = "()",
    type = "function"
   },
   gsub = {
    args = "(s, pattern, repl [, n])",
    description = "Returns a copy of s in which all (or the first n, if given) occurrences of the pattern have been replaced",
    returns = "()",
    type = "function"
   },
   insert = {
    args = "(s [, idx, substring])",
    description = "Insert a substring to s",
    returns = "()",
    type = "function"
   },
   len = {
    args = "(s [, i, j])",
    description = "Returns the number of UTF-8 characters in string",
    returns = "()",
    type = "function"
   },
   lower = {
    args = "(s)",
    description = "Receives a string and returns a copy of this string with all uppercase letters changed to lowercase",
    returns = "()",
    type = "function"
   },
   match = {
    args = "(s, pattern [, init])",
    description = "Looks for the first match of pattern in the string s",
    returns = "()",
    type = "function"
   },
   ncasecmp = {
    args = "(a, b)",
    description = "Compare a and b without case",
    returns = "()",
    type = "function"
   },
   next = {
    args = "(s [, charpos, offset])",
    description = "Iterate though the UTF-8 string s",
    returns = "()",
    type = "function"
   },
   offset = {
    args = "(s, n [, i])",
    description = "Returns the position (in bytes) where the encoding of the n-th character of s",
    returns = "()",
    type = "function"
   },
   remove = {
    args = "(s [, start, stop])",
    description = "Delete a substring in s",
    returns = "()",
    type = "function"
   },
   reverse = {
    args = "(s)",
    description = "Returns a string that is the string s reversed.",
    returns = "()",
    type = "function"
   },
   sub = {
    args = "(s, i [, j])",
    description = "Returns the substring of s that starts at i and continues until j",
    returns = "()",
    type = "function"
   },
   title = {
    args = "(s)",
    description = "Convert UTF-8 string s to title case used to compare by ignore case",
    returns = "()",
    type = "function"
   },
   upper = {
    args = "(s)",
    description = "Receives a string and returns a copy of this string with all lowercase letters changed to uppercase. ",
    returns = "()",
    type = "function"
   },
   width = {
    args = "(s [, ambi_is_double, default_width])",
    description = "Calculate the width of UTF-8 string s",
    returns = "()",
    type = "function"
   },
   widthindex = {
    args = "(s, location [, ambi_is_double, default_width])",
    description = "Return the character index at given location in string s.",
    returns = "()",
    type = "function"
   }
  },
  type = "class"
 },
 zlib = {
  childs = {
   adler32 = {
    args = "([adler32, buffer])",
    description = "Update the adler32 value",
    returns = "()",
    type = "function"
   },
   compress = {
    args = "(buffer [, level, method, windowBits, memLevel, strategy])",
    description = "Return compressed string",
    returns = "()",
    type = "function"
   },
   crc32 = {
    args = "([crc32, buffer])",
    description = "Update the crc32 value",
    returns = "()",
    type = "function"
   },
   decompress = {
    args = "(buffer [, windowBits])",
    description = "Return the decompressed stream",
    returns = "()",
    type = "function"
   },
   deflate = {
    args = "(sink [, level, method, windowBits, memLevel, strategy, dictionary])",
    description = "Return a deflate stream.",
    returns = "()",
    type = "function"
   },
   inflate = {
    args = "(source [, windowBits, dictionary])",
    description = "Return an inflate stream.",
    returns = "()",
    type = "function"
   }
  },
  type = "class"
 }
}

-- when loaded as a package, return the package; otherwise continue with the script
if pcall(debug.getlocal, 4, 1) then return api end

--[[
  Conversion script for Gideros API (http://docs.giderosmobile.com/reference/autocomplete.php)
  Run as "../../bin/lua gideros.lua <gideros_annot.api >newapi" from ZBS/api/lua folder

  Event
  Event.new(type) creates a new Event object
  Event.ENTER_FRAME
  getType() Event - returns the type of Event
  getTarget() Event - returns the element on which the event listener was registered

  Limitations
  - only handles two levels of class hierarchy (as in b2.Body.*)

  Notes
  + replace &quot; with "
  + remove standard Lua functions and classes
  + b2.* and flurry.* don't have any headers (assume those)
  + there some duplicates, like Stage and stage (ignore lowecase ones)
  + remove "CLASS - " from the description
  + b2.World and many others have several levels
  + create different methods for Application and application
  + missing new() methods for some classes (+geolocation, +gyroscope, +accelerometer, +storekit)
  + application, stage, world are global variables
--]]

local class = ""
local t = {}
local inherits = {}
while true do
  local s = io.read()
  if not s then break end

  local newclass, base = s:match('^([%w%.]+)%s+>%s+([%w%.]+)%s*$')
  if base then
    inherits[newclass] = base
  else
    newclass = s:match('^([A-Z]%w+)$') or s:match('^(%w+%.%w+)$')
      or s:match('^([%.%w]+)%.')
  end

  if newclass and class:lower() ~= newclass:lower() then
    class = newclass
    if not class:match('%.') and not _G[class] then
      t[class] = t[class] or {childs = {}, type = "class", inherits = inherits[class]} end
  end
  s = s:gsub('^'..class..'%.', ""):gsub('^'..class:lower()..'%:', "")
  local fun, args, desc = s:match('(%w+)(%b())%s*(.*)%s*$')
  local const, value = s:match('^([A-Z_0-9]+)[ -]+(.+)$')
  -- try one more time with lowercase/mixed constants if nothing has been found
  if not const and not fun and s:find(" value ") then
    const, value = s:match('^([%w_]+)[ -]+(.+)$')
  end
  if #class == 0 then
    -- do nothing; haven't found a single class yet; skipping Lua methods
  elseif _G[class] then
    -- do nothing; skipping Lua tables (io, table, math, etc.)
  elseif s:lower() == class:lower() or base then
    -- do nothing; it's either class or its duplicate
  elseif const or fun then
    local t, class = t, class
    local c1, c2 = class:match('^(%w+)%.(%w+)$')
    if c1 and c2 then
      t[c1] = t[c1] or {childs = {}, type = "class"}
      t = t[c1].childs
      local base = inherits[class]
      class = c2
      t[class] = t[class] or {childs = {}, type = "class", inherits = base}
    end
    if fun then
      local removeclass = "^"..(c2 and c1..'.'..c2 or class)..'[- ]*'
      desc = (desc or "")
        :gsub("&quot;?", '"')
        :gsub(removeclass, "") -- remove class
        :gsub(removeclass, "") -- some descriptions have it twice
        :gsub("^(%w)", string.upper) -- convert first letter to uppercase

      -- function have "Class.function" format and
      -- methods have "function() Class"; "newclass" means it a function
      t[class].childs[fun] = {
        type = (newclass and "function" or "method"),
        args = args or "()",
        description = desc,
        returns = "()",
      }
    elseif const then
      t[class].childs[const] = {type = "value", description = value}
    end
  else
    io.stderr:write("Unrecognized string: "..s, "\n")
    class = ""
  end
end

-- several manual tweaks --

-- move functions/methods to "application" (and to "stage")
-- as there are global variables with these name.
-- "world" is also a global variable, but what are its methods?
for _, class in ipairs({"Application", "Stage"}) do
  local global = class:lower()
  t[global] = t[global] or {childs = {}, type = t[class].type, inherits = t[class].inherits}
  for key, value in pairs(t[class].childs) do
    if value.type == "function" or value.type == "method" then
      t[global].childs[key] = value
      t[class].childs[key] = nil
    end
  end
end

-- add missing new() methods
for _, class in ipairs{'StoreKit'} do
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

package.path = package.path .. ';../../lualibs/?/?.lua;../../lualibs/?.lua'
package.cpath = package.cpath .. ';../../bin/clibs/?.dll'
print((require 'mobdebug').line(t, {indent = ' ', comment = false}))
