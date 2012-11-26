-- Copyright 2011-12 Paul Kulchenko, ZeroBrane LLC

return {
  MOAIAction = {
    type = "class",
    description = "Base class for actions.",
    childs = {
      EVENT_STOP = {
        type = "value",
        description = "ID of event stop callback. Signature is: nil onStop ()",
      },
      addChild = {
        type = "function",
        description = "Attaches a child action for updating.",
        args = '(self: MOAIAction, child: MOAIAction)',
        returns = '(self: MOAIAction)',
      },
      attach = {
        type = "function",
        description = "Attaches a child to a parent action. The child will receive updates from the parent only if the parent is in the action tree.",
        args = '(self: MOAIAction [, parent: MOAIAction | nil])',
        returns = '(self: MOAIAction)',
      },
      clear = {
        type = "function",
        description = "Removes all child actions.",
        args = '(self: MOAIAction)',
        returns = '(self: MOAIAction)',
      },
      detach = {
        type = "function",
        description = "Detaches an action from its parent (if any) thereby removing it from the action tree. Same effect as calling stop ().",
        args = '(self: MOAIAction)',
        returns = '(self: MOAIAction)',
      },
      isActive = {
        type = "function",
        description = "Checks to see if an action is currently in the action tree.",
        args = '(self: MOAIAction)',
        returns = '(isActive: bool)',
      },
      isBusy = {
        type = "function",
        description = "Checks to see if an action is currently busy. An action is 'busy' only if it is 'active' and not 'done.'",
        args = '(self: MOAIAction)',
        returns = '(isBusy: bool)',
      },
      isDone = {
        type = "function",
        description = "Checks to see if an action is 'done.' Definition of 'done' is up to individual action implementations.",
        args = '(self: MOAIAction)',
        returns = '(isDone: bool)',
      },
      pause = {
        type = "function",
        description = "Leaves the action in the action tree but prevents it from receiving updates. Call pause ( false ) or start () to unpause.",
        args = '(self: MOAIAction [, pause: bool | true])',
        returns = '()',
      },
      start = {
        type = "function",
        description = "Adds the action to a parent action or the root of the action tree.",
        args = '(self: MOAIAction [, parent: MOAIAction | MOAIActionMgr])',
        returns = '(self: MOAIAction)',
      },
      stop = {
        type = "function",
        description = "Removed the action from its parent action; action will stop being updated.",
        args = '(self: MOAIAction)',
        returns = '(self: MOAIAction)',
      },
      throttle = {
        type = "function",
        description = "Sets the actions throttle. Throttle is a scalar on time. Is is passed to the action's children.",
        args = '(self: MOAIAction [, throttle: number | 1])',
        returns = '(self: MOAIAction)',
      },
    },
  },
  MOAIActionMgr = {
    type = "class",
    description = "Manager class for MOAIActions.",
    childs = {
      getRoot = {
        type = "function",
        description = "Returns the current root action.",
        args = '()',
        returns = '(root: MOAIAction)',
      },
      setProfilingEnabled = {
        type = "function",
        description = "Enables action profiling.",
        args = '([enable: boolean | false])',
        returns = '()',
      },
      setRoot = {
        type = "function",
        description = "Replaces or clears the root action.",
        args = '([root: MOAIAction | nil])',
        returns = '()',
      },
      setThreadInfoEnabled = {
        type = "function",
        description = "Enables function name and line number info for MOAICoroutine.",
        args = '([enable: boolean | false])',
        returns = '()',
      },
    },
  },
  MOAIAnim = {
    type = "class",
    description = "Bind anim curves to nodes and provides timer controls for anim playback.",
    childs = {
      apply = {
        type = "function",
        description = "Apply the anim at a given time or time step.",
        args = '(self: MOAIAnim [, t0: number | 0])',
        returns = '()',
      },
      getLength = {
        type = "function",
        description = "Return the length of the animation.",
        args = '(self: MOAIAnim)',
        returns = '(length: number)',
      },
      reserveLinks = {
        type = "function",
        description = "Reserves a specified number of links for the animation.",
        args = '(self: MOAIAnim, nLinks: number)',
        returns = '()',
      },
      setLink = {
        type = "function",
        description = "Connect a curve to a given node attribute.",
        args = '(self: MOAIAnim, linkID: number, curve: MOAIAnimCurveBase, target: MOAINode, attrID: number [, asDelta: boolean | false])',
        returns = '()',
      },
    },
  },
  MOAIAnimCurve = {
    type = "class",
    description = "Implementation of anim curve for floating point values.",
    childs = {
      getValueAtTime = {
        type = "function",
        description = "Return the interpolated value given a point in time along the curve. This does not change the curve's built in TIME attribute (it simply performs the requisite computation on demand).",
        args = '(self: MOAIAnimCurve, time: number)',
        returns = '(interpolated: number)',
      },
      setKey = {
        type = "function",
        description = "Initialize a key frame at a given time with a give value. Also set the transition type between the specified key frame and the next key frame.",
        args = '(self: MOAIAnimCurve, index: number, time: number, value: number [, mode: number [, weight: number]])',
        returns = '()',
      },
    },
  },
  MOAIAnimCurveBase = {
    type = "class",
    description = "Piecewise animation function with one input (time) and one output (value). This is the base class for typed anim curves (float, quaternion, etc.).",
    childs = {
      getLength = {
        type = "function",
        description = "Return the largest key frame time value in the curve.",
        args = '(self: MOAIAnimCurveBase)',
        returns = '(length: number)',
      },
      reserveKeys = {
        type = "function",
        description = "Reserve key frames.",
        args = '(self: MOAIAnimCurveBase, nKeys: number)',
        returns = '()',
      },
      setWrapMode = {
        type = "function",
        description = "Sets the wrap mode for values above 1.0 and below 0.0. CLAMP sets all values above and below 1.0 and 0.0 to values at 1.0 and 0.0 respectively",
        args = '(self: MOAIAnimCurveBase [, mode: number])',
        returns = '()',
      },
    },
  },
  MOAIAnimCurveQuat = {
    type = "class",
    description = "Implementation of anim curve for rotation (via quaternion) values.",
    childs = {
      getValueAtTime = {
        type = "function",
        description = "Return the interpolated value (as Euler angles) given a point in time along the curve. This does not change the curve's built in TIME attribute (it simply performs the requisite computation on demand).",
        args = '(self: MOAIAnimCurveQuat, time: number)',
        returns = '(xRot: number, yRot: number, zRot: number)',
      },
      setKey = {
        type = "function",
        description = "Initialize a key frame at a given time with a give value (as Euler angles). Also set the transition type between the specified key frame and the next key frame.",
        args = '(self: MOAIAnimCurve, index: number, time: number, xRot: number, yRot: number, zRot: number [, mode: number [, weight: number]])',
        returns = '()',
      },
    },
  },
  MOAIAnimCurveVec = {
    type = "class",
    description = "Implementation of anim curve for 3D vector values.",
    childs = {
      getValueAtTime = {
        type = "function",
        description = "Return the interpolated vector components given a point in time along the curve. This does not change the curve's built in TIME attribute (it simply performs the requisite computation on demand).",
        args = '(self: MOAIAnimCurveQuat, time: number)',
        returns = '(x: number, y: number, z: number)',
      },
      setKey = {
        type = "function",
        description = "Initialize a key frame at a given time with a give vector. Also set the transition type between the specified key frame and the next key frame.",
        args = '(self: MOAIAnimCurve, index: number, time: number, x: number, y: number, z: number [, mode: number [, weight: number]])',
        returns = '()',
      },
    },
  },
  MOAIBitmapFontReader = {
    type = "class",
    description = "Legacy font reader for Moai's original bitmap font format. The original format is just a bitmap containing each glyph in the font divided by solid-color guide lines (see examples). This is an easy way for artists to create bitmap fonts. Kerning is not supported by this format.</p>",
    childs = {
      loadPage = {
        type = "function",
        description = "Rips a set of glyphs from a bitmap and associates them with a size.",
        args = '(self: MOAIFont, filename: string, charCodes: string, points: number [, dpi: number | 72])',
        returns = '()',
      },
    },
  },
  MOAIBoundsDeck = {
    type = "class",
    description = "Deck of bounding boxes. Bounding boxes are allocated in a separate array from that used for box indices. The index array is used to map deck indices onto bounding boxes. In other words there may be more indices then boxes thus allowing for re-use of boxes over multiple indices.</p>",
    childs = {
      reserveBounds = {
        type = "function",
        description = "Reserve an array of bounds to be indexed.",
        args = '(self: MOAIBoundsDeck, nBounds: number)',
        returns = '()',
      },
      reserveIndices = {
        type = "function",
        description = "Reserve indices. Each index maps a deck item onto a bounding box.",
        args = '(self: MOAIBoundsDeck, nIndices: number)',
        returns = '()',
      },
      setBounds = {
        type = "function",
        description = "Set the dimensions of a bounding box at a given index.",
        args = '(self: MOAIBoundsDeck, idx: number, xMin: number, yMin: number, zMin: number, xMax: number, yMax: number, zMax: number)',
        returns = '()',
      },
      setIndex = {
        type = "function",
        description = "Associate a deck index with a bounding box.",
        args = '(self: MOAIBoundsDeck, idx: number, boundsID: number)',
        returns = '()',
      },
    },
  },
  MOAIBox2DArbiter = {
    type = "class",
    description = "Box2D Arbiter.",
    childs = {
      getNormalImpulse = {
        type = "function",
        description = "Returns total normal impulse for contact.",
        args = '(self: MOAIBox2DArbiter, self: MOAIBox2DArbiter)',
        returns = '(normal.x: number, normal.y: number, impulse: number)',
      },
      getTangentImpulse = {
        type = "function",
        description = "Returns total tangent impulse for contact.",
        args = '(self: MOAIBox2DArbiter)',
        returns = '(impulse: number)',
      },
      setContactEnabled = {
        type = "function",
        description = "Enabled or disable the contact.",
        args = '(self: MOAIBox2DArbiter)',
        returns = '(impulse: number)',
      },
    },
  },
  MOAIBox2DBody = {
    type = "class",
    description = "Box2D body.",
    childs = {
      DYNAMIC = {
        type = "value",
        description = "",
      },
      KINEMATIC = {
        type = "value",
        description = "",
      },
      STATIC = {
        type = "value",
        description = "",
      },
      addChain = {
        type = "function",
        description = "Create and add a set of collision edges to teh body.",
        args = '(self: MOAIBox2DBody, verts: table [, closeChain: boolean | false])',
        returns = '(fixture: MOAIBox2DFixture)',
      },
      addCircle = {
        type = "function",
        description = "Create and add circle fixture to the body.",
        args = '(self: MOAIBox2DBody, x: number, y: number, radius: number)',
        returns = '(fixture: MOAIBox2DFixture)',
      },
      addPolygon = {
        type = "function",
        description = "Create and add a polygon fixture to the body.",
        args = '(self: MOAIBox2DBody, verts: table, self: MOAIBox2DBody, verts: table)',
        returns = '(Array: table, fixture: MOAIBox2DFixture)',
      },
      addRect = {
        type = "function",
        description = "Create and add a rect fixture to the body.",
        args = '(self: MOAIBox2DBody, xMin: number, yMin: number, xMax: number, yMax: number, angle: number)',
        returns = '(fixture: MOAIBox2DFixture)',
      },
      applyAngularImpulse = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody, angularImpulse: number)',
        returns = '()',
      },
      applyForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody, forceX: number, forceY: number [, pointX: number [, pointY: number]])',
        returns = '()',
      },
      applyLinearImpulse = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody, impulseX: number, impulseY: number [, pointX: number [, pointY: number]])',
        returns = '()',
      },
      applyTorque = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, torque: number | 0])',
        returns = '()',
      },
      destroy = {
        type = "function",
        description = "Schedule body for destruction.",
        args = '(self: MOAIBox2DBody)',
        returns = '()',
      },
      getAngle = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(angle: number)',
      },
      getAngularVelocity = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(omega: number)',
      },
      getInertia = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(inertia: number)',
      },
      getLinearVelocity = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(velocityX: number, velocityY: number)',
      },
      getLocalCenter = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(centerX: number, centerY: number)',
      },
      getMass = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(Mass: number)',
      },
      getPosition = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(positionX: number, positionY: number)',
      },
      getWorldCenter = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(worldX: number, worldY: number)',
      },
      isActive = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(isActive: boolean)',
      },
      isAwake = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(isAwake: boolean)',
      },
      isBullet = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(isBullet: boolean)',
      },
      isFixedRotation = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '(isFixedRotation: boolean)',
      },
      resetMassData = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody)',
        returns = '()',
      },
      setActive = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, active: boolean | false])',
        returns = '()',
      },
      setAngularDamping = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody, damping: number)',
        returns = '()',
      },
      setAngularVelocity = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, omega: number | 0])',
        returns = '()',
      },
      setAwake = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, awake: boolean | true])',
        returns = '()',
      },
      setBullet = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, bullet: boolean | true])',
        returns = '()',
      },
      setFixedRotation = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, fixedRotation: boolean | true])',
        returns = '()',
      },
      setLinearDamping = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, damping: number])',
        returns = '()',
      },
      setLinearVelocity = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, velocityX: number [, velocityY: number]])',
        returns = '()',
      },
      setMassData = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody, mass: number [, I: number [, centerX: number [, centerY: number]]])',
        returns = '()',
      },
      setTransform = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DBody [, positionX: number [, positionY: number [, angle: number]]])',
        returns = '()',
      },
    },
  },
  MOAIBox2DDistanceJoint = {
    type = "class",
    description = "Box2D distance joint.",
    childs = {
      getDampingRatio = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DDistanceJoint)',
        returns = '(dampingRatio: number)',
      },
      getFrequency = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DDistanceJoint)',
        returns = '(frequency: number)',
      },
      getLength = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DDistanceJoint)',
        returns = '(length: number)',
      },
      setDampingRatio = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DDistanceJoint [, dampingRatio: number | 0])',
        returns = '()',
      },
      setFrequency = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DDistanceJoint [, frequency: number | 0])',
        returns = '()',
      },
      setLength = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DDistanceJoint [, length: number | 0])',
        returns = '()',
      },
    },
  },
  MOAIBox2DFixture = {
    type = "class",
    description = "Box2D fixture.",
    childs = {
      destroy = {
        type = "function",
        description = "Schedule fixture for destruction.",
        args = '(self: MOAIBox2DFixture)',
        returns = '()',
      },
      getBody = {
        type = "function",
        description = "Returns the body that owns the fixture.",
        args = '(self: MOAIBox2DFixture)',
        returns = '(body: MOAIBox2DBody)',
      },
      getFilter = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFixture)',
        returns = '(categoryBits: (number), maskBits: (number), groupIndex: (number))',
      },
      setCollisionHandler = {
        type = "function",
        description = "Sets a Lua function to call when collisions occur. The handler should accept the following parameters: ( phase, fixtureA, fixtureB, arbiter ). 'phase' will be one of the phase masks. 'fixtureA' will be the fixture receiving the collision. 'fixtureB' will be the other fixture in the collision. 'arbiter' will be the MOAIArbiter. Note that the arbiter is only good for the current collision: do not keep references to it for later use.",
        args = '(self: MOAIBox2DFixture, handler: function [, phaseMask: number [, categoryMask: number]])',
        returns = '()',
      },
      setDensity = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFixture, density: number)',
        returns = '()',
      },
      setFilter = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFixture, categoryBits: number [, maskBits: number [, groupIndex: number]])',
        returns = '()',
      },
      setFriction = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFixture, friction: number)',
        returns = '()',
      },
      setRestitution = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFixture, restitution: number)',
        returns = '()',
      },
      setSensor = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFixture [, isSensor: boolean | true])',
        returns = '()',
      },
    },
  },
  MOAIBox2DFrictionJoint = {
    type = "class",
    description = "Box2D friction joint.",
    childs = {
      getMaxForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFrictionJoint)',
        returns = '(maxForce: number)',
      },
      getMaxTorque = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFrictionJoint)',
        returns = '(maxTorque: number)',
      },
      setMaxForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFrictionJoint [, maxForce: number | 0])',
        returns = '()',
      },
      setMaxTorque = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DFrictionJoint [, maxTorque: number | 0])',
        returns = '()',
      },
    },
  },
  MOAIBox2DGearJoint = {
    type = "class",
    description = "Box2D gear joint.",
    childs = {
      getJointA = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DGearJoint)',
        returns = '(jointA: MOAIBox2DJoint)',
      },
      getJointB = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DGearJoint)',
        returns = '(jointB: MOAIBox2DJoint)',
      },
      getRatio = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DGearJoint)',
        returns = '(ratio: number)',
      },
      setRatio = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DGearJoint [, ratio: number | 0])',
        returns = '()',
      },
    },
  },
  MOAIBox2DJoint = {
    type = "class",
    description = "Box2D joint.",
    childs = {
      destroy = {
        type = "function",
        description = "Schedule joint for destruction.",
        args = '(self: MOAIBox2DJoint)',
        returns = '()',
      },
      getAnchorA = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DJoint)',
        returns = '(in: anchorX, in: anchorY)',
      },
      getAnchorB = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DJoint)',
        returns = '(in: anchorX, in: anchorY)',
      },
      getBodyA = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DJoint)',
        returns = '(body: MOAIBox2DBody)',
      },
      getBodyB = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DJoint)',
        returns = '(body: MOAIBox2DBody)',
      },
      getReactionForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DJoint)',
        returns = '(forceX: number, forceY: number)',
      },
      getReactionTorque = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DJoint)',
        returns = '(reactionTorque: number)',
      },
    },
  },
  MOAIBox2DMouseJoint = {
    type = "class",
    description = "Box2D 'mouse' joint.",
    childs = {
      getDampingRatio = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint)',
        returns = '(dampingRatio: number)',
      },
      getFrequency = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint)',
        returns = '(frequency: number)',
      },
      getMaxForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint)',
        returns = '(maxForce: number)',
      },
      getTarget = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint)',
        returns = '(x: number, y: number)',
      },
      setDampingRatio = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint [, dampingRatio: number | 0])',
        returns = '()',
      },
      setFrequency = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint [, frequency: number | 0])',
        returns = '()',
      },
      setMaxForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint [, maxForce: number | 0])',
        returns = '()',
      },
      setTarget = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DMouseJoint [, x: number | 0 [, y: number | 0]])',
        returns = '()',
      },
    },
  },
  MOAIBox2DPrismaticJoint = {
    type = "class",
    description = "Box2D prismatic joint.",
    childs = {
      getJointSpeed = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(jointSpeed: number)',
      },
      getJointTranslation = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(jointTranslation: number)',
      },
      getLowerLimit = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(lowerLimit: number)',
      },
      getMotorForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(motorForce: number)',
      },
      getMotorSpeed = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(motorSpeed: number)',
      },
      getUpperLimit = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(upperLimit: number)',
      },
      isLimitEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(limitEnabled: boolean)',
      },
      isMotorEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint)',
        returns = '(motorEnabled: boolean)',
      },
      setLimit = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint [, lower: number | 0 [, upper: number | 0]])',
        returns = '()',
      },
      setLimitEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint [, enabled: boolean | true])',
        returns = '()',
      },
      setMaxMotorForce = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint [, maxMotorForce: number | 0])',
        returns = '()',
      },
      setMotor = {
        type = "function",
        description = "See Box2D documentation. If speed is determined to be zero, the motor is disabled, unless forceEnable is set.",
        args = '(self: MOAIBox2DPrismaticJoint [, speed: number | 0 [, maxForce: number | 0 [, forceEnable: boolean | false]]])',
        returns = '()',
      },
      setMotorEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPrismaticJoint [, enabled: boolean | true])',
        returns = '()',
      },
    },
  },
  MOAIBox2DPulleyJoint = {
    type = "class",
    description = "Box2D pulley joint.",
    childs = {
      getGroundAnchorA = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPulleyJoint)',
        returns = '(x: number, y: number)',
      },
      getGroundAnchorB = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPulleyJoint)',
        returns = '(x: number, y: number)',
      },
      getLength1 = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPulleyJoint)',
        returns = '(length1: number)',
      },
      getLength2 = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPulleyJoint)',
        returns = '(length2: number)',
      },
      getRatio = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DPulleyJoint)',
        returns = '(ratio: number)',
      },
    },
  },
  MOAIBox2DRevoluteJoint = {
    type = "class",
    description = "Box2D revolute joint.",
    childs = {
      getJointAngle = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(angle: number)',
      },
      getJointSpeed = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(jointSpeed: number)',
      },
      getLowerLimit = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(lowerLimit: number)',
      },
      getMotorSpeed = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(motorSpeed: number)',
      },
      getMotorTorque = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(motorTorque: number)',
      },
      getUpperLimit = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(upperLimit: number)',
      },
      isLimitEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(limitEnabled: boolean)',
      },
      isMotorEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint)',
        returns = '(motorEnabled: boolean)',
      },
      setLimit = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint [, lower: number | 0 [, upper: number | 0]])',
        returns = '()',
      },
      setLimitEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint [, enabled: boolean | true])',
        returns = '()',
      },
      setMaxMotorTorque = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint [, maxMotorTorque: number | 0])',
        returns = '()',
      },
      setMotor = {
        type = "function",
        description = "See Box2D documentation. If speed is determined to be zero, the motor is disabled, unless forceEnable is set.",
        args = '(self: MOAIBox2DRevoluteJoint [, speed: number | 0 [, maxMotorTorque: number | 0 [, forceEnable: boolean | false]]])',
        returns = '()',
      },
      setMotorEnabled = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DRevoluteJoint, self: MOAIBox2DRevoluteJoint [, motorSpeed: number | 0 [, enabled: boolean | true]])',
        returns = '()',
      },
    },
  },
  MOAIBox2DWeldJoint = {
    type = "class",
    description = "Box2D weld joint.",
    childs = {
    },
  },
  MOAIBox2DWorld = {
    type = "class",
    description = "Box2D world.",
    childs = {
      DEBUG_DRAW_SHAPES = {
        type = "value",
        description = "",
      },
      DEBUG_DRAW_JOINTS = {
        type = "value",
        description = "",
      },
      DEBUG_DRAW_BOUNDS = {
        type = "value",
        description = "",
      },
      DEBUG_DRAW_PAIRS = {
        type = "value",
        description = "",
      },
      DEBUG_DRAW_CENTERS = {
        type = "value",
        description = "",
      },
      DEBUG_DRAW_DEFAULT = {
        type = "value",
        description = "",
      },
      addBody = {
        type = "function",
        description = "Create and add a body to the world.",
        args = '(self: MOAIBox2DWorld, type: number [, x: number [, y: number]])',
        returns = '(joint: MOAIBox2DBody)',
      },
      addDistanceJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, anchorA_X: number, anchorA_Y: number, anchorB_X: number, anchorB_Y: number [, frequencyHz: number [, dampingRatio: number [, collideConnected: number | false]]])',
        returns = '(joint: MOAIBox2DJoint)',
      },
      addFrictionJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, anchorX: number, anchorY: number [, maxForce: number [, maxTorque: number]])',
        returns = '(joint: MOAIBox2DJoint)',
      },
      addGearJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, jointA: MOAIBox2DJoint, jointB: MOAIBox2DJoint, ratio: float)',
        returns = '(joint: MOAIBox2DJoint)',
      },
      addMouseJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, targetX: number, targetY: number, maxForce: number [, frequencyHz: number [, dampingRatio: number]])',
        returns = '(joint: MOAIBox2DJoint)',
      },
      addPrismaticJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, anchorA: number, anchorB: number, axisA: number, axisB: number)',
        returns = '(joint: MOAIBox2DJoint)',
      },
      addPulleyJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, groundAnchorA_X: number, groundAnchorA_Y: number, groundAnchorB_X: number, groundAnchorB_Y: number, anchorA_X: number, anchorA_Y: number, anchorB_X: number, anchorB_Y: number, ratio: number, maxLengthA: number, maxLengthB: number)',
        returns = '(joint: MOAIBox2DJoint)',
      },
      addRevoluteJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, anchorX: number, anchorY: number)',
        returns = '(joint: MOAIBox2DJoint)',
      },
      addWeldJoint = {
        type = "function",
        description = "Create and add a joint to the world. See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, maxLength: number, self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, anchorX: number, anchorY: number [, anchorAX: number [, anchorAY: number [, anchorBX: number [, anchorBY: number]]]])',
        returns = '(joint: MOAIBox2DJoint, joint: MOAIBox2DJoint)',
      },
      getAngularSleepTolerance = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld, bodyA: MOAIBox2DBody, bodyB: MOAIBox2DBody, anchorX: number, anchorY: number, axisX: number, axisY: number, self: MOAIBox2DWorld)',
        returns = '(joint: MOAIBox2DJoint, angularSleepTolerance: number)',
      },
      getAutoClearForces = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld)',
        returns = '(autoClearForces: boolean)',
      },
      getGravity = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld)',
        returns = '(gravityX: number, gravityY: number)',
      },
      getLinearSleepTolerance = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld)',
        returns = '(linearSleepTolerance: number)',
      },
      getTimeToSleep = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld)',
        returns = '(timeToSleep: number)',
      },
      setAngularSleepTolerance = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld [, angularSleepTolerance: number | 0])',
        returns = '()',
      },
      setAutoClearForces = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld [, autoClearForces: boolean | true])',
        returns = '()',
      },
      setDebugDrawEnabled = {
        type = "function",
        description = "enable/disable debug drawing.",
        args = '(self: MOAIBox2dWorld, bEnable: number)',
        returns = '()',
      },
      setDebugDrawFlags = {
        type = "function",
        description = "Sets mask for debug drawing.",
        args = '(self: MOAIBox2DWorld [, flags: number])',
        returns = '()',
      },
      setGravity = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld [, gravityX: number | 0 [, gravityY: number | 0]])',
        returns = '()',
      },
      setIterations = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld [, velocityIteratons: number | current [, positionIterations: number | current]])',
        returns = '()',
      },
      setLinearSleepTolerance = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld [, linearSleepTolerance: number | 0])',
        returns = '()',
      },
      setTimeToSleep = {
        type = "function",
        description = "See Box2D documentation.",
        args = '(self: MOAIBox2DWorld [, timeToSleep: number | 0])',
        returns = '()',
      },
      setUnitsToMeters = {
        type = "function",
        description = "Sets a scale factor for converting game world units to Box2D meters.",
        args = '(self: MOAIBox2DWorld [, unitsToMeters: number | 1])',
        returns = '()',
      },
    },
  },
  MOAIButtonSensor = {
    type = "class",
    description = "Button sensor.",
    childs = {
      down = {
        type = "function",
        description = "Checks to see if the button was pressed during the last iteration.",
        args = '(self: MOAIButtonSensor)',
        returns = '(wasPressed: boolean)',
      },
      isDown = {
        type = "function",
        description = "Checks to see if the button is currently down.",
        args = '(self: MOAIButtonSensor)',
        returns = '(isDown: boolean)',
      },
      isUp = {
        type = "function",
        description = "Checks to see if the button is currently up.",
        args = '(self: MOAIButtonSensor)',
        returns = '(isUp: boolean)',
      },
      up = {
        type = "function",
        description = "Checks to see if the button was released during the last iteration.",
        args = '(self: MOAIButtonSensor)',
        returns = '(wasReleased: boolean)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when button events occur.",
        args = '(self: MOAIButtonSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAICamera = {
    type = "class",
    description = "Perspective or orthographic camera.",
    childs = {
      getFarPlane = {
        type = "function",
        description = "Returns the camera's far plane.",
        args = '(self: MOAICamera)',
        returns = '(far: number)',
      },
      getFieldOfView = {
        type = "function",
        description = "Returns the camera's horizontal field of view.",
        args = '(self: MOAICamera)',
        returns = '(hfov: number)',
      },
      getFocalLength = {
        type = "function",
        description = "Returns the camera's focal length given the width of the view plane.",
        args = '(self: MOAICamera, width: number)',
        returns = '(length: number)',
      },
      getNearPlane = {
        type = "function",
        description = "Returns the camera's near plane.",
        args = '(self: MOAICamera)',
        returns = '(near: number)',
      },
      setFarPlane = {
        type = "function",
        description = "Sets the camera's far plane distance.",
        args = '(self: MOAICamera [, far: number | 10000])',
        returns = '()',
      },
      setFieldOfView = {
        type = "function",
        description = "Sets the camera's horizontal field of view.",
        args = '(self: MOAICamera [, hfow: number | 60])',
        returns = '()',
      },
      setNearPlane = {
        type = "function",
        description = "Sets the camera's near plane distance.",
        args = '(self: MOAICamera [, near: number | 1])',
        returns = '()',
      },
      setOrtho = {
        type = "function",
        description = "Sets orthographic mode.",
        args = '(self: MOAICamera [, ortho: boolean | true])',
        returns = '()',
      },
    },
  },
  MOAICamera2D = {
    type = "class",
    description = "2D camera.",
    childs = {
      getFarPlane = {
        type = "function",
        description = "Returns the camera's far plane.",
        args = '(self: MOAICamera2D)',
        returns = '(far: number)',
      },
      getNearPlane = {
        type = "function",
        description = "Returns the camera's near plane.",
        args = '(self: MOAICamera2D)',
        returns = '(near: number)',
      },
      setFarPlane = {
        type = "function",
        description = "Sets the camera's far plane distance.",
        args = '(self: MOAICamera2D [, far: number | -1])',
        returns = '()',
      },
      setNearPlane = {
        type = "function",
        description = "Sets the camera's near plane distance.",
        args = '(self: MOAICamera2D [, near: number | 1])',
        returns = '()',
      },
    },
  },
  MOAICameraAnchor2D = {
    type = "class",
    description = "Attaches fitting information to a transform. Used by MOAICameraFitter2D.",
    childs = {
      setParent = {
        type = "function",
        description = "Attach the anchor to a transform.",
        args = '(self: MOAICameraAnchor2D [, parent: MOAITransformBase])',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set the dinemsions (in world units) of the anchor.",
        args = '(self: MOAICameraAnchor2D, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
    },
  },
  MOAICameraFitter2D = {
    type = "class",
    description = "Action to dynamically fit a camera transform to a set of targets given a viewport and world space constraints.",
    childs = {
      FITTING_MODE_SEEK_LOC = {
        type = "value",
        description = "",
      },
      FITTING_MODE_SEEK_SCALE = {
        type = "value",
        description = "",
      },
      FITTING_MODE_APPLY_ANCHORS = {
        type = "value",
        description = "",
      },
      FITTING_MODE_APPLY_BOUNDS = {
        type = "value",
        description = "",
      },
      FITTING_MODE_DEFAULT = {
        type = "value",
        description = "",
      },
      FITTING_MODE_MASK = {
        type = "value",
        description = "",
      },
      clearAnchors = {
        type = "function",
        description = "Remove all camera anchors from the fitter.",
        args = '(self: MOAICameraFitter2D)',
        returns = '()',
      },
      clearFitMode = {
        type = "function",
        description = "Clears bits in the fitting mask.",
        args = '(self: MOAICameraFitter2D [, mask: number | FITTING_MODE_MASK])',
        returns = '()',
      },
      getFitDistance = {
        type = "function",
        description = "Returns the distance between the camera's current x, y, scale and the target x, y, scale. As the camera approaches its target, the distance approaches 0. Check the value returned by this function against a small epsilon value.",
        args = '(self: MOAICameraFitter2D)',
        returns = '(distance: number)',
      },
      getFitLoc = {
        type = "function",
        description = "Get the fitter location.",
        args = '(self: MOAICameraFitter2D)',
        returns = '(x: number, y: number)',
      },
      getFitMode = {
        type = "function",
        description = "Gets bits in the fitting mask.",
        args = '(self: MOAICameraFitter2D)',
        returns = '(mask: number)',
      },
      getFitScale = {
        type = "function",
        description = "Returns the fit scale",
        args = '(self: MOAICameraFitter2D)',
        returns = '(scale: number)',
      },
      insertAnchor = {
        type = "function",
        description = "Add an anchor to the fitter.",
        args = '(self: MOAICameraFitter2D, self: MOAICameraFitter2D, self: MOAICameraFitter2D, anchor: MOAICameraAnchor2D)',
        returns = '(x: number, y: number, scale: number)',
      },
      removeAnchor = {
        type = "function",
        description = "Remove an anchor from the fitter.",
        args = '(self: MOAICameraFitter2D, anchor: MOAICameraAnchor2D)',
        returns = '()',
      },
      setBounds = {
        type = "function",
        description = "Sets or clears the world bounds of the fitter. The camera will not move outside of the fitter's bounds.",
        args = '(self: MOAICameraFitter2D, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setCamera = {
        type = "function",
        description = "Set a MOAITransform for the fitter to use as a camera. The fitter will dynamically change the location and scale of the camera to keep all of the anchors on the screen.",
        args = '(self: MOAICameraFitter2D [, camera: MOAITransform | nil])',
        returns = '()',
      },
      setDamper = {
        type = "function",
        description = "Set's the fitter's damper coefficient. This is a scalar applied to the difference between the camera transform's location and the fitter's target location every frame. The smaller the coefficient, the tighter the fit will be. A value of '0' will not dampen at all; a value of '1' will never move the camera.",
        args = '(self: MOAICameraFitter2D [, damper: number | 0])',
        returns = '()',
      },
      setFitLoc = {
        type = "function",
        description = "Set the fitter's location.",
        args = '(self: MOAICameraFitter2D [, x: number | 0 [, y: number | 0 [, snap: boolean | false]]])',
        returns = '()',
      },
      setFitMode = {
        type = "function",
        description = "Sets bits in the fitting mask.",
        args = '(self: MOAICameraFitter2D [, mask: number | FITTING_MODE_DEFAULT])',
        returns = '()',
      },
      setFitScale = {
        type = "function",
        description = "Set the fitter's scale.",
        args = '(self: MOAICameraFitter2D [, scale: number | 1 [, snap: boolean | false]])',
        returns = '()',
      },
      setMin = {
        type = "function",
        description = "Set the minimum number of world units to be displayed by the camera along either axis.",
        args = '(self: MOAICameraFitter2D [, min: number | 0])',
        returns = '()',
      },
      setViewport = {
        type = "function",
        description = "Set the viewport to be used for fitting.",
        args = '(self: MOAICameraFitter2D [, viewport: MOAIViewport | nil])',
        returns = '()',
      },
      snapToTarget = {
        type = "function",
        description = "Snap the camera to the target fitting.",
        args = '(self: MOAICameraFitter2D)',
        returns = '()',
      },
    },
  },
  MOAIColor = {
    type = "class",
    description = "Color vector with animation helper methods.",
    childs = {
      moveColor = {
        type = "function",
        description = "Animate the color by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAIColor, rDelta: number, gDelta: number, bDelta: number, aDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekColor = {
        type = "function",
        description = "Animate the color by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAIColor, rGoal: number, gGoal: number, bGoal: number, aGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      setColor = {
        type = "function",
        description = "Initialize the color.",
        args = '(self: MOAIColor, r: number, g: number, b: number [, a: number | 1])',
        returns = '()',
      },
      setParent = {
        type = "function",
        description = "This method has been deprecated. Use MOAINode setAttrLink instead.",
        args = '(self: MOAIColor [, parent: MOAINode | nil])',
        returns = '()',
      },
    },
  },
  MOAICompassSensor = {
    type = "class",
    description = "Device heading sensor.",
    childs = {
      getHeading = {
        type = "function",
        description = "Returns the current heading according to the built-in compass.",
        args = '(self: MOAICompassSensor)',
        returns = '(heading: number)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when the heading changes.",
        args = '(self: MOAICompassSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAICoroutine = {
    type = "class",
    description = "Binds a Lua coroutine to a MOAIAction.",
    childs = {
      blockOnAction = {
        type = "function",
        description = "Skip updating current thread until the specified action is no longer busy. A little more efficient that spinlocking from Lua.",
        args = '(blocker: MOAIAction)',
        returns = '()',
      },
      currentThread = {
        type = "function",
        description = "Returns the currently running thread (if any).",
        args = '()',
        returns = '(currentThread: MOAICoroutine)',
      },
      run = {
        type = "function",
        description = "Starts a thread with a function and passes parameters to it.",
        args = '(self: MOAICoroutine, threadFunc: function, ...)',
        returns = '()',
      },
    },
  },
  MOAICp = {
    type = "class",
    description = "Singleton for Chipmunk global configuration.",
    childs = {
      getBiasCoefficient = {
        type = "function",
        description = "Returns the current bias coefficient.",
        args = '()',
        returns = '(bias: number)',
      },
    },
  },
  MOAICpArbiter = {
    type = "class",
    description = "Chipmunk Arbiter.",
    childs = {
      countContacts = {
        type = "function",
        description = "Returns the number of contacts occurring with this arbiter.",
        args = '(self: MOAICpArbiter)',
        returns = '(count: number)',
      },
      getContactDepth = {
        type = "function",
        description = "Returns the depth of a contact point between two objects.",
        args = '(self: MOAICpArbiter, id: number)',
        returns = '(depth: number)',
      },
      getContactNormal = {
        type = "function",
        description = "Returns the normal of a contact point between two objects.",
        args = '(self: MOAICpArbiter, id: number)',
        returns = '(x: boolean, y: boolean)',
      },
      getContactPoint = {
        type = "function",
        description = "Returns the position of a contact point between two objects.",
        args = '(self: MOAICpArbiter, id: number)',
        returns = '(x: boolean, y: boolean)',
      },
      getTotalImpulse = {
        type = "function",
        description = "Returns the total impulse of a contact point between two objects.",
        args = '(self: MOAICpArbiter)',
        returns = '(x: boolean, y: boolean)',
      },
      getTotalImpulseWithFriction = {
        type = "function",
        description = "Returns the total impulse of a contact point between two objects, also including frictional forces.",
        args = '(self: MOAICpArbiter)',
        returns = '(x: boolean, y: boolean)',
      },
      isFirstContact = {
        type = "function",
        description = "Returns whether this is the first time that these two objects have contacted.",
        args = '(self: MOAICpArbiter)',
        returns = '(first: boolean)',
      },
    },
  },
  MOAICpBody = {
    type = "class",
    description = "Chipmunk Body.",
    childs = {
      NONE = {
        type = "value",
        description = "",
      },
      REMOVE_BODY = {
        type = "value",
        description = "",
      },
      REMOVE_BODY_AND_SHAPES = {
        type = "value",
        description = "",
      },
      activate = {
        type = "function",
        description = "Activates a body after it has been put to sleep (physics will now be processed for this body again).",
        args = '(self: MOAICpBody)',
        returns = '()',
      },
      addCircle = {
        type = "function",
        description = "Adds a circle to the body.",
        args = '(self: MOAICpBody, radius: number, x: number, y: number)',
        returns = '(circle: MOAICpShape)',
      },
      addPolygon = {
        type = "function",
        description = "Adds a polygon to the body.",
        args = '(self: MOAICpBody, polygon: table)',
        returns = '(polygon: MOAICpShape)',
      },
      addRect = {
        type = "function",
        description = "Adds a rectangle to the body.",
        args = '(self: MOAICpBody, x1: number, y1: number, x2: number, y2: number)',
        returns = '(rectangle: MOAICpShape)',
      },
      addSegment = {
        type = "function",
        description = "Adds a segment to the body.",
        args = '(self: MOAICpBody, x1: number, y1: number, x2: number, y2: number [, radius: number])',
        returns = '(segment: MOAICpShape)',
      },
      applyForce = {
        type = "function",
        description = "Applies force to the body, taking into account any existing forces being applied.",
        args = '(self: MOAICpBody, fx: number, fy: number, rx: number, ry: number)',
        returns = '()',
      },
      applyImpulse = {
        type = "function",
        description = "Applies impulse to the body, taking into account any existing impulses being applied.",
        args = '(self: MOAICpBody, jx: number, jy: number, rx: number, ry: number)',
        returns = '()',
      },
      getAngle = {
        type = "function",
        description = "Returns the angle of the body.",
        args = '(self: MOAICpBody)',
        returns = '(angle: number)',
      },
      getAngVel = {
        type = "function",
        description = "Returns the angular velocity of the body.",
        args = '(self: MOAICpBody)',
        returns = '(angle: number)',
      },
      getForce = {
        type = "function",
        description = "Returns the force of the body.",
        args = '(self: MOAICpBody)',
        returns = '(x: number, y: number)',
      },
      getMass = {
        type = "function",
        description = "Returns the mass of the body.",
        args = '(self: MOAICpBody)',
        returns = '(mass: number)',
      },
      getMoment = {
        type = "function",
        description = "Returns the moment of the body.",
        args = '(self: MOAICpBody)',
        returns = '(moment: number)',
      },
      getPos = {
        type = "function",
        description = "Returns the position of the body.",
        args = '(self: MOAICpBody)',
        returns = '(x: number, y: number)',
      },
      getRot = {
        type = "function",
        description = "Returns the rotation of the body.",
        args = '(self: MOAICpBody)',
        returns = '(x: number, y: number)',
      },
      getTorque = {
        type = "function",
        description = "Returns the torque of the body.",
        args = '(self: MOAICpBody)',
        returns = '(torque: number)',
      },
      getVel = {
        type = "function",
        description = "Returns the velocity of the body.",
        args = '(self: MOAICpBody)',
        returns = '(x: number, y: number)',
      },
      isSleeping = {
        type = "function",
        description = "Returns whether the body is currently sleeping.",
        args = '(self: MOAICpBody)',
        returns = '(sleeping: boolean)',
      },
      isStatic = {
        type = "function",
        description = "Returns whether the body is static.",
        args = '(self: MOAICpBody)',
        returns = '(static: boolean)',
      },
      isRogue = {
        type = "function",
        description = "Returns whether the body is not yet currently associated with a space.",
        args = '(self: MOAICpBody)',
        returns = '(static: boolean)',
      },
      localToWorld = {
        type = "function",
        description = "Converts the relative position to an absolute position based on position of the object being (0, 0) for the relative position.",
        args = '(self: MOAICpBody, rx: number, ry: number)',
        returns = '(ax: number, ay: number)',
      },
      new = {
        type = "function",
        description = "Creates a new body with the specified mass and moment.",
        args = '(m: number, i: number)',
        returns = '(body: MOAICpBody)',
      },
      newStatic = {
        type = "function",
        description = "Creates a new static body.",
        args = '()',
        returns = '(body: MOAICpBody)',
      },
      resetForces = {
        type = "function",
        description = "Resets all forces on the body.",
        args = '(self: MOAICpBody)',
        returns = '()',
      },
      setAngle = {
        type = "function",
        description = "Sets the angle of the body.",
        args = '(self: MOAICpBody, angle: number)',
        returns = '()',
      },
      setAngVel = {
        type = "function",
        description = "Sets the angular velocity of the body.",
        args = '(self: MOAICpBody, angvel: number)',
        returns = '()',
      },
      setForce = {
        type = "function",
        description = "Sets the force on the body.",
        args = '(self: MOAICpBody, forcex: number, forcey: number)',
        returns = '()',
      },
      setMass = {
        type = "function",
        description = "Sets the mass of the body.",
        args = '(self: MOAICpBody, mass: number)',
        returns = '()',
      },
      setMoment = {
        type = "function",
        description = "Sets the moment of the body.",
        args = '(self: MOAICpBody, moment: number)',
        returns = '()',
      },
      setPos = {
        type = "function",
        description = "Sets the position of the body.",
        args = '(self: MOAICpBody, x: number, y: number)',
        returns = '()',
      },
      setRemoveFlag = {
        type = "function",
        description = "Sets the removal flag on the body.",
        args = '(self: MOAICpBody, flag: number)',
        returns = '()',
      },
      setTorque = {
        type = "function",
        description = "Sets the torque of the body.",
        args = '(self: MOAICpBody, torque: number)',
        returns = '()',
      },
      setVel = {
        type = "function",
        description = "Sets the velocity of the body.",
        args = '(self: MOAICpBody, x: number, y: number)',
        returns = '()',
      },
      sleep = {
        type = "function",
        description = "Puts the body to sleep (physics will no longer be processed for it until it is activated).",
        args = '(self: MOAICpBody)',
        returns = '()',
      },
      sleepWithGroup = {
        type = "function",
        description = "Forces an object to sleep. Pass in another sleeping body to add the object to the sleeping body's existing group.",
        args = '(self: MOAICpBody, group: MOAICpBody)',
        returns = '()',
      },
      worldToLocal = {
        type = "function",
        description = "Converts the absolute position to a relative position based on position of the object being (0, 0) for the relative position.",
        args = '(self: MOAICpBody, ax: number, ay: number)',
        returns = '(rx: number, ry: number)',
      },
    },
  },
  MOAICpConstraint = {
    type = "class",
    description = "Chipmunk Constraint.",
    childs = {
      getBiasCoef = {
        type = "function",
        description = "Returns the current bias coefficient.",
        args = '(self: MOAICpConstraint)',
        returns = '(bias: number)',
      },
      getMaxBias = {
        type = "function",
        description = "Returns the maximum bias coefficient.",
        args = '(self: MOAICpConstraint)',
        returns = '(bias: number)',
      },
      getMaxForce = {
        type = "function",
        description = "Returns the maximum force allowed.",
        args = '(self: MOAICpConstraint)',
        returns = '(bias: number)',
      },
      newDampedRotarySpring = {
        type = "function",
        description = "Creates a new damped rotary string between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, restAngle: number, stiffness: number, damping: number)',
        returns = '(spring: MOAICpConstraint)',
      },
      newDampedSpring = {
        type = "function",
        description = "Creates a new damped string between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, x1: number, y1: number, x2: number, y2: number, restAngle: number, stiffness: number, damping: number)',
        returns = '(spring: MOAICpConstraint)',
      },
      newGearJoint = {
        type = "function",
        description = "Creates a new gear joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, phase: number, ratio: number)',
        returns = '(gear: MOAICpConstraint)',
      },
      newGrooveJoint = {
        type = "function",
        description = "Creates a new groove joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, gx1: number, gy1: number, gx2: number, gy2: number, ax: number, ay: number)',
        returns = '(groove: MOAICpConstraint)',
      },
      newPinJoint = {
        type = "function",
        description = "Creates a new pin joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, ax1: number, ay1: number, ax2: number, ay2: number)',
        returns = '(pin: MOAICpConstraint)',
      },
      newPivotJoint = {
        type = "function",
        description = "Creates a new pivot joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, x: number, y: number [, ax: number [, ay: number]])',
        returns = '(pivot: MOAICpConstraint)',
      },
      newRatchetJoint = {
        type = "function",
        description = "Creates a new ratchet joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, phase: number, ratchet: number)',
        returns = '(ratchet: MOAICpConstraint)',
      },
      newRotaryLimitJoint = {
        type = "function",
        description = "Creates a new rotary limit joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, min: number, max: number)',
        returns = '(limit: MOAICpConstraint)',
      },
      newSimpleMotor = {
        type = "function",
        description = "Creates a new simple motor joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, rate: number)',
        returns = '(motor: MOAICpConstraint)',
      },
      newSlideJoint = {
        type = "function",
        description = "Creates a new slide joint between the two specified bodies.",
        args = '(first: MOAICpShape, second: MOAICpShape, ax1: number, ay1: number, ax2: number, ay2: number, min: number, max: number)',
        returns = '(motor: MOAICpConstraint)',
      },
      setBiasCoef = {
        type = "function",
        description = "Sets the current bias coefficient.",
        args = '(self: MOAICpConstraint, bias: number)',
        returns = '()',
      },
      setMaxBias = {
        type = "function",
        description = "Sets the maximum bias coefficient.",
        args = '(self: MOAICpConstraint, bias: number)',
        returns = '()',
      },
      setMaxForce = {
        type = "function",
        description = "Sets the maximum force allowed.",
        args = '(self: MOAICpConstraint, bias: number)',
        returns = '()',
      },
    },
  },
  MOAICpShape = {
    type = "class",
    description = "Chipmunk Shape.",
    childs = {
      areaForCircle = {
        type = "function",
        description = "Returns the area for a ring or circle.",
        args = '(radius: number)',
        returns = '(area: number)',
      },
      areaForCircle = {
        type = "function",
        description = "Returns the area for a polygon.",
        args = '(vertices: table)',
        returns = '(area: number)',
      },
      areaForRect = {
        type = "function",
        description = "Returns the area for the specified rectangle.",
        args = '(x1: number, y1: number, x2: number, y2: number)',
        returns = '(area: number)',
      },
      areaForSegment = {
        type = "function",
        description = "Returns the area for the specified segment.",
        args = '(x1: number, y1: number, x2: number, y2: number, r: number)',
        returns = '(area: number)',
      },
      getBody = {
        type = "function",
        description = "Returns the current body for the shape.",
        args = '(self: MOAICpShape)',
        returns = '(body: MOAICpBody)',
      },
      getElasticity = {
        type = "function",
        description = "Returns the current elasticity.",
        args = '(self: MOAICpShape)',
        returns = '(elasticity: number)',
      },
      getFriction = {
        type = "function",
        description = "Returns the current friction.",
        args = '(self: MOAICpShape)',
        returns = '(friction: number)',
      },
      getGroup = {
        type = "function",
        description = "Returns the current group ID.",
        args = '(self: MOAICpShape)',
        returns = '(group: number)',
      },
      getLayers = {
        type = "function",
        description = "Returns the current layer ID.",
        args = '(self: MOAICpShape)',
        returns = '(layer: number)',
      },
      getSurfaceVel = {
        type = "function",
        description = "Returns the current surface velocity?",
        args = '(self: MOAICpShape)',
        returns = '(x: number, y: number)',
      },
      getType = {
        type = "function",
        description = "Returns the current collision type.",
        args = '(self: MOAICpShape)',
        returns = '(type: number)',
      },
      inside = {
        type = "function",
        description = "Returns whether the specified point is inside the shape.",
        args = '(self: MOAICpShape, x: number, y: number)',
        returns = '(inside: boolean)',
      },
      isSensor = {
        type = "function",
        description = "Returns whether the current shape is a sensor.",
        args = '(self: MOAICpShape)',
        returns = '(sensor: boolean)',
      },
      momentForCircle = {
        type = "function",
        description = "Return the moment of inertia for the circle.",
        args = '(m: number, r2: number, ox: number, oy: number [, r1: number])',
        returns = '(moment: number)',
      },
      momentForPolygon = {
        type = "function",
        description = "Returns the moment of intertia for the polygon.",
        args = '(m: number, polygon: table)',
        returns = '(moment: number)',
      },
      momentForRect = {
        type = "function",
        description = "Returns the moment of intertia for the rect.",
        args = '(m: number, x1: number, y1: number, x2: number, y2: number)',
        returns = '(moment: number)',
      },
      momentForSegment = {
        type = "function",
        description = "Returns the moment of intertia for the segment.",
        args = '(m: number, x1: number, y1: number, x2: number, y2: number)',
        returns = '(moment: number)',
      },
      setElasticity = {
        type = "function",
        description = "Sets the current elasticity.",
        args = '(self: MOAICpShape, elasticity: number)',
        returns = '()',
      },
      setFriction = {
        type = "function",
        description = "Sets the current friction.",
        args = '(self: MOAICpShape, friction: number)',
        returns = '()',
      },
      setGroup = {
        type = "function",
        description = "Sets the current group ID.",
        args = '(self: MOAICpShape, group: number)',
        returns = '()',
      },
      setIsSensor = {
        type = "function",
        description = "Sets whether this shape is a sensor.",
        args = '(self: MOAICpShape, sensor: boolean)',
        returns = '()',
      },
      setLayers = {
        type = "function",
        description = "Sets the current layer ID.",
        args = '(self: MOAICpShape, layer: number)',
        returns = '()',
      },
      setSurfaceVel = {
        type = "function",
        description = "Sets the current surface velocity.",
        args = '(self: MOAICpShape, x: number, y: number)',
        returns = '()',
      },
      setType = {
        type = "function",
        description = "Sets the current collision type.",
        args = '(self: MOAICpShape, type: number)',
        returns = '()',
      },
    },
  },
  MOAICpSpace = {
    type = "class",
    description = "Chipmunk Space.",
    childs = {
      activateShapesTouchingShape = {
        type = "function",
        description = "Activates shapes that are currently touching the specified shape.",
        args = '(self: MOAICpSpace, shape: MOAICpShape)',
        returns = '()',
      },
      getDamping = {
        type = "function",
        description = "Returns the current damping in the space.",
        args = '(self: MOAICpSpace)',
        returns = '(damping: number)',
      },
      getGravity = {
        type = "function",
        description = "Returns the current gravity as two return values (x grav, y grav).",
        args = '(self: MOAICpSpace)',
        returns = '(xGrav: number, yGrav: number)',
      },
      getIdleSpeedThreshold = {
        type = "function",
        description = "Returns the speed threshold which indicates whether a body is idle (less than or equal to threshold) or in motion (greater than threshold).",
        args = '(self: MOAICpSpace)',
        returns = '(idleThreshold: number)',
      },
      getIterations = {
        type = "function",
        description = "Returns the number of iterations the space is configured to perform.",
        args = '(self: MOAICpSpace)',
        returns = '(iterations: number)',
      },
      getSleepTimeThreshold = {
        type = "function",
        description = "Returns the sleep time threshold.",
        args = '(self: MOAICpSpace)',
        returns = '(sleepTimeThreshold: number)',
      },
      getStaticBody = {
        type = "function",
        description = "Returns the static body associated with this space.",
        args = '(self: MOAICpSpace)',
        returns = '(staticBody: MOAICpBody)',
      },
      insertProp = {
        type = "function",
        description = "Inserts a new prop into the world (can be used as a body, joint, etc.)",
        args = '(self: MOAICpSpace, prop: MOAICpPrim)',
        returns = '()',
      },
      rehashShape = {
        type = "function",
        description = "Updates the shape in the spatial hash.",
        args = '(self: MOAICpSpace)',
        returns = '()',
      },
      rehashStatic = {
        type = "function",
        description = "Updates the static shapes in the spatial hash.",
        args = '(self: MOAICpSpace)',
        returns = '()',
      },
      removeProp = {
        type = "function",
        description = "Removes a prop (body, joint, etc.) from the space.",
        args = '(self: MOAICpSpace, prop: MOAICpPrim)',
        returns = '()',
      },
      resizeActiveHash = {
        type = "function",
        description = "Sets the dimenstions of the active object hash.",
        args = '(self: MOAICpSpace, dim: number, count: number)',
        returns = '()',
      },
      resizeStaticHash = {
        type = "function",
        description = "Sets the dimenstions of the static object hash.",
        args = '(self: MOAICpSpace, dim: number, count: number)',
        returns = '()',
      },
      setCollisionHandler = {
        type = "function",
        description = "Sets a function to handle the specific collision type on this object. If nil is passed as the handler, the collision handler is unset.",
        args = '(self: MOAICpSpace, collisionTypeA: number, collisionTypeB: number, mask: number, handler: function)',
        returns = '()',
      },
      setDamping = {
        type = "function",
        description = "Sets the current damping in the space.",
        args = '(self: MOAICpSpace, damping: number)',
        returns = '()',
      },
      setGravity = {
        type = "function",
        description = "Sets the current gravity in the space.",
        args = '(self: MOAICpSpace, xGrav: number, yGrav: number)',
        returns = '()',
      },
      setIdleSpeedThreshold = {
        type = "function",
        description = "Sets the speed threshold which indicates whether a body is idle (less than or equal to threshold) or in motion (greater than threshold).",
        args = '(self: MOAICpSpace, threshold: number)',
        returns = '()',
      },
      setIterations = {
        type = "function",
        description = "Sets the number of iterations performed each simulation step.",
        args = '(self: MOAICpSpace, iterations: number)',
        returns = '()',
      },
      setSleepTimeThreshold = {
        type = "function",
        description = "Sets the sleep time threshold. This is the amount of time it takes bodies at rest to fall asleep.",
        args = '(self: MOAICpSpace, threshold: number)',
        returns = '()',
      },
      shapeForPoint = {
        type = "function",
        description = "Retrieves a shape located at the specified X and Y position, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).",
        args = '(self: MOAICpSpace, x: number, y: number [, layers: number [, group: number]])',
        returns = '(shape: MOAICpShape)',
      },
      shapeForSegment = {
        type = "function",
        description = "Retrieves a shape that crosses the segment specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).",
        args = '(self: MOAICpSpace, x1: number, y1: number, x2: number, y2: number [, layers: number [, group: number]])',
        returns = '(shape: MOAICpShape)',
      },
      shapeListForPoint = {
        type = "function",
        description = "Retrieves a list of shaps that overlap the point specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).",
        args = '(self: MOAICpSpace, x: number, y: number [, layers: number [, group: number]])',
        returns = '(shapes: MOAICpShape)',
      },
      shapeListForRect = {
        type = "function",
        description = "Retrieves a list of shaps that overlap the rect specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).",
        args = '(self: MOAICpSpace, xMin: number, yMin: number, xMax: number, yMax: number [, layers: number [, group: number]])',
        returns = '(shapes: MOAICpShape)',
      },
      shapeListForSegment = {
        type = "function",
        description = "Retrieves a list of shaps that overlap the segment specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).",
        args = '(self: MOAICpSpace, x1: number, y1: number, x2: number, y2: number [, layers: number [, group: number]])',
        returns = '(shapes: MOAICpShape)',
      },
    },
  },
  MOAIDataBuffer = {
    type = "class",
    description = "Buffer for loading and holding data. Data operations may be performed without additional penalty of marshalling buffers between Lua and C.",
    childs = {
      base64Decode = {
        type = "function",
        description = "If a string is provided, decodes it as a base64 encoded string. Otherwise, decodes the current data stored in this object as a base64 encoded sequence of characters.",
        args = '([self: MOAIDataBuffer [, data: string]])',
        returns = '(output: string)',
      },
      base64Encode = {
        type = "function",
        description = "If a string is provided, encodes it in base64. Otherwise, encodes the current data stored in this object as a base64 encoded sequence of characters.",
        args = '([self: MOAIDataBuffer [, data: string]])',
        returns = '(output: string)',
      },
      deflate = {
        type = "function",
        description = "Compresses the string or the current data stored in this object using the DEFLATE algorithm.",
        args = '(level: number, windowBits: number [, self: MOAIDataBuffer [, data: string]])',
        returns = '(output: string)',
      },
      getSize = {
        type = "function",
        description = "Returns the number of bytes in this data buffer object.",
        args = '(self: MOAIDataBuffer)',
        returns = '(size: number)',
      },
      getString = {
        type = "function",
        description = "Returns the contents of the data buffer object as a string value.",
        args = '(self: MOAIDataBuffer)',
        returns = '(data: string)',
      },
      inflate = {
        type = "function",
        description = "Decompresses the string or the current data stored in this object using the DEFLATE algorithm.",
        args = '(windowBits: number [, self: MOAIDataBuffer [, data: string]])',
        returns = '(output: string)',
      },
      load = {
        type = "function",
        description = "Copies the data from the given file into this object. This method is a synchronous operation and will block until the file is loaded.",
        args = '(self: MOAIDataBuffer, filename: string)',
        returns = '(success: boolean)',
      },
      loadAsync = {
        type = "function",
        description = "Asynchronously copies the data from the given file into this object. This method is an asynchronous operation and will return immediately; the callback for completion should be set using setCallback.",
        args = '(self: MOAIDataBuffer, filename: string)',
        returns = '(task: MOAIDataIOAction)',
      },
      save = {
        type = "function",
        description = "Saves the data in this object to the given file. This method is a synchronous operation and will block until the data is saved.",
        args = '(self: MOAIDataBuffer, filename: string)',
        returns = '(success: boolean)',
      },
      saveAsync = {
        type = "function",
        description = "Asynchronously saves the data in this object to the given file. This method is an asynchronous operation and will return immediately; the callback for completion should be set using setCallback.",
        args = '(self: MOAIDataBuffer, filename: string)',
        returns = '(task: MOAIDataIOAction)',
      },
      setString = {
        type = "function",
        description = "Replaces the contents of this object with the string specified.",
        args = '(self: MOAIDataBuffer, data: string)',
        returns = '()',
      },
      toCppHeader = {
        type = "function",
        description = "Convert data to CPP header file.",
        args = '(data: string, name: string [, columns: number | 12])',
        returns = '(output: string)',
      },
    },
  },
  MOAIDataBufferStream = {
    type = "class",
    description = "MOAIDataBufferStream locks an associated MOAIDataBuffer for reading and writing.",
    childs = {
      close = {
        type = "function",
        description = "Disassociates and unlocks the stream's MOAIDataBuffer.",
        args = '(self: MOAIDataBufferStream)',
        returns = '()',
      },
      open = {
        type = "function",
        description = "Associate the stream with a MOAIDataBuffer. Note that the MOAIDataBuffer will be locked with a mutex while it is open thus blocking any asynchronous operations.",
        args = '(self: MOAIDataBufferStream, buffer: MOAIDataBuffer)',
        returns = '(success: boolean)',
      },
    },
  },
  MOAIDataIOAction = {
    type = "class",
    description = "Action for asynchronously loading and saving data.",
    childs = {
      setCallback = {
        type = "function",
        description = "Sets the callback to be used when the asynchronous data IO operation completes.",
        args = '(self: MOAIDataIOAction, callback: function)',
        returns = '()',
      },
    },
  },
  MOAIDebugLines = {
    type = "class",
    description = "Singleton for managing rendering of world space debug vectors.",
    childs = {
      PARTITION_CELLS = {
        type = "value",
        description = "",
      },
      PARTITION_PADDED_CELLS = {
        type = "value",
        description = "",
      },
      PROP_MODEL_BOUNDS = {
        type = "value",
        description = "",
      },
      PROP_WORLD_BOUNDS = {
        type = "value",
        description = "",
      },
      TEXT_BOX = {
        type = "value",
        description = "",
      },
      TEXT_BOX_BASELINES = {
        type = "value",
        description = "",
      },
      TEXT_BOX_LAYOUT = {
        type = "value",
        description = "",
      },
      setStyle = {
        type = "function",
        description = "Sets the particulars of a given debug line style.",
        args = '(styleID: number [, size: number | 1 [, r: number | 1 [, g: number | 1 [, b: number | 1 [, a: number | 1]]]]])',
        returns = '()',
      },
      showStyle = {
        type = "function",
        description = "Enables of disables drawing of a given debug line style.",
        args = '(styleID: number [, show: boolean | true])',
        returns = '()',
      },
    },
  },
  MOAIDeck = {
    type = "class",
    description = "Base class for decks.",
    childs = {
      setBoundsDeck = {
        type = "function",
        description = "Set or clear the bounds override deck.",
        args = '(self: MOAIDeck [, boundsDeck: MOAIBoundsDeck])',
        returns = '()',
      },
      setShader = {
        type = "function",
        description = "Set the shader to use if neither the deck item nor the prop specifies a shader.",
        args = '(self: MOAIDeck, shader: MOAIShader)',
        returns = '()',
      },
      setTexture = {
        type = "function",
        description = "Set or load a texture for this deck.",
        args = '(self: MOAIDeck, texture: variant [, transform: number])',
        returns = '(texture: MOAIGfxState)',
      },
    },
  },
  MOAIDeckRemapper = {
    type = "class",
    description = "Remap deck indices. Most useful for controlling animated tiles in tilemaps. All indices are exposed as attributes that may be connected by setAttrLink or driven using MOAIAnim or MOAIAnimCurve.",
    childs = {
      reserve = {
        type = "function",
        description = "The total number of indices to remap. Index remaps will be initialized from 1 to N.",
        args = '(self: MOAIDeckRemapper [, size: number | 0])',
        returns = '()',
      },
      setBase = {
        type = "function",
        description = "Set the base offset for the range of indices to remap. Used when remapping only a portion of the indices in the original deck.",
        args = '(self: MOAIDeckRemapper [, base: number | 0])',
        returns = '()',
      },
      setRemap = {
        type = "function",
        description = "Remap a single index to a new value.",
        args = '(self: MOAIDeckRemapper, index: number [, remap: number | index])',
        returns = '()',
      },
    },
  },
  MOAIDraw = {
    type = "class",
    description = "Singleton for performing immediate mode drawing operations. See MOAIScriptDeck.",
    childs = {
      drawBoxOutline = {
        type = "function",
        description = "Draw a box outline.",
        args = '(x0: number, y0: number, z0: number, x1: number, y1: number, z1: number)',
        returns = '()',
      },
      drawCircle = {
        type = "function",
        description = "Draw a circle.",
        args = '(x: number, y: number, r: number, steps: number)',
        returns = '()',
      },
      drawEllipse = {
        type = "function",
        description = "Draw an ellipse.",
        args = '(x: number, y: number, xRad: number, yRad: number, steps: number)',
        returns = '()',
      },
      drawLine = {
        type = "function",
        description = "Draw a line.",
        args = '(...)',
        returns = '()',
      },
      drawPoints = {
        type = "function",
        description = "Draw a list of points.",
        args = '(...)',
        returns = '()',
      },
      drawRay = {
        type = "function",
        description = "Draw a ray.",
        args = '(x: number, y: number, dx: number, dy: number)',
        returns = '()',
      },
      drawRect = {
        type = "function",
        description = "Draw a rectangle.",
        args = '(x0: number, y0: number, x1: number, y1: number)',
        returns = '()',
      },
      fillCircle = {
        type = "function",
        description = "Draw a filled circle.",
        args = '(x: number, y: number, r: number, steps: number)',
        returns = '()',
      },
      fillEllipse = {
        type = "function",
        description = "Draw a filled ellipse.",
        args = '(x: number, y: number, xRad: number, yRad: number, steps: number)',
        returns = '()',
      },
      fillFan = {
        type = "function",
        description = "Draw a filled fan.",
        args = '(...)',
        returns = '()',
      },
      fillRect = {
        type = "function",
        description = "Draw a filled rectangle.",
        args = '(x0: number, y0: number, x1: number, y1: number)',
        returns = '()',
      },
    },
  },
  MOAIEaseDriver = {
    type = "class",
    description = "Action that applies simple ease curves to node attributes.",
    childs = {
      reserveLinks = {
        type = "function",
        description = "Reserve links.",
        args = '(self: MOAIEaseDriver, nLinks: number)',
        returns = '()',
      },
      setLink = {
        type = "function",
        description = "Set the ease for a target node attribute.",
        args = '(self: MOAIEaseDriver, idx: number, target: MOAINode, attrID: number [, value: number [, mode: number]])',
        returns = '()',
      },
    },
  },
  MOAIEaseType = {
    type = "class",
    description = "Namespace to hold ease modes. Moai ease in/out has opposite meaning of Flash ease in/out.",
    childs = {
      EASE_IN = {
        type = "value",
        description = "Quartic ease in - Fast start then slow when approaching value; ease into position. ",
      },
      EASE_OUT = {
        type = "value",
        description = "Quartic ease out - Slow start then fast when approaching value; ease out of position.",
      },
      FLAT = {
        type = "value",
        description = "Stepped change - Maintain original value until end of ease.",
      },
      LINEAR = {
        type = "value",
        description = "Linear interpolation.",
      },
      SHARP_EASE_IN = {
        type = "value",
        description = "Octic ease in.",
      },
      SHARP_EASE_OUT = {
        type = "value",
        description = "Octic ease out.",
      },
      SHARP_SMOOTH = {
        type = "value",
        description = "Octic smooth.",
      },
      SMOOTH = {
        type = "value",
        description = "Quartic ease out then ease in.",
      },
      SOFT_EASE_IN = {
        type = "value",
        description = "Quadratic ease in.",
      },
      SOFT_EASE_OUT = {
        type = "value",
        description = "Quadratic ease out.",
      },
      SOFT_SMOOTH = {
        type = "value",
        description = "Quadratic smooth.",
      },
    },
  },
  MOAIEnvironment = {
    type = "class",
    description = "Table of key/value pairs containing information about the current environment. Also contains the generateGUID (), which will move to MOAIUnique in a future release.</p>",
    childs = {
      CONNECTION_TYPE_NONE = {
        type = "value",
        description = "Signifies that there is no active connection",
      },
      CONNECTION_TYPE_WIFI = {
        type = "value",
        description = "Signifies that the current connection is via WiFi",
      },
      CONNECTION_TYPE_WWAN = {
        type = "value",
        description = "Signifies that the current connection is via WWAN",
      },
      OS_BRAND_ANDROID = {
        type = "value",
        description = "Signifies that Moai is currently running on Android",
      },
      OS_BRAND_IOS = {
        type = "value",
        description = "Signifies that Moai is currently running on iOS",
      },
      OS_BRAND_OSX = {
        type = "value",
        description = "Signifies that Moai is currently running on OSX",
      },
      OS_BRAND_LINUX = {
        type = "value",
        description = "Signifies that Moai is currently running on Linux",
      },
      OS_BRAND_WINDOWS = {
        type = "value",
        description = "Signifies that Moai is currently running on Windows",
      },
      OS_BRAND_UNAVAILABLE = {
        type = "value",
        description = "Signifies that the operating system cannot be determined",
      },
      generateGUID = {
        type = "function",
        description = "Generates a globally unique identifier. This method will be moved to MOAIUnique in a future release.",
        args = '()',
        returns = '(GUID: string)',
      },
      getMACAddress = {
        type = "function",
        description = "Finds and returns the primary MAC Address",
        args = '()',
        returns = '(MAC: string)',
      },
      setValue = {
        type = "function",
        description = "Sets an evironment value and also triggers the listener callback (if any).",
        args = '(key: string [, value: variant | nil])',
        returns = '()',
      },
    },
  },
  MOAIEventSource = {
    type = "class",
    description = "Derivation of MOAIEventSource for global lua objects.",
    childs = {
      setListener = {
        type = "function",
        description = "Sets a listener callback for a given event ID. It is up to individual classes to declare their event IDs.",
        args = '(self: MOAIInstanceEventSource, eventID: number [, callback: function | nil])',
        returns = '(self: MOAIInstanceEventSource)',
      },
    },
  },
  MOAIFileStream = {
    type = "class",
    description = "MOAIFileStream opens a system file handle for eading or writing.",
    childs = {
      READ = {
        type = "value",
        description = "",
      },
      READ_WRITE = {
        type = "value",
        description = "",
      },
      READ_WRITE_AFFIRM = {
        type = "value",
        description = "",
      },
      READ_WRITE_NEW = {
        type = "value",
        description = "",
      },
      WRITE = {
        type = "value",
        description = "",
      },
      close = {
        type = "function",
        description = "Close and release the associated file handle.",
        args = '(self: MOAIFileStream)',
        returns = '()',
      },
      open = {
        type = "function",
        description = "Open or create a file stream given a valid path.",
        args = '(self: MOAIFileStream [, mode: number])',
        returns = '(success: boolean)',
      },
    },
  },
  MOAIFileSystem = {
    type = "class",
    description = "Functions for manipulating the file system.",
    childs = {
      affirmPath = {
        type = "function",
        description = "Creates a folder at 'path' if none exists.",
        args = '(path: string)',
        returns = '()',
      },
      checkFileExists = {
        type = "function",
        description = "Check for the existence of a file.",
        args = '(filename: string)',
        returns = '(exists: boolean)',
      },
      checkPathExists = {
        type = "function",
        description = "Check for the existence of a path.",
        args = '(path: string)',
        returns = '(exists: boolean)',
      },
      copy = {
        type = "function",
        description = "Copy a file or directory to a new location.",
        args = '(srcPath: string, destPath: string)',
        returns = '(result: boolean)',
      },
      deleteDirectory = {
        type = "function",
        description = "Deletes a directory and all of its contents.",
        args = '(path: string, (Optional): boolean)',
        returns = '(success: boolean)',
      },
      deleteFile = {
        type = "function",
        description = "Deletes a file.",
        args = '(filename: string)',
        returns = '(success: boolean)',
      },
      getAbsoluteDirectoryPath = {
        type = "function",
        description = "Returns the absolute path given a relative path.",
        args = '(path: string)',
        returns = '(absolute: string)',
      },
      getAbsoluteFilePath = {
        type = "function",
        description = "Returns the absolute path to a file. Result includes the file name.",
        args = '(filename: string)',
        returns = '(absolute: string)',
      },
      getWorkingDirectory = {
        type = "function",
        description = "Returns the path to current working directory.",
        args = '(path: string)',
        returns = '(path: string, path: string)',
      },
      listDirectories = {
        type = "function",
        description = "Lists the sub-directories contained in a directory.",
        args = '([path: string])',
        returns = '(diresctories: table)',
      },
      listFiles = {
        type = "function",
        description = "Lists the files contained in a directory",
        args = '([path: string])',
        returns = '(files: table)',
      },
      mountVirtualDirectory = {
        type = "function",
        description = "Mount an archive as a virtual filesystem directory.",
        args = '(path: string [, archive: string | nil])',
        returns = '(success: boolean)',
      },
      rename = {
        type = "function",
        description = "Renames a file or folder.",
        args = '(oldPath: string, newPath: string)',
        returns = '(success: boolean)',
      },
      setWorkingDirectory = {
        type = "function",
        description = "Sets the current working directory.",
        args = '(path: string)',
        returns = '(success: boolean)',
      },
    },
  },
  MOAIFont = {
    type = "class",
    description = "MOAIFont is the top level object for managing sets of glyphs associated with a single font face. An instance of MOAIFont may contain glyph sets for multiple sizes of the font. Alternatively, a separate instance of MOAIFont may be used for each font size. Using a single font object for each size of a font face can make it easier to unload font sizes that are no longer needed.</p>",
    childs = {
      FONT_AUTOLOAD_KERNING = {
        type = "value",
        description = "",
      },
      DEFAULT_FLAGS = {
        type = "value",
        description = "",
      },
      getFilename = {
        type = "function",
        description = "Returns the filename of the font.",
        args = '(self: MOAIFont)',
        returns = '(name: ?)',
      },
      getFlags = {
        type = "function",
        description = "Returns the current flags.",
        args = '(self: MOAIFont)',
        returns = '(flags: ?)',
      },
      getImage = {
        type = "function",
        description = "Requests a 'glyph map image' from the glyph cache currently attached to the font. The glyph map image stiches together the texture pages used by the glyph cache to produce a single image that represents a snapshot of all of the texture memory being used by the font.",
        args = '(self: MOAIFont)',
        returns = '(image: MOAIImage)',
      },
      load = {
        type = "function",
        description = "Sets the filename of the font for use when loading glyphs.",
        args = '(self: MOAIFont, filename: string)',
        returns = '()',
      },
      loadFromBMFont = {
        type = "function",
        description = "Sets the filename of the font for use when loading a BMFont.",
        args = '(self: MOAIFont, filename: string)',
        returns = '()',
      },
      preloadGlyphs = {
        type = "function",
        description = "Loads and caches glyphs for quick access later.",
        args = '(self: MOAIFont, charCodes: string, points: number [, dpi: number | 72])',
        returns = '()',
      },
      rebuildKerningTables = {
        type = "function",
        description = "Forces a full reload of the kerning tables for either a single glyph set within the font (if a size is specified) or for all glyph sets in the font.",
        args = '(self: MOAIFont)',
        returns = '()',
      },
      setCache = {
        type = "function",
        description = "Attaches or cloears the glyph cache associated with the font. The cache is an object derived from MOAIGlyphCacheBase and may be a dynamic cache that can allocate space for new glyphs on an as-needed basis or a static cache that only supports direct loading of glyphs and glyph textures through MOAIFont's setImage () command.",
        args = '(self: MOAIFont [, cache: MOAIGlyphCacheBase | nil])',
        returns = '()',
      },
      setDefaultSize = {
        type = "function",
        description = "Selects a glyph set size to use as the default size when no other size is specified by objects wishing to use MOAIFont to render text.",
        args = '(self: MOAIFont, points: number [, dpi: number | 72])',
        returns = '()',
      },
      setFlags = {
        type = "function",
        description = "Set flags to control font loading behavior. Right now the only supported flag is FONT_AUTOLOAD_KERNING which may be used to enable automatic loading of kern tables. This flag is initially true by default.",
        args = '(self: MOAIFont [, flags: number])',
        returns = '()',
      },
      setImage = {
        type = "function",
        description = "Passes an image to the glyph cache currently attached to the font. The image will be used to recreate and initialize the texture memory managed by the glyph cache and used by the font. It will not affect any glyph entires that have already been laid out and stored in the glyph cache.",
        args = '(self: MOAIFont, image: MOAIImage)',
        returns = '()',
      },
      setReader = {
        type = "function",
        description = "Attaches or clears the MOAIFontReader associated withthe font. MOAIFontReader is responsible for loading and rendering glyphs from a font file on demand. If you are using a static font and do not need a reader, set this field to nil.",
        args = '(self: MOAIFont [, reader: MOAIFontReader | nil])',
        returns = '()',
      },
    },
  },
  MOAIFoo = {
    type = "class",
    description = "Example class for extending Moai using MOAILuaObject. Copy this object, rename it and add your own stuff. Just don't forget to register it with the runtime using the REGISTER_LUA_CLASS macro (see moaicore.cpp).",
    childs = {
      classHello = {
        type = "function",
        description = "Class (a.k.a. static) method. Prints the string 'MOAIFoo class foo!' to the console.",
        args = '()',
        returns = '()',
      },
      instanceHello = {
        type = "function",
        description = "Prints the string 'MOAIFoo instance foo!' to the console.",
        args = '()',
        returns = '()',
      },
    },
  },
  MOAIFooMgr = {
    type = "class",
    description = "Example singleton for extending Moai using MOAILuaObject. Copy this object, rename it and add your own stuff. Just don't forget to register it with the runtime using the REGISTER_LUA_CLASS macro (see moaicore.cpp).",
    childs = {
      _singletonHello = {
        type = "function",
        description = "Prints the string 'MOAIFooMgr singleton foo!' to the console.",
        args = '()',
        returns = '()',
      },
    },
  },
  MOAIFrameBuffer = {
    type = "class",
    description = "This is an implementation of a frame buffer that may be attached to a MOAILayer for offscreen rendering. It is also a texture that may be bound and used like any other.",
    childs = {
      init = {
        type = "function",
        description = "Initializes frame buffer.",
        args = '(self: MOAIFrameBuffer, width: number, height: number)',
        returns = '()',
      },
      setClearColor = {
        type = "function",
        description = "At the start of each frame the buffer will by default automatically	render a background color. Using this function you can set the background color	that is drawn each frame. If you specify no arguments to this function, then	automatic redraw of the background color will be turned off (i.e	the previous render will be used as the background).",
        args = '(self: MOAIFrameBuffer [, red: number [, green: number [, blue: number [, alpha: number]]]])',
        returns = '()',
      },
      setClearDepth = {
        type = "function",
        description = "At the start of each frame the buffer will by default automatically clear the depth buffer. This function sets whether or not the depth buffer should be cleared at the start of each frame.",
        args = '(self: MOAIFrameBuffer, clearDepth: boolean)',
        returns = '()',
      },
    },
  },
  MOAIFreeTypeFontReader = {
    type = "class",
    description = "Implementation of MOAIFontReader that based on FreeType 2. Can load and render TTF and OTF font files.",
    childs = {
    },
  },
  MOAIGfxDevice = {
    type = "class",
    description = "Interface to the graphics singleton.",
    childs = {
      EVENT_RESIZE = {
        type = "value",
        description = "",
      },
      getMaxTextureUnits = {
        type = "function",
        description = "Returns the total number of texture units available on the device.",
        args = '(self: MOAIGfxDevice)',
        returns = '(maxTextureUnits: number)',
      },
      getViewSize = {
        type = "function",
        description = "Returns the width and height of the view",
        args = '()',
        returns = '(width: int, height: int)',
      },
      isProgrammable = {
        type = "function",
        description = "Returns a boolean indicating whether or not Moai is running under the programmable pipeline.",
        args = '(self: MOAIGfxDevice)',
        returns = '(isProgrammable: boolean)',
      },
      setClearColor = {
        type = "function",
        description = "At the start of each frame the device will by default automatically render a background color. Using this function you can set the background color that is drawn each frame. If you specify no arguments to this function, then automatic redraw of the background color will be turned off (i.e. the previous render will be used as the background).",
        args = '([red: number [, green: number [, blue: number [, alpha: number]]]])',
        returns = '()',
      },
      setClearDepth = {
        type = "function",
        description = "At the start of each frame the device will by default automatically clear the depth buffer. This function sets whether or not the depth buffer should be cleared at the start of each frame.",
        args = '(clearDepth: boolean)',
        returns = '()',
      },
      setPenColor = {
        type = "function",
        description = "",
        args = '(r: number, g: number, b: number [, a: number | 1])',
        returns = '()',
      },
      setPenWidth = {
        type = "function",
        description = "",
        args = '(width: number)',
        returns = '()',
      },
      setPointSize = {
        type = "function",
        description = "",
        args = '(size: number)',
        returns = '()',
      },
    },
  },
  MOAIGfxQuad2D = {
    type = "class",
    description = "Single textured quad.",
    childs = {
      setQuad = {
        type = "function",
        description = "Set model space quad. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAIGfxQuad2D, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set the model space dimensions of the quad.",
        args = '(self: MOAIGfxQuad2D, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setUVQuad = {
        type = "function",
        description = "Set the UV space dimensions of the quad. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAIGfxQuad2D, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setUVRect = {
        type = "function",
        description = "Set the UV space dimensions of the quad.",
        args = '(self: MOAIGfxQuad2D, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      transform = {
        type = "function",
        description = "Apply the given MOAITransform to all the vertices in the deck.",
        args = '(self: MOAIGfxQuad2D, transform: MOAITransform)',
        returns = '()',
      },
      transformUV = {
        type = "function",
        description = "Apply the given MOAITransform to all the uv coordinates in the deck.",
        args = '(self: MOAIGfxQuad2D, transform: MOAITransform)',
        returns = '()',
      },
    },
  },
  MOAIGfxQuadDeck2D = {
    type = "class",
    description = "Deck of textured quads.",
    childs = {
      reserve = {
        type = "function",
        description = "Set capacity of quad deck.",
        args = '(self: MOAIGfxQuadDeck2D, nQuads: number)',
        returns = '()',
      },
      setQuad = {
        type = "function",
        description = "Set model space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAIGfxQuadDeck2D, idx: number, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set model space quad given a valid deck index and a rect.",
        args = '(self: MOAIGfxQuadDeck2D, idx: number, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setUVQuad = {
        type = "function",
        description = "Set UV space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAIGfxQuadDeck2D, idx: number, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setUVRect = {
        type = "function",
        description = "Set UV space quad given a valid deck index and a rect.",
        args = '(self: MOAIGfxQuadDeck2D, idx: number, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      transform = {
        type = "function",
        description = "Apply the given MOAITransform to all the vertices in the deck.",
        args = '(self: MOAIGfxQuadDeck2D, transform: MOAITransform)',
        returns = '()',
      },
      transformUV = {
        type = "function",
        description = "Apply the given MOAITransform to all the uv coordinates in the deck.",
        args = '(self: MOAIGfxQuadDeck2D, transform: MOAITransform)',
        returns = '()',
      },
    },
  },
  MOAIGfxQuadListDeck2D = {
    type = "class",
    description = "Deck of lists of textured quads. UV and model space quads are specified independently and associated via pairs. Pairs are referenced by lists sequentially. There may be multiple pairs with the same UV/model quad indicices if geometry is used in multiple lists.",
    childs = {
      reserveLists = {
        type = "function",
        description = "Reserve quad lists.",
        args = '(self: MOAIGfxQuadListDeck2D, nLists: number)',
        returns = '()',
      },
      reservePairs = {
        type = "function",
        description = "Reserve pairs.",
        args = '(self: MOAIGfxQuadListDeck2D, nPairs: number)',
        returns = '()',
      },
      reserveQuads = {
        type = "function",
        description = "Reserve quads.",
        args = '(self: MOAIGfxQuadListDeck2D, nQuads: number)',
        returns = '()',
      },
      reserveUVQuads = {
        type = "function",
        description = "Reserve UV quads.",
        args = '(self: MOAIGfxQuadListDeck2D, nUVQuads: number)',
        returns = '()',
      },
      setList = {
        type = "function",
        description = "Initializes quad pair list at index. A list starts at the index of a pair and then continues sequentially for n pairs after. So a list with base 3 and a run of 4 would display pair 3, 4, 5, and 6.",
        args = '(self: MOAIGfxQuadListDeck2D, idx: number, basePairID: number, totalPairs: number)',
        returns = '()',
      },
      setPair = {
        type = "function",
        description = "Associates a quad with its UV coordinates.",
        args = '(self: MOAIGfxQuadListDeck2D, idx: number, uvQuadID: number, quadID: number)',
        returns = '()',
      },
      setQuad = {
        type = "function",
        description = "Set model space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAIGfxQuadListDeck2D, idx: number, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set model space quad given a valid deck index and a rect.",
        args = '(self: MOAIGfxQuadListDeck2D, idx: number, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setUVQuad = {
        type = "function",
        description = "Set UV space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAIGfxQuadListDeck2D, idx: number, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setUVRect = {
        type = "function",
        description = "Set UV space quad given a valid deck index and a rect.",
        args = '(self: MOAIGfxQuadListDeck2D, idx: number, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      transform = {
        type = "function",
        description = "Apply the given MOAITransform to all the vertices in the deck.",
        args = '(self: MOAIGfxQuadListDeck2D, transform: MOAITransform)',
        returns = '()',
      },
      transformUV = {
        type = "function",
        description = "Apply the given MOAITransform to all the uv coordinates in the deck.",
        args = '(self: MOAIGfxQuadListDeck2D, transform: MOAITransform)',
        returns = '()',
      },
    },
  },
  MOAIGfxResource = {
    type = "class",
    description = "Base class for graphics resources owned by OpenGL. Implements resource lifecycle including restoration from a lost graphics context (if possible).",
    childs = {
      getAge = {
        type = "function",
        description = "Returns the 'age' of the graphics resource. The age is the number of times MOAIRenderMgr has rendered a scene since the resource was last bound. It is part of the render count, not a timestamp. This may change to be time-based in futurecreleases.",
        args = '(self: MOAIGfxResource)',
        returns = '(age: number)',
      },
      softRelease = {
        type = "function",
        description = "Attempt to release the resource. Generally this is used when responding to a memory warning from the system. A resource will only be released if it is reneweable (i.e. has a renew callback or contains all information needed to reload the resources on demand).",
        args = '(self: MOAIGfxResource [, age: int])',
        returns = '(True: boolean)',
      },
    },
  },
  MOAIGlyphCache = {
    type = "class",
    description = "This is the default implementation of a dynamic glyph cache. Right now it can only grow but support for reference counting glyphs and garbage collection will be added later.</p>",
    childs = {
    },
  },
  MOAIGlyphCacheBase = {
    type = "class",
    description = "Base class for implementations of glyph caches. A glyph cache is responsible for allocating textures to hold rendered glyphs and for placing individuals glyphs on those textures.</p>",
    childs = {
      setColorFormat = {
        type = "function",
        description = "The color format may be used by dynamic cache implementations when allocating new textures.",
        args = '(self: MOAIFont, colorFmt: number)',
        returns = '()',
      },
    },
  },
  MOAIGrid = {
    type = "class",
    description = "Grid data object. Grid cells are indexed starting and (1,1). Grid indices will wrap if out of range.",
    childs = {
      clearTileFlags = {
        type = "function",
        description = "Clears bits specified in mask.",
        args = '(self: MOAIGrid, xTile: number, yTile: number, mask: number)',
        returns = '()',
      },
      getTile = {
        type = "function",
        description = "Returns the value of a given tile.",
        args = '(self: MOAIGrid, xTile: number, yTile: number)',
        returns = '(tile: number)',
      },
      getTileFlags = {
        type = "function",
        description = "Returns the masked value of a given tile.",
        args = '(self: MOAIGrid, xTile: number, yTile: number, mask: number)',
        returns = '(tile: number)',
      },
      setRow = {
        type = "function",
        description = "Initializes a grid row given a variable argument list of values.",
        args = '(self: MOAIGrid, row: number, ...)',
        returns = '()',
      },
      setTile = {
        type = "function",
        description = "Sets the value of a given tile",
        args = '(self: MOAIGrid, xTile: number, yTile: number, value: number)',
        returns = '()',
      },
      setTileFlags = {
        type = "function",
        description = "Sets a tile's flags given a mask.",
        args = '(self: MOAIGrid, xTile: number, yTile: number, mask: number)',
        returns = '()',
      },
      streamTilesIn = {
        type = "function",
        description = "Reads tiles directly from a stream. Call this only after initializing the grid. Only the content of the tiles buffer is read.",
        args = '(self: MOAIGrid, stream: MOAIStream)',
        returns = '(bytesRead: number)',
      },
      streamTilesOut = {
        type = "function",
        description = "Writes tiles directly to a stream. Only the content of the tiles buffer is written.",
        args = '(self: MOAIGrid, stream: MOAIStream)',
        returns = '(bytesWritten: number)',
      },
      toggleTileFlags = {
        type = "function",
        description = "Toggles a tile's flags given a mask.",
        args = '(self: MOAIGrid, xTile: number, yTile: number, mask: number)',
        returns = '()',
      },
    },
  },
  MOAIGridDeck2D = {
    type = "class",
    description = "This deck renders 'brushes' which are sampled from a tile map. The tile map is specified by the attached grid, deck and remapper. Each 'brush' defines a rectangle of tiles to draw and an offset.",
    childs = {
      reserve = {
        type = "function",
        description = "Set capacity of grid deck.",
        args = '(self: MOAIGridDeck2D, nBrushes: number)',
        returns = '()',
      },
      setBrush = {
        type = "function",
        description = "Initializes a brush.",
        args = '(self: MOAIGridDeck2D, idx: number, xTile: number, yTile: number, width: number, height: number [, xOff: number | 0 [, yOff: number | 0]])',
        returns = '()',
      },
      setDeck = {
        type = "function",
        description = "Sets or clears the deck to be indexed by the grid.",
        args = '(self: MOAIGridDeck2D [, deck: MOAIDeck | nil])',
        returns = '()',
      },
      setGrid = {
        type = "function",
        description = "Sets or clears the grid to be sampled by the brushes.",
        args = '(self: MOAIGridDeck2D [, grid: MOAIGrid | nil])',
        returns = '()',
      },
      setRemapper = {
        type = "function",
        description = "Sets or clears the remapper (for remapping index values held in the grid).",
        args = '(self: MOAIGridDeck2D [, remapper: MOAIDeckRemapper | nil])',
        returns = '()',
      },
    },
  },
  MOAIGridPathGraph = {
    type = "class",
    description = "Pathfinder graph adapter for MOAIGrid.",
    childs = {
      setGrid = {
        type = "function",
        description = "Set graph data to use for pathfinding. ",
        args = '(self: MOAIGridPathGraph [, grid: MOAIGrid | nil])',
        returns = '()',
      },
    },
  },
  MOAIGridSpace = {
    type = "class",
    description = "Represents spatial configuration of a grid. The grid is made up of cells. Inside of each cell is a tile. The tile can be larger or smaller than the cell and also offset from the cell. By default, tiles are the same size of their cells and are no offset.",
    childs = {
      TILE_BOTTOM_CENTER = {
        type = "value",
        description = "",
      },
      TILE_CENTER = {
        type = "value",
        description = "",
      },
      TILE_LEFT_BOTTOM = {
        type = "value",
        description = "",
      },
      TILE_LEFT_CENTER = {
        type = "value",
        description = "",
      },
      TILE_LEFT_TOP = {
        type = "value",
        description = "",
      },
      TILE_RIGHT_BOTTOM = {
        type = "value",
        description = "",
      },
      TILE_RIGHT_CENTER = {
        type = "value",
        description = "",
      },
      TILE_RIGHT_TOP = {
        type = "value",
        description = "",
      },
      TILE_TOP_CENTER = {
        type = "value",
        description = "",
      },
      SQUARE_SHAPE = {
        type = "value",
        description = "",
      },
      DIAMOND_SHAPE = {
        type = "value",
        description = "",
      },
      OBLIQUE_SHAPE = {
        type = "value",
        description = "",
      },
      HEX_SHAPE = {
        type = "value",
        description = "",
      },
      cellAddrToCoord = {
        type = "function",
        description = "Returns the coordinate of a cell given an address.",
        args = '(self: MOAIGridSpace, xTile: number, yTile: number)',
        returns = '(cellAddr: number)',
      },
      getCellAddr = {
        type = "function",
        description = "Returns the address of a cell given a coordinate (in tiles).",
        args = '(self: MOAIGridSpace, xTile: number, yTile: number)',
        returns = '(cellAddr: number)',
      },
      getCellSize = {
        type = "function",
        description = "Returns the dimensions of a single grid cell.",
        args = '(self: MOAIGridSpace)',
        returns = '(width: number, height: number)',
      },
      getOffset = {
        type = "function",
        description = "Returns the offset of tiles from cells.",
        args = '(self: MOAIGridSpace)',
        returns = '(xOff: number, yOff: number)',
      },
      getSize = {
        type = "function",
        description = "Returns the dimensions of the grid (in tiles).",
        args = '(self: MOAIGridSpace)',
        returns = '(width: number, height: number)',
      },
      getTileLoc = {
        type = "function",
        description = "Returns the grid space coordinate of the tile. The optional 'position' flag determines the location of the coordinate within the tile.",
        args = '(self: MOAIGridSpace, xTile: number, yTile: number [, position: number])',
        returns = '(x: number, y: number)',
      },
      getTileSize = {
        type = "function",
        description = "Returns the dimensions of a single grid tile.",
        args = '(self: MOAIGridSpace)',
        returns = '(width: number, height: number)',
      },
      initDiamondGrid = {
        type = "function",
        description = "Initialize a grid with hexagonal tiles.",
        args = '(self: MOAIGridSpace, width: number, height: number [, tileWidth: number [, tileHeight: number [, xGutter: number [, yGutter: number | 0]]]])',
        returns = '()',
      },
      initHexGrid = {
        type = "function",
        description = "Initialize a grid with hexagonal tiles.",
        args = '(self: MOAIGridSpace, width: number, height: number [, radius: number [, xGutter: number [, yGutter: number | 0]]])',
        returns = '()',
      },
      initObliqueGrid = {
        type = "function",
        description = "Initialize a grid with oblique tiles.",
        args = '(self: MOAIGridSpace, width: number, height: number [, tileWidth: number [, tileHeight: number [, xGutter: number [, yGutter: number | 0]]]])',
        returns = '()',
      },
      initRectGrid = {
        type = "function",
        description = "Initialize a grid with rectangular tiles.",
        args = '(self: MOAIGridSpace, width: number, height: number [, tileWidth: number [, tileHeight: number [, xGutter: number [, yGutter: number | 0]]]])',
        returns = '()',
      },
      locToCellAddr = {
        type = "function",
        description = "Returns the address of a cell given a a coordinate in grid space.",
        args = '(self: MOAIGridSpace, x: number, y: number)',
        returns = '(cellAddr: number)',
      },
      locToCoord = {
        type = "function",
        description = "Transforms a coordinate in grid space into a tile index.",
        args = '(self: MOAIGridSpace, x: number, y: number)',
        returns = '(xTile: number, yTile: number)',
      },
      setRepeat = {
        type = "function",
        description = "Repeats a grid indexer along X or Y. Only used when a grid is attached.",
        args = '(self: MOAIGridSpace [, repeatX: boolean | true [, repeatY: boolean | repeatX]])',
        returns = '()',
      },
      setShape = {
        type = "function",
        description = "Set the shape of the grid tiles.",
        args = '(self: MOAIGridSpace [, shape: number])',
        returns = '()',
      },
      setSize = {
        type = "function",
        description = "Initializes dimensions of grid and reserves storage for tiles.",
        args = '(self: MOAIGridSpace, width: number, height: number [, cellWidth: number | 1 [, cellHeight: number | 1 [, xOff: number [, yOff: number [, tileWidth: number | cellWidth [, tileHeight: number | cellHeight]]]]]])',
        returns = '()',
      },
      wrapCoord = {
        type = "function",
        description = "Wraps a tile index to the range of the grid.",
        args = '(self: MOAIGridSpace, xTile: number, yTile: number)',
        returns = '(xTile: number, yTile: number)',
      },
    },
  },
  MOAIHttpTaskBase = {
    type = "class",
    description = "Object for performing asynchronous HTTP/HTTPS tasks.",
    childs = {
      HTTP_GET = {
        type = "value",
        description = "",
      },
      HTTP_HEAD = {
        type = "value",
        description = "",
      },
      HTTP_POST = {
        type = "value",
        description = "",
      },
      HTTP_PUT = {
        type = "value",
        description = "",
      },
      HTTP_DELETE = {
        type = "value",
        description = "",
      },
      getResponseHeader = {
        type = "function",
        description = "Returns the response header given its name, or nil if it wasn't provided by the server. Header names are case-insensitive and if multiple responses are given, they will be concatenated with a comma separating the values.",
        args = '(self: MOAIHttpTask, self: MOAIHttpTask, header: string)',
        returns = '(code: number, response: string)',
      },
      getSize = {
        type = "function",
        description = "Returns the size of the string obtained from a httpPost or httpGet call.",
        args = '(self: MOAIHttpTaskBase)',
        returns = '(size: number)',
      },
      getString = {
        type = "function",
        description = "Returns the text obtained from a httpPost or httpGet call.",
        args = '(self: MOAIHttpTaskBase)',
        returns = '(text: string)',
      },
      httpGet = {
        type = "function",
        description = "Sends an API call to the server for downloading data. The callback function (from setCallback) will run when the call is complete, i.e. this action is asynchronous and returns almost instantly.",
        args = '(self: MOAIHttpTaskBase, url: string [, useragent: string | "Moai SDK beta; support@getmoai.com" [, verbose: boolean [, blocking: boolean | false]]])',
        returns = '()',
      },
      httpPost = {
        type = "function",
        description = "Sends an API call to the server for downloading data. The callback function (from setCallback) will run when the call is complete, i.e. this action is asynchronous and returns almost instantly.",
        args = '(self: MOAIHttpTaskBase, url: string [, data: string [, useragent: string | "Moai SDK beta; support@getmoai.com" [, verbose: boolean [, blocking: boolean | false]]]])',
        returns = '()',
      },
      parseXml = {
        type = "function",
        description = "Parses the text data returned from a httpGet or httpPost operation as XML and then returns a MOAIXmlParser with the XML content initialized.",
        args = '(self: MOAIHttpTaskBase)',
        returns = '(parser: MOAIXmlParser)',
      },
      performAsync = {
        type = "function",
        description = "Perform the http task asynchronously.",
        args = '(self: MOAIHttpTaskBase)',
        returns = '()',
      },
      performSync = {
        type = "function",
        description = "Perform the http task synchronously ( blocking).",
        args = '(self: MOAIHttpTaskBase)',
        returns = '()',
      },
      setBody = {
        type = "function",
        description = "Sets the body for a POST or PUT.",
        args = '(self: MOAIHttpTaskBase [, data: string])',
        returns = '()',
      },
      setCallback = {
        type = "function",
        description = "Sets the callback function used when a request is complete.",
        args = '(self: MOAIHttpTaskBase, callback: function)',
        returns = '()',
      },
      setHeader = {
        type = "function",
        description = "Sets a custom header field. May be used to override default headers.",
        args = '(self: MOAIHttpTaskBase, filename: string, self: MOAIHttpTaskBase, filename: string, self: MOAIHttpTaskBase, follow: bool, self: MOAIHttpTaskBase, key: string [, value: string])',
        returns = '()',
      },
      setUrl = {
        type = "function",
        description = "Sets the URL for the task.",
        args = '(self: MOAIHttpTaskBase, url: string, self: MOAIHttpTaskBase, url: string)',
        returns = '()',
      },
      setUserAgent = {
        type = "function",
        description = "Sets the 'useragent' header for the task.",
        args = '(self: MOAIHttpTaskBase [, useragent: string | "Moai SDK beta; support@getmoai.com"])',
        returns = '()',
      },
      setVerb = {
        type = "function",
        description = "Sets the http verb.",
        args = '(self: MOAIHttpTaskBase, verb: number)',
        returns = '()',
      },
      setVerbose = {
        type = "function",
        description = "Sets the task implementation to print out debug information (if any).",
        args = '(self: MOAIHttpTaskBase [, verbose: boolean | false])',
        returns = '()',
      },
    },
  },
  MOAIHttpTaskCurl = {
    type = "class",
    description = "Implementation of MOAIHttpTask based on libcurl.",
    childs = {
    },
  },
  MOAIHttpTaskNaCl = {
    type = "class",
    description = "Implementation of MOAIHttpTask based on NaCl.",
    childs = {
    },
  },
  MOAIImage = {
    type = "class",
    description = "Image/bitmap class.",
    childs = {
      FILTER_LINEAR = {
        type = "value",
        description = "",
      },
      FILTER_NEAREST = {
        type = "value",
        description = "",
      },
      bleedRect = {
        type = "function",
        description = "'Bleeds' the interior of the rectangle out by one pixel.",
        args = '(self: MOAIImage, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      convertColors = {
        type = "function",
        description = "Return a copy of the image with a new color format. Not all provided formats are supported by OpenGL.",
        args = '(self: MOAIImage, colorFmt: number)',
        returns = '(image: MOAIImage)',
      },
      copy = {
        type = "function",
        description = "Copies an image.",
        args = '(self: MOAIImage)',
        returns = '(image: MOAIImage)',
      },
      copyBits = {
        type = "function",
        description = "Copy a section of one image to another.",
        args = '(self: MOAIImage, source: MOAIImage, srcX: number, srcY: number, destX: number, destY: number, width: number, height: number)',
        returns = '()',
      },
      copyRect = {
        type = "function",
        description = "Copy a section of one image to another. Accepts two rectangles. Rectangles may be of different size and proportion. Section of image may also be flipped horizontally or vertically by reversing min/max of either rectangle.",
        args = '(self: MOAIImage, source: MOAIImage, srcXMin: number, srcYMin: number, srcXMax: number, srcYMax: number, destXMin: number, destYMin: number [, destXMax: number | destXMin [, destYMax: number | destYMin [, filter: number]]])',
        returns = '()',
      },
      fillRect = {
        type = "function",
        description = "Fill a rectangle in the image with a solid color.",
        args = '(self: MOAIImage, xMin: number, yMin: number, xMax: number, yMax: number [, r: number | 0 [, g: number | 0 [, b: number | 0 [, a: number | 0]]]])',
        returns = '()',
      },
      getColor32 = {
        type = "function",
        description = "Returns a 32-bit packed RGBA value from the image for a given pixel coordinate.",
        args = '(self: MOAIImage, x: number, y: number)',
        returns = '(color: number)',
      },
      getFormat = {
        type = "function",
        description = "Returns the color format of the image.",
        args = '(self: MOAIImage)',
        returns = '(colorFormat: number)',
      },
      getRGBA = {
        type = "function",
        description = "Returns an RGBA color as four floating point values.",
        args = '(self: MOAIImage, x: number, y: number)',
        returns = '(r: number, g: number, b: number, a: number)',
      },
      getSize = {
        type = "function",
        description = "Returns the width and height of the image.",
        args = '(self: MOAIImage)',
        returns = '(width: number, height: number)',
      },
      init = {
        type = "function",
        description = "Initializes the image with a width, height and color format.",
        args = '(self: MOAIImage, width: number, height: number [, One: colorFmt])',
        returns = '()',
      },
      load = {
        type = "function",
        description = "Loads an image from a PNG.",
        args = '(self: MOAIImage, filename: string [, transform: number])',
        returns = '()',
      },
      loadFromBuffer = {
        type = "function",
        description = "Loads an image from a buffer.",
        args = '(self: MOAIImage, Buffer: MOAIDataBuffer [, transform: number])',
        returns = '()',
      },
      padToPow2 = {
        type = "function",
        description = "Copies an image and returns a new image padded to the next power of 2 along each dimension. Original image will be in the upper left hand corner of the new image.",
        args = '(self: MOAIImage)',
        returns = '(image: MOAIImage)',
      },
      resize = {
        type = "function",
        description = "Copies the image to an image with a new size.",
        args = '(self: MOAIImage, width: number, height: number [, filter: number])',
        returns = '(image: MOAIImage)',
      },
      resizeCanvas = {
        type = "function",
        description = "Copies the image to a canvas with a new size. If the canvas is larger than the original image, the exta pixels will be initialized with 0. Pass in a new frame or just a new width and height. Negative values are permitted for the frame.",
        args = '(self: MOAIImage, width: number, height: number)',
        returns = '(image: MOAIImage)',
      },
      setColor32 = {
        type = "function",
        description = "Sets 32-bit the packed RGBA value for a given pixel coordinate. Parameter will be converted to the native format of the image.",
        args = '(self: MOAIImage, x: number, y: number, color: number)',
        returns = '()',
      },
      setRGBA = {
        type = "function",
        description = "Sets a color using RGBA floating point values.",
        args = '(self: MOAIImage, x: number, y: number, r: number, g: number, b: number [, a: number | 1])',
        returns = '()',
      },
      writePNG = {
        type = "function",
        description = "Write image to a PNG file.",
        args = '(self: MOAIImage, filename: string)',
        returns = '()',
      },
    },
  },
  MOAIImageTexture = {
    type = "class",
    description = "Binds an image (CPU memory) to a texture (GPU memory). Regions of the texture (or the entire texture) may be invalidated. Invalidated regions will be reloaded into GPU memory the next time the texture is bound.",
    childs = {
      invalidate = {
        type = "function",
        description = "Invalidate either a sub-region of the texture or the whole texture. Invalidated regions will be reloaded from the image the next time the texture is bound.",
        args = '(self: MOAIImage, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
    },
  },
  MOAIIndexBuffer = {
    type = "class",
    description = "Index buffer class. Unused at this time.",
    childs = {
      release = {
        type = "function",
        description = "Release any memory held by this index buffer.",
        args = '(self: MOAIIndexBuffer)',
        returns = '()',
      },
      reserve = {
        type = "function",
        description = "Set capacity of buffer.",
        args = '(self: MOAIIndexBuffer, nIndices: number)',
        returns = '()',
      },
      setIndex = {
        type = "function",
        description = "Initialize an index.",
        args = '(self: MOAIIndexBuffer, idx: number, value: number)',
        returns = '()',
      },
    },
  },
  MOAIInputDevice = {
    type = "class",
    description = "Manager class for input bindings. Has no public methods.",
    childs = {
    },
  },
  MOAIInputMgr = {
    type = "class",
    description = "Input device class. Has no public methods.",
    childs = {
    },
  },
  MOAIJoystickSensor = {
    type = "class",
    description = "Analog and digital joystick sensor.",
    childs = {
      getVector = {
        type = "function",
        description = "Returns the joystick vector.",
        args = '(self: MOAIJoystickSensor)',
        returns = '(x: number, y: number)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when the joystick vector changes.",
        args = '(self: MOAIJoystickSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAIJsonParser = {
    type = "class",
    description = "Converts between Lua and JSON.",
    childs = {
      decode = {
        type = "function",
        description = "Decode a JSON string into a hierarchy of Lua tables.",
        args = '(input: string)',
        returns = '(result: table)',
      },
      encode = {
        type = "function",
        description = "Encode a hierarchy of Lua tables into a JSON string.",
        args = '(input: table)',
        returns = '(result: string)',
      },
    },
  },
  MOAIKeyboardSensor = {
    type = "class",
    description = "Hardware keyboard sensor.",
    childs = {
      keyDown = {
        type = "function",
        description = "Checks to see if the button was pressed during the last iteration.",
        args = '(self: MOAIKeyboardSensor, key: string)',
        returns = '(wasPressed: boolean)',
      },
      keyIsDown = {
        type = "function",
        description = "Checks to see if the button is currently down.",
        args = '(self: MOAIKeyboardSensor, key: string)',
        returns = '(isDown: boolean)',
      },
      keyIsUp = {
        type = "function",
        description = "Checks to see if the specified key is currently up.",
        args = '(self: MOAIKeyboardSensor, key: string)',
        returns = '(wasReleased: boolean)',
      },
      keyUp = {
        type = "function",
        description = "Checks to see if the specified key was released during the last iteration.",
        args = '(self: MOAIKeyboardSensor, key: string)',
        returns = '(wasReleased: boolean)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when a key is pressed.",
        args = '(self: MOAIKeyboardSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAILayer = {
    type = "class",
    description = "Scene controls class.",
    childs = {
      SORT_NONE = {
        type = "value",
        description = "",
      },
      SORT_ISO = {
        type = "value",
        description = "",
      },
      SORT_PRIORITY_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_PRIORITY_DESCENDING = {
        type = "value",
        description = "",
      },
      SORT_X_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_X_DESCENDING = {
        type = "value",
        description = "",
      },
      SORT_Y_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_Y_DESCENDING = {
        type = "value",
        description = "",
      },
      SORT_Z_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_Z_DESCENDING = {
        type = "value",
        description = "",
      },
      SORT_VECTOR_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_VECTOR_DESCENDING = {
        type = "value",
        description = "",
      },
      clear = {
        type = "function",
        description = "Remove all props from the layer's partition.",
        args = '(self: MOAILayer)',
        returns = '()',
      },
      getFitting = {
        type = "function",
        description = "Computes a camera fitting for a given world rect along with an optional screen space padding. To do a fitting, compute the world rect based on whatever you are fitting to, use this method to get the fitting, then animate the camera to match.",
        args = '(self: MOAILayer, xMin: number, yMin: number, xMax: number, yMax: number [, xPad: number [, yPad: number]])',
        returns = '(x: number, y: number, s: number)',
      },
      getPartition = {
        type = "function",
        description = "Returns the partition (if any) currently attached to this layer.",
        args = '(self: MOAILayer)',
        returns = '(partition: MOAIPartition)',
      },
      getSortMode = {
        type = "function",
        description = "Get the sort mode for rendering.",
        args = '(self: MOAILayer)',
        returns = '(sortMode: number)',
      },
      getSortScale = {
        type = "function",
        description = "Return the scalar applied to axis sorts.",
        args = '(self: MOAILayer2D)',
        returns = '(x: number, y: number, priority: number)',
      },
      insertProp = {
        type = "function",
        description = "Adds a prop to the layer's partition.",
        args = '(self: MOAILayer, prop: MOAIProp)',
        returns = '()',
      },
      removeProp = {
        type = "function",
        description = "Removes a prop from the layer's partition.",
        args = '(self: MOAILayer, prop: MOAIProp)',
        returns = '()',
      },
      setBox2DWorld = {
        type = "function",
        description = "Sets a Box2D world for debug drawing.",
        args = '(self: MOAILayer, world: MOAIBox2DWorld)',
        returns = '()',
      },
      setCamera = {
        type = "function",
        description = "Sets a camera for the layer. If no camera is supplied, layer will render using the identity matrix as view/proj.",
        args = '(self: MOAILayer [, camera: MOAICamera | nil])',
        returns = '()',
      },
      setCpSpace = {
        type = "function",
        description = "Sets a Chipmunk space for debug drawing.",
        args = '(self: MOAILayer, space: MOAICpSpace)',
        returns = '()',
      },
      setFrameBuffer = {
        type = "function",
        description = "Attach a frame buffer. Layer will render to frame buffer instead of the main view.",
        args = '(self: MOAILayer, frameBuffer: MOAIFrameBuffer)',
        returns = '()',
      },
      setParallax = {
        type = "function",
        description = "Sets the parallax scale for this layer. This is simply a scalar applied to the view transform before rendering.",
        args = '(self: MOAILayer [, xParallax: number | 1 [, yParallax: number | 1 [, zParallax: number | 1]]])',
        returns = '()',
      },
      setPartition = {
        type = "function",
        description = "Sets a partition for the layer to use. The layer will automatically create a partition when the first prop is added if no partition has been set.",
        args = '(self: MOAILayer, partition: MOAIPartition)',
        returns = '()',
      },
      setPartitionCull2D = {
        type = "function",
        description = "Enables 2D partition cull (projection of frustum AABB will be used instead of AABB or frustum).",
        args = '(self: MOAILayer, partitionCull2D: boolean)',
        returns = '()',
      },
      setSortMode = {
        type = "function",
        description = "Set the sort mode for rendering.",
        args = '(self: MOAILayer, sortMode: number)',
        returns = '()',
      },
      setSortScale = {
        type = "function",
        description = "Set the scalar applied to axis sorts.",
        args = '(self: MOAILayer [, x: number | 0 [, y: number | 0 [, z: number | 0 [, priority: number | 1]]]])',
        returns = '()',
      },
      setViewport = {
        type = "function",
        description = "Set the layer's viewport.",
        args = '(self: MOAILayer, viewport: MOAIViewport)',
        returns = '()',
      },
      showDebugLines = {
        type = "function",
        description = "Display debug lines for props in this layer.",
        args = '(self: MOAILayer [, showDebugLines: bool | true])',
        returns = '()',
      },
      wndToWorld = {
        type = "function",
        description = "Project a point from window space into world space and return a normal vector representing a ray cast from the point into the world away from the camera (suitable for 3D picking).",
        args = '(self: MOAILayer, x: number, y: number, z: number)',
        returns = '(x: number, y: number, z: number, xn: number, yn: number, zn: number)',
      },
      worldToWnd = {
        type = "function",
        description = "Transform a point from world space to window space.",
        args = '(self: MOAILayer, x: number, y: number, Z: number)',
        returns = '(x: number, y: number, z: number)',
      },
    },
  },
  MOAILayer2D = {
    type = "class",
    description = "2D layer.",
    childs = {
      SORT_NONE = {
        type = "value",
        description = "",
      },
      SORT_PRIORITY_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_PRIORITY_DESCENDING = {
        type = "value",
        description = "",
      },
      SORT_X_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_X_DESCENDING = {
        type = "value",
        description = "",
      },
      SORT_Y_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_Y_DESCENDING = {
        type = "value",
        description = "",
      },
      SORT_VECTOR_ASCENDING = {
        type = "value",
        description = "",
      },
      SORT_VECTOR_DESCENDING = {
        type = "value",
        description = "",
      },
      clear = {
        type = "function",
        description = "Remove all props from the layer's partition.",
        args = '(self: MOAILayer2D)',
        returns = '()',
      },
      getFitting = {
        type = "function",
        description = "Computes a camera fitting for a given world rect along with an optional screen space padding. To do a fitting, compute the world rect based on whatever you are fitting to, use this method to get the fitting, then animate the camera to match.",
        args = '(self: MOAILayer2D, xMin: number, yMin: number, xMax: number, yMax: number [, xPad: number [, yPad: number]])',
        returns = '(x: number, y: number, s: number)',
      },
      getPartition = {
        type = "function",
        description = "Returns the partition (if any) currently attached to this layer.",
        args = '(self: MOAILayer2D)',
        returns = '(partition: MOAIPartition)',
      },
      getSortMode = {
        type = "function",
        description = "Get the sort mode for rendering.",
        args = '(self: MOAILayer2D)',
        returns = '(sortMode: number)',
      },
      getSortScale = {
        type = "function",
        description = "Return the scalar applied to axis sorts.",
        args = '(self: MOAILayer2D)',
        returns = '(x: number, y: number, priority: number)',
      },
      insertProp = {
        type = "function",
        description = "Adds a prop to the layer's partition.",
        args = '(self: MOAILayer2D, prop: MOAIProp)',
        returns = '()',
      },
      removeProp = {
        type = "function",
        description = "Removes a prop from the layer's partition.",
        args = '(self: MOAILayer2D, prop: MOAIProp)',
        returns = '()',
      },
      setBox2DWorld = {
        type = "function",
        description = "Sets a Box2D world for debug drawing.",
        args = '(self: MOAILayer2D, world: MOAIBox2DWorld)',
        returns = '()',
      },
      setCamera = {
        type = "function",
        description = "Sets a camera for the layer. If no camera is supplied, layer will render using the identity matrix as view/proj.",
        args = '(self: MOAILayer2D [, camera: MOAICamera2D | nil])',
        returns = '()',
      },
      setCpSpace = {
        type = "function",
        description = "Sets a Chipmunk space for debug drawing.",
        args = '(self: MOAILayer2D, space: MOAICpSpace)',
        returns = '()',
      },
      setFrameBuffer = {
        type = "function",
        description = "Attach a frame buffer. Layer will render to frame buffer instead of the main view.",
        args = '(self: MOAILayer2D, frameBuffer: MOAIFrameBuffer)',
        returns = '()',
      },
      setParallax = {
        type = "function",
        description = "Sets the parallax scale for this layer. This is simply a scalar applied to the view transform before rendering.",
        args = '(self: MOAILayer2D, xParallax: number, yParallax: number)',
        returns = '()',
      },
      setPartition = {
        type = "function",
        description = "Sets a partition for the layer to use. The layer will automatically create a partition when the first prop is added if no partition has been set.",
        args = '(self: MOAILayer2D, partition: MOAIPartition)',
        returns = '()',
      },
      setSortMode = {
        type = "function",
        description = "Set the sort mode for rendering.",
        args = '(self: MOAILayer2D, sortMode: number)',
        returns = '()',
      },
      setSortScale = {
        type = "function",
        description = "Set the scalar applied to axis sorts.",
        args = '(self: MOAILayer2D [, x: number | 0 [, y: number | 0 [, priority: number | 1]]])',
        returns = '()',
      },
      setViewport = {
        type = "function",
        description = "Set the layer's viewport.",
        args = '(self: MOAILayer2D, viewport: MOAIViewport)',
        returns = '()',
      },
      showDebugLines = {
        type = "function",
        description = "Display debug lines for props in this layer.",
        args = '(self: MOAILayer2D [, showDebugLines: bool | true])',
        returns = '()',
      },
      wndToWorld = {
        type = "function",
        description = "Project a point from window space into world space.",
        args = '(self: MOAILayer2D, x: number, y: number)',
        returns = '(x: number, y: number)',
      },
      worldToWnd = {
        type = "function",
        description = "Transform a point from world space to window space.",
        args = '(self: MOAILayer2D, x: number, y: number)',
        returns = '(x: number, y: number)',
      },
    },
  },
  MOAILayerBridge = {
    type = "class",
    description = "2D transform for connecting transforms across scenes. Useful for HUD overlay items and map pins.",
    childs = {
      init = {
        type = "function",
        description = "Initialize the bridge transform (map coordinates in one layer onto another; useful for rendering screen space objects tied to world space coordinates - map pins, for example).",
        args = '(self: MOAILayerBridge, sourceTransform: MOAITransformBase, sourceLayer: MOAILayer, destLayer: MOAILayer)',
        returns = '()',
      },
    },
  },
  MOAILocationSensor = {
    type = "class",
    description = "Location services sensor.",
    childs = {
      getLocation = {
        type = "function",
        description = "Returns the current information about the physical location.",
        args = '(self: MOAILocationSensor)',
        returns = '(longitude: number, latitude: number, haccuracy: number, altitude: number, vaccuracy: number, speed: number)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when the location changes.",
        args = '(self: MOAILocationSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAILogMgr = {
    type = "class",
    description = "Singleton for managing debug log messages and log level.",
    childs = {
      LOG_NONE = {
        type = "value",
        description = "",
      },
      LOG_ERROR = {
        type = "value",
        description = "",
      },
      LOG_WARNING = {
        type = "value",
        description = "",
      },
      LOG_STATUS = {
        type = "value",
        description = "",
      },
      closeFile = {
        type = "function",
        description = "Resets log output to stdout.",
        args = '()',
        returns = '()',
      },
      isDebugBuild = {
        type = "function",
        description = "Returns a boolean value indicating whether Moai has been compiles as a debug build or not.",
        args = '()',
        returns = '(isDebugBuild: boolean)',
      },
      log = {
        type = "function",
        description = "Alias for print.",
        args = '(message: string)',
        returns = '()',
      },
      openFile = {
        type = "function",
        description = "Opens a new file to receive log messages.",
        args = '(filename: string)',
        returns = '()',
      },
      registerLogMessage = {
        type = "function",
        description = "Register a format string to handle a log message. Register an empty string to hide messages.",
        args = '(messageID: number [, formatString: string | an [, level: number]])',
        returns = '()',
      },
      setLogLevel = {
        type = "function",
        description = "Set the logging level.",
        args = '(logLevel: number)',
        returns = '()',
      },
      setTypeCheckLuaParams = {
        type = "function",
        description = "Set or clear type checking of parameters passed to lua bound Moai API functions.",
        args = '([check: boolean | false])',
        returns = '()',
      },
    },
  },
  MOAIMemStream = {
    type = "class",
    description = "MOAIMemStream implements an in-memory stream and grows as needed. The mem stream expands on demands by allocating additional 'chunks' or memory. The chunk size may be configured by the user. Note that the chunks are not guaranteed to be contiguous in memory.",
    childs = {
      close = {
        type = "function",
        description = "Close the mem stream and release its buffers.",
        args = '(self: MOAIMemStream)',
        returns = '()',
      },
      open = {
        type = "function",
        description = "Create a mem stream and optionally reserve some memory and set the chunk size by which the stream will grow if additional memory is needed.",
        args = '(self: MOAIMemStream [, reserve: number | 0 [, chunkSize: number | MOAIMemStream]])',
        returns = '(success: boolean)',
      },
    },
  },
  MOAIMesh = {
    type = "class",
    description = "Loads a texture and renders the contents of a vertex buffer. Grid drawing not supported.",
    childs = {
      GL_POINTS = {
        type = "value",
        description = "",
      },
      GL_LINES = {
        type = "value",
        description = "",
      },
      GL_TRIANGLES = {
        type = "value",
        description = "",
      },
      GL_LINE_LOOP = {
        type = "value",
        description = "",
      },
      GL_LINE_STRIP = {
        type = "value",
        description = "",
      },
      GL_TRIANGLE_FAN = {
        type = "value",
        description = "",
      },
      GL_TRIANGLE_STRIP = {
        type = "value",
        description = "",
      },
      setIndexBuffer = {
        type = "function",
        description = "Set the index buffer to render.",
        args = '(self: MOAIMesh, indexBuffer: MOAIIndexBuffer)',
        returns = '()',
      },
      setPenWidth = {
        type = "function",
        description = "Sets the pen with for drawing prims in this vertex buffer. Only valid with prim types GL_LINES, GL_LINE_LOOP, GL_LINE_STRIP.",
        args = '(self: MOAIMesh, penWidth: number)',
        returns = '()',
      },
      setPointSize = {
        type = "function",
        description = "Sets the point size for drawing prims in this vertex buffer. Only valid with prim types GL_POINTS.",
        args = '(self: MOAIMesh, pointSize: number)',
        returns = '()',
      },
      setPrimType = {
        type = "function",
        description = "Sets the prim type the buffer represents.",
        args = '(self: MOAIMesh, primType: number)',
        returns = '()',
      },
      setVertexBuffer = {
        type = "function",
        description = "Set the vertex buffer to render.",
        args = '(self: MOAIMesh, vertexBuffer: MOAIVertexBuffer)',
        returns = '()',
      },
    },
  },
  MOAIMotionSensor = {
    type = "class",
    description = "Gravity/acceleration sensor.",
    childs = {
      getLevel = {
        type = "function",
        description = "Polls the current status of the level sensor.",
        args = '(self: MOAIMotionSensor)',
        returns = '(x: number, y: number, z: number)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when the level changes.",
        args = '(self: MOAIMotionSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAIMultiTexture = {
    type = "class",
    description = "Attay of textures for multi-texturing.",
    childs = {
      reserve = {
        type = "function",
        description = "Reserve or clears indices for textures.",
        args = '(self: MOAITextureBase [, total: number | 0])',
        returns = '()',
      },
      setTexture = {
        type = "function",
        description = "Sets of clears a texture for the given index.",
        args = '(self: MOAITextureBase, index: number [, texture: MOAITextureBase | nil])',
        returns = '()',
      },
    },
  },
  MOAINode = {
    type = "class",
    description = "Base for all attribute bearing Moai objects and dependency graph nodes.",
    childs = {
      clearAttrLink = {
        type = "function",
        description = "Clears an attribute *pull* link - call this from the node receiving the attribute value.",
        args = '(self: MOAINode, attrID: number)',
        returns = '()',
      },
      clearNodeLink = {
        type = "function",
        description = "Clears a dependency on a foreign node.",
        args = '(self: MOAINode, sourceNode: MOAINode)',
        returns = '()',
      },
      forceUpdate = {
        type = "function",
        description = "Evaluates the dependency graph for this node. Typically, the entire active dependency graph is evaluated once per frame, but in some cases it may be desirable to force evaluation of a node to make sure source dependencies are propagated to it immediately.",
        args = '(self: MOAINode)',
        returns = '()',
      },
      getAttr = {
        type = "function",
        description = "Returns the value of the attribute if it exists or nil if it doesn't.",
        args = '(self: MOAINode, attrID: number)',
        returns = '(value: number)',
      },
      getAttrLink = {
        type = "function",
        description = "Returns the link if it exists or nil if it doesn't.",
        args = '(self: MOAINode, attrID: number)',
        returns = '(sourceNode: MOAINode, sourceAttrID: number)',
      },
      moveAttr = {
        type = "function",
        description = "Animate the attribute by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAINode, attrID: number, delta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      scheduleUpdate = {
        type = "function",
        description = "Schedule the node for an update next time the dependency graph is processed. Any depdendent nodes will also be updated.",
        args = '(self: MOAINode)',
        returns = '()',
      },
      seekAttr = {
        type = "function",
        description = "Animate the attribute by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAINode, attrID: number, goal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      setAttr = {
        type = "function",
        description = "Sets the value of an attribute.",
        args = '(self: MOAINode, attrID: number, value: number)',
        returns = '()',
      },
      setAttrLink = {
        type = "function",
        description = "Sets a *pull* attribute connecting an attribute in the node to an attribute in a foreign node.",
        args = '(self: MOAINode, attrID: number, sourceNode: MOAINode [, sourceAttrID: number | attrID])',
        returns = '()',
      },
      setNodeLink = {
        type = "function",
        description = "Creates a dependency between the node and a foreign node without the use of attributes; if the foreign node is updated, the dependent node will be updated after.",
        args = '(self: MOAINode, sourceNode: MOAINode)',
        returns = '()',
      },
    },
  },
  MOAIParser = {
    type = "class",
    description = "Parses strings using a LALR parser. Generates an abstract syntax tree that may then be traversed in Lua.",
    childs = {
      loadFile = {
        type = "function",
        description = "Parses the contents of a file and builds an abstract syntax tree.",
        args = '(self: MOAIParser, filename: string)',
        returns = '(ast: table)',
      },
      loadRules = {
        type = "function",
        description = "Parses the contents of the specified CGT.",
        args = '(self: MOAIParser, filename: string)',
        returns = '()',
      },
      loadString = {
        type = "function",
        description = "Parses the contents of a string and builds an abstract syntax tree.",
        args = '(self: MOAIParser, filename: string)',
        returns = '(ast: table)',
      },
      setCallbacks = {
        type = "function",
        description = "Set Lua syntax tree node handlers for tree traversal.",
        args = '(self: MOAIParser [, onStartNonterminal: function | nil [, onEndNonterminal: function | nil [, onTerminal: function | nil]]])',
        returns = '()',
      },
      traverse = {
        type = "function",
        description = "Top down traversal of the abstract syntax tree.",
        args = '(self: MOAIParser)',
        returns = '()',
      },
    },
  },
  MOAIParticleCallbackPlugin = {
    type = "class",
    description = "Allows custom particle processing via C language callbacks.",
    childs = {
    },
  },
  MOAIParticleDistanceEmitter = {
    type = "class",
    description = "Particle emitter.",
    childs = {
      reset = {
        type = "function",
        description = "Resets the distance travelled. Use this to avoid large emissions when 'warping' the emitter to a new location.",
        args = '(self: MOAIParticleDistanceEmitter)',
        returns = '()',
      },
      setDistance = {
        type = "function",
        description = "Set the travel distance required for new particle emission.",
        args = '(self: MOAIParticleDistanceEmitter, min: number [, max: number | min])',
        returns = '()',
      },
    },
  },
  MOAIParticleEmitter = {
    type = "class",
    description = "Particle emitter.",
    childs = {
      setAngle = {
        type = "function",
        description = "Set the size and angle of the emitter.",
        args = '(self: MOAIParticleEmitter, min: number, max: number)',
        returns = '()',
      },
      setEmission = {
        type = "function",
        description = "Set the size of each emission.",
        args = '(self: MOAIParticleEmitter, min: number [, max: number])',
        returns = '()',
      },
      setMagnitude = {
        type = "function",
        description = "Set the starting magnitude of particles deltas.",
        args = '(self: MOAIParticleEmitter, min: number [, max: number])',
        returns = '()',
      },
      setRadius = {
        type = "function",
        description = "Set the shape and radius of the emitter.",
        args = '(self: MOAIParticleEmitter, radius: number)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set the shape and dimensions of the emitter.",
        args = '(self: MOAIParticleEmitter, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setSystem = {
        type = "function",
        description = "Attaches the emitter to a particle system.",
        args = '(self: MOAIParticleEmitter, system: MOAIParticleSystem)',
        returns = '()',
      },
      surge = {
        type = "function",
        description = "Forces the emission of one or more particles.",
        args = '(self: MOAIParticleEmitter [, total: number | a])',
        returns = '()',
      },
    },
  },
  MOAIParticleForce = {
    type = "class",
    description = "Particle force.",
    childs = {
      FORCE = {
        type = "value",
        description = "",
      },
      GRAVITY = {
        type = "value",
        description = "",
      },
      OFFSET = {
        type = "value",
        description = "",
      },
      initAttractor = {
        type = "function",
        description = "Greater force is exerted on particles as they approach attractor.",
        args = '(self: MOAIParticleForce, radius: number [, magnitude: number])',
        returns = '()',
      },
      initBasin = {
        type = "function",
        description = "Greater force is exerted on particles as they leave attractor.",
        args = '(self: MOAIParticleForce, radius: number [, magnitude: number])',
        returns = '()',
      },
      initLinear = {
        type = "function",
        description = "A constant linear force will be applied to the particles.",
        args = '(self: MOAIParticleForce, x: number [, y: number])',
        returns = '()',
      },
      initRadial = {
        type = "function",
        description = "A constant radial force will be applied to the particles.",
        args = '(self: MOAIParticleForce, magnitude: number)',
        returns = '()',
      },
      setType = {
        type = "function",
        description = "Set the type of force. FORCE will factor in the particle's mass. GRAVITY will ignore the particle's mass. OFFSET will ignore both mass and damping.",
        args = '(self: MOAIParticleForce, type: number)',
        returns = '()',
      },
    },
  },
  MOAIParticlePexPlugin = {
    type = "class",
    description = "Allows custom particle processing derived from .pex file via C language callback.",
    childs = {
      getTextureName = {
        type = "function",
        description = "Return the texture name associated with plugin.",
        args = '(self: MOAIParticlePlugin)',
        returns = '(textureName: string)',
      },
      load = {
        type = "function",
        description = "Create a particle plugin from an XML file",
        args = '(file: String)',
        returns = '(-: MOAIParticlePexPlugin)',
      },
    },
  },
  MOAIParticlePlugin = {
    type = "class",
    description = "Allows custom particle processing.",
    childs = {
      getSize = {
        type = "function",
        description = "Return the particle size expected by the plugin.",
        args = '(self: MOAIParticlePlugin)',
        returns = '(size: number)',
      },
    },
  },
  MOAIParticleScript = {
    type = "class",
    description = "Particle script.",
    childs = {
      PARTICLE_X = {
        type = "value",
        description = "",
      },
      PARTICLE_Y = {
        type = "value",
        description = "",
      },
      PARTICLE_DX = {
        type = "value",
        description = "",
      },
      PARTICLE_DY = {
        type = "value",
        description = "",
      },
      SPRITE_X_LOC = {
        type = "value",
        description = "",
      },
      SPRITE_Y_LOC = {
        type = "value",
        description = "",
      },
      SPRITE_ROT = {
        type = "value",
        description = "",
      },
      SPRITE_X_SCL = {
        type = "value",
        description = "",
      },
      SPRITE_Y_SCL = {
        type = "value",
        description = "",
      },
      SPRITE_RED = {
        type = "value",
        description = "",
      },
      SPRITE_GREEN = {
        type = "value",
        description = "",
      },
      SPRITE_BLUE = {
        type = "value",
        description = "",
      },
      SPRITE_OPACITY = {
        type = "value",
        description = "",
      },
      SPRITE_GLOW = {
        type = "value",
        description = "",
      },
      SPRITE_IDX = {
        type = "value",
        description = "",
      },
      add = {
        type = "function",
        description = "r0 = v0 + v1",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number)',
        returns = '()',
      },
      angleVec = {
        type = "function",
        description = "Load two registers with the X and Y components of a unit vector with a given angle.",
        args = '(self: MOAIParticleScript, r0: number, r1: number, v0: number)',
        returns = '()',
      },
      cycle = {
        type = "function",
        description = "Cycle v0 between v1 and v2.",
        args = '(self: MOAIParticleScript, r0: number, v0: number, self: MOAIParticleScript, r0: number, v0: number, v1: number, v2: number)',
        returns = '()',
      },
      div = {
        type = "function",
        description = "r0 = v0 / v1",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number)',
        returns = '()',
      },
      ease = {
        type = "function",
        description = "Load a register with a value interpolated between two numbers using an ease curve.",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number, easeType: number)',
        returns = '()',
      },
      easeDelta = {
        type = "function",
        description = "Load a register with a value interpolated between two numbers using an ease curve. Apply as a delta.",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number, easeType: number)',
        returns = '()',
      },
      mul = {
        type = "function",
        description = "r0 = v0 * v1",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number)',
        returns = '()',
      },
      norm = {
        type = "function",
        description = "r0 = v0 / |v|	@text	r1 = v1 / |v|	@text	Where |v| == sqrt( v0^2 + v1^2)",
        args = '(self: MOAIParticleScript, r0: number, r1: number, v0: number, v1: number)',
        returns = '()',
      },
      packConst = {
        type = "function",
        description = "Pack a const value into a particle script param.",
        args = '(self: MOAIParticleScript, const: number)',
        returns = '()',
      },
      packReg = {
        type = "function",
        description = "Pack a register index into a particle script param.",
        args = '(self: MOAIParticleScript, regIdx: number)',
        returns = '()',
      },
      rand = {
        type = "function",
        description = "Load a register with a random number from a range.",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number)',
        returns = '()',
      },
      randVec = {
        type = "function",
        description = "Load two registers with the X and Y components of a vector with randomly chosen direction and length.",
        args = '(self: MOAIParticleScript, r0: number, r1: number, v0: number, v1: number)',
        returns = '()',
      },
      set = {
        type = "function",
        description = "Load a value into a register.",
        args = '(self: MOAIParticleScript, r0: number, v0: number)',
        returns = '()',
      },
      sprite = {
        type = "function",
        description = "Push a new sprite for rendering. To render a particle, first call 'sprite' to create a new sprite at the particle's location. Then modify the sprite's registers to create animated effects based on the age of the particle (normalized to its term).",
        args = '(self: MOAIParticleScript, r0: number, v0: number, self: MOAIParticleScript)',
        returns = '()',
      },
      sub = {
        type = "function",
        description = "r0 = v0 - v1",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number)',
        returns = '()',
      },
      time = {
        type = "function",
        description = "Load the normalized age of the particle into a register.",
        args = '(self: MOAIParticleScript, r0: number, v0: number, self: MOAIParticleScript, r0: number)',
        returns = '()',
      },
      wrap = {
        type = "function",
        description = "Wrap v0 between v1 and v2.",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number, v2: number)',
        returns = '()',
      },
      vecAngle = {
        type = "function",
        description = "Compute angle (in degrees) between v0 and v1.",
        args = '(self: MOAIParticleScript, r0: number, v0: number, v1: number)',
        returns = '()',
      },
    },
  },
  MOAIParticleState = {
    type = "class",
    description = "Particle state.",
    childs = {
      clearForces = {
        type = "function",
        description = "Removes all particle forces from the state.",
        args = '(self: MOAIParticleState)',
        returns = '()',
      },
      pushForce = {
        type = "function",
        description = "Adds a force to the state.",
        args = '(self: MOAIParticleState, force: MOAIParticleForce)',
        returns = '()',
      },
      setDamping = {
        type = "function",
        description = "Sets damping for particle physics model.",
        args = '(self: MOAIParticleState, damping: number)',
        returns = '()',
      },
      setInitScript = {
        type = "function",
        description = "Sets the particle script to use for initializing new particles.",
        args = '(self: MOAIParticleState [, script: MOAIParticleScript])',
        returns = '()',
      },
      setMass = {
        type = "function",
        description = "Sets range of masses (chosen randomly) for particles initialized by the state.",
        args = '(self: MOAIParticleState, minMass: number [, maxMass: number | minMass])',
        returns = '()',
      },
      setNext = {
        type = "function",
        description = "Sets the next state (if any).",
        args = '(self: MOAIParticleState [, next: MOAIParticleState | nil])',
        returns = '()',
      },
      setPlugin = {
        type = "function",
        description = "Sets the particle plugin to use for initializing and updating particles.",
        args = '(self: MOAIParticleState [, plugin: MOAIParticlePlugin])',
        returns = '()',
      },
      setRenderScript = {
        type = "function",
        description = "Sets the particle script to use for rendering particles.",
        args = '(self: MOAIParticleState [, script: MOAIParticleScript])',
        returns = '()',
      },
      setTerm = {
        type = "function",
        description = "Sets range of terms (chosen randomly) for particles initialized by the state.",
        args = '(self: MOAIParticleState, minTerm: number [, maxTerm: number | minTerm])',
        returns = '()',
      },
    },
  },
  MOAIParticleSystem = {
    type = "class",
    description = "Particle system.",
    childs = {
      capParticles = {
        type = "function",
        description = "Controls capping vs. wrapping of particles in overflow situation. Capping will prevent emission of additional particles when system is full. Wrapping will overwrite the oldest particles with new particles.",
        args = '(self: MOAIParticleSystem [, cap: boolean | true])',
        returns = '()',
      },
      capSprites = {
        type = "function",
        description = "Controls capping vs. wrapping of sprites.",
        args = '(self: MOAIParticleSystem [, cap: boolean | true])',
        returns = '()',
      },
      clearSprites = {
        type = "function",
        description = "Flushes any existing sprites in system.",
        args = '(self: MOAIParticleSystem)',
        returns = '()',
      },
      getState = {
        type = "function",
        description = "Returns a particle state for an index or nil if none exists.",
        args = '(self: MOAIParticleSystem)',
        returns = '(state: MOAIParticleState)',
      },
      isIdle = {
        type = "function",
        description = "Returns true if the current system is not currently processing any particles.",
        args = '(self: MOAIParticleSystem)',
        returns = '(whether: boolean)',
      },
      pushParticle = {
        type = "function",
        description = "Adds a particle to the system.",
        args = '(self: MOAIParticleSystem [, x: number | 0 [, y: number | 0 [, dx: number | 0 [, dy: number | 0]]]])',
        returns = '(result: boolean)',
      },
      pushSprite = {
        type = "function",
        description = "Adds a sprite to the system. Sprite will persist until particle simulation is begun or 'clearSprites' is called.",
        args = '(self: MOAIParticleSystem, x: number, y: number [, rot: number | 0 [, xScale: number | 1 [, yScale: number | 1]]])',
        returns = '(result: boolean)',
      },
      reserveParticles = {
        type = "function",
        description = "Reserve particle capacity of system.",
        args = '(self: MOAIParticleSystem, nParticles: number, particleSize: number)',
        returns = '()',
      },
      reserveSprites = {
        type = "function",
        description = "Reserve sprite capacity of system.",
        args = '(self: MOAIParticleSystem, nSprites: number)',
        returns = '()',
      },
      reserveStates = {
        type = "function",
        description = "Reserve total number of states for system.",
        args = '(self: MOAIParticleSystem, nStates: number)',
        returns = '()',
      },
      setComputeBounds = {
        type = "function",
        description = "Set the a flag controlling whether the particle system re-computes its bounds every frame.",
        args = '(self: MOAIParticleSystem [, computBounds: boolean | false])',
        returns = '()',
      },
      setSpriteColor = {
        type = "function",
        description = "Set the color of the most recently added sprite.",
        args = '(self: MOAIParticleSystem, r: number, g: number, b: number, a: number)',
        returns = '()',
      },
      setSpriteDeckIdx = {
        type = "function",
        description = "Set the sprite's deck index.",
        args = '(self: MOAIParticleSystem, index: number)',
        returns = '()',
      },
      setState = {
        type = "function",
        description = "Set a particle state.",
        args = '(self: MOAIParticleSystem, index: number, state: MOAIParticleState)',
        returns = '()',
      },
      surge = {
        type = "function",
        description = "Release a batch emission or particles into the system.",
        args = '(self: MOAIParticleSystem [, total: number | 1 [, x: number | 0 [, y: number | 0 [, dx: number | 0 [, dy: number | 0]]]]])',
        returns = '()',
      },
    },
  },
  MOAIParticleTimedEmitter = {
    type = "class",
    description = "Particle emitter.",
    childs = {
      setFrequency = {
        type = "function",
        description = "Set timer frequency.",
        args = '(self: MOAIParticleTimedEmitter, min: number [, max: number])',
        returns = '()',
      },
    },
  },
  MOAIPartition = {
    type = "class",
    description = "Class for optimizing spatial queries against sets of primitives. Configure for performance; default behavior is a simple list.",
    childs = {
      PLANE_XY = {
        type = "value",
        description = "",
      },
      PLANE_XZ = {
        type = "value",
        description = "",
      },
      PLANE_YZ = {
        type = "value",
        description = "",
      },
      clear = {
        type = "function",
        description = "Remove all props from the partition.",
        args = '(self: MOAIPartition)',
        returns = '()',
      },
      insertProp = {
        type = "function",
        description = "Inserts a prop into the partition. A prop can only be in one partition at a time.",
        args = '(self: MOAIPartition, prop: MOAIProp)',
        returns = '()',
      },
      propForPoint = {
        type = "function",
        description = "Returns the prop with the highest priority that contains the given world space point.",
        args = '(self: MOAIPartition, x: number, y: number, z: number [, sortMode: number | SORT_PRIORITY_ASCENDING [, xScale: number | 0 [, yScale: number | 0 [, zScale: number | 0 [, priorityScale: number | 1]]]]])',
        returns = '(prop: MOAIProp)',
      },
      propForRay = {
        type = "function",
        description = "Returns the prop closest to the camera that intersects the given ray",
        args = '(self: MOAIPartition, x: number, y: number, z: number, xdirection: number, ydirection: number, zdirection: number)',
        returns = '(prop: MOAIProp)',
      },
      propListForPoint = {
        type = "function",
        description = "Returns all props under a given world space point.",
        args = '(self: MOAIPartition, x: number, y: number, z: number [, sortMode: number | SORT_NONE [, xScale: number | 0 [, yScale: number | 0 [, zScale: number | 0 [, priorityScale: number | 1]]]]])',
        returns = '()',
      },
      propListForRay = {
        type = "function",
        description = "Returns all props under a given world space point.",
        args = '(self: MOAIPartition, x: number, y: number, z: number, xdirection: number, ydirection: number, zdirection: number)',
        returns = '()',
      },
      propListForRect = {
        type = "function",
        description = "Returns all props under a given world space rect.",
        args = '(self: MOAIPartition, xMin: number, yMin: number, xMax: number, yMax: number [, sortMode: number | SORT_NONE [, xScale: number | 0 [, yScale: number | 0 [, zScale: number | 0 [, priorityScale: number | 1]]]]])',
        returns = '()',
      },
      removeProp = {
        type = "function",
        description = "Removes a prop from the partition.",
        args = '(self: MOAIPartition, prop: MOAIProp)',
        returns = '()',
      },
      reserveLayers = {
        type = "function",
        description = "Reserves a stack of levels in the partition. Levels must be initialized with setLevel (). This will trigger a full rebuild of the partition if it contains any props.",
        args = '(self: MOAIPartition, nLevels: number)',
        returns = '()',
      },
      setLevel = {
        type = "function",
        description = "Initializes a level previously created by reserveLevels (). This will trigger a full rebuild of the partition if it contains any props. Each level is a loose grid. Props of a given size may be placed by the system into any level with cells large enough to accomodate them. The dimensions of a level control how many cells the level contains. If an object goes off of the edge of a level, it will wrap around to the other side. It is possible to model a quad tree by initalizing levels correctly, but for some simulations better structures may be possible.",
        args = '(self: MOAIPartition, levelID: number, cellSize: number, xCells: number, yCells: number)',
        returns = '()',
      },
      setPlane = {
        type = "function",
        description = "Selects the plane the partition will use. If this is different from the current plane then all non-global props will be redistributed. Redistribution works by moving all props to the 'empties' cell and then scheduling them all for a dep node update (which refreshes the prop's bounds and may also flag it as global).",
        args = '(self: MOAIPartition, planeID: number)',
        returns = '()',
      },
    },
  },
  MOAIPartitionResultBuffer = {
    type = "class",
    description = "Class for optimizing spatial queries against sets of primitives. Configure for performance; default behavior is a simple list.",
    childs = {
    },
  },
  MOAIPathFinder = {
    type = "class",
    description = "Object for maintaining pathfinding state.",
    childs = {
      findPath = {
        type = "function",
        description = "Attempts to find an efficient path from the start node to the finish node. May be called incrementally.",
        args = '(self: MOAIPathFinder [, iterations: number])',
        returns = '(more: boolean)',
      },
      getGraph = {
        type = "function",
        description = "Returns the attached graph (if any).",
        args = '(self: MOAIPathFinder)',
        returns = '(graph: MOAIPathGraph)',
      },
      getPathEntry = {
        type = "function",
        description = "Returns a path entry. This is a node ID that may be passed back to the graph to get a location.",
        args = '(self: MOAIPathFinder, index: number)',
        returns = '(entry: number)',
      },
      getPathSize = {
        type = "function",
        description = "Returns the size of the path (in nodes).",
        args = '(self: MOAIPathFinder)',
        returns = '(size: number)',
      },
      init = {
        type = "function",
        description = "Specify the ID of the start and target node.",
        args = '(self: MOAIPathFinder, startNodeID: number, targetNodeID: number)',
        returns = '()',
      },
      reserveTerrainWeights = {
        type = "function",
        description = "Specify the size of the terrain weight vector. ",
        args = '(self: MOAIPathFinder [, size: number | 0])',
        returns = '()',
      },
      setFlags = {
        type = "function",
        description = "Set flags to use for pathfinding. These are graph specific flags provided by the graph implementation.",
        args = '(self: MOAIPathFinder [, heuristic: number])',
        returns = '()',
      },
      setGraph = {
        type = "function",
        description = "Set graph data to use for pathfinding. ",
        args = '(self: MOAIPathFinder [, grid: MOAIGrid | nil])',
        returns = '()',
      },
      setHeuristic = {
        type = "function",
        description = "Set heuristic to use for pathfinding. This is a const provided by the graph implementation being used.",
        args = '(self: MOAIPathFinder [, heuristic: number])',
        returns = '()',
      },
      setTerrainDeck = {
        type = "function",
        description = "Set terrain deck to use with graph.",
        args = '(self: MOAIPathFinder [, terrainDeck: MOAIPathTerrainDeck | nil])',
        returns = '()',
      },
      setTerrainScale = {
        type = "function",
        description = "Set a component of the terrain scale vector.",
        args = '(self: MOAIPathFinder, index: number [, deltaScale: number | 0 [, penaltyScale: number | 0]])',
        returns = '()',
      },
      setWeight = {
        type = "function",
        description = "Sets weights to be applied to G and H.",
        args = '(self: MOAIPathFinder [, gWeight: number | 1 [, hWeight: number | 1]])',
        returns = '()',
      },
    },
  },
  MOAIPathTerrainDeck = {
    type = "class",
    description = "Terrain specifications for use with pathfinding graphs. Contains indexed terrain types for graph nodes.",
    childs = {
      getMask = {
        type = "function",
        description = "Returns mask for cell.",
        args = '(self: MOAIPathTerrainDeck, idx: number)',
        returns = '(mask: number)',
      },
      getTerrainVec = {
        type = "function",
        description = "Returns terrain vector for cell.",
        args = '(self: MOAIPathTerrainDeck, idx: number)',
        returns = '()',
      },
      setMask = {
        type = "function",
        description = "Returns mask for cell.",
        args = '(self: MOAIPathTerrainDeck, idx: number, mask: number)',
        returns = '()',
      },
      setTerrainVec = {
        type = "function",
        description = "Sets terrain vector for cell.",
        args = '(self: MOAIPathTerrainDeck, idx: number, ...)',
        returns = '()',
      },
      reserve = {
        type = "function",
        description = "Allocates terrain vectors.",
        args = '(deckSize: number, terrainVecSize: number)',
        returns = '()',
      },
    },
  },
  MOAIPointerSensor = {
    type = "class",
    description = "Pointer sensor.",
    childs = {
      getLoc = {
        type = "function",
        description = "Returns the location of the pointer on the screen.",
        args = '(self: MOAIPointerSensor)',
        returns = '(x: number, y: number)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when the pointer location changes.",
        args = '(self: MOAIPointerSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAIProp = {
    type = "class",
    description = "Base class for props.",
    childs = {
      FRAME_FROM_DECK = {
        type = "value",
        description = "",
      },
      FRAME_FROM_PARENT = {
        type = "value",
        description = "",
      },
      FRAME_FROM_SELF = {
        type = "value",
        description = "",
      },
      BLEND_NORMAL = {
        type = "value",
        description = "",
      },
      BLEND_ADD = {
        type = "value",
        description = "",
      },
      BLEND_MULTIPLY = {
        type = "value",
        description = "",
      },
      GL_ONE = {
        type = "value",
        description = "",
      },
      GL_ZERO = {
        type = "value",
        description = "",
      },
      GL_DST_ALPHA = {
        type = "value",
        description = "",
      },
      GL_DST_COLOR = {
        type = "value",
        description = "",
      },
      GL_SRC_COLOR = {
        type = "value",
        description = "",
      },
      GL_ONE_MINUS_DST_ALPHA = {
        type = "value",
        description = "",
      },
      GL_ONE_MINUS_DST_COLOR = {
        type = "value",
        description = "",
      },
      GL_ONE_MINUS_SRC_ALPHA = {
        type = "value",
        description = "",
      },
      GL_ONE_MINUS_SRC_COLOR = {
        type = "value",
        description = "",
      },
      GL_SRC_ALPHA = {
        type = "value",
        description = "",
      },
      GL_SRC_ALPHA_SATURATE = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_DISABLE = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_NEVER = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_LESS = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_EQUAL = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_LESS_EQUAL = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_GREATER = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_NOTEQUAL = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_GREATER_EQUAL = {
        type = "value",
        description = "",
      },
      DEPTH_TEST_ALWAYS = {
        type = "value",
        description = "",
      },
      CULL_NONE = {
        type = "value",
        description = "",
      },
      CULL_ALL = {
        type = "value",
        description = "",
      },
      CULL_BACK = {
        type = "value",
        description = "",
      },
      CULL_FRONT = {
        type = "value",
        description = "",
      },
      getBounds = {
        type = "function",
        description = "Return the prop's local bounds or 'nil' if prop bounds is global or missing. The bounds are in model space and will be overidden by the prop's bounds if it's been set (using setBounds ())",
        args = '(self: MOAIProp)',
        returns = '(xMin: number, yMin: number, zMin: number, xMax: number, yMax: number, zMax: number)',
      },
      getWorldBounds = {
        type = "function",
        description = "Return the prop's world bounds or 'nil' if prop bounds is global or missing.",
        args = '(self: MOAIProp)',
        returns = '(xMin: number, yMin: number, zMin: number, xMax: number, yMax: number, zMax: number)',
      },
      getDims = {
        type = "function",
        description = "Return the prop's width and height or 'nil' if prop rect is global.",
        args = '(self: MOAIProp)',
        returns = '(width: number, height: number, depth: number)',
      },
      getGrid = {
        type = "function",
        description = "Get the grid currently connected to the prop.",
        args = '(self: MOAIProp)',
        returns = '(grid: MOAIGrid)',
      },
      getIndex = {
        type = "function",
        description = "Gets the value of the deck indexer.",
        args = '(self: MOAIProp)',
        returns = '(index: number)',
      },
      getPriority = {
        type = "function",
        description = "Returns the current priority of the node or 'nil' if the priority is uninitialized.",
        args = '(self: MOAIProp)',
        returns = '(priority: number)',
      },
      inside = {
        type = "function",
        description = "Returns true if the given world space point falls inside the prop's bounds.",
        args = '(self: MOAIProp, x: number, y: number, z: number [, pad: number])',
        returns = '(isInside: boolean)',
      },
      setBillboard = {
        type = "function",
        description = "If set, prop will face camera when rendering.",
        args = '(self: MOAIProp [, billboard: boolean | false])',
        returns = '()',
      },
      setBlendMode = {
        type = "function",
        description = "Set the blend mode.",
        args = '(self: MOAIProp)',
        returns = '()',
      },
      setBounds = {
        type = "function",
        description = "Sets or clears the partition bounds override.",
        args = '(self: MOAIProp)',
        returns = '()',
      },
      setCullMode = {
        type = "function",
        description = "Sets and enables face culling.",
        args = '(self: MOAIProp [, cullMode: number | MOAIProp])',
        returns = '()',
      },
      setDeck = {
        type = "function",
        description = "Sets or clears the deck to be indexed by the prop.",
        args = '(self: MOAIProp [, deck: MOAIDeck | nil])',
        returns = '()',
      },
      setDepthMask = {
        type = "function",
        description = "Disables or enables depth writing.",
        args = '(self: MOAIProp [, depthMask: boolean | true])',
        returns = '()',
      },
      setDepthTest = {
        type = "function",
        description = "Sets and enables depth testing (assuming depth buffer is present).",
        args = '(self: MOAIProp [, depthFunc: number | MOAIProp])',
        returns = '()',
      },
      setExpandForSort = {
        type = "function",
        description = "Used when drawing with a layout scheme (i.e. MOAIGrid). Expanding for sort causes the prop to emit a sub-prim for each component of the layout. For example, when attaching a MOAIGrid to a prop, each cell of the grid will be added to the render queue for sorting against all other props and sub-prims. This is obviously less efficient, but still more efficient then using an separate prop for each cell or object.",
        args = '(self: MOAIProp, expandForSort: boolean)',
        returns = '()',
      },
      setGrid = {
        type = "function",
        description = "Sets or clears the prop's grid indexer. The grid indexer (if any) will override the standard indexer.",
        args = '(self: MOAIProp [, grid: MOAIGrid | nil])',
        returns = '()',
      },
      setGridScale = {
        type = "function",
        description = "Scale applied to deck items before rendering to grid cell.",
        args = '(self: MOAIProp [, xScale: number | 1 [, yScale: number | 1]])',
        returns = '()',
      },
      setIndex = {
        type = "function",
        description = "Set the prop's index into its deck.",
        args = '(self: MOAIProp [, index: number | 1])',
        returns = '()',
      },
      setParent = {
        type = "function",
        description = "This method has been deprecated. Use MOAINode setAttrLink instead.",
        args = '(self: MOAIProp [, parent: MOAINode | nil])',
        returns = '()',
      },
      setPriority = {
        type = "function",
        description = "Sets or clears the node's priority. Clear the priority to have MOAIPartition automatically assign a priority to a node when it is added.",
        args = '(self: MOAIProp [, priority: number | nil])',
        returns = '()',
      },
      setRemapper = {
        type = "function",
        description = "Set a remapper for this prop to use when drawing deck members.",
        args = '(self: MOAIProp [, remapper: MOAIDeckRemapper | nil])',
        returns = '()',
      },
      setScissorRect = {
        type = "function",
        description = "Set or clear the prop's scissor rect.",
        args = '(self: MOAIProp [, scissorRect: MOAIScissorRect | nil])',
        returns = '()',
      },
      setShader = {
        type = "function",
        description = "Sets or clears the prop's shader. The prop's shader takes precedence over any shader specified by the deck or its elements.",
        args = '(self: MOAIProp [, shader: MOAIShader | nil])',
        returns = '()',
      },
      setTexture = {
        type = "function",
        description = "Set or load a texture for this prop. The prop's texture will override the deck's texture.",
        args = '(self: MOAIProp, texture: variant [, transform: number])',
        returns = '(texture: MOAIGfxState)',
      },
      setUVTransform = {
        type = "function",
        description = "Sets or clears the prop's UV transform.",
        args = '(self: MOAIProp [, transform: MOAITransformBase | nil])',
        returns = '()',
      },
      setVisible = {
        type = "function",
        description = "Sets or clears the prop's visibility.",
        args = '(self: MOAIProp [, visible: boolean | true])',
        returns = '()',
      },
    },
  },
  MOAIProp2D = {
    type = "class",
    description = "2D prop.",
    childs = {
      getRect = {
        type = "function",
        description = "Return the prop's local bounds or 'nil' if prop bounds is global or missing. The bounds are in model space and will be overidden by the prop's frame if it's been set (using setFrame ())",
        args = '(self: MOAIProp2D)',
        returns = '(xMin: number, yMin: number, xMax: number, yMax: number)',
      },
      getGrid = {
        type = "function",
        description = "Get the grid currently connected to the prop.",
        args = '(self: MOAIProp2D)',
        returns = '(grid: MOAIGrid)',
      },
      getIndex = {
        type = "function",
        description = "Gets the value of the deck indexer.",
        args = '(self: MOAIProp2D)',
        returns = '(index: number)',
      },
      getPriority = {
        type = "function",
        description = "Returns the current priority of the node or 'nil' if the priority is uninitialized.",
        args = '(self: MOAIProp2D)',
        returns = '(priority: number)',
      },
      inside = {
        type = "function",
        description = "Returns true if the given world space point falls inside the prop's bounds.",
        args = '(self: MOAIProp2D, x: number, y: number, z: number [, pad: number])',
        returns = '(isInside: boolean)',
      },
      setBlendMode = {
        type = "function",
        description = "Set the blend mode.",
        args = '(self: MOAIProp2D)',
        returns = '()',
      },
      setCullMode = {
        type = "function",
        description = "Sets and enables face culling.",
        args = '(self: MOAIProp2D [, cullMode: number | MOAIProp2D])',
        returns = '()',
      },
      setDeck = {
        type = "function",
        description = "Sets or clears the deck to be indexed by the prop.",
        args = '(self: MOAIProp2D [, deck: MOAIDeck | nil])',
        returns = '()',
      },
      setDepthMask = {
        type = "function",
        description = "Disables or enables depth writing.",
        args = '(self: MOAIProp2D [, depthMask: boolean | true])',
        returns = '()',
      },
      setDepthTest = {
        type = "function",
        description = "Sets and enables depth testing (assuming depth buffer is present).",
        args = '(self: MOAIProp2D [, depthFunc: number | MOAIProp2D])',
        returns = '()',
      },
      setExpandForSort = {
        type = "function",
        description = "Used when drawing with a layout scheme (i.e. MOAIGrid). Expanding for sort causes the prop to emit a sub-prim for each component of the layout. For example, when attaching a MOAIGrid to a prop, each cell of the grid will be added to the render queue for sorting against all other props and sub-prims. This is obviously less efficient, but still more efficient then using an separate prop for each cell or object.",
        args = '(self: MOAIProp2D, expandForSort: boolean)',
        returns = '()',
      },
      setFrame = {
        type = "function",
        description = "Sets the fitting frame of the prop.",
        args = '(self: MOAIProp2D)',
        returns = '()',
      },
      setGrid = {
        type = "function",
        description = "Sets or clears the prop's grid indexer. The grid indexer (if any) will override the standard indexer.",
        args = '(self: MOAIProp2D [, grid: MOAIGrid | nil])',
        returns = '()',
      },
      setGridScale = {
        type = "function",
        description = "Scale applied to deck items before rendering to grid cell.",
        args = '(self: MOAIProp2D [, xScale: number | 1 [, yScale: number | 1]])',
        returns = '()',
      },
      setIndex = {
        type = "function",
        description = "Set the prop's index into its deck.",
        args = '(self: MOAIProp2D [, index: number | 1])',
        returns = '()',
      },
      setParent = {
        type = "function",
        description = "This method has been deprecated. Use MOAINode setAttrLink instead.",
        args = '(self: MOAIProp2D [, parent: MOAINode | nil])',
        returns = '()',
      },
      setPriority = {
        type = "function",
        description = "Sets or clears the node's priority. Clear the priority to have MOAIPartition automatically assign a priority to a node when it is added.",
        args = '(self: MOAIProp2D [, priority: number | nil])',
        returns = '()',
      },
      setRemapper = {
        type = "function",
        description = "Set a remapper for this prop to use when drawing deck members.",
        args = '(self: MOAIProp2D [, remapper: MOAIDeckRemapper | nil])',
        returns = '()',
      },
      setShader = {
        type = "function",
        description = "Sets or clears the prop's shader. The prop's shader takes precedence over any shader specified by the deck or its elements.",
        args = '(self: MOAIProp2D [, shader: MOAIShader | nil])',
        returns = '()',
      },
      setTexture = {
        type = "function",
        description = "Set or load a texture for this prop. The prop's texture will override the deck's texture.",
        args = '(self: MOAIProp2D, texture: variant [, transform: number])',
        returns = '(texture: MOAIGfxState)',
      },
      setUVTransform = {
        type = "function",
        description = "Sets or clears the prop's UV transform.",
        args = '(self: MOAIProp2D [, transform: MOAITransformBase | nil])',
        returns = '()',
      },
      setVisible = {
        type = "function",
        description = "Sets or clears the prop's visibility.",
        args = '(self: MOAIProp2D [, visible: boolean | true])',
        returns = '()',
      },
    },
  },
  MOAIRenderable = {
    type = "class",
    description = "Abstract base class for objects that can be rendered by MOAIRenderMgr.",
    childs = {
    },
  },
  MOAIRenderMgr = {
    type = "class",
    description = "MOAIRenderMgr is responsible for drawing a list of MOAIRenderable objects. MOAIRenderable is the base class for any object that can be drawn. This includes MOAIProp and MOAILayer. To use MOAIRenderMgr pass a table of MOAIRenderable objects to MOAIRenderMgr.setRenderTable (). The table will usually be a stack of MOAILayer objects. The contents of the table will be rendered the next time a frame is drawn. Note that the table must be an array starting with index 1. Objects will be rendered counting from the base index until 'nil' is encountered. The render table may include other tables as entries. These must also be arrays indexed from 1.",
    childs = {
      getPerformanceDrawCount = {
        type = "function",
        description = "Returns the number of draw calls last frame.	",
        args = '()',
        returns = '(count: number)',
      },
      getRenderTable = {
        type = "function",
        description = "Returns the table currently being used for rendering.",
        args = '()',
        returns = '(renderTable: table)',
      },
      grabNextFrame = {
        type = "function",
        description = "Save the next frame rendered to ",
        args = '(image: MOAIImage, callback: function)',
        returns = '(renderTable: table)',
      },
      setRenderTable = {
        type = "function",
        description = "Sets the table to be used for rendering. This should be an array indexed from 1 consisting of MOAIRenderable objects and sub-tables. Objects will be rendered in order starting from index 1 and continuing until 'nil' is encountered.",
        args = '(renderTable: table)',
        returns = '()',
      },
    },
  },
  MOAIScissorRect = {
    type = "class",
    description = "Class for clipping props when drawing.",
    childs = {
      getRect = {
        type = "function",
        description = "Return the extents of the scissor rect.",
        args = '(self: MOAIScissorRect)',
        returns = '(xMin: number, yMin: number, xMax: number, yMax: number)',
      },
      setRect = {
        type = "function",
        description = "Sets the extents of the scissor rect.",
        args = '(x1: number, y1: number, x2: number, y2: number)',
        returns = '()',
      },
      setScissorRect = {
        type = "function",
        description = "Set or clear the parent scissor rect.",
        args = '(self: MOAIProp [, parent: MOAIScissorRect | nil])',
        returns = '()',
      },
    },
  },
  MOAIScriptDeck = {
    type = "class",
    description = "Scriptable deck object.",
    childs = {
      setDrawCallback = {
        type = "function",
        description = "Sets the callback to be issued when draw events occur. The callback's parameters are ( number index, number xOff, number yOff, number xScale, number yScale ).",
        args = '(self: MOAIScriptDeck, callback: function)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set the model space dimensions of the deck's default rect.",
        args = '(self: MOAIScriptDeck, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setRectCallback = {
        type = "function",
        description = "Sets the callback to be issued when the size of a deck item needs to be determined. The callback's parameters are ( number index ).",
        args = '(self: MOAIScriptDeck, callback: function)',
        returns = '()',
      },
      setTotalRectCallback = {
        type = "function",
        description = "Sets the callback to be issued when the size of a deck item needs to be determined. The callback's parameters are ( ).",
        args = '(self: MOAIScriptDeck, callback: function)',
        returns = '()',
      },
    },
  },
  MOAIScriptNode = {
    type = "class",
    description = "User scriptable dependency node. User may specify Lua callback to handle node updating as well as custom floating point attributes.",
    childs = {
      reserveAttrs = {
        type = "function",
        description = "Reserve memory for custom attributes and initializes them to 0.",
        args = '(self: MOAIScriptNode, nAttributes: number)',
        returns = '()',
      },
      setCallback = {
        type = "function",
        description = "Sets a Lua function to be called whenever the node is updated.",
        args = '(self: MOAIScriptNode, onUpdate: function)',
        returns = '()',
      },
    },
  },
  MOAISensor = {
    type = "class",
    description = "Base class for sensors.",
    childs = {
    },
  },
  MOAISerializer = {
    type = "class",
    description = "Manages serialization state of Lua tables and Moai objects. The serializer will produce a Lua script that, when executed, will return the ordered list of objects added to it using the serialize () function.",
    childs = {
      exportToFile = {
        type = "function",
        description = "Exports the contents of the serializer to a file.",
        args = '(self: MOAISerializer, filename: string)',
        returns = '()',
      },
      exportToString = {
        type = "function",
        description = "Exports the contents of the serializer to a string.",
        args = '(self: MOAISerializer)',
        returns = '(result: string)',
      },
      serialize = {
        type = "function",
        description = "Adds a table or object to the serializer.",
        args = '(self: MOAISerializer, data: table)',
        returns = '()',
      },
      serializeToFile = {
        type = "function",
        description = "Serializes the specified table or object to a file.",
        args = '(filename: string, data: table)',
        returns = '()',
      },
      serializeToString = {
        type = "function",
        description = "Serializes the specified table or object to a string.",
        args = '(data: table)',
        returns = '(serialized: string)',
      },
    },
  },
  MOAIShader = {
    type = "class",
    description = "Programmable shader class.",
    childs = {
      UNIFORM_COLOR = {
        type = "value",
        description = "",
      },
      UNIFORM_FLOAT = {
        type = "value",
        description = "",
      },
      UNIFORM_INT = {
        type = "value",
        description = "",
      },
      UNIFORM_PEN_COLOR = {
        type = "value",
        description = "",
      },
      UNIFORM_SAMPLER = {
        type = "value",
        description = "",
      },
      UNIFORM_TRANSFORM = {
        type = "value",
        description = "",
      },
      UNIFORM_VIEW_PROJ = {
        type = "value",
        description = "",
      },
      UNIFORM_WORLD = {
        type = "value",
        description = "",
      },
      UNIFORM_WORLD_VIEW_PROJ = {
        type = "value",
        description = "",
      },
      clearUniform = {
        type = "function",
        description = "Clears a uniform mapping.",
        args = '(self: MOAIShader, idx: number)',
        returns = '()',
      },
      declareUniform = {
        type = "function",
        description = "Declares a uniform mapping.",
        args = '(self: MOAIShader, idx: number, name: string [, type: number])',
        returns = '()',
      },
      declareUniformFloat = {
        type = "function",
        description = "Declares an float uniform.",
        args = '(self: MOAIShader, idx: number, name: string [, value: number | 0])',
        returns = '()',
      },
      declareUniformInt = {
        type = "function",
        description = "Declares an integer uniform.",
        args = '(self: MOAIShader, idx: number, name: string [, value: number | 0])',
        returns = '()',
      },
      declareUniformSampler = {
        type = "function",
        description = "Declares an uniform to be used as a texture unit index. This uniform is internally an int, but when loaded into the shader the number one subtracted from its value. This allows the user to maintain consistency with Lua's convention of indexing from one.",
        args = '(self: MOAIShader, idx: number, name: string [, textureUnit: number | 1])',
        returns = '()',
      },
      load = {
        type = "function",
        description = "Load a shader program.",
        args = '(self: MOAIShader, vertexShaderSource: string, fragmentShaderSource: string)',
        returns = '()',
      },
      reserveUniforms = {
        type = "function",
        description = "Reserve shader uniforms.",
        args = '(self: MOAIShader [, nUniforms: number | 0])',
        returns = '()',
      },
      setVertexAttribute = {
        type = "function",
        description = "Names a shader vertex attribute.",
        args = '(self: MOAIShader, index: number, name: string)',
        returns = '()',
      },
    },
  },
  MOAIShaderMgr = {
    type = "class",
    description = "Shader presets.",
    childs = {
      getShader = {
        type = "function",
        description = "Return one of the built-in shaders.",
        args = '(shaderID: number)',
        returns = '()',
      },
    },
  },
  MOAISim = {
    type = "class",
    description = "Sim timing and settings class.",
    childs = {
      EVENT_FINALIZE = {
        type = "value",
        description = "",
      },
      SIM_LOOP_FORCE_STEP = {
        type = "value",
        description = "",
      },
      SIM_LOOP_ALLOW_BOOST = {
        type = "value",
        description = "",
      },
      SIM_LOOP_ALLOW_SPIN = {
        type = "value",
        description = "",
      },
      SIM_LOOP_NO_DEFICIT = {
        type = "value",
        description = "",
      },
      SIM_LOOP_NO_SURPLUS = {
        type = "value",
        description = "",
      },
      SIM_LOOP_RESET_CLOCK = {
        type = "value",
        description = "",
      },
      SIM_LOOP_ALLOW_SOAK = {
        type = "value",
        description = "",
      },
      LOOP_FLAGS_DEFAULT = {
        type = "value",
        description = "",
      },
      LOOP_FLAGS_FIXED = {
        type = "value",
        description = "",
      },
      LOOP_FLAGS_MULTISTEP = {
        type = "value",
        description = "",
      },
      DEFAULT_STEPS_PER_SECOND = {
        type = "value",
        description = "Value is 60",
      },
      DEFAULT_BOOST_THRESHOLD = {
        type = "value",
        description = "Value is 3",
      },
      DEFAULT_LONG_DELAY_THRESHOLD = {
        type = "value",
        description = "Value is 10",
      },
      DEFAULT_CPU_BUDGET = {
        type = "value",
        description = "Value is 2",
      },
      DEFAULT_STEP_MULTIPLIER = {
        type = "value",
        description = "Value is 1",
      },
      clearLoopFlags = {
        type = "function",
        description = "Uses the mask provided to clear the loop flags.",
        args = '([mask: number | 0xffffffff])',
        returns = '()',
      },
      crash = {
        type = "function",
        description = "Crashes moai with a null pointer dereference.",
        args = '()',
        returns = '()',
      },
      enterFullscreenMode = {
        type = "function",
        description = "Enters fullscreen mode on the device if possible.",
        args = '()',
        returns = '()',
      },
      exitFullscreenMode = {
        type = "function",
        description = "Exits fullscreen mode on the device if possible.",
        args = '()',
        returns = '()',
      },
      forceGarbageCollection = {
        type = "function",
        description = "Runs the garbage collector repeatedly until no more MOAIObjects can be collected.",
        args = '()',
        returns = '()',
      },
      framesToTime = {
        type = "function",
        description = "Converts the number of frames to time passed in seconds.",
        args = '(frames: number)',
        returns = '(time: number)',
      },
      getDeviceTime = {
        type = "function",
        description = "Gets the raw device clock. This is a replacement for Lua's os.time ().",
        args = '()',
        returns = '(time: number)',
      },
      getElapsedFrames = {
        type = "function",
        description = "Gets the number of frames elapsed since the application was started.",
        args = '()',
        returns = '(frames: number)',
      },
      getElapsedTime = {
        type = "function",
        description = "Gets the number of seconds elapsed since the application was started.",
        args = '()',
        returns = '(time: number)',
      },
      getHistogram = {
        type = "function",
        description = "Generates a histogram of active MOAIObjects and returns it in a table containing object tallies indexed by object class names.",
        args = '()',
        returns = '(histogram: table)',
      },
      getLoopFlags = {
        type = "function",
        description = "Returns the current loop flags.",
        args = '()',
        returns = '(mask: number)',
      },
      getLuaObjectCount = {
        type = "function",
        description = "Gets the total number of objects in memory that inherit MOAILuaObject. Count includes objects that are not bound to the Lua runtime.",
        args = '()',
        returns = '(count: number)',
      },
      getMemoryUsage = {
        type = "function",
        description = "Get the current amount of memory used by MOAI and its subsystems. This will attempt to return reasonable estimates where exact values cannot be obtained. Some fields represent informational fields (i.e. are not double counted in the total, but present to assist debugging) and may be only available on certain platforms (e.g. Windows, etc). These fields begin with a '_' character.",
        args = '()',
        returns = '(usage: table)',
      },
      getPerformance = {
        type = "function",
        description = "Returns an estimated frames per second based on measurements taken at every render.",
        args = '()',
        returns = '(fps: number)',
      },
      getStep = {
        type = "function",
        description = "Gets the amount of time (in seconds) that it takes for one frame to pass.",
        args = '()',
        returns = '(size: number)',
      },
      openWindow = {
        type = "function",
        description = "Opens a new window for the application to render on. This must be called before any rendering can be done, and it must only be called once.",
        args = '(title: string, width: number, height: number)',
        returns = '()',
      },
      pauseTimer = {
        type = "function",
        description = "Pauses or unpauses the device timer, preventing any visual updates (rendering) while paused.",
        args = '(pause: boolean)',
        returns = '()',
      },
      reportHistogram = {
        type = "function",
        description = "Generates a histogram of active MOAIObjects.",
        args = '()',
        returns = '()',
      },
      reportLeaks = {
        type = "function",
        description = "Analyze the currently allocated MOAI objects and create a textual report of where they were declared, and what Lua references (if any) can be found. NOTE: This is incredibly slow, so only use to debug leaking memory issues.",
        args = '(clearAfter: bool)',
        returns = '()',
      },
      setBoostThreshold = {
        type = "function",
        description = "Sets the boost threshold, a scalar applied to step. If the gap between simulation time and device time is greater than the step size multiplied by the boost threshold and MOAISim.SIM_LOOP_ALLOW_BOOST is set in the loop flags, then the simulation is updated once with a large, variable step to make up the entire gap.",
        args = '([boostThreshold: number | DEFAULT_BOOST_THRESHOLD])',
        returns = '()',
      },
      setCpuBudget = {
        type = "function",
        description = "Sets the amount of time (given in simulation steps) to allow for updating the simulation.",
        args = '(budget: number)',
        returns = '()',
      },
      setHistogramEnabled = {
        type = "function",
        description = "Enable tracking of every MOAILuaObject so that an object count histogram may be generated.",
        args = '([enable: bool | false])',
        returns = '()',
      },
      setLeakTrackingEnabled = {
        type = "function",
        description = "Enable extra memory book-keeping measures that allow all MOAI objects to be tracked back to their point of allocation (in Lua). Use together with MOAISim.reportLeaks() to determine exactly where your memory usage is being created. NOTE: This is very expensive in terms of both CPU and the extra memory associated with the stack info book-keeping. Use only when tracking down leaks.",
        args = '([enable: bool | false])',
        returns = '()',
      },
      setLongDelayThreshold = {
        type = "function",
        description = "Sets the long delay threshold. If the sim step falls behind the given threshold, the deficit will be dropped: sim will neither spin nor boost to catch up.",
        args = '([longDelayThreshold: number | DEFAULT_LONG_DELAY_THRESHOLD])',
        returns = '()',
      },
      setLoopFlags = {
        type = "function",
        description = "Fine tune behavior of the simulation loop. MOAISim.SIM_LOOP_ALLOW_SPIN will allow the simulation step to run multiple times per update to try and catch up with device time, but will abort if processing the simulation exceeds the configfured step time. MOAISim.SIM_LOOP_ALLOW_BOOST will permit a *variable* update step if simulation time falls too far behind device time (based on the boost threshold). Be warned: this can wreak havok with physics and stepwise animation or game AI.",
        args = '([flags: number])',
        returns = '()',
      },
      setLuaAllocLogEnabled = {
        type = "function",
        description = "Toggles log messages from Lua allocator.",
        args = '([enable: boolean | false])',
        returns = '()',
      },
      setStep = {
        type = "function",
        description = "Sets the size of each simulation step (in seconds).",
        args = '(step: number)',
        returns = '()',
      },
      setStepMultiplier = {
        type = "function",
        description = "Runs the simulation multiple times per step (but with a fixed step size). This is used to speed up the simulation without providing a larger step size (which could destabilize physics simulation).",
        args = '(count: number)',
        returns = '()',
      },
      setTimerError = {
        type = "function",
        description = "Sets the tolerance for timer error. This is a multiplier of step. Timer error tolerance is step * timerError.",
        args = '(timerError: number)',
        returns = '()',
      },
      setTraceback = {
        type = "function",
        description = "Sets the function to call when a traceback occurs in lua",
        args = '(callback: function)',
        returns = '()',
      },
      timeToFrames = {
        type = "function",
        description = "Converts the number of time passed in seconds to frames.",
        args = '(time: number)',
        returns = '(frames: number)',
      },
    },
  },
  MOAIStaticGlyphCache = {
    type = "class",
    description = "This is the default implementation of a static glyph cache. All is does is accept an image via setImage () and create a set of textures from that image. It does not implement getImage ().",
    childs = {
    },
  },
  MOAIStream = {
    type = "class",
    description = "Interface for reading/writing binary data.",
    childs = {
      SEEK_CUR = {
        type = "value",
        description = "",
      },
      SEEK_END = {
        type = "value",
        description = "",
      },
      SEEK_SET = {
        type = "value",
        description = "",
      },
      flush = {
        type = "function",
        description = "Forces any remaining buffered data into the stream.",
        args = '(self: MOAIStream)',
        returns = '()',
      },
      getCursor = {
        type = "function",
        description = "Returns the current cursor position in the stream.",
        args = '(self: MOAIStream)',
        returns = '(cursor: number)',
      },
      getLength = {
        type = "function",
        description = "Returns the length of the stream.",
        args = '(self: MOAIStream)',
        returns = '(length: number)',
      },
      read = {
        type = "function",
        description = "Reads bytes from the stream.",
        args = '(self: MOAIStream)',
        returns = '(size: number, bytes: string, size: number)',
      },
      read8 = {
        type = "function",
        description = "Reads a signed 8-bit value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      read16 = {
        type = "function",
        description = "Reads a signed 16-bit value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      read32 = {
        type = "function",
        description = "Reads a signed 32-bit value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      readDouble = {
        type = "function",
        description = "Reads a 64-bit floating point value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      readFloat = {
        type = "function",
        description = "Reads a 32-bit floating point value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      readFormat = {
        type = "function",
        description = "Reads a series of values from the stream given a format string. Valid tokens for the format string are: u8 u16 u32 f d s8 s16 s32. Tokens may be optionally separeted by spaces of commas.",
        args = '(self: MOAIStream, format: string)',
        returns = '(size: number)',
      },
      readU8 = {
        type = "function",
        description = "Reads an unsigned 8-bit value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      readU16 = {
        type = "function",
        description = "Reads an unsigned 16-bit value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      readU32 = {
        type = "function",
        description = "Reads an unsigned 32-bit value from the stream.",
        args = '(self: MOAIStream)',
        returns = '(value: number, size: number)',
      },
      seek = {
        type = "function",
        description = "Repositions the cursor in the stream.",
        args = '(self: MOAIStream, offset: number [, mode: number])',
        returns = '()',
      },
      write = {
        type = "function",
        description = "Write binary data to the stream.",
        args = '(self: MOAIStream [, size: number | the])',
        returns = '(bytes: string, size: number)',
      },
      write8 = {
        type = "function",
        description = "Writes a signed 8-bit value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
      write16 = {
        type = "function",
        description = "Writes a signed 16-bit value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
      write32 = {
        type = "function",
        description = "Writes a signed 32-bit value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
      writeDouble = {
        type = "function",
        description = "Writes a 64-bit floating point value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
      write32 = {
        type = "function",
        description = "Writes a 32-bit floating point value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
      writeFormat = {
        type = "function",
        description = "Writes a series of values to the stream given a format string. See 'readFormat' for a list of valid format tokens.",
        args = '(self: MOAIStream, format: string, ...)',
        returns = '(size: number)',
      },
      writeStream = {
        type = "function",
        description = "Reads bytes from the given stream into the calling stream.",
        args = '(self: MOAIStream, stream: MOAIStream [, size: number | the])',
        returns = '(size: number)',
      },
      writeU8 = {
        type = "function",
        description = "Writes an unsigned 8-bit value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
      writeU16 = {
        type = "function",
        description = "Writes an unsigned 16-bit value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
      writeU32 = {
        type = "function",
        description = "Writes an unsigned 32-bit value to the stream.",
        args = '(self: MOAIStream, value: number)',
        returns = '(size: number)',
      },
    },
  },
  MOAIStreamReader = {
    type = "class",
    description = "MOAIStreamReader may be attached to another stream for the purpose of decoding and/or decompressing bytes read from that stream using a given algorithm (such as base64 or 'deflate'). ",
    childs = {
      close = {
        type = "function",
        description = "Detach the target stream. (This only detatches the target from the formatter; it does not also close the target stream).",
        args = '(self: MOAIStreamReader)',
        returns = '()',
      },
      openBase64 = {
        type = "function",
        description = "Open a base 64 formatted stream for reading (i.e. decode bytes from base64).",
        args = '(self: MOAIStreamReader, target: MOAIStream)',
        returns = '(success: boolean)',
      },
      openDeflate = {
        type = "function",
        description = "Open a 'deflate' formatted stream for reading (i.e. decompress bytes using the 'deflate' algorithm).",
        args = '(self: MOAIStreamReader, target: MOAIStream)',
        returns = '(success: boolean)',
      },
    },
  },
  MOAIStreamWriter = {
    type = "class",
    description = "MOAIStreamWriter may be attached to another stream for the purpose of encoding and/or compressing bytes written to that stream using a given algorithm (such as base64 or 'deflate'). ",
    childs = {
      close = {
        type = "function",
        description = "Flush any remaining buffered data and detach the target stream. (This only detatches the target from the formatter; it does not also close the target stream).",
        args = '(self: MOAIStreamWriter)',
        returns = '()',
      },
      openBase64 = {
        type = "function",
        description = "Open a base 64 formatted stream for writing (i.e. encode bytes to base64).",
        args = '(self: MOAIStreamWriter, target: MOAIStream)',
        returns = '(success: boolean)',
      },
      openDeflate = {
        type = "function",
        description = "Open a 'deflate' formatted stream for writing (i.e. compress bytes using the 'deflate' algorithm).",
        args = '(self: MOAIStreamWriter, target: MOAIStream)',
        returns = '(success: boolean)',
      },
    },
  },
  MOAIStretchPatch2D = {
    type = "class",
    description = "Moai implementation of a 9-patch. Textured quad with any number of stretchable and non-stretchable 'bands.' Grid drawing not supported.",
    childs = {
      reserveColumns = {
        type = "function",
        description = "Reserve total columns in patch.",
        args = '(self: MOAIStretchPatch2D, nColumns: number)',
        returns = '()',
      },
      reserveRows = {
        type = "function",
        description = "Reserve total rows in patch.",
        args = '(self: MOAIStretchPatch2D, nRows: number)',
        returns = '()',
      },
      reserveUVRects = {
        type = "function",
        description = "Reserve total UV rects in patch. When a patch is indexed it will change its UV rects.",
        args = '(self: MOAIStretchPatch2D, nUVRects: number)',
        returns = '()',
      },
      setColumn = {
        type = "function",
        description = "Set the stretch properties of a patch column.",
        args = '(self: MOAIStretchPatch2D, idx: number, weight: number, conStretch: boolean)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set the model space dimensions of the patch.",
        args = '(self: MOAIStretchPatch2D, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setRow = {
        type = "function",
        description = "Set the stretch properties of a patch row.",
        args = '(self: MOAIStretchPatch2D, idx: number, weight: number, conStretch: boolean)',
        returns = '()',
      },
      setUVRect = {
        type = "function",
        description = "Set the UV space dimensions of the patch.",
        args = '(self: MOAIStretchPatch2D, idx: number, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
    },
  },
  MOAISurfaceDeck2D = {
    type = "class",
    description = "Deck of surface edge lists. Unused in this version of Moai.",
    childs = {
      reserveSurfaceLists = {
        type = "function",
        description = "Reserve surface lists for deck.",
        args = '(self: MOAISurfaceDeck2D, nLists: number)',
        returns = '()',
      },
      reserveSurfaces = {
        type = "function",
        description = "Reserve surfaces for a given list in deck.",
        args = '(self: MOAISurfaceDeck2D, idx: number, nSurfaces: number)',
        returns = '()',
      },
      setSurface = {
        type = "function",
        description = "Set a surface in a surface list.",
        args = '(self: MOAISurfaceDeck2D, idx: number, surfaceIdx: number, x0: number, y0: number, x1: number, y1: number)',
        returns = '()',
      },
    },
  },
  MOAITextBox = {
    type = "class",
    description = "The text box manages styling, laying out and displaying text. You can attach named styles to the text box to be applied to the text using style escapes. You can also inline style escapes to control color. Style escapes may be nested.</p>",
    childs = {
      LEFT_JUSTIFY = {
        type = "value",
        description = "",
      },
      CENTER_JUSTIFY = {
        type = "value",
        description = "",
      },
      RIGHT_JUSTIFY = {
        type = "value",
        description = "",
      },
      clearHighlights = {
        type = "function",
        description = "Removes all highlights currently associated with the text box.",
        args = '(self: MOAITextBox)',
        returns = '()',
      },
      getGlyphScale = {
        type = "function",
        description = "Returns the current glyph scale.",
        args = '(self: MOAITextBox)',
        returns = '(glyphScale: number)',
      },
      getLineSpacing = {
        type = "function",
        description = "Returns the spacing between lines (in pixels).",
        args = '(self: MOAITextBox)',
        returns = '(lineScale: number)',
      },
      getRect = {
        type = "function",
        description = "Returns the two dimensional boundary of the text box.",
        args = '(self: MOAITextBox)',
        returns = '(xMin: number, yMin: number, xMax: number, yMax: number)',
      },
      getStringBounds = {
        type = "function",
        description = "Returns the bounding rectange of a given substring on a single line in the local space of the text box.",
        args = '(self: MOAITextBox, index: number, size: number)',
        returns = '(xMin: number, yMin: number, xMax: number, yMax: number)',
      },
      getStyle = {
        type = "function",
        description = "Returns the style associated with a name or, if no name is given, returns the default style.",
        args = '(self: MOAITextBox)',
        returns = '(defaultStyle: MOAITextStyle)',
      },
      more = {
        type = "function",
        description = "Returns whether there are additional pages of text below the cursor position that are not visible on the screen.",
        args = '(self: MOAITextBox)',
        returns = '(isMore: boolean)',
      },
      nextPage = {
        type = "function",
        description = "Advances to the next page of text (if any) or wraps to the start of the text (if at end).",
        args = '(self: MOAITextBox)',
        returns = '()',
      },
      reserveCurves = {
        type = "function",
        description = "Reserves a set of IDs for animation curves to be binding to this text object. See setCurves.",
        args = '(self: MOAITextBox, nCurves: number)',
        returns = '()',
      },
      revealAll = {
        type = "function",
        description = "Displays as much text as will fit in the text box.",
        args = '(self: MOAITextBox)',
        returns = '()',
      },
      setAlignment = {
        type = "function",
        description = "Sets the horizontal and/or vertical alignment of the text in the text box.",
        args = '(self: MOAITextBox, hAlignment: enum, vAlignment: enum)',
        returns = '()',
      },
      setCurve = {
        type = "function",
        description = "Binds an animation curve to the text, where the Y value of the curve indicates the text offset, or clears the curves.",
        args = '(self: MOAITextBox, curveID: number, curve: MOAIAnimCurve)',
        returns = '()',
      },
      setGlyphScale = {
        type = "function",
        description = "Sets the glyph scale. This is a scalar applied to glyphs as they are positioned in the text box.",
        args = '(self: MOAITextBox [, glyphScale: number | 1])',
        returns = '(glyphScale: number)',
      },
      setHighlight = {
        type = "function",
        description = "Set or clear the highlight color of a sub string in the text. Only affects text displayed on the current page. Highlight will automatically clear when layout or page changes.",
        args = '(self: MOAITextBox, index: number, size: number, r: number, g: number, b: number [, a: number | 1])',
        returns = '()',
      },
      setLineSpacing = {
        type = "function",
        description = "Sets additional space between lines in text units. '0' uses the default spacing. Valus must be positive.",
        args = '(self: MOAITextBox, lineSpacing: number)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Sets the rectangular area for this text box.",
        args = '(self: MOAITextBox, x1: number, y1: number, x2: number, y2: number)',
        returns = '()',
      },
      setReveal = {
        type = "function",
        description = "Sets the number of renderable characters to be shown. Can range from 0 to any value; values greater than the number of renderable characters in the current text will be ignored.",
        args = '(self: MOAITextBox, reveal: number)',
        returns = '()',
      },
      setSpeed = {
        type = "function",
        description = "Sets the base spool speed used when creating a spooling MOAIAction with the spool() function.",
        args = '(self: MOAITextBox, speed: number)',
        returns = '()',
      },
      setString = {
        type = "function",
        description = "Sets the text string to be displayed by this textbox.",
        args = '(self: MOAITextBox, newStr: string)',
        returns = '()',
      },
      setStyle = {
        type = "function",
        description = "Attaches a style to the textbox and associates a name with it. If no name is given, sets the default style.",
        args = '(self: MOAITextBox, defaultStyle: MOAITextStyle)',
        returns = '()',
      },
      setWordBreak = {
        type = "function",
        description = "Sets the rule for breaking words across lines.",
        args = '(self: MOAITextBox [, rule: number])',
        returns = '()',
      },
      setYFlip = {
        type = "function",
        description = "Sets the rendering direction for the text. Default assumes a window style screen space (positive Y moves down the screen). Set to true to render text for world style coordinate systems (positive Y moves up the screen).",
        args = '(self: MOAITextBox, yFlip: number)',
        returns = '()',
      },
      spool = {
        type = "function",
        description = "Creates a new MOAIAction which when run has the effect of increasing the amount of characters revealed from 0 to the length of the string currently set. The spool action is automatically added to the root of the action tree, but may be reparented or stopped by the developer. This function also automatically sets the current number of revealed characters to 0 (i.e. MOAITextBox:setReveal(0)).",
        args = '(self: MOAITextBox, yFlip: number)',
        returns = '(action: MOAIAction)',
      },
    },
  },
  MOAITextBundle = {
    type = "class",
    description = "A read-only lookup table of strings suitable for internationalization purposes. This currently wraps a loaded gettext() style MO file (see http://www.gnu.org/software/gettext/manual/gettext.html). So you are going to want to generate the .mo file from one of the existing tools such as poedit or msgfmt, and then load that file using this class. Then you can lookup strings using MOAITextBundle->Lookup(). */",
    childs = {
    },
  },
  MOAITextStyle = {
    type = "class",
    description = "Represents a style that may be applied to a text box or a secion of text in a text box using a style escape.",
    childs = {
      getColor = {
        type = "function",
        description = "Gets the color of the style.",
        args = '(self: MOAITextStyle)',
        returns = '(r: number, g: number, b: number, a: number)',
      },
      getFont = {
        type = "function",
        description = "Gets the font of the style.",
        args = '(self: MOAITextStyle)',
        returns = '(font: MOAIFont)',
      },
      getScale = {
        type = "function",
        description = "Gets the scale of the style.",
        args = '(self: MOAITextStyle)',
        returns = '(scale: number)',
      },
      getSize = {
        type = "function",
        description = "Gets the size of the style.",
        args = '(self: MOAITextStyle)',
        returns = '(size: number)',
      },
      setColor = {
        type = "function",
        description = "Initialize the style's color.",
        args = '(self: MOAITextStyle, r: number, g: number, b: number [, a: number | 1])',
        returns = '()',
      },
      setFont = {
        type = "function",
        description = "Sets or clears the style's font.",
        args = '(self: MOAITextStyle [, font: MOAIFont | nil])',
        returns = '()',
      },
      setScale = {
        type = "function",
        description = "Sets the scale of the style. The scale is applied to any glyphs drawn using the style after the glyph set has been selected by size.",
        args = '(self: MOAITextStyle [, scale: number | 1])',
        returns = '()',
      },
      setSize = {
        type = "function",
        description = "Sets or clears the style's size.",
        args = '(self: MOAITextStyle, points: number [, dpi: number | 72])',
        returns = '()',
      },
    },
  },
  MOAITexture = {
    type = "class",
    description = "Texture class.",
    childs = {
      load = {
        type = "function",
        description = "Loads a texture from a data buffer or a file. Optionally pass in an image transform (not applicable to PVR textures).",
        args = '(self: MOAITexture, filename: string [, transform: number [, debugname: string]])',
        returns = '()',
      },
    },
  },
  MOAITextureBase = {
    type = "class",
    description = "Base class for texture resources.",
    childs = {
      GL_LINEAR = {
        type = "value",
        description = "",
      },
      GL_LINEAR_MIPMAP_LINEAR = {
        type = "value",
        description = "",
      },
      GL_LINEAR_MIPMAP_NEAREST = {
        type = "value",
        description = "",
      },
      GL_NEAREST = {
        type = "value",
        description = "",
      },
      GL_NEAREST_MIPMAP_LINEAR = {
        type = "value",
        description = "",
      },
      GL_NEAREST_MIPMAP_NEAREST = {
        type = "value",
        description = "",
      },
      getSize = {
        type = "function",
        description = "Returns the width and height of the texture's source image. Avoid using the texture width and height to compute UV coordinates from pixels, as this will prevent texture resolution swapping.",
        args = '(self: MOAITextureBase)',
        returns = '(width: number, height: number)',
      },
      release = {
        type = "function",
        description = "Releases any memory associated with the texture.",
        args = '(self: MOAITextureBase)',
        returns = '()',
      },
      setFilter = {
        type = "function",
        description = "Set default filtering mode for texture.",
        args = '(self: MOAITextureBase, min: number [, mag: number])',
        returns = '()',
      },
      setWrap = {
        type = "function",
        description = "Set wrapping mode for texture.",
        args = '(self: MOAITextureBase, wrap: boolean)',
        returns = '()',
      },
    },
  },
  MOAITileDeck2D = {
    type = "class",
    description = "Subdivides a single texture into uniform tiles enumerated from the texture's left top to right bottom.",
    childs = {
      setQuad = {
        type = "function",
        description = "Set model space quad. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAITileDeck2D, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setRect = {
        type = "function",
        description = "Set the model space dimensions of a single tile. When grid drawing, this should be a unit rect centered at the origin for tiles that fit each grid cell. Growing or shrinking the rect will cause tiles to overlap or leave gaps between them.",
        args = '(self: MOAITileDeck2D, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setUVQuad = {
        type = "function",
        description = "Set the UV space dimensions of the quad. Vertex order is clockwise from upper left (xMin, yMax)",
        args = '(self: MOAITileDeck2D, x0: number, y0: number, x1: number, y1: number, x2: number, y2: number, x3: number, y3: number)',
        returns = '()',
      },
      setUVRect = {
        type = "function",
        description = "Set the UV space dimensions of the quad.",
        args = '(self: MOAITileDeck2D, xMin: number, yMin: number, xMax: number, yMax: number)',
        returns = '()',
      },
      setSize = {
        type = "function",
        description = "Controls how the texture is subdivided into tiles. Default behavior is to subdivide the texture into N by M tiles, but is tile dimensions are provided (in UV space) then the resulting tile set will be N * tileWidth by M * tileHeight in UV space. This means the tile set does not have to fill all of the texture. The upper left hand corner of the tile set will always be at UV 0, 0.",
        args = '(self: MOAITileDeck2D, width: number, height: number [, cellWidth: number [, cellHeight: number [, xOff: number [, yOff: number [, tileWidth: number | cellWidth [, tileHeight: number | cellHeight]]]]]])',
        returns = '()',
      },
      transform = {
        type = "function",
        description = "Apply the given MOAITransform to all the vertices in the deck.",
        args = '(self: MOAITileDeck2D, transform: MOAITransform)',
        returns = '()',
      },
      transformUV = {
        type = "function",
        description = "Apply the given MOAITransform to all the uv coordinates in the deck.",
        args = '(self: MOAITileDeck2D, transform: MOAITransform)',
        returns = '()',
      },
    },
  },
  MOAITimer = {
    type = "class",
    description = "Timer class for driving curves and animations.",
    childs = {
      NORMAL = {
        type = "value",
        description = "",
      },
      REVERSE = {
        type = "value",
        description = "",
      },
      CONTINUE = {
        type = "value",
        description = "",
      },
      CONTINUE_REVERSE = {
        type = "value",
        description = "",
      },
      LOOP = {
        type = "value",
        description = "",
      },
      LOOP_REVERSE = {
        type = "value",
        description = "",
      },
      PING_PONG = {
        type = "value",
        description = "",
      },
      EVENT_TIMER_KEYFRAME = {
        type = "value",
        description = "ID of event stop callback. Signature is: nil onKeyframe ( MOAITimer self, number keyframe, number timesExecuted, number time, number value )",
      },
      EVENT_TIMER_LOOP = {
        type = "value",
        description = "ID of event loop callback. Signature is: nil onLoop ( MOAITimer self, number timesExecuted )",
      },
      EVENT_TIMER_BEGIN_SPAN = {
        type = "value",
        description = "Called when timer starts or after roll over (if looping). Signature is: nil onBeginSpan ( MOAITimer self, number timesExecuted )",
      },
      EVENT_TIMER_END_SPAN = {
        type = "value",
        description = "Called when timer ends or before roll over (if looping). Signature is: nil onEndSpan ( MOAITimer self, number timesExecuted )",
      },
      getTime = {
        type = "function",
        description = "Return the current time.",
        args = '(self: MOAITimer)',
        returns = '(time: number)',
      },
      getTimesExecuted = {
        type = "function",
        description = "Gets the number of times the timer has completed a cycle.",
        args = '(self: MOAITimer)',
        returns = '(nTimes: number)',
      },
      setCurve = {
        type = "function",
        description = "Set or clear the curve to use for event generation.",
        args = '(self: MOAITimer [, curve: MOAIAnimCurve | nil])',
        returns = '()',
      },
      setMode = {
        type = "function",
        description = "Sets the playback mode of the timer.",
        args = '(self: MOAITimer, mode: number)',
        returns = '()',
      },
      setSpan = {
        type = "function",
        description = "Sets the playback mode of the timer.",
        args = '(self: MOAITimer, endTime: number)',
        returns = '()',
      },
      setSpeed = {
        type = "function",
        description = "Sets the playback speed. This affects only the timer, not its children in the action tree.",
        args = '(self: MOAITimer, speed: number)',
        returns = '()',
      },
      setTime = {
        type = "function",
        description = "Manually set the current time. This will be wrapped into the current span.",
        args = '(self: MOAITimer [, time: number | 0])',
        returns = '()',
      },
    },
  },
  MOAITouchSensor = {
    type = "class",
    description = "Multitouch sensor. Tracks up to 16 simultaneous touches.",
    childs = {
      TOUCH_DOWN = {
        type = "value",
        description = "",
      },
      TOUCH_MOVE = {
        type = "value",
        description = "",
      },
      TOUCH_UP = {
        type = "value",
        description = "",
      },
      TOUCH_CANCEL = {
        type = "value",
        description = "",
      },
      down = {
        type = "function",
        description = "Checks to see if the screen was touched during the last iteration.",
        args = '(self: MOAITouchSensor [, idx: number])',
        returns = '(wasPressed: boolean)',
      },
      getActiveTouches = {
        type = "function",
        description = "Returns the IDs of all of the touches currently occurring (for use with getTouch).",
        args = '(self: MOAITouchSensor)',
        returns = '(idx1: number, idxN: number)',
      },
      getTouch = {
        type = "function",
        description = "Checks to see if there are currently touches being made on the screen.",
        args = '(self: MOAITouchSensor, id: number)',
        returns = '(x: number, y: number, tapCount: number)',
      },
      hasTouches = {
        type = "function",
        description = "Checks to see if there are currently touches being made on the screen.",
        args = '(self: MOAITouchSensor)',
        returns = '(hasTouches: boolean)',
      },
      isDown = {
        type = "function",
        description = "Checks to see if the touch status is currently down.",
        args = '(self: MOAITouchSensor)',
        returns = '(isDown: boolean)',
      },
      setAcceptCancel = {
        type = "function",
        description = "Sets whether or not to accept cancel events ( these happen on iOS backgrounding ), default value is false",
        args = '(self: MOAITouchSensor, accept: boolean)',
        returns = '()',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued when the pointer location changes.",
        args = '(self: MOAITouchSensor [, callback: function | nil])',
        returns = '()',
      },
      setTapTime = {
        type = "function",
        description = "Sets the time between each touch for it to be counted as a tap",
        args = '(self: MOAITouchSensor, margin: number, self: MOAITouchSensor, time: number)',
        returns = '()',
      },
      up = {
        type = "function",
        description = "Checks to see if the screen was untouched (is no longer being touched) during the last iteration.",
        args = '(self: MOAITouchSensor)',
        returns = '(wasPressed: boolean)',
      },
    },
  },
  MOAITransform = {
    type = "class",
    description = "Transformation hierarchy node.",
    childs = {
      addLoc = {
        type = "function",
        description = "Adds a delta to the transform's location.",
        args = '(self: MOAITransform, xDelta: number, yDelta: number, zDelta: number)',
        returns = '()',
      },
      addPiv = {
        type = "function",
        description = "Adds a delta to the transform's pivot.",
        args = '(self: MOAITransform, xDelta: number, yDelta: number, zDelta: number)',
        returns = '()',
      },
      addRot = {
        type = "function",
        description = "Adds a delta to the transform's rotation",
        args = '(self: MOAITransform, xDelta: number, yDelta: number, zDelta: number)',
        returns = '()',
      },
      addScl = {
        type = "function",
        description = "Adds a delta to the transform's scale",
        args = '(self: MOAITransform, xSclDelta: number [, ySclDelta: number | xSclDelta [, zSclDelta: number | 0]])',
        returns = '()',
      },
      getLoc = {
        type = "function",
        description = "Returns the transform's current location.",
        args = '(self: MOAITransform)',
        returns = '(xLoc: number, yLoc: number, zLoc: number)',
      },
      getPiv = {
        type = "function",
        description = "Returns the transform's current pivot.",
        args = '(self: MOAITransform)',
        returns = '(xPiv: number, yPiv: number, zPiv: number)',
      },
      getRot = {
        type = "function",
        description = "Returns the transform's current rotation.",
        args = '(self: MOAITransform)',
        returns = '(xRot: number, yRot: number, zRot: number)',
      },
      getScl = {
        type = "function",
        description = "Returns the transform's current scale.",
        args = '(self: MOAITransform)',
        returns = '(xScl: number, yScl: number, zScl: number)',
      },
      modelToWorld = {
        type = "function",
        description = "Transform a point in model space to world space.",
        args = '(self: MOAITransform [, x: number | 0 [, y: number | 0 [, z: number | 0]]])',
        returns = '(x: number, y: number, z: number)',
      },
      move = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xDelta: number, yDelta: number, zDelta: number, xRotDelta: number, yRotDelta: number, zRotDelta: number, xSclDelta: number, ySclDelta: number, zSclDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      moveLoc = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xDelta: number, yDelta: number, zDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      movePiv = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xDelta: number, yDelta: number, zDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      moveRot = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xDelta: number, yDelta: number, zDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      moveScl = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xSclDelta: number, ySclDelta: number, zSclDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seek = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xGoal: number, yGoal: number, zGoal: number, xRotGoal: number, yRotGoal: number, zRotGoal: number, xSclGoal: number, ySclGoal: number, zSclGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekLoc = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xGoal: number, yGoal: number, zGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekPiv = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xGoal: number, yGoal: number, zGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekRot = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xRotGoal: number, yRotGoal: number, zRotGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekScl = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform, xSclGoal: number, ySclGoal: number, zSclGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      setLoc = {
        type = "function",
        description = "Sets the transform's location.",
        args = '(self: MOAITransform [, x: number | 0 [, y: number | 0 [, z: number | 0]]])',
        returns = '()',
      },
      setParent = {
        type = "function",
        description = "This method has been deprecated. Use MOAINode setAttrLink instead.",
        args = '(self: MOAITransform [, parent: MOAINode | nil])',
        returns = '()',
      },
      setPiv = {
        type = "function",
        description = "Sets the transform's pivot.",
        args = '(self: MOAITransform [, xPiv: number | 0 [, yPiv: number | 0 [, zPiv: number | 0]]])',
        returns = '()',
      },
      setRot = {
        type = "function",
        description = "Sets the transform's rotation.",
        args = '(self: MOAITransform [, xRot: number | 0 [, yRot: number | 0 [, zRot: number | 0]]])',
        returns = '()',
      },
      setScl = {
        type = "function",
        description = "Sets the transform's scale.",
        args = '(self: MOAITransform, xScl: number [, yScl: number | xScl [, zScl: number | 1]])',
        returns = '()',
      },
      setShearByX = {
        type = "function",
        description = "Sets the shear for the Y and Z axes by X.",
        args = '(self: MOAITransform, yx: number [, zx: number | 0])',
        returns = '()',
      },
      setShearByY = {
        type = "function",
        description = "Sets the shear for the X and Z axes by Y.",
        args = '(self: MOAITransform, xy: number [, zy: number | 0])',
        returns = '()',
      },
      setShearByZ = {
        type = "function",
        description = "Sets the shear for the X and Y axes by Z.",
        args = '(self: MOAITransform, xz: number [, yz: number | 0])',
        returns = '()',
      },
      worldToModel = {
        type = "function",
        description = "Transform a point in world space to model space.",
        args = '(self: MOAITransform [, x: number | 0 [, y: number | 0 [, z: number | 0]]])',
        returns = '(x: number, y: number, z: number)',
      },
    },
  },
  MOAITransform2D = {
    type = "class",
    description = "2D transformation hierarchy node.",
    childs = {
      addLoc = {
        type = "function",
        description = "Adds a delta to the transform's location.",
        args = '(self: MOAITransform2D, xDelta: number, yDelta: number)',
        returns = '()',
      },
      addPiv = {
        type = "function",
        description = "Adds a delta to the transform's pivot.",
        args = '(self: MOAITransform2D, xDelta: number, yDelta: number)',
        returns = '()',
      },
      addRot = {
        type = "function",
        description = "Adds a delta to the transform's rotation",
        args = '(self: MOAITransform2D, xDelta: number, yDelta: number)',
        returns = '()',
      },
      addScl = {
        type = "function",
        description = "Adds a delta to the transform's scale",
        args = '(self: MOAITransform2D, xSclDelta: number [, ySclDelta: number | xSclDelta])',
        returns = '()',
      },
      getLoc = {
        type = "function",
        description = "Returns the transform's current location.",
        args = '(self: MOAITransform2D)',
        returns = '(xLoc: number, yLoc: number)',
      },
      getPiv = {
        type = "function",
        description = "Returns the transform's current pivot.",
        args = '(self: MOAITransform2D)',
        returns = '(xPiv: number, yPiv: number)',
      },
      getRot = {
        type = "function",
        description = "Returns the transform's current rotation.",
        args = '(self: MOAITransform2D)',
        returns = '(zRot: number)',
      },
      getScl = {
        type = "function",
        description = "Returns the transform's current scale.",
        args = '(self: MOAITransform2D)',
        returns = '(xScl: number, yScl: number)',
      },
      modelToWorld = {
        type = "function",
        description = "Transform a point in model space to world space.",
        args = '(self: MOAITransform2D [, x: number | 0 [, y: number | 0]])',
        returns = '(x: number, y: number)',
      },
      move = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xDelta: number, yDelta: number, zRotDelta: number, xSclDelta: number, ySclDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      moveLoc = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xDelta: number, yDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      movePiv = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xDelta: number, yDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      moveRot = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, zDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      moveScl = {
        type = "function",
        description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xSclDelta: number, ySclDelta: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seek = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xGoal: number, yGoal: number, zRotGoal: number, xSclGoal: number, ySclGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekLoc = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xGoal: number, yGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekPiv = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xGoal: number, yGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekRot = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, zRotGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      seekScl = {
        type = "function",
        description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.",
        args = '(self: MOAITransform2D, xSclGoal: number, ySclGoal: number, length: number [, mode: number])',
        returns = '(easeDriver: MOAIEaseDriver)',
      },
      setLoc = {
        type = "function",
        description = "Sets the transform's location.",
        args = '(self: MOAITransform2D [, x: number | 0 [, y: number | 0]])',
        returns = '()',
      },
      setParent = {
        type = "function",
        description = "This method has been deprecated. Use MOAINode setAttrLink instead.",
        args = '(self: MOAITransform2D [, parent: MOAINode | nil])',
        returns = '()',
      },
      setPiv = {
        type = "function",
        description = "Sets the transform's pivot.",
        args = '(self: MOAITransform2D [, xPiv: number | 0 [, yPiv: number | 0]])',
        returns = '()',
      },
      setRot = {
        type = "function",
        description = "Sets the transform's rotation.",
        args = '(self: MOAITransform2D [, zRot: number | 0])',
        returns = '()',
      },
      setScl = {
        type = "function",
        description = "Sets the transform's scale.",
        args = '(self: MOAITransform2D, xScl: number [, yScl: number | xScl])',
        returns = '()',
      },
      worldToModel = {
        type = "function",
        description = "Transform a point in world space to model space.",
        args = '(self: MOAITransform2D [, x: number | 0 [, y: number | 0]])',
        returns = '(x: number, y: number)',
      },
    },
  },
  MOAITransformBase = {
    type = "class",
    description = "Base class for 2D affine transforms.",
    childs = {
      getWorldDir = {
        type = "function",
        description = "Returns the normalized direction vector of the transform. This value is returned in world space so includes parent transforms (if any).",
        args = '(self: MOAITransformBase)',
        returns = '(xDirection: number, yDirection: number, zDirection: number)',
      },
      getWorldLoc = {
        type = "function",
        description = "Get the transform's location in world space.",
        args = '(self: MOAITransformBase)',
        returns = '(xLoc: number, yLoc: number, zLoc: number)',
      },
      getWorldRot = {
        type = "function",
        description = "Get the transform's rotation in world space.",
        args = '(self: MOAITransformBase)',
        returns = '(degrees: number)',
      },
      getWorldScl = {
        type = "function",
        description = "Get the transform's scale in world space.",
        args = '(self: MOAITransformBase)',
        returns = '(xScale: number, yScale: number, zScale: number)',
      },
    },
  },
  MOAIVertexBuffer = {
    type = "class",
    description = "Vertex buffer class.",
    childs = {
      bless = {
        type = "function",
        description = "Call this after initializing the buffer and settings it vertices to prepare it for use.",
        args = '(self: MOAIVertexBuffer)',
        returns = '()',
      },
      release = {
        type = "function",
        description = "Releases any memory associated with buffer.",
        args = '(self: MOAIVertexBuffer)',
        returns = '()',
      },
      reserve = {
        type = "function",
        description = "Sets capacity of buffer in bytes.",
        args = '(self: MOAIVertexBuffer, size: number)',
        returns = '()',
      },
      reserveVerts = {
        type = "function",
        description = "Sets capacity of buffer in vertices. This function should only be used after attaching a valid MOAIVertexFormat to the buffer.",
        args = '(self: MOAIVertexBuffer, size: number)',
        returns = '()',
      },
      reset = {
        type = "function",
        description = "Resets the vertex stream writing to the head of the stream.",
        args = '(self: MOAIVertexBuffer)',
        returns = '()',
      },
      setFormat = {
        type = "function",
        description = "Sets the vertex format for the buffer.",
        args = '(self: MOAIVertexBuffer, format: MOAIVertexFormat)',
        returns = '()',
      },
      writeColor32 = {
        type = "function",
        description = "Write a packed 32-bit color to the vertex buffer.",
        args = '(self: MOAIVertexBuffer [, r: number | 1 [, g: number | 1 [, b: number | 1 [, a: number | 1]]]])',
        returns = '()',
      },
      writeFloat = {
        type = "function",
        description = "Write a 32-bit float to the vertex buffer.",
        args = '(self: MOAIVertexBuffer [, f: number | 0])',
        returns = '()',
      },
      writeInt8 = {
        type = "function",
        description = "Write an 8-bit integer to the vertex buffer.",
        args = '(self: MOAIVertexBuffer [, i: number | 0])',
        returns = '()',
      },
      writeInt16 = {
        type = "function",
        description = "Write an 16-bit integer to the vertex buffer.",
        args = '(self: MOAIVertexBuffer [, i: number | 0])',
        returns = '()',
      },
      writeInt32 = {
        type = "function",
        description = "Write an 32-bit integer to the vertex buffer.",
        args = '(self: MOAIVertexBuffer [, i: number | 0])',
        returns = '()',
      },
    },
  },
  MOAIVertexFormat = {
    type = "class",
    description = "Vertex format class.",
    childs = {
      declareAttribute = {
        type = "function",
        description = "Declare a custom attribute (for use with programmable pipeline).",
        args = '(self: MOAIVertexFormat, index: number, type: number, size: number [, normalized: boolean])',
        returns = '()',
      },
      declareColor = {
        type = "function",
        description = "Declare a vertex color.",
        args = '(self: MOAIVertexFormat, type: number)',
        returns = '()',
      },
      declareCoord = {
        type = "function",
        description = "Declare a vertex coord.",
        args = '(self: MOAIVertexFormat, type: number, size: number)',
        returns = '()',
      },
      declareNormal = {
        type = "function",
        description = "Declare a vertex normal.",
        args = '(self: MOAIVertexFormat, type: number)',
        returns = '()',
      },
      declareUV = {
        type = "function",
        description = "Declare a vertex texture coord.",
        args = '(self: MOAIVertexFormat, type: number, size: number)',
        returns = '()',
      },
    },
  },
  MOAIViewport = {
    type = "class",
    description = "Viewport object.",
    childs = {
      setOffset = {
        type = "function",
        description = "Sets the viewport offset in normalized view space (size of viewport is -1 to 1 in both directions).",
        args = '(self: MOAIViewport, xOff: number, yOff: number)',
        returns = '()',
      },
      setRotation = {
        type = "function",
        description = "Sets global rotation to be added to camera transform.",
        args = '(self: MOAIViewport, rotation: number)',
        returns = '()',
      },
      setScale = {
        type = "function",
        description = "Sets the number of world units visible of the viewport for one or both dimensions. Set 0 for one of the dimensions to use a derived value based on the other dimension and the aspect ratio. Negative values are also OK.",
        args = '(self: MOAIViewport, xScale: number, yScale: number)',
        returns = '()',
      },
      setSize = {
        type = "function",
        description = "Sets the dimensions of the this->",
        args = '(self: MOAIViewport, width: number, height: number)',
        returns = '()',
      },
    },
  },
  MOAIWheelSensor = {
    type = "class",
    description = "Hardware wheel sensor.",
    childs = {
      getValue = {
        type = "function",
        description = "Returns the current value of the wheel, based on delta events",
        args = '(self: MOAIWheelSensor)',
        returns = '(value: number)',
      },
      getDelta = {
        type = "function",
        description = "Returns the delta of the wheel",
        args = '(self: MOAIWheelSensor)',
        returns = '(delta: number)',
      },
      setCallback = {
        type = "function",
        description = "Sets or clears the callback to be issued on a wheel delta event",
        args = '(self: MOAIWheelSensor [, callback: function | nil])',
        returns = '()',
      },
    },
  },
  MOAIXmlParser = {
    type = "class",
    description = "Converts XML DOM to Lua trees. Provided as a convenience; not advised for parsing very large XML documents. (Use of XML not advised at all - use JSON or Lua.)",
    childs = {
      parseFile = {
        type = "function",
        description = "Parses the contents of the specified file as XML.",
        args = '(self: MOAIXmlParser, filename: string)',
        returns = '(data: table)',
      },
      parseString = {
        type = "function",
        description = "Parses the contents of the specified string as XML.",
        args = '(self: MOAIXmlParser, filename: string)',
        returns = '(data: table)',
      },
    },
  },
}

--[[ lua script to generate a list of Moai classes that have new() method
------------------------>> cut here <<-----------------------------

local moai = dofile("moai.lua")
local list = {}
for k, v in pairs(moai) do
  local str = "return " .. k .. ".new()"
  local func = loadstring(str)
  local ok, res = pcall(func)
  if ok and res then list[#list+1] = k end
end
print(table.concat(list, " "))

------------------------>> cut here <<-----------------------------]]

--[[ perl script to generate this API description from MOAI sources
------------------------>> cut here <<-----------------------------

# Use it as "cd src/moaicore; perl moai.pl >moai.lua"
# Limitations:
#  - doesn't deal with @overload; only uses the first group

use strict;
use warnings;

my @list = glob("MOAI*.cpp");
my %news = map {$_ => 1} qw(MOAISerializer MOAIAction MOAIStretchPatch2D MOAIVertexBuffer MOAICoroutine MOAIGridDeck2D MOAIGridPathGraph MOAIBox2DRevoluteJoint MOAIAnimCurve MOAIDeckRemapper MOAIViewport MOAITextStyle MOAIBox2DFrictionJoint MOAIBox2DBody MOAIGfxQuad2D MOAITransform2D MOAIScissorRect MOAICamera2D MOAICpBody MOAIFoo MOAIGridSpace MOAIFreeTypeFontReader MOAIMultiTexture MOAIBox2DPrismaticJoint MOAIAnimCurveQuat MOAIGrid MOAIParticleState MOAITimer MOAITextBundle MOAIBox2DWeldJoint MOAICamera MOAIDataBuffer MOAIFont MOAIGlyphCache MOAIParticleCallbackPlugin MOAIVertexFormat MOAIDataIOAction MOAIBitmapFontReader MOAIShader MOAILayer MOAITextBox MOAIFileStream MOAICameraFitter2D MOAIBox2DWorld MOAIMemStream MOAIBoundsDeck MOAIDataBufferStream MOAIPathTerrainDeck MOAIBox2DFixture MOAIProp MOAIGfxQuadListDeck2D MOAIAnimCurveVec MOAIBox2DPulleyJoint MOAIParticleScript MOAIImage MOAITileDeck2D MOAITexture MOAICameraAnchor2D MOAISurfaceDeck2D MOAIProp2D MOAIBox2DGearJoint MOAIStreamWriter MOAIStreamReader MOAIParser MOAICpConstraint MOAIStaticGlyphCache MOAIBox2DDistanceJoint MOAIScriptDeck MOAIPathFinder MOAIPartition MOAIAnim MOAIParticleTimedEmitter MOAIParticleSystem MOAITransform MOAIParticlePexPlugin MOAIParticleForce MOAIParticleDistanceEmitter MOAIMesh MOAILayerBridge MOAILayer2D MOAIIndexBuffer MOAIImageTexture MOAIHttpTaskCurl MOAIGfxQuadDeck2D MOAIColor MOAIFrameBuffer MOAIEaseDriver MOAIBox2DMouseJoint MOAICpSpace MOAIScriptNode);

print "return {\n";

foreach my $cfile (@list) {
  (my $hfile = $cfile) =~ s/cpp$/h/;
  next if !-e $hfile;

  my($lib, $desc, @consts);
  open(H, "<$hfile") or die "Can't open file '$hfile': $!\n";
  while (<H>) {
    if (m!^/\*\*! .. m!^\*/!) {
      if (/\@name\s+(\S+)/) {
        if ($hfile ne "$1.h") { # some .h files may list multiple names
          warn "WARNING: Ignored conflicting name in $hfile: $1\n";
          next;
        }
        $lib = $1;
      }
      if ($lib && /\@text\s+(.+)/) { # handle multi-line descriptions
        $desc = $1;
        while (<H>) {
          last if m!^\*/! || !/\S/;
          $desc .= $_;
        }
        s/[\r\n]//g, s/\s{2,}/ /g for $desc;

        # in some cases @text is not followed by an empty line, but instead
        # has closing */; just redo the processing if that's the case.
        redo if m!^\*/!;
      }
      push(@consts, [$1, $2 || ""]) if $lib && /\@const\s+(\S+)(?:\s+(.+))?/;
    }
  }
  close(H);

  if (!$lib) {
    warn "WARNING: Skipped $hfile\n";
    next;
  }
  $desc =~ s/<p>//g; # FIX: remove <p> from some descriptions

  print <<"EOS";
  $lib = {
    type = "class",
    description = "$desc",
    childs = {
EOS

  foreach (@consts) {
    my($const, $desc) = @$_;
    print <<"EOS";
      $const = {
        type = "value",
        description = "$desc",
      },
EOS
  }

=for comment
# adding new() method disabled as it's hardcoded and doesn't distinguish
# between Foo.new() and object foo.new().
  if ($news{$lib}) { # if the class has new() method
    print <<"EOS";
      new = {
        type = "function",
        description = "Constructs an object.",
        args = '()',
        returns = '(self: $lib)',
      },
EOS
  }
=cut

  my($func, @parms);
  undef $desc;
  open(CPP, "<$cfile") or die "Can't open file '$cfile': $!\n";
  while (<CPP>) {
    if (m!^/\*\*! ... m!^\*/!) {
      $func = $1 if /\@name\s+(\S+)/;
      if (/\@text\s+(.+)/) { # handle multi-line descriptions
        $desc = $1;
        while (<CPP>) {
          last if !/\S/;
          $desc .= $_;
        }
        s/[\r\n]//g, s/\s{2,}/ /g for $desc;
      }
      push(@parms, [$1, $2]) if /\@(overload)(?:\s+(.+))?$/;
      push(@parms, [$1, $2, $3, $4]) if /\@(in|out|opt)\s+(\S+)(?:\s+(\S+)(?:\s+(.+))?)?$/;

      if (m!^\*/!) {
        my(@args, @returns, @opts);

        shift(@parms) if @parms and $parms[0][0] eq 'overload';
        foreach (@parms) {
          last if $_->[0] eq 'overload';
          push(@args, $_->[1] eq '...' ? $_->[1] : $_->[2] . ": " . $_->[1])
            if $_->[0] eq 'in';
          push(@opts, $_->[2] . ": " . $_->[1] 
            . (($_->[3] || '') =~ /Default value is '?("[^"]+"|[^\.;'\s]+)/ ? " | $1" : ""))
            if $_->[0] eq 'opt';
          push(@returns, $_->[2] ? ($_->[2] . ": " . $_->[1]) : $_->[1] . ": ?")
            if $_->[0] eq 'out' && $_->[1] ne 'nil' && $_->[1] ne '...';
        }

        my $args = join(", ", @args) . (@opts ? (@args ? " [, " : "[") : "")
          . join(" [, ", @opts) . (']' x @opts);
        my $returns = join(", ", @returns);
        $desc ||= "";
        print <<"EOS" if $func;
      $func = {
        type = "function",
        description = "$desc",
        args = '($args)',
        returns = '($returns)',
      },
EOS
        ($func, $desc, @parms) = (); # reset for the next run
      }
    }
  }

  print <<"EOS";
    },
  },
EOS

}

print "}\n";

------------------------>> cut here <<-----------------------------]]
