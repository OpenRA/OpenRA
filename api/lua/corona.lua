-- Author Sergey Lerg
-- converted from CoronaSDK-APIDOC-2014-2189.zip;
-- the conversion script is at the bottom of this file

do return {
 _BitmapPaint = {
  childs = {
   rotation = {
    description = "",
    type = "value"
   },
   scaleX = {
    description = "",
    type = "value"
   },
   scaleY = {
    description = "",
    type = "value"
   },
   x = {
    description = "",
    type = "value"
   },
   y = {
    description = "",
    type = "value"
   }
  },
  description = "A bitmap paint is a paint that contains a single texture.",
  type = "class"
 },
 _Body = {
  childs = {
   angularDamping = {
    description = "The numerical value for how much the body's rotation should be damped. The default is zero (0). There is no minimum or maximum value-adjust to get your desired results.",
    type = "value"
   },
   angularVelocity = {
    description = "The numerical value of the current angular (rotational) velocity, in degrees per second. There is no minimum or maximum value-adjust to get your desired results.",
    type = "value"
   },
   applyAngularImpulse = {
    args = "( appliedForce )",
    description = "Like applyTorque, except that an angular impulse is a single, momentary jolt.",
    returns = "()",
    type = "method"
   },
   applyForce = {
    args = "( xForce, yForce, bodyX, bodyY )",
    description = "A function that accepts x,y components of a linear force, applied at a given point with x,y world coordinates. If the target point is the body's center of mass, it will tend to push the body in a straight line; if the target is offset from the body's center of mass, the body will spin about its center of mass.",
    returns = "()",
    type = "method"
   },
   applyLinearImpulse = {
    args = "( xForce, yForce, bodyX, bodyY )",
    description = "Like Body:applyForce(), except that an impulse is a single, momentary jolt.",
    returns = "()",
    type = "method"
   },
   applyTorque = {
    args = "( appliedForce )",
    description = "A function that accepts a numerical value for applied rotational force. The body will rotate about its center of mass.",
    returns = "()",
    type = "method"
   },
   bodyType = {
    description = "A string value for the type of physical body being simulated. Possible values include:",
    type = "value"
   },
   getLinearVelocity = {
    args = "()",
    description = "A function that returns x,y components for the body's linear velocity, in pixels per second.",
    returns = "(Numbers)",
    type = "method"
   },
   gravityScale = {
    description = "You can use the gravity scale to adjust the gravity on a single body. For example, setting it to `0` makes the body float (no gravity). The default value is `1.0`, meaning normal gravity.",
    type = "value"
   },
   isAwake = {
    description = "A boolean for the bodies current awake state. By default, all bodies automatically “go to sleep” when nothing interacts with them for a couple of seconds, and then they stop being simulated until something (e.g., a collision) wakes them up. This property can either fetch their current state or forcibly wake them up.",
    type = "value"
   },
   isBodyActive = {
    description = "Used to set or get the bodies current active state. Inactive bodies are not destroyed, but they are removed from the simulation and cease to interact with other bodies.",
    type = "value"
   },
   isBullet = {
    description = "A boolean for whether the body should be treated as a “bullet”. Bullets are subject to continuous collision detection, rather than periodic collision detection at world timesteps. This is more computationally expensive, but it prevents fast-moving objects from passing through solid barriers. The default is `false`.",
    type = "value"
   },
   isFixedRotation = {
    description = "A boolean for whether the rotation of the body should be locked, even if the body is under load or subjected to off-center forces. The default is `false`.",
    type = "value"
   },
   isSensor = {
    description = "A sensor is a fixture that detects collisions but does not produce a response. Sensors do not generate contact points.",
    type = "value"
   },
   isSleepingAllowed = {
    description = "A boolean for whether the body is ever allowed to go to sleep. Keeping bodies awake has a larger overhead, and is often not required, because collisions with other bodies will automatically wake them up. However, forcing awakeness is useful in cases such as the tilt-gravity case (since sleeping bodies do not respond to changes in global gravity). The default is `true`.",
    type = "value"
   },
   linearDamping = {
    description = "The numerical value for how much the body's linear motion is damped. The default is 0 (zero).",
    type = "value"
   },
   resetMassData = {
    args = "()",
    description = "If the default mass data for the body has been overridden (TBD), this function resets it to the mass calculated from the shapes.",
    returns = "()",
    type = "method"
   },
   setLinearVelocity = {
    args = "( xVelocity, yVelocity )",
    description = "A function that accepts x,y components for the body's linear velocity, in pixels per second.",
    returns = "()",
    type = "method"
   }
  },
  description = "Body objects are what interact with the physics world in Corona, are attached to display objects.",
  type = "class"
 },
 _ButtonWidget = {
  childs = {
   getLabel = {
    args = "()",
    description = "Returns the Button label text as a string.",
    returns = "(String)",
    type = "method",
    valuetype = "string"
   },
   setEnabled = {
    args = "( isEnabled )",
    description = "Sets the Button as either enabled or disabled.",
    returns = "()",
    type = "method"
   },
   setLabel = {
    args = "( label )",
    description = "Sets/updates the Button label text.",
    returns = "()",
    type = "method"
   }
  },
  description = "Button objects are created using widget.newButton().",
  type = "class"
 },
 _CirclePath = {
  childs = {},
  description = "The circle path specifies the geometry of the corresponding shape object. ",
  type = "class"
 },
 _CompositePaint = {
  childs = {},
  description = "Composite paints contain multiple textures. They enable multi-texturing.",
  type = "class"
 },
 _CoronaLibrary = {
  childs = {
   getCurrentProvider = {
    args = "()",
    description = "Returns the currently set provider for the library.",
    returns = "(CoronaProvider)",
    type = "method",
    valuetype = "_CoronaProvider"
   },
   name = {
    description = "The name of the library",
    type = "value"
   },
   publisherId = {
    description = "The name of the publisher.",
    type = "value"
   },
   revision = {
    description = "The revision number of the library.",
    type = "value"
   },
   setCurrentProvider = {
    args = "( provider )",
    description = "Sets the current provider for the library.",
    returns = "()",
    type = "method"
   },
   version = {
    description = "The version number of the library.",
    type = "value"
   }
  },
  description = "This is the standard type for Corona libraries in Lua.",
  type = "class"
 },
 _CoronaPrototype = {
  childs = {
   initialize = {
    args = "()",
    description = "Subclasses can override this method to provide custom initialization of instances.",
    returns = "(TYPE)",
    type = "method"
   },
   instanceOf = {
    args = "( class )",
    description = "Determines if `object` is an instance of `class`.",
    returns = "(Boolean)",
    type = "method"
   },
   isClass = {
    args = "()",
    description = "Tells you whether `object` is really a class.",
    returns = "(Boolean)",
    type = "method"
   },
   isRoot = {
    args = "()",
    description = "Tells you whether `object` is the root class, i.e. CoronaPrototype.",
    returns = "(Boolean)",
    type = "method"
   },
   new = {
    args = "()",
    description = "Constructor for creating new object instances. The `object` is assumed to be a class object.",
    returns = "(CoronaPrototype)",
    type = "method",
    valuetype = "_CoronaPrototype"
   },
   newClass = {
    args = "( name )",
    description = "Constructor for creating new class objects. The `object` is assumed to be a class object.",
    returns = "(CoronaClass)",
    type = "method"
   },
   setExtension = {
    args = "( indexFunc )",
    description = "Objects based on CoronaPrototype cannot have their `__index` metamethod overridden.",
    returns = "()",
    type = "method"
   }
  },
  description = "Insert overview",
  type = "class"
 },
 _CoronaProvider = {
  childs = {},
  description = "Insert overview",
  type = "class"
 },
 _DisplayObject = {
  childs = {
   alpha = {
    description = "This property represents the alpha value of a display object. Use it to set or retrieve the object's opacity. A value of `0` is transparent and `1.0` is fully opaque.",
    type = "value"
   },
   anchorX = {
    description = "This property allows you to control the alignment of the object along the __x__ direction.",
    type = "value"
   },
   anchorY = {
    description = "This property allows you to control the alignment of the object along the __y__ direction.",
    type = "value"
   },
   contentBounds = {
    description = "A read-only table with properties `xMin`, `xMax`, `yMin`, `yMax` that represent the boundaries of a display object, in content coordinates.",
    type = "value"
   },
   contentHeight = {
    description = "The read-only height of the object in content coordinates. This is similar to object.height except that its value is affected by __y__ scaling and rotation.",
    type = "value"
   },
   contentToLocal = {
    args = "( xContent, yContent )",
    description = "Maps the given __x__ and __y__ values in content (stage) coordinates to the target object's local coordinates (center point).",
    returns = "(Numbers)",
    type = "method"
   },
   contentWidth = {
    description = "The read-only width of the object in content coordinates. This is similar to object.width except that its value is affected by __x__ scaling and rotation.",
    type = "value"
   },
   height = {
    description = "Retrieve or change the height of a display object. For text objects, this property can be used to get (but not set) the height.",
    type = "value"
   },
   isHitTestMasked = {
    description = "Limits touch events to the masked portion of the object. This property can be read or set.",
    type = "value"
   },
   isHitTestable = {
    description = "Allows an object to continue to receive hit events even if it is not visible. If `true`, objects will receive hit events regardless of visibility; if `false`, events are only sent to visible objects. Defaults to `false`.",
    type = "value"
   },
   isVisible = {
    description = "Controls whether the object is visible on the screen. The property is also readable. The default is `true`.",
    type = "value"
   },
   localToContent = {
    args = "( x, y )",
    description = "Maps the given __x__ and __y__ coordinates of an object to content (stage) coordinates.",
    returns = "(Numbers)",
    type = "method"
   },
   maskRotation = {
    description = "Retrieve or set the rotation of the display object's corresponding mask object, if one exists.",
    type = "value"
   },
   maskScaleX = {
    description = "Retrieve or set the x-scale factor for the display object's corresponding mask object, if any.",
    type = "value"
   },
   maskScaleY = {
    description = "Retrieves or sets the y-scale factor for the display object's corresponding mask object, if any.",
    type = "value"
   },
   maskX = {
    description = "Retrieve or set the __x__ position of the mask applied to the display object using object:setMask().",
    type = "value"
   },
   maskY = {
    description = "Retrieve or set the __y__ position of the mask applied to the display object using object:setMask().",
    type = "value"
   },
   parent = {
    description = "A read-only property that returns the object's parent.",
    type = "value"
   },
   removeSelf = {
    args = "()",
    description = "Removes the display object and frees its memory, assuming there are no other references to it. This is equivalent to calling group:remove() on the same display object, but it is syntactically simpler. The `object:removeSelf()` syntax is also supported in other cases, such as removing physics joints in the physics engine.",
    returns = "()",
    type = "method"
   },
   rotate = {
    args = "( deltaAngle )",
    description = "Effectively adds a value (`deltaAngle`) to the object's current rotation property value. The rotation occurs around the object's anchor point.",
    returns = "()",
    type = "method"
   },
   rotation = {
    description = "Change or retrieve the rotation of an object. The rotation occurs around the object's anchor point.",
    type = "value"
   },
   scale = {
    args = "( xScale, yScale )",
    description = "Effectively multiplies the size of a display object by `xScale` and `yScale` respectively. The scaling occurs around the object's anchor point.",
    returns = "()",
    type = "method"
   },
   setMask = {
    args = "( mask )",
    description = "Associates a mask with a display object. To remove an object's mask, use `object:setMask( nil )`. You can modify a display object's mask __x__ and __y__ position (object.maskX).",
    returns = "()",
    type = "method"
   },
   setReferencePoint = {
    args = "( referencePoint )",
    description = "Sets the reference point either to the center of the object (default) or to one of several convenient points along the bounding box of the object.",
    returns = "()",
    type = "method"
   },
   toBack = {
    args = "()",
    description = "Moves the target object to the visual back of its parent group (object.parent).",
    returns = "()",
    type = "method"
   },
   toFront = {
    args = "()",
    description = "Moves the target object to the visual front of its parent group (object.parent).",
    returns = "()",
    type = "method"
   },
   translate = {
    args = "( deltaX, deltaY )",
    description = "Effectively adds values to the object.x properties of an object (changing its on-screen position).",
    returns = "()",
    type = "method"
   },
   width = {
    description = "Retrieve or change the width of a display object. For text objects, this property can be used to get (but not set) the width.",
    type = "value"
   },
   x = {
    description = "Specifies the __x__ position (in local coordinates) of the object relative to its parent point relative to its parent. Changing this value will move the object in the __x__ direction.",
    type = "value"
   },
   xScale = {
    description = "Retrieve or change the scale of the object in the __x__ direction. The scaling occurs around the object's anchor point.",
    type = "value"
   },
   y = {
    description = "Specifies the __y__ position (in local coordinates) of the object relative to its parent point relative to its parent. Changing this value will move the object in the __y__ direction.",
    type = "value"
   },
   yScale = {
    description = "Retrieve or change the scale of the object in the __y__ direction. The scaling occurs around the object's anchor point.",
    type = "value"
   }
  },
  description = "All drawing that occurs on the screen is accomplished by creating display objects. Anything that appears on the screen is an instance of a display object.",
  type = "class"
 },
 _EventListener = {
  childs = {
   addEventListener = {
    args = "( eventName, listener )",
    description = "Adds a listener to the object's list of listeners. When the named event occurs, the listener will be invoked and be supplied with a table representing the event.",
    returns = "()",
    type = "method"
   },
   dispatchEvent = {
    args = "( event )",
    description = "Dispatches specified `event` to object. The event parameter must be a table with a `name` property which is a String object, if it has a listener registered to receive name events. We recommend you also include a `target` property in your event to the event so that your listener can know which object received the event.",
    returns = "()",
    type = "method"
   },
   removeEventListener = {
    args = "( eventName, listener )",
    description = "Removes the specified listener from the object's list of listeners so that it no longer is notified of events corresponding to the specified event.",
    returns = "()",
    type = "method"
   }
  },
  description = "`EventListener` is any DisplayObject object) that can receive events.",
  type = "class"
 },
 _File = {
  childs = {
   close = {
    args = "()",
    description = "Closes the open file (file is returned from io.open()).",
    returns = "()",
    type = "method"
   },
   flush = {
    args = "()",
    description = "Commits the file's output buffer. Saves any written data to the file.",
    returns = "()",
    type = "method"
   },
   lines = {
    args = "()",
    description = "Returns an iterator function that, each time it is called, returns a new line from the file.",
    returns = "(Function)",
    type = "method"
   },
   read = {
    args = "( [fmt1] [, fmt2] [, ...] )",
    description = "eads the file file, according to the given formats, which specify what to read. For each format, the function returns a String) with the characters read, or `nil` if it cannot read data with the specified format. When called without formats, it uses a default format that reads the entire next line.",
    returns = "(String)",
    type = "method",
    valuetype = "string"
   },
   seek = {
    args = "( [mode] [, offset] )",
    description = "Sets and gets the file position, measured from the beginning of the file, to the position given by offset plus a base.",
    returns = "(Number)",
    type = "method"
   },
   setvbuf = {
    args = "( mode [, size ] )",
    description = "Sets the buffering mode for an output file (or console).",
    returns = "()",
    type = "method"
   },
   write = {
    args = "( arg1 [, arg2] [, ...] )",
    description = "Writes the value of each of its arguments to the file. The arguments must be Strings before calling `File:write()`.",
    returns = "()",
    type = "method"
   }
  },
  description = "File objects in Corona are used to interact with (read and write) external files, and are returned from io.open().",
  type = "class"
 },
 _GameNetwork = {
  childs = {
   match = {
    description = "A match object returned from gameNetwork.",
    type = "value"
   },
   outcome = {
    description = "Valid string outcome values for a match participant",
    type = "value"
   },
   participant = {
    description = "A participant object returned from gameNetwork.",
    type = "value"
   },
   status = {
    description = "Valid string outcome values for a match status",
    type = "value"
   }
  },
  description = "The details for various items returned from gameNetwork callbacks",
  type = "class"
 },
 _GradientPaint = {
  childs = {},
  description = "A linear gradient is a linear progression from one color to another.",
  type = "class"
 },
 _GroupObject = {
  childs = {
   insert = {
    args = "( [index,] child, [, resetTransform] )",
    description = "Inserts an object into a group.",
    returns = "()",
    type = "method"
   },
   numChildren = {
    description = "Retrieve the number of children in a group. You access the children by integer (whole number) index.",
    type = "value"
   },
   remove = {
    args = "( indexOrChild )",
    description = "Remove an object from a group by either an index number or a reference to the object.",
    returns = "()",
    type = "method"
   }
  },
  description = "Group objects are a special type of display object. You can add other display objects as children of a group object. You can also remove them. Even if an object is not visible, it remains in the group object until explicitly removed. Thus, to minimize memory consumption, you should explicitly remove any object that will no longer be used.",
  type = "class"
 },
 _ImageSheet = {
  childs = {},
  description = "ImageSheet objects are created using graphics.newImageSheet().",
  type = "class"
 },
 _ImageSheetPaint = {
  childs = {},
  description = "Image sheets are a special kind of BitmapPaint for specifying texture atlases.",
  type = "class"
 },
 _InputAxis = {
  childs = {
   accuracy = {
    description = "The +/- accuracy of the data provided by the axis input. This value will always be greater than or equal to zero.",
    type = "value"
   },
   descriptor = {
    description = "Provides a human readable string assigned by Corona used to uniquely identify one axis input belonging to one device. This descriptor string is intended to be used by applications that set up key and axis bindings with a particular device, such as the first joystick connected to the system.",
    type = "value"
   },
   maxValue = {
    description = "Indicates the maximum value that an axis input event will provide. This property and the minValue property are needed to make sense of the raw data received by an axis, because it will indicate how far that axis has been moved relative to its range.",
    type = "value"
   },
   minValue = {
    description = "Indicates the minimum value that an axis input event will provide. This property and the maxValue property are needed to make sense of the raw data received by an axis, because it will indicate how far that axis has been moved relative to its range.",
    type = "value"
   },
   number = {
    description = "The number assigned to an input device's axis. This number is based on the number of axes found on one input device. For example, if an input device has 4 axes, then they will be assigned numbers 1, 2, 3, and 4 in the order that they were found. You can use this number to uniquely identify an axis belonging to one input device.",
    type = "value"
   },
   type = {
    description = "* `InputAxis:type`\9\9<!--- completion -->",
    type = "value"
   }
  },
  description = "Provides information about one analog axis input belonging to an InputDevice, such as an x-axis or y-axis of a joystick, a gamepad's left analog trigger, a gamepad's right analog trigger, etc. The main usage of this object is to acquire the min and max values that the axis can provide in order to make sense of its data.",
  type = "class"
 },
 _InputDevice = {
  childs = {
   androidDeviceId = {
    description = "The unique integer ID assigned to the input device by Android.",
    type = "value"
   },
   canVibrate = {
    description = "Determines if the input device supports vibration/rumble feedback. This is a features that usually gamepads support.",
    type = "value"
   },
   connectionState = {
    description = "Indicates the input device's current connection state with the system such as \"connected\", \"disconnected\", etc.",
    type = "value"
   },
   descriptor = {
    description = "Provides a unique human readable string assigned by Corona used to uniquely identify the device. This descriptor string is intended to be used by applications that set up key bindings with a particular device, such as the first joystick connected to the system.",
    type = "value"
   },
   displayName = {
    description = "The name that was assigned to the input device by the system or end-user. Can be used to display the name of the device to the end-user, such as on a key/button binding screen.",
    type = "value"
   },
   getAxes = {
    args = "()",
    description = "Fetches information about all axis inputs available on the device. This information can be used to detect the device's capabilities, such as whether or not an analog joystick is available.",
    returns = "(Array)",
    type = "method"
   },
   isConnected = {
    description = "Determines if the input device is currently connected to the system and can provide input events.",
    type = "value"
   },
   permanentId = {
    description = "Provides the input device's unique string ID which will be consistent between reboots. That is, the ID for the input device will not change after rebooting the system. This means that this ID can be saved to file and used to set up key bindings with a very specific input device.",
    type = "value"
   },
   type = {
    description = "A string that specifies what kind of device is connected to the system. Possible values are:",
    type = "value"
   },
   vibrate = {
    args = "()",
    description = "Causes the input device to vibrate/rumble.",
    returns = "()",
    type = "method"
   }
  },
  description = "An object of this type respresent one input device such as a keyboard, mouse, touchscreen, gamepad, etc. This object provides access to the input device's information and its current connection status.",
  type = "class"
 },
 _Joint = {
  childs = {
   dampingRatio = {
    description = "The joint's damping ratio. Valid values ranges from 0 (no damping) to 1 (critical damping).",
    type = "value"
   },
   frequency = {
    description = "The mass-spring damping frequency (in Hz).",
    type = "value"
   },
   getAnchorA = {
    args = "()",
    description = "A function that returns the `x,y` coordinates of the Joint's anchor points within objects A and B, which are the two joined objects. The values are given in the local coordinates of each object, so an anchor point at the center of the object would be at position `0, 0`.",
    returns = "(Numbers)",
    type = "method"
   },
   getAnchorB = {
    args = "()",
    description = "A function that returns the `x,y` coordinates of the Joint's anchor points within objects A and B, which are the two joined objects. The values are given in the local coordinates of each object, so an anchor point at the center of the object would be at position `0, 0`.",
    returns = "(Numbers)",
    type = "method"
   },
   getGroundAnchorA = {
    args = "()",
    description = "A function that returns the `x,y` coordinates of the Joint's first ground anchor in world coordinates.",
    returns = "(TYPE)",
    type = "method"
   },
   getGroundAnchorB = {
    args = "()",
    description = "A function that returns the `x,y` coordinates of the Joint's second ground anchor in world coordinates.",
    returns = "(TYPE)",
    type = "method"
   },
   getLimits = {
    args = "()",
    description = "Returns the upper and lower limits of linear motion set on the joint with Joint:setLimits().",
    returns = "(Numbers)",
    type = "method"
   },
   getLocalAnchorA = {
    args = "()",
    description = "A function that returns the `x,y` coordinates of the Joint's anchor points within objects A and B, which are the two joined objects. The values are given in the local coordinates of each object, so an anchor point at the center of the object would be at position `0, 0`.",
    returns = "(Numbers)",
    type = "method"
   },
   getLocalAnchorB = {
    args = "()",
    description = "A function that returns the `x,y` coordinates of the Joint's anchor points within objects A and B, which are the two joined objects. The values are given in the local coordinates of each object, so an anchor point at the center of the object would be at position `0, 0`.",
    returns = "(Numbers)",
    type = "method"
   },
   getLocalAxisA = {
    args = "()",
    description = "A function that returns the `x,y` coordinates of the Joint's anchor points within objects A and B, which are the two joined objects. The values are given in the local coordinates of each object, so an anchor point at the center of the object would be at position `0, 0`.",
    returns = "(Numbers)",
    type = "method"
   },
   getReactionForce = {
    args = "()",
    description = "A function that returns the reaction force (in Newtons) at the joint anchor in the second body.",
    returns = "(Numbers)",
    type = "method"
   },
   getRotationLimits = {
    args = "()",
    description = "Returns the joint's rotation limits (two return values).",
    returns = "(Numbers)",
    type = "method"
   },
   getTarget = {
    args = "()",
    description = "Returns the x,y position of the target in world coordinates.",
    returns = "(Number)",
    type = "method"
   },
   isActive = {
    description = "Specifies whether the joint is limited (`true`) or not (`false`).",
    type = "value"
   },
   isCollideConnected = {
    description = "Specifies if the joint objects will follow the collision rules (`true`) or not (`false`). In some cases it is useful to allow the joint bodies to collide (for example in a platform that's connected to a ground body), in other cases, not. ",
    type = "value"
   },
   isLimitEnabled = {
    description = "Specifies whether the joint is limited (`true`) or not (`false`).",
    type = "value"
   },
   isMotorEnabled = {
    description = "Indicates whether the piston joint is enabled by a motor or not. Unlike the pivot joint, motor-driven motion will be linear, along the specified axis, rather than rotational.",
    type = "value"
   },
   joint1 = {
    description = "Property of Gear joints. Either a piston of pivot joint.",
    type = "value"
   },
   joint2 = {
    description = "Property of Gear joints. Either a piston of pivot joint.",
    type = "value"
   },
   jointAngle = {
    description = "A read-only property that provides the current angle of the joint in degrees.",
    type = "value"
   },
   jointSpeed = {
    description = "A read-only property that provides the speed of the joint in degrees per second.",
    type = "value"
   },
   jointTranslation = {
    description = "Read-only property that provides the linear translation of the joint in pixels.",
    type = "value"
   },
   length = {
    description = "The distance in pixels between the anchor points. The value you assign to this property should not be zero or very short. Consider using a pivot joint if you need to directly anchor one body to another.",
    type = "value"
   },
   length1 = {
    description = "A read-only property that provides the distance in pixels between the first joint anchor point and the stationary pulley anchor point.",
    type = "value"
   },
   length2 = {
    description = "A read-only property that provides the distance in pixels between the second joint anchor point and the stationary pulley anchor point.",
    type = "value"
   },
   limitState = {
    description = "The maximum length of the rope in pixels.",
    type = "value"
   },
   maxForce = {
    description = "The maximum force that can be exerted on the joint.",
    type = "value"
   },
   maxLength = {
    description = "The maximum length of the rope in pixels.",
    type = "value"
   },
   maxMotorForce = {
    description = "A number that specifies the maximum motor force that can be applied to the joint.",
    type = "value"
   },
   maxMotorTorque = {
    description = "Specifies the maximum motor torque that can be applied to the joint.",
    type = "value"
   },
   maxTorque = {
    description = "The maximum torque that can be applied to the joint.",
    type = "value"
   },
   motorForce = {
    description = "Read-only property that specifies the current motor force.",
    type = "value"
   },
   motorSpeed = {
    description = "Specifies the linear speed of the motor, in units of pixels per second.",
    type = "value"
   },
   motorTorque = {
    description = "A read-only property that provides the torque of the joint's motor.",
    type = "value"
   },
   ratio = {
    description = "Used to optionally simulate a block and tackle arrangement, in which one side of the rope moves faster than the other. By default, this ratio is 1.0, which simulates a simple pulley.",
    type = "value"
   },
   reactionTorque = {
    description = "A read-only property that returns the reaction torque (in N*m) at the joint anchor in the second body.",
    type = "value"
   },
   referenceAngle = {
    description = "A read-only property that provides the current angle of the joint in degrees.",
    type = "value"
   },
   removeSelf = {
    args = "()",
    description = "Destroys an existing joint, and detaches the two bodies (which may then collide).",
    returns = "(TYPE)",
    type = "method"
   },
   setLimits = {
    args = "( upper, lower )",
    description = "Sets lower and upper limits on the joint's range of linear motion.",
    returns = "()",
    type = "method"
   },
   setRotationLimits = {
    args = "( lowerLimit, upperLimit )",
    description = "Sets upper and lower rotation limits on the joint in degrees.",
    returns = "()",
    type = "method"
   },
   setTarget = {
    args = "( x, y )",
    description = "For use in Touch joints.",
    returns = "()",
    type = "method"
   },
   springDampingRatio = {
    description = "The joint's damping ratio. Valid values ranges from 0 (no damping) to 1 (critical damping).",
    type = "value"
   },
   springFrequency = {
    description = "The mass-spring damping frequency (in Hz). A value of `0` indicates no suspension.",
    type = "value"
   }
  },
  description = "Joint objects are used to join two physics bodies together and are created using physics.newJoint().",
  type = "class"
 },
 _LineObject = {
  childs = {
   anchorSegments = {
    description = "This boolean property controls whether LineObjects into account. This value is `false` by default.",
    type = "value"
   },
   append = {
    args = "( x, y [, ... ] )",
    description = "Append one or more segments to an existing object created using display.newLine().",
    returns = "()",
    type = "method"
   },
   setColor = {
    args = "( red, green, blue [, alpha ] )",
    description = "Sets the color for an object created using display.newLine().",
    returns = "()",
    type = "method"
   }
  },
  description = "Line objects are created using display.newLine().",
  type = "class"
 },
 _Map = {
  childs = {
   addMarker = {
    args = "( latitude, longitude )",
    description = "Adds a pin to the map at the specified location. The optional title and subtitle will appear on a small popup when the pin is touched.  Returns an number for the id of the marker added or nil if there was a failure.",
    returns = "(Number)",
    type = "method"
   },
   getAddressLocation = {
    args = "( location )",
    description = "**Note: As of 2013/3, this function is deprecated and non-functional. Use object:requestLocation() instead.**",
    returns = "()",
    type = "method"
   },
   getUserLocation = {
    args = "()",
    description = "Returns a table containing values for the user's current location. The fields of this table are the same as those currently used in GPS location events.",
    returns = "(location)",
    type = "method",
    valuetype = "_location"
   },
   isLocationVisible = {
    description = "A read-only Boolean value indicating whether the user's current location is visible within the area currently displayed on the map.",
    type = "value"
   },
   isScrollEnabled = {
    description = "A Boolean that determines whether users can scroll the map by hand. Default is `true`. Set to `false` to prevent users from scrolling the map.",
    type = "value"
   },
   isZoomEnabled = {
    description = "A Boolean that determines whether users may use pinch/zoom gestures to zoom the map. Default is `true`. Set to `false` to prevent users from zooming the map.",
    type = "value"
   },
   mapType = {
    description = "A string that specifies the type of map display. Possible values are:",
    type = "value"
   },
   nearestAddress = {
    args = "( latitude, longitude, resultHandler )",
    description = "Returns the nearest address based on the given latitude and longitude values. The address is returned as a mapAddress event.",
    returns = "()",
    type = "method"
   },
   removeAllMarkers = {
    args = "()",
    description = "Clears all markers (pins) from the map.",
    returns = "()",
    type = "method"
   },
   removeMarker = {
    args = "(markerId)",
    description = "Clears a specific marker (pin) from the map.",
    returns = "()",
    type = "method"
   },
   requestLocation = {
    args = "( location, resultHandler )",
    description = "This is a replacement for the deprecated object:getAddressLocation().",
    returns = "()",
    type = "method"
   },
   setCenter = {
    args = "( latitude, longitude [, isAnimated] )",
    description = "Moves the displayed map region to a new location, using the new center point but keeping the zoom level the same. The final parameter is an optional Boolean (default `false`) that determines whether the transition is animated or happens instantly.",
    returns = "()",
    type = "method"
   },
   setRegion = {
    args = "( latitude, longitude, latitudeSpan, longitudeSpan [, isAnimated] )",
    description = "Moves the displayed map region to a new location, with the new center point and horizontal/vertical span distances given in degrees of latitude and longitude (which implicitly sets the zoom level). This function will sanity-check the span settings, and will interpolate a consistent zoom level even if the latitudeSpan and longitudeSpan are specified with radically different values. The final parameter is an optional Boolean (default `false`) that determines whether the transition is animated or happens instantly.",
    returns = "()",
    type = "method"
   }
  },
  description = "The map view feature lets the developer integrate one or more map views into their app, and supports various methods of two-way communication between the map contents and the surrounding application, including address or landmark lookup, marking the map with pins, and reverse geocoding (converting latitude/longitude into the closest street address).",
  type = "class"
 },
 _Mask = {
  childs = {},
  description = "Mask objects are created with graphics.newMask().",
  type = "class"
 },
 _NativeDisplayObject = {
  childs = {},
  description = "NativeDisplayObjects are separate from Corona's OpenGL drawing canvas, and always appear above normal display objects.",
  type = "class"
 },
 _Paint = {
  childs = {},
  description = "Paint objects control the way in which geometry is rasterized. ",
  type = "class"
 },
 _Path = {
  childs = {},
  description = "Paths specify the geometry of shapes. Path objects give you access to manipulate this geometry.",
  type = "class"
 },
 _PhysicsContact = {
  childs = {
   bounce = {
    description = "Overrides the bounciness (\"restitution\" in Box2D terms) of the contact. This can be set inside the preCollision event listener.",
    type = "value"
   },
   friction = {
    description = "Overrides the default friction of the contact. This can be set inside the preCollision event listener.",
    type = "value"
   },
   isEnabled = {
    description = "Enable/disable the contact. This can be used inside the preCollision event listener. The contact is only disabled for the current time step.",
    type = "value"
   },
   isTouching = {
    description = "Indicates whether this contact is touching.",
    type = "value"
   }
  },
  description = "Physics contact objects are created by Box2D to manage collisions. ",
  type = "class"
 },
 _PickerWidget = {
  childs = {
   getValues = {
    args = "()",
    description = "Returns a table with the currently selected value/index of each column in the picker wheel.",
    returns = "(Table)",
    type = "method"
   }
  },
  description = "PickerWheel objects are created using widget.newPickerWheel().",
  type = "class"
 },
 _ProgressViewWidget = {
  childs = {
   getProgress = {
    description = "Returns the current progress value of a ProgressView.",
    type = "value"
   },
   resizeView = {
    description = "Resizes the width of a ProgressView after creation.",
    type = "value"
   },
   setProgress = {
    description = "Sets the current progress of a ProgressView.",
    type = "value"
   }
  },
  description = "ProgressView objects are created using widget.newProgressView().",
  type = "class"
 },
 _Recording = {
  childs = {
   getSampleRate = {
    args = "()",
    description = "Gets the current audio recording sample rate. (Used for recording audio.)",
    returns = "(Number)",
    type = "method"
   },
   getTunerFrequency = {
    args = "()",
    description = "Returns the last calculated frequency in Hz. Works only if the tuner was on. See: Recording:startTuner().",
    returns = "(Number)",
    type = "method"
   },
   getTunerVolume = {
    args = "()",
    description = "Returns the mean squared normalized sample value of the current audio buffer. Works only if the tuner was on. See: Recording:startTuner().",
    returns = "(Number)",
    type = "method"
   },
   isRecording = {
    args = "()",
    description = "Returns `true` if audio recording is currently in progress; `false` if otherwise.",
    returns = "(Boolean)",
    type = "method"
   },
   setSampleRate = {
    args = "( r )",
    description = "Request an audio recording sample rate.",
    returns = "()",
    type = "method"
   },
   startRecording = {
    args = "()",
    description = "Start recording audio.",
    returns = "()",
    type = "method"
   },
   startTuner = {
    args = "()",
    description = "Enables audio tuning feature. This function should be called before Recording:startRecording().",
    returns = "()",
    type = "method"
   },
   stopRecording = {
    args = "()",
    description = "Stops recording audio.",
    returns = "()",
    type = "method"
   },
   stopTuner = {
    args = "()",
    description = "Stops the tuner module.",
    returns = "()",
    type = "method"
   }
  },
  description = "Recording objects are created using media.newRecording().",
  type = "class"
 },
 _RectPath = {
  childs = {},
  description = "Paths specify the geometry of shapes. Path objects give you access to manipulate this geometry.",
  type = "class"
 },
 _RoundedRectPath = {
  childs = {},
  description = "The rounded rectangle path specifies the geometry of the corresponding shape object. ",
  type = "class"
 },
 _Runtime = {
  childs = {
   hasEventSource = {
    args = "( eventSourceName )",
    description = "Determines if the device is capable of providing events for a given event source such as an `\"accelerometer\"` or `\"gyroscope\"`. You should call this function before calling the Runtime:addEventListener() function.",
    returns = "(Boolean)",
    type = "method"
   },
   hideErrorAlerts = {
    args = "( )",
    description = "Disables the runtime error alert that appears if the application hits an error condition.  It's a shorthand for defining your own `unhandledError` listener and returning `true`.",
    returns = "()",
    type = "method"
   }
  },
  description = "The `Runtime` class inherits from EventListener.",
  type = "class"
 },
 _Scene = {
  childs = {},
  description = "Scene objects are the focal point of the Storyboard API.",
  type = "class"
 },
 _ScrollViewWidget = {
  childs = {
   getContentPosition = {
    args = "()",
    description = "Returns the __x__ and __y__ coordinates of the ScrollView content.",
    returns = "(Numbers)",
    type = "method"
   },
   scrollTo = {
    args = "( position, options )",
    description = "Makes a ScrollView scroll to a specified position constant.",
    returns = "()",
    type = "method"
   },
   scrollToPosition = {
    args = "( options )",
    description = "Makes a ScrollView scroll to a specific __x__ or __y__ position.",
    returns = "()",
    type = "method"
   },
   setIsLocked = {
    args = "( isLocked )",
    description = "Sets a ScrollView to either locked (does not scroll) or unlocked (default behavior).",
    returns = "()",
    type = "method"
   },
   setScrollHeight = {
    args = "( newHeight )",
    description = "Updates the scrollable content height of a ScrollView.",
    returns = "(Number)",
    type = "method"
   },
   setScrollWidth = {
    args = "( newWidth )",
    description = "Updates the scrollable content width of a ScrollView.",
    returns = "(Number)",
    type = "method"
   },
   takeFocus = {
    args = "( event )",
    description = "If you have an object with a touch listener inside a scroll view, such as a Button widget, you should call this method within the `\"moved\"` phase of that object's touch listener and pass the touch event's `event` table as the sole parameter of this method. This will give focus back to the scroll view, enabling it to continue to scroll.",
    returns = "(Number)",
    type = "method"
   }
  },
  description = "ScrollView objects are created using widget.newScrollView().",
  type = "class"
 },
 _SegmentedControlWidget = {
  childs = {
   segmentLabel = {
    description = "The read-only label of the currently-selected segment on a Segmented Control object.",
    type = "value"
   },
   segmentNumber = {
    description = "The read-only segment number of the currently-selected segment on a Segmented Control object.",
    type = "value"
   }
  },
  description = "SegmentedControl objects are created using widget.newSegmentedControl().",
  type = "class"
 },
 _ShapeObject = {
  childs = {
   blendMode = {
    description = "Allows you to change the blend mode on a specific object. ",
    type = "value"
   },
   fill = {
    description = "For object fills, Corona uses the concept of paint to a fill, you control how the interior area of the shape is rendered.",
    type = "value"
   },
   path = {
    description = "Paths are a property of shapes that let you control the geometry of the shape.",
    type = "value"
   },
   setFillColor = {
    args = "( gray )",
    description = "Sets the fill color of vector and text objects. Also applies a tint to image objects.",
    returns = "()",
    type = "method"
   },
   setStrokeColor = {
    args = "( gray )",
    description = "Sets the stroke (border) color of vector objects.",
    returns = "()",
    type = "method"
   },
   stroke = {
    description = "For object strokes, Corona uses the concept of paint to a stroke, you control how the boundary is rendered.",
    type = "value"
   },
   strokeWidth = {
    description = "Sets the stroke width of vector objects, in pixels. Note that stroke widths are broken up to inner and outer parts. The stroke is centered on the boundaries of the object, but when the stroke width is odd, Corona does an integer divide by 2 then a math.floor() on the value. For example, `3 / 2 = 1` or `1 / 2 = 0`. The inner width is set to that value, and the remainder is the outer width. This avoids blurring around the edges of the stroke.",
    type = "value"
   }
  },
  description = "Vector display objects are objects created without the use of images, such as rectangles, circles, rounded rectangles, and lines.",
  type = "class"
 },
 _SliderWidget = {
  childs = {
   setValue = {
    args = "( percent )",
    description = "Changes the Slider property.",
    returns = "()",
    type = "method"
   },
   value = {
    description = "A read-only property that represents the current value of the Slider. This is a number between `0` and `100`, representing the percentage at which the slider handle is located.",
    type = "value"
   }
  },
  description = "Slider objects are created using widget.newSlider().",
  type = "class"
 },
 _SnapshotObject = {
  childs = {
   canvas = {
    description = "This group is a special offscreen group that enables you to draw on the snapshot's texture without redrawing the objects in snapshot.group.",
    type = "value"
   },
   canvasMode = {
    description = "The canvas mode controls what happens to the children of the snapshot's canvas. ",
    type = "value"
   },
   clearColor = {
    description = "The clear color controls how the snapshot's texture is cleared when the snapshot is invalidated.",
    type = "value"
   },
   group = {
    description = "This group is a special offscreen group that determines what is rendered in the snapshot.",
    type = "value"
   },
   invalidate = {
    args = "()",
    description = "Invalidating snapshots tells Corona to invalidate its texture cache and re-render the children to a texture on the next render pass.",
    returns = "()",
    type = "method"
   },
   textureFilter = {
    description = "This controls the texture filter mode for the snapshot. See the 'Texture Keys' section of display.setDefault() for valid filter values.",
    type = "value"
   },
   textureWrapX = {
    description = "This controls the texture wrap in the x-direction for the snapshot. See the 'Texture Keys' section of display.setDefault() for valid wrap values.",
    type = "value"
   },
   textureWrapY = {
    description = "This controls the texture wrap in the y-direction for the snapshot. See the 'Texture Keys' section of display.setDefault() for valid wrap values.",
    type = "value"
   }
  },
  description = "Snapshot objects let you capture a group of display objects and render them into a flattened image. ",
  type = "class"
 },
 _SpinnerWidget = {
  childs = {
   start = {
    args = "()",
    description = "This method is used to start the animation or rotation of a Spinner object.",
    returns = "()",
    type = "method"
   },
   stop = {
    args = "()",
    description = "This method is used to stop the animation or rotation of a Spinner object.",
    returns = "()",
    type = "method"
   }
  },
  description = "Spinner objects are created using widget.newSpinner().",
  type = "class"
 },
 _SpriteObject = {
  childs = {
   frame = {
    description = "A read-only property that represents the currently shown frame index of loaded sequence.",
    type = "value"
   },
   isPlaying = {
    description = "Returns `true` if the currently loaded sequence is currently playing; `false` if it is not.",
    type = "value"
   },
   numFrames = {
    description = "A read-only property that represents the number of frames in currently loaded sequence.",
    type = "value"
   },
   pause = {
    args = "()",
    description = "Pauses the current animation, and frame remains on the currently shown frame. Playback can resume later with object:play().",
    returns = "()",
    type = "method"
   },
   play = {
    args = "()",
    description = "Play an animation sequence, starting at the current frame. This does not reset looping.",
    returns = "()",
    type = "method"
   },
   sequence = {
    description = "A read-only property that returns the name of the currently playing sequence.",
    type = "value"
   },
   setFrame = {
    args = "( frameIndex )",
    description = "Sets the frame in the currently loaded sequence.",
    returns = "()",
    type = "method"
   },
   setSequence = {
    args = "( [ sequenceName ] )",
    description = "Loads an animation sequence by name. If you do not specify a sequence, the first frame in the currently loaded sequence will be shown instead.",
    returns = "()",
    type = "method"
   },
   timeScale = {
    description = "Gets or sets the scale to be applied to the animation time. This is used to control a sprite's animation speed dynamically.",
    type = "value"
   }
  },
  description = "Sprite objects are created using display.newSprite().",
  type = "class"
 },
 _StageObject = {
  childs = {
   setFocus = {
    args = "( object [, touchID ] )",
    description = "Sets a specific display object as the target for all future hit events (`\"touch\"` and `\"tap\"`). Pass `nil` to restore default behavior for hit event dispatches. This is typically used to implement buttons that change appearance when a user initially presses them.",
    returns = "()",
    type = "method"
   }
  },
  description = "This object is the current \"stage\" object, which is the parent group for all display objects and child groups. This global stage object can be retrieved at any time via display.getCurrentStage().",
  type = "class"
 },
 _StepperWidget = {
  childs = {
   maximumValue = {
    description = "A read-only property that represents the maximum value of the stepper.",
    type = "value"
   },
   minimumValue = {
    description = "A read-only property that represents the minimum value of the stepper.",
    type = "value"
   },
   value = {
    description = "A read-only property that represents the current value of the stepper.",
    type = "value"
   }
  },
  description = "Stepper.",
  type = "class"
 },
 _SwitchWidget = {
  childs = {
   isOn = {
    description = "The read-only state of a switch, either `true` for on/selected or `false` for off/deselected.",
    type = "value"
   },
   setState = {
    args = "( options )",
    description = "Used to programatically set the state of a Switch. This also changes the state of the switch visually.",
    returns = "()",
    type = "method"
   }
  },
  description = "Switch objects are created using widget.newSwitch().",
  type = "class"
 },
 _TabBarWidget = {
  childs = {
   setSelected = {
    args = "( buttonIndex )",
    description = "Use this method to set a specific TabBar button to its \"selected\" state. Optionally, you can invoke the `onPress` listener for the button at the same time.",
    returns = "()",
    type = "method"
   }
  },
  description = "TabBar objects are created using widget.newTabBar().",
  type = "class"
 },
 _TableViewWidget = {
  childs = {
   deleteAllRows = {
    args = "()",
    description = "This method is used to delete __all__ rows contained inside a TableView.",
    returns = "()",
    type = "method"
   },
   deleteRow = {
    args = "( rowIndex )",
    description = "This method is used to delete a row contained inside a TableView.",
    returns = "()",
    type = "method"
   },
   getContentPosition = {
    args = "()",
    description = "Returns the __y__ coordinate of the TableView content.",
    returns = "(Number)",
    type = "method"
   },
   getNumRows = {
    args = "()",
    description = "Returns the number of rows contained in a TableView.",
    returns = "(Number)",
    type = "method"
   },
   getRowAtIndex = {
    args = "( rowIndex )",
    description = "Returns the row group reference to a specific visible row in a TableView.",
    returns = "(GroupObject)",
    type = "method",
    valuetype = "_GroupObject"
   },
   insertRow = {
    args = "( options )",
    description = "This method is used for inserting rows into a TableView.",
    returns = "()",
    type = "method"
   },
   reloadData = {
    args = "()",
    description = "This method is used to reload (re-render) the visible rows of a TableView.",
    returns = "()",
    type = "method"
   },
   scrollToIndex = {
    args = "( rowIndex, time, onComplete )",
    description = "Makes a TableView scroll to a specific row.",
    returns = "()",
    type = "method"
   },
   scrollToY = {
    args = "( options )",
    description = "Makes a TableView.",
    returns = "()",
    type = "method"
   }
  },
  description = "Table views - sometimes referred to as list views - are created using widget.newTableView().",
  type = "class"
 },
 _TextBox = {
  childs = {
   align = {
    description = "Alignment of text displayed in the text box. May be set to one of the following strings: ",
    type = "value"
   },
   font = {
    description = "Font of the text box. May be set to a font object as returned by native.newFont().",
    type = "value"
   },
   hasBackground = {
    description = "Controls whether the text box has an opaque background or not. Default is `true` (opaque).",
    type = "value"
   },
   isEditable = {
    description = "Controls whether the text box is editable. Default is `false`.",
    type = "value"
   },
   setReturnKey = {
    args = "(key)",
    description = "Sets the return key of the keyboard.",
    returns = "()",
    type = "method"
   },
   setTextColor = {
    args = "( r, g, b )",
    description = "Sets the color of the text in a native text input box.",
    returns = "()",
    type = "method"
   },
   size = {
    description = "The font size of the text in a native text input box.",
    type = "value"
   },
   text = {
    description = "The contents of the native text input box.",
    type = "value"
   }
  },
  description = "A native text box is a scrollable, multi-line input field for displaying text-based content. See native.newTextBox() for more details.",
  type = "class"
 },
 _TextField = {
  childs = {
   align = {
    description = "Alignment of text displayed in the input text field. May be set to one of the following strings: ",
    type = "value"
   },
   font = {
    description = "Font of the text input field. May be set to a font object as returned by native.newFont().",
    type = "value"
   },
   hasBackground = {
    description = "Controls whether the text box has an opaque background or not. Default is `true` (opaque).",
    type = "value"
   },
   inputType = {
    description = "The keyboard type of the native text input field.",
    type = "value"
   },
   isSecure = {
    description = "Controls whether text in the field is obscured, e.g. passwords. Default is `false`.",
    type = "value"
   },
   placeholder = {
    description = "Native text fields can display optional placeholder text (`nil` by default). This can provide a \"hint\" as to what the user should enter in the field. If set, the placeholder string is displayed using the same style information (except the text color). The placeholder does not appear once actual text has been input into the field and it does not currently participate in determining the size of the text field.",
    type = "value"
   },
   setReturnKey = {
    args = "(key)",
    description = "Sets the return key of the keyboard.",
    returns = "()",
    type = "method"
   },
   setTextColor = {
    args = "( r, g, b )",
    description = "Sets the color of the text in a native text input field.",
    returns = "()",
    type = "method"
   },
   size = {
    description = "The font size of the text in a native text input field.",
    type = "value"
   },
   text = {
    description = "The contents of the native text input field.",
    type = "value"
   }
  },
  description = "A native text field is a single-line input field for displaying text-based content. See native.newTextField() for more details.",
  type = "class"
 },
 _TextObject = {
  childs = {
   setTextColor = {
    args = "()",
    description = "Sets the color or gradient fill of a text object.",
    returns = "()",
    type = "method"
   },
   size = {
    description = "Retrieves or sets the size of the text in a text object.",
    type = "value"
   },
   text = {
    description = "Retrieves or sets the text string of a text object.",
    type = "value"
   }
  },
  description = "Text objects are created using the display.newText() function.",
  type = "class"
 },
 _Video = {
  childs = {
   currentTime = {
    description = "The current time position of the video in seconds (read-only). Use this in conjunction with native.newVideo().",
    type = "value"
   },
   isMuted = {
    description = "Controls whether the video's audio channel is muted. Use this in conjunction with native.newVideo().",
    type = "value"
   },
   load = {
    args = "( path )",
    description = "Loads a specified video. Use this in conjunction with native.newVideo().",
    returns = "()",
    type = "method"
   },
   pause = {
    args = "()",
    description = "Pauses the currently loaded video. Use this in conjunction with native.newVideo().",
    returns = "()",
    type = "method"
   },
   play = {
    args = "()",
    description = "Plays the currently loaded video. Use this in conjunction with native.newVideo().",
    returns = "()",
    type = "method"
   },
   seek = {
    args = "( timeInSeconds )",
    description = "Seeks and jumps to the specified time in the currently loaded video. Use this in conjunction with native.newVideo().",
    returns = "()",
    type = "method"
   },
   totalTime = {
    description = "The length of currently loaded video in seconds. (read-only)  This should be called after the video is ready to play or an inaccurate value might be returned.",
    type = "value"
   }
  },
  description = "A video object is a native display object. You can move it, rotate it, or even add a physics body.",
  type = "class"
 },
 _WebView = {
  childs = {
   back = {
    args = "()",
    description = "Takes the webView back one step in the webView's history.",
    returns = "()",
    type = "method"
   },
   canGoBack = {
    description = "Read-only property that will be true if webView can go back a webpage, and false if not.",
    type = "value"
   },
   canGoForward = {
    description = "Read-only property that will be true if webView can go forward a webpage, and false if not.",
    type = "value"
   },
   forward = {
    args = "()",
    description = "Takes the webView forward one step in the webView's history.",
    returns = "()",
    type = "method"
   },
   hasBackground = {
    description = "This is a get/set property that corresponds to whether or not the background of the webView is visible or transparent. Default is `true`.",
    type = "value"
   },
   reload = {
    args = "()",
    description = "Reloads currently loaded page in webView.",
    returns = "()",
    type = "method"
   },
   request = {
    args = "( url )",
    description = "Loads specified url (string) into webView. For local content, you **must** specify a base directory as a search path (see system-defined directory constants in system.* ).",
    returns = "(TYPE)",
    type = "method"
   },
   stop = {
    args = "()",
    description = "Stops loading current page in webView (if loading).",
    returns = "()",
    type = "method"
   }
  },
  description = "A web view object is a native display object. You can move and rotate it, or even add a physics body.",
  type = "class"
 },
 ads = {
  childs = {
   hide = {
    args = "()",
    description = "Removes the currently shown ad from the screen and prevents new ads from being retrieved until ads.show() with the new provider.",
    returns = "()",
    type = "function"
   },
   init = {
    args = "( providerName, appId [, listener] )",
    description = "`ads.init()` initializes the Corona `ads` library by specifying the name of the ad network service provider and the application identifier. For more information, please refer to the Implementing Adsguide.monetization.adSupport guide.",
    returns = "()",
    type = "function"
   },
   show = {
    args = "( adUnitType [, params] )",
    description = "`ads.show()` begins showing ads at the given screen location on the provided refresh period.",
    returns = "()",
    type = "function"
   }
  },
  description = "The Corona `ads` library lets you display ads within your application. The following ad networks are currently supported:",
  type = "lib"
 },
 analytics = {
  childs = {
   init = {
    args = "( apiKey )",
    description = "Initializes the analytics library.",
    returns = "()",
    type = "function"
   },
   logEvent = {
    args = "( eventID )",
    description = "Reports a custom event defined by a string value.",
    returns = "()",
    type = "function"
   }
  },
  description = "The Corona analytics library lets you easily log interesting events in your application.",
  type = "lib"
 },
 audio = {
  childs = {
   dispose = {
    args = "( audioHandle )",
    description = "Releases audio memory associated with the handle.",
    returns = "()",
    type = "function"
   },
   fade = {
    args = "( [ { [channel=c] [, time=t] [, volume=v ] } ] )",
    description = "This fades a playing sound in a specified amount to a specified volume. The audio will continue playing after the fade completes.",
    returns = "(Number)",
    type = "function"
   },
   fadeOut = {
    args = "( [ { [channel=c] [, time=t] } ] )",
    description = "This stops a playing sound in a specified amount of time and fades to min volume while doing it. The audio will stop at the end of the time and the channel will be freed.",
    returns = "(Number)",
    type = "function"
   },
   findFreeChannel = {
    args = "( [ startChannel ] )",
    description = "Will look for an available channel for playback. You can provide a start channel number as parameter which will start searching from that channel and increase upward to the highest channel. The search does not include reserved channels.",
    returns = "(Number)",
    type = "function"
   },
   freeChannels = {
    description = "This property is always equal to the number of channels that are currently available for playback (not playing, and not paused).",
    type = "value"
   },
   getDuration = {
    args = "( audioHandle )",
    description = "This function returns the total time in milliseconds of the audio resource. If the total length cannot be determined, `-1` will be returned.",
    returns = "(Number)",
    type = "function"
   },
   getMaxVolume = {
    args = "( { channel=c } )",
    description = "Gets the max volume for a specific channel. **NOTE:** There is no max volume for the master volume.",
    returns = "(Number)",
    type = "function"
   },
   getMinVolume = {
    args = "( { channel=c } )",
    description = "Gets the min volume for a specific channel. **NOTE:** There is no min volume for the master volume.",
    returns = "(Number)",
    type = "function"
   },
   getVolume = {
    args = "( [ { [channel=c]  } ] )",
    description = "Gets the volume either for a specific channel or gets the master volume.",
    returns = "(Number)",
    type = "function"
   },
   isChannelActive = {
    args = "( channel )",
    description = "Returns `true` if the specified channel is currently playing or paused; `false` if otherwise.",
    returns = "(Boolean)",
    type = "function"
   },
   isChannelPaused = {
    args = "( channel )",
    description = "Returns `true` if the specified channel is currently paused; `false` if not.",
    returns = "(Boolean)",
    type = "function"
   },
   isChannelPlaying = {
    args = "( channel )",
    description = "Returns `true` if the specified channel is currently playing; `false` if otherwise.",
    returns = "(Boolean)",
    type = "function"
   },
   loadSound = {
    args = "( audiofileName [, baseDir ]  )",
    description = "Loads an entire file completely into memory and returns a reference to the audio data. Files that are loaded completely into memory may be reused/played/shared simultaneously on multiple channels so you only need to load one instance of the file. You should use this to load all your short sounds, especially ones you may play frequently. For best results, load all the sounds at the launch of your app or the start of a new level/scene.",
    returns = "(Object)",
    type = "function"
   },
   loadStream = {
    args = "( audioFileName [, baseDir ]  )",
    description = "Loads (opens) a file to be read as a stream. Streamed files are read in little chunks at a time to minimize memory use. These are intended for large/long files like background music and speech. Unlike files loaded with audio.loadSound(), these cannot be shared simultaneously across multiple channels. If you need to play multiple simulataneous instances of the same file, you must load multiple instances of the file.",
    returns = "(Object)",
    type = "function"
   },
   pause = {
    args = "( [channel] )",
    description = "Pauses playback on a channel (or all channels if no channels are specified). Has no effect on channels that aren't playing.",
    returns = "(Number)",
    type = "function"
   },
   play = {
    args = "( audioHandle [, options ] )",
    description = "Plays the audio specified by the audio handle on a channel. If a channel is not explicitly specified, an available channel will be automatically chosen for you.",
    returns = "(Number)",
    type = "function"
   },
   reserveChannels = {
    args = "( channels )",
    description = "Allows you to reserve a certain number of channels so they won't be automatically assigned to play on. This function blocks off the lower number channels up to the number you specify so they won't be automatically assigned to be played on when you call various play functions.",
    returns = "(Number)",
    type = "function"
   },
   reservedChannels = {
    description = "Returns the number of reserved channels set by audio.reserveChannels().",
    type = "value"
   },
   resume = {
    args = "( [channel] )",
    description = "Resumes playback on a channel that is paused (or all channels if no channel is specified). Should have no effect on channels that aren't paused.",
    returns = "(Number)",
    type = "function"
   },
   rewind = {
    args = "( [audioHandle | options] )",
    description = "Rewinds audio to the beginning position on either an active channel or directly on the audio handle (rewinds all channels if no arguments are specified).",
    returns = "(Boolean)",
    type = "function"
   },
   seek = {
    args = "( time [, audioHandle ] [, options ] )",
    description = "Seeks to a time position on either an active channel or directly on the audio handle.",
    returns = "(Boolean)",
    type = "function"
   },
   setMaxVolume = {
    args = "( volume, options )",
    description = "Clamps the maximum volume to the set value. Any volumes that exceed the value will be played at the maximum volume level.",
    returns = "(Boolean)",
    type = "function"
   },
   setMinVolume = {
    args = "( volume, options )",
    description = "Clamps the minimum volume to the set value. Any volumes that fall below the value will be played at the minimum volume level.",
    returns = "(Boolean)",
    type = "function"
   },
   setVolume = {
    args = "( volume [, options ] )",
    description = "Sets the volume either for a specific channel or sets the master volume.",
    returns = "(Boolean)",
    type = "function"
   },
   stop = {
    args = "( [channel] )",
    description = "Stops playback on a channel and clears the channel so it can be played on again (or all channels if no channel is specified). Callbacks will still be invoked, but the completed flag will be set to `false`.",
    returns = "(Number)",
    type = "function"
   },
   stopWithDelay = {
    args = "( duration [, options ] )",
    description = "Stops the playing a currently playing sound at the specified amount of time.",
    returns = "(Number)",
    type = "function"
   },
   totalChannels = {
    description = "Returns the total number of channels. (Should be 32 for now, but this limit may be subject to change in the future.)",
    type = "value"
   },
   unreservedFreeChannels = {
    description = "Returns the number of channels that are currently available for playback (not playing, not paused), excluding the channels that have been reserved.",
    type = "value"
   },
   unreservedUsedChannels = {
    description = "Returns the number of channels that are currently in use (playing/paused), excluding the channels that have been reserved.",
    type = "value"
   },
   usedChannels = {
    description = "Returns the number of channels that are currently in use (playing/paused).",
    type = "value"
   }
  },
  description = "The Corona Audio system gives you access to advanced OpenAL features. See the Audio Usage/Functionsguide.media.audioSystem guide for more information.",
  type = "lib"
 },
 composer = {
  childs = {
   getScene = {
    args = "( sceneName )",
    description = "Returns the specified scene object, as returned from composer.newScene(). Returns `nil` if the scene object does not exist. This function is useful for getting a reference to a specific scene object - for instance, if the current scene needs access to a specific function that's attached to another scene.",
    returns = "(Table)",
    type = "function"
   },
   getSceneName = {
    args = "( sceneType )",
    description = "Returns the current, previous, or overlay scene name as a string. This can then be used with composer.gotoScene().",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   getVariable = {
    args = "( variableName )",
    description = "This function retrieves the value of a variable (from within the Composer object) that you previously set via composer.setVariable().",
    returns = "()",
    type = "function"
   },
   gotoScene = {
    args = "( sceneName [, options] )",
    description = "This function is used to transition to a specific scene. When called, the hide event will be dispatched.",
    returns = "()",
    type = "function"
   },
   hideOverlay = {
    args = "( [recycleOnly] [, effect] [, time] )",
    description = "This function hides/removes the current overlay scene (see composer.showOverlay()), if one is being displayed.",
    returns = "()",
    type = "function"
   },
   isDebug = {
    description = "Toggles \"Composer Debug Mode\" which, if set to `true`, prints useful debugging information to the Corona Terminal in certain situations. This should be set to `false` (default) before building the project for deployment.",
    type = "value"
   },
   loadScene = {
    args = "( sceneName [, doNotLoadView] [, params] )",
    description = "Loads the specified scene, hidden behind the current scene, without initiating a scene transition. This function is similar to composer.gotoScene() event will be dispatched on the scene, assuming its `self.view` does not already exist.",
    returns = "()",
    type = "function"
   },
   migration = {
    description = "This guide aims to ease your migration from Storyboard to Composer.",
    type = "value"
   },
   newScene = {
    args = "()",
    description = "This function creates new scene objects which can be used with the Composer library.",
    returns = "(Table)",
    type = "function"
   },
   purgeOnSceneChange = {
    description = "If set to `true`, whenever a scene change is completed, all scenes (except for the newly active scene) will be automatically purged. The default value for this property is `false`.",
    type = "value"
   },
   purgeScene = {
    args = "( sceneName )",
    description = "Unloads the specified scene's `scene.view` property, which is a group that contains all of the scene's display objects. It takes one (required) argument, `sceneName`, which is the name of the scene (usually the name of the module, without the \".lua\" extension) you are attempting to unload. If the scene has its own module, this function preserves the scene's module in the global `package.loaded` table.",
    returns = "()",
    type = "function"
   },
   recycleAutomatically = {
    description = "If this property is set to `true` (default), the scene used least recently will be automatically recycled if the OS issues a low memory warning. If you prefer to manage the recycling of scenes manually and disable the <nobr>auto-recycling</nobr> functionality, set this property to `false`.",
    type = "value"
   },
   recycleOnLowMemory = {
    description = "If the OS issues a low memory warning, Composer will automatically recycle the scene used least recently (that scene's `self.view` will be removed). If you prefer to disable <nobr>auto-recycling</nobr> functionality, set this property to `false`. Default is `true`.",
    type = "value"
   },
   recycleOnSceneChange = {
    description = "By default, when changing scenes, Composer keeps the current scene's view in memory, which can improve performance if you access the same scenes frequently. If you wish to recycle the current scene when changing to a new scene (remove the current scene's `self.view`) set `composer.recycleOnSceneChange` to `true`. The default value is `false`.",
    type = "value"
   },
   reloadScene = {
    args = "()",
    description = "Reloads the currently loaded scene. This does not support scene transition effects. When a scene is reloaded, it behaves as if composer.gotoScene() was called on the currently active scene.",
    returns = "()",
    type = "function"
   },
   removeHidden = {
    args = "( shouldRecycle )",
    description = "This function removes (or optionally recycles) all scenes __except__ for the currently active scene. A destroy event is first dispatched to all of these scenes.",
    returns = "()",
    type = "function"
   },
   removeScene = {
    args = "( sceneName, shouldRecycle )",
    description = "This function removes the specified scene (or optionally recycles it). A destroy event is first dispatched to the scene.",
    returns = "()",
    type = "function"
   },
   setVariable = {
    args = "( variableName, value )",
    description = "This function sets a variable (within the Composer module) which you can access from any other scene via  composer.getVariable().",
    returns = "()",
    type = "function"
   },
   showOverlay = {
    args = "( sceneName [, options] )",
    description = "This function loads an overlay scene above the currently active scene (the __parent__ scene), leaving the parent scene intact.",
    returns = "()",
    type = "function"
   },
   stage = {
    description = "Returns a reference to the top-level Composer display group which all scene views are inserted into. This can be considered as the \"Composer scene layer\" and it's useful if you need to place display objects above or below all Composer scenes, even during transition effects. For example:",
    type = "value"
   }
  },
  description = "Composer is the official scene (screen) creation and management library in Corona SDK. This library provides developers with an easy way to create and transition between individual scenes.",
  type = "lib"
 },
 credits = {
  childs = {
   init = {
    args = "( ARG1 [, ARG2] )",
    description = "This API is currently deprecated and should not be used.",
    returns = "(TYPE)",
    type = "function"
   },
   requestUpdate = {
    args = "( ARG1 [, ARG2] )",
    description = "This API is currently deprecated and should not be used.",
    returns = "(TYPE)",
    type = "function"
   },
   showOffers = {
    args = "( ARG1 [, ARG2] )",
    description = "This API is currently deprecated and should not be used.",
    returns = "(TYPE)",
    type = "function"
   }
  },
  description = "This API is currently deprecated and should not be used.",
  type = "lib"
 },
 crypto = {
  childs = {
   digest = {
    args = "( algorithm, data [, raw] )",
    description = "Generates the message digest (the hash) of the input string.",
    returns = "(TYPE)",
    type = "function"
   },
   hmac = {
    args = "( algorithm, data, key [, raw] )",
    description = "Computes HMAC (Key-Hashing for Message Authentication Code) of the string and returns it.",
    returns = "(TYPE)",
    type = "function"
   },
   md4 = {
    description = "Constant used to specify the MD4 algorithm (Message-Digest algorithm 4)",
    type = "value"
   },
   md5 = {
    description = "Constant used to specify the MD5 algorithm (Message-Digest algorithm 5)",
    type = "value"
   },
   sha1 = {
    description = "Constant used to specify the SHA-1 algorithm.",
    type = "value"
   },
   sha224 = {
    description = "Constant used to specify the SHA-224 algorithm.",
    type = "value"
   },
   sha256 = {
    description = "Constant used to specify the SHA-256 algorithm.",
    type = "value"
   },
   sha384 = {
    description = "Constant used to specify the SHA-384 algorithm.",
    type = "value"
   },
   sha512 = {
    description = "Constant used to specify the SHA-512 algorithm.",
    type = "value"
   }
  },
  description = "Corona provides routines for calculating common message digests (hashes) and hash-based message authentication codes (HMAC).",
  type = "lib"
 },
 display = {
  childs = {
   actualContentHeight = {
    description = "The height in Corona content units of the screen. The result depends on the `scale` setting in `config.lua`. See the Project Configurationguide.basics.configSettings guide for more information.",
    type = "value"
   },
   actualContentWidth = {
    description = "The width in Corona content units of the screen. The result depends on the `scale` setting in `config.lua`. See the Project Configurationguide.basics.configSettings guide for more information.",
    type = "value"
   },
   capture = {
    args = "( displayObject, options )",
    description = "This function is the same as display.save(), but it returns a display object instead of saving to a file by default. You can optionally save the capture to the device's photo library, but this is not the default action - you must explicitly tell it to do so when calling the function.",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   captureBounds = {
    args = "( screenBounds [, saveToAlbum ] )",
    description = "Captures a portion of the screen and returns it as a new DisplayObject positioned at the top-left corner of the screen. You can specify what portion of the screen to capture by passing in a rectangle to this function. You can optionally save the capture image as a file to the device's photo library.",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   captureScreen = {
    args = "( [saveToAlbum] )",
    description = "Captures the contents of the screen and returns it as a new display object. You can optionally save the capture as a file to the device's photo library.",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   colorSample = {
    args = "( x, y, listener )",
    description = "Get the color of a pixel on screen.",
    returns = "(LineObject)",
    type = "function",
    valuetype = "_LineObject"
   },
   contentCenterX = {
    description = "Equivalent to `display.contentWidth * 0.5`.",
    type = "value"
   },
   contentCenterY = {
    description = "Equivalent to `display.contentHeight * 0.5`.",
    type = "value"
   },
   contentHeight = {
    description = "A read-only property representing the original height of the content in pixels. This will default to the screen height, but may be another value if you are using content scaling in `config.lua`.",
    type = "value"
   },
   contentScaleX = {
    description = "The ratio between content pixel and screen pixel width.",
    type = "value"
   },
   contentScaleY = {
    description = "The ratio between content pixel and screen pixel height.",
    type = "value"
   },
   contentWidth = {
    description = "A read-only property representing the original width of the content in pixels. This will default to the screen width, but may be another value if you are using content scaling in `config.lua`.",
    type = "value"
   },
   currentStage = {
    description = "A reference to the current stage object, which is the root group for all display objects and groups. Currently, Corona has a single stage instance, so this function always returns a reference to the same object.",
    type = "value"
   },
   fps = {
    description = "Current framerate of the running application.",
    type = "value"
   },
   getCurrentStage = {
    args = "()",
    description = "Returns a reference to the current stage object, which is the parent group for all display objects and groups. Currently, Corona has a single stage instance, so this function always returns a reference to the same object.",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   getDefault = {
    args = "( key )",
    description = "Get default display values including default colors for display objects, anchor point defaults, texture wrapping settings, etc.",
    returns = "()",
    type = "function"
   },
   imageSuffix = {
    description = "Returns the suffix used by the dynamic image selectionguide.basics.configSettings feature of Corona. If the content scaling is 1, then returns `nil`.",
    type = "value"
   },
   loadRemoteImage = {
    args = "( url, method, listener [, params], destFilename [, baseDir] [, x, y] )",
    description = "This a convenience method, similar to network.download() containing the image, as well as saving the image to a file.",
    returns = "()",
    type = "function"
   },
   newCircle = {
    args = "( xCenter, yCenter, radius )",
    description = "Creates a circle with radius radius centered at specified coordinates (`xCenter`, `yCenter`). The local origin is at the center of the circle. The anchor point is initialized to this local origin.",
    returns = "(ShapeObject)",
    type = "function",
    valuetype = "_ShapeObject"
   },
   newContainer = {
    args = "( [parent, ] width, height )",
    description = "Containers are a special kind of group in which the children are clipped (masked) by the bounds of the container.",
    returns = "()",
    type = "function"
   },
   newEmbossedText = {
    args = "( [parentGroup,] string, left, top, [width, height,] font, size )",
    description = "Creates text with an embossed (inset) effect.",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   newGroup = {
    args = "()",
    description = "Creates a group in which you can add and remove child display objects. Initially, there are no children in a group. The local origin is at the parent's origin; the anchor point is initialized to this local origin.",
    returns = "(GroupObject)",
    type = "function",
    valuetype = "_GroupObject"
   },
   newImage = {
    args = "( [parent,] filename [,baseDir] [,x,y] [,isFullResolution])",
    description = "Displays an image on the screen from a file (supports tinting via object:setFillColor)",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   newImageGroup = {
    args = "()",
    returns = "(GroupObject)",
    type = "function",
    valuetype = "_GroupObject"
   },
   newImageRect = {
    args = "( [parentGroup,] filename, [baseDir,] width, height )",
    description = "Displays an image on the screen from a file (supports tinting via object:setFillColor).",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   newLine = {
    args = "( [parentGroup,] x1, y1, x2, y2 [, x3, y3, ... ] )",
    description = "Draw a line from one point to another. Optionally, you may append points to the end of the line to create outline shapes or paths.",
    returns = "(LineObject)",
    type = "function",
    valuetype = "_LineObject"
   },
   newPolygon = {
    args = "( x, y, vertices )",
    description = "Draw a polygon shape by providing the outline (contour) of the shape. This includes convex or concave shapes. Self-intersecting shapes, however, are not supported and will result in undefined behavior.",
    returns = "(ShapeObject)",
    type = "function",
    valuetype = "_ShapeObject"
   },
   newRect = {
    args = "( x, y, width, height )",
    description = "Creates a rectangle object. The local origin is at the center of the rectangle; the anchor point is initialized to this local origin.",
    returns = "(ShapeObject)",
    type = "function",
    valuetype = "_ShapeObject"
   },
   newRoundedRect = {
    args = "( x, y, width, height, cornerRadius )",
    description = "Creates a rounded rectangle object. The corners are rounded by quarter circles of a specified radius value. The local origin is at the center of the rectangle; the anchor point is initialized to this local origin.",
    returns = "(ShapeObject)",
    type = "function",
    valuetype = "_ShapeObject"
   },
   newSnapshot = {
    args = "( w, h )",
    description = "Snapshot objects let you capture a group of display objects and render them into a flattened image. The image is defined by objects that are added to the snapshot's group property.",
    returns = "(SnapshotObject)",
    type = "function",
    valuetype = "_SnapshotObject"
   },
   newSprite = {
    args = "( imageSheet, sequenceData )",
    description = "Sprites allow for animated sequences of frames that reside on __image sheets__. Please see the Image Sheets (Sprite Sheets)guide.media.imageSheets guide for more information.",
    returns = "(SpriteObject)",
    type = "function",
    valuetype = "_SpriteObject"
   },
   newText = {
    args = "( { options } )",
    description = "Creates a text object with its top-left corner at (left, top). The local origin is at the center of the text; the reference point is initialized to this local origin. By default, the text color is white `(255, 255, 255)`. See object:setTextColor() for more information.",
    returns = "(DisplayObject)",
    type = "function",
    valuetype = "_DisplayObject"
   },
   pixelHeight = {
    description = "The height of the screen, in pixels.",
    type = "value"
   },
   pixelWidth = {
    description = "The width of the screen, in pixels.",
    type = "value"
   },
   remove = {
    args = "( object )",
    description = "Removes a group or object if not `nil`.",
    returns = "()",
    type = "function"
   },
   save = {
    args = "( displayObject, options )",
    description = "Renders the display object referenced by first argument into a JPEG or PNG image and saves it as a new file. The display object must currently be in the display hierarchy or no image will be saved. If the object is a display group, all children are also rendered. The object to be saved must be on the screen and fully within the screen boundaries.",
    returns = "()",
    type = "function"
   },
   screenOriginX = {
    description = "Returns the __x__ distance from the left side of the actual screen to the left side of the content area, in reference screen units. For example, in `\"letterbox\"` or `\"zoomEven\"` scaling modes, there may be added area or cropped area on the current device screen. These methods let you find out how much visible area has been added or removed on the current device.",
    type = "value"
   },
   screenOriginY = {
    description = "Returns the __y__ distance from the top of the actual screen to the top of the content area, in reference screen units. For example, in `\"letterbox\"` or `\"zoomEven\"` scaling modes, there may be added area or cropped area on the current device screen. These methods let you find out how much visible area has been added or removed on the current device.",
    type = "value"
   },
   setDefault = {
    args = "()",
    description = "Set default display values including default colors for display objects, anchor point defaults, texture wrapping settings, etc.",
    returns = "()",
    type = "function"
   },
   setDrawMode = {
    args = "( key, value )",
    description = "Sets the draw mode.",
    returns = "()",
    type = "function"
   },
   setStatusBar = {
    args = "( mode )",
    description = "Hides or changes the appearance of the status bar on certain devices (iPad, iPhone, iPod Touch, and Android 2.x devices).",
    returns = "()",
    type = "function"
   },
   statusBarHeight = {
    description = "A read-only property representing the height of the status bar in content pixels on iOS devices.",
    type = "value"
   },
   topStatusBarContentHeight = {
    description = "A read-only property representing the height of the status bar in content coordinates.",
    type = "value"
   },
   viewableContentHeight = {
    description = "A read-only property that contains the height of the viewable screen area in content coordinates. ",
    type = "value"
   },
   viewableContentWidth = {
    description = "A read-only property that contains the width of the viewable screen area in content coordinates. ",
    type = "value"
   }
  },
  type = "lib"
 },
 easing = {
  childs = {
   inExpo = {
    args = "( displayObject, { transition=easing.inExpo } )",
    description = "Starts animation from zero velocity and then accelerates motion as it executes. The interpolation is done using an exponential equation.",
    returns = "(Number)",
    type = "function"
   },
   inOutExpo = {
    args = "( displayObject, { transition=easing.inOutExpo } )",
    description = "Starts animation from zero velocity, accelerates, then decelerates to a zero velocity. The interpolation is done using an exponential equation.",
    returns = "(Number)",
    type = "function"
   },
   inOutQuad = {
    args = "( displayObject, { transition=easing.inOutQuad } )",
    description = "Starts animation from zero velocity, accelerates, then decelerates to a zero velocity. The interpolation is done using a quadratic function.",
    returns = "(Number)",
    type = "function"
   },
   inQuad = {
    args = "( displayObject, { transition=easing.inQuad } )",
    description = "Starts animation from zero velocity and then accelerates motion as it executes. The interpolation is done using a quadratic function.",
    returns = "(Number)",
    type = "function"
   },
   linear = {
    args = "( displayObject, { transition=easing.linear } )",
    description = "Performs linear interpolation. This is the default easing mode.",
    returns = "(Number)",
    type = "function"
   },
   outExpo = {
    args = "( displayObject, { transition=easing.outExpo } )",
    description = "Starts animation at a high velocity and decelerates towards zero. The interpolation is done using an exponential function.",
    returns = "(Number)",
    type = "function"
   },
   outQuad = {
    args = "( displayObject, { transition=easing.outQuad } )",
    description = "Starts animation at a high velocity and decelerates towards zero. The interpolation is done using a quadratic function.",
    returns = "(Number)",
    type = "function"
   }
  },
  description = "Easing functions provide a simple way of interpolating between two values to achieve varied animations. They are used in conjunction with the transition library.",
  type = "lib"
 },
 facebook = {
  childs = {
   accessDenied = {
    args = "()",
    description = "Queries iOS to see if access to the Facebook integrated account has been denied.",
    returns = "()",
    type = "function"
   },
   login = {
    args = "( appId, listener [, permissions ] )",
    description = "Prompts the user to login to Facebook. This API can be called even if the user is already logged in. If the user is already logged in but new permissions are requested, the user will be prompted to grant the new permissions.  The recommended flow is to request no permissions the first time and only ask for additional permissions as they are needed.",
    returns = "()",
    type = "function"
   },
   logout = {
    args = "()",
    description = "Logs the application out of the user's Facebook session.",
    returns = "(event)",
    type = "function",
    valuetype = "_event"
   },
   publishInstall = {
    args = "( appId )",
    description = "Sends a message to the Facebook server indicating that the app has been installed. After a single successful connection to the Facebook server, it will not try again on subsequent calls. For more information, see the Implementing Facebookguide.social.implementFacebook guide.",
    returns = "()",
    type = "function"
   },
   request = {
    args = "( path [, httpMethod, params] )",
    description = "Get or post data to the logged-in Facebook account. Can be used to post messages and photos to the account and friends' accounts. Can also be used to get user data and recent posts.",
    returns = "(event)",
    type = "function",
    valuetype = "_event"
   },
   showDialog = {
    args = "( action [, params ] )",
    description = "Displays a Facebook UI dialog for publishing posts to a user's stream using only a few lines of code.",
    returns = "(event)",
    type = "function",
    valuetype = "_event"
   }
  },
  description = "The `facebook` library provides access to Facebook Connect, a set of web API's for accessing the Facebook social network. The functions allow a user to login/logout, post messages/images, and retrieve status.",
  type = "lib"
 },
 gameNetwork = {
  childs = {
   init = {
    args = "( providerName [, ...] )",
    description = "Initializes an app for use with the game network provider.",
    returns = "()",
    type = "function"
   },
   request = {
    args = "( command [, params ...] )",
    description = "Send or request information to/from the game network provider.",
    returns = "()",
    type = "function"
   },
   show = {
    args = "( name [, data ] )",
    description = "Displays the requested game network information to the user.",
    returns = "()",
    type = "function"
   }
  },
  description = "Corona's `gameNetwork` library provides access to social gaming features such as public leaderboards and achievements.",
  type = "lib"
 },
 graphics = {
  childs = {
   newGradient = {
    args = "( color1, color2 [, direction] )",
    description = "Creates a gradient object that adds horizontal/vertical linear gradients to rectangle and text objects.",
    returns = "(Object)",
    type = "function"
   },
   newImageSheet = {
    args = "( filename, [baseDir, ] options )",
    description = "ImageSheet.",
    returns = "(ImageSheet)",
    type = "function",
    valuetype = "_ImageSheet"
   },
   newMask = {
    args = "( filename [, baseDir] )",
    description = "Creates a bit mask from an image file. The image is converted internally to grayscale. The white pixels of the bit mask allow the covered image to be visible, while black pixels hide (mask) the covered image. The area outside of the mask must be filled with black pixels which mask off the area outside the mask image.",
    returns = "(Mask)",
    type = "function",
    valuetype = "_Mask"
   },
   newOutline = {
    args = "()",
    description = "This function produces the outline of the shape obtained from an image file or a frame within an ImageSheet. A shape is recognized by the image's <nobr>non-transparent</nobr> alpha value. Generally, simpler images produce better outlines.",
    returns = "(Table)",
    type = "function"
   }
  },
  description = "",
  type = "lib"
 },
 io = {
  childs = {
   close = {
    args = "( [file] )",
    description = "Closes an open file handle. Equivalent to `file:close()`.",
    returns = "()",
    type = "function"
   },
   flush = {
    args = "()",
    description = "Flushes the default output file. Equivalent to `io.output():flush`.",
    returns = "()",
    type = "function"
   },
   input = {
    args = "( [file] )",
    description = "Sets the standard input file. When called with a file name, it opens the named file (in text mode), and sets its handle as the default input file. When called with a file handle, it simply sets this file handle as the default input file. When called without parameters, it returns the current default input file.",
    returns = "(Object)",
    type = "function"
   },
   lines = {
    args = "( filename )",
    description = "Opens the given file name in read mode and returns an iterator function that, each time it is called, returns a new line from the file.",
    returns = "(Function)",
    type = "function"
   },
   open = {
    args = "( filename_path, [, mode] )",
    description = "This function opens a file for reading or writing, in the string (default) or binary mode. It returns a new file handle or, in case of errors, `nil` plus an error message. This function can also be used to create a new file.",
    returns = "(Object)",
    type = "function"
   },
   output = {
    args = "( [file] )",
    description = "Sets the standard output file. When called with a file name, it opens the named file (in text mode), and sets its handle as the default output file. When called with a file handle, it simply sets this file handle as the default output file. When called without parameters, it returns the current default output file.",
    returns = "(Object)",
    type = "function"
   },
   read = {
    args = "( [fmt1] [, fmt2] [, ...] )",
    description = "Reads the file set by io.input() with the characters read, or `nil` if it cannot read data with the specified format. When called without formats, it uses a default format that reads the entire next line (`\"*l\"`).",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   tmpfile = {
    args = "()",
    description = "Opens a temporary file for reading and writing and returns a handle to it. When the app ends normally, this file will be deleted.",
    returns = "(Object)",
    type = "function"
   },
   type = {
    args = "( obj )",
    description = "Checks whether obj is a valid file handle. Returns the string \"file\" if obj is an open file handle, \"closed file\" if obj is a closed file handle, or `nil` if obj is not a file handle.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   write = {
    args = "( arg1 [, arg2] [, ...] )",
    description = "Writes the value of each of its arguments to the file. The arguments must be strings or numbers. To write other values, use tostring() before write.",
    returns = "()",
    type = "function"
   }
  },
  description = "Standard Lua library to create, write, and read files.",
  type = "lib"
 },
 json = {
  childs = {
   decode = {
    args = "( data )",
    description = "Decodes the JSON encoded data structure, and returns a Lua object (table) with the appropriate data.",
    returns = "(Table)",
    type = "function"
   },
   encode = {
    args = "( table )",
    description = "Returns the Lua object (table) as a JSON-encoded string.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   null = {
    args = "()",
    description = "Returns a unique value that will be encoded as null in a JSON encoding.",
    returns = "(Constant)",
    type = "function"
   }
  },
  description = "The `json` library allows you serialize and deserialize Lua tables into JSON (JavaScript Object Notation) and vice-versa. You typically use JSON-formatted data in conjunction with web APIs, e.g. requests sent in the facebook library.",
  type = "lib"
 },
 lfs = {
  childs = {},
  description = "LuaFileSystem provides functionality to perform various filystem-related tasks, to include but is not limited to:",
  type = "lib"
 },
 licensing = {
  childs = {
   init = {
    args = "( providerName )",
    description = "Initialize the `licensing` service library by specifying the name of the licensing provider.",
    returns = "(Boolean)",
    type = "function"
   },
   verify = {
    args = "( listener )",
    description = "Starts the verification process.",
    returns = "()",
    type = "function"
   }
  },
  description = "The Corona licensing library lets you check to see if the app was bought from a store. Currently, only one store is supported:",
  type = "lib"
 },
 math = {
  childs = {
   abs = {
    args = "( x )",
    description = "Returns the absolute value (magnitude) of `x`.",
    returns = "(Number)",
    type = "function"
   },
   acos = {
    args = "( x )",
    description = "Returns the arc cosine of `x` in radians. The result will be in the range `0` to `pi`. If the parameter `x` is outside the range `-1..1`, the result will be `NaN`.",
    returns = "(Number)",
    type = "function"
   },
   asin = {
    args = "( x )",
    description = "Returns the arc sine of a `x` (in radians). The result will be in the range `-pi/2` to `pi/2`. If the parameter `x` is outside the range `-1..1`, the result will be `NaN`.",
    returns = "(Number)",
    type = "function"
   },
   atan = {
    args = "( x )",
    description = "Returns the arc tangent of a `x` in radians. The result will be in the range `-pi/2` to `pi/2`.",
    returns = "(Number)",
    type = "function"
   },
   atan2 = {
    args = "( y, x )",
    description = "Returns the arc tangent of `y/x` (in radians), but uses the signs of both parameters to find the quadrant of the result. It also correctly handles the case of `x` being `0`.",
    returns = "(Number)",
    type = "function"
   },
   ceil = {
    args = "( x )",
    description = "Returns the integer larger than or equal to `x`.",
    returns = "(Number)",
    type = "function"
   },
   cos = {
    args = "( x )",
    description = "Returns the cosine of `x` (the angle in radians). The result is a number in the range `-1, 1`.",
    returns = "(Number)",
    type = "function"
   },
   cosh = {
    args = "( x )",
    description = "Returns the hyperbolic cosine of `x`.",
    returns = "(Number)",
    type = "function"
   },
   deg = {
    args = "( x )",
    description = "Convert a value from radians to degrees.",
    returns = "(Number)",
    type = "function"
   },
   exp = {
    args = "( x )",
    description = "Returns the value `e^x^`.",
    returns = "(Number)",
    type = "function"
   },
   floor = {
    args = "( x )",
    description = "Returns the integer smaller than or equal to `x`.",
    returns = "(Number)",
    type = "function"
   },
   fmod = {
    args = "( x, y )",
    description = "Returns the remainder of the division of `x` by `y` that rounds the quotient towards zero.",
    returns = "(Number)",
    type = "function"
   },
   frexp = {
    args = "( x )",
    description = "Splits `x` into a normalized fraction and an exponent.",
    returns = "(Number)",
    type = "function"
   },
   huge = {
    description = "A value larger than or equal to any other numerical value.",
    type = "value"
   },
   inf = {
    description = "Returns a value larger than or equal to any other numerical value.",
    type = "value"
   },
   ldexp = {
    args = "( m, e )",
    description = "Returns `m*2^e^` where `m` containts the significant binary digits and `e` is the exponent.",
    returns = "(Number)",
    type = "function"
   },
   log = {
    args = "( x )",
    description = "Returns the natural logarithm of `x`.",
    returns = "(Number)",
    type = "function"
   },
   log10 = {
    args = "( x )",
    description = "Returns the base-10 logarithm of `x`.",
    returns = "(Number)",
    type = "function"
   },
   max = {
    args = "( x, ... )",
    description = "Returns the maximum value among its arguments.",
    returns = "(Number)",
    type = "function"
   },
   min = {
    args = "( x, ... )",
    description = "Return the minimum value among its arguments.",
    returns = "(Number)",
    type = "function"
   },
   modf = {
    args = "( x )",
    description = "Returns two numbers, the integral part of `x` and the fractional part of `x`.",
    returns = "(Numbers)",
    type = "function"
   },
   pi = {
    description = "Returns the constant _pi_.",
    type = "value"
   },
   pow = {
    args = "( x, y )",
    description = "Returns `x^y^`. (You can also use the expression `x^y` to compute this value.)",
    returns = "(Number)",
    type = "function"
   },
   rad = {
    args = "( x )",
    description = "Converts to radians an angle given in degrees.",
    returns = "(Number)",
    type = "function"
   },
   random = {
    args = "()",
    description = "Returns a pseudo-random number from a sequence with uniform distribution.",
    returns = "(Number)",
    type = "function"
   },
   randomseed = {
    args = "( x )",
    description = "Sets `x` as the \"seed\" for the pseudo-random generator: equal seeds produce equal sequences of numbers.",
    returns = "()",
    type = "function"
   },
   round = {
    args = "( x )",
    description = "Rounds number to the nearest integer following the same rules as the JavaScript version, i.e. if the fractional portion of number is `0.5` or greater, the argument is rounded to the next higher integer. If the fractional portion of number is less than `0.5`, the argument is rounded to the next lower integer.",
    returns = "(Number)",
    type = "function"
   },
   sin = {
    args = "( x )",
    description = "Returns the sine of `x` (the angle in radians). The result is a number in the range `-1, 1`.",
    returns = "(Number)",
    type = "function"
   },
   sinh = {
    args = "( x )",
    description = "Returns the hyperbolic sine of `x`.",
    returns = "(Number)",
    type = "function"
   },
   sqrt = {
    args = "( x )",
    description = "Returns the square root of `x` (equivalent to the expression `x^0.5`).",
    returns = "(Number)",
    type = "function"
   },
   tan = {
    args = "( x )",
    description = "Returns the tangent of `x` (the angle in radians).",
    returns = "(Number)",
    type = "function"
   },
   tanh = {
    args = "( x )",
    description = "Returns the hyperbolic tangent of `x`.",
    returns = "(Number)",
    type = "function"
   }
  },
  description = "This set of math function uses the standard Lua 5.1 math library (which calls the standard C library).",
  type = "lib"
 },
 media = {
  childs = {
   RemoteSource = {
    description = "Used to indicate that a file path is to be interpreted as a URL to a remote server. See native.newVideo() for a usage example.",
    type = "value"
   },
   capturePhoto = {
    args = "()",
    description = "Opens a platform-specific interface to the device's camera. This function is asynchronous, i.e. it returns immediately so the calling code will continue to execute until the end of its scope; after that, the application will be suspended until the session is complete. By default, the image object is added to the top of the current stage (there is an option to save the image to a directory instead). A listener is required to handle the returned display object (image).",
    returns = "()",
    type = "function"
   },
   captureVideo = {
    args = "()",
    description = "Opens a platform-specific interface to the device's camera. This function is asynchronous, i.e. it returns immediately so the calling code will continue to execute until the end of its scope; after that, the application will be suspended until the session is complete. A listener is required to handle the returned display object (URL).  The returned URL can be used in native.newVideo().",
    returns = "()",
    type = "function"
   },
   getSoundVolume = {
    args = "()",
    description = "Returns the current volume setting for playback of extended sounds (media.playSound()).",
    returns = "(Number)",
    type = "function"
   },
   hasSource = {
    args = "( mediaSource )",
    description = "Determines if the given media source, such as a camera or photo library, is available on the platform. This function should be called before calling media.show() to determine if that media service is available. Returns `true` if the media source is available on the platform/device. Returns `false` if not.",
    returns = "(Boolean)",
    type = "function"
   },
   newEventSound = {
    args = "( filename [, baseDir] )",
    description = "Loads the event sound (1-3 seconds) from a sound file and returns an event sound ID that can be passed to media.playEventSound().",
    returns = "(Userdata)",
    type = "function"
   },
   newRecording = {
    args = "( [path] )",
    description = "Create an object for audio recording (Recording).",
    returns = "(Recording)",
    type = "function",
    valuetype = "_Recording"
   },
   pauseSound = {
    args = "()",
    description = "Pauses playback of the extended sound currently opened by the previous call to media.playSound() to resume playback of a paused sound.",
    returns = "(TYPE)",
    type = "function"
   },
   playEventSound = {
    args = "( sound [, baseDir] [, completionListener] )",
    description = "Plays an event sound (1-3 seconds). The first argument may be either an event sound ID or a filename for the event sound. Recommended for short sounds, especially to avoid performance hiccups.",
    returns = "()",
    type = "function"
   },
   playSound = {
    args = "( soundfile [, baseDir] [, onComplete] )",
    description = "Plays an extended sound (as opposed to an \"event sound\", 1-3 seconds duration), or resumes play of a paused extended sound. You can only have one such sound file open at a time.",
    returns = "()",
    type = "function"
   },
   playVideo = {
    args = "( path [, baseSource ], showControls, listener )",
    description = "Plays the video at the specified path (both local and remote) in a device-specific popup Video media player.",
    returns = "()",
    type = "function"
   },
   save = {
    args = "( filename [, baseDir] )",
    description = "Adds specified file to photo library.",
    returns = "()",
    type = "function"
   },
   selectPhoto = {
    args = "()",
    description = "Opens a platform-specific interface to the device's photo library. This function is asynchronous, i.e. it returns immediately so the calling code will continue to execute until the end of its scope; after that, the application will be suspended until the session is complete. By default, the image object is added to the top of the current stage (there is an option to save the image to a directory instead). A listener is required to handle the returned display object (image).",
    returns = "()",
    type = "function"
   },
   selectVideo = {
    args = "()",
    description = "Opens a platform-specific interface to the device's photo library. This function is asynchronous, i.e. it returns immediately so the calling code will continue to execute until the end of its scope; after that, the application will be suspended until the session is complete. A listener is required to handle the returned.",
    returns = "()",
    type = "function"
   },
   setSoundVolume = {
    args = "( vol )",
    description = "Adjusts the playback volume of an extended sound (media.playSound()). This setting can be adjusted at any time before or during the extended sound playback.",
    returns = "(TYPE)",
    type = "function"
   },
   show = {
    args = "( mediaSource, listener )",
    description = "**Note: As of 2013/10, this function is deprecated. Use media.selectPhoto() instead.**",
    returns = "()",
    type = "function"
   },
   stopSound = {
    args = "()",
    description = "Stops playback of the extended sound currently opened by the previous call to media.playSound().",
    returns = "()",
    type = "function"
   }
  },
  description = "The media library provides access to the multimedia features of the device: audio, video, camera and photo library.",
  type = "lib"
 },
 native = {
  childs = {
   canShowPopup = {
    args = "( name )",
    description = "Returns whether or not the popup type can be shown.",
    returns = "(Boolean)",
    type = "function"
   },
   cancelAlert = {
    args = "( alert )",
    description = "Dismisses an alert box programmatically. For example, you may wish to have a popup alert that automatically disappears after ten seconds even if the user doesn't click it. In that case, you could call this function at the end of a ten-second timer.",
    returns = "()",
    type = "function"
   },
   cancelWebPopup = {
    args = "()",
    description = "Dismisses the currently displaying web pop-up. This function takes no arguments because only one web popup can be shown at once time (not to be confused with native web views, which can have multiple instances shown at once)",
    returns = "()",
    type = "function"
   },
   getFontNames = {
    args = "()",
    description = "Returns an array of the available native fonts.",
    returns = "(Array)",
    type = "function"
   },
   getProperty = {
    args = "( key )",
    description = "Gets the value of a platform-specific property. This is only works on the iOS platform at the moment.",
    returns = "(Number)",
    type = "function"
   },
   getSync = {
    args = "( filename, parms )",
    description = "Get the iCloud automatic backup settings for files in the system.DocumentsDirectory on Mac and iOS systems.",
    returns = "(Boolean)",
    type = "function"
   },
   newFont = {
    args = "( name [, size] )",
    description = "Creates a font object that you can use to specify fonts in native text fields and text boxes. You can also pass it as a parameter to the display text objects in the display.newText() function.",
    returns = "(Userdata)",
    type = "function"
   },
   newMapView = {
    args = "( centerX, centerY, width, height )",
    description = "Renders a map view within the specified boundaries and returns a display object wrapper.",
    returns = "(Map)",
    type = "function",
    valuetype = "_Map"
   },
   newTextBox = {
    args = "( centerX, centerY, width, height [, listener ] )",
    description = "Creates a scrollable, multi-line text box for displaying text-based content. Text boxes can also be used for text input (and multi-line text input) by setting the `isEditable` property to `true`.",
    returns = "(TextBox)",
    type = "function",
    valuetype = "_TextBox"
   },
   newTextField = {
    args = "( centerX, centerY, width, height [, listener] )",
    description = "Creates a single-line text field for text input.",
    returns = "(TextField)",
    type = "function",
    valuetype = "_TextField"
   },
   newVideo = {
    args = "( centerX, centerY, width, height )",
    description = "Returns a video object that can be moved and rotated. You can also add a physics body to video objects. This API supports local videos (in one of the system directories) or from a remote location (server).",
    returns = "(Video)",
    type = "function",
    valuetype = "_Video"
   },
   newWebView = {
    args = "( centerX, centerY, width, height )",
    description = "Loads a remote web page in a web view container. Native web views differ from web popups in that you can move them (via `x`/`y` properties), rotate them (via the `rotation` property), or assign physics bodies to them in the same manner you would any other display objects.",
    returns = "(WebView)",
    type = "function",
    valuetype = "_WebView"
   },
   requestExit = {
    args = "()",
    description = "Closes the application window on Android gracefully without terminating the process.",
    returns = "()",
    type = "function"
   },
   setActivityIndicator = {
    args = "( state )",
    description = "Displays or hides a platform-specific activity indicator. Touch events are ignored while the indicator is shown.",
    returns = "()",
    type = "function"
   },
   setKeyboardFocus = {
    args = "( textField )",
    description = "Sets keyboard focus on a native.newTextField() and shows (or hides) the keyboard. Pass `nil` to remove focus and hide the keyboard.",
    returns = "()",
    type = "function"
   },
   setProperty = {
    args = "( key, value )",
    description = "Sets a platform specific property. This is only works on the iOS platform at the moment (not supported for Android development at this time).",
    returns = "()",
    type = "function"
   },
   setSync = {
    args = "( filename, params )",
    description = "Sets the iCloud automatic backup flag for files in the `system.DocumentsDirectory` on Mac OS X and iOS systems.",
    returns = "(Boolean)",
    type = "function"
   },
   showAlert = {
    args = "( title, message [, { buttonLabels } [, listener] ] )",
    description = "Displays a popup alert box with one or more buttons, using a native alert control. Program activity, including animation, will continue in the background, but all other user interactivity will be blocked until the user selects a button or cancels the dialog.",
    returns = "(Object)",
    type = "function"
   },
   showPopup = {
    args = "( name )",
    description = "Displays the operating system's default popup window for a specified service.",
    returns = "(Boolean)",
    type = "function"
   },
   showWebPopup = {
    args = "( url [, options] )",
    description = "Creates a web popup that loads a local or remote web page.",
    returns = "()",
    type = "function"
   },
   systemFont = {
    description = "Native system font for display.newText().",
    type = "value"
   },
   systemFontBold = {
    description = "Native \"bold\" system font for display.newText().",
    type = "value"
   }
  },
  description = "The native library wraps platform-specific UI elements. These are rendered by the OS, not the Corona engine. ",
  type = "lib"
 },
 network = {
  childs = {
   canDetectNetworkStatusChanges = {
    description = "Returns `true` if network status APIs are supported on the current platform.",
    type = "value"
   },
   cancel = {
    args = "( requestId )",
    description = "Cancel an outstanding network request made with network.request().",
    returns = "()",
    type = "function"
   },
   download = {
    args = "( url, method, listener [, params], filename [, baseDirectory] )",
    description = "This API is a convenience method that is very similar to the asynchronous network.request(), putting the destination filename and baseDirectory parameters into params.response and specifying `\"download\"` progress notifications. ",
    returns = "()",
    type = "function"
   },
   request = {
    args = "( url, method, listener [, params] )",
    description = "Makes an asynchronous HTTP or HTTPS request to a URL.",
    returns = "(Object)",
    type = "function"
   },
   setStatusListener = {
    args = "( hostURL, listener )",
    description = "Starts monitoring a host for its network reachability status.",
    returns = "()",
    type = "function"
   },
   upload = {
    args = "( url, method, listener [, params], filename [, baseDirectory] [, contentType] )",
    description = "This API is a convenience method that is very similar to the asynchronous network.request(), putting the source filename and baseDirectory parameters into a params.body table, adding contentType as a params.headers request header value, and specifying `\"upload\"` progress notifications. ",
    returns = "()",
    type = "function"
   }
  },
  description = "",
  type = "lib"
 },
 os = {
  childs = {
   clock = {
    args = "()",
    description = "Returns an approximation of the amount in seconds of CPU time used by the program.",
    returns = "(Number)",
    type = "function"
   },
   date = {
    args = "( [format [, time] ] )",
    description = "Returns a string or a table containing date and time, formatted according to the given string format.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   difftime = {
    args = "( t1, t2 )",
    description = "Returns the number of seconds from time t1 to time t2. In POSIX, Windows, and some other systems, this value is exactly t2-t1.",
    returns = "(Number)",
    type = "function"
   },
   execute = {
    args = "( cmd )",
    description = "Passes a string to the operating system for execution and returns a system-dependent status code. This function is equivalent to the C function `system()`. ",
    returns = "(Number)",
    type = "function"
   },
   exit = {
    args = "( [ exit ] )",
    description = "Calls the C function `exit()`, with an optional code, to terminate the host program. The default value for code is the success code.",
    returns = "()",
    type = "function"
   },
   remove = {
    args = "( file )",
    description = "Deletes a file or directory. It returns the two possible values:",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   rename = {
    args = "( oldname, newname )",
    description = "Renames a file or directory.",
    returns = "(Boolean)",
    type = "function"
   },
   time = {
    args = "( [table] )",
    description = "Returns the current time in seconds from 1970 when called without arguments, or a time representing the date and time specified by the given table.",
    returns = "(Number)",
    type = "function"
   }
  },
  description = "This standard Lua library provides functions for dealing with system time and date and other OS-related functions.",
  type = "lib"
 },
 package = {
  childs = {
   loaded = {
    description = "A table used by `require `to control which modules are already loaded. When you require a module modname and package.loadedmodname is not false, require simply returns the value stored there.",
    type = "value"
   },
   loaders = {
    description = "A table used by `require` to control how to load modules.",
    type = "value"
   },
   module = {
    args = "( name [, ...] )",
    description = "*Deprecated*",
    returns = "()",
    type = "function"
   },
   require = {
    args = "( moduleName )",
    description = "Loads the given module. ",
    returns = "(Library)",
    type = "function"
   },
   seeall = {
    description = "* Deprecated *",
    type = "value"
   }
  },
  description = "Corona supports Lua's module functionality for creating and loading external libraries. You can create your own libraries and call them from your application.",
  type = "lib"
 },
 physics = {
  childs = {
   addBody = {
    args = "()",
    description = "Allows you to turn almost any Corona display object into a simulated physical object with one line of code, including the assignment of physical properties.",
    returns = "(Boolean)",
    type = "function"
   },
   fromMKS = {
    args = "( unitName, value )",
    description = "Convenience function for converting from MKS units to Corona units.",
    returns = "(Number)",
    type = "function"
   },
   getAverageCollisionPositions = {
    args = "()",
    description = "It's common for Box2D to report multiple contact points during a single iteration of a simulation.",
    returns = "(Boolean)",
    type = "function"
   },
   getDebugErrorsEnabled = {
    args = "()",
    description = "This function is used to determine if extra physics errors are allowed to be caught by Box2D.",
    returns = "(Boolean)",
    type = "function"
   },
   getGravity = {
    args = "()",
    description = "Returns the __x__ and __y__ components of the global gravity vector, in units of m/s2. This takes advantage of the fact that Lua functions can return multiple values, which in this case are:",
    returns = "(Numbers)",
    type = "function"
   },
   getMKS = {
    args = "( key )",
    description = "Get the MKS value of the physics simulation for specific keys.",
    returns = "(Number)",
    type = "function"
   },
   getParticleRadius = {
    args = "()",
    description = "Returns the radius of particles in content units.",
    returns = "(Numbers)",
    type = "function"
   },
   getReportCollisionsInContentCoordinates = {
    args = "()",
    description = "This function is used to determine if the content origin is the collision point in the collision physics events.",
    returns = "(Boolean)",
    type = "function"
   },
   newJoint = {
    args = "( jointType, ... )",
    description = "Joints are used to assemble more complex game objects from multiple rigid bodes. To construct a joint, first construct the bodies that will be joined, and then call this method with the desired joint constructor type.",
    returns = "(Joint)",
    type = "function",
    valuetype = "_Joint"
   },
   pause = {
    args = "()",
    description = "Pause the physics engine.",
    returns = "()",
    type = "function"
   },
   queryRegion = {
    args = "( box_upper_left_x, box_upper_left_y, box_lower_right_x, box_lower_right_y )",
    description = "This function is used to find the objects that intersect with an axis-aligned (non-rotated) box.",
    returns = "()",
    type = "function"
   },
   rayCast = {
    args = "( from_x, from_y, to_x, to_y, \"closest\" )",
    description = "This function is used to find the objects that collide with a line, and the",
    returns = "()",
    type = "function"
   },
   reflectRay = {
    args = "( from_x, from_y, hits[1] )",
    description = "This function is used to reflect a vector as returned by physics.rayCast().",
    returns = "()",
    type = "function"
   },
   removeBody = {
    args = "( object )",
    description = "Removes a physics body from a display object without destroying the entire object. This removes the body created with physics.addBody(). This API cannot be used in a collision event listener.",
    returns = "(Boolean)",
    type = "function"
   },
   setAverageCollisionPositions = {
    args = "( enabled )",
    description = "Because it's common for Box2D to report multiple collision points during a single frame iteration, this setting is useful to report those points as one averaged point. This function applies to all the contact points in the collision physics events.",
    returns = "()",
    type = "function"
   },
   setContinuous = {
    args = "( enabled )",
    description = "By default, Box2D performs continuous collision detection, which prevents objects from \"tunneling.\" If it were turned off, an object that moves quickly enough could potentially pass through a thin wall.",
    returns = "()",
    type = "function"
   },
   setDebugErrorsEnabled = {
    args = "( enabled )",
    description = "This function allows extra physics errors to be caught by Box2D.",
    returns = "()",
    type = "function"
   },
   setDrawMode = {
    args = "( mode )",
    description = "Sets one of three possible \"rendering modes\" for the physics engine. While this feature will run on devices, it's most useful in the Corona Simulator when debugging unexpected physics engine behavior.",
    returns = "()",
    type = "function"
   },
   setGravity = {
    args = "( gx, gy )",
    description = "Sets the __x__ and __y__ components of the global gravity vector in units of m/s2. The default is `( 0, 9.8 )` to simulate standard Earth gravity, pointing downward on the __y__ axis.",
    returns = "()",
    type = "function"
   },
   setMKS = {
    args = "( key, value )",
    description = "Sets the MKS (meters, kilograms, and seconds) value of the physics simulation for specific keys. This is strictly for advanced purposes — the average developer and project will not require this function.",
    returns = "()",
    type = "function"
   },
   setParticleRadius = {
    args = "( radius_in_content_units )",
    description = "Sets the the radius of particles in content units.",
    returns = "()",
    type = "function"
   },
   setPositionIterations = {
    args = "( value )",
    description = "Sets the accuracy of the engine's position calculations. The default value is `8`.",
    returns = "()",
    type = "function"
   },
   setReportCollisionsInContentCoordinates = {
    args = "( enabled )",
    description = "This function sets the content origin as the collision point in the collision physics events.",
    returns = "()",
    type = "function"
   },
   setScale = {
    args = "( value )",
    description = "Sets the internal pixels-per-meter ratio that is used in converting between on-screen Corona coordinates and simulated physics coordinates. This should be done only once, before any physical objects are instantiated.",
    returns = "()",
    type = "function"
   },
   setTimeStep = {
    args = "( dt )",
    description = "Specifies either a frame-based (approximated) physics simulation or a time-based simulation.",
    returns = "()",
    type = "function"
   },
   setVelocityIterations = {
    args = "( value )",
    description = "Sets the accuracy of the engine's velocity calculations. The default value is `3`.",
    returns = "()",
    type = "function"
   },
   start = {
    args = "()",
    description = "This function start the physics simulation and should be called before any other physics functions.",
    returns = "()",
    type = "function"
   },
   stop = {
    args = "()",
    description = "Stops the physics engine. This function will return `false` and display a warning message in the Corona Simulator if the API cannot be processed.",
    returns = "(Boolean)",
    type = "function"
   },
   toMKS = {
    args = "( unitName, value )",
    description = "Convenience function for converting from Corona units to MKS units.",
    returns = "(Number)",
    type = "function"
   }
  },
  description = "Physics are commonly used for apps that involve a simulation of objects that move, collide, and interact under various physical forces like gravity. Corona makes it very easy to add physics to your apps, even if you've never worked with a physics engine before. While the underlying engine is built around the popular Box2D, we've taken a different design approach which eliminates a lot of the coding that is traditionally required.",
  type = "lib"
 },
 socket = {
  childs = {},
  description = "Corona currently includes the version 2.02 of the LuaSocket libraries. These Lua modules implement common network protocols such as SMTP (sending e-mails), HTTP (WWW access) and FTP (uploading and downloading files). Also included are features to support MIME (common encodings), URL manipulation and LTN12 (transferring and filtering data).",
  type = "lib"
 },
 sprite = {
  childs = {
   newSpriteSheet = {
    args = "( spriteSheetFile, [baseDir,]  frameWidth, frameHeight )",
    description = "Creates a new sprite sheet object from an sprite sheet image file whose animation frames are all the same height and width. To use a sprite sheet with non-uniformly spaced animation frames, use sprite.newSpriteSheetFromData()sprite.newSpriteSheetFromData.",
    returns = "(SpriteSheet)",
    type = "function",
    valuetype = "_SpriteSheet"
   }
  },
  description = "The old sprite library is only available in legacy versions of Corona. It has been removed from production builds of Corona.",
  type = "lib"
 },
 sqlite3 = {
  childs = {},
  description = "Corona includes support for SQLite databases on all platforms. This is based on the built-in sqlite support on the iPhone, and a compiled version of SQLite on Android (this increases the size of Android binaries by 300K).",
  type = "lib"
 },
 store = {
  childs = {
   availableStores = {
    args = "()",
    description = "Returns an array of store names that Corona supports in-app purchases for on the current device.  Only `google` and `apple` are supported.",
    returns = "(Array)",
    type = "function"
   },
   canLoadProducts = {
    args = "()",
    description = "This should be called before store.loadProducts().",
    returns = "(Boolean)",
    type = "function"
   },
   canMakePurchases = {
    args = "()",
    description = "This property is `true` if purchases are allowed, `false` otherwise.",
    returns = "(Boolean)",
    type = "function"
   },
   finishTransaction = {
    args = "( transaction )",
    description = "Notifies the Apple iTunes Store that a transaction is complete.",
    returns = "()",
    type = "function"
   },
   init = {
    args = "( [storeName,] listener )",
    description = "Activates in-app purchases (or in-app billing as it is known on Android platforms).",
    returns = "()",
    type = "function"
   },
   isActive = {
    args = "()",
    description = "To confirm that a store is properly initialized after calling store.init(), use the `store.isActive()` function. This returns `true` if the store has successfully initialized. Currently, it will return `false` on a Corona app built for Amazon or Nook.",
    returns = "(Boolean)",
    type = "function"
   },
   loadProducts = {
    args = "( productIdentifiers, listener )",
    description = "iOS supports the loading of product information that you've entered into iTunes Connect(https://itunesconnect.apple.com/). Corona provides the `store.canLoadProducts` function which returns `true` if the initialized store supports the loading of products. Following this check, the `store.loadProducts()` function will retrieve information about items available for sale. This includes the price of each item, a localized name, and a localized description.",
    returns = "()",
    type = "function"
   },
   purchase = {
    args = "( productList )",
    description = "Initiates a purchase transaction on a provided list of products.",
    returns = "()",
    type = "function"
   },
   restore = {
    args = "()",
    description = "Users who wipe the information on a device or buy a new device, may wish to restore previously purchased items without paying for them again. The `store.restore()` API initiates this process.",
    returns = "()",
    type = "function"
   },
   target = {
    args = "()",
    description = "Returns the app store the app was built for.  It will return one of the following strings: `\"amazon\"`, `\"apple\"`, `\"gameStick\"`, `\"google\"`, `\"nook\"`, `\"ouya\"`, `\"samsung\"`, or `\"none\"`.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   }
  },
  description = "This feature allows you to support In-App Purchases. Currently, only the Apple iTunes Store and the Google Play Marketplace are supported. In the future, other storefronts may be added.",
  type = "lib"
 },
 storyboard = {
  childs = {
   disableAutoPurge = {
    description = "By default, storyboard will automatically purge (e.g. remove the scene's display group, while leaving the actual module in memory) the least recently used scene whenever the OS receives a low memory warning.",
    type = "value"
   },
   getCurrentSceneName = {
    args = "()",
    description = "Returns the current scene name as a string, which can be used with storyboard.gotoScene() functions.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   getPrevious = {
    args = "()",
    description = "Gets the name of the previously active scene and returns it as a string.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   getScene = {
    args = "( sceneName )",
    description = "Returns the specified scene object (as returned from storyboard.newScene()). If the scene object does not exist, the function will return `nil`. This is useful for getting a reference to a specific scene object. For instance, if you have a function attached to a specific scene that a separate scene needs access to.",
    returns = "(Table)",
    type = "function"
   },
   gotoScene = {
    args = "( sceneName [, options] )",
    description = "Used to transition to a specific scene. Scenes must be explicitly purged/removed by the user. However, if the storyboard.disableAutoPurge flag is not set to `true`, the least recently used scene will be automatically purged whenever the OS issues a low memory warning.",
    returns = "()",
    type = "function"
   },
   hideOverlay = {
    args = "( [ purgeOnly, effect, effectTime ] )",
    description = "This function will hide/remove the current overlay scene (if one is currently being displayed). Overlay (e.g. \"pop up\") scenes can be shown using storyboard.showOverlay().",
    returns = "()",
    type = "function"
   },
   isDebug = {
    description = "Toggles \"Storyboard Debug Mode\", which will print useful debugging information to the Corona Terminal in certain situations if set to true. This should be set to `false` before production builds (default).",
    type = "value"
   },
   loadScene = {
    args = "( sceneName [, doNotLoadView, params ] )",
    description = "Loads specified scene, behind the currently active scene and hidden, without initiating a scene transition. This function is similar to storyboard.gotoScene(), but does not change the currently active scene. The only event that will be dispatched on the scene will be a \"createScene\" event (if the scene's view does not already exist).",
    returns = "()",
    type = "function"
   },
   newScene = {
    args = "( [ sceneName ] )",
    description = "Used to create new scene objects to be used with the Storyboard API (see that page for a scene tempalate module).",
    returns = "(Table)",
    type = "function"
   },
   printMemUsage = {
    args = "()",
    description = "Will print Lua memory and texture memory usage information in the terminal, but only if storyboard.isDebug is set to `true`.",
    returns = "()",
    type = "function"
   },
   purgeAll = {
    args = "()",
    description = "Will purge all scenes (except for the one that is currently active). For each scene that is purged, a destroyScene event will be dispatched prior to being purged.",
    returns = "()",
    type = "function"
   },
   purgeOnSceneChange = {
    description = "If set to `true`, whenever a scene change is completed, all scenes (except for the newly active scene) will be automatically purged. The default value for this property is `false`.",
    type = "value"
   },
   purgeScene = {
    args = "( sceneName )",
    description = "Unloads the specified scene's `scene.view` property, which is a group that contains all of the scene's display objects. It takes one (required) argument, `sceneName`, which is the name of the scene (usually the name of the module, without the \".lua\" extension) you are attempting to unload. If the scene has its own module, this function preserves the scene's module in the global `package.loaded` table.",
    returns = "()",
    type = "function"
   },
   reloadScene = {
    args = "()",
    description = "Reloads the currently loaded scene. This does not support scene transition effects. When a scene is reloaded, it behaves as if storyboard.gotoScene() was called on the currently active scene.",
    returns = "()",
    type = "function"
   },
   removeAll = {
    args = "()",
    description = "Will purge and remove all scenes (except for the currently active scene). A destroyScene event will be dispatched to all scenes before they are purged.",
    returns = "()",
    type = "function"
   },
   removeScene = {
    args = "( sceneName )",
    description = "Purges the specified scene, and then completely unloads the scene's associated module (if there is one).",
    returns = "()",
    type = "function"
   },
   showOverlay = {
    args = "( sceneName [, options ] )",
    description = "Load a scene above the currently active scene, leaving the currently active scene in-tact. This is commonly referred to as a \"pop up\" scene. This function supports all scene transition effects that are available for use with storyboard.gotoScene().",
    returns = "()",
    type = "function"
   },
   stage = {
    description = "This is a reference to the top-level storyboard display group that all scene views are inserted into. It can be thought of as the storyboard \"scene layer\". This is useful to have, for instance, if you need objects to be placed above or below ALL storyboard scenes (even during transition effects).",
    type = "value"
   }
  },
  description = "Storyboard is the built-in solution to scene (\"screens\") creation and management in Corona. This library provides developers an easy way to create and transition between individual scenes.",
  type = "lib"
 },
 string = {
  childs = {
   byte = {
    args = "( s [, i [, j]] )",
    description = "Returns the internal numerical codes of the characters in a string.",
    returns = "(Number)",
    type = "function"
   },
   char = {
    args = "( [arg1 [, ...] )",
    description = "Returns a string in which each character has the internal numerical code equal to its corresponding argument.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   find = {
    args = "( s, pattern [, init [, plain]] )",
    description = "Looks for the first match of a pattern in a string. If found, it returns the indices where the occurrence starts and ends; otherwise, returns `nil`.",
    returns = "(Numbers)",
    type = "function"
   },
   format = {
    args = "( formatstring [, ...] )",
    description = "Returns a formatted string following the description given in its arguments.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   gmatch = {
    args = "( s, pattern )",
    description = "Returns a pattern finding iterator.",
    returns = "(TYPE)",
    type = "function"
   },
   gsub = {
    args = "( s, pattern, repl [, n] )",
    description = "Replace all occurrences of a pattern in a string.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   len = {
    args = "( s )",
    description = "Returns the length of a string (e.g. amount of characters).",
    returns = "(Number)",
    type = "function"
   },
   lower = {
    args = "( s )",
    description = "Change uppercase characters in a string to lowercase.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   match = {
    args = "( s, pattern [, init] )",
    description = "Extract substrings by matching a pattern in a string. If a match is found, the captures from the pattern; otherwise it returns `nil`. If pattern specifies no captures, then the whole match is returned.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   rep = {
    args = "( s, n )",
    description = "Replicates a string by returning a string that is the concatenation of `n` copies of a specified String.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   reverse = {
    args = "( s )",
    description = "Reverses a string.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   sub = {
    args = "( s, i [, j] )",
    description = "Returns a substring (e.g. a specified portion of an existing string).",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   upper = {
    args = "( s )",
    description = "Change lowercase characters in a string to uppercase. All other characters are left unchanged.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   }
  },
  description = "This library provides generic functions for string manipulation, such as finding and extracting substrings, and pattern matching. This uses the standard Lua 5.1 library.",
  type = "lib"
 },
 system = {
  childs = {
   CachesDirectory = {
    description = "Used with system.pathForFile() to create a path for storing and retrieving files that are available across application launches. This is ideal for saving state information. ",
    type = "value"
   },
   DocumentsDirectory = {
    description = "Used with system.pathForFile() to create a path for storing and retrieving files that need to persist between application sessions. The path is `/Documents`.",
    type = "value"
   },
   ResourceDirectory = {
    description = "Used with system.pathForFile() to create a path for retrieving files where all the application assets exist (e.g., image and sound files). This often called the \"app bundle.\"",
    type = "value"
   },
   TemporaryDirectory = {
    description = "Used with system.pathForFile() to create a path for storing and retrieving files that only need to persist while the application is running. The path is `/tmp`.",
    type = "value"
   },
   activate = {
    args = "( feature )",
    description = "Activates a system level feature, such as `\"multitouch\"`. Use system.deactivate() to disable a feature.",
    returns = "()",
    type = "function"
   },
   cancelNotification = {
    args = "()",
    description = "Removes the specified notification from the scheduler, status bar, or notification center.",
    returns = "()",
    type = "function"
   },
   deactivate = {
    args = "()",
    description = "Deactivates a system level feature, such as `\"multitouch\"`.",
    returns = "()",
    type = "function"
   },
   getIdleTimer = {
    args = "()",
    description = "Returns whether the application idle timer is enabled. ",
    returns = "(Boolean)",
    type = "function"
   },
   getInfo = {
    args = "( property )",
    description = "Returns information about the system on which the application is running.",
    returns = "()",
    type = "function"
   },
   getInputDevices = {
    args = "()",
    description = "Fetches all input devices that are currenty connected to the system, such as a touchscreen, keyboard, mouse, joystick, etc. Input devices that were once connected but later disconnected will not be returned by this function.",
    returns = "(InputDevice)",
    type = "function",
    valuetype = "_InputDevice"
   },
   getPreference = {
    args = "( category, name )",
    description = "Returns a preference value as a string.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   getTimer = {
    args = "()",
    description = "Returns time in milliseconds since application launch. ",
    returns = "(Number)",
    type = "function"
   },
   hasEventSource = {
    args = "( eventName )",
    description = "Returns whether the system delivers events corresponding to `eventName`.",
    returns = "(Boolean)",
    type = "function"
   },
   openURL = {
    args = "( url )",
    description = "Open a web page in the browser, create an email, or call a phone number.",
    returns = "()",
    type = "function"
   },
   orientation = {
    args = "()",
    description = "Returns a string identifying the orientation of the device:",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   pathForFile = {
    args = "( filename [, baseDirectory] )",
    description = "Generates an absolute path using system-defined directories as the base. An additional optional parameter, `baseDirectory`, specifies which base directory is used to construct the full path, with its default value being `system.ResourceDirectory`.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   scheduleNotification = {
    args = "( secondsFromNow [, options] )",
    description = "Schedule a local notification event to be delivered in the future. Returns a reference ID that can be used to cancel the notification.",
    returns = "()",
    type = "function"
   },
   setAccelerometerInterval = {
    args = "( frequency )",
    description = "Sets the frequency of accelerometer events. On the iPhone, the minimum frequency is 10 Hz and the maximum is 100 Hz. Accelerometer events are a significant drain on battery, so only increase the frequency when you need faster responses.",
    returns = "()",
    type = "function"
   },
   setGyroscopeInterval = {
    args = "( frequency )",
    description = "Sets the frequency of gyroscope events in Hz. Gyroscope events are a significant drain on battery, so only increase the frequency when you need faster responses. ",
    returns = "()",
    type = "function"
   },
   setIdleTimer = {
    args = "( enabled )",
    description = "Controls whether the idle timer is enabled. If set to `true`, the timer will be active (default) or inactive if `false`. When active, the idle timer dims the screen and eventually puts the device to sleep when no user activity occurs (e.g. screen touches).",
    returns = "()",
    type = "function"
   },
   setLocationAccuracy = {
    args = "( distance )",
    description = "Sets the desired accuracy of location (GPS) events to distance in meters. Note: the actual accuracy depends on the capabilities of the device and/or platform. On the iPhone, accuracy is limited to discrete distances: < 10, 10, 100, 1000, and 3000 meters. Higher accuracy (smaller distances) requires more battery life, so use larger distance to preserve battery life.",
    returns = "()",
    type = "function"
   },
   setLocationThreshold = {
    args = "( distance )",
    description = "Sets how much distance in meters must be travelled until the next location (GPS) event is sent. Because location events involve hardware that can drain the battery, using larger threshold distances preserve battery life.",
    returns = "()",
    type = "function"
   },
   setTapDelay = {
    args = "( delayTime )",
    description = "The delay time between when a tap is detected and when the tap event is delivered. By default, this time is 0.",
    returns = "()",
    type = "function"
   },
   vibrate = {
    args = "()",
    description = "Vibrates the phone. On the Corona simulator this will sound a system beep (Mac OS X only).",
    returns = "()",
    type = "function"
   }
  },
  description = "The system functions listed here return information about the system (device information, current orientation, etc.) and control system functions (enable multitouch, control the idle time, accelerometer, GPS, etc.)",
  type = "lib"
 },
 table = {
  childs = {
   concat = {
    args = "( t )",
    description = "Concatenate the elements of a table together to form a string. Each element must be able to be coerced into a string. A separator can be specified which is placed between concatenated elements.",
    returns = "(String)",
    type = "function",
    valuetype = "string"
   },
   copy = {
    args = "( t )",
    description = "Returns a shallow copy of array, i.e. the portion of the array (table) with integer keys. A variable number of additional arrays can be passed in as optional arguments. If an array has a hole (a `nil` entry), copying in a given source array stops at the last consecutive item prior to the hole.",
    returns = "(Array)",
    type = "function"
   },
   indexOf = {
    args = "( t, element )",
    description = "Returns the integer index of `element` in array `t`. Returns `nil` if not in array. The search goes through the length of the array as determined by `#t`, whose value is undefined if there are holes.",
    returns = "(Number)",
    type = "function"
   },
   insert = {
    args = "( t, value )",
    description = "Insert a given value into a table. If a position is given insert the value before the element currently at that position.",
    returns = "()",
    type = "function"
   },
   maxn = {
    args = "( t )",
    description = "Returns the largest positive numerical index of the given table, or zero if the table has no positive numerical indices. To do its job, this function does a linear traversal of the whole table.",
    returns = "(Number)",
    type = "function"
   },
   remove = {
    args = "( t )",
    description = "Removes from table the element at position `pos`, shifting down other elements to close the space, if necessary. Returns the value of the removed element. ",
    returns = "()",
    type = "function"
   },
   sort = {
    args = "( t )",
    description = "Sorts table elements in a given order, in-place, from `table1` to `tablen`, where `n` is the length of the table. It receives the table (array) to be sorted plus an optional `compare` function. This `compare` function receives two arguments and must return `true` if the first argument should come first in the sorted array. If `compare` is __not__ given, then the standard Lua operator `<` is used instead.",
    returns = "(Table)",
    type = "function"
   }
  },
  description = "Tables in Lua implement associative arrays. That is, they can be indexed not just with numbers, but also with strings or any other value of the language, except nil.",
  type = "lib"
 },
 timer = {
  childs = {
   cancel = {
    args = "( timerID )",
    description = "Cancels a timer operation initiated with timer.performWithDelay().",
    returns = "()",
    type = "function"
   },
   pause = {
    args = "( timerId )",
    description = "Pauses a timer started with timer.performWithDelay() that represents the amount of time remaining in the timer.",
    returns = "(Number)",
    type = "function"
   },
   performWithDelay = {
    args = "( delay, listener [, iterations] )",
    description = "Call a specified function after a delay. This function returns an object to cancel the invocation of the specified listener.",
    returns = "(Object)",
    type = "function"
   },
   resume = {
    args = "( timerId )",
    description = "Resumes a timer that was paused with timer.pause() that represents the amount of time remaining in the timer.",
    returns = "(Number)",
    type = "function"
   }
  },
  description = "Timer functions allow calling a function some time in the future rather than immediately. The timer library provides the basic functions to let set up the timer and manage the event listener.",
  type = "lib"
 },
 transition = {
  childs = {
   blink = {
    args = "( target, params )",
    description = "Blinks (glows) an object in and out over a specified time, repeating indefinitely.",
    returns = "(Object)",
    type = "function"
   },
   cancel = {
    args = "()",
    description = "The `transition.cancel()` function will cancel one of the following, depending on the passed parameter:",
    returns = "()",
    type = "function"
   },
   dissolve = {
    args = "( object1, object2, time, delay )",
    description = "Performs a dissolve transition between two display objects.",
    returns = "(Object)",
    type = "function"
   },
   fadeIn = {
    args = "( target, params )",
    description = "Fades an object to alpha of `1.0` over the specified time.",
    returns = "(Object)",
    type = "function"
   },
   fadeOut = {
    args = "( target, params )",
    description = "Fades an object to alpha of `0.0` over the specified time.",
    returns = "(Object)",
    type = "function"
   },
   from = {
    args = "( target, params )",
    description = "Similar to transition.to() except that the starting property values are specified in the parameters table and the final values are the corresponding property values of the object prior to the call.",
    returns = "(Reference)",
    type = "function",
    valuetype = "_Reference"
   },
   moveBy = {
    args = "( target, params )",
    description = "Moves an object by the specified `x` and `y` coordinate amount over a specified time.",
    returns = "(Object)",
    type = "function"
   },
   moveTo = {
    args = "( target, params )",
    description = "Moves an object to the specified `x` and `y` coordinates over a specified time.",
    returns = "(Object)",
    type = "function"
   },
   pause = {
    args = "()",
    description = "The `transition.pause()` function pauses one of the following, depending on the passed parameter:",
    returns = "()",
    type = "function"
   },
   resume = {
    args = "()",
    description = "The `transition.resume()` function resumes one of the following, depending on the passed parameter:",
    returns = "()",
    type = "function"
   },
   scaleBy = {
    args = "( target, params )",
    description = "Scales an object by the specified `xScale` and `yScale` amounts over a specified time.",
    returns = "(Object)",
    type = "function"
   },
   scaleTo = {
    args = "( target, params )",
    description = "Scales an object to the specified `xScale` and `yScale` amounts over a specified time.",
    returns = "(Object)",
    type = "function"
   },
   to = {
    args = "( target, params )",
    description = "Animates (transitions) a display object algorithm. Use this to move, rotate, fade, or scale an object over a specific period of time.",
    returns = "(Object)",
    type = "function"
   }
  },
  description = "The transition library provides functions and methods to transition/tween display objects or display groups over a specific period of time. Library features include:",
  type = "lib"
 },
 widget = {
  childs = {
   migration = {
    description = "This guide aims to ease your migration from widgets v1.0 to widgets v2.0",
    type = "value"
   },
   newButton = {
    args = "( options )",
    description = "Creates a Button object.",
    returns = "(ButtonWidget)",
    type = "function",
    valuetype = "_ButtonWidget"
   },
   newPickerWheel = {
    args = "( options )",
    description = "Creates a PickerWheel object.",
    returns = "(PickerWidget)",
    type = "function",
    valuetype = "_PickerWidget"
   },
   newProgressView = {
    args = "( options )",
    description = "Creates a ProgressView object.",
    returns = "(ProgressViewWidget)",
    type = "function",
    valuetype = "_ProgressViewWidget"
   },
   newScrollView = {
    args = "( options )",
    description = "Creates a ScrollView object.",
    returns = "(ScrollViewWidget)",
    type = "function",
    valuetype = "_ScrollViewWidget"
   },
   newSegmentedControl = {
    args = "( options )",
    description = "Creates a Segmented Control object.",
    returns = "(SegmentedControlWidget)",
    type = "function",
    valuetype = "_SegmentedControlWidget"
   },
   newSlider = {
    args = "( options )",
    description = "Creates a Slider object.",
    returns = "(SliderWidget)",
    type = "function",
    valuetype = "_SliderWidget"
   },
   newSpinner = {
    args = "( options )",
    description = "Creates a Spinner object.",
    returns = "(SpinnerWidget)",
    type = "function",
    valuetype = "_SpinnerWidget"
   },
   newStepper = {
    args = "( options )",
    description = "Creates a Stepper object. The stepper consists of a minus and plus button which can be tapped or held down to decrement/increment a value, for example, the music or sound volume setting in a game.",
    returns = "(StepperWidget)",
    type = "function",
    valuetype = "_StepperWidget"
   },
   newSwitch = {
    args = "( options )",
    description = "Creates a Switch object.",
    returns = "(SwitchWidget)",
    type = "function",
    valuetype = "_SwitchWidget"
   },
   newTabBar = {
    args = "( options )",
    description = "Creates a TabBar object.",
    returns = "(TabBarWidget)",
    type = "function",
    valuetype = "_TabBarWidget"
   },
   newTableView = {
    args = "( options )",
    description = "Creates a TableView object.",
    returns = "(TableView)",
    type = "function",
    valuetype = "_TableView"
   },
   setTheme = {
    args = "( themeFile )",
    description = "Use this function to set the overall theme of the widget library. This should be called immediately after you `require()` the widget library.",
    returns = "()",
    type = "function"
   }
  },
  type = "lib"
 }
}
end

-->-o-<-- Save this part as corona-script.lua in this folder,
-- open with ZBS and run with F6.
-- The script will overwrite api/lua/corona.lua in this installation of ZBS.

local mobdebug = require('mobdebug')
local lfs = require('lfs')

local DIR_SEP = '/'

local API = {}

local function trim(s)
    local from = s:match"^%s*()"
    return from > #s and "" or s:match(".*%S", from)
end

local function startswith(s, piece)
    return string.sub(s, 1, string.len(piece)) == piece
end

local function endswith(s, send)
    return #s >= #send and s:find(send, #s-#send+1, true) and true or false
end

local function extractPath(p)
    local c
    for i = p:len(), 1, -1 do
        c = p:sub(i, i)
        if c == DIR_SEP then
            return p:sub(1, i - 1), p:sub(i + 1)
        end
    end
end

local function beforeComma(line)
    local c
    for i = 1, line:len() do
        if line:sub(i, i) == '.' then
            return line:sub(1, i - 1)
        end
    end
    return line
end

local function extractFirstSentence(line)
    local count = line:len()
    for i = 1, count do
        if line:sub(i, i) == '.' then
            if i == count then
                return line
            elseif line:sub(i + 1, i + 1) == ' ' then
                return line:sub(1, i)
            end
        end
    end
    return line
end

local function stripTags(line)
    return line:gsub('%[api.+]', '')
      :gsub('[%]%[]', '')
      :gsub('&nbsp;', ' ')
      :gsub('&mdash;', '-')
      :gsub('&sup(%d);', '%1')
      :gsub('’', "'") -- replace unicode quote
end

local function extractOverview(filename)
    local overviewInd
    local i = 0
    local f = io.open(filename, 'r')
    for l in f:lines() do
        i = i + 1
        if l == '## Overview' then
            overviewInd = i + 2
        elseif i == overviewInd then
            if l:sub(1, 1) == '!' then
                overviewInd = i + 2
            else
                -- Uncomment to make descriptions shorter
                --l = extractFirstSentence(l)
                return stripTags(l)
            end
        end
    end
    f:close()
end

local function extractTypeArgsReturns(filename)
    local t, a, r
    local syntaxInd
    local i = 0
    local f = io.open(filename, 'r')
    for l in f:lines() do
        i = i + 1
        if startswith(l, '> __Type__') then
            t = l:match('%[(%a*)%]', 1):lower()
        elseif startswith(l, '> __Return value__') then
            r = l:match('%[(%a*)%]', 1)
        elseif l == '## Syntax' then
            syntaxInd = i + 2
        elseif i == syntaxInd then
            a = l:match('%(.+%)', 1)
        end
        if t and a and r then
            return t, a, r
        end
    end
    f:close()
    return t or '', a or '', r or ''
end

-- generate special valuetype names to avoid conflicts
-- with variable names that may have different capitalization
local function specialType(r) return '_'..r end

local function processApiDir(root, kind, item)
	for entity in lfs.dir(root) do
		if entity:sub(1, 1) ~= '.' then
			local fullPath = root .. DIR_SEP .. entity
			local mode = lfs.attributes(fullPath, 'mode')
			if mode == 'file' and item then
				if entity == 'index.markdown' then
                    API[item].description = extractOverview(fullPath)
                else
                    local t, a, r = extractTypeArgsReturns(fullPath)
                    if t ~= 'function' then
                        t, a, r = 'value', nil, nil
                    elseif kind == 'class' then
                        t = 'method'
                    end
                    local child = {type = t, description = extractOverview(fullPath), args = a, returns = r}
                    if r and r ~= ''
                        and r ~= 'String'
                        and r ~= 'Boolean'
                        and r ~= 'Number'
                        and r ~= 'Numbers'
                        and r ~= 'Constant'
                        and r ~= 'Function'
                        and r ~= 'Array'
                        and r ~= 'Table'
                        and r ~= 'TYPE'
                        and r ~= 'Object'
                        and r ~= 'Library'
                        and r ~= 'Module'
                        and r ~= 'CoronaClass'
                        and r ~= 'Event'
                        and r ~= 'Listener'
                        and r ~= 'Userdata' then
                        child.valuetype = specialType(r)
                    elseif r == 'String' then
                        child.valuetype = 'string'
                    end
                    if t == 'function' or t == 'method' then
                      child.args = a and #a > 1 and a or '()'
                      child.returns = '('..(r or '')..')'
                    end
                    API[item].childs[beforeComma(entity)] = child
                end
			elseif mode == 'directory' and entity ~= 'global' then
		if kind == 'class' then entity = specialType(entity) end
                API[entity] = {type = kind, description = '', childs = {}}
				processApiDir(fullPath, kind, entity)
			end
		end
	end
end
processApiDir('library', 'lib')
processApiDir('type', 'class')

local _, path = nil, arg[-3]:gsub('[\\/]', DIR_SEP)
repeat
    path, _ = extractPath(path)
until endswith(path, 'bin')
path, _ = extractPath(path)
-- Path to ZeroBraneStudio dir
local filename = path .. DIR_SEP .. 'api' .. DIR_SEP .. 'lua' .. DIR_SEP .. 'corona.lua'
local file = io.open(filename, 'w')
file:write('do return ')
file:write(mobdebug.line(API, {indent = ' ', comment = false}),"\nend\n")
file:close()
