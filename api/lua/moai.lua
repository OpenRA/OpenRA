-- Documentation for Moai SDK 1.5 revision 1 (interim version by MoaiEdition) (http://getmoai.com/)
-- Generated on 2014-04-01 by DocExport v2.1.
-- DocExport is part of MoaiUtils (https://github.com/DanielSWolf/MoaiUtils).

return {
    b2ContactListener = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    b2DestructionListener = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAIAction = {
        type = "class",
        inherits = "MOAIBlocker MOAIInstanceEventSource",
        description = "Base class for actions.",
        childs = {
            EVENT_STOP = {
                type = "value",
                description = "ID of event stop callback. Signature is: nil onStop ()"
            },
            addChild = {
                type = "method",
                description = "Attaches a child action for updating.\n\n–> MOAIAction self\n–> MOAIAction child\n<– MOAIAction self",
                args = "(MOAIAction self, MOAIAction child)",
                returns = "MOAIAction self",
                valuetype = "MOAIAction"
            },
            attach = {
                type = "method",
                description = "Attaches a child to a parent action. The child will receive updates from the parent only if the parent is in the action tree.\n\n–> MOAIAction self\n[–> MOAIAction parent: Default value is nil; same effect as calling detach ().]\n<– MOAIAction self",
                args = "(MOAIAction self, [MOAIAction parent])",
                returns = "MOAIAction self",
                valuetype = "MOAIAction"
            },
            clear = {
                type = "method",
                description = "Removes all child actions.\n\n–> MOAIAction self\n<– MOAIAction self",
                args = "MOAIAction self",
                returns = "MOAIAction self",
                valuetype = "MOAIAction"
            },
            detach = {
                type = "method",
                description = "Detaches an action from its parent (if any) thereby removing it from the action tree. Same effect as calling stop ().\n\n–> MOAIAction self\n<– MOAIAction self",
                args = "MOAIAction self",
                returns = "MOAIAction self",
                valuetype = "MOAIAction"
            },
            isActive = {
                type = "method",
                description = "Checks to see if an action is currently in the action tree.\n\n–> MOAIAction self\n<– boolean isActive",
                args = "MOAIAction self",
                returns = "boolean isActive",
                valuetype = "boolean"
            },
            isBusy = {
                type = "method",
                description = "Checks to see if an action is currently busy. An action is 'busy' only if it is 'active' and not 'done.'\n\n–> MOAIAction self\n<– boolean isBusy",
                args = "MOAIAction self",
                returns = "boolean isBusy",
                valuetype = "boolean"
            },
            isDone = {
                type = "method",
                description = "Checks to see if an action is 'done.' Definition of 'done' is up to individual action implementations.\n\n–> MOAIAction self\n<– boolean isDone",
                args = "MOAIAction self",
                returns = "boolean isDone",
                valuetype = "boolean"
            },
            pause = {
                type = "method",
                description = "Leaves the action in the action tree but prevents it from receiving updates. Call pause ( false ) or start () to unpause.\n\n–> MOAIAction self\n[–> boolean pause: Default value is 'true.']\n<– nil",
                args = "(MOAIAction self, [boolean pause])",
                returns = "nil"
            },
            start = {
                type = "method",
                description = "Adds the action to a parent action or the root of the action tree.\n\n–> MOAIAction self\n[–> MOAIAction parent: Default value is MOAIActionMgr.getRoot ()]\n<– MOAIAction self",
                args = "(MOAIAction self, [MOAIAction parent])",
                returns = "MOAIAction self",
                valuetype = "MOAIAction"
            },
            stop = {
                type = "method",
                description = "Removed the action from its parent action; action will stop being updated.\n\n–> MOAIAction self\n<– MOAIAction self",
                args = "MOAIAction self",
                returns = "MOAIAction self",
                valuetype = "MOAIAction"
            },
            throttle = {
                type = "method",
                description = "Sets the actions throttle. Throttle is a scalar on time. Is is passed to the action's children.\n\n–> MOAIAction self\n[–> number throttle: Default value is 1.]\n<– MOAIAction self",
                args = "(MOAIAction self, [number throttle])",
                returns = "MOAIAction self",
                valuetype = "MOAIAction"
            }
        }
    },
    MOAIActionMgr = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Manager class for MOAIActions.",
        childs = {
            getRoot = {
                type = "function",
                description = "Returns the current root action.\n\n<– MOAIAction root",
                args = "()",
                returns = "MOAIAction root",
                valuetype = "MOAIAction"
            },
            setProfilingEnabled = {
                type = "function",
                description = "Enables action profiling.\n\n[–> boolean enable: Default value is false.]\n<– nil",
                args = "[boolean enable]",
                returns = "nil"
            },
            setRoot = {
                type = "function",
                description = "Replaces or clears the root action.\n\n[–> MOAIAction root: Default value is nil.]\n<– nil",
                args = "[MOAIAction root]",
                returns = "nil"
            },
            setThreadInfoEnabled = {
                type = "function",
                description = "Enables function name and line number info for MOAICoroutine.\n\n[–> boolean enable: Default value is false.]\n<– nil",
                args = "[boolean enable]",
                returns = "nil"
            }
        }
    },
    MOAIAdColonyAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {
            getDeviceID = {
                type = "function",
                description = "Request a unique ID for the device.\n\n<– string id: The device ID.",
                args = "()",
                returns = "string id",
                valuetype = "string"
            },
            init = {
                type = "function",
                description = "Initialize AdColony.\n\n–> string appId: Available in AdColony dashboard settings.\n–> table zones: A list of zones to configure. Available in AdColony dashboard settings.\n<– nil",
                args = "(string appId, table zones)",
                returns = "nil"
            },
            playVideo = {
                type = "function",
                description = "Play an AdColony video ad.\n\n–> string zone: The zone from which to play a video ad.\n[–> boolean prompt: Determines whether the user is asked whether they want to play a video ad or not. Default is true.]\n[–> boolean confirm: Determines whether the user is presented with a confirmation dialog after video ad playback completes. Default is true.]\n<– nil",
                args = "(string zone, [boolean prompt, [boolean confirm]])",
                returns = "nil"
            },
            videoReadyForZone = {
                type = "function",
                description = "Check the readiness of a video ad for a given zone.\n\n–> string zone: The zone from which to check for a video ad.\n<– boolean isReady: True, if a video ad is ready to play.",
                args = "string zone",
                returns = "boolean isReady",
                valuetype = "boolean"
            }
        }
    },
    MOAIAnim = {
        type = "class",
        inherits = "MOAITimer",
        description = "Bind animation curves to nodes and provides timer controls for animation playback.",
        childs = {
            apply = {
                type = "method",
                description = "Apply the animation at a given time or time step.\n\nOverload:\n–> MOAIAnim self\n[–> number t0: Default value is 0.]\n<– nil\n\nOverload:\n–> MOAIAnim self\n–> number t0\n–> number t1\n<– nil",
                args = "(MOAIAnim self, [number t0, [number t1]])",
                returns = "nil"
            },
            getLength = {
                type = "method",
                description = "Return the length of the animation.\n\n–> MOAIAnim self\n<– number length",
                args = "MOAIAnim self",
                returns = "number length",
                valuetype = "number"
            },
            reserveLinks = {
                type = "method",
                description = "Reserves a specified number of links for the animation.\n\n–> MOAIAnim self\n–> number nLinks\n<– nil",
                args = "(MOAIAnim self, number nLinks)",
                returns = "nil"
            },
            setLink = {
                type = "method",
                description = "Connect a curve to a given node attribute.\n\n–> MOAIAnim self\n–> number linkID\n–> MOAIAnimCurveBase curve\n–> MOAINode target: Target node.\n–> number attrID: Attribute of the target node to be driven by the curve.\n[–> boolean asDelta: 'true' to apply the curve as a delta instead of an absolute. Default value is false.]\n<– nil",
                args = "(MOAIAnim self, number linkID, MOAIAnimCurveBase curve, MOAINode target, number attrID, [boolean asDelta])",
                returns = "nil"
            }
        }
    },
    MOAIAnimCurve = {
        type = "class",
        inherits = "MOAIAnimCurveBase",
        description = "Implementation of animation curve for floating point values.",
        childs = {
            getValueAtTime = {
                type = "method",
                description = "Return the interpolated value given a point in time along the curve. This does not change the curve's built in TIME attribute (it simply performs the requisite computation on demand).\n\n–> MOAIAnimCurve self\n–> number time\n<– number value: The interpolated value",
                args = "(MOAIAnimCurve self, number time)",
                returns = "number value",
                valuetype = "number"
            },
            setKey = {
                type = "method",
                description = "Initialize a key frame at a given time with a give value. Also set the transition type between the specified key frame and the next key frame.\n\n–> MOAIAnimCurve self\n–> number index: Index of the keyframe.\n–> number time: Location of the key frame along the curve.\n–> number value: Value of the curve at time.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n[–> number weight: Blends between chosen ease type (of any) and a linear transition. Defaults to 1.]\n<– nil",
                args = "(MOAIAnimCurve self, number index, number time, number value, [number mode, [number weight]])",
                returns = "nil"
            }
        }
    },
    MOAIAnimCurveBase = {
        type = "class",
        inherits = "MOAINode",
        description = "Piecewise animation function with one input (time) and one output (value). This is the base class for typed animation curves (float, quaternion, etc.).",
        childs = {
            APPEND = {
                type = "value"
            },
            ATTR_TIME = {
                type = "value"
            },
            ATTR_VALUE = {
                type = "value"
            },
            CLAMP = {
                type = "value"
            },
            MIRROR = {
                type = "value"
            },
            WRAP = {
                type = "value"
            },
            getLength = {
                type = "method",
                description = "Return the largest key frame time value in the curve.\n\n–> MOAIAnimCurveBase self\n<– number length",
                args = "MOAIAnimCurveBase self",
                returns = "number length",
                valuetype = "number"
            },
            reserveKeys = {
                type = "method",
                description = "Reserve key frames.\n\n–> MOAIAnimCurveBase self\n–> number nKeys\n<– nil",
                args = "(MOAIAnimCurveBase self, number nKeys)",
                returns = "nil"
            },
            setWrapMode = {
                type = "method",
                description = "Sets the wrap mode for values above 1.0 and below 0.0. CLAMP sets all values above and below 1.0 and 0.0 to values at 1.0 and 0.0 respectively\n\n–> MOAIAnimCurveBase self\n[–> number mode: One of MOAIAnimCurveBase.CLAMP, MOAIAnimCurveBase.WRAP, MOAIAnimCurveBase.MIRROR, MOAIAnimCurveBase.APPEND. Default value is MOAIAnimCurveBase.CLAMP.]\n<– nil",
                args = "(MOAIAnimCurveBase self, [number mode])",
                returns = "nil"
            }
        }
    },
    MOAIAnimCurveQuat = {
        type = "class",
        inherits = "MOAIAnimCurveBase",
        description = "Implementation of animation curve for rotation (via quaternion) values.",
        childs = {
            getValueAtTime = {
                type = "method",
                description = "Return the interpolated value (as Euler angles) given a point in time along the curve. This does not change the curve's built in TIME attribute (it simply performs the requisite computation on demand).\n\n–> MOAIAnimCurveQuat self\n–> number time\n<– number xRot\n<– number yRot\n<– number zRot",
                args = "(MOAIAnimCurveQuat self, number time)",
                returns = "(number xRot, number yRot, number zRot)",
                valuetype = "number"
            },
            setKey = {
                type = "method",
                description = "Initialize a key frame at a given time with a give value (as Euler angles). Also set the transition type between the specified key frame and the next key frame.\n\n–> MOAIAnimCurveQuat self\n–> number index: Index of the keyframe.\n–> number time: Location of the key frame along the curve.\n–> number xRot: X rotation at time.\n–> number yRot: Y rotation at time.\n–> number zRot: Z rotation at time.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n[–> number weight: Blends between chosen ease type (of any) and a linear transition. Defaults to 1.]\n<– nil",
                args = "(MOAIAnimCurveQuat self, number index, number time, number xRot, number yRot, number zRot, [number mode, [number weight]])",
                returns = "nil"
            }
        }
    },
    MOAIAnimCurveVec = {
        type = "class",
        inherits = "MOAIAnimCurveBase",
        description = "Implementation of animation curve for 3D vector values.",
        childs = {
            getValueAtTime = {
                type = "method",
                description = "Return the interpolated vector components given a point in time along the curve. This does not change the curve's built in TIME attribute (it simply performs the requisite computation on demand).\n\n–> MOAIAnimCurveVec self\n–> number time\n<– number x\n<– number y\n<– number z",
                args = "(MOAIAnimCurveVec self, number time)",
                returns = "(number x, number y, number z)",
                valuetype = "number"
            },
            setKey = {
                type = "method",
                description = "Initialize a key frame at a given time with a give vector. Also set the transition type between the specified key frame and the next key frame.\n\n–> MOAIAnimCurveVec self\n–> number index: Index of the keyframe.\n–> number time: Location of the key frame along the curve.\n–> number x: X component at time.\n–> number y: Y component at time.\n–> number z: Z component at time.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n[–> number weight: Blends between chosen ease type (of any) and a linear transition. Defaults to 1.]\n<– nil",
                args = "(MOAIAnimCurveVec self, number index, number time, number x, number y, number z, [number mode, [number weight]])",
                returns = "nil"
            }
        }
    },
    MOAIApp = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {
            alert = {
                type = "function",
                description = "Display a modal style dialog box with one or more buttons, including a cancel button. This will not halt execution (this function returns immediately), so if you need to respond to the user's selection, pass a callback.\n\n–> string title: The title of the dialog box.\n–> string message: The message to display.\n–> function callback: The function that will receive an integer index as which button was pressed.\n–> string cancelTitle: The title of the cancel button.\n–> string... buttons: Other buttons to add to the alert box.\n<– nil",
                args = "(string title, string message, function callback, string cancelTitle, string... buttons)",
                returns = "nil"
            },
            canMakePayments = {
                type = "function",
                description = "Verify that the app has permission to request payments.\n\n<– boolean canMakePayments",
                args = "()",
                returns = "boolean canMakePayments",
                valuetype = "boolean"
            },
            getDirectoryInDomain = {
                type = "function",
                description = "Search the platform's internal directory structure for a special directory as defined by the platform.\n\n–> number domain: One of MOAIApp.DOMAIN_DOCUMENTS, MOAIApp.DOMAIN_APP_SUPPORT\n<– string directory: The directory associated with the given domain.",
                args = "number domain",
                returns = "string directory",
                valuetype = "string"
            },
            requestPaymentForProduct = {
                type = "function",
                description = "Request payment for a product.\n\n–> string productIdentifier\n[–> number quantity: Default value is 1.]\n<– nil",
                args = "(string productIdentifier, [number quantity])",
                returns = "nil"
            },
            requestProductIdentifiers = {
                type = "function",
                description = "Verify the validity of a set of products.\n\n–> table productIdentifiers: A table of product identifiers.\n<– nil",
                args = "table productIdentifiers",
                returns = "nil"
            },
            restoreCompletedTransactions = {
                type = "function",
                description = "Request a restore of all purchased non-consumables from the App Store. Use this to retrieve a list of all previously purchased items (for example after reinstalling the app on a different device).\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            setListener = {
                type = "function",
                description = "Set a callback to handle events of a type.\n\n–> number event: One of MOAIApp.ERROR, MOAIApp.DID_REGISTER, MOAIApp.REMOTE_NOTIFICATION, MOAIApp.PAYMENT_QUEUE_TRANSACTION, MOAIApp.PRODUCT_REQUEST_RESPONSE.\n[–> function handler]\n<– nil",
                args = "(number event, [function handler])",
                returns = "nil"
            }
        }
    },
    MOAIAppAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for base application class on Android devices. Exposed to Lua via MOAIApp on all mobile platforms.",
        childs = {
            BACK_BUTTON_PRESSED = {
                type = "value",
                description = "Event code indicating that the physical device back button was pressed."
            },
            SESSION_END = {
                type = "value",
                description = "Event code indicating the end of an app sessions."
            },
            SESSION_START = {
                type = "value",
                description = "Event code indicating the beginning of an app session."
            },
            getStatusBarHeight = {
                type = "function",
                description = "Gets the Height of an Android 3.x status bar\n\n<– number height",
                args = "()",
                returns = "number height",
                valuetype = "number"
            },
            getUTCTime = {
                type = "function",
                description = "Gets the UTC time.\n\n<– number time: UTC Time",
                args = "()",
                returns = "number time",
                valuetype = "number"
            },
            sendMail = {
                type = "function",
                description = "Send a mail with the passed in default values\n\n–> string recipient\n–> string subject\n–> string message\n<– nil",
                args = "(string recipient, string subject, string message)",
                returns = "nil"
            },
            share = {
                type = "function",
                description = "Open a generic Android dialog to allow the user to share via email, SMS, Facebook, Twitter, etc.\n\n–> string prompt: The prompt to show the user.\n–> string subject: The subject of the message to share.\n–> string text: The text of the message to share.\n<– nil",
                args = "(string prompt, string subject, string text)",
                returns = "nil"
            }
        }
    },
    MOAIAppIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for base application class on iOS devices. Exposed to Lua via MOAIApp on all mobile platforms.",
        childs = {
            APP_OPENED_FROM_URL = {
                type = "value",
                description = "Event code indicating that the app was stared via a URL click."
            },
            DOMAIN_APP_SUPPORT = {
                type = "value",
                description = "Directory domain 'application support'."
            },
            DOMAIN_CACHES = {
                type = "value",
                description = "Directory domain 'caches'."
            },
            DOMAIN_DOCUMENTS = {
                type = "value",
                description = "Directory domain 'documents'."
            },
            INTERFACE_ORIENTATION_LANDSCAPE_LEFT = {
                type = "value",
                description = "Interface orientation UIInterfaceOrientationLandscapeLeft."
            },
            INTERFACE_ORIENTATION_LANDSCAPE_RIGHT = {
                type = "value",
                description = "Interface orientation UIInterfaceOrientationLandscapeRight."
            },
            INTERFACE_ORIENTATION_PORTRAIT = {
                type = "value",
                description = "Interface orientation UIInterfaceOrientationPortrait."
            },
            INTERFACE_ORIENTATION_PORTRAIT_UPSIDE_DOWN = {
                type = "value",
                description = "Interface orientation UIInterfaceOrientationPortraitUpsideDown."
            },
            SESSION_END = {
                type = "value",
                description = "Event code indicating the end of an app sessions."
            },
            SESSION_START = {
                type = "value",
                description = "Event code indicating the beginning of an app session."
            }
        }
    },
    MOAIAudioSampler = {
        type = "class",
        inherits = "MOAINode",
        description = "Audio sampler singleton",
        childs = {}
    },
    MOAIBillingAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for in-app purchase integration on Android devices using either Google Play or Amazon. Exposed to Lua via MOAIBilling on all mobile platforms, but API differs on iOS and Android.",
        childs = {
            BILLING_PROVIDER_AMAZON = {
                type = "value",
                description = "Provider code for Amazon."
            },
            BILLING_PROVIDER_GOOGLE = {
                type = "value",
                description = "Provider code for Google Play."
            },
            BILLING_PURCHASE_STATE_ITEM_PURCHASED = {
                type = "value",
                description = "Purchase state code for a successfully purchased item."
            },
            BILLING_PURCHASE_STATE_ITEM_REFUNDED = {
                type = "value",
                description = "Purchase state code for a refunded/revoked purchase."
            },
            BILLING_PURCHASE_STATE_PURCHASE_CANCELED = {
                type = "value",
                description = "Purchase state code for a canceled purchase."
            },
            BILLING_RESULT_BILLING_UNAVAILABLE = {
                type = "value",
                description = "Error code for a billing request attempted with no billing provider present."
            },
            BILLING_RESULT_ERROR = {
                type = "value",
                description = "Error code for a billing request error."
            },
            BILLING_RESULT_ITEM_UNAVAILABLE = {
                type = "value",
                description = "Error code for a billing request for an unavailable item."
            },
            BILLING_RESULT_SUCCESS = {
                type = "value",
                description = "Error code for a successful billing request."
            },
            BILLING_RESULT_USER_CANCELED = {
                type = "value",
                description = "Error code for a billing request canceled by the user, if detected."
            },
            CHECK_BILLING_SUPPORTED = {
                type = "value",
                description = "Event code for billing support request completion."
            },
            PURCHASE_RESPONSE_RECEIVED = {
                type = "value",
                description = "Event code for item purchase request receipt."
            },
            PURCHASE_STATE_CHANGED = {
                type = "value",
                description = "Event code for item purchase state change (purchased, refunded, etc.)."
            },
            RESTORE_RESPONSE_RECEIVED = {
                type = "value",
                description = "Event code for restore purchases request receipt."
            },
            USER_ID_DETERMINED = {
                type = "value",
                description = "Event code for user ID request completion."
            },
            checkBillingSupported = {
                type = "function",
                description = "Check to see if the currently selected billing provider is available.\n\n<– boolean success: True, if the request was successfully initiated.",
                args = "()",
                returns = "boolean success",
                valuetype = "boolean"
            },
            checkInAppSupported = {
                type = "function",
                description = "Check to see if the device can get in app billing\n\n<– boolean success",
                args = "()",
                returns = "boolean success",
                valuetype = "boolean"
            },
            checkSubscriptionSupported = {
                type = "function",
                description = "Check to see if the device can get subscription billing\n\n<– boolean success",
                args = "()",
                returns = "boolean success",
                valuetype = "boolean"
            },
            confirmNotification = {
                type = "function",
                description = "Confirm a previously received notification. Only applies to the Google Play billing provider.\n\n–> string notification: The notification ID to confirm.\n<– boolean success: True, if the request was successfully initiated.",
                args = "string notification",
                returns = "boolean success",
                valuetype = "boolean"
            },
            consumePurchaseSync = {
                type = "function",
                description = "Consumes a purchase\n\n–> string token\n<– nil",
                args = "string token",
                returns = "nil"
            },
            getPurchasedProducts = {
                type = "function",
                description = "Gets the user's purchased products\n\n–> number type\n[–> string continuation]\n<– string products: JSON string of products",
                args = "(number type, [string continuation])",
                returns = "string products",
                valuetype = "string"
            },
            getUserId = {
                type = "function",
                description = "Get the ID of the current user for the currently selected billing provider. Only applies to the Amazon billing provider.\n\n<– boolean success: True, if the request was successfully initiated.",
                args = "()",
                returns = "boolean success",
                valuetype = "boolean"
            },
            purchaseProduct = {
                type = "function",
                description = "Starts a purchase intent for the desired product\n\n–> string sku\n–> number type\n[–> string devPayload]\n<– nil",
                args = "(string sku, number type, [string devPayload])",
                returns = "nil"
            },
            requestProductsSync = {
                type = "function",
                description = "Gets the products from Google Play for the current app\n\n–> table skus\n–> number type\n<– string products: JSON string of products",
                args = "(table skus, number type)",
                returns = "string products",
                valuetype = "string"
            },
            requestPurchase = {
                type = "function",
                description = "Request the purchase of an item.\n\n–> string sku: The SKU to purchase.\n[–> string payload: The request payload to be returned upon request completion. Default is nil.]\n<– boolean success: True, if the request was successfully initiated.",
                args = "(string sku, [string payload])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            restoreTransactions = {
                type = "function",
                description = "Request the restoration of any previously purchased items.\n\n[–> string offset: The offset in the paginated results to start from. Only applies to the Amazon billing provider. Default is nil.]\n<– boolean success: True, if the request was successfully initiated.",
                args = "[string offset]",
                returns = "boolean success",
                valuetype = "boolean"
            },
            setBillingProvider = {
                type = "function",
                description = "Set the billing provider to use for in-app purchases.\n\n–> number provider: The billing provider.\n<– boolean success: True, if the provider was successfully set.",
                args = "number provider",
                returns = "boolean success",
                valuetype = "boolean"
            },
            setPublicKey = {
                type = "function",
                description = "Set the public key to be used for receipt verification. Only applies to the Google Play billing provider.\n\n–> string key: The public key.\n<– nil",
                args = "string key",
                returns = "nil"
            }
        }
    },
    MOAIBillingIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for in-app purchase integration on iOS devices using Apple StoreKit. Exposed to Lua via MOAIBilling on all mobile platforms, but API differs on iOS and Android.",
        childs = {
            PAYMENT_QUEUE_ERROR = {
                type = "value",
                description = "Event invoked when a transaction fails."
            },
            PAYMENT_QUEUE_TRANSACTION = {
                type = "value",
                description = "Event invoked when a transaction changes state."
            },
            PRODUCT_REQUEST_RESPONSE = {
                type = "value",
                description = "Event invoked when a product information request completes."
            },
            PRODUCT_RESTORE_FINISHED = {
                type = "value",
                description = "Event invoked when a transactions restore is finished."
            },
            TRANSACTION_STATE_CANCELLED = {
                type = "value",
                description = "Error code indicating a canceled transaction."
            },
            TRANSACTION_STATE_FAILED = {
                type = "value",
                description = "Error code indicating a failed transaction."
            },
            TRANSACTION_STATE_PURCHASED = {
                type = "value",
                description = "Error code indicating a completed transaction."
            },
            TRANSACTION_STATE_PURCHASING = {
                type = "value",
                description = "Error code indicating a transaction in progress."
            },
            TRANSACTION_STATE_RESTORED = {
                type = "value",
                description = "Error code indicating a restored transaction."
            }
        }
    },
    MOAIBitmapFontReader = {
        type = "class",
        inherits = "MOAIFontReader MOAILuaObject",
        description = "Legacy font reader for Moai's original bitmap font format. The original format is just a bitmap containing each glyph in the font divided by solid-color guide lines (see examples). This is an easy way for artists to create bitmap fonts. Kerning is not supported by this format.\nRuntime use of MOAIBitmapFontReader is not recommended. Instead, use MOAIBitmapFontReader as part of your tool chain to initialize a glyph cache and image to be serialized in later.",
        childs = {
            loadPage = {
                type = "method",
                description = "Rips a set of glyphs from a bitmap and associates them with a size.\n\n–> MOAIBitmapFontReader self\n–> string filename: Filename of the image containing the bitmap font.\n–> string charCodes: A string which defines the characters found in the bitmap\n–> number points: The point size to be associated with the glyphs ripped from the bitmap.\n[–> number dpi: The device DPI (dots per inch of device screen). Default value is 72 (points same as pixels).]\n<– nil",
                args = "(MOAIBitmapFontReader self, string filename, string charCodes, number points, [number dpi])",
                returns = "nil"
            }
        }
    },
    MOAIBlocker = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAIBoundsDeck = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Deck of bounding boxes. Bounding boxes are allocated in a separate array from that used for box indices. The index array is used to map deck indices onto bounding boxes. In other words there may be more indices then boxes thus allowing for re-use of boxes over multiple indices.\nThe main purpose of the bounds deck is to override the default bounds of elements inside of another deck. The bounds deck may be attached to any other type of deck by using MOAIDeck's setBoundsDeck () method.",
        childs = {
            reserveBounds = {
                type = "method",
                description = "Reserve an array of bounds to be indexed.\n\n–> MOAIBoundsDeck self\n–> number nBounds\n<– nil",
                args = "(MOAIBoundsDeck self, number nBounds)",
                returns = "nil"
            },
            reserveIndices = {
                type = "method",
                description = "Reserve indices. Each index maps a deck item onto a bounding box.\n\n–> MOAIBoundsDeck self\n–> number nIndices\n<– nil",
                args = "(MOAIBoundsDeck self, number nIndices)",
                returns = "nil"
            },
            setBounds = {
                type = "method",
                description = "Set the dimensions of a bounding box at a given index.\n\n–> MOAIBoundsDeck self\n–> number idx\n–> number xMin\n–> number yMin\n–> number zMin\n–> number xMax\n–> number yMax\n–> number zMax\n<– nil",
                args = "(MOAIBoundsDeck self, number idx, number xMin, number yMin, number zMin, number xMax, number yMax, number zMax)",
                returns = "nil"
            },
            setIndex = {
                type = "method",
                description = "Associate a deck index with a bounding box.\n\n–> MOAIBoundsDeck self\n–> number idx\n–> number boundsID\n<– nil",
                args = "(MOAIBoundsDeck self, number idx, number boundsID)",
                returns = "nil"
            }
        }
    },
    MOAIBox2DArbiter = {
        type = "class",
        inherits = "MOAILuaObject b2ContactListener",
        description = "Box2D Arbiter.",
        childs = {
            ALL = {
                type = "value"
            },
            BEGIN = {
                type = "value"
            },
            END = {
                type = "value"
            },
            POST_SOLVE = {
                type = "value"
            },
            PRE_SOLVE = {
                type = "value"
            },
            getContactNormal = {
                type = "method",
                description = "Returns the normal for the contact.\n\n–> MOAIBox2DArbiter self\n<– number normal.x\n<– number normal.y",
                args = "MOAIBox2DArbiter self",
                returns = "(number normal.x, number normal.y)",
                valuetype = "number"
            },
            getNormalImpulse = {
                type = "method",
                description = "Returns total normal impulse for contact.\n\n–> MOAIBox2DArbiter self\n<– number impulse: Impulse in kg * units / s converted from kg * m / s",
                args = "MOAIBox2DArbiter self",
                returns = "number impulse",
                valuetype = "number"
            },
            getTangentImpulse = {
                type = "method",
                description = "Returns total tangent impulse for contact.\n\n–> MOAIBox2DArbiter self\n<– number impulse: Impulse in kg * units / s converted from kg * m / s",
                args = "MOAIBox2DArbiter self",
                returns = "number impulse",
                valuetype = "number"
            },
            setContactEnabled = {
                type = "method",
                description = "Enabled or disable the contact.\n\n–> MOAIBox2DArbiter self\n–> boolean enabled\n<– nil",
                args = "(MOAIBox2DArbiter self, boolean enabled)",
                returns = "nil"
            }
        }
    },
    MOAIBox2DBody = {
        type = "class",
        inherits = "MOAIBox2DPrim MOAITransformBase",
        description = "Box2D body.",
        childs = {
            DYNAMIC = {
                type = "value"
            },
            KINEMATIC = {
                type = "value"
            },
            STATIC = {
                type = "value"
            },
            addChain = {
                type = "method",
                description = "Create and add a set of collision edges to the body.\n\n–> MOAIBox2DBody self\n–> table verts: Array containing vertex coordinate components ( t[1] = x0, t[2] = y0, t[3] = x1, t[4] = y1... )\n[–> boolean closeChain: Default value is false.]\n<– MOAIBox2DFixture fixture: Returns nil on failure.",
                args = "(MOAIBox2DBody self, table verts, [boolean closeChain])",
                returns = "MOAIBox2DFixture fixture",
                valuetype = "MOAIBox2DFixture"
            },
            addCircle = {
                type = "method",
                description = "Create and add circle fixture to the body.\n\n–> MOAIBox2DBody self\n–> number x: in units, world coordinates, converted to meters\n–> number y: in units, world coordinates, converted to meters\n–> number radius: in units, converted to meters\n<– MOAIBox2DFixture fixture",
                args = "(MOAIBox2DBody self, number x, number y, number radius)",
                returns = "MOAIBox2DFixture fixture",
                valuetype = "MOAIBox2DFixture"
            },
            addEdges = {
                type = "method",
                description = "Create and add a polygon fixture to the body.\n\n–> MOAIBox2DBody self\n–> table verts: Array containing vertex coordinate components in units, world coordinates, converted to meters ( t[1] = x0, t[2] = y0, t[3] = x1, t[4] = y1... )\n<– table fixtures: Array containing MOAIBox2DFixture fixtures. Returns nil on failure.",
                args = "(MOAIBox2DBody self, table verts)",
                returns = "table fixtures",
                valuetype = "table"
            },
            addPolygon = {
                type = "method",
                description = "Create and add a polygon fixture to the body.\n\n–> MOAIBox2DBody self\n–> table verts: Array containing vertex coordinate components in units, world coordinates, converted to meters. ( t[1] = x0, t[2] = y0, t[3] = x1, t[4] = y1... )\n<– MOAIBox2DFixture fixture: Returns nil on failure.",
                args = "(MOAIBox2DBody self, table verts)",
                returns = "MOAIBox2DFixture fixture",
                valuetype = "MOAIBox2DFixture"
            },
            addRect = {
                type = "method",
                description = "Create and add a rect fixture to the body.\n\n–> MOAIBox2DBody self\n–> number xMin: in units, world coordinates, converted to meters\n–> number yMin: in units, world coordinates, converted to meters\n–> number xMax: in units, world coordinates, converted to meters\n–> number yMax: in units, world coordinates, converted to meters\n–> number angle\n<– MOAIBox2DFixture fixture",
                args = "(MOAIBox2DBody self, number xMin, number yMin, number xMax, number yMax, number angle)",
                returns = "MOAIBox2DFixture fixture",
                valuetype = "MOAIBox2DFixture"
            },
            applyAngularImpulse = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n–> number angularImpulse: in kg * units / s, converted to kg * m / s\n<– nil",
                args = "(MOAIBox2DBody self, number angularImpulse)",
                returns = "nil"
            },
            applyForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n–> number forceX: in kg * units / s^2, converted to N [kg * m / s^2]\n–> number forceY: in kg * units / s^2, converted to N [kg * m / s^2]\n[–> number pointX: in units, world coordinates, converted to meters]\n[–> number pointY: in units, world coordinates, converted to meters]\n<– nil",
                args = "(MOAIBox2DBody self, number forceX, number forceY, [number pointX, [number pointY]])",
                returns = "nil"
            },
            applyLinearImpulse = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n–> number impulseX: in kg * units / s, converted to kg * m / s\n–> number impulseY: in kg * units / s, converted to kg * m / s\n[–> number pointX: in units, world coordinates, converted to meters]\n[–> number pointY: in units, world coordinates, converted to meters]\n<– nil",
                args = "(MOAIBox2DBody self, number impulseX, number impulseY, [number pointX, [number pointY]])",
                returns = "nil"
            },
            applyTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> number torque: in (kg * units / s^2) * units, converted to N-m. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DBody self, [number torque])",
                returns = "nil"
            },
            destroy = {
                type = "method",
                description = "Schedule body for destruction.\n\n–> MOAIBox2DBody self\n<– nil",
                args = "MOAIBox2DBody self",
                returns = "nil"
            },
            getAngle = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number angle: Angle in degrees, converted from radians",
                args = "MOAIBox2DBody self",
                returns = "number angle",
                valuetype = "number"
            },
            getAngularVelocity = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number omega: Angular velocity in degrees/s, converted from radians/s",
                args = "MOAIBox2DBody self",
                returns = "number omega",
                valuetype = "number"
            },
            getInertia = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number inertia: Calculated inertia (based on last call to resetMassData()). In kg * unit/s^s, converted from kg*m/s^2.",
                args = "MOAIBox2DBody self",
                returns = "number inertia",
                valuetype = "number"
            },
            getLinearVelocity = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number velocityX: in unit/s, converted from m/s\n<– number velocityY: in unit/s, converted from m/s",
                args = "MOAIBox2DBody self",
                returns = "(number velocityX, number velocityY)",
                valuetype = "number"
            },
            getLocalCenter = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number centerX: in units, local coordinates, converted from meters\n<– number centerY: in units, local coordinates, converted from meters",
                args = "MOAIBox2DBody self",
                returns = "(number centerX, number centerY)",
                valuetype = "number"
            },
            getMass = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number mass: Calculated mass in kg (based on last call to resetMassData()).",
                args = "MOAIBox2DBody self",
                returns = "number mass",
                valuetype = "number"
            },
            getPosition = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number positionX: in units, world coordinates, converted from meters\n<– number positionY: in units, world coordinates, converted from meters",
                args = "MOAIBox2DBody self",
                returns = "(number positionX, number positionY)",
                valuetype = "number"
            },
            getWorldCenter = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– number worldX: in units, world coordinates, converted from meters\n<– number worldY: in units, world coordinates, converted from meters",
                args = "MOAIBox2DBody self",
                returns = "(number worldX, number worldY)",
                valuetype = "number"
            },
            isActive = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– boolean isActive",
                args = "MOAIBox2DBody self",
                returns = "boolean isActive",
                valuetype = "boolean"
            },
            isAwake = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– boolean isAwake",
                args = "MOAIBox2DBody self",
                returns = "boolean isAwake",
                valuetype = "boolean"
            },
            isBullet = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– boolean isBullet",
                args = "MOAIBox2DBody self",
                returns = "boolean isBullet",
                valuetype = "boolean"
            },
            isFixedRotation = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– boolean isFixedRotation",
                args = "MOAIBox2DBody self",
                returns = "boolean isFixedRotation",
                valuetype = "boolean"
            },
            resetMassData = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n<– nil",
                args = "MOAIBox2DBody self",
                returns = "nil"
            },
            setActive = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> boolean active: Default value is false.]\n<– nil",
                args = "(MOAIBox2DBody self, [boolean active])",
                returns = "nil"
            },
            setAngularDamping = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n–> number damping\n<– nil",
                args = "(MOAIBox2DBody self, number damping)",
                returns = "nil"
            },
            setAngularVelocity = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> number omega: Angular velocity in degrees/s, converted to radians/s. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DBody self, [number omega])",
                returns = "nil"
            },
            setAwake = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> boolean awake: Default value is true.]\n<– nil",
                args = "(MOAIBox2DBody self, [boolean awake])",
                returns = "nil"
            },
            setBullet = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> boolean bullet: Default value is true.]\n<– nil",
                args = "(MOAIBox2DBody self, [boolean bullet])",
                returns = "nil"
            },
            setFixedRotation = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> boolean fixedRotation: Default value is true.]\n<– nil",
                args = "(MOAIBox2DBody self, [boolean fixedRotation])",
                returns = "nil"
            },
            setLinearDamping = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> number damping]\n<– nil",
                args = "(MOAIBox2DBody self, [number damping])",
                returns = "nil"
            },
            setLinearVelocity = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> number velocityX: in unit/s, converted to m/s. Default is 0.]\n[–> number velocityY: in unit/s, converted to m/s. Default is 0.]\n<– nil",
                args = "(MOAIBox2DBody self, [number velocityX, [number velocityY]])",
                returns = "nil"
            },
            setMassData = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n–> number mass: in kg.\n[–> number I: in kg*units^2, converted to kg * m^2. Default is previous value for I.]\n[–> number centerX: in units, local coordinates, converted to meters. Default is previous value for centerX.]\n[–> number centerY: in units, local coordinates, converted to meters. Default is previous value for centerY.]\n<– nil",
                args = "(MOAIBox2DBody self, number mass, [number I, [number centerX, [number centerY]]])",
                returns = "nil"
            },
            setTransform = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n[–> number positionX: in units, world coordinates, converted to meters. Default is 0.]\n[–> number positionY: in units, world coordinates, converted to meters. Default is 0.]\n[–> number angle: In degrees, converted to radians. Default is 0.]\n<– nil",
                args = "(MOAIBox2DBody self, [number positionX, [number positionY, [number angle]]])",
                returns = "nil"
            },
            setType = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DBody self\n–> number type: One of MOAIBox2DBody.DYNAMIC, MOAIBox2DBody.KINEMATIC, MOAIBox2DBody.STATIC\n<– nil",
                args = "(MOAIBox2DBody self, number type)",
                returns = "nil"
            }
        }
    },
    MOAIBox2DDistanceJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D distance joint.",
        childs = {
            getDampingRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DDistanceJoint self\n<– number dampingRatio",
                args = "MOAIBox2DDistanceJoint self",
                returns = "number dampingRatio",
                valuetype = "number"
            },
            getFrequency = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DDistanceJoint self\n<– number frequency: In Hz.",
                args = "MOAIBox2DDistanceJoint self",
                returns = "number frequency",
                valuetype = "number"
            },
            getLength = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DDistanceJoint self\n<– number length: In units, converted from meters.",
                args = "MOAIBox2DDistanceJoint self",
                returns = "number length",
                valuetype = "number"
            },
            setDampingRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DDistanceJoint self\n[–> number dampingRatio: Default value is 0.]\n<– nil",
                args = "(MOAIBox2DDistanceJoint self, [number dampingRatio])",
                returns = "nil"
            },
            setFrequency = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DDistanceJoint self\n[–> number frequency: In Hz. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DDistanceJoint self, [number frequency])",
                returns = "nil"
            },
            setLength = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DDistanceJoint self\n[–> number length: in units, converted to meters. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DDistanceJoint self, [number length])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DFixture = {
        type = "class",
        inherits = "MOAIBox2DPrim MOAILuaObject",
        description = "Box2D fixture.",
        childs = {
            destroy = {
                type = "method",
                description = "Schedule fixture for destruction.\n\n–> MOAIBox2DFixture self\n<– nil",
                args = "MOAIBox2DFixture self",
                returns = "nil"
            },
            getBody = {
                type = "method",
                description = "Returns the body that owns the fixture.\n\n–> MOAIBox2DFixture self\n<– MOAIBox2DBody body",
                args = "MOAIBox2DFixture self",
                returns = "MOAIBox2DBody body",
                valuetype = "MOAIBox2DBody"
            },
            getFilter = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFixture self\n<– number categoryBits\n<– number maskBits\n<– number groupIndex",
                args = "MOAIBox2DFixture self",
                returns = "(number categoryBits, number maskBits, number groupIndex)",
                valuetype = "number"
            },
            setCollisionHandler = {
                type = "method",
                description = "Sets a Lua function to call when collisions occur. The handler should accept the following parameters: ( phase, fixtureA, fixtureB, arbiter ). 'phase' will be one of the phase masks. 'fixtureA' will be the fixture receiving the collision. 'fixtureB' will be the other fixture in the collision. 'arbiter' will be the MOAIArbiter. Note that the arbiter is only good for the current collision: do not keep references to it for later use.\n\n–> MOAIBox2DFixture self\n–> function handler\n[–> number phaseMask: Any bitwise combination of MOAIBox2DArbiter.BEGIN, MOAIBox2DArbiter.END, MOAIBox2DArbiter.POST_SOLVE, MOAIBox2DArbiter.PRE_SOLVE, MOAIBox2DArbiter.ALL]\n[–> number categoryMask: Check against opposing fixture's category bits and generate collision events if match.]\n<– nil",
                args = "(MOAIBox2DFixture self, function handler, [number phaseMask, [number categoryMask]])",
                returns = "nil"
            },
            setDensity = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFixture self\n–> number density: In kg/units^2, converted to kg/m^2\n<– nil",
                args = "(MOAIBox2DFixture self, number density)",
                returns = "nil"
            },
            setFilter = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFixture self\n–> number categoryBits\n[–> number maskBits]\n[–> number groupIndex]\n<– nil",
                args = "(MOAIBox2DFixture self, number categoryBits, [number maskBits, [number groupIndex]])",
                returns = "nil"
            },
            setFriction = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFixture self\n–> number friction\n<– nil",
                args = "(MOAIBox2DFixture self, number friction)",
                returns = "nil"
            },
            setRestitution = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFixture self\n–> number restitution\n<– nil",
                args = "(MOAIBox2DFixture self, number restitution)",
                returns = "nil"
            },
            setSensor = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFixture self\n[–> boolean isSensor: Default value is 'true']\n<– nil",
                args = "(MOAIBox2DFixture self, [boolean isSensor])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DFrictionJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D friction joint.",
        childs = {
            getMaxForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFrictionJoint self\n<– number maxForce: in kg * units / s^2, converted from N [kg * m / s^2].",
                args = "MOAIBox2DFrictionJoint self",
                returns = "number maxForce",
                valuetype = "number"
            },
            getMaxTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFrictionJoint self\n<– number maxTorque: in (kg * units / s^2) * units, converted from N-m.",
                args = "MOAIBox2DFrictionJoint self",
                returns = "number maxTorque",
                valuetype = "number"
            },
            setMaxForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFrictionJoint self\n[–> number maxForce: in kg * units / s^2, converted to N [kg * m / s^2]. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DFrictionJoint self, [number maxForce])",
                returns = "nil"
            },
            setMaxTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DFrictionJoint self\n[–> number maxTorque: in (kg * units / s^2) * units, converted to N-m. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DFrictionJoint self, [number maxTorque])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DGearJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D gear joint.",
        childs = {
            getJointA = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DGearJoint self\n<– MOAIBox2DJoint jointA",
                args = "MOAIBox2DGearJoint self",
                returns = "MOAIBox2DJoint jointA",
                valuetype = "MOAIBox2DJoint"
            },
            getJointB = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DGearJoint self\n<– MOAIBox2DJoint jointB",
                args = "MOAIBox2DGearJoint self",
                returns = "MOAIBox2DJoint jointB",
                valuetype = "MOAIBox2DJoint"
            },
            getRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DGearJoint self\n<– number ratio",
                args = "MOAIBox2DGearJoint self",
                returns = "number ratio",
                valuetype = "number"
            },
            setRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DGearJoint self\n[–> number ratio: Default value is 0.]\n<– nil",
                args = "(MOAIBox2DGearJoint self, [number ratio])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DJoint = {
        type = "class",
        inherits = "MOAIBox2DPrim",
        description = "Box2D joint.",
        childs = {
            destroy = {
                type = "method",
                description = "Schedule joint for destruction.\n\n–> MOAIBox2DJoint self\n<– nil",
                args = "MOAIBox2DJoint self",
                returns = "nil"
            },
            getAnchorA = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DJoint self\n<– number anchorX: in units, in world coordinates, converted to meters\n<– number anchorY: in units, in world coordinates, converted to meters",
                args = "MOAIBox2DJoint self",
                returns = "(number anchorX, number anchorY)",
                valuetype = "number"
            },
            getAnchorB = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DJoint self\n<– number anchorX: in units, in world coordinates, converted from meters\n<– number anchorY: in units, in world coordinates, converted from meters",
                args = "MOAIBox2DJoint self",
                returns = "(number anchorX, number anchorY)",
                valuetype = "number"
            },
            getBodyA = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DJoint self\n<– MOAIBox2DBody body",
                args = "MOAIBox2DJoint self",
                returns = "MOAIBox2DBody body",
                valuetype = "MOAIBox2DBody"
            },
            getBodyB = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DJoint self\n<– MOAIBox2DBody body",
                args = "MOAIBox2DJoint self",
                returns = "MOAIBox2DBody body",
                valuetype = "MOAIBox2DBody"
            },
            getReactionForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DJoint self\n<– number forceX: in kg * units / s^2 converted from N [kg * m / s^2]\n<– number forceY: in kg * units / s^2 converted from N [kg * m / s^2]",
                args = "MOAIBox2DJoint self",
                returns = "(number forceX, number forceY)",
                valuetype = "number"
            },
            getReactionTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DJoint self\n<– number reactionTorque: in (kg * units / s^2) * units, converted from N-m.",
                args = "MOAIBox2DJoint self",
                returns = "number reactionTorque",
                valuetype = "number"
            }
        }
    },
    MOAIBox2DMouseJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D 'mouse' joint.",
        childs = {
            getDampingRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n<– number dampingRatio",
                args = "MOAIBox2DMouseJoint self",
                returns = "number dampingRatio",
                valuetype = "number"
            },
            getFrequency = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n<– number frequency: in Hz",
                args = "MOAIBox2DMouseJoint self",
                returns = "number frequency",
                valuetype = "number"
            },
            getMaxForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n<– number maxForce: in kg * units / s^2 converted from N [kg * m / s^2]",
                args = "MOAIBox2DMouseJoint self",
                returns = "number maxForce",
                valuetype = "number"
            },
            getTarget = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n<– number x: in units, world coordinates, converted from meters\n<– number y: in units, world coordinates, converted from meters",
                args = "MOAIBox2DMouseJoint self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            setDampingRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n[–> number dampingRatio: Default value is 0.]\n<– nil",
                args = "(MOAIBox2DMouseJoint self, [number dampingRatio])",
                returns = "nil"
            },
            setFrequency = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n[–> number frequency: in Hz. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DMouseJoint self, [number frequency])",
                returns = "nil"
            },
            setMaxForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n[–> number maxForce: in kg * units / s^2 converted to N [kg * m / s^2]. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DMouseJoint self, [number maxForce])",
                returns = "nil"
            },
            setTarget = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DMouseJoint self\n[–> number x: in units, world coordinates, converted to meters. Default value is 0.]\n[–> number y: in units, world coordinates, converted to meters. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DMouseJoint self, [number x, [number y]])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DPrim = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAIBox2DPrismaticJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D prismatic joint.",
        childs = {
            getJointSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– number jointSpeed: in units/s, converted from m/s",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "number jointSpeed",
                valuetype = "number"
            },
            getJointTranslation = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– number jointTranslation: in units, converted from meters.",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "number jointTranslation",
                valuetype = "number"
            },
            getLowerLimit = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– number lowerLimit: in units, converted from meters.",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "number lowerLimit",
                valuetype = "number"
            },
            getMotorForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– number motorForce: in kg * units / s^2, converted from N [kg * m / s^2]",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "number motorForce",
                valuetype = "number"
            },
            getMotorSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– number motorSpeed: in units/s, converted from m/s",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "number motorSpeed",
                valuetype = "number"
            },
            getUpperLimit = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– number upperLimit: in units, converted from meters.",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "number upperLimit",
                valuetype = "number"
            },
            isLimitEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– boolean limitEnabled",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "boolean limitEnabled",
                valuetype = "boolean"
            },
            isMotorEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n<– boolean motorEnabled",
                args = "MOAIBox2DPrismaticJoint self",
                returns = "boolean motorEnabled",
                valuetype = "boolean"
            },
            setLimit = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n[–> number lower: in units, converted to meters. Default value is 0.]\n[–> number upper: in units, converted to meters. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DPrismaticJoint self, [number lower, [number upper]])",
                returns = "nil"
            },
            setLimitEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n[–> boolean enabled: Default value is 'true']\n<– nil",
                args = "(MOAIBox2DPrismaticJoint self, [boolean enabled])",
                returns = "nil"
            },
            setMaxMotorForce = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n[–> number maxMotorForce: in kg * units / s^2, converted to N [kg * m / s^2]. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DPrismaticJoint self, [number maxMotorForce])",
                returns = "nil"
            },
            setMotor = {
                type = "method",
                description = "See Box2D documentation. If speed is determined to be zero, the motor is disabled, unless forceEnable is set.\n\n–> MOAIBox2DPrismaticJoint self\n[–> number speed: in units/s converted to m/s. Default value is 0.]\n[–> number maxForce: in kg * units / s^2, converted to N [kg * m / s^2]. Default value is 0.]\n[–> boolean forceEnable: Default value is false.]\n<– nil",
                args = "(MOAIBox2DPrismaticJoint self, [number speed, [number maxForce, [boolean forceEnable]]])",
                returns = "nil"
            },
            setMotorEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n[–> boolean enabled: Default value is 'true']\n<– nil",
                args = "(MOAIBox2DPrismaticJoint self, [boolean enabled])",
                returns = "nil"
            },
            setMotorSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPrismaticJoint self\n[–> number motorSpeed: in units/s, converted to m/s. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DPrismaticJoint self, [number motorSpeed])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DPulleyJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D pulley joint.",
        childs = {
            getGroundAnchorA = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPulleyJoint self\n<– number x: in units, world coordinates, converted from meters\n<– number y: in units, world coordinates, converted from meters",
                args = "MOAIBox2DPulleyJoint self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getGroundAnchorB = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPulleyJoint self\n<– number x: in units, world coordinates, converted from meters\n<– number y: in units, world coordinates, converted from meters",
                args = "MOAIBox2DPulleyJoint self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getLength1 = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPulleyJoint self\n<– number length1: in units, converted from meters.",
                args = "MOAIBox2DPulleyJoint self",
                returns = "number length1",
                valuetype = "number"
            },
            getLength2 = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPulleyJoint self\n<– number length2: in units, converted from meters.",
                args = "MOAIBox2DPulleyJoint self",
                returns = "number length2",
                valuetype = "number"
            },
            getRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DPulleyJoint self\n<– number ratio",
                args = "MOAIBox2DPulleyJoint self",
                returns = "number ratio",
                valuetype = "number"
            }
        }
    },
    MOAIBox2DRevoluteJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D revolute joint.",
        childs = {
            getJointAngle = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– number angle: in degrees, converted from radians",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "number angle",
                valuetype = "number"
            },
            getJointSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– number jointSpeed: in degrees/s, converted from radians/s",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "number jointSpeed",
                valuetype = "number"
            },
            getLowerLimit = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– number lowerLimit: in degrees, converted from radians",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "number lowerLimit",
                valuetype = "number"
            },
            getMotorSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– number motorSpeed: in degrees/s, converted from radians/s",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "number motorSpeed",
                valuetype = "number"
            },
            getMotorTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– number motorTorque: in (kg * units / s^2) * units, converted from N-m..",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "number motorTorque",
                valuetype = "number"
            },
            getUpperLimit = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– number upperLimit: in degrees, converted from radians",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "number upperLimit",
                valuetype = "number"
            },
            isLimitEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– boolean limitEnabled",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "boolean limitEnabled",
                valuetype = "boolean"
            },
            isMotorEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n<– boolean motorEnabled",
                args = "MOAIBox2DRevoluteJoint self",
                returns = "boolean motorEnabled",
                valuetype = "boolean"
            },
            setLimit = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n[–> number lower: in degrees, converted to radians. Default value is 0.]\n[–> number upper: in degrees, converted to radians. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DRevoluteJoint self, [number lower, [number upper]])",
                returns = "nil"
            },
            setLimitEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n[–> boolean enabled: Default value is 'true']\n<– nil",
                args = "(MOAIBox2DRevoluteJoint self, [boolean enabled])",
                returns = "nil"
            },
            setMaxMotorTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n[–> number maxMotorTorque: in (kg * units / s^2) * units, converted to N-m. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DRevoluteJoint self, [number maxMotorTorque])",
                returns = "nil"
            },
            setMotor = {
                type = "method",
                description = "See Box2D documentation. If speed is determined to be zero, the motor is disabled, unless forceEnable is set.\n\n–> MOAIBox2DRevoluteJoint self\n[–> number speed: in degrees/s, converted to radians/s. Default value is 0.]\n[–> number maxMotorTorque: in (kg * units / s^2) * units, converted to N-m. Default value is 0.]\n[–> boolean forceEnable: Default value is false.]\n<– nil",
                args = "(MOAIBox2DRevoluteJoint self, [number speed, [number maxMotorTorque, [boolean forceEnable]]])",
                returns = "nil"
            },
            setMotorEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n[–> boolean enabled: Default value is 'true']\n<– nil",
                args = "(MOAIBox2DRevoluteJoint self, [boolean enabled])",
                returns = "nil"
            },
            setMotorSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRevoluteJoint self\n[–> number motorSpeed: in degrees/s, converted to radians/s. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DRevoluteJoint self, [number motorSpeed])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DRopeJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D rope joint.",
        childs = {
            getLimitState = {
                type = "method",
                description = 'See Box2D documentation.\n\n–> MOAIBox2DRopeJoint self\n<– number limitState: one of the "LimitState" codes',
                args = "MOAIBox2DRopeJoint self",
                returns = "number limitState",
                valuetype = "number"
            },
            getMaxLength = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRopeJoint self\n<– number maxLength: in units, converted from meters.",
                args = "MOAIBox2DRopeJoint self",
                returns = "number maxLength",
                valuetype = "number"
            },
            setMaxLength = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DRopeJoint self\n–> number maxLength: in units, converted to meters. Default is 0.\n<– nil",
                args = "(MOAIBox2DRopeJoint self, number maxLength)",
                returns = "nil"
            }
        }
    },
    MOAIBox2DWeldJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D weld joint.",
        childs = {}
    },
    MOAIBox2DWheelJoint = {
        type = "class",
        inherits = "MOAIBox2DJoint",
        description = "Box2D wheel joint.",
        childs = {
            getJointSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– number jointSpeed: in units / s, converted from m/s",
                args = "MOAIBox2DWheelJoint self",
                returns = "number jointSpeed",
                valuetype = "number"
            },
            getJointTranslation = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– number jointTranslation: in units, converted from meters",
                args = "MOAIBox2DWheelJoint self",
                returns = "number jointTranslation",
                valuetype = "number"
            },
            getMaxMotorTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– number maxMotorTorque: in (kg * units / s^2) * units, converted from N-m.",
                args = "MOAIBox2DWheelJoint self",
                returns = "number maxMotorTorque",
                valuetype = "number"
            },
            getMotorSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– number motorSpeed: in degrees/s, converted from radians/s",
                args = "MOAIBox2DWheelJoint self",
                returns = "number motorSpeed",
                valuetype = "number"
            },
            getMotorTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– number torque: in (kg * units / s^2) * units, converted from N-m.",
                args = "MOAIBox2DWheelJoint self",
                returns = "number torque",
                valuetype = "number"
            },
            getSpringDampingRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– number dampingRatio",
                args = "MOAIBox2DWheelJoint self",
                returns = "number dampingRatio",
                valuetype = "number"
            },
            getSpringFrequencyHz = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– number springFrequency: in Hz",
                args = "MOAIBox2DWheelJoint self",
                returns = "number springFrequency",
                valuetype = "number"
            },
            isMotorEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n<– boolean motorEnabled",
                args = "MOAIBox2DWheelJoint self",
                returns = "boolean motorEnabled",
                valuetype = "boolean"
            },
            setMaxMotorTorque = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n[–> number maxMotorTorque: in (kg * units / s^2) * units, converted to N-m. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DWheelJoint self, [number maxMotorTorque])",
                returns = "nil"
            },
            setMotor = {
                type = "method",
                description = "See Box2D documentation. If speed is determined to be zero, the motor is disabled, unless forceEnable is set.\n\n–> MOAIBox2DWheelJoint self\n[–> number speed: in degrees/s, converted to radians/s. Default value is 0.]\n[–> number maxMotorTorque: in (kg * units / s^2) * units, converted from N-m. Default value is 0.]\n[–> boolean forceEnable: Default value is false.]\n<– nil",
                args = "(MOAIBox2DWheelJoint self, [number speed, [number maxMotorTorque, [boolean forceEnable]]])",
                returns = "nil"
            },
            setMotorEnabled = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n[–> boolean enabled: Default value is 'true']\n<– nil",
                args = "(MOAIBox2DWheelJoint self, [boolean enabled])",
                returns = "nil"
            },
            setMotorSpeed = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n[–> number motorSpeed: in degrees/s, converted to radians/s. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DWheelJoint self, [number motorSpeed])",
                returns = "nil"
            },
            setSpringDampingRatio = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n[–> number dampingRatio: Default value is 0.]\n<– nil",
                args = "(MOAIBox2DWheelJoint self, [number dampingRatio])",
                returns = "nil"
            },
            setSpringFrequencyHz = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWheelJoint self\n[–> number springFrequencyHz: in Hz. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DWheelJoint self, [number springFrequencyHz])",
                returns = "nil"
            }
        }
    },
    MOAIBox2DWorld = {
        type = "class",
        inherits = "MOAIAction b2DestructionListener",
        description = "Box2D world.",
        childs = {
            DEBUG_DRAW_BOUNDS = {
                type = "value"
            },
            DEBUG_DRAW_CENTERS = {
                type = "value"
            },
            DEBUG_DRAW_DEFAULT = {
                type = "value"
            },
            DEBUG_DRAW_JOINTS = {
                type = "value"
            },
            DEBUG_DRAW_PAIRS = {
                type = "value"
            },
            DEBUG_DRAW_SHAPES = {
                type = "value"
            },
            addBody = {
                type = "method",
                description = "Create and add a body to the world.\n\n–> MOAIBox2DWorld self\n–> number type: One of MOAIBox2DBody.DYNAMIC, MOAIBox2DBody.KINEMATIC, MOAIBox2DBody.STATIC\n[–> number x: in units, in world coordinates, converted to meters]\n[–> number y: in units, in world coordinates, converted to meters]\n<– MOAIBox2DBody joint",
                args = "(MOAIBox2DWorld self, number type, [number x, [number y]])",
                returns = "MOAIBox2DBody joint",
                valuetype = "MOAIBox2DBody"
            },
            addDistanceJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number anchorA_X: in units, in world coordinates, converted to meters\n–> number anchorA_Y: in units, in world coordinates, converted to meters\n–> number anchorB_X: in units, in world coordinates, converted to meters\n–> number anchorB_Y: in units, in world coordinates, converted to meters\n[–> number frequencyHz: in Hz. Default value determined by Box2D]\n[–> number dampingRatio: Default value determined by Box2D]\n[–> boolean collideConnected: Default value is false]\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number anchorA_X, number anchorA_Y, number anchorB_X, number anchorB_Y, [number frequencyHz, [number dampingRatio, [boolean collideConnected]]])",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addFrictionJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number anchorX: in units, in world coordinates, converted to meters\n–> number anchorY: in units, in world coordinates, converted to meters\n[–> number maxForce: in kg * units / s^2, converted to N [kg * m / s^2]. Default value determined by Box2D]\n[–> number maxTorque: in kg * units / s^2 * units, converted to N-m [kg * m / s^2 * m]. Default value determined by Box2D]\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number anchorX, number anchorY, [number maxForce, [number maxTorque]])",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addGearJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DJoint jointA\n–> MOAIBox2DJoint jointB\n–> number ratio\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DJoint jointA, MOAIBox2DJoint jointB, number ratio)",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addMouseJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number targetX: in units, in world coordinates, converted to meters\n–> number targetY: in units, in world coordinates, converted to meters\n–> number maxForce: in kg * units / s^2, converted to N [kg * m / s^2].\n[–> number frequencyHz: in Hz. Default value determined by Box2D]\n[–> number dampingRatio: Default value determined by Box2D]\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number targetX, number targetY, number maxForce, [number frequencyHz, [number dampingRatio]])",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addPrismaticJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number anchorA: in units, in world coordinates, converted to meters\n–> number anchorB: in units, in world coordinates, converted to meters\n–> number axisA: translation axis vector X component (no units)\n–> number axisB: translation axis vector Y component (no units)\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number anchorA, number anchorB, number axisA, number axisB)",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addPulleyJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number groundAnchorA_X: in units, in world coordinates, converted to meters\n–> number groundAnchorA_Y: in units, in world coordinates, converted to meters\n–> number groundAnchorB_X: in units, in world coordinates, converted to meters\n–> number groundAnchorB_Y: in units, in world coordinates, converted to meters\n–> number anchorA_X: in units, in world coordinates, converted to meters\n–> number anchorA_Y: in units, in world coordinates, converted to meters\n–> number anchorB_X: in units, in world coordinates, converted to meters\n–> number anchorB_Y: in units, in world coordinates, converted to meters\n–> number ratio\n–> number maxLengthA: in units, converted to meters\n–> number maxLengthB: in units, converted to meters\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number groundAnchorA_X, number groundAnchorA_Y, number groundAnchorB_X, number groundAnchorB_Y, number anchorA_X, number anchorA_Y, number anchorB_X, number anchorB_Y, number ratio, number maxLengthA, number maxLengthB)",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addRevoluteJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number anchorX: in units, in world coordinates, converted to meters\n–> number anchorY: in units, in world coordinates, converted to meters\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number anchorX, number anchorY)",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addRopeJoint = {
                type = "method",
                description = "Create and add a rope joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number maxLength: in units, converted to meters\n[–> number anchorAX: in units, in world coordinates, converted to meters]\n[–> number anchorAY: in units, in world coordinates, converted to meters]\n[–> number anchorBX: in units, in world coordinates, converted to meters]\n[–> number anchorBY: in units, in world coordinates, converted to meters]\n[–> boolean collideConnected: Default value is false]\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number maxLength, [number anchorAX, [number anchorAY, [number anchorBX, [number anchorBY, [boolean collideConnected]]]]])",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addWeldJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number anchorX: in units, in world coordinates, converted to meters\n–> number anchorY: in units, in world coordinates, converted to meters\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number anchorX, number anchorY)",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            addWheelJoint = {
                type = "method",
                description = "Create and add a joint to the world. See Box2D documentation.\n\n–> MOAIBox2DWorld self\n–> MOAIBox2DBody bodyA\n–> MOAIBox2DBody bodyB\n–> number anchorX: in units, in world coordinates, converted to meters\n–> number anchorY: in units, in world coordinates, converted to meters\n–> number axisX: translation axis vector X component (no units)\n–> number axisY: translation axis vector Y component (no units)\n<– MOAIBox2DJoint joint",
                args = "(MOAIBox2DWorld self, MOAIBox2DBody bodyA, MOAIBox2DBody bodyB, number anchorX, number anchorY, number axisX, number axisY)",
                returns = "MOAIBox2DJoint joint",
                valuetype = "MOAIBox2DJoint"
            },
            getAngularSleepTolerance = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n<– number angularSleepTolerance: in degrees/s, converted from radians/s",
                args = "MOAIBox2DWorld self",
                returns = "number angularSleepTolerance",
                valuetype = "number"
            },
            getAutoClearForces = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n<– boolean autoClearForces",
                args = "MOAIBox2DWorld self",
                returns = "boolean autoClearForces",
                valuetype = "boolean"
            },
            getGravity = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n<– number gravityX: in units/s^2, converted from m/s^2\n<– number gravityY: in units/s^2, converted from m/s^2",
                args = "MOAIBox2DWorld self",
                returns = "(number gravityX, number gravityY)",
                valuetype = "number"
            },
            getLinearSleepTolerance = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n<– number linearSleepTolerance: in units/s, converted from m/s",
                args = "MOAIBox2DWorld self",
                returns = "number linearSleepTolerance",
                valuetype = "number"
            },
            getRayCast = {
                type = "method",
                description = "return RayCast 1st point hit\n\n–> MOAIBox2DWorld self\n–> number p1x\n–> number p1y\n–> number p2x\n–> number p2y\n<– boolean hit: true if hit, false otherwise\n<– MOAIBox2DFixture fixture: the fixture that was hit, or nil\n<– number hitpointX\n<– number hitpointY",
                args = "(MOAIBox2DWorld self, number p1x, number p1y, number p2x, number p2y)",
                returns = "(boolean hit, MOAIBox2DFixture fixture, number hitpointX, number hitpointY)",
                valuetype = "boolean"
            },
            getTimeToSleep = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n<– number timeToSleep",
                args = "MOAIBox2DWorld self",
                returns = "number timeToSleep",
                valuetype = "number"
            },
            setAngularSleepTolerance = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n[–> number angularSleepTolerance: in degrees/s, converted to radians/s. Default value is 0.0f.]\n<– nil",
                args = "(MOAIBox2DWorld self, [number angularSleepTolerance])",
                returns = "nil"
            },
            setAutoClearForces = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n[–> boolean autoClearForces: Default value is 'true']\n<– nil",
                args = "(MOAIBox2DWorld self, [boolean autoClearForces])",
                returns = "nil"
            },
            setDebugDrawEnabled = {
                type = "method",
                description = "enable/disable debug drawing.\n\n–> MOAIBox2DWorld self\n–> boolean enable\n<– nil",
                args = "(MOAIBox2DWorld self, boolean enable)",
                returns = "nil"
            },
            setDebugDrawFlags = {
                type = "method",
                description = "Sets mask for debug drawing.\n\n–> MOAIBox2DWorld self\n[–> number flags: One of MOAIBox2DWorld.DEBUG_DRAW_SHAPES, MOAIBox2DWorld.DEBUG_DRAW_JOINTS, MOAIBox2DWorld.DEBUG_DRAW_BOUNDS, MOAIBox2DWorld.DEBUG_DRAW_PAIRS, MOAIBox2DWorld.DEBUG_DRAW_CENTERS. Default value is MOAIBox2DWorld.DEBUG_DRAW_DEFAULT.]\n<– nil",
                args = "(MOAIBox2DWorld self, [number flags])",
                returns = "nil"
            },
            setGravity = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n[–> number gravityX: in units/s^2, converted to m/s^2. Default value is 0.]\n[–> number gravityY: in units/s^2, converted to m/s^2. Default value is 0.]\n<– nil",
                args = "(MOAIBox2DWorld self, [number gravityX, [number gravityY]])",
                returns = "nil"
            },
            setIterations = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n[–> number velocityIteratons: Default value is current value of velocity iterations.]\n[–> number positionIterations: Default value is current value of positions iterations.]\n<– nil",
                args = "(MOAIBox2DWorld self, [number velocityIteratons, [number positionIterations]])",
                returns = "nil"
            },
            setLinearSleepTolerance = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n[–> number linearSleepTolerance: in units/s, converted to m/s. Default value is 0.0f.]\n<– nil",
                args = "(MOAIBox2DWorld self, [number linearSleepTolerance])",
                returns = "nil"
            },
            setTimeToSleep = {
                type = "method",
                description = "See Box2D documentation.\n\n–> MOAIBox2DWorld self\n[–> number timeToSleep: Default value is 0.0f.]\n<– nil",
                args = "(MOAIBox2DWorld self, [number timeToSleep])",
                returns = "nil"
            },
            setUnitsToMeters = {
                type = "method",
                description = "Sets a scale factor for converting game world units to Box2D meters.\n\n–> MOAIBox2DWorld self\n[–> number unitsToMeters: Default value is 1.]\n<– nil",
                args = "(MOAIBox2DWorld self, [number unitsToMeters])",
                returns = "nil"
            }
        }
    },
    MOAIBrowserAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for access to the native web browser. Exposed to Lua via MOAIBrowser on all mobile platforms.",
        childs = {
            canOpenURL = {
                type = "function",
                description = "Return true if the device has an app installed that can open the URL.\n\n–> string url: The URL to check.\n<– boolean",
                args = "string url",
                returns = "boolean",
                valuetype = "boolean"
            },
            openURL = {
                type = "function",
                description = "Open the given URL in the device browser.\n\n–> string url: The URL to open.\n<– nil",
                args = "string url",
                returns = "nil"
            },
            openURLWithParams = {
                type = "function",
                description = "Open the native device web browser at the specified URL with the specified list of query string parameters.\n\n–> string url\n–> table params\n<– nil",
                args = "(string url, table params)",
                returns = "nil"
            }
        }
    },
    MOAIBrowserIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for access to the native web browser. Exposed to Lua via MOAIBrowser on all mobile platforms.",
        childs = {}
    },
    MOAIButtonSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Button sensor.",
        childs = {
            down = {
                type = "method",
                description = "Checks to see if the button was pressed during the last iteration.\n\n–> MOAIButtonSensor self\n<– boolean wasPressed",
                args = "MOAIButtonSensor self",
                returns = "boolean wasPressed",
                valuetype = "boolean"
            },
            isDown = {
                type = "method",
                description = "Checks to see if the button is currently down.\n\n–> MOAIButtonSensor self\n<– boolean isDown",
                args = "MOAIButtonSensor self",
                returns = "boolean isDown",
                valuetype = "boolean"
            },
            isUp = {
                type = "method",
                description = "Checks to see if the button is currently up.\n\n–> MOAIButtonSensor self\n<– boolean isUp",
                args = "MOAIButtonSensor self",
                returns = "boolean isUp",
                valuetype = "boolean"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when button events occur.\n\n–> MOAIButtonSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAIButtonSensor self, [function callback])",
                returns = "nil"
            },
            up = {
                type = "method",
                description = "Checks to see if the button was released during the last iteration.\n\n–> MOAIButtonSensor self\n<– boolean wasReleased",
                args = "MOAIButtonSensor self",
                returns = "boolean wasReleased",
                valuetype = "boolean"
            }
        }
    },
    MOAICamera = {
        type = "class",
        inherits = "MOAITransform",
        description = "Perspective or orthographic camera.",
        childs = {
            getFarPlane = {
                type = "method",
                description = "Returns the camera's far plane.\n\n–> MOAICamera self\n<– number far",
                args = "MOAICamera self",
                returns = "number far",
                valuetype = "number"
            },
            getFieldOfView = {
                type = "method",
                description = "Returns the camera's horizontal field of view.\n\n–> MOAICamera self\n<– number hfov",
                args = "MOAICamera self",
                returns = "number hfov",
                valuetype = "number"
            },
            getFocalLength = {
                type = "method",
                description = "Returns the camera's focal length given the width of the view plane.\n\n–> MOAICamera self\n–> number width\n<– number length",
                args = "(MOAICamera self, number width)",
                returns = "number length",
                valuetype = "number"
            },
            getNearPlane = {
                type = "method",
                description = "Returns the camera's near plane.\n\n–> MOAICamera self\n<– number near",
                args = "MOAICamera self",
                returns = "number near",
                valuetype = "number"
            },
            setFarPlane = {
                type = "method",
                description = "Sets the camera's far plane distance.\n\n–> MOAICamera self\n[–> number far: Default value is 10000.]\n<– nil",
                args = "(MOAICamera self, [number far])",
                returns = "nil"
            },
            setFieldOfView = {
                type = "method",
                description = "Sets the camera's horizontal field of view.\n\n–> MOAICamera self\n[–> number hfow: Default value is 60.]\n<– nil",
                args = "(MOAICamera self, [number hfow])",
                returns = "nil"
            },
            setNearPlane = {
                type = "method",
                description = "Sets the camera's near plane distance.\n\n–> MOAICamera self\n[–> number near: Default value is 1.]\n<– nil",
                args = "(MOAICamera self, [number near])",
                returns = "nil"
            },
            setOrtho = {
                type = "method",
                description = "Sets orthographic mode.\n\n–> MOAICamera self\n[–> boolean ortho: Default value is true.]\n<– nil",
                args = "(MOAICamera self, [boolean ortho])",
                returns = "nil"
            }
        }
    },
    MOAICamera2D = {
        type = "class",
        inherits = "MOAITransform2D",
        description = "2D camera.",
        childs = {
            getFarPlane = {
                type = "method",
                description = "Returns the camera's far plane.\n\n–> MOAICamera2D self\n<– number far",
                args = "MOAICamera2D self",
                returns = "number far",
                valuetype = "number"
            },
            getNearPlane = {
                type = "method",
                description = "Returns the camera's near plane.\n\n–> MOAICamera2D self\n<– number near",
                args = "MOAICamera2D self",
                returns = "number near",
                valuetype = "number"
            },
            setFarPlane = {
                type = "method",
                description = "Sets the camera's far plane distance.\n\n–> MOAICamera2D self\n[–> number far: Default value is -1.]\n<– nil",
                args = "(MOAICamera2D self, [number far])",
                returns = "nil"
            },
            setNearPlane = {
                type = "method",
                description = "Sets the camera's near plane distance.\n\n–> MOAICamera2D self\n[–> number near: Default value is 1.]\n<– nil",
                args = "(MOAICamera2D self, [number near])",
                returns = "nil"
            }
        }
    },
    MOAICameraAnchor2D = {
        type = "class",
        inherits = "MOAINode",
        description = "Attaches fitting information to a transform. Used by MOAICameraFitter2D.",
        childs = {
            setParent = {
                type = "method",
                description = "Attach the anchor to a transform.\n\n–> MOAICameraAnchor2D self\n[–> MOAINode parent]\n<– nil",
                args = "(MOAICameraAnchor2D self, [MOAINode parent])",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set the dimensions (in world units) of the anchor.\n\n–> MOAICameraAnchor2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAICameraAnchor2D self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            }
        }
    },
    MOAICameraFitter2D = {
        type = "class",
        inherits = "MOAIAction MOAINode",
        description = "Action to dynamically fit a camera transform to a set of targets given a viewport and world space constraints.",
        childs = {
            FITTING_MODE_APPLY_ANCHORS = {
                type = "value"
            },
            FITTING_MODE_APPLY_BOUNDS = {
                type = "value"
            },
            FITTING_MODE_DEFAULT = {
                type = "value"
            },
            FITTING_MODE_MASK = {
                type = "value"
            },
            FITTING_MODE_SEEK_LOC = {
                type = "value"
            },
            FITTING_MODE_SEEK_SCALE = {
                type = "value"
            },
            clearAnchors = {
                type = "method",
                description = "Remove all camera anchors from the fitter.\n\n–> MOAICameraFitter2D self\n<– nil",
                args = "MOAICameraFitter2D self",
                returns = "nil"
            },
            clearFitMode = {
                type = "method",
                description = "Clears bits in the fitting mask.\n\n–> MOAICameraFitter2D self\n[–> number mask: Default value is FITTING_MODE_MASK]\n<– nil",
                args = "(MOAICameraFitter2D self, [number mask])",
                returns = "nil"
            },
            getFitDistance = {
                type = "method",
                description = "Returns the distance between the camera's current x, y, scale and the target x, y, scale. As the camera approaches its target, the distance approaches 0. Check the value returned by this function against a small epsilon value.\n\n–> MOAICameraFitter2D self\n<– number distance",
                args = "MOAICameraFitter2D self",
                returns = "number distance",
                valuetype = "number"
            },
            getFitLoc = {
                type = "method",
                description = "Get the fitter location.\n\n–> MOAICameraFitter2D self\n<– number x\n<– number y",
                args = "MOAICameraFitter2D self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getFitMode = {
                type = "method",
                description = "Gets bits in the fitting mask.\n\n–> MOAICameraFitter2D self\n<– number mask",
                args = "MOAICameraFitter2D self",
                returns = "number mask",
                valuetype = "number"
            },
            getFitScale = {
                type = "method",
                description = "Returns the fit scale\n\n–> MOAICameraFitter2D self\n<– number scale",
                args = "MOAICameraFitter2D self",
                returns = "number scale",
                valuetype = "number"
            },
            getTargetLoc = {
                type = "method",
                description = "Get the target location.\n\n–> MOAICameraFitter2D self\n<– number x\n<– number y",
                args = "MOAICameraFitter2D self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getTargetScale = {
                type = "method",
                description = "Returns the target scale\n\n–> MOAICameraFitter2D self\n<– number scale",
                args = "MOAICameraFitter2D self",
                returns = "number scale",
                valuetype = "number"
            },
            insertAnchor = {
                type = "method",
                description = "Add an anchor to the fitter.\n\n–> MOAICameraFitter2D self\n–> MOAICameraAnchor2D anchor\n<– nil",
                args = "(MOAICameraFitter2D self, MOAICameraAnchor2D anchor)",
                returns = "nil"
            },
            removeAnchor = {
                type = "method",
                description = "Remove an anchor from the fitter.\n\n–> MOAICameraFitter2D self\n–> MOAICameraAnchor2D anchor\n<– nil",
                args = "(MOAICameraFitter2D self, MOAICameraAnchor2D anchor)",
                returns = "nil"
            },
            setBounds = {
                type = "method",
                description = "Sets or clears the world bounds of the fitter. The camera will not move outside of the fitter's bounds.\n\nOverload:\n–> MOAICameraFitter2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil\n\nOverload:\n–> MOAICameraFitter2D self\n<– nil",
                args = "(MOAICameraFitter2D self, [number xMin, number yMin, number xMax, number yMax])",
                returns = "nil"
            },
            setCamera = {
                type = "method",
                description = "Set a MOAITransform for the fitter to use as a camera. The fitter will dynamically change the location and scale of the camera to keep all of the anchors on the screen.\n\n–> MOAICameraFitter2D self\n[–> MOAITransform camera: Default value is nil.]\n<– nil",
                args = "(MOAICameraFitter2D self, [MOAITransform camera])",
                returns = "nil"
            },
            setDamper = {
                type = "method",
                description = "Sets the fitter's damper coefficient. This is a scalar applied to the difference between the camera transform's location and the fitter's target location every frame. The smaller the coefficient, the tighter the fit will be. A value of '0' will not dampen at all; a value of '1' will never move the camera.\n\n–> MOAICameraFitter2D self\n[–> number damper: Default value is 0.]\n<– nil",
                args = "(MOAICameraFitter2D self, [number damper])",
                returns = "nil"
            },
            setFitLoc = {
                type = "method",
                description = "Set the fitter's location.\n\n–> MOAICameraFitter2D self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> boolean snap: Default value is false.]\n<– nil",
                args = "(MOAICameraFitter2D self, [number x, [number y, [boolean snap]]])",
                returns = "nil"
            },
            setFitMode = {
                type = "method",
                description = "Sets bits in the fitting mask.\n\n–> MOAICameraFitter2D self\n[–> number mask: Default value is FITTING_MODE_DEFAULT]\n<– nil",
                args = "(MOAICameraFitter2D self, [number mask])",
                returns = "nil"
            },
            setFitScale = {
                type = "method",
                description = "Set the fitter's scale.\n\n–> MOAICameraFitter2D self\n[–> number scale: Default value is 1.]\n[–> boolean snap: Default value is false.]\n<– nil",
                args = "(MOAICameraFitter2D self, [number scale, [boolean snap]])",
                returns = "nil"
            },
            setMin = {
                type = "method",
                description = "Set the minimum number of world units to be displayed by the camera along either axis.\n\n–> MOAICameraFitter2D self\n[–> number min: Default value is 0.]\n<– nil",
                args = "(MOAICameraFitter2D self, [number min])",
                returns = "nil"
            },
            setViewport = {
                type = "method",
                description = "Set the viewport to be used for fitting.\n\n–> MOAICameraFitter2D self\n[–> MOAIViewport viewport: Default value is nil.]\n<– nil",
                args = "(MOAICameraFitter2D self, [MOAIViewport viewport])",
                returns = "nil"
            },
            snapToTarget = {
                type = "method",
                description = "Snap the camera to the target fitting.\n\nOverload:\n–> MOAICameraFitter2D self\n<– nil\n\nOverload:\n–> MOAICameraFitter2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAICameraFitter2D self, [MOAITransform transform])",
                returns = "nil"
            }
        }
    },
    MOAIChartBoostAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {
            hasCachedInterstitial = {
                type = "function",
                description = "Determine whether or not a cached interstitial is available.\n\n<– boolean hasCached",
                args = "()",
                returns = "boolean hasCached",
                valuetype = "boolean"
            },
            init = {
                type = "function",
                description = "Initialize ChartBoost.\n\n–> string appId: Available in ChartBoost dashboard settings.\n–> string appSignature: Available in ChartBoost dashboard settings.\n<– nil",
                args = "(string appId, string appSignature)",
                returns = "nil"
            },
            loadInterstitial = {
                type = "function",
                description = "Request that an interstitial ad be cached for later display.\n\n[–> string locationId: Optional location ID.]\n<– nil",
                args = "[string locationId]",
                returns = "nil"
            },
            showInterstitial = {
                type = "function",
                description = "Request an interstitial ad display if a cached ad is available.\n\n[–> string locationId: Optional location ID.]\n<– nil",
                args = "[string locationId]",
                returns = "nil"
            }
        }
    },
    MOAIClearableView = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {
            setClearColor = {
                type = "method",
                description = "At the start of each frame the device will by default automatically render a background color. Using this function you can set the background color that is drawn each frame. If you specify no arguments to this function, then automatic redraw of the background color will be turned off (i.e. the previous render will be used as the background).\n\nOverload:\n–> MOAIClearableView self\n[–> number red: The red value of the color.]\n[–> number green: The green value of the color.]\n[–> number blue: The blue value of the color.]\n[–> number alpha: The alpha value of the color.]\n<– nil\n\nOverload:\n–> MOAIClearableView self\n–> MOAIColor color\n<– nil",
                args = "(MOAIClearableView self, [(number red, [number green, [number blue, [number alpha]]]) | MOAIColor color])",
                returns = "nil"
            },
            setClearDepth = {
                type = "method",
                description = "At the start of each frame the buffer will by default automatically clear the depth buffer. This function sets whether or not the depth buffer should be cleared at the start of each frame.\n\n–> MOAIClearableView self\n–> boolean clearDepth: Whether to clear the depth buffer each frame.\n<– nil",
                args = "(MOAIClearableView self, boolean clearDepth)",
                returns = "nil"
            }
        }
    },
    MOAIColor = {
        type = "class",
        inherits = "MOAINode ZLColorVec",
        description = "Color vector with animation helper methods.",
        childs = {
            ATTR_A_COL = {
                type = "value"
            },
            ATTR_B_COL = {
                type = "value"
            },
            ATTR_G_COL = {
                type = "value"
            },
            ATTR_R_COL = {
                type = "value"
            },
            COLOR_TRAIT = {
                type = "value"
            },
            INHERIT_COLOR = {
                type = "value"
            },
            moveColor = {
                type = "method",
                description = "Animate the color by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAIColor self\n–> number rDelta: Delta to be added to r.\n–> number gDelta: Delta to be added to g.\n–> number bDelta: Delta to be added to b.\n–> number aDelta: Delta to be added to a.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAIColor self, number rDelta, number gDelta, number bDelta, number aDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekColor = {
                type = "method",
                description = "Animate the color by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAIColor self\n–> number rGoal: Desired resulting value for r.\n–> number gGoal: Desired resulting value for g.\n–> number bGoal: Desired resulting value for b.\n–> number aGoal: Desired resulting value for a.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAIColor self, number rGoal, number gGoal, number bGoal, number aGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            setColor = {
                type = "method",
                description = "Initialize the color.\n\n–> MOAIColor self\n–> number r: Default value is 0.\n–> number g: Default value is 0.\n–> number b: Default value is 0.\n[–> number a: Default value is 1.]\n<– nil",
                args = "(MOAIColor self, number r, number g, number b, [number a])",
                returns = "nil"
            },
            setParent = {
                type = "method",
                description = "This method has been deprecated. Use MOAINode setAttrLink instead.\n\n–> MOAIColor self\n[–> MOAINode parent: Default value is nil.]\n<– nil",
                args = "(MOAIColor self, [MOAINode parent])",
                returns = "nil"
            }
        }
    },
    MOAICompassSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Device heading sensor.",
        childs = {
            getHeading = {
                type = "method",
                description = "Returns the current heading according to the built-in compass.\n\n–> MOAICompassSensor self\n<– number heading",
                args = "MOAICompassSensor self",
                returns = "number heading",
                valuetype = "number"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when the heading changes.\n\n–> MOAICompassSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAICompassSensor self, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAICoroutine = {
        type = "class",
        inherits = "MOAIAction",
        description = "Binds a Lua coroutine to a MOAIAction.",
        childs = {
            blockOnAction = {
                type = "function",
                description = "Skip updating current thread until the specified action is no longer busy. A little more efficient than spinlocking from Lua.\n\n–> MOAIAction blocker\n<– nil",
                args = "MOAIAction blocker",
                returns = "nil"
            },
            currentThread = {
                type = "function",
                description = "Returns the currently running thread (if any).\n\n<– MOAICoroutine currentThread: Current thread or nil.",
                args = "()",
                returns = "MOAICoroutine currentThread",
                valuetype = "MOAICoroutine"
            },
            run = {
                type = "method",
                description = "Starts a thread with a function and passes parameters to it.\n\n–> MOAICoroutine self\n–> function threadFunc\n–> ... parameters\n<– nil",
                args = "(MOAICoroutine self, function threadFunc, ... parameters)",
                returns = "nil"
            }
        }
    },
    MOAICp = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Singleton for Chipmunk global configuration.",
        childs = {
            getBiasCoefficient = {
                type = "function",
                description = "Returns the current bias coefficient.\n\n<– number bias: The bias coefficient.",
                args = "()",
                returns = "number bias",
                valuetype = "number"
            },
            getCollisionSlop = {
                type = "function",
                description = "Returns the current collision slop.\n\n<– number slop",
                args = "()",
                returns = "number slop",
                valuetype = "number"
            },
            getContactPersistence = {
                type = "function",
                description = "Returns the current contact persistence.\n\n<– number persistence",
                args = "()",
                returns = "number persistence",
                valuetype = "number"
            },
            setBiasCoefficient = {
                type = "function",
                description = "Sets the bias coefficient.\n\n–> number bias\n<– nil",
                args = "number bias",
                returns = "nil"
            },
            setCollisionSlop = {
                type = "function",
                description = "Sets the collision slop.\n\n–> number slop\n<– nil",
                args = "number slop",
                returns = "nil"
            },
            setContactPersistence = {
                type = "function",
                description = "Sets the contact persistance.\n\n–> number persistance\n<– nil",
                args = "number persistance",
                returns = "nil"
            }
        }
    },
    MOAICpArbiter = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Chipmunk Arbiter.",
        childs = {
            countContacts = {
                type = "method",
                description = "Returns the number of contacts occurring with this arbiter.\n\n–> MOAICpArbiter self\n<– number count: The number of contacts occurring.",
                args = "MOAICpArbiter self",
                returns = "number count",
                valuetype = "number"
            },
            getContactDepth = {
                type = "method",
                description = "Returns the depth of a contact point between two objects.\n\n–> MOAICpArbiter self\n–> number id: The ID of the contact.\n<– number depth: The depth of the contact in pixels (i.e. how far it overlaps).",
                args = "(MOAICpArbiter self, number id)",
                returns = "number depth",
                valuetype = "number"
            },
            getContactNormal = {
                type = "method",
                description = "Returns the normal of a contact point between two objects.\n\n–> MOAICpArbiter self\n–> number id: The ID of the contact.\n<– boolean x: The X component of the normal vector.\n<– boolean y: The Y component of the normal vector.",
                args = "(MOAICpArbiter self, number id)",
                returns = "(boolean x, boolean y)",
                valuetype = "boolean"
            },
            getContactPoint = {
                type = "method",
                description = "Returns the position of a contact point between two objects.\n\n–> MOAICpArbiter self\n–> number id: The ID of the contact.\n<– boolean x: The X component of the position vector.\n<– boolean y: The Y component of the position vector.",
                args = "(MOAICpArbiter self, number id)",
                returns = "(boolean x, boolean y)",
                valuetype = "boolean"
            },
            getTotalImpulse = {
                type = "method",
                description = "Returns the total impulse of a contact point between two objects.\n\n–> MOAICpArbiter self\n<– boolean x: The X component of the force involved in the contact.\n<– boolean y: The Y component of the force involved in the contact.",
                args = "MOAICpArbiter self",
                returns = "(boolean x, boolean y)",
                valuetype = "boolean"
            },
            getTotalImpulseWithFriction = {
                type = "method",
                description = "Returns the total impulse of a contact point between two objects, also including frictional forces.\n\n–> MOAICpArbiter self\n<– boolean x: The X component of the force involved in the contact.\n<– boolean y: The Y component of the force involved in the contact.",
                args = "MOAICpArbiter self",
                returns = "(boolean x, boolean y)",
                valuetype = "boolean"
            },
            isFirstContact = {
                type = "method",
                description = "Returns whether this is the first time that these two objects have contacted.\n\n–> MOAICpArbiter self\n<– boolean first: Whether this is the first instance of a collision.",
                args = "MOAICpArbiter self",
                returns = "boolean first",
                valuetype = "boolean"
            }
        }
    },
    MOAICpBody = {
        type = "class",
        inherits = "MOAITransformBase MOAICpPrim",
        description = "Chipmunk Body.",
        childs = {
            NONE = {
                type = "value"
            },
            REMOVE_BODY = {
                type = "value"
            },
            REMOVE_BODY_AND_SHAPES = {
                type = "value"
            },
            activate = {
                type = "method",
                description = "Activates a body after it has been put to sleep (physics will now be processed for this body again).\n\n–> MOAICpBody self\n<– nil",
                args = "MOAICpBody self",
                returns = "nil"
            },
            addCircle = {
                type = "method",
                description = "Adds a circle to the body.\n\n–> MOAICpBody self\n–> number radius\n–> number x\n–> number y\n<– MOAICpShape circle",
                args = "(MOAICpBody self, number radius, number x, number y)",
                returns = "MOAICpShape circle",
                valuetype = "MOAICpShape"
            },
            addPolygon = {
                type = "method",
                description = "Adds a polygon to the body.\n\n–> MOAICpBody self\n–> table polygon\n<– MOAICpShape polygon",
                args = "(MOAICpBody self, table polygon)",
                returns = "MOAICpShape polygon",
                valuetype = "MOAICpShape"
            },
            addRect = {
                type = "method",
                description = "Adds a rectangle to the body.\n\n–> MOAICpBody self\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n<– MOAICpShape rectangle",
                args = "(MOAICpBody self, number x1, number y1, number x2, number y2)",
                returns = "MOAICpShape rectangle",
                valuetype = "MOAICpShape"
            },
            addSegment = {
                type = "method",
                description = "Adds a segment to the body.\n\n–> MOAICpBody self\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n[–> number radius]\n<– MOAICpShape segment",
                args = "(MOAICpBody self, number x1, number y1, number x2, number y2, [number radius])",
                returns = "MOAICpShape segment",
                valuetype = "MOAICpShape"
            },
            applyForce = {
                type = "method",
                description = "Applies force to the body, taking into account any existing forces being applied.\n\n–> MOAICpBody self\n–> number fx\n–> number fy\n–> number rx\n–> number ry\n<– nil",
                args = "(MOAICpBody self, number fx, number fy, number rx, number ry)",
                returns = "nil"
            },
            applyImpulse = {
                type = "method",
                description = "Applies impulse to the body, taking into account any existing impulses being applied.\n\n–> MOAICpBody self\n–> number jx\n–> number jy\n–> number rx\n–> number ry\n<– nil",
                args = "(MOAICpBody self, number jx, number jy, number rx, number ry)",
                returns = "nil"
            },
            getAngle = {
                type = "method",
                description = "Returns the angle of the body.\n\n–> MOAICpBody self\n<– number angle: The current angle.",
                args = "MOAICpBody self",
                returns = "number angle",
                valuetype = "number"
            },
            getAngVel = {
                type = "method",
                description = "Returns the angular velocity of the body.\n\n–> MOAICpBody self\n<– number angle: The current angular velocity.",
                args = "MOAICpBody self",
                returns = "number angle",
                valuetype = "number"
            },
            getForce = {
                type = "method",
                description = "Returns the force of the body.\n\n–> MOAICpBody self\n<– number x: The X component of the current force being applied.\n<– number y: The Y component of the current force being applied.",
                args = "MOAICpBody self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getMass = {
                type = "method",
                description = "Returns the mass of the body.\n\n–> MOAICpBody self\n<– number mass: The current mass.",
                args = "MOAICpBody self",
                returns = "number mass",
                valuetype = "number"
            },
            getMoment = {
                type = "method",
                description = "Returns the moment of the body.\n\n–> MOAICpBody self\n<– number moment: The current moment.",
                args = "MOAICpBody self",
                returns = "number moment",
                valuetype = "number"
            },
            getPos = {
                type = "method",
                description = "Returns the position of the body.\n\n–> MOAICpBody self\n<– number x: The X position.\n<– number y: The Y position.",
                args = "MOAICpBody self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getRot = {
                type = "method",
                description = "Returns the rotation of the body.\n\n–> MOAICpBody self\n<– number x: The X position.\n<– number y: The Y position.",
                args = "MOAICpBody self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getTorque = {
                type = "method",
                description = "Returns the torque of the body.\n\n–> MOAICpBody self\n<– number torque: The current torque.",
                args = "MOAICpBody self",
                returns = "number torque",
                valuetype = "number"
            },
            getVel = {
                type = "method",
                description = "Returns the velocity of the body.\n\n–> MOAICpBody self\n<– number x: The X component of the current velocity.\n<– number y: The Y component of the current velocity.",
                args = "MOAICpBody self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            isRogue = {
                type = "method",
                description = "Returns whether the body is not yet currently associated with a space.\n\n–> MOAICpBody self\n<– boolean static: Whether the body is not associated with a space.",
                args = "MOAICpBody self",
                returns = "boolean static",
                valuetype = "boolean"
            },
            isSleeping = {
                type = "method",
                description = "Returns whether the body is currently sleeping.\n\n–> MOAICpBody self\n<– boolean sleeping: Whether the body is sleeping.",
                args = "MOAICpBody self",
                returns = "boolean sleeping",
                valuetype = "boolean"
            },
            isStatic = {
                type = "method",
                description = "Returns whether the body is static.\n\n–> MOAICpBody self\n<– boolean static: Whether the body static.",
                args = "MOAICpBody self",
                returns = "boolean static",
                valuetype = "boolean"
            },
            localToWorld = {
                type = "method",
                description = "Converts the relative position to an absolute position based on position of the object being (0, 0) for the relative position.\n\n–> MOAICpBody self\n–> number rx: The relative X position.\n–> number ry: The relative Y position.\n<– number ax: The absolute X position.\n<– number ay: The absolute Y position.",
                args = "(MOAICpBody self, number rx, number ry)",
                returns = "(number ax, number ay)",
                valuetype = "number"
            },
            new = {
                type = "function",
                description = "Creates a new body with the specified mass and moment.\n\n–> number m: The mass of the new body.\n–> number i: The moment of the new body.\n<– MOAICpBody body: The new body.",
                args = "(number m, number i)",
                returns = "MOAICpBody body",
                valuetype = "MOAICpBody"
            },
            newStatic = {
                type = "function",
                description = "Creates a new static body.\n\n<– MOAICpBody body: The new static body.",
                args = "()",
                returns = "MOAICpBody body",
                valuetype = "MOAICpBody"
            },
            resetForces = {
                type = "method",
                description = "Resets all forces on the body.\n\n–> MOAICpBody self\n<– nil",
                args = "MOAICpBody self",
                returns = "nil"
            },
            setAngle = {
                type = "method",
                description = "Sets the angle of the body.\n\n–> MOAICpBody self\n–> number angle: The angle of the body.\n<– nil",
                args = "(MOAICpBody self, number angle)",
                returns = "nil"
            },
            setAngVel = {
                type = "method",
                description = "Sets the angular velocity of the body.\n\n–> MOAICpBody self\n–> number angvel: The angular velocity of the body.\n<– nil",
                args = "(MOAICpBody self, number angvel)",
                returns = "nil"
            },
            setForce = {
                type = "method",
                description = "Sets the force on the body.\n\n–> MOAICpBody self\n–> number forcex: The X force being applied to the body.\n–> number forcey: The Y force being applied to the body.\n<– nil",
                args = "(MOAICpBody self, number forcex, number forcey)",
                returns = "nil"
            },
            setMass = {
                type = "method",
                description = "Sets the mass of the body.\n\n–> MOAICpBody self\n–> number mass: The mass of the body.\n<– nil",
                args = "(MOAICpBody self, number mass)",
                returns = "nil"
            },
            setMoment = {
                type = "method",
                description = "Sets the moment of the body.\n\n–> MOAICpBody self\n–> number moment: The moment of the body.\n<– nil",
                args = "(MOAICpBody self, number moment)",
                returns = "nil"
            },
            setPos = {
                type = "method",
                description = "Sets the position of the body.\n\n–> MOAICpBody self\n–> number x: The X position of the body.\n–> number y: The Y position of the body.\n<– nil",
                args = "(MOAICpBody self, number x, number y)",
                returns = "nil"
            },
            setRemoveFlag = {
                type = "method",
                description = "Sets the removal flag on the body.\n\n–> MOAICpBody self\n–> number flag: The removal flag.\n<– nil",
                args = "(MOAICpBody self, number flag)",
                returns = "nil"
            },
            setTorque = {
                type = "method",
                description = "Sets the torque of the body.\n\n–> MOAICpBody self\n–> number torque: The torque of the body.\n<– nil",
                args = "(MOAICpBody self, number torque)",
                returns = "nil"
            },
            setVel = {
                type = "method",
                description = "Sets the velocity of the body.\n\n–> MOAICpBody self\n–> number x: The horizontal velocity.\n–> number y: The vertical velocity.\n<– nil",
                args = "(MOAICpBody self, number x, number y)",
                returns = "nil"
            },
            sleep = {
                type = "method",
                description = "Puts the body to sleep (physics will no longer be processed for it until it is activated).\n\n–> MOAICpBody self\n<– nil",
                args = "MOAICpBody self",
                returns = "nil"
            },
            sleepWithGroup = {
                type = "method",
                description = "Forces an object to sleep. Pass in another sleeping body to add the object to the sleeping body's existing group.\n\n–> MOAICpBody self\n–> MOAICpBody group\n<– nil",
                args = "(MOAICpBody self, MOAICpBody group)",
                returns = "nil"
            },
            worldToLocal = {
                type = "method",
                description = "Converts the absolute position to a relative position based on position of the object being (0, 0) for the relative position.\n\n–> MOAICpBody self\n–> number ax: The absolute X position.\n–> number ay: The absolute Y position.\n<– number rx: The relative X position.\n<– number ry: The relative Y position.",
                args = "(MOAICpBody self, number ax, number ay)",
                returns = "(number rx, number ry)",
                valuetype = "number"
            }
        }
    },
    MOAICpConstraint = {
        type = "class",
        inherits = "MOAILuaObject MOAICpPrim",
        description = "Chipmunk Constraint.",
        childs = {
            getBiasCoef = {
                type = "method",
                description = "Returns the current bias coefficient.\n\n–> MOAICpConstraint self\n<– number bias: The bias coefficient.",
                args = "MOAICpConstraint self",
                returns = "number bias",
                valuetype = "number"
            },
            getMaxBias = {
                type = "method",
                description = "Returns the maximum bias coefficient.\n\n–> MOAICpConstraint self\n<– number bias: The maximum bias coefficient.",
                args = "MOAICpConstraint self",
                returns = "number bias",
                valuetype = "number"
            },
            getMaxForce = {
                type = "method",
                description = "Returns the maximum force allowed.\n\n–> MOAICpConstraint self\n<– number bias: The maximum force allowed.",
                args = "MOAICpConstraint self",
                returns = "number bias",
                valuetype = "number"
            },
            newDampedRotarySpring = {
                type = "function",
                description = "Creates a new damped rotary string between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number restAngle: The angle at which the spring is at rest.\n–> number stiffness: The stiffness of the spring.\n–> number damping: The damping applied to the spring.\n<– MOAICpConstraint spring: The new spring.",
                args = "(MOAICpBody first, MOAICpBody second, number restAngle, number stiffness, number damping)",
                returns = "MOAICpConstraint spring",
                valuetype = "MOAICpConstraint"
            },
            newDampedSpring = {
                type = "function",
                description = "Creates a new damped string between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number x1: The X position of the first anchor.\n–> number y1: The Y position of the first anchor.\n–> number x2: The X position of the second anchor.\n–> number y2: The Y position of the second anchor.\n–> number restAngle: The angle at which the spring is at rest.\n–> number stiffness: The stiffness of the spring.\n–> number damping: The damping applied to the spring.\n<– MOAICpConstraint spring: The new spring.",
                args = "(MOAICpBody first, MOAICpBody second, number x1, number y1, number x2, number y2, number restAngle, number stiffness, number damping)",
                returns = "MOAICpConstraint spring",
                valuetype = "MOAICpConstraint"
            },
            newGearJoint = {
                type = "function",
                description = "Creates a new gear joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number phase: The phase of the gear.\n–> number ratio: The gear ratio.\n<– MOAICpConstraint gear: The new gear joint.",
                args = "(MOAICpBody first, MOAICpBody second, number phase, number ratio)",
                returns = "MOAICpConstraint gear",
                valuetype = "MOAICpConstraint"
            },
            newGrooveJoint = {
                type = "function",
                description = "Creates a new groove joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number gx1\n–> number gy1\n–> number gx2\n–> number gy2\n–> number ax\n–> number ay\n<– MOAICpConstraint groove: The new groove joint.",
                args = "(MOAICpBody first, MOAICpBody second, number gx1, number gy1, number gx2, number gy2, number ax, number ay)",
                returns = "MOAICpConstraint groove",
                valuetype = "MOAICpConstraint"
            },
            newPinJoint = {
                type = "function",
                description = "Creates a new pin joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number ax1\n–> number ay1\n–> number ax2\n–> number ay2\n<– MOAICpConstraint pin: The new pin joint.",
                args = "(MOAICpBody first, MOAICpBody second, number ax1, number ay1, number ax2, number ay2)",
                returns = "MOAICpConstraint pin",
                valuetype = "MOAICpConstraint"
            },
            newPivotJoint = {
                type = "function",
                description = "Creates a new pivot joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number x\n–> number y\n[–> number ax]\n[–> number ay]\n<– MOAICpConstraint pivot: The new pivot joint.",
                args = "(MOAICpBody first, MOAICpBody second, number x, number y, [number ax, [number ay]])",
                returns = "MOAICpConstraint pivot",
                valuetype = "MOAICpConstraint"
            },
            newRatchetJoint = {
                type = "function",
                description = "Creates a new ratchet joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number phase: The phase of the gear.\n–> number ratchet: The ratchet value.\n<– MOAICpConstraint ratchet: The new pivot joint.",
                args = "(MOAICpBody first, MOAICpBody second, number phase, number ratchet)",
                returns = "MOAICpConstraint ratchet",
                valuetype = "MOAICpConstraint"
            },
            newRotaryLimitJoint = {
                type = "function",
                description = "Creates a new rotary limit joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number min: The minimum rotary value.\n–> number max: The maximum rotary value.\n<– MOAICpConstraint limit: The new rotary limit joint.",
                args = "(MOAICpBody first, MOAICpBody second, number min, number max)",
                returns = "MOAICpConstraint limit",
                valuetype = "MOAICpConstraint"
            },
            newSimpleMotor = {
                type = "function",
                description = "Creates a new simple motor joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number rate: The rotation rate of the simple motor.\n<– MOAICpConstraint motor: The new simple motor joint.",
                args = "(MOAICpBody first, MOAICpBody second, number rate)",
                returns = "MOAICpConstraint motor",
                valuetype = "MOAICpConstraint"
            },
            newSlideJoint = {
                type = "function",
                description = "Creates a new slide joint between the two specified bodies.\n\n–> MOAICpBody first\n–> MOAICpBody second\n–> number ax1\n–> number ay1\n–> number ax2\n–> number ay2\n–> number min\n–> number max\n<– MOAICpConstraint motor: The new slide joint.",
                args = "(MOAICpBody first, MOAICpBody second, number ax1, number ay1, number ax2, number ay2, number min, number max)",
                returns = "MOAICpConstraint motor",
                valuetype = "MOAICpConstraint"
            },
            setBiasCoef = {
                type = "method",
                description = "Sets the current bias coefficient.\n\n–> MOAICpConstraint self\n–> number bias: The bias coefficient.\n<– nil",
                args = "(MOAICpConstraint self, number bias)",
                returns = "nil"
            },
            setMaxBias = {
                type = "method",
                description = "Sets the maximum bias coefficient.\n\n–> MOAICpConstraint self\n–> number bias: The maximum bias coefficient.\n<– nil",
                args = "(MOAICpConstraint self, number bias)",
                returns = "nil"
            },
            setMaxForce = {
                type = "method",
                description = "Sets the maximum force allowed.\n\n–> MOAICpConstraint self\n–> number bias: The maximum force allowed.\n<– nil",
                args = "(MOAICpConstraint self, number bias)",
                returns = "nil"
            }
        }
    },
    MOAICpPrim = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAICpShape = {
        type = "class",
        inherits = "MOAILuaObject MOAICpPrim",
        description = "Chipmunk Shape.",
        childs = {
            areaForCircle = {
                type = "function",
                description = "Returns the area for a ring or circle.\n\nOverload:\n–> number radius\n<– number area\n\nOverload:\n–> number innerRadius\n–> number outerRadius\n<– number area",
                args = "(number radius | (number innerRadius, number outerRadius))",
                returns = "number area",
                valuetype = "number"
            },
            areaForPolygon = {
                type = "function",
                description = "Returns the area for a polygon.\n\n–> table vertices: Array containing vertex coordinate components ( t[1] = x0, t[2] = y0, t[3] = x1, t[4] = y1... )\n<– number area",
                args = "table vertices",
                returns = "number area",
                valuetype = "number"
            },
            areaForRect = {
                type = "function",
                description = "Returns the area for the specified rectangle.\n\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n<– number area: The calculated area.",
                args = "(number x1, number y1, number x2, number y2)",
                returns = "number area",
                valuetype = "number"
            },
            areaForSegment = {
                type = "function",
                description = "Returns the area for the specified segment.\n\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number r\n<– number area: The calculated area.",
                args = "(number x1, number y1, number x2, number y2, number r)",
                returns = "number area",
                valuetype = "number"
            },
            getBody = {
                type = "method",
                description = "Returns the current body for the shape.\n\n–> MOAICpShape self\n<– MOAICpBody body: The body.",
                args = "MOAICpShape self",
                returns = "MOAICpBody body",
                valuetype = "MOAICpBody"
            },
            getElasticity = {
                type = "method",
                description = "Returns the current elasticity.\n\n–> MOAICpShape self\n<– number elasticity: The elasticity.",
                args = "MOAICpShape self",
                returns = "number elasticity",
                valuetype = "number"
            },
            getFriction = {
                type = "method",
                description = "Returns the current friction.\n\n–> MOAICpShape self\n<– number friction: The friction.",
                args = "MOAICpShape self",
                returns = "number friction",
                valuetype = "number"
            },
            getGroup = {
                type = "method",
                description = "Returns the current group ID.\n\n–> MOAICpShape self\n<– number group: The group ID.",
                args = "MOAICpShape self",
                returns = "number group",
                valuetype = "number"
            },
            getLayers = {
                type = "method",
                description = "Returns the current layer ID.\n\n–> MOAICpShape self\n<– number layer: The layer ID.",
                args = "MOAICpShape self",
                returns = "number layer",
                valuetype = "number"
            },
            getSurfaceVel = {
                type = "method",
                description = "Returns the current surface velocity?\n\n–> MOAICpShape self\n<– number x: The X component of the surface velocity.\n<– number y: The Y component of the surface velocity.",
                args = "MOAICpShape self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getType = {
                type = "method",
                description = "Returns the current collision type.\n\n–> MOAICpShape self\n<– number type: The collision type.",
                args = "MOAICpShape self",
                returns = "number type",
                valuetype = "number"
            },
            inside = {
                type = "method",
                description = "Returns whether the specified point is inside the shape.\n\n–> MOAICpShape self\n–> number x\n–> number y\n<– boolean inside: Whether the point is inside the shape.",
                args = "(MOAICpShape self, number x, number y)",
                returns = "boolean inside",
                valuetype = "boolean"
            },
            isSensor = {
                type = "method",
                description = "Returns whether the current shape is a sensor.\n\n–> MOAICpShape self\n<– boolean sensor: Whether the shape is a sensor.",
                args = "MOAICpShape self",
                returns = "boolean sensor",
                valuetype = "boolean"
            },
            momentForCircle = {
                type = "function",
                description = "Return the moment of inertia for the circle.\n\n–> number m\n–> number r1\n–> number r2\n–> number ox\n–> number oy\n<– number moment",
                args = "(number m, number r1, number r2, number ox, number oy)",
                returns = "number moment",
                valuetype = "number"
            },
            momentForPolygon = {
                type = "function",
                description = "Returns the moment of intertia for the polygon.\n\n–> number m\n–> table polygon\n<– number moment",
                args = "(number m, table polygon)",
                returns = "number moment",
                valuetype = "number"
            },
            momentForRect = {
                type = "function",
                description = "Returns the moment of intertia for the rect.\n\n–> number m\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n<– number moment",
                args = "(number m, number x1, number y1, number x2, number y2)",
                returns = "number moment",
                valuetype = "number"
            },
            momentForSegment = {
                type = "function",
                description = "Returns the moment of intertia for the segment.\n\n–> number m\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n<– number moment",
                args = "(number m, number x1, number y1, number x2, number y2)",
                returns = "number moment",
                valuetype = "number"
            },
            setElasticity = {
                type = "method",
                description = "Sets the current elasticity.\n\n–> MOAICpShape self\n–> number elasticity: The elasticity.\n<– nil",
                args = "(MOAICpShape self, number elasticity)",
                returns = "nil"
            },
            setFriction = {
                type = "method",
                description = "Sets the current friction.\n\n–> MOAICpShape self\n–> number friction: The friction.\n<– nil",
                args = "(MOAICpShape self, number friction)",
                returns = "nil"
            },
            setGroup = {
                type = "method",
                description = "Sets the current group ID.\n\n–> MOAICpShape self\n–> number group: The group ID.\n<– nil",
                args = "(MOAICpShape self, number group)",
                returns = "nil"
            },
            setIsSensor = {
                type = "method",
                description = "Sets whether this shape is a sensor.\n\n–> MOAICpShape self\n–> boolean sensor: Whether this shape is a sensor.\n<– nil",
                args = "(MOAICpShape self, boolean sensor)",
                returns = "nil"
            },
            setLayers = {
                type = "method",
                description = "Sets the current layer ID.\n\n–> MOAICpShape self\n–> number layer: The layer ID.\n<– nil",
                args = "(MOAICpShape self, number layer)",
                returns = "nil"
            },
            setSurfaceVel = {
                type = "method",
                description = "Sets the current surface velocity.\n\n–> MOAICpShape self\n–> number x: The X component of the surface velocity.\n–> number y: The Y component of the surface velocity.\n<– nil",
                args = "(MOAICpShape self, number x, number y)",
                returns = "nil"
            },
            setType = {
                type = "method",
                description = "Sets the current collision type.\n\n–> MOAICpShape self\n–> number type: The collision type.\n<– nil",
                args = "(MOAICpShape self, number type)",
                returns = "nil"
            }
        }
    },
    MOAICpSpace = {
        type = "class",
        inherits = "MOAIAction",
        description = "Chipmunk Space.",
        childs = {
            ALL = {
                type = "value"
            },
            BEGIN = {
                type = "value"
            },
            POST_SOLVE = {
                type = "value"
            },
            PRE_SOLVE = {
                type = "value"
            },
            SEPARATE = {
                type = "value"
            },
            activateShapesTouchingShape = {
                type = "method",
                description = "Activates shapes that are currently touching the specified shape.\n\n–> MOAICpSpace self\n–> MOAICpShape shape\n<– nil",
                args = "(MOAICpSpace self, MOAICpShape shape)",
                returns = "nil"
            },
            getDamping = {
                type = "method",
                description = "Returns the current damping in the space.\n\n–> MOAICpSpace self\n<– number damping",
                args = "MOAICpSpace self",
                returns = "number damping",
                valuetype = "number"
            },
            getGravity = {
                type = "method",
                description = "Returns the current gravity as two return values (x grav, y grav).\n\n–> MOAICpSpace self\n<– number xGrav\n<– number yGrav",
                args = "MOAICpSpace self",
                returns = "(number xGrav, number yGrav)",
                valuetype = "number"
            },
            getIdleSpeedThreshold = {
                type = "method",
                description = "Returns the speed threshold which indicates whether a body is idle (less than or equal to threshold) or in motion (greater than threshold).\n\n–> MOAICpSpace self\n<– number idleThreshold",
                args = "MOAICpSpace self",
                returns = "number idleThreshold",
                valuetype = "number"
            },
            getIterations = {
                type = "method",
                description = "Returns the number of iterations the space is configured to perform.\n\n–> MOAICpSpace self\n<– number iterations",
                args = "MOAICpSpace self",
                returns = "number iterations",
                valuetype = "number"
            },
            getSleepTimeThreshold = {
                type = "method",
                description = "Returns the sleep time threshold.\n\n–> MOAICpSpace self\n<– number sleepTimeThreshold",
                args = "MOAICpSpace self",
                returns = "number sleepTimeThreshold",
                valuetype = "number"
            },
            getStaticBody = {
                type = "method",
                description = "Returns the static body associated with this space.\n\n–> MOAICpSpace self\n<– MOAICpBody staticBody",
                args = "MOAICpSpace self",
                returns = "MOAICpBody staticBody",
                valuetype = "MOAICpBody"
            },
            insertPrim = {
                type = "method",
                description = "Inserts a new prim into the world (can be used as a body, joint, etc.)\n\n–> MOAICpSpace self\n–> MOAICpPrim prim\n<– nil",
                args = "(MOAICpSpace self, MOAICpPrim prim)",
                returns = "nil"
            },
            rehashShape = {
                type = "method",
                description = "Updates the shape in the spatial hash.\n\n–> MOAICpSpace self\n–> MOAICpShape shape\n<– nil",
                args = "(MOAICpSpace self, MOAICpShape shape)",
                returns = "nil"
            },
            rehashStatic = {
                type = "method",
                description = "Updates the static shapes in the spatial hash.\n\n–> MOAICpSpace self\n<– nil",
                args = "MOAICpSpace self",
                returns = "nil"
            },
            removePrim = {
                type = "method",
                description = "Removes a prim (body, joint, etc.) from the space.\n\n–> MOAICpSpace self\n–> MOAICpPrim prim\n<– nil",
                args = "(MOAICpSpace self, MOAICpPrim prim)",
                returns = "nil"
            },
            resizeActiveHash = {
                type = "method",
                description = "Sets the dimensions of the active object hash.\n\n–> MOAICpSpace self\n–> number dim\n–> number count\n<– nil",
                args = "(MOAICpSpace self, number dim, number count)",
                returns = "nil"
            },
            resizeStaticHash = {
                type = "method",
                description = "Sets the dimensions of the static object hash.\n\n–> MOAICpSpace self\n–> number dim\n–> number count\n<– nil",
                args = "(MOAICpSpace self, number dim, number count)",
                returns = "nil"
            },
            setCollisionHandler = {
                type = "method",
                description = "Sets a function to handle the specific collision type on this object. If nil is passed as the handler, the collision handler is unset.\n\n–> MOAICpSpace self\n–> number collisionTypeA\n–> number collisionTypeB\n–> number mask\n–> function handler\n<– nil",
                args = "(MOAICpSpace self, number collisionTypeA, number collisionTypeB, number mask, function handler)",
                returns = "nil"
            },
            setDamping = {
                type = "method",
                description = "Sets the current damping in the space.\n\n–> MOAICpSpace self\n–> number damping\n<– nil",
                args = "(MOAICpSpace self, number damping)",
                returns = "nil"
            },
            setGravity = {
                type = "method",
                description = "Sets the current gravity in the space.\n\n–> MOAICpSpace self\n–> number xGrav\n–> number yGrav\n<– nil",
                args = "(MOAICpSpace self, number xGrav, number yGrav)",
                returns = "nil"
            },
            setIdleSpeedThreshold = {
                type = "method",
                description = "Sets the speed threshold which indicates whether a body is idle (less than or equal to threshold) or in motion (greater than threshold).\n\n–> MOAICpSpace self\n–> number threshold\n<– nil",
                args = "(MOAICpSpace self, number threshold)",
                returns = "nil"
            },
            setIterations = {
                type = "method",
                description = "Sets the number of iterations performed each simulation step.\n\n–> MOAICpSpace self\n–> number iterations\n<– nil",
                args = "(MOAICpSpace self, number iterations)",
                returns = "nil"
            },
            setSleepTimeThreshold = {
                type = "method",
                description = "Sets the sleep time threshold. This is the amount of time it takes bodies at rest to fall asleep.\n\n–> MOAICpSpace self\n–> number threshold\n<– nil",
                args = "(MOAICpSpace self, number threshold)",
                returns = "nil"
            },
            shapeForPoint = {
                type = "method",
                description = "Retrieves a shape located at the specified X and Y position, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).\n\n–> MOAICpSpace self\n–> number x\n–> number y\n[–> number layers]\n[–> number group]\n<– MOAICpShape shape",
                args = "(MOAICpSpace self, number x, number y, [number layers, [number group]])",
                returns = "MOAICpShape shape",
                valuetype = "MOAICpShape"
            },
            shapeForSegment = {
                type = "method",
                description = "Retrieves a shape that crosses the segment specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).\n\n–> MOAICpSpace self\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n[–> number layers]\n[–> number group]\n<– MOAICpShape shape",
                args = "(MOAICpSpace self, number x1, number y1, number x2, number y2, [number layers, [number group]])",
                returns = "MOAICpShape shape",
                valuetype = "MOAICpShape"
            },
            shapeListForPoint = {
                type = "method",
                description = "Retrieves a list of shapes that overlap the point specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).\n\n–> MOAICpSpace self\n–> number x\n–> number y\n[–> number layers]\n[–> number group]\n<– MOAICpShape shapes: The shapes that were matched as multiple return values.",
                args = "(MOAICpSpace self, number x, number y, [number layers, [number group]])",
                returns = "MOAICpShape shapes",
                valuetype = "MOAICpShape"
            },
            shapeListForRect = {
                type = "method",
                description = "Retrieves a list of shapes that overlap the rect specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).\n\n–> MOAICpSpace self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n[–> number layers]\n[–> number group]\n<– MOAICpShape shapes: The shapes that were matched as multiple return values.",
                args = "(MOAICpSpace self, number xMin, number yMin, number xMax, number yMax, [number layers, [number group]])",
                returns = "MOAICpShape shapes",
                valuetype = "MOAICpShape"
            },
            shapeListForSegment = {
                type = "method",
                description = "Retrieves a list of shapes that overlap the segment specified, that exists on the specified layer (or any layer if nil) and is part of the specified group (or any group if nil).\n\n–> MOAICpSpace self\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n[–> number layers]\n[–> number group]\n<– MOAICpShape shapes: The shapes that were matched as multiple return values.",
                args = "(MOAICpSpace self, number x1, number y1, number x2, number y2, [number layers, [number group]])",
                returns = "MOAICpShape shapes",
                valuetype = "MOAICpShape"
            }
        }
    },
    MOAICrittercismAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for Crittercism integration on Android devices. Crittercism provides real-time, actionable crash reports for mobile apps. Exposed to Lua via MOAICrittercism on all mobile platforms.",
        childs = {
            forceException = {
                type = "function",
                description = "Force and exception to send breadcrumbs to crittercism\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            init = {
                type = "function",
                description = "Initialize Crittercism.\n\n–> string appId: Available in Crittercism dashboard settings.\n<– nil",
                args = "string appId",
                returns = "nil"
            },
            leaveBreadcrumb = {
                type = "function",
                description = "Leave a breadcrumb (log statement) to trace execution.\n\n–> string breadcrumb: A string describing the code location.\n<– nil",
                args = "string breadcrumb",
                returns = "nil"
            },
            setUser = {
                type = "function",
                description = "Sets an identifier for a user\n\n–> string identifier: A string identifying the user.\n<– nil",
                args = "string identifier",
                returns = "nil"
            }
        }
    },
    MOAICrittercismIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for Crittercism integration on iOS devices. Crittercism provides real-time, actionable crash reports for mobile apps. Exposed to Lua via MOAICrittercism on all mobile platforms.",
        childs = {}
    },
    MOAIDataBuffer = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Buffer for loading and holding data. Data operations may be performed without additional penalty of marshalling buffers between Lua and C.",
        childs = {
            base64Decode = {
                type = "method",
                description = "If a string is provided, decodes it as a base64 encoded string. Otherwise, decodes the current data stored in this object as a base64 encoded sequence of characters.\n\n[–> MOAIDataBuffer self]\n[–> string data: The string data to decode. You must either provide either a MOAIDataBuffer (via a :base64Decode type call) or string data (via a .base64Decode type call), but not both.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be decoded. Otherwise the decoding occurs inline on the existing data buffer in this object, and nil is returned.",
                args = "[MOAIDataBuffer self, [string data]]",
                returns = "string output",
                valuetype = "string"
            },
            base64Encode = {
                type = "method",
                description = "If a string is provided, encodes it in base64. Otherwise, encodes the current data stored in this object as a base64 encoded sequence of characters.\n\n[–> MOAIDataBuffer self]\n[–> string data: The string data to encode. You must either provide either a MOAIDataBuffer (via a :base64Encode type call) or string data (via a .base64Encode type call), but not both.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be encoded. Otherwise the encoding occurs inline on the existing data buffer in this object, and nil is returned.",
                args = "[MOAIDataBuffer self, [string data]]",
                returns = "string output",
                valuetype = "string"
            },
            deflate = {
                type = "function",
                description = "Compresses the string or the current data stored in this object using the DEFLATE algorithm.\n\nOverload:\n–> string data: The string data to deflate.\n[–> number level: The level used in the DEFLATE algorithm.]\n[–> number windowBits: The window bits used in the DEFLATE algorithm.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be compressed. Otherwise the compression occurs inline on the existing data buffer in this object, and nil is returned.\n\nOverload:\n–> MOAIDataBuffer self\n[–> number level: The level used in the DEFLATE algorithm.]\n[–> number windowBits: The window bits used in the DEFLATE algorithm.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be compressed. Otherwise the compression occurs inline on the existing data buffer in this object, and nil is returned.",
                args = "((string data | MOAIDataBuffer self), [number level, [number windowBits]])",
                returns = "string output",
                valuetype = "string"
            },
            getSize = {
                type = "method",
                description = "Returns the number of bytes in this data buffer object.\n\n–> MOAIDataBuffer self\n<– number size: The number of bytes in this data buffer object.",
                args = "MOAIDataBuffer self",
                returns = "number size",
                valuetype = "number"
            },
            getString = {
                type = "method",
                description = "Returns the contents of the data buffer object as a string value.\n\n–> MOAIDataBuffer self\n<– string data: The data buffer object as a string.",
                args = "MOAIDataBuffer self",
                returns = "string data",
                valuetype = "string"
            },
            hexDecode = {
                type = "method",
                description = "If a string is provided, decodes it as a hex encoded string. Otherwise, decodes the current data stored in this object as a hex encoded sequence of bytes.\n\n[–> MOAIDataBuffer self]\n[–> string data: The string data to decode. You must either provide either a MOAIDataBuffer (via a :hexDecode type call) or string data (via a .hexDecode type call), but not both.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be decoded. Otherwise the decoding occurs inline on the existing data buffer in this object, and nil is returned.",
                args = "[MOAIDataBuffer self, [string data]]",
                returns = "string output",
                valuetype = "string"
            },
            hexEncode = {
                type = "method",
                description = "If a string is provided, encodes it in hex. Otherwise, encodes the current data stored in this object as a hex encoded sequence of characters.\n\n[–> MOAIDataBuffer self]\n[–> string data: The string data to encode. You must either provide either a MOAIDataBuffer (via a :hexEncode type call) or string data (via a .hexEncode type call), but not both.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be encoded. Otherwise the encoding occurs inline on the existing data buffer in this object, and nil is returned.",
                args = "[MOAIDataBuffer self, [string data]]",
                returns = "string output",
                valuetype = "string"
            },
            inflate = {
                type = "function",
                description = "Decompresses the string or the current data stored in this object using the DEFLATE algorithm.\n\nOverload:\n–> string data: The string data to inflate.\n[–> number windowBits: The window bits used in the DEFLATE algorithm.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be decompressed. Otherwise the decompression occurs inline on the existing data buffer in this object, and nil is returned.\n\nOverload:\n–> MOAIDataBuffer self\n[–> number windowBits: The window bits used in the DEFLATE algorithm.]\n<– string output: If passed a string, returns either a string or nil depending on whether it could be decompressed. Otherwise the decompression occurs inline on the existing data buffer in this object, and nil is returned.",
                valuetype = "string"
            },
            load = {
                type = "method",
                description = "Copies the data from the given file into this object. This method is a synchronous operation and will block until the file is loaded.\n\n–> MOAIDataBuffer self\n–> string filename: The path to the file that the data should be loaded from.\n[–> number detectZip: One of MOAIDataBuffer.NO_UNZIP, MOAIDataBuffer.NO_UNZIP, MOAIDataBuffer.NO_UNZIP]\n[–> number windowBits: The window bits used in the DEFLATE algorithm. Pass nil to use the default value.]\n<– boolean success: Whether the file could be loaded into the object.",
                args = "(MOAIDataBuffer self, string filename, [number detectZip, [number windowBits]])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            loadAsync = {
                type = "method",
                description = "Asynchronously copies the data from the given file into this object. This method is an asynchronous operation and will return immediately.\n\n–> MOAIDataBuffer self\n–> string filename: The path to the file that the data should be loaded from.\n–> MOAITaskQueue queue: The queue to perform the loading operation.\n[–> function callback: The function to be called when the asynchronous operation is complete. The MOAIDataBuffer is passed as the first parameter.]\n[–> number detectZip: One of MOAIDataBuffer.NO_INFLATE, MOAIDataBuffer.FORCE_INFLATE, MOAIDataBuffer.INFLATE_ON_EXT]\n[–> boolean inflateAsync: 'true' to inflate on task thread. 'false' to inflate on subscriber thread. Default value is 'true.']\n[–> number windowBits: The window bits used in the DEFLATE algorithm. Pass nil to use the default value.]\n<– MOAIDataIOTask task: A new MOAIDataIOTask which indicates the status of the task.",
                args = "(MOAIDataBuffer self, string filename, MOAITaskQueue queue, [function callback, [number detectZip, [boolean inflateAsync, [number windowBits]]]])",
                returns = "MOAIDataIOTask task",
                valuetype = "MOAIDataIOTask"
            },
            save = {
                type = "method",
                description = "Saves the data in this object to the given file. This method is a synchronous operation and will block until the data is saved.\n\n–> MOAIDataBuffer self\n–> string filename: The path to the file that the data should be saved to.\n<– boolean success: Whether the data could be saved to the file.",
                args = "(MOAIDataBuffer self, string filename)",
                returns = "boolean success",
                valuetype = "boolean"
            },
            saveAsync = {
                type = "method",
                description = "Asynchronously saves the data in this object to the given file. This method is an asynchronous operation and will return immediately.\n\n–> MOAIDataBuffer self\n–> string filename: The path to the file that the data should be saved to.\n–> MOAITaskQueue queue: The queue to perform the saving operation.\n[–> function callback: The function to be called when the asynchronous operation is complete. The MOAIDataBuffer is passed as the first parameter.]\n<– MOAIDataIOTask task: A new MOAIDataIOTask which indicates the status of the task.",
                args = "(MOAIDataBuffer self, string filename, MOAITaskQueue queue, [function callback])",
                returns = "MOAIDataIOTask task",
                valuetype = "MOAIDataIOTask"
            },
            setString = {
                type = "method",
                description = "Replaces the contents of this object with the string specified.\n\n–> MOAIDataBuffer self\n–> string data: The string data to replace the contents of this object with.\n<– nil",
                args = "(MOAIDataBuffer self, string data)",
                returns = "nil"
            },
            toCppHeader = {
                type = "function",
                description = "Convert data to CPP header file.\n\nOverload:\n–> string data: The string data to encode\n–> string name\n[–> number columns: Default value is 12]\n<– string output\n\nOverload:\n–> MOAIDataBuffer data: The data buffer to encode\n–> string name\n[–> number columns: Default value is 12]\n<– string output",
                args = "((string data | MOAIDataBuffer data), string name, [number columns])",
                returns = "string output",
                valuetype = "string"
            }
        }
    },
    MOAIDataBufferStream = {
        type = "class",
        inherits = "MOAIStream",
        description = "MOAIDataBufferStream locks an associated MOAIDataBuffer for reading and writing.",
        childs = {
            close = {
                type = "method",
                description = "Disassociates and unlocks the stream's MOAIDataBuffer.\n\n–> MOAIDataBufferStream self\n<– nil",
                args = "MOAIDataBufferStream self",
                returns = "nil"
            },
            open = {
                type = "method",
                description = "Associate the stream with a MOAIDataBuffer. Note that the MOAIDataBuffer will be locked with a mutex while it is open thus blocking any asynchronous operations.\n\n–> MOAIDataBufferStream self\n–> MOAIDataBuffer buffer\n<– boolean success",
                args = "(MOAIDataBufferStream self, MOAIDataBuffer buffer)",
                returns = "boolean success",
                valuetype = "boolean"
            }
        }
    },
    MOAIDebugLines = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Singleton for managing rendering of world space debug vectors.",
        childs = {
            PARTITION_CELLS = {
                type = "value"
            },
            PARTITION_PADDED_CELLS = {
                type = "value"
            },
            PROP_MODEL_BOUNDS = {
                type = "value"
            },
            PROP_WORLD_BOUNDS = {
                type = "value"
            },
            TEXT_BOX = {
                type = "value"
            },
            TEXT_BOX_BASELINES = {
                type = "value"
            },
            TEXT_BOX_LAYOUT = {
                type = "value"
            },
            setStyle = {
                type = "function",
                description = "Sets the particulars of a given debug line style.\n\n–> number styleID: See MOAIDebugLines class documentation for a list of styles.\n[–> number size: Pen size (in pixels) for the style. Default value is 1.]\n[–> number r: Red component of line color. Default value is 1.]\n[–> number g: Green component of line color. Default value is 1.]\n[–> number b: Blue component of line color. Default value is 1.]\n[–> number a: Alpha component of line color. Default value is 1.]\n<– nil",
                args = "(number styleID, [number size, [number r, [number g, [number b, [number a]]]]])",
                returns = "nil"
            },
            showStyle = {
                type = "function",
                description = "Enables or disables drawing of a given debug line style.\n\n–> number styleID: See MOAIDebugLines class documentation for a list of styles.\n[–> boolean show: Default value is 'true']\n<– nil",
                args = "(number styleID, [boolean show])",
                returns = "nil"
            }
        }
    },
    MOAIDeck = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Base class for decks.",
        childs = {
            setBoundsDeck = {
                type = "method",
                description = "Set or clear the bounds override deck.\n\n–> MOAIDeck self\n[–> MOAIBoundsDeck boundsDeck]\n<– nil",
                args = "(MOAIDeck self, [MOAIBoundsDeck boundsDeck])",
                returns = "nil"
            },
            setShader = {
                type = "method",
                description = "Set the shader to use if neither the deck item nor the prop specifies a shader.\n\n–> MOAIDeck self\n–> MOAIShader shader\n<– nil",
                args = "(MOAIDeck self, MOAIShader shader)",
                returns = "nil"
            },
            setTexture = {
                type = "method",
                description = "Set or load a texture for this deck.\n\n–> MOAIDeck self\n–> variant texture: A MOAITexture, MOAIMultiTexture, MOAIDataBuffer or a path to a texture file\n[–> number transform: Any bitwise combination of MOAITextureBase.QUANTIZE, MOAITextureBase.TRUECOLOR, MOAITextureBase.PREMULTIPLY_ALPHA]\n<– MOAIGfxState texture",
                args = "(MOAIDeck self, variant texture, [number transform])",
                returns = "MOAIGfxState texture",
                valuetype = "MOAIGfxState"
            }
        }
    },
    MOAIDeckRemapper = {
        type = "class",
        inherits = "MOAINode",
        description = "Remap deck indices. Most useful for controlling animated tiles in tilemaps. All indices are exposed as attributes that may be connected by setAttrLink or driven using MOAIAnim or MOAIAnimCurve.",
        childs = {
            reserve = {
                type = "method",
                description = "The total number of indices to remap. Index remaps will be initialized from 1 to N.\n\n–> MOAIDeckRemapper self\n[–> number size: Default value is 0.]\n<– nil",
                args = "(MOAIDeckRemapper self, [number size])",
                returns = "nil"
            },
            setBase = {
                type = "method",
                description = "Set the base offset for the range of indices to remap. Used when remapping only a portion of the indices in the original deck.\n\n–> MOAIDeckRemapper self\n[–> number base: Default value is 0.]\n<– nil",
                args = "(MOAIDeckRemapper self, [number base])",
                returns = "nil"
            },
            setRemap = {
                type = "method",
                description = "Remap a single index to a new value.\n\n–> MOAIDeckRemapper self\n–> number index: Index to remap.\n[–> number remap: New value for index. Default value is index (i.e. remove the remap).]\n<– nil",
                args = "(MOAIDeckRemapper self, number index, [number remap])",
                returns = "nil"
            }
        }
    },
    MOAIDialogAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for a simple native dialog implementation on Android devices. Exposed to Lua via MOAIDialog on all mobile platforms.",
        childs = {
            DIALOG_RESULT_CANCEL = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the cancel button."
            },
            DIALOG_RESULT_NEGATIVE = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the negative button."
            },
            DIALOG_RESULT_NEUTRAL = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the neutral button."
            },
            DIALOG_RESULT_POSITIVE = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the positive button."
            },
            showDialog = {
                type = "function",
                description = "Show a native dialog to the user.\n\n–> string title: The title of the dialog box. Can be nil.\n–> string message: The message to show the user. Can be nil.\n–> string positive: The text for the positive response dialog button. Can be nil.\n–> string neutral: The text for the neutral response dialog button. Can be nil.\n–> string negative: The text for the negative response dialog button. Can be nil.\n–> boolean cancelable: Specifies whether or not the dialog is cancelable\n[–> function callback: A function to callback when the dialog is dismissed. Default is nil.]\n<– nil",
                args = "(string title, string message, string positive, string neutral, string negative, boolean cancelable, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAIDialogIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for a simple native dialog implementation on iOS devices. Exposed to Lua via MOAIDialog on all mobile platforms.",
        childs = {
            DIALOG_RESULT_CANCEL = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the cancel button."
            },
            DIALOG_RESULT_NEGATIVE = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the negative button."
            },
            DIALOG_RESULT_NEUTRAL = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the neutral button."
            },
            DIALOG_RESULT_POSITIVE = {
                type = "value",
                description = "Result code when the dialog is dismissed by pressing the positive button."
            }
        }
    },
    MOAIDraw = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Singleton for performing immediate mode drawing operations. See MOAIScriptDeck.",
        childs = {
            drawBoxOutline = {
                type = "function",
                description = "Draw a box outline.\n\n–> number x0\n–> number y0\n–> number z0\n–> number x1\n–> number y1\n–> number z1\n<– nil",
                args = "(number x0, number y0, number z0, number x1, number y1, number z1)",
                returns = "nil"
            },
            drawCircle = {
                type = "function",
                description = "Draw a circle.\n\n–> number x\n–> number y\n–> number r\n–> number steps\n<– nil",
                args = "(number x, number y, number r, number steps)",
                returns = "nil"
            },
            drawEllipse = {
                type = "function",
                description = "Draw an ellipse.\n\n–> number x\n–> number y\n–> number xRad\n–> number yRad\n–> number steps\n<– nil",
                args = "(number x, number y, number xRad, number yRad, number steps)",
                returns = "nil"
            },
            drawLine = {
                type = "function",
                description = "Draw a line.\n\n–> ... vertices: List of vertices (x, y) or an array of vertices { x0, y0, x1, y1, ... , xn, yn }\n<– nil",
                args = "... vertices",
                returns = "nil"
            },
            drawPoints = {
                type = "function",
                description = "Draw a list of points.\n\n–> ... vertices: List of vertices (x, y) or an array of vertices { x0, y0, x1, y1, ... , xn, yn }\n<– nil",
                args = "... vertices",
                returns = "nil"
            },
            drawRay = {
                type = "function",
                description = "Draw a ray.\n\n–> number x\n–> number y\n–> number dx\n–> number dy\n<– nil",
                args = "(number x, number y, number dx, number dy)",
                returns = "nil"
            },
            drawRect = {
                type = "function",
                description = "Draw a rectangle.\n\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n<– nil",
                args = "(number x0, number y0, number x1, number y1)",
                returns = "nil"
            },
            drawText = {
                type = "function",
                description = "Draws a string.\n\n–> MOAIFont font\n–> number size: Font size\n–> string text\n–> number x: Left position\n–> number y: Top position\n–> number scale\n–> number shadowOffsetX\n–> number shadowOffsetY\n<– nil",
                args = "(MOAIFont font, number size, string text, number x, number y, number scale, number shadowOffsetX, number shadowOffsetY)",
                returns = "nil"
            },
            drawTexture = {
                type = "function",
                description = "Draw a filled rectangle.\n\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> MOAITexture texture\n<– nil",
                args = "(number x0, number y0, number x1, number y1, MOAITexture texture)",
                returns = "nil"
            },
            fillCircle = {
                type = "function",
                description = "Draw a filled circle.\n\n–> number x\n–> number y\n–> number r\n–> number steps\n<– nil",
                args = "(number x, number y, number r, number steps)",
                returns = "nil"
            },
            fillEllipse = {
                type = "function",
                description = "Draw a filled ellipse.\n\n–> number x\n–> number y\n–> number xRad\n–> number yRad\n–> number steps\n<– nil",
                args = "(number x, number y, number xRad, number yRad, number steps)",
                returns = "nil"
            },
            fillFan = {
                type = "function",
                description = "Draw a filled fan.\n\n–> ... vertices: List of vertices (x, y) or an array of vertices { x0, y0, x1, y1, ... , xn, yn }\n<– nil",
                args = "... vertices",
                returns = "nil"
            },
            fillRect = {
                type = "function",
                description = "Draw a filled rectangle.\n\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n<– nil",
                args = "(number x0, number y0, number x1, number y1)",
                returns = "nil"
            }
        }
    },
    MOAIEaseDriver = {
        type = "class",
        inherits = "MOAITimer",
        description = "Action that applies simple ease curves to node attributes.",
        childs = {
            reserveLinks = {
                type = "method",
                description = "Reserve links.\n\n–> MOAIEaseDriver self\n–> number nLinks\n<– nil",
                args = "(MOAIEaseDriver self, number nLinks)",
                returns = "nil"
            },
            setLink = {
                type = "method",
                description = "Set the ease for a target node attribute.\n\nOverload:\n–> MOAIEaseDriver self\n–> number idx: Index of the link;\n–> MOAINode target: Target node.\n–> number attrID: Index of the attribute to be driven.\n[–> number value: Value for attribute at the end of the ease. Default is 0.]\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– nil\n\nOverload:\n–> MOAIEaseDriver self\n–> number idx: Index of the link;\n–> MOAINode target: Target node.\n–> number attrID: Index of the attribute to be driven.\n–> MOAINode source: Node that you are linking to target.\n–> number sourceAttrID: Index of the attribute being linked.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– nil"
            }
        }
    },
    MOAIEaseType = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Namespace to hold ease modes. Moai ease in/out has opposite meaning of Flash ease in/out.",
        childs = {
            EASE_IN = {
                type = "value",
                description = "Quartic ease in - Fast start then slow when approaching value; ease into position."
            },
            EASE_OUT = {
                type = "value",
                description = "Quartic ease out - Slow start then fast when approaching value; ease out of position."
            },
            FLAT = {
                type = "value",
                description = "Stepped change - Maintain original value until end of ease."
            },
            LINEAR = {
                type = "value",
                description = "Linear interpolation."
            },
            SHARP_EASE_IN = {
                type = "value",
                description = "Octic ease in."
            },
            SHARP_EASE_OUT = {
                type = "value",
                description = "Octic ease out."
            },
            SHARP_SMOOTH = {
                type = "value",
                description = "Octic smooth."
            },
            SMOOTH = {
                type = "value",
                description = "Quartic ease out then ease in."
            },
            SOFT_EASE_IN = {
                type = "value",
                description = "Quadratic ease in."
            },
            SOFT_EASE_OUT = {
                type = "value",
                description = "Quadratic ease out."
            },
            SOFT_SMOOTH = {
                type = "value",
                description = "Quadratic smooth."
            }
        }
    },
    MOAIEnvironment = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Table of key/value pairs containing information about the current environment. Also contains the generateGUID (), which will move to MOAIUnique in a future release.\nIf a given key is not supported in the current environment it will not exist (it's value will be nil).\nThe keys are:\n- appDisplayName\n- appID\n- appVersion\n- cacheDirectory\n- carrierISOCountryCode\n- carrierMobileCountryCode\n- carrierMobileNetworkCode\n- carrierName\n- connectionType\n- countryCode\n- cpuabi\n- devBrand\n- devName\n- devManufacturer\n- devModel\n- devPlatform\n- devProduct\n- documentDirectory\n- iosRetinaDisplay\n- languageCode\n- numProcessors\n- osBrand\n- osVersion\n- resourceDirectory\n- screenDpi\n- verticalResolution\n- horizontalResolution\n- udid\n- openUdid",
        childs = {
            CONNECTION_TYPE_NONE = {
                type = "value",
                description = "Signifies that there is no active connection"
            },
            CONNECTION_TYPE_WIFI = {
                type = "value",
                description = "Signifies that the current connection is via WiFi"
            },
            CONNECTION_TYPE_WWAN = {
                type = "value",
                description = "Signifies that the current connection is via WWAN"
            },
            OS_BRAND_ANDROID = {
                type = "value",
                description = "Signifies that Moai is currently running on Android"
            },
            OS_BRAND_IOS = {
                type = "value",
                description = "Signifies that Moai is currently running on iOS"
            },
            OS_BRAND_LINUX = {
                type = "value",
                description = "Signifies that Moai is currently running on Linux"
            },
            OS_BRAND_OSX = {
                type = "value",
                description = "Signifies that Moai is currently running on OSX"
            },
            OS_BRAND_UNAVAILABLE = {
                type = "value",
                description = "Signifies that the operating system cannot be determined"
            },
            OS_BRAND_WINDOWS = {
                type = "value",
                description = "Signifies that Moai is currently running on Windows"
            },
            generateGUID = {
                type = "function",
                description = "Generates a globally unique identifier. This method will be moved to MOAIUnique in a future release.\n\n<– string GUID",
                args = "()",
                returns = "string GUID",
                valuetype = "string"
            },
            getMACAddress = {
                type = "function",
                description = "Finds and returns the primary MAC Address\n\n<– string MAC",
                args = "()",
                returns = "string MAC",
                valuetype = "string"
            },
            setValue = {
                type = "function",
                description = "Sets an environment value and also triggers the listener callback (if any).\n\n–> string key\n[–> variant value: Default value is nil.]\n<– nil",
                args = "(string key, [variant value])",
                returns = "nil"
            }
        }
    },
    MOAIEventSource = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Base class for all Lua-bound Moai objects that emit events and have an event table.",
        childs = {}
    },
    MOAIFacebookAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for Facebook integration on Android devices. Facebook provides social integration for sharing on www.facebook.com. Exposed to Lua via MOAIFacebook on all mobile platforms.",
        childs = {
            DIALOG_DID_COMPLETE = {
                type = "value",
                description = "Event code for a successfully completed Facebook dialog."
            },
            DIALOG_DID_NOT_COMPLETE = {
                type = "value",
                description = "Event code for a failed (or canceled) Facebook dialog."
            },
            REQUEST_RESPONSE = {
                type = "value",
                description = "Event code for graph request responses."
            },
            REQUEST_RESPONSE_FAILED = {
                type = "value",
                description = "Event code for failed graph request responses."
            },
            SESSION_DID_LOGIN = {
                type = "value",
                description = "Event code for a successfully completed Facebook login."
            },
            SESSION_DID_NOT_LOGIN = {
                type = "value",
                description = "Event code for a failed (or canceled) Facebook login."
            },
            extendToken = {
                type = "function",
                description = "Extend the token if needed\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            getExpirationDate = {
                type = "function",
                description = "Retrieve the Facebook login token expiration date.\n\n<– string expirationDate: token expiration date",
                args = "()",
                returns = "string expirationDate",
                valuetype = "string"
            },
            getToken = {
                type = "function",
                description = "Retrieve the Facebook login token.\n\n<– string token",
                args = "()",
                returns = "string token",
                valuetype = "string"
            },
            graphRequest = {
                type = "function",
                description = "Make a request on Facebook's Graph API\n\n–> string path\n[–> table parameters]\n<– nil",
                args = "(string path, [table parameters])",
                returns = "nil"
            },
            init = {
                type = "function",
                description = "Initialize Facebook.\n\n–> string appId: Available in Facebook developer settings.\n<– nil",
                args = "string appId",
                returns = "nil"
            },
            login = {
                type = "function",
                description = "Prompt the user to login to Facebook.\n\n[–> table permissions: Optional set of required permissions. See Facebook documentation for a full list. Default is nil.]\n<– nil",
                args = "[table permissions]",
                returns = "nil"
            },
            logout = {
                type = "function",
                description = "Log the user out of Facebook.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            postToFeed = {
                type = "function",
                description = "Post a message to the logged in users' news feed.\n\n–> string link: The URL that the post links to. See Facebook documentation.\n–> string picture: The URL of an image to include in the post. See Facebook documentation.\n–> string name: The name of the link. See Facebook documentation.\n–> string caption: The caption of the link. See Facebook documentation.\n–> string description: The description of the link. See Facebook documentation.\n–> string message: The message for the post. See Facebook documentation.\n<– nil",
                args = "(string link, string picture, string name, string caption, string description, string message)",
                returns = "nil"
            },
            sendRequest = {
                type = "function",
                description = "Send an app request to the logged in users' friends.\n\n[–> string message: The message for the request. See Facebook documentation. Default is nil.]\n<– nil",
                args = "[string message]",
                returns = "nil"
            },
            sessionValid = {
                type = "function",
                description = "Determine whether or not the current Facebook session is valid.\n\n<– boolean valid",
                args = "()",
                returns = "boolean valid",
                valuetype = "boolean"
            },
            setExpirationDate = {
                type = "function",
                description = "Set the Facebook login token expiration date.\n\n–> string expirationDate: The login token expiration date. See Facebook documentation.\n<– nil",
                args = "string expirationDate",
                returns = "nil"
            },
            setToken = {
                type = "function",
                description = "Set the Facebook login token.\n\n–> string token: The login token. See Facebook documentation.\n<– nil",
                args = "string token",
                returns = "nil"
            }
        }
    },
    MOAIFacebookIOS = {
        type = "class",
        inherits = "MOAIGlobalEventSource",
        description = "Wrapper for Facebook integration on iOS devices. Facebook provides social integration for sharing on www.facebook.com. Exposed to Lua via MOAIFacebook on all mobile platforms.",
        childs = {
            DIALOG_DID_COMPLETE = {
                type = "value",
                description = "Event code for a successfully completed Facebook dialog."
            },
            DIALOG_DID_NOT_COMPLETE = {
                type = "value",
                description = "Event code for a failed (or canceled) Facebook dialog."
            },
            REQUEST_RESPONSE = {
                type = "value",
                description = "Event code for graph request responses."
            },
            REQUEST_RESPONSE_FAILED = {
                type = "value",
                description = "Event code for failed graph request responses."
            },
            SESSION_DID_LOGIN = {
                type = "value",
                description = "Event code for a successfully completed Facebook login."
            },
            SESSION_DID_NOT_LOGIN = {
                type = "value",
                description = "Event code for a failed (or canceled) Facebook login."
            }
        }
    },
    MOAIFileStream = {
        type = "class",
        inherits = "MOAIStream",
        description = "MOAIFileStream opens a system file handle for reading or writing.",
        childs = {
            READ = {
                type = "value"
            },
            READ_WRITE = {
                type = "value"
            },
            READ_WRITE_AFFIRM = {
                type = "value"
            },
            READ_WRITE_NEW = {
                type = "value"
            },
            WRITE = {
                type = "value"
            },
            close = {
                type = "method",
                description = "Close and release the associated file handle.\n\n–> MOAIFileStream self\n<– nil",
                args = "MOAIFileStream self",
                returns = "nil"
            },
            open = {
                type = "method",
                description = "Open or create a file stream given a valid path.\n\n–> MOAIFileStream self\n–> string fileName\n[–> number mode: One of MOAIFileStream.APPEND, MOAIFileStream.READ, MOAIFileStream.READ_WRITE, MOAIFileStream.READ_WRITE_AFFIRM, MOAIFileStream.READ_WRITE_NEW, MOAIFileStream.WRITE. Default value is MOAIFileStream.READ.]\n<– boolean success",
                args = "(MOAIFileStream self, string fileName, [number mode])",
                returns = "boolean success",
                valuetype = "boolean"
            }
        }
    },
    MOAIFileSystem = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Functions for manipulating the file system.",
        childs = {
            affirmPath = {
                type = "function",
                description = "Creates a folder at 'path' if none exists.\n\n–> string path\n<– nil",
                args = "string path",
                returns = "nil"
            },
            checkFileExists = {
                type = "function",
                description = "Check for the existence of a file.\n\n–> string filename\n<– boolean exists",
                args = "string filename",
                returns = "boolean exists",
                valuetype = "boolean"
            },
            checkPathExists = {
                type = "function",
                description = "Check for the existence of a path.\n\n–> string path\n<– boolean exists",
                args = "string path",
                returns = "boolean exists",
                valuetype = "boolean"
            },
            copy = {
                type = "function",
                description = "Copy a file or directory to a new location.\n\n–> string srcPath\n–> string destPath\n<– boolean result",
                args = "(string srcPath, string destPath)",
                returns = "boolean result",
                valuetype = "boolean"
            },
            deleteDirectory = {
                type = "function",
                description = "Deletes a directory and all of its contents.\n\n–> string path\n[–> boolean recursive: If true, the directory and all contents beneath it will be purged. Otherwise, the directory will only be removed if empty.]\n<– boolean success",
                args = "(string path, [boolean recursive])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            deleteFile = {
                type = "function",
                description = "Deletes a file.\n\n–> string filename\n<– boolean success",
                args = "string filename",
                returns = "boolean success",
                valuetype = "boolean"
            },
            getAbsoluteDirectoryPath = {
                type = "function",
                description = "Returns the absolute path given a relative path.\n\n–> string path\n<– string absolute",
                args = "string path",
                returns = "string absolute",
                valuetype = "string"
            },
            getAbsoluteFilePath = {
                type = "function",
                description = "Returns the absolute path to a file. Result includes the file name.\n\n–> string filename\n<– string absolute",
                args = "string filename",
                returns = "string absolute",
                valuetype = "string"
            },
            getRelativePath = {
                type = "function",
                description = "Given an absolute path returns the relative path in relation to the current working directory.\n\n–> string path\n<– string path: -",
                args = "string path",
                returns = "string path",
                valuetype = "string"
            },
            getWorkingDirectory = {
                type = "function",
                description = "Returns the path to current working directory.\n\n<– string path",
                args = "()",
                returns = "string path",
                valuetype = "string"
            },
            listDirectories = {
                type = "function",
                description = "Lists the sub-directories contained in a directory.\n\n[–> string path: Path to search. Default is current directory.]\n<– table diresctories: A table of directory names (or nil if the path is invalid)",
                args = "[string path]",
                returns = "table diresctories",
                valuetype = "table"
            },
            listFiles = {
                type = "function",
                description = "Lists the files contained in a directory\n\n[–> string path: Path to search. Default is current directory.]\n<– table files: A table of filenames (or nil if the path is invalid)",
                args = "[string path]",
                returns = "table files",
                valuetype = "table"
            },
            mountVirtualDirectory = {
                type = "function",
                description = "Mount an archive as a virtual filesystem directory.\n\n–> string path: Virtual path.\n[–> string archive: Name of archive file to mount. Default value is nil.]\n<– boolean success",
                args = "(string path, [string archive])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            rename = {
                type = "function",
                description = "Renames a file or folder.\n\n–> string oldPath\n–> string newPath\n<– boolean success",
                args = "(string oldPath, string newPath)",
                returns = "boolean success",
                valuetype = "boolean"
            },
            setWorkingDirectory = {
                type = "function",
                description = "Sets the current working directory.\n\n–> string path\n<– boolean success",
                args = "string path",
                returns = "boolean success",
                valuetype = "boolean"
            }
        }
    },
    MOAIFmodEventInstance = {
        type = "class",
        inherits = "MOAITransform",
        description = "An instance of an FMOD Event Not to be confused with an Event, MOAIFmodEvent.",
        childs = {
            getBeatFraction = {
                type = "method",
                description = "Returns the beat fraction of this Event Instance (useful for music)\n\n–> MOAIFmodEventInstance self\n<– number beatFraction: Beat Fraction of this Event Instance",
                args = "MOAIFmodEventInstance self",
                returns = "number beatFraction",
                valuetype = "number"
            },
            getDominantFrequency = {
                type = "method",
                description = "Returns the fundamental frequency of this Event Instance.\n\n–> MOAIFmodEventInstance self\n<– number frequency: Dominant frequency of this Event Instance",
                args = "MOAIFmodEventInstance self",
                returns = "number frequency",
                valuetype = "number"
            },
            getMeasureFraction = {
                type = "method",
                description = "Returns the measure fraction of this Event Instance (useful for music)\n\n–> MOAIFmodEventInstance self\n<– number measureFraction: Measure Fraction of this Event Instance",
                args = "MOAIFmodEventInstance self",
                returns = "number measureFraction",
                valuetype = "number"
            },
            getName = {
                type = "method",
                description = "Get the name of the Event\n\n–> MOAIFmodEventInstance self\n<– string name: Name of the event",
                args = "MOAIFmodEventInstance self",
                returns = "string name",
                valuetype = "string"
            },
            getNumChannels = {
                type = "method",
                description = "Get the number of Channels in this Event's Channel Group\n\n–> MOAIFmodEventInstance self\n<– number channels: Number of channels in Event Instance's Channel Group",
                args = "MOAIFmodEventInstance self",
                returns = "number channels",
                valuetype = "number"
            },
            getParameter = {
                type = "method",
                description = "Gets the value (a number) of an Event parameter\n\n–> MOAIFmodEventInstance self\n–> string parameterName: The name of the Event Parameter\n<– number paramValue: The value of the Event Parameter",
                args = "(MOAIFmodEventInstance self, string parameterName)",
                returns = "number paramValue",
                valuetype = "number"
            },
            getPitch = {
                type = "method",
                description = "Gets the pitch of the Event Instance.\n\n–> MOAIFmodEventInstance self\n<– number pitch: pitch of this Event Instance",
                args = "MOAIFmodEventInstance self",
                returns = "number pitch",
                valuetype = "number"
            },
            getTempo = {
                type = "method",
                description = "Returns the tempo of this Event Instance (useful for music)\n\n–> MOAIFmodEventInstance self\n<– number tempo: Tempo of this Event Instance",
                args = "MOAIFmodEventInstance self",
                returns = "number tempo",
                valuetype = "number"
            },
            getTime = {
                type = "method",
                description = "Returns time within the Event, or if useSubsoundTime, will return the time within the *the first subsound only*\n\n–> MOAIFmodEventInstance self\n[–> boolean useSubsoundTime: If true, will return the time within the first subsound only (Default: false)]\n<– number time: Time within the Event",
                args = "(MOAIFmodEventInstance self, [boolean useSubsoundTime])",
                returns = "number time",
                valuetype = "number"
            },
            getVolume = {
                type = "method",
                description = "Gets the volume of the Event Instance.\n\n–> MOAIFmodEventInstance self\n<– number volume: volume of this Event Instance",
                args = "MOAIFmodEventInstance self",
                returns = "number volume",
                valuetype = "number"
            },
            isValid = {
                type = "method",
                description = "Checks to see if the instance is valid (i.e., currently playing)\n\n–> MOAIFmodEventInstance self\n<– boolean valid: True if the instance is currently playing, false otherwise",
                args = "MOAIFmodEventInstance self",
                returns = "boolean valid",
                valuetype = "boolean"
            },
            keyOff = {
                type = "method",
                description = "Sets the Event Instance to key off based on the passed in Event Parameter\n\n–> MOAIFmodEventInstance self\n–> string parameterName: The name of the Event Parameter\n<– nil",
                args = "(MOAIFmodEventInstance self, string parameterName)",
                returns = "nil"
            },
            mute = {
                type = "method",
                description = "Mutes the Event Instance currently playing.\n\n–> MOAIFmodEventInstance self\n[–> boolean setting: Whether to mute or unmute. Defaults to true (mute).]\n<– nil",
                args = "(MOAIFmodEventInstance self, [boolean setting])",
                returns = "nil"
            },
            pause = {
                type = "method",
                description = "Pauses the Event Instance currently playing.\n\n–> MOAIFmodEventInstance self\n[–> boolean setting: Whether to pause or unpause. Defaults to true (pause).]\n<– nil",
                args = "(MOAIFmodEventInstance self, [boolean setting])",
                returns = "nil"
            },
            setParameter = {
                type = "method",
                description = "Sets the value (a number) of an Event parameter\n\n–> MOAIFmodEventInstance self\n–> string parameterName: The name of the Event Parameter\n–> number paramValue: New value of the Event Parameter\n<– nil",
                args = "(MOAIFmodEventInstance self, string parameterName, number paramValue)",
                returns = "nil"
            },
            setPitch = {
                type = "method",
                description = "Sets the pitch of the Event Instance.\n\n–> MOAIFmodEventInstance self\n–> number pitch: A pitch level, from 0.0 to 10.0 inclusive. 0.5 = half pitch, 2.0 = double pitch. Default = 1.0\n<– nil",
                args = "(MOAIFmodEventInstance self, number pitch)",
                returns = "nil"
            },
            setVolume = {
                type = "method",
                description = "Sets the volume of the Event Instance.\n\n–> MOAIFmodEventInstance self\n–> number volume: Volume is a number between 0 and 1 (where 1 is at full volume).\n<– nil",
                args = "(MOAIFmodEventInstance self, number volume)",
                returns = "nil"
            },
            stop = {
                type = "method",
                description = "Stops the Event Instance from playing.\n\n–> MOAIFmodEventInstance self\n<– nil",
                args = "MOAIFmodEventInstance self",
                returns = "nil"
            },
            unloadOnSilence = {
                type = "method",
                description = "For streaming sounds -- unloads the Event from memory on silence. (A good guideline to use is: for sounds that don't repeat often)\n\n–> MOAIFmodEventInstance self\n[–> boolean setting: Whether to unload on silence or not (Default: true)]\n<– nil",
                args = "(MOAIFmodEventInstance self, [boolean setting])",
                returns = "nil"
            }
        }
    },
    MOAIFmodEventMgr = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Event Manager singleton that provides an interface to all implemented FMOD Designer features.",
        childs = {
            getEventDuration = {
                type = "function",
                description = "Returns the duration of an Event. Although multiple sounds can potentially be played from 1 Event, to support determinism, a consistent duration will be returned for a given Event.\n\n–> string eventName: The name of the Event\n[–> string lineName: Pass this is if you want the duration of a voice line]\n<– number duration: Duration of the Event, nil if Event is invalid",
                args = "(string eventName, [string lineName])",
                returns = "number duration",
                valuetype = "number"
            },
            getMemoryStats = {
                type = "function",
                description = "Get memory usage.\n\n[–> boolean blocking: Default value is 'false.']\n<– number currentAlloc\n<– number maxAlloc",
                args = "[boolean blocking]",
                returns = "(number currentAlloc, number maxAlloc)",
                valuetype = "number"
            },
            getMicrophone = {
                type = "function",
                description = "Returns the game microphone.\n\n<– MOAIFmodMicrophone mic: The game microphone",
                args = "()",
                returns = "MOAIFmodMicrophone mic",
                valuetype = "MOAIFmodMicrophone"
            },
            getSoundCategoryVolume = {
                type = "function",
                description = "Gets the volume for a sound category\n\n–> string categoryName: Name of the category whose volume you wanna know\n<– number categoryVolume: The volume of the category",
                args = "string categoryName",
                returns = "number categoryVolume",
                valuetype = "number"
            },
            init = {
                type = "function",
                description = "Initializes the sound system.\n\n<– boolean enabled",
                args = "()",
                returns = "boolean enabled",
                valuetype = "boolean"
            },
            isEnabled = {
                type = "function",
                description = "Returns true if the Event Manager is enabled (active).\n\n<– boolean enabled: True if the Event Manager is active, false otherwise",
                args = "()",
                returns = "boolean enabled",
                valuetype = "boolean"
            },
            isSoundCategoryMuted = {
                type = "function",
                description = "Checks to see whether a sound category is muted\n\n–> string categoryName: Name of the category whose volume you wanna know\n<– boolean isMuted: Returns whether the sound category is muted",
                args = "string categoryName",
                returns = "boolean isMuted",
                valuetype = "boolean"
            },
            isSoundCategoryPaused = {
                type = "function",
                description = "Checks to see whether a sound category is muted\n\n–> string categoryName: Name of the category whose volume you wanna know\n<– boolean isMuted: Returns whether the sound category is muted",
                args = "string categoryName",
                returns = "boolean isMuted",
                valuetype = "boolean"
            },
            loadGroup = {
                type = "function",
                description = "Loads the wave data and instance data associated with a particular group. Groups are reference counted internally, with each call to LoadGroup incrementing the ref count.\n\n–> string groupPath: Should be of the form ProjectName/GroupName\n–> boolean persistent: Passing true means that this group is expected to be around for the entire duration of the app\n–> boolean blockOnLoad: Passing true means that the main thread will block while loading wav data for this Group.\n<– boolean loaded: True if the Group successfully loaded after this call, false otherwise",
                args = "(string groupPath, boolean persistent, boolean blockOnLoad)",
                returns = "boolean loaded",
                valuetype = "boolean"
            },
            loadProject = {
                type = "function",
                description = "Loads a project from disk, but does not load any wav or instance data. Projects must be loaded before sounds can be played from them. For special voice projects, use loadVoiceProject()\n\n–> string projectName: The name of the .fev file (path should be relative from project root)\n<– boolean loaded: True if the project successfully loaded, false otherwise",
                args = "string projectName",
                returns = "boolean loaded",
                valuetype = "boolean"
            },
            loadVoiceProject = {
                type = "function",
                description = "Calls LoadProject and does all attendant special voice load stuff, as well. For regular projects, use loadProject()\n\n–> string projectName: The name of the .fsb file (path should be relative from project root)\n<– boolean loaded: True if the voice project successfully loaded, false otherwise",
                args = "string projectName",
                returns = "boolean loaded",
                valuetype = "boolean"
            },
            muteAllEvents = {
                type = "function",
                description = "Stops all events/sounds from playing.\n\n–> boolean muteSetting: Whether to mute (true) or unmute (false)\n<– nil",
                args = "boolean muteSetting",
                returns = "nil"
            },
            muteSoundCategory = {
                type = "function",
                description = "Mute a sound category\n\n–> string categoryName: Name of the category whose volume you wanna modify\n–> boolean muteSetting: Whether to mute the category or not (Default: true)\n<– nil",
                args = "(string categoryName, boolean muteSetting)",
                returns = "nil"
            },
            pauseSoundCategory = {
                type = "function",
                description = "Mute a sound category\n\n–> string categoryName: Name of the category whose volume you wanna modify\n–> boolean pauseSetting: Whether to mute the category or not (Default: true)\n<– nil",
                args = "(string categoryName, boolean pauseSetting)",
                returns = "nil"
            },
            playEvent2D = {
                type = "function",
                description = "Plays an FMOD Event in 2D. Calling this function on 3D Events is undefined.\n\n–> string eventName: Event to play\n[–> boolean loopSound: Will force the Event to loop even if it does not in the data.]\n<– MOAIFmodEventInstance eventInst: The Event instance",
                args = "(string eventName, [boolean loopSound])",
                returns = "MOAIFmodEventInstance eventInst",
                valuetype = "MOAIFmodEventInstance"
            },
            playEvent3D = {
                type = "function",
                description = "Plays an FMOD Event 3D. Calling this function on 2D Events is harmless, but not advised.\n\n–> string eventName: Event to play\n[–> number x: x position of this sound]\n[–> number y: y position of this sound]\n[–> number z: z position of this sound]\n[–> boolean loopSound: Will force the Event to loop even if it does not in the data.]\n<– MOAIFmodEventInstance eventInst: The Event instance",
                args = "(string eventName, [number x, [number y, [number z, [boolean loopSound]]]])",
                returns = "MOAIFmodEventInstance eventInst",
                valuetype = "MOAIFmodEventInstance"
            },
            playVoiceLine = {
                type = "function",
                description = "Plays a voice line that exists in a loaded voice project. Will play it 2D or 3D based on Event settings. Uses a unique identifier for the line that is not the name of the Event -- although the event serves as a template for how the line will play.\n\n–> string linename: Unique identifier for a voice line\n–> string eventName: The Event template to use for playing this line\n[–> number x: x position of this sound]\n[–> number y: y position of this sound]\n[–> number z: z position of this sound]\n<– MOAIFmodEventInstance eventInst: The Event instance",
                args = "(string linename, string eventName, [number x, [number y, [number z]]])",
                returns = "MOAIFmodEventInstance eventInst",
                valuetype = "MOAIFmodEventInstance"
            },
            preloadVoiceLine = {
                type = "function",
                description = "Preload a high demand voice line\n\n–> string eventName: The Event template to use for this line\n–> string lineName: The unique ID of the line to preload\n<– nil",
                args = "(string eventName, string lineName)",
                returns = "nil"
            },
            setDefaultReverb = {
                type = "function",
                description = "Set the default regional Reverb\n\n–> string reverbName: Name of the Reverb (defined in Designer)\n<– nil",
                args = "string reverbName",
                returns = "nil"
            },
            setDistantOcclusion = {
                type = "function",
                description = "Sets a lowpass filter on distant sounds -- a filter added to everything greater than a certain distance (minRange to maxRange) from the microphone to make it sound muffled/far away.\n\n–> number minRange: Minimum distance from mic\n–> number maxRange: Maximum distance from mic\n–> number maxOcclusion: Maximum occlusion value\n<– nil",
                args = "(number minRange, number maxRange, number maxOcclusion)",
                returns = "nil"
            },
            setNear2DBlend = {
                type = "function",
                description = "Blend sounds near the microphone to 2D sounds. When 3D Events are playing near the microphone, their positioning becomes distracting rather than helpful/interesting, so this is a way to blend them together as if they were all taking place just at the mic.\n\n–> number minRange: Minimum distance from mic\n–> number maxRange: Maximum distance from mic\n–> number maxLevel: Maximum pan level\n<– nil",
                args = "(number minRange, number maxRange, number maxLevel)",
                returns = "nil"
            },
            setSoundCategoryVolume = {
                type = "function",
                description = "Sets the volume for a sound category\n\n–> string categoryName: Name of the category whose volume you wanna modify\n–> number newVolume: New volume to set for the category\n<– nil",
                args = "(string categoryName, number newVolume)",
                returns = "nil"
            },
            stopAllEvents = {
                type = "function",
                description = "Stops all events/sounds from playing.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            unloadAllVoiceProjects = {
                type = "function",
                description = "Calls UnloadProject and does all attendant special voice unload stuff, as well. For regular projects, use unloadProject()\n\n<– boolean unloaded: True if voice projects are no longer loaded after this call, false otherwise.",
                args = "()",
                returns = "boolean unloaded",
                valuetype = "boolean"
            },
            unloadEvent = {
                type = "function",
                description = "Unloads the data associated with an Event. All instances of this Event will be stopped when this call is made. Returns true if the Event is no longer loaded after this call.\n\n–> string eventName: The name of the Event\n[–> boolean blockOnUnload: Passing true means that the main thread will block while unloading]\n<– boolean eventUnloaded: True if the Event is no longer loaded after this call.",
                args = "(string eventName, [boolean blockOnUnload])",
                returns = "boolean eventUnloaded",
                valuetype = "boolean"
            },
            unloadGroup = {
                type = "function",
                description = "Unloads the wave data and instance data associated with a particular group. Groups might not be unloaded immediately either because their reference count is not zero, or because the sound system is not ready to release the group.\n\n–> string groupPath: Should be of the form ProjectName/GroupName\n–> boolean unloadImmediately: If true is passed in, it'll try to unload now, but won't block. Passing false will allow it to wait before trying to unload\n<– boolean unloaded: True if the Group is no longer loaded after this call, false otherwise",
                args = "(string groupPath, boolean unloadImmediately)",
                returns = "boolean unloaded",
                valuetype = "boolean"
            },
            unloadPendingUnloads = {
                type = "function",
                description = "Unloads all the pending unload groups. Use between act changes in the game.\n\n[–> boolean blockOnUnload: Passing true means that the main thread will block while unloading]\n<– nil",
                args = "[boolean blockOnUnload]",
                returns = "nil"
            },
            unloadProject = {
                type = "function",
                description = "Unloads all data associated with a particular project. Completely flushes all memory associated with the project. For special voice projects, use unloadAllVoiceProjects()\n\n–> string projectName: The name of the .fev file (path should be relative from project root)\n<– boolean unloaded: True if the project is no longer loaded after this call, false otherwise.",
                args = "string projectName",
                returns = "boolean unloaded",
                valuetype = "boolean"
            }
        }
    },
    MOAIFmodEx = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "FMOD singleton.",
        childs = {
            getMemoryStats = {
                type = "function",
                description = "Get memory usage.\n\n[–> boolean blocking: Default value is 'false.']\n<– number currentAlloc\n<– number maxAlloc",
                args = "[boolean blocking]",
                returns = "(number currentAlloc, number maxAlloc)",
                valuetype = "number"
            },
            init = {
                type = "function",
                description = "Initializes the sound system.\n\n<– nil",
                args = "()",
                returns = "nil"
            }
        }
    },
    MOAIFmodExChannel = {
        type = "class",
        inherits = "MOAINode",
        description = "FMOD singleton.",
        childs = {
            getVolume = {
                type = "method",
                description = "Returns the current volume of the channel.\n\n–> MOAIFmodExChannel self\n<– number volume: the volume currently set in this channel.",
                args = "MOAIFmodExChannel self",
                returns = "number volume",
                valuetype = "number"
            },
            isPlaying = {
                type = "method",
                description = "Returns true if channel is playing.\n\n–> MOAIFmodExChannel self\n<– boolean",
                args = "MOAIFmodExChannel self",
                returns = "boolean",
                valuetype = "boolean"
            },
            moveVolume = {
                type = "method",
                description = "Creates a new MOAIAction that will move the volume from it's current value to the value specified.\n\n–> MOAIFmodExChannel self\n–> number target: The target volume.\n–> number delay: The delay until the action starts.\n–> number mode: The interpolation mode for the action.\n<– MOAIAction action: The new action. It is automatically started by this function.",
                args = "(MOAIFmodExChannel self, number target, number delay, number mode)",
                returns = "MOAIAction action",
                valuetype = "MOAIAction"
            },
            play = {
                type = "method",
                description = "Plays the specified sound, looping it if desired.\n\n–> MOAIFmodExChannel self\n–> MOAIFmodExSound sound: The sound to play.\n–> number loopCount: Number of loops.\n<– nil",
                args = "(MOAIFmodExChannel self, MOAIFmodExSound sound, number loopCount)",
                returns = "nil"
            },
            seekVolume = {
                type = "method",
                description = "Creates a new MOAIAction that will move the volume from it's current value to the value specified.\n\n–> MOAIFmodExChannel self\n–> number target: The target volume.\n–> number delay: The delay until the action starts.\n–> number mode: The interpolation mode for the action.\n<– MOAIAction action: The new action. It is automatically started by this function.",
                args = "(MOAIFmodExChannel self, number target, number delay, number mode)",
                returns = "MOAIAction action",
                valuetype = "MOAIAction"
            },
            setLooping = {
                type = "method",
                description = "Immediately sets looping for this channel.\n\n–> MOAIFmodExChannel self\n–> boolean looping: True if channel should loop.\n<– nil",
                args = "(MOAIFmodExChannel self, boolean looping)",
                returns = "nil"
            },
            setPaused = {
                type = "method",
                description = "Sets whether this channel is paused and hence does not play any sounds.\n\n–> MOAIFmodExChannel self\n–> boolean paused: Whether this channel is paused.\n<– nil",
                args = "(MOAIFmodExChannel self, boolean paused)",
                returns = "nil"
            },
            setVolume = {
                type = "method",
                description = "Immediately sets the volume of this channel.\n\n–> MOAIFmodExChannel self\n–> number volume: The volume of this channel.\n<– nil",
                args = "(MOAIFmodExChannel self, number volume)",
                returns = "nil"
            },
            stop = {
                type = "method",
                description = "Stops playing the sound on this channel.\n\n–> MOAIFmodExChannel self\n<– nil",
                args = "MOAIFmodExChannel self",
                returns = "nil"
            }
        }
    },
    MOAIFmodExSound = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "FMOD singleton.",
        childs = {
            load = {
                type = "method",
                description = "Loads the specified sound from file, or from a MOAIDataBuffer.\n\nOverload:\n–> MOAIFmodExSound self\n–> string filename: The path to the sound to load from file.\n–> boolean streaming: Whether the sound should be streamed from the data source, rather than preloaded.\n–> boolean async: Whether the sound file should be loaded asynchronously.\n<– nil\n\nOverload:\n–> MOAIFmodExSound self\n–> MOAIDataBuffer data: The MOAIDataBuffer that is storing sound data. You must either provide a string or MOAIDataBuffer, but not both.\n–> boolean streaming: Whether the sound should be streamed from the data source, rather than preloaded.\n<– nil",
                args = "(MOAIFmodExSound self, ((string filename, boolean streaming, boolean async) | (MOAIDataBuffer data, boolean streaming)))",
                returns = "nil"
            },
            loadBGM = {
                type = "method",
                description = "Loads the specified BGM sound from file, or from a MOAIDataBuffer.\n\nOverload:\n–> MOAIFmodExSound self\n–> string filename: The path to the sound to load from file.\n<– nil\n\nOverload:\n–> MOAIFmodExSound self\n–> MOAIDataBuffer data: The MOAIDataBuffer that is storing sound data.\n<– nil",
                args = "(MOAIFmodExSound self, (string filename | MOAIDataBuffer data))",
                returns = "nil"
            },
            loadSFX = {
                type = "method",
                description = "Loads the specified SFX sound from file, or from a MOAIDataBuffer.\n\nOverload:\n–> MOAIFmodExSound self\n–> string filename: The path to the sound to load from file.\n<– nil\n\nOverload:\n–> MOAIFmodExSound self\n–> MOAIDataBuffer data: The MOAIDataBuffer that is storing sound data.\n<– nil",
                args = "(MOAIFmodExSound self, (string filename | MOAIDataBuffer data))",
                returns = "nil"
            },
            release = {
                type = "method",
                description = "Releases the sound data from memory.\n\n–> MOAIFmodExSound self\n<– nil",
                args = "MOAIFmodExSound self",
                returns = "nil"
            }
        }
    },
    MOAIFmodMicrophone = {
        type = "class",
        inherits = "MOAITransform",
        description = "The in-game Microphone, with respect to which all the sounds are heard in the game. The Event Manager (MOAIFmodEventManager) must be initialized before the Microphone can be accessed. Should only be grabbed from MOAIFmodEventMgr",
        childs = {}
    },
    MOAIFont = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "MOAIFont is the top level object for managing sets of glyphs associated with a single font face. An instance of MOAIFont may contain glyph sets for multiple sizes of the font. Alternatively, a separate instance of MOAIFont may be used for each font size. Using a single font object for each size of a font face can make it easier to unload font sizes that are no longer needed.\nAn instance of MOAIFont may represent a dynamic or static font. Dynamic fonts are used to retrieve glyphs from a font file format on an as-needed basis. Static fonts have no associated font file format and therefore contain a fixed set of glyphs at runtime. For languages demanding very large character sets (such as Chinese), dynamic fonts are typically used. For languages where it is feasible to pre-render a full set of glyphs to texture (or bitmap fonts), static fonts may be used.\nMOAIFont orchestrates objects derived from MOAIFontReader and MOAIGlyphCacheBase to render glyphs into glyph sets. MOAIFontReader is responsible for interpreting the font file format (if any), retrieving glyph metrics (including kerning) and rendering glyphs to texture. MOAIGlyphCache is responsible for allocating textures to hold glyphs and for managing glyph placement within textures. For dynamic fonts, the typical setup uses MOAIFreeTypeFontReader and MOAIGlyphCache. For static fonts, there is usually no font reader; MOAIStaticGlyphCache is loaded directly from a serialized file and its texture memory is initialized with MOAIFont's setImage () command.\nAs mentioned, a single MOAIFont may be used to render multiple sizes of a font face. When glyphs need to be laid out or rendered, the font object will return a set of glyphs matching whatever size was requested. It is also possible to specify a default size that will be used if no size is requested for rendering or if no matching size is found. If no default size is set by the user, it will be set automatically the first time a specific size is requested.\nMOAIFont can also control how or if kerning tables are loaded when glyphs are being rendered. The default behavior is to load kerning information automatically. It is possible to prevent kerning information from being loaded. In this case, kerning tables may be loaded manually if so desired.",
        childs = {
            DEFAULT_FLAGS = {
                type = "value"
            },
            FONT_AUTOLOAD_KERNING = {
                type = "value"
            },
            getDefaultSize = {
                type = "method",
                description = "Requests the font's default size\n\n–> MOAIFont self\n<– number defaultSize",
                args = "MOAIFont self",
                returns = "number defaultSize",
                valuetype = "number"
            },
            getFilename = {
                type = "method",
                description = "Returns the filename of the font.\n\n–> MOAIFont self\n<– string name",
                args = "MOAIFont self",
                returns = "string name",
                valuetype = "string"
            },
            getFlags = {
                type = "method",
                description = "Returns the current flags.\n\n–> MOAIFont self\n<– number flags",
                args = "MOAIFont self",
                returns = "number flags",
                valuetype = "number"
            },
            getImage = {
                type = "method",
                description = "Requests a 'glyph map image' from the glyph cache currently attached to the font. The glyph map image stitches together the texture pages used by the glyph cache to produce a single image that represents a snapshot of all of the texture memory being used by the font.\n\n–> MOAIFont self\n<– MOAIImage image",
                args = "MOAIFont self",
                returns = "MOAIImage image",
                valuetype = "MOAIImage"
            },
            load = {
                type = "method",
                description = "Sets the filename of the font for use when loading glyphs.\n\n–> MOAIFont self\n–> string filename: The path to the font file to load.\n<– nil",
                args = "(MOAIFont self, string filename)",
                returns = "nil"
            },
            loadFromBMFont = {
                type = "method",
                description = "Sets the filename of the font for use when loading a BMFont.\n\n–> MOAIFont self\n–> string filename: The path to the BMFont file to load.\n[–> table textures: Table of preloaded textures.]\n<– nil",
                args = "(MOAIFont self, string filename, [table textures])",
                returns = "nil"
            },
            loadFromTTF = {
                type = "method",
                description = "Preloads a set of glyphs from a TTF or OTF. Included for backward compatibility. May be removed in a future release.\n\n–> MOAIFont self\n–> string filename\n–> string charcodes\n–> number points: The point size to be loaded from the TTF.\n[–> number dpi: The device DPI (dots per inch of device screen). Default value is 72 (points same as pixels).]\n<– nil",
                args = "(MOAIFont self, string filename, string charcodes, number points, [number dpi])",
                returns = "nil"
            },
            preloadGlyphs = {
                type = "method",
                description = "Loads and caches glyphs for quick access later.\n\n–> MOAIFont self\n–> string charCodes: A string which defines the characters found in the this->\n–> number points: The point size to be rendered onto the internal texture.\n[–> number dpi: The device DPI (dots per inch of device screen). Default value is 72 (points same as pixels).]\n<– nil",
                args = "(MOAIFont self, string charCodes, number points, [number dpi])",
                returns = "nil"
            },
            rebuildKerningTables = {
                type = "method",
                description = "Forces a full reload of the kerning tables for either a single glyph set within the font (if a size is specified) or for all glyph sets in the font.\n\nOverload:\n–> MOAIFont self\n<– nil\n\nOverload:\n–> MOAIFont self\n–> number points: The point size to be rendered onto the internal texture.\n[–> number dpi: The device DPI (dots per inch of device screen). Default value is 72 (points same as pixels).]\n<– nil",
                args = "(MOAIFont self, [number points, [number dpi]])",
                returns = "nil"
            },
            setCache = {
                type = "method",
                description = "Attaches or clears the glyph cache associated with the font. The cache is an object derived from MOAIGlyphCacheBase and may be a dynamic cache that can allocate space for new glyphs on an as-needed basis or a static cache that only supports direct loading of glyphs and glyph textures through MOAIFont's setImage () command.\n\n–> MOAIFont self\n[–> MOAIGlyphCacheBase cache: Default value is nil.]\n<– nil",
                args = "(MOAIFont self, [MOAIGlyphCacheBase cache])",
                returns = "nil"
            },
            setDefaultSize = {
                type = "method",
                description = "Selects a glyph set size to use as the default size when no other size is specified by objects wishing to use MOAIFont to render text.\n\n–> MOAIFont self\n–> number points: The point size to be rendered onto the internal texture.\n[–> number dpi: The device DPI (dots per inch of device screen). Default value is 72 (points same as pixels).]\n<– nil",
                args = "(MOAIFont self, number points, [number dpi])",
                returns = "nil"
            },
            setFlags = {
                type = "method",
                description = "Set flags to control font loading behavior. Right now the only supported flag is FONT_AUTOLOAD_KERNING which may be used to enable automatic loading of kern tables. This flag is initially true by default.\n\n–> MOAIFont self\n[–> number flags: Flags are FONT_AUTOLOAD_KERNING or DEFAULT_FLAGS. DEFAULT_FLAGS is the same as FONT_AUTOLOAD_KERNING. Alternatively, pass '0' to clear the flags.]\n<– nil",
                args = "(MOAIFont self, [number flags])",
                returns = "nil"
            },
            setImage = {
                type = "method",
                description = "Passes an image to the glyph cache currently attached to the font. The image will be used to recreate and initialize the texture memory managed by the glyph cache and used by the font. It will not affect any glyph entires that have already been laid out and stored in the glyph cache. If no cache is attached to the font, an instance of MOAIStaticGlyphCache will automatically be allocated.\n\n–> MOAIFont self\n–> MOAIImage image\n<– nil",
                args = "(MOAIFont self, MOAIImage image)",
                returns = "nil"
            },
            setReader = {
                type = "method",
                description = "Attaches or clears the MOAIFontReader associated with the font. MOAIFontReader is responsible for loading and rendering glyphs from a font file on demand. If you are using a static font and do not need a reader, set this field to nil.\n\n–> MOAIFont self\n[–> MOAIFontReader reader: Default value is nil.]\n<– nil",
                args = "(MOAIFont self, [MOAIFontReader reader])",
                returns = "nil"
            }
        }
    },
    MOAIFontReader = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAIFoo = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Example class for extending Moai using MOAILuaObject. Copy this object, rename it and add your own stuff. Just don't forget to register it with the runtime using the REGISTER_LUA_CLASS macro (see moaicore.cpp).",
        childs = {
            classHello = {
                type = "function",
                description = "Class (a.k.a. static) method. Prints the string 'MOAIFoo class foo!' to the console.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            instanceHello = {
                type = "method",
                description = "Prints the string 'MOAIFoo instance foo!' to the console.\n\n–> MOAIFoo self\n<– nil",
                args = "MOAIFoo self",
                returns = "nil"
            }
        }
    },
    MOAIFrameBuffer = {
        type = "class",
        inherits = "MOAIClearableView",
        description = "MOAIFrameBuffer is responsible for drawing a list of MOAIRenderable objects. MOAIRenderable is the base class for any object that can be drawn. This includes MOAIProp and MOAILayer. To use MOAIFrameBuffer pass a table of MOAIRenderable objects to setRenderTable (). The table will usually be a stack of MOAILayer objects. The contents of the table will be rendered the next time a frame is drawn. Note that the table must be an array starting with index 1. Objects will be rendered counting from the base index until 'nil' is encountered. The render table may include other tables as entries. These must also be arrays indexed from 1.",
        childs = {
            getPerformanceDrawCount = {
                type = "method",
                description = 'Returns the number of draw calls last frame.\n\n–> MOAIFrameBuffer self\n<– number count: Number of underlying graphics "draw" calls last frame.',
                args = "MOAIFrameBuffer self",
                returns = "number count",
                valuetype = "number"
            },
            getRenderTable = {
                type = "method",
                description = "Returns the table currently being used for rendering.\n\n–> MOAIFrameBuffer self\n<– table renderTable",
                args = "MOAIFrameBuffer self",
                returns = "table renderTable",
                valuetype = "table"
            },
            grabNextFrame = {
                type = "method",
                description = "Save the next frame rendered to\n\n–> MOAIFrameBuffer self\n–> MOAIImage image: Image to save the backbuffer to\n–> function callback: The function to execute when the frame has been saved into the image specified\n<– nil",
                args = "(MOAIFrameBuffer self, MOAIImage image, function callback)",
                returns = "nil"
            },
            setRenderTable = {
                type = "method",
                description = "Sets the table to be used for rendering. This should be an array indexed from 1 consisting of MOAIRenderable objects and sub-tables. Objects will be rendered in order starting from index 1 and continuing until 'nil' is encountered.\n\n–> MOAIFrameBuffer self\n–> table renderTable\n<– nil",
                args = "(MOAIFrameBuffer self, table renderTable)",
                returns = "nil"
            }
        }
    },
    MOAIFrameBufferTexture = {
        type = "class",
        inherits = "MOAIFrameBuffer MOAITextureBase",
        description = "This is an implementation of a frame buffer that may be attached to a MOAILayer for offscreen rendering. It is also a texture that may be bound and used like any other.",
        childs = {
            init = {
                type = "method",
                description = "Initializes frame buffer.\n\n–> MOAIFrameBufferTexture self\n–> number width\n–> number height\n[–> number colorFormat]\n[–> number depthFormat]\n[–> number stencilFormat]\n<– nil",
                args = "(MOAIFrameBufferTexture self, number width, number height, [number colorFormat, [number depthFormat, [number stencilFormat]]])",
                returns = "nil"
            }
        }
    },
    MOAIFreeTypeFontReader = {
        type = "class",
        inherits = "MOAIFontReader",
        description = "Implementation of MOAIFontReader that based on FreeType 2. Can load and render TTF and OTF font files.",
        childs = {}
    },
    MOAIGameCenterIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for iOS GameCenter functionality.",
        childs = {
            PLAYERSCOPE_FRIENDS = {
                type = "value",
                description = "Get leaderboard scores only for active player's friends."
            },
            PLAYERSCOPE_GLOBAL = {
                type = "value",
                description = "Get leaderboard scores for everyone."
            },
            TIMESCOPE_ALLTIME = {
                type = "value",
                description = "Get leaderboard scores for all time."
            },
            TIMESCOPE_TODAY = {
                type = "value",
                description = "Get leaderboard scores for today."
            },
            TIMESCOPE_WEEK = {
                type = "value",
                description = "Get leaderboard scores for the week."
            }
        }
    },
    MOAIGfxDevice = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Interface to the graphics singleton.",
        childs = {
            EVENT_RESIZE = {
                type = "value"
            },
            getFrameBuffer = {
                type = "function",
                description = "Returns the frame buffer associated with the device.\n\n<– MOAIFrameBuffer frameBuffer",
                args = "()",
                returns = "MOAIFrameBuffer frameBuffer",
                valuetype = "MOAIFrameBuffer"
            },
            getMaxTextureUnits = {
                type = "function",
                description = "Returns the total number of texture units available on the device.\n\n<– number maxTextureUnits",
                args = "()",
                returns = "number maxTextureUnits",
                valuetype = "number"
            },
            getViewSize = {
                type = "function",
                description = "Returns the width and height of the view\n\n<– number width\n<– number height",
                args = "()",
                returns = "(number width, number height)",
                valuetype = "number"
            },
            isProgrammable = {
                type = "function",
                description = "Returns a boolean indicating whether or not Moai is running under the programmable pipeline.\n\n<– boolean isProgrammable",
                args = "()",
                returns = "boolean isProgrammable",
                valuetype = "boolean"
            },
            setPenColor = {
                type = "function",
                description = "–> number r\n–> number g\n–> number b\n[–> number a: Default value is 1.]\n<– nil",
                args = "(number r, number g, number b, [number a])",
                returns = "nil"
            },
            setPenWidth = {
                type = "function",
                description = "–> number width\n<– nil",
                args = "number width",
                returns = "nil"
            },
            setPointSize = {
                type = "function",
                description = "–> number size\n<– nil",
                args = "number size",
                returns = "nil"
            }
        }
    },
    MOAIGfxQuad2D = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Single textured quad.",
        childs = {
            setQuad = {
                type = "method",
                description = "Set model space quad. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAIGfxQuad2D self\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAIGfxQuad2D self, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set the model space dimensions of the quad.\n\n–> MOAIGfxQuad2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIGfxQuad2D self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            setUVQuad = {
                type = "method",
                description = "Set the UV space dimensions of the quad. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAIGfxQuad2D self\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAIGfxQuad2D self, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setUVRect = {
                type = "method",
                description = "Set the UV space dimensions of the quad.\n\n–> MOAIGfxQuad2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIGfxQuad2D self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            transform = {
                type = "method",
                description = "Apply the given MOAITransform to all the vertices in the deck.\n\n–> MOAIGfxQuad2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAIGfxQuad2D self, MOAITransform transform)",
                returns = "nil"
            },
            transformUV = {
                type = "method",
                description = "Apply the given MOAITransform to all the uv coordinates in the deck.\n\n–> MOAIGfxQuad2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAIGfxQuad2D self, MOAITransform transform)",
                returns = "nil"
            }
        }
    },
    MOAIGfxQuadDeck2D = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Deck of textured quads.",
        childs = {
            reserve = {
                type = "method",
                description = "Set capacity of quad deck.\n\n–> MOAIGfxQuadDeck2D self\n–> number nQuads\n<– nil",
                args = "(MOAIGfxQuadDeck2D self, number nQuads)",
                returns = "nil"
            },
            setQuad = {
                type = "method",
                description = "Set model space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAIGfxQuadDeck2D self\n–> number idx: Index of the quad.\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAIGfxQuadDeck2D self, number idx, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set model space quad given a valid deck index and a rect.\n\n–> MOAIGfxQuadDeck2D self\n–> number idx: Index of the quad.\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIGfxQuadDeck2D self, number idx, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            setUVQuad = {
                type = "method",
                description = "Set UV space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAIGfxQuadDeck2D self\n–> number idx: Index of the quad.\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAIGfxQuadDeck2D self, number idx, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setUVRect = {
                type = "method",
                description = "Set UV space quad given a valid deck index and a rect.\n\n–> MOAIGfxQuadDeck2D self\n–> number idx: Index of the quad.\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIGfxQuadDeck2D self, number idx, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            transform = {
                type = "method",
                description = "Apply the given MOAITransform to all the vertices in the deck.\n\n–> MOAIGfxQuadDeck2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAIGfxQuadDeck2D self, MOAITransform transform)",
                returns = "nil"
            },
            transformUV = {
                type = "method",
                description = "Apply the given MOAITransform to all the uv coordinates in the deck.\n\n–> MOAIGfxQuadDeck2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAIGfxQuadDeck2D self, MOAITransform transform)",
                returns = "nil"
            }
        }
    },
    MOAIGfxQuadListDeck2D = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Deck of lists of textured quads. UV and model space quads are specified independently and associated via pairs. Pairs are referenced by lists sequentially. There may be multiple pairs with the same UV/model quad indices if geometry is used in multiple lists.",
        childs = {
            reserveLists = {
                type = "method",
                description = "Reserve quad lists.\n\n–> MOAIGfxQuadListDeck2D self\n–> number nLists\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number nLists)",
                returns = "nil"
            },
            reservePairs = {
                type = "method",
                description = "Reserve pairs.\n\n–> MOAIGfxQuadListDeck2D self\n–> number nPairs\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number nPairs)",
                returns = "nil"
            },
            reserveQuads = {
                type = "method",
                description = "Reserve quads.\n\n–> MOAIGfxQuadListDeck2D self\n–> number nQuads\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number nQuads)",
                returns = "nil"
            },
            reserveUVQuads = {
                type = "method",
                description = "Reserve UV quads.\n\n–> MOAIGfxQuadListDeck2D self\n–> number nUVQuads\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number nUVQuads)",
                returns = "nil"
            },
            setList = {
                type = "method",
                description = "Initializes quad pair list at index. A list starts at the index of a pair and then continues sequentially for n pairs after. So a list with base 3 and a run of 4 would display pair 3, 4, 5, and 6.\n\n–> MOAIGfxQuadListDeck2D self\n–> number idx\n–> number basePairID: The base pair of the list.\n–> number totalPairs: The run of the list - total pairs to display (including base).\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number idx, number basePairID, number totalPairs)",
                returns = "nil"
            },
            setPair = {
                type = "method",
                description = "Associates a quad with its UV coordinates.\n\n–> MOAIGfxQuadListDeck2D self\n–> number idx\n–> number uvQuadID\n–> number quadID\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number idx, number uvQuadID, number quadID)",
                returns = "nil"
            },
            setQuad = {
                type = "method",
                description = "Set model space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAIGfxQuadListDeck2D self\n–> number idx: Index of the quad.\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number idx, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set model space quad given a valid deck index and a rect.\n\n–> MOAIGfxQuadListDeck2D self\n–> number idx: Index of the quad.\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number idx, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            setUVQuad = {
                type = "method",
                description = "Set UV space quad given a valid deck index. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAIGfxQuadListDeck2D self\n–> number idx: Index of the quad.\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number idx, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setUVRect = {
                type = "method",
                description = "Set UV space quad given a valid deck index and a rect.\n\n–> MOAIGfxQuadListDeck2D self\n–> number idx: Index of the quad.\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, number idx, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            transform = {
                type = "method",
                description = "Apply the given MOAITransform to all the vertices in the deck.\n\n–> MOAIGfxQuadListDeck2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, MOAITransform transform)",
                returns = "nil"
            },
            transformUV = {
                type = "method",
                description = "Apply the given MOAITransform to all the uv coordinates in the deck.\n\n–> MOAIGfxQuadListDeck2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAIGfxQuadListDeck2D self, MOAITransform transform)",
                returns = "nil"
            }
        }
    },
    MOAIGfxResource = {
        type = "class",
        inherits = "MOAIGfxState",
        description = "Base class for graphics resources owned by OpenGL. Implements resource lifecycle including restoration from a lost graphics context (if possible).",
        childs = {
            getAge = {
                type = "method",
                description = "Returns the 'age' of the graphics resource. The age is the number of times MOAIRenderMgr has rendered a scene since the resource was last bound. It is part of the render count, not a timestamp. This may change to be time-based in future releases.\n\n–> MOAIGfxResource self\n<– number age",
                args = "MOAIGfxResource self",
                returns = "number age",
                valuetype = "number"
            },
            softRelease = {
                type = "method",
                description = "Attempt to release the resource. Generally this is used when responding to a memory warning from the system. A resource will only be released if it is renewable (i.e. has a renew callback or contains all information needed to reload the resources on demand). Using soft release can save an app in extreme memory circumstances, but may trigger reloads of resources during runtime which can significantly degrade performance.\n\n–> MOAIGfxResource self\n[–> number age: Release only if the texture hasn't been used in X frames.]\n<– boolean released: True if the texture was actually released.",
                args = "(MOAIGfxResource self, [number age])",
                returns = "boolean released",
                valuetype = "boolean"
            }
        }
    },
    MOAIGfxState = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Abstract base class for objects that represent changes to graphics state.",
        childs = {}
    },
    MOAIGlobalEventSource = {
        type = "class",
        inherits = "MOAIEventSource",
        description = "Derivation of MOAIEventSource for global Lua objects.",
        childs = {}
    },
    MOAIGlyphCache = {
        type = "class",
        inherits = "MOAIGlyphCacheBase",
        description = "This is the default implementation of a dynamic glyph cache. Right now it can only grow but support for reference counting glyphs and garbage collection will be added later.\nThe current implementation is set up in anticipation of garbage collection. If you use MOAIFont's getImage () to inspect the work of this cache you'll see that it is not as efficient in terms of texture use as it could be - glyphs are grouped by size, all glyphs for a given face size are given the same height and layout is orientated around rows. All of this will make it much easier to replace individual glyph slots as the set of glyphs that needs to be in memory changes. That said, we may offer an alternative dynamic cache implementation that attempts a more compact use of texture space, the tradeoff being that there won't be any garbage collection.\nThis implementation of the dynamic glyph cache does not implement setImage ().\nOf course, you can also derive your own implementation from MOAIGlyphCacheBase.",
        childs = {}
    },
    MOAIGlyphCacheBase = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Base class for implementations of glyph caches. A glyph cache is responsible for allocating textures to hold rendered glyphs and for placing individuals glyphs on those textures.\nEven though the glyph cache is responsible for placing glyphs on textures, the glyph cache does not have to keep track of glyph metrics. Glyph metrics are stored independently by the font. This means that glyph caches with equivalent textures may be swapped out for use with the same font.",
        childs = {
            setColorFormat = {
                type = "method",
                description = "The color format may be used by dynamic cache implementations when allocating new textures.\n\n–> MOAIGlyphCacheBase self\n–> number colorFmt: One of MOAIImage.COLOR_FMT_A_8, MOAIImage.COLOR_FMT_RGB_888, MOAIImage.COLOR_FMT_RGB_565, MOAIImage.COLOR_FMT_RGBA_5551, MOAIImage.COLOR_FMT_RGBA_4444, COLOR_FMT_RGBA_8888\n<– nil",
                args = "(MOAIGlyphCacheBase self, number colorFmt)",
                returns = "nil"
            }
        }
    },
    MOAIGrid = {
        type = "class",
        inherits = "MOAIGridSpace",
        description = "Grid data object. Grid cells are indexed starting and (1,1). Grid indices will wrap if out of range.",
        childs = {
            clearTileFlags = {
                type = "method",
                description = "Clears bits specified in mask.\n\n–> MOAIGrid self\n–> number xTile\n–> number yTile\n–> number mask\n<– nil",
                args = "(MOAIGrid self, number xTile, number yTile, number mask)",
                returns = "nil"
            },
            fill = {
                type = "method",
                description = "Set all tiles to a single value\n\n–> MOAIGrid self\n–> number value\n<– nil",
                args = "(MOAIGrid self, number value)",
                returns = "nil"
            },
            getTile = {
                type = "method",
                description = "Returns the value of a given tile.\n\n–> MOAIGrid self\n–> number xTile\n–> number yTile\n<– number tile",
                args = "(MOAIGrid self, number xTile, number yTile)",
                returns = "number tile",
                valuetype = "number"
            },
            getTileFlags = {
                type = "method",
                description = "Returns the masked value of a given tile.\n\n–> MOAIGrid self\n–> number xTile\n–> number yTile\n–> number mask\n<– number tile",
                args = "(MOAIGrid self, number xTile, number yTile, number mask)",
                returns = "number tile",
                valuetype = "number"
            },
            setRow = {
                type = "method",
                description = "Initializes a grid row given a variable argument list of values.\n\n–> MOAIGrid self\n–> number row\n–> ... values\n<– nil",
                args = "(MOAIGrid self, number row, ... values)",
                returns = "nil"
            },
            setTile = {
                type = "method",
                description = "Sets the value of a given tile\n\n–> MOAIGrid self\n–> number xTile\n–> number yTile\n–> number value\n<– nil",
                args = "(MOAIGrid self, number xTile, number yTile, number value)",
                returns = "nil"
            },
            setTileFlags = {
                type = "method",
                description = "Sets a tile's flags given a mask.\n\n–> MOAIGrid self\n–> number xTile\n–> number yTile\n–> number mask\n<– nil",
                args = "(MOAIGrid self, number xTile, number yTile, number mask)",
                returns = "nil"
            },
            streamTilesIn = {
                type = "method",
                description = "Reads tiles directly from a stream. Call this only after initializing the grid. Only the content of the tiles buffer is read.\n\n–> MOAIGrid self\n–> MOAIStream stream\n<– number bytesRead",
                args = "(MOAIGrid self, MOAIStream stream)",
                returns = "number bytesRead",
                valuetype = "number"
            },
            streamTilesOut = {
                type = "method",
                description = "Writes tiles directly to a stream. Only the content of the tiles buffer is written.\n\n–> MOAIGrid self\n–> MOAIStream stream\n<– number bytesWritten",
                args = "(MOAIGrid self, MOAIStream stream)",
                returns = "number bytesWritten",
                valuetype = "number"
            },
            toggleTileFlags = {
                type = "method",
                description = "Toggles a tile's flags given a mask.\n\n–> MOAIGrid self\n–> number xTile\n–> number yTile\n–> number mask\n<– nil",
                args = "(MOAIGrid self, number xTile, number yTile, number mask)",
                returns = "nil"
            }
        }
    },
    MOAIGridDeck2D = {
        type = "class",
        inherits = "MOAIDeck",
        description = "This deck renders 'brushes' which are sampled from a tile map. The tile map is specified by the attached grid, deck and remapper. Each 'brush' defines a rectangle of tiles to draw and an offset.",
        childs = {
            reserveBrushes = {
                type = "method",
                description = "Set capacity of grid deck.\n\n–> MOAIGridDeck2D self\n–> number nBrushes\n<– nil",
                args = "(MOAIGridDeck2D self, number nBrushes)",
                returns = "nil"
            },
            setBrush = {
                type = "method",
                description = "Initializes a brush.\n\n–> MOAIGridDeck2D self\n–> number idx: Index of the brush.\n–> number xTile\n–> number yTile\n–> number width\n–> number height\n[–> number xOff: Default value is 0.]\n[–> number yOff: Default value is 0.]\n<– nil",
                args = "(MOAIGridDeck2D self, number idx, number xTile, number yTile, number width, number height, [number xOff, [number yOff]])",
                returns = "nil"
            },
            setDeck = {
                type = "method",
                description = "Sets or clears the deck to be indexed by the grid.\n\n–> MOAIGridDeck2D self\n[–> MOAIDeck deck: Default value is nil.]\n<– nil",
                args = "(MOAIGridDeck2D self, [MOAIDeck deck])",
                returns = "nil"
            },
            setGrid = {
                type = "method",
                description = "Sets or clears the grid to be sampled by the brushes.\n\n–> MOAIGridDeck2D self\n[–> MOAIGrid grid: Default value is nil.]\n<– nil",
                args = "(MOAIGridDeck2D self, [MOAIGrid grid])",
                returns = "nil"
            },
            setRemapper = {
                type = "method",
                description = "Sets or clears the remapper (for remapping index values held in the grid).\n\n–> MOAIGridDeck2D self\n[–> MOAIDeckRemapper remapper: Default value is nil.]\n<– nil",
                args = "(MOAIGridDeck2D self, [MOAIDeckRemapper remapper])",
                returns = "nil"
            }
        }
    },
    MOAIGridPathGraph = {
        type = "class",
        inherits = "MOAIPathGraph MOAILuaObject",
        description = "Pathfinder graph adapter for MOAIGrid.",
        childs = {
            setGrid = {
                type = "method",
                description = "Set graph data to use for pathfinding.\n\n–> MOAIGridPathGraph self\n[–> MOAIGrid grid: Default value is nil.]\n<– nil",
                args = "(MOAIGridPathGraph self, [MOAIGrid grid])",
                returns = "nil"
            }
        }
    },
    MOAIGridSpace = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Represents spatial configuration of a grid. The grid is made up of cells. Inside of each cell is a tile. The tile can be larger or smaller than the cell and also offset from the cell. By default, tiles are the same size of their cells and are no offset.",
        childs = {
            DIAMOND_SHAPE = {
                type = "value"
            },
            HEX_SHAPE = {
                type = "value"
            },
            OBLIQUE_SHAPE = {
                type = "value"
            },
            SQUARE_SHAPE = {
                type = "value"
            },
            TILE_BOTTOM_CENTER = {
                type = "value"
            },
            TILE_CENTER = {
                type = "value"
            },
            TILE_LEFT_BOTTOM = {
                type = "value"
            },
            TILE_LEFT_CENTER = {
                type = "value"
            },
            TILE_LEFT_TOP = {
                type = "value"
            },
            TILE_RIGHT_BOTTOM = {
                type = "value"
            },
            TILE_RIGHT_CENTER = {
                type = "value"
            },
            TILE_RIGHT_TOP = {
                type = "value"
            },
            TILE_TOP_CENTER = {
                type = "value"
            },
            TILE_HIDE = {
                type = "value"
            },
            TILE_X_FLIP = {
                type = "value"
            },
            TILE_XY_FLIP = {
                type = "value"
            },
            TILE_Y_FLIP = {
                type = "value"
            },
            cellAddrToCoord = {
                type = "method",
                description = "Returns the coordinate of a cell given an address.\n\n–> MOAIGridSpace self\n–> number cellAddr\n<– number xTile\n<– number yTile",
                args = "(MOAIGridSpace self, number cellAddr)",
                returns = "(number xTile, number yTile)",
                valuetype = "number"
            },
            getCellAddr = {
                type = "method",
                description = "Returns the address of a cell given a coordinate (in tiles).\n\n–> MOAIGridSpace self\n–> number xTile\n–> number yTile\n<– number cellAddr",
                args = "(MOAIGridSpace self, number xTile, number yTile)",
                returns = "number cellAddr",
                valuetype = "number"
            },
            getCellSize = {
                type = "method",
                description = "Returns the dimensions of a single grid cell.\n\n–> MOAIGridSpace self\n<– number width\n<– number height",
                args = "MOAIGridSpace self",
                returns = "(number width, number height)",
                valuetype = "number"
            },
            getOffset = {
                type = "method",
                description = "Returns the offset of tiles from cells.\n\n–> MOAIGridSpace self\n<– number xOff\n<– number yOff",
                args = "MOAIGridSpace self",
                returns = "(number xOff, number yOff)",
                valuetype = "number"
            },
            getSize = {
                type = "method",
                description = "Returns the dimensions of the grid (in tiles).\n\n–> MOAIGridSpace self\n<– number width\n<– number height",
                args = "MOAIGridSpace self",
                returns = "(number width, number height)",
                valuetype = "number"
            },
            getTileLoc = {
                type = "method",
                description = "Returns the grid space coordinate of the tile. The optional 'position' flag determines the location of the coordinate within the tile.\n\n–> MOAIGridSpace self\n–> number xTile\n–> number yTile\n[–> number position: See MOAIGridSpace for list of positions. Default it MOAIGridSpace.TILE_CENTER.]\n<– number x\n<– number y",
                args = "(MOAIGridSpace self, number xTile, number yTile, [number position])",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            getTileSize = {
                type = "method",
                description = "Returns the dimensions of a single grid tile.\n\n–> MOAIGridSpace self\n<– number width\n<– number height",
                args = "MOAIGridSpace self",
                returns = "(number width, number height)",
                valuetype = "number"
            },
            initDiamondGrid = {
                type = "method",
                description = "Initialize a grid with hexagonal tiles.\n\n–> MOAIGridSpace self\n–> number width\n–> number height\n[–> number tileWidth: Default value is 1.]\n[–> number tileHeight: Default value is 1.]\n[–> number xGutter: Default value is 0.]\n[–> number yGutter: Default value is 0.]\n<– nil",
                args = "(MOAIGridSpace self, number width, number height, [number tileWidth, [number tileHeight, [number xGutter, [number yGutter]]]])",
                returns = "nil"
            },
            initHexGrid = {
                type = "method",
                description = "Initialize a grid with hexagonal tiles.\n\n–> MOAIGridSpace self\n–> number width\n–> number height\n[–> number radius: Default value is 1.]\n[–> number xGutter: Default value is 0.]\n[–> number yGutter: Default value is 0.]\n<– nil",
                args = "(MOAIGridSpace self, number width, number height, [number radius, [number xGutter, [number yGutter]]])",
                returns = "nil"
            },
            initObliqueGrid = {
                type = "method",
                description = "Initialize a grid with oblique tiles.\n\n–> MOAIGridSpace self\n–> number width\n–> number height\n[–> number tileWidth: Default value is 1.]\n[–> number tileHeight: Default value is 1.]\n[–> number xGutter: Default value is 0.]\n[–> number yGutter: Default value is 0.]\n<– nil",
                args = "(MOAIGridSpace self, number width, number height, [number tileWidth, [number tileHeight, [number xGutter, [number yGutter]]]])",
                returns = "nil"
            },
            initRectGrid = {
                type = "method",
                description = "Initialize a grid with rectangular tiles.\n\n–> MOAIGridSpace self\n–> number width\n–> number height\n[–> number tileWidth: Default value is 1.]\n[–> number tileHeight: Default value is 1.]\n[–> number xGutter: Default value is 0.]\n[–> number yGutter: Default value is 0.]\n<– nil",
                args = "(MOAIGridSpace self, number width, number height, [number tileWidth, [number tileHeight, [number xGutter, [number yGutter]]]])",
                returns = "nil"
            },
            locToCellAddr = {
                type = "method",
                description = "Returns the address of a cell given a a coordinate in grid space.\n\n–> MOAIGridSpace self\n–> number x\n–> number y\n<– number cellAddr",
                args = "(MOAIGridSpace self, number x, number y)",
                returns = "number cellAddr",
                valuetype = "number"
            },
            locToCoord = {
                type = "method",
                description = "Transforms a coordinate in grid space into a tile index.\n\n–> MOAIGridSpace self\n–> number x\n–> number y\n<– number xTile\n<– number yTile",
                args = "(MOAIGridSpace self, number x, number y)",
                returns = "(number xTile, number yTile)",
                valuetype = "number"
            },
            setRepeat = {
                type = "method",
                description = "Repeats a grid indexer along X or Y. Only used when a grid is attached.\n\n–> MOAIGridSpace self\n[–> boolean repeatX: Default value is true.]\n[–> boolean repeatY: Default value is repeatX.]\n<– nil",
                args = "(MOAIGridSpace self, [boolean repeatX, [boolean repeatY]])",
                returns = "nil"
            },
            setShape = {
                type = "method",
                description = "Set the shape of the grid tiles.\n\n–> MOAIGridSpace self\n[–> number shape: One of MOAIGridSpace.RECT_SHAPE, MOAIGridSpace.DIAMOND_SHAPE, MOAIGridSpace.OBLIQUE_SHAPE, MOAIGridSpace.HEX_SHAPE. Default value is MOAIGridSpace.RECT_SHAPE.]\n<– nil",
                args = "(MOAIGridSpace self, [number shape])",
                returns = "nil"
            },
            setSize = {
                type = "method",
                description = "Initializes dimensions of grid and reserves storage for tiles.\n\n–> MOAIGridSpace self\n–> number width\n–> number height\n[–> number cellWidth: Default value is 1.]\n[–> number cellHeight: Default value is 1.]\n[–> number xOff: X offset of the tile from the cell.]\n[–> number yOff: Y offset of the tile from the cell.]\n[–> number tileWidth: Default value is cellWidth.]\n[–> number tileHeight: Default value is cellHeight.]\n<– nil",
                args = "(MOAIGridSpace self, number width, number height, [number cellWidth, [number cellHeight, [number xOff, [number yOff, [number tileWidth, [number tileHeight]]]]]])",
                returns = "nil"
            },
            wrapCoord = {
                type = "method",
                description = "Wraps a tile index to the range of the grid.\n\n–> MOAIGridSpace self\n–> number xTile\n–> number yTile\n<– number xTile\n<– number yTile",
                args = "(MOAIGridSpace self, number xTile, number yTile)",
                returns = "(number xTile, number yTile)",
                valuetype = "number"
            }
        }
    },
    MOAIHarness = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Internal debugging and hooking class.",
        childs = {}
    },
    MOAIHashWriter = {
        type = "class",
        inherits = "MOAIStream",
        description = "MOAIHashWriter may be attached to another stream for the purpose of computing a hash while writing data to the other stream. Currently only MD5 and SHA256 are available.",
        childs = {
            close = {
                type = "method",
                description = "Flush any remaining buffered data and detach the target stream. (This only detaches the target from the formatter; it does not also close the target stream). Return the hash as a hex string.\n\n–> MOAIHashWriter self\n<– string hash",
                args = "MOAIHashWriter self",
                returns = "string hash",
                valuetype = "string"
            },
            openAdler32 = {
                type = "method",
                description = "Open a Adler32 hash stream for writing. (i.e. compute Adler32 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openCRC32 = {
                type = "method",
                description = "Open a CRC32 hash stream for writing. (i.e. compute CRC32 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openCRC32b = {
                type = "method",
                description = "Open a CRC32b hash stream for writing. (i.e. compute CRC32b hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openMD5 = {
                type = "method",
                description = "Open a MD5 hash stream for writing. (i.e. compute MD5 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openSHA1 = {
                type = "method",
                description = "Open a SHA1 hash stream for writing. (i.e. compute SHA1 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openSHA224 = {
                type = "method",
                description = "Open a SHA224 hash stream for writing. (i.e. compute SHA256 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openSHA256 = {
                type = "method",
                description = "Open a SHA256 hash stream for writing. (i.e. compute SHA256 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openSHA384 = {
                type = "method",
                description = "Open a SHA384 hash stream for writing. (i.e. compute SHA256 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openSHA512 = {
                type = "method",
                description = "Open a SHA512 hash stream for writing. (i.e. compute SHA256 hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openWhirlpool = {
                type = "method",
                description = "Open a Whirlpool hash stream for writing. (i.e. compute Whirlpool hash of data while writing)\n\n–> MOAIHashWriter self\n[–> MOAIStream target]\n<– boolean success",
                args = "(MOAIHashWriter self, [MOAIStream target])",
                returns = "boolean success",
                valuetype = "boolean"
            }
        }
    },
    MOAIHttpTaskBase = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Object for performing asynchronous HTTP/HTTPS tasks.",
        childs = {
            HTTP_DELETE = {
                type = "value"
            },
            HTTP_GET = {
                type = "value"
            },
            HTTP_HEAD = {
                type = "value"
            },
            HTTP_POST = {
                type = "value"
            },
            HTTP_PUT = {
                type = "value"
            },
            getProgress = {
                type = "method",
                description = "Returns the progress of the download.\n\n–> MOAIHttpTaskBase self\n<– number progress: the percentage the download has left ( form 0.0 to 1.0 )",
                args = "MOAIHttpTaskBase self",
                returns = "number progress",
                valuetype = "number"
            },
            getResponseCode = {
                type = "method",
                description = "Returns the response code returned by the server after a httpPost or httpGet call.\n\n–> MOAIHttpTaskBase self\n<– number code: The numeric response code returned by the server.",
                args = "MOAIHttpTaskBase self",
                returns = "number code",
                valuetype = "number"
            },
            getResponseHeader = {
                type = "method",
                description = "Returns the response header given its name, or nil if it wasn't provided by the server. Header names are case-insensitive and if multiple responses are given, they will be concatenated with a comma separating the values.\n\n–> MOAIHttpTaskBase self\n–> string header: The name of the header to return (case-insensitive).\n<– string response: The response given by the server or nil if none was specified.",
                args = "(MOAIHttpTaskBase self, string header)",
                returns = "string response",
                valuetype = "string"
            },
            getSize = {
                type = "method",
                description = "Returns the size of the string obtained from a httpPost or httpGet call.\n\n–> MOAIHttpTaskBase self\n<– number size: The string size. If the call found nothing, this will return the value zero (not nil).",
                args = "MOAIHttpTaskBase self",
                returns = "number size",
                valuetype = "number"
            },
            getString = {
                type = "method",
                description = "Returns the text obtained from a httpPost or httpGet call.\n\n–> MOAIHttpTaskBase self\n<– string text: The text string.",
                args = "MOAIHttpTaskBase self",
                returns = "string text",
                valuetype = "string"
            },
            httpGet = {
                type = "method",
                description = 'Sends an API call to the server for downloading data. The callback function (from setCallback) will run when the call is complete, i.e. this action is asynchronous and returns almost instantly.\n\n–> MOAIHttpTaskBase self\n–> string url: The URL on which to perform the GET request.\n[–> string useragent: Default value is "Moai SDK beta; support@getmoai.com"]\n[–> boolean verbose]\n[–> boolean blocking: Synchronous operation; block execution until complete. Default value is false.]\n<– nil',
                args = "(MOAIHttpTaskBase self, string url, [string useragent, [boolean verbose, [boolean blocking]]])",
                returns = "nil"
            },
            httpPost = {
                type = "method",
                description = 'Sends an API call to the server for downloading data. The callback function (from setCallback) will run when the call is complete, i.e. this action is asynchronous and returns almost instantly.\n\nOverload:\n–> MOAIHttpTaskBase self\n–> string url: The URL on which to perform the GET request.\n[–> string data: The string containing text to send as POST data.]\n[–> string useragent: Default value is "Moai SDK beta; support@getmoai.com"]\n[–> boolean verbose]\n[–> boolean blocking: Synchronous operation; block execution until complete. Default value is false.]\n<– nil\n\nOverload:\n–> MOAIHttpTaskBase self\n–> string url: The URL on which to perform the GET request.\n[–> MOAIDataBuffer data: A MOAIDataBuffer object to send as POST data.]\n[–> string useragent]\n[–> boolean verbose]\n[–> boolean blocking: Synchronous operation; block execution until complete. Default value is false.]\n<– nil',
                args = "(MOAIHttpTaskBase self, string url, [(string data | MOAIDataBuffer data), [string useragent, [boolean verbose, [boolean blocking]]]])",
                returns = "nil"
            },
            isBusy = {
                type = "method",
                description = "Returns whether or not the task is busy.\n\n–> MOAIHttpTaskBase self\n<– boolean busy",
                args = "MOAIHttpTaskBase self",
                returns = "boolean busy",
                valuetype = "boolean"
            },
            parseXml = {
                type = "method",
                description = "Parses the text data returned from a httpGet or httpPost operation as XML and then returns a MOAIXmlParser with the XML content initialized.\n\n–> MOAIHttpTaskBase self\n<– MOAIXmlParser parser: The MOAIXmlParser which has parsed the returned data.",
                args = "MOAIHttpTaskBase self",
                returns = "MOAIXmlParser parser",
                valuetype = "MOAIXmlParser"
            },
            performAsync = {
                type = "method",
                description = "Perform the HTTP task asynchronously.\n\n–> MOAIHttpTaskBase self\n<– nil",
                args = "MOAIHttpTaskBase self",
                returns = "nil"
            },
            performSync = {
                type = "method",
                description = "Perform the HTTP task synchronously ( blocking).\n\n–> MOAIHttpTaskBase self\n<– nil",
                args = "MOAIHttpTaskBase self",
                returns = "nil"
            },
            setBody = {
                type = "method",
                description = "Sets the body for a POST or PUT.\n\nOverload:\n–> MOAIHttpTaskBase self\n[–> string data: The string containing text to send as POST data.]\n<– nil\n\nOverload:\n–> MOAIHttpTaskBase self\n[–> MOAIDataBuffer data: A MOAIDataBuffer object to send as POST data.]\n<– nil",
                args = "(MOAIHttpTaskBase self, [string data | MOAIDataBuffer data])",
                returns = "nil"
            },
            setCallback = {
                type = "method",
                description = "Sets the callback function used when a request is complete.\n\n–> MOAIHttpTaskBase self\n–> function callback: The function to execute when the HTTP request is complete. The MOAIHttpTaskBase is passed as the first argument.\n<– nil",
                args = "(MOAIHttpTaskBase self, function callback)",
                returns = "nil"
            },
            setCookieDst = {
                type = "method",
                description = "Sets the file to save the cookies for this HTTP request\n\n–> MOAIHttpTaskBase self\n–> string filename: name and path of the file to save the cookies to\n<– nil",
                args = "(MOAIHttpTaskBase self, string filename)",
                returns = "nil"
            },
            setCookieSrc = {
                type = "method",
                description = "Sets the file to read the cookies for this HTTP request\n\n–> MOAIHttpTaskBase self\n–> string filename: name and path of the file to read the cookies from\n<– nil",
                args = "(MOAIHttpTaskBase self, string filename)",
                returns = "nil"
            },
            setFailOnError = {
                type = "method",
                description = "Sets whether or not curl calls will fail if the HTTP status code is above 400\n\n–> MOAIHttpTaskBase self\n–> boolean enable\n<– nil",
                args = "(MOAIHttpTaskBase self, boolean enable)",
                returns = "nil"
            },
            setFollowRedirects = {
                type = "method",
                description = "Sets whether or not curl should follow HTTP header redirects.\n\n–> MOAIHttpTaskBase self\n–> boolean follow\n<– nil",
                args = "(MOAIHttpTaskBase self, boolean follow)",
                returns = "nil"
            },
            setHeader = {
                type = "method",
                description = 'Sets a custom header field. May be used to override default headers.\n\n–> MOAIHttpTaskBase self\n–> string key\n[–> string value: Default is "".]\n<– nil',
                args = "(MOAIHttpTaskBase self, string key, [string value])",
                returns = "nil"
            },
            setStream = {
                type = "method",
                description = "Sets a custom stream to read data into.\n\n–> MOAIHttpTaskBase self\n–> MOAIStream stream\n<– nil",
                args = "(MOAIHttpTaskBase self, MOAIStream stream)",
                returns = "nil"
            },
            setTimeout = {
                type = "method",
                description = "Sets the timeout for the task.\n\n–> MOAIHttpTaskBase self\n–> number timeout\n<– nil",
                args = "(MOAIHttpTaskBase self, number timeout)",
                returns = "nil"
            },
            setUrl = {
                type = "method",
                description = "Sets the URL for the task.\n\n–> MOAIHttpTaskBase self\n–> string url\n<– nil",
                args = "(MOAIHttpTaskBase self, string url)",
                returns = "nil"
            },
            setUserAgent = {
                type = "method",
                description = "Sets the 'useragent' header for the task.\n\n–> MOAIHttpTaskBase self\n[–> string useragent: Default value is \"Moai SDK beta; support@getmoai.com\"]\n<– nil",
                args = "(MOAIHttpTaskBase self, [string useragent])",
                returns = "nil"
            },
            setVerb = {
                type = "method",
                description = "Sets the HTTP verb.\n\n–> MOAIHttpTaskBase self\n–> number verb: One of MOAIHttpTaskBase.HTTP_GET, MOAIHttpTaskBase.HTTP_HEAD, MOAIHttpTaskBase.HTTP_POST, MOAIHttpTaskBase.HTTP_PUT, MOAIHttpTaskBase.HTTP_DELETE\n<– nil",
                args = "(MOAIHttpTaskBase self, number verb)",
                returns = "nil"
            },
            setVerbose = {
                type = "method",
                description = "Sets the task implementation to print out debug information (if any).\n\n–> MOAIHttpTaskBase self\n[–> boolean verbose: Default value is false.]\n<– nil",
                args = "(MOAIHttpTaskBase self, [boolean verbose])",
                returns = "nil"
            }
        }
    },
    MOAIHttpTaskCurl = {
        type = "class",
        inherits = "MOAIHttpTaskBase",
        description = "Implementation of MOAIHttpTask based on libcurl.",
        childs = {}
    },
    MOAIHttpTaskNaCl = {
        type = "class",
        inherits = "MOAIHttpTaskBase",
        description = "Implementation of MOAIHttpTask based on NaCl.",
        childs = {}
    },
    MOAIHttpTaskNSURL = {
        type = "class",
        inherits = "MOAIHttpTaskBase",
        description = "Implementation of MOAIHttpTask based on libcurl.",
        childs = {}
    },
    MOAIImage = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Image/bitmap class.",
        childs = {
            FILTER_LINEAR = {
                type = "value"
            },
            FILTER_NEAREST = {
                type = "value"
            },
            COLOR_FMT_A_8 = {
                type = "value"
            },
            COLOR_FMT_RGB_565 = {
                type = "value"
            },
            COLOR_FMT_RGB_888 = {
                type = "value"
            },
            COLOR_FMT_RGBA_4444 = {
                type = "value"
            },
            COLOR_FMT_RGBA_5551 = {
                type = "value"
            },
            COLOR_FMT_RGBA_8888 = {
                type = "value"
            },
            PIXEL_FMT_INDEX_4 = {
                type = "value"
            },
            PIXEL_FMT_INDEX_8 = {
                type = "value"
            },
            PIXEL_FMT_TRUECOLOR = {
                type = "value"
            },
            POW_TWO = {
                type = "value"
            },
            PREMULTIPLY_ALPHA = {
                type = "value"
            },
            QUANTIZE = {
                type = "value"
            },
            TRUECOLOR = {
                type = "value"
            },
            bleedRect = {
                type = "method",
                description = "'Bleeds' the interior of the rectangle out by one pixel.\n\n–> MOAIImage self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIImage self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            compare = {
                type = "method",
                description = "Compares the image to another image.\n\n–> MOAIImage self\n–> MOAIImage other\n<– boolean areEqual: A value indicating whether the images are equal.",
                args = "(MOAIImage self, MOAIImage other)",
                returns = "boolean areEqual",
                valuetype = "boolean"
            },
            convertColors = {
                type = "method",
                description = "Return a copy of the image with a new color format. Not all provided formats are supported by OpenGL.\n\n–> MOAIImage self\n–> number colorFmt: One of MOAIImage.COLOR_FMT_A_8, MOAIImage.COLOR_FMT_RGB_888, MOAIImage.COLOR_FMT_RGB_565, MOAIImage.COLOR_FMT_RGBA_5551, MOAIImage.COLOR_FMT_RGBA_4444, COLOR_FMT_RGBA_8888\n<– MOAIImage image: Copy of the image initialized with given format.",
                args = "(MOAIImage self, number colorFmt)",
                returns = "MOAIImage image",
                valuetype = "MOAIImage"
            },
            convertToGrayScale = {
                type = "method",
                description = "Convert image to grayscale.\n\n–> MOAIImage self\n<– nil",
                args = "MOAIImage self",
                returns = "nil"
            },
            copy = {
                type = "method",
                description = "Copies an image.\n\n–> MOAIImage self\n<– MOAIImage image: Copy of the image.",
                args = "MOAIImage self",
                returns = "MOAIImage image",
                valuetype = "MOAIImage"
            },
            copyBits = {
                type = "method",
                description = "Copy a section of one image to another.\n\n–> MOAIImage self\n–> MOAIImage source: Source image.\n–> number srcX: X location in source image.\n–> number srcY: Y location in source image.\n–> number destX: X location in destination image.\n–> number destY: Y location in destination image.\n–> number width: Width of section to copy.\n–> number height: Height of section to copy.\n<– nil",
                args = "(MOAIImage self, MOAIImage source, number srcX, number srcY, number destX, number destY, number width, number height)",
                returns = "nil"
            },
            copyRect = {
                type = "method",
                description = "Copy a section of one image to another. Accepts two rectangles. Rectangles may be of different size and proportion. Section of image may also be flipped horizontally or vertically by reversing min/max of either rectangle.\n\n–> MOAIImage self\n–> MOAIImage source: Source image.\n–> number srcXMin\n–> number srcYMin\n–> number srcXMax\n–> number srcYMax\n–> number destXMin\n–> number destYMin\n[–> number destXMax: Default value is destXMin + srcXMax - srcXMin;]\n[–> number destYMax: Default value is destYMin + srcYMax - srcYMin;]\n[–> number filter: One of MOAIImage.FILTER_LINEAR, MOAIImage.FILTER_NEAREST. Default value is MOAIImage.FILTER_LINEAR.]\n<– nil",
                args = "(MOAIImage self, MOAIImage source, number srcXMin, number srcYMin, number srcXMax, number srcYMax, number destXMin, number destYMin, [number destXMax, [number destYMax, [number filter]]])",
                returns = "nil"
            },
            fillCircle = {
                type = "function",
                description = "Draw a filled circle.\n\n–> number x\n–> number y\n–> number radius\n[–> number r: Default value is 0.]\n[–> number g: Default value is 0.]\n[–> number b: Default value is 0.]\n[–> number a: Default value is 0.]\n<– nil",
                args = "(number x, number y, number radius, [number r, [number g, [number b, [number a]]]])",
                returns = "nil"
            },
            fillRect = {
                type = "method",
                description = "Fill a rectangle in the image with a solid color.\n\n–> MOAIImage self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n[–> number r: Default value is 0.]\n[–> number g: Default value is 0.]\n[–> number b: Default value is 0.]\n[–> number a: Default value is 0.]\n<– nil",
                args = "(MOAIImage self, number xMin, number yMin, number xMax, number yMax, [number r, [number g, [number b, [number a]]]])",
                returns = "nil"
            },
            getColor32 = {
                type = "method",
                description = "Returns a 32-bit packed RGBA value from the image for a given pixel coordinate.\n\n–> MOAIImage self\n–> number x\n–> number y\n<– number color",
                args = "(MOAIImage self, number x, number y)",
                returns = "number color",
                valuetype = "number"
            },
            getFormat = {
                type = "method",
                description = "Returns the color format of the image.\n\n–> MOAIImage self\n<– number colorFormat",
                args = "MOAIImage self",
                returns = "number colorFormat",
                valuetype = "number"
            },
            getRGBA = {
                type = "method",
                description = "Returns an RGBA color as four floating point values.\n\n–> MOAIImage self\n–> number x\n–> number y\n<– number r\n<– number g\n<– number b\n<– number a",
                args = "(MOAIImage self, number x, number y)",
                returns = "(number r, number g, number b, number a)",
                valuetype = "number"
            },
            getSize = {
                type = "method",
                description = "Returns the width and height of the image.\n\n–> MOAIImage self\n<– number width\n<– number height",
                args = "MOAIImage self",
                returns = "(number width, number height)",
                valuetype = "number"
            },
            init = {
                type = "method",
                description = "Initializes the image with a width, height and color format.\n\n–> MOAIImage self\n–> number width\n–> number height\n[–> number colorFmt: One of MOAIImage.COLOR_FMT_A_8, MOAIImage.COLOR_FMT_RGB_888, MOAIImage.COLOR_FMT_RGB_565, MOAIImage.COLOR_FMT_RGBA_5551, MOAIImage.COLOR_FMT_RGBA_4444, MOAIImage.COLOR_FMT_RGBA_8888. Default value is MOAIImage.COLOR_FMT_RGBA_8888.]\n<– nil",
                args = "(MOAIImage self, number width, number height, [number colorFmt])",
                returns = "nil"
            },
            load = {
                type = "method",
                description = "Loads an image from a PNG.\n\n–> MOAIImage self\n–> string filename\n[–> number transform: One of MOAIImage.POW_TWO, One of MOAIImage.QUANTIZE, One of MOAIImage.TRUECOLOR, One of MOAIImage.PREMULTIPLY_ALPHA]\n<– nil",
                args = "(MOAIImage self, string filename, [number transform])",
                returns = "nil"
            },
            loadFromBuffer = {
                type = "method",
                description = "Loads an image from a buffer.\n\n–> MOAIImage self\n–> MOAIDataBuffer buffer: Buffer containing the image\n[–> number transform: One of MOAIImage.POW_TWO, One of MOAIImage.QUANTIZE, One of MOAIImage.TRUECOLOR, One of MOAIImage.PREMULTIPLY_ALPHA]\n<– nil",
                args = "(MOAIImage self, MOAIDataBuffer buffer, [number transform])",
                returns = "nil"
            },
            padToPow2 = {
                type = "method",
                description = "Copies an image and returns a new image padded to the next power of 2 along each dimension. Original image will be in the upper left hand corner of the new image.\n\n–> MOAIImage self\n<– MOAIImage image: Copy of the image padded to the next nearest power of two along each dimension.",
                args = "MOAIImage self",
                returns = "MOAIImage image",
                valuetype = "MOAIImage"
            },
            resize = {
                type = "method",
                description = "Copies the image to an image with a new size.\n\n–> MOAIImage self\n–> number width: New width of the image.\n–> number height: New height of the image.\n[–> number filter: One of MOAIImage.FILTER_LINEAR, MOAIImage.FILTER_NEAREST. Default value is MOAIImage.FILTER_LINEAR.]\n<– MOAIImage image",
                args = "(MOAIImage self, number width, number height, [number filter])",
                returns = "MOAIImage image",
                valuetype = "MOAIImage"
            },
            resizeCanvas = {
                type = "method",
                description = "Copies the image to a canvas with a new size. If the canvas is larger than the original image, the extra pixels will be initialized with 0. Pass in a new frame or just a new width and height. Negative values are permitted for the frame.\n\nOverload:\n–> MOAIImage self\n–> number width: New width of the image.\n–> number height: New height of the image.\n<– MOAIImage image\n\nOverload:\n–> MOAIImage self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– MOAIImage image",
                args = "(MOAIImage self, ((number width, number height) | (number xMin, number yMin, number xMax, number yMax)))",
                returns = "MOAIImage image",
                valuetype = "MOAIImage"
            },
            setColor32 = {
                type = "method",
                description = "Sets 32-bit the packed RGBA value for a given pixel coordinate. Parameter will be converted to the native format of the image.\n\n–> MOAIImage self\n–> number x\n–> number y\n–> number color\n<– nil",
                args = "(MOAIImage self, number x, number y, number color)",
                returns = "nil"
            },
            setRGBA = {
                type = "method",
                description = "Sets a color using RGBA floating point values.\n\n–> MOAIImage self\n–> number x\n–> number y\n–> number r\n–> number g\n–> number b\n[–> number a: Default value is 1.]\n<– nil",
                args = "(MOAIImage self, number x, number y, number r, number g, number b, [number a])",
                returns = "nil"
            },
            writePNG = {
                type = "method",
                description = "Write image to a PNG file.\n\n–> MOAIImage self\n–> string filename\n<– nil",
                args = "(MOAIImage self, string filename)",
                returns = "nil"
            }
        }
    },
    MOAIImageTexture = {
        type = "class",
        inherits = "MOAITextureBase MOAIImage",
        description = "Binds an image (CPU memory) to a texture (GPU memory). Regions of the texture (or the entire texture) may be invalidated. Invalidated regions will be reloaded into GPU memory the next time the texture is bound.",
        childs = {
            invalidate = {
                type = "method",
                description = "Invalidate either a sub-region of the texture or the whole texture. Invalidated regions will be reloaded from the image the next time the texture is bound.\n\n–> MOAIImageTexture self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIImageTexture self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            }
        }
    },
    MOAIIndexBuffer = {
        type = "class",
        inherits = "MOAILuaObject MOAIGfxResource",
        description = "Index buffer class. Unused at this time.",
        childs = {
            release = {
                type = "method",
                description = "Release any memory held by this index buffer.\n\n–> MOAIIndexBuffer self\n<– nil",
                args = "MOAIIndexBuffer self",
                returns = "nil"
            },
            reserve = {
                type = "method",
                description = "Set capacity of buffer.\n\n–> MOAIIndexBuffer self\n–> number nIndices\n<– nil",
                args = "(MOAIIndexBuffer self, number nIndices)",
                returns = "nil"
            },
            setIndex = {
                type = "method",
                description = "Initialize an index.\n\n–> MOAIIndexBuffer self\n–> number idx\n–> number value\n<– nil",
                args = "(MOAIIndexBuffer self, number idx, number value)",
                returns = "nil"
            }
        }
    },
    MOAIInputDevice = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Manager class for input bindings. Has no public methods.",
        childs = {}
    },
    MOAIInputMgr = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Input device class. Has no public methods.",
        childs = {}
    },
    MOAIInstanceEventSource = {
        type = "class",
        inherits = "MOAIEventSource",
        description = "Derivation of MOAIEventSource for non-global Lua objects.",
        childs = {
            getListener = {
                type = "method",
                description = "Gets the listener callback for a given event ID.\n\n–> MOAIInstanceEventSource self\n–> number eventID: The ID of the event.\n<– function listener: The listener callback.",
                args = "(MOAIInstanceEventSource self, number eventID)",
                returns = "function listener",
                valuetype = "function"
            },
            setListener = {
                type = "method",
                description = "Sets a listener callback for a given event ID. It is up to individual classes to declare their event IDs.\n\n–> MOAIInstanceEventSource self\n–> number eventID: The ID of the event.\n[–> function callback: The callback to be called when the object emits the event. Default value is nil.]\n<– nil",
                args = "(MOAIInstanceEventSource self, number eventID, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAIJoystickSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Analog and digital joystick sensor.",
        childs = {
            getVector = {
                type = "method",
                description = "Returns the joystick vector.\n\n–> MOAIJoystickSensor self\n<– number x\n<– number y",
                args = "MOAIJoystickSensor self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when the joystick vector changes.\n\n–> MOAIJoystickSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAIJoystickSensor self, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAIJsonParser = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Converts between Lua and JSON.",
        childs = {
            decode = {
                type = "function",
                description = "Decode a JSON string into a hierarchy of Lua tables.\n\n–> string input\n<– table result",
                args = "string input",
                returns = "table result",
                valuetype = "table"
            },
            encode = {
                type = "function",
                description = "Encode a hierarchy of Lua tables into a JSON string.\n\n–> table input\n[–> number flags]\n<– string result",
                args = "(table input, [number flags])",
                returns = "string result",
                valuetype = "string"
            }
        }
    },
    MOAIKeyboardAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for the native keyboard on Android. Compatible with the iOS methods, albeit with JNI interjection.",
        childs = {}
    },
    MOAIKeyboardIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for the native keyboard. This is a first pass of the keyboard functionality and is missing some important features (such as spell check) that will be added in the near future. We also need to trap the keyboard notifications that tell us the position and dimensions of the keyboard.\nThe decision to divorce the keyboard from the text input field was deliberate. We're not ready (and may never be ready) to own a binding to a full set of native UI widgets. In future releases we'll give better was to connect the software keyboard to a MOAITextBox. This will give better integration of editable text fields with Moai. Even though this is more work up front, it means it will be easier in the long run to keep editable text synchronized with everything else in a scene.\nThe other short term limitation we face is complex text layout for languages such as Arabic. Since we aren't displaying native text fields, input is limited to what MOAITextBox can display. Support for complex text layout is something we want to handle down the road, but until then we're going to be limited.\nIf the keyboard (as written) doesn't meet your needs, if should be straightforward to adapt it into a native text field class that does. You'd need to change it from a singleton to a factory/instance type and add some API to position it on the screen, but otherwise most of the callbacks are already handled.",
        childs = {
            APPEARANCE_ALERT = {
                type = "value"
            },
            APPEARANCE_DEFAULT = {
                type = "value"
            },
            AUTOCAP_ALL = {
                type = "value"
            },
            AUTOCAP_NONE = {
                type = "value"
            },
            AUTOCAP_SENTENCES = {
                type = "value"
            },
            AUTOCAP_WORDS = {
                type = "value"
            },
            EVENT_INPUT = {
                type = "value"
            },
            EVENT_RETURN = {
                type = "value"
            },
            KEYBOARD_ASCII = {
                type = "value"
            },
            KEYBOARD_DECIMAL_PAD = {
                type = "value"
            },
            KEYBOARD_DEFAULT = {
                type = "value"
            },
            KEYBOARD_EMAIL = {
                type = "value"
            },
            KEYBOARD_NUM_PAD = {
                type = "value"
            },
            KEYBOARD_NUMERIC = {
                type = "value"
            },
            KEYBOARD_PHONE_PAD = {
                type = "value"
            },
            KEYBOARD_TWITTER = {
                type = "value"
            },
            KEYBOARD_URL = {
                type = "value"
            },
            RETURN_KEY_DEFAULT = {
                type = "value"
            },
            RETURN_KEY_DONE = {
                type = "value"
            },
            RETURN_KEY_GO = {
                type = "value"
            },
            RETURN_KEY_JOIN = {
                type = "value"
            },
            RETURN_KEY_NEXT = {
                type = "value"
            },
            RETURN_KEY_ROUTE = {
                type = "value"
            },
            RETURN_KEY_SEARCH = {
                type = "value"
            },
            RETURN_KEY_SEND = {
                type = "value"
            }
        }
    },
    MOAIKeyboardSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Hardware keyboard sensor.",
        childs = {
            keyDown = {
                type = "method",
                description = "Checks to see if one or more buttons were pressed during the last iteration.\n\nOverload:\n–> MOAIKeyboardSensor self\n–> string keyCodes: Keycode value(s) to be checked against the input table.\n<– boolean... wasPressed\n\nOverload:\n–> MOAIKeyboardSensor self\n–> number keyCode: Keycode value to be checked against the input table.\n<– boolean wasPressed",
                args = "(MOAIKeyboardSensor self, (string keyCodes | number keyCode))",
                returns = "(boolean... wasPressed | boolean wasPressed)"
            },
            keyIsDown = {
                type = "method",
                description = "Checks to see if the button is currently down.\n\nOverload:\n–> MOAIKeyboardSensor self\n–> string keyCodes: Keycode value(s) to be checked against the input table.\n<– boolean... isDown\n\nOverload:\n–> MOAIKeyboardSensor self\n–> number keyCode: Keycode value to be checked against the input table.\n<– boolean isDown",
                args = "(MOAIKeyboardSensor self, (string keyCodes | number keyCode))",
                returns = "(boolean... isDown | boolean isDown)"
            },
            keyIsUp = {
                type = "method",
                description = "Checks to see if the specified key is currently up.\n\nOverload:\n–> MOAIKeyboardSensor self\n–> string keyCodes: Keycode value(s) to be checked against the input table.\n<– boolean... isUp\n\nOverload:\n–> MOAIKeyboardSensor self\n–> number keyCode: Keycode value to be checked against the input table.\n<– boolean isUp",
                args = "(MOAIKeyboardSensor self, (string keyCodes | number keyCode))",
                returns = "(boolean... isUp | boolean isUp)"
            },
            keyUp = {
                type = "method",
                description = "Checks to see if the specified key was released during the last iteration.\n\nOverload:\n–> MOAIKeyboardSensor self\n–> string keyCodes: Keycode value(s) to be checked against the input table.\n<– boolean... wasReleased\n\nOverload:\n–> MOAIKeyboardSensor self\n–> number keyCode: Keycode value to be checked against the input table.\n<– boolean wasReleased",
                args = "(MOAIKeyboardSensor self, (string keyCodes | number keyCode))",
                returns = "(boolean... wasReleased | boolean wasReleased)"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when a key is pressed.\n\n–> MOAIKeyboardSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAIKeyboardSensor self, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAILayer = {
        type = "class",
        inherits = "MOAIProp MOAIClearableView",
        description = "Scene controls class.",
        childs = {
            SORT_ISO = {
                type = "value"
            },
            SORT_NONE = {
                type = "value"
            },
            SORT_PRIORITY_ASCENDING = {
                type = "value"
            },
            SORT_PRIORITY_DESCENDING = {
                type = "value"
            },
            SORT_VECTOR_ASCENDING = {
                type = "value"
            },
            SORT_VECTOR_DESCENDING = {
                type = "value"
            },
            SORT_X_ASCENDING = {
                type = "value"
            },
            SORT_X_DESCENDING = {
                type = "value"
            },
            SORT_Y_ASCENDING = {
                type = "value"
            },
            SORT_Y_DESCENDING = {
                type = "value"
            },
            SORT_Z_ASCENDING = {
                type = "value"
            },
            SORT_Z_DESCENDING = {
                type = "value"
            },
            clear = {
                type = "method",
                description = "Remove all props from the layer's partition.\n\n–> MOAILayer self\n<– nil",
                args = "MOAILayer self",
                returns = "nil"
            },
            getFitting = {
                type = "method",
                description = "Computes a camera fitting for a given world rect along with an optional screen space padding. To do a fitting, compute the world rect based on whatever you are fitting to, use this method to get the fitting, then animate the camera to match.\n\n–> MOAILayer self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n[–> number xPad]\n[–> number yPad]\n<– number x: X center of fitting (use for camera location).\n<– number y: Y center of fitting (use for camera location).\n<– number s: Scale of fitting (use for camera scale).",
                args = "(MOAILayer self, number xMin, number yMin, number xMax, number yMax, [number xPad, [number yPad]])",
                returns = "(number x, number y, number s)",
                valuetype = "number"
            },
            getPartition = {
                type = "method",
                description = "Returns the partition (if any) currently attached to this layer.\n\n–> MOAILayer self\n<– MOAIPartition partition",
                args = "MOAILayer self",
                returns = "MOAIPartition partition",
                valuetype = "MOAIPartition"
            },
            getSortMode = {
                type = "method",
                description = "Get the sort mode for rendering.\n\n–> MOAILayer self\n<– number sortMode",
                args = "MOAILayer self",
                returns = "number sortMode",
                valuetype = "number"
            },
            getSortScale = {
                type = "method",
                description = "Return the scalar applied to axis sorts.\n\n–> MOAILayer self\n<– number x\n<– number y\n<– number priority",
                args = "MOAILayer self",
                returns = "(number x, number y, number priority)",
                valuetype = "number"
            },
            insertProp = {
                type = "method",
                description = "Adds a prop to the layer's partition.\n\n–> MOAILayer self\n–> MOAIProp prop\n<– nil",
                args = "(MOAILayer self, MOAIProp prop)",
                returns = "nil"
            },
            removeProp = {
                type = "method",
                description = "Removes a prop from the layer's partition.\n\n–> MOAILayer self\n–> MOAIProp prop\n<– nil",
                args = "(MOAILayer self, MOAIProp prop)",
                returns = "nil"
            },
            setBox2DWorld = {
                type = "method",
                description = "Sets a Box2D world for debug drawing.\n\n–> MOAILayer self\n–> MOAIBox2DWorld world\n<– nil",
                args = "(MOAILayer self, MOAIBox2DWorld world)",
                returns = "nil"
            },
            setCamera = {
                type = "method",
                description = "Sets a camera for the layer. If no camera is supplied, layer will render using the identity matrix as view/proj.\n\nOverload:\n–> MOAILayer self\n[–> MOAICamera camera: Default value is nil.]\n<– nil\n\nOverload:\n–> MOAILayer self\n[–> MOAICamera2D camera: Default value is nil.]\n<– nil",
                args = "(MOAILayer self, [MOAICamera camera | MOAICamera2D camera])",
                returns = "nil"
            },
            setCpSpace = {
                type = "method",
                description = "Sets a Chipmunk space for debug drawing.\n\n–> MOAILayer self\n–> MOAICpSpace space\n<– nil",
                args = "(MOAILayer self, MOAICpSpace space)",
                returns = "nil"
            },
            setParallax = {
                type = "method",
                description = "Sets the parallax scale for this layer. This is simply a scalar applied to the view transform before rendering.\n\n–> MOAILayer self\n[–> number xParallax: Default value is 1.]\n[–> number yParallax: Default value is 1.]\n[–> number zParallax: Default value is 1.]\n<– nil",
                args = "(MOAILayer self, [number xParallax, [number yParallax, [number zParallax]]])",
                returns = "nil"
            },
            setPartition = {
                type = "method",
                description = "Sets a partition for the layer to use. The layer will automatically create a partition when the first prop is added if no partition has been set.\n\n–> MOAILayer self\n–> MOAIPartition partition\n<– nil",
                args = "(MOAILayer self, MOAIPartition partition)",
                returns = "nil"
            },
            setPartitionCull2D = {
                type = "method",
                description = "Enables 2D partition cull (projection of frustum AABB will be used instead of AABB or frustum).\n\n–> MOAILayer self\n–> boolean partitionCull2D: Default value is false.\n<– nil",
                args = "(MOAILayer self, boolean partitionCull2D)",
                returns = "nil"
            },
            setSortMode = {
                type = "method",
                description = "Set the sort mode for rendering.\n\n–> MOAILayer self\n–> number sortMode: One of MOAILayer.SORT_NONE, MOAILayer.SORT_PRIORITY_ASCENDING, MOAILayer.SORT_PRIORITY_DESCENDING, MOAILayer.SORT_X_ASCENDING, MOAILayer.SORT_X_DESCENDING, MOAILayer.SORT_Y_ASCENDING, MOAILayer.SORT_Y_DESCENDING, MOAILayer.SORT_Z_ASCENDING, MOAILayer.SORT_Z_DESCENDING\n<– nil",
                args = "(MOAILayer self, number sortMode)",
                returns = "nil"
            },
            setSortScale = {
                type = "method",
                description = "Set the scalar applied to axis sorts.\n\n–> MOAILayer self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> number z: Default value is 0.]\n[–> number priority: Default value is 1.]\n<– nil",
                args = "(MOAILayer self, [number x, [number y, [number z, [number priority]]]])",
                returns = "nil"
            },
            setViewport = {
                type = "method",
                description = "Set the layer's viewport.\n\n–> MOAILayer self\n–> MOAIViewport viewport\n<– nil",
                args = "(MOAILayer self, MOAIViewport viewport)",
                returns = "nil"
            },
            showDebugLines = {
                type = "method",
                description = "Display debug lines for props in this layer.\n\n–> MOAILayer self\n[–> boolean showDebugLines: Default value is 'true'.]\n<– nil",
                args = "(MOAILayer self, [boolean showDebugLines])",
                returns = "nil"
            },
            wndToWorld = {
                type = "method",
                description = "Project a point from window space into world space and return a normal vector representing a ray cast from the point into the world away from the camera (suitable for 3D picking).\n\n–> MOAILayer self\n–> number x\n–> number y\n–> number z\n<– number x\n<– number y\n<– number z\n<– number xn\n<– number yn\n<– number zn",
                args = "(MOAILayer self, number x, number y, number z)",
                returns = "(number x, number y, number z, number xn, number yn, number zn)",
                valuetype = "number"
            },
            worldToWnd = {
                type = "method",
                description = "Transform a point from world space to window space.\n\n–> MOAILayer self\n–> number x\n–> number y\n–> number Z\n<– number x\n<– number y\n<– number z",
                args = "(MOAILayer self, number x, number y, number Z)",
                returns = "(number x, number y, number z)",
                valuetype = "number"
            }
        }
    },
    MOAILayer2D = {
        type = "class",
        inherits = "MOAIProp2D",
        description = "2D layer.",
        childs = {
            SORT_NONE = {
                type = "value"
            },
            SORT_PRIORITY_ASCENDING = {
                type = "value"
            },
            SORT_PRIORITY_DESCENDING = {
                type = "value"
            },
            SORT_VECTOR_ASCENDING = {
                type = "value"
            },
            SORT_VECTOR_DESCENDING = {
                type = "value"
            },
            SORT_X_ASCENDING = {
                type = "value"
            },
            SORT_X_DESCENDING = {
                type = "value"
            },
            SORT_Y_ASCENDING = {
                type = "value"
            },
            SORT_Y_DESCENDING = {
                type = "value"
            },
            clear = {
                type = "method",
                description = "Remove all props from the layer's partition.\n\n–> MOAILayer2D self\n<– nil",
                args = "MOAILayer2D self",
                returns = "nil"
            },
            getFitting = {
                type = "method",
                description = "Computes a camera fitting for a given world rect along with an optional screen space padding. To do a fitting, compute the world rect based on whatever you are fitting to, use this method to get the fitting, then animate the camera to match.\n\n–> MOAILayer2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n[–> number xPad]\n[–> number yPad]\n<– number x: X center of fitting (use for camera location).\n<– number y: Y center of fitting (use for camera location).\n<– number s: Scale of fitting (use for camera scale).",
                args = "(MOAILayer2D self, number xMin, number yMin, number xMax, number yMax, [number xPad, [number yPad]])",
                returns = "(number x, number y, number s)",
                valuetype = "number"
            },
            getPartition = {
                type = "method",
                description = "Returns the partition (if any) currently attached to this layer.\n\n–> MOAILayer2D self\n<– MOAIPartition partition",
                args = "MOAILayer2D self",
                returns = "MOAIPartition partition",
                valuetype = "MOAIPartition"
            },
            getSortMode = {
                type = "method",
                description = "Get the sort mode for rendering.\n\n–> MOAILayer2D self\n<– number sortMode",
                args = "MOAILayer2D self",
                returns = "number sortMode",
                valuetype = "number"
            },
            getSortScale = {
                type = "method",
                description = "Return the scalar applied to axis sorts.\n\n–> MOAILayer2D self\n<– number x\n<– number y\n<– number priority",
                args = "MOAILayer2D self",
                returns = "(number x, number y, number priority)",
                valuetype = "number"
            },
            insertProp = {
                type = "method",
                description = "Adds a prop to the layer's partition.\n\n–> MOAILayer2D self\n–> MOAIProp prop\n<– nil",
                args = "(MOAILayer2D self, MOAIProp prop)",
                returns = "nil"
            },
            removeProp = {
                type = "method",
                description = "Removes a prop from the layer's partition.\n\n–> MOAILayer2D self\n–> MOAIProp prop\n<– nil",
                args = "(MOAILayer2D self, MOAIProp prop)",
                returns = "nil"
            },
            setBox2DWorld = {
                type = "method",
                description = "Sets a Box2D world for debug drawing.\n\n–> MOAILayer2D self\n–> MOAIBox2DWorld world\n<– nil",
                args = "(MOAILayer2D self, MOAIBox2DWorld world)",
                returns = "nil"
            },
            setCamera = {
                type = "method",
                description = "Sets a camera for the layer. If no camera is supplied, layer will render using the identity matrix as view/proj.\n\nOverload:\n–> MOAILayer2D self\n[–> MOAICamera2D camera: Default value is nil.]\n<– nil\n\nOverload:\n–> MOAILayer2D self\n[–> MOAICamera camera: Default value is nil.]\n<– nil",
                args = "(MOAILayer2D self, [MOAICamera2D camera | MOAICamera camera])",
                returns = "nil"
            },
            setCpSpace = {
                type = "method",
                description = "Sets a Chipmunk space for debug drawing.\n\n–> MOAILayer2D self\n–> MOAICpSpace space\n<– nil",
                args = "(MOAILayer2D self, MOAICpSpace space)",
                returns = "nil"
            },
            setFrameBuffer = {
                type = "method",
                description = "Attach a frame buffer. Layer will render to frame buffer instead of the main view.\n\n–> MOAILayer2D self\n–> MOAIFrameBufferTexture frameBuffer\n<– nil",
                args = "(MOAILayer2D self, MOAIFrameBufferTexture frameBuffer)",
                returns = "nil"
            },
            setParallax = {
                type = "method",
                description = "Sets the parallax scale for this layer. This is simply a scalar applied to the view transform before rendering.\n\n–> MOAILayer2D self\n–> number xParallax: Default value is 1.\n–> number yParallax: Default value is 1.\n<– nil",
                args = "(MOAILayer2D self, number xParallax, number yParallax)",
                returns = "nil"
            },
            setPartition = {
                type = "method",
                description = "Sets a partition for the layer to use. The layer will automatically create a partition when the first prop is added if no partition has been set.\n\n–> MOAILayer2D self\n–> MOAIPartition partition\n<– nil",
                args = "(MOAILayer2D self, MOAIPartition partition)",
                returns = "nil"
            },
            setSortMode = {
                type = "method",
                description = "Set the sort mode for rendering.\n\n–> MOAILayer2D self\n–> number sortMode: One of MOAILayer2D.SORT_NONE, MOAILayer2D.SORT_PRIORITY_ASCENDING, MOAILayer2D.SORT_PRIORITY_DESCENDING, MOAILayer2D.SORT_X_ASCENDING, MOAILayer2D.SORT_X_DESCENDING, MOAILayer2D.SORT_Y_ASCENDING, MOAILayer2D.SORT_Y_DESCENDING\n<– nil",
                args = "(MOAILayer2D self, number sortMode)",
                returns = "nil"
            },
            setSortScale = {
                type = "method",
                description = "Set the scalar applied to axis sorts.\n\n–> MOAILayer2D self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> number priority: Default value is 1.]\n<– nil",
                args = "(MOAILayer2D self, [number x, [number y, [number priority]]])",
                returns = "nil"
            },
            setViewport = {
                type = "method",
                description = "Set the layer's viewport.\n\n–> MOAILayer2D self\n–> MOAIViewport viewport\n<– nil",
                args = "(MOAILayer2D self, MOAIViewport viewport)",
                returns = "nil"
            },
            showDebugLines = {
                type = "method",
                description = "Display debug lines for props in this layer.\n\n–> MOAILayer2D self\n[–> boolean showDebugLines: Default value is 'true'.]\n<– nil",
                args = "(MOAILayer2D self, [boolean showDebugLines])",
                returns = "nil"
            },
            wndToWorld = {
                type = "method",
                description = "Project a point from window space into world space.\n\n–> MOAILayer2D self\n–> number x\n–> number y\n<– number x\n<– number y",
                args = "(MOAILayer2D self, number x, number y)",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            worldToWnd = {
                type = "method",
                description = "Transform a point from world space to window space.\n\n–> MOAILayer2D self\n–> number x\n–> number y\n<– number x\n<– number y",
                args = "(MOAILayer2D self, number x, number y)",
                returns = "(number x, number y)",
                valuetype = "number"
            }
        }
    },
    MOAILayerBridge = {
        type = "class",
        inherits = "MOAITransform",
        description = "2D transform for connecting transforms across scenes. Useful for HUD overlay items and map pins.",
        childs = {
            init = {
                type = "method",
                description = "Initialize the bridge transform (map coordinates in one layer onto another; useful for rendering screen space objects tied to world space coordinates - map pins, for example).\n\n–> MOAILayerBridge self\n–> MOAITransformBase sourceTransform\n–> MOAILayer sourceLayer\n–> MOAILayer destLayer\n<– nil",
                args = "(MOAILayerBridge self, MOAITransformBase sourceTransform, MOAILayer sourceLayer, MOAILayer destLayer)",
                returns = "nil"
            }
        }
    },
    MOAILocationSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Location services sensor.",
        childs = {
            getLocation = {
                type = "method",
                description = "Returns the current information about the physical location.\n\n–> MOAILocationSensor self\n<– number longitude\n<– number latitude\n<– number haccuracy: The horizontal (long/lat) accuracy.\n<– number altitude\n<– number vaccuracy: The vertical (altitude) accuracy.\n<– number speed",
                args = "MOAILocationSensor self",
                returns = "(number longitude, number latitude, number haccuracy, number altitude, number vaccuracy, number speed)",
                valuetype = "number"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when the location changes.\n\n–> MOAILocationSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAILocationSensor self, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAILogMgr = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Singleton for managing debug log messages and log level.",
        childs = {
            LOG_ERROR = {
                type = "value"
            },
            LOG_NONE = {
                type = "value"
            },
            LOG_STATUS = {
                type = "value"
            },
            LOG_WARNING = {
                type = "value"
            },
            closeFile = {
                type = "function",
                description = "Resets log output to stdout.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            isDebugBuild = {
                type = "function",
                description = "Returns a boolean value indicating whether Moai has been compiles as a debug build or not.\n\n<– boolean isDebugBuild",
                args = "()",
                returns = "boolean isDebugBuild",
                valuetype = "boolean"
            },
            log = {
                type = "function",
                description = "Alias for print.\n\n–> string message\n<– nil",
                args = "string message",
                returns = "nil"
            },
            openFile = {
                type = "function",
                description = "Opens a new file to receive log messages.\n\n–> string filename\n<– nil",
                args = "string filename",
                returns = "nil"
            },
            registerLogMessage = {
                type = "function",
                description = "Register a format string to handle a log message. Register an empty string to hide messages.\n\n–> number messageID\n[–> string formatString: Default value is an empty string.]\n[–> number level: One of MOAILogMgr.LOG_ERROR, MOAILogMgr.LOG_WARNING, MOAILogMgr.LOG_STATUS. Default value is MOAILogMgr.LOG_STATUS.]\n<– nil",
                args = "(number messageID, [string formatString, [number level]])",
                returns = "nil"
            },
            setLogLevel = {
                type = "function",
                description = "Set the logging level.\n\n–> number logLevel: One of MOAILogMgr LOG_NONE, LOG_ERROR, LOG_WARNING, LOG_STATUS\n<– nil",
                args = "number logLevel",
                returns = "nil"
            },
            setTypeCheckLuaParams = {
                type = "function",
                description = "Set or clear type checking of parameters passed to Lua bound Moai API functions.\n\n[–> boolean check: Default value is false.]\n<– nil",
                args = "[boolean check]",
                returns = "nil"
            }
        }
    },
    MOAILuaObject = {
        type = "class",
        description = "Base class for all of Moai's Lua classes.",
        childs = {
            getClass = {
                type = "method",
                description = "",
                args = "()",
                returns = "()"
            },
            getClassName = {
                type = "method",
                description = "Return the class name for the current object.\n\n–> MOAILuaObject self\n<– string",
                args = "MOAILuaObject self",
                returns = "string",
                valuetype = "string"
            },
            setInterface = {
                type = "method",
                description = "",
                args = "()",
                returns = "()"
            }
        }
    },
    MOAIMemStream = {
        type = "class",
        inherits = "MOAIStream",
        description = "MOAIMemStream implements an in-memory stream and grows as needed. The memory stream expands on demands by allocating additional 'chunks' or memory. The chunk size may be configured by the user. Note that the chunks are not guaranteed to be contiguous in memory.",
        childs = {
            close = {
                type = "method",
                description = "Close the memory stream and release its buffers.\n\n–> MOAIMemStream self\n<– nil",
                args = "MOAIMemStream self",
                returns = "nil"
            },
            open = {
                type = "method",
                description = "Create a memory stream and optionally reserve some memory and set the chunk size by which the stream will grow if additional memory is needed.\n\n–> MOAIMemStream self\n[–> number reserve: Default value is 0.]\n[–> number chunkSize: Default value is MOAIMemStream.DEFAULT_CHUNK_SIZE (2048 bytes).]\n<– boolean success",
                args = "(MOAIMemStream self, [number reserve, [number chunkSize]])",
                returns = "boolean success",
                valuetype = "boolean"
            }
        }
    },
    MOAIMesh = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Loads a texture and renders the contents of a vertex buffer. Grid drawing not supported.",
        childs = {
            GL_LINE_LOOP = {
                type = "value"
            },
            GL_LINE_STRIP = {
                type = "value"
            },
            GL_LINES = {
                type = "value"
            },
            GL_POINTS = {
                type = "value"
            },
            GL_TRIANGLE_FAN = {
                type = "value"
            },
            GL_TRIANGLE_STRIP = {
                type = "value"
            },
            GL_TRIANGLES = {
                type = "value"
            },
            setIndexBuffer = {
                type = "method",
                description = "Set the index buffer to render.\n\n–> MOAIMesh self\n–> MOAIIndexBuffer indexBuffer\n<– nil",
                args = "(MOAIMesh self, MOAIIndexBuffer indexBuffer)",
                returns = "nil"
            },
            setPenWidth = {
                type = "method",
                description = "Sets the pen with for drawing prims in this vertex buffer. Only valid with prim types GL_LINES, GL_LINE_LOOP, GL_LINE_STRIP.\n\n–> MOAIMesh self\n–> number penWidth\n<– nil",
                args = "(MOAIMesh self, number penWidth)",
                returns = "nil"
            },
            setPointSize = {
                type = "method",
                description = "Sets the point size for drawing prims in this vertex buffer. Only valid with prim types GL_POINTS.\n\n–> MOAIMesh self\n–> number pointSize\n<– nil",
                args = "(MOAIMesh self, number pointSize)",
                returns = "nil"
            },
            setPrimType = {
                type = "method",
                description = "Sets the prim type the buffer represents.\n\n–> MOAIMesh self\n–> number primType: One of MOAIMesh GL_POINTS, GL_LINES, GL_TRIANGLES, GL_LINE_LOOP, GL_LINE_STRIP, GL_TRIANGLE_FAN, GL_TRIANGLE_STRIP\n<– nil",
                args = "(MOAIMesh self, number primType)",
                returns = "nil"
            },
            setVertexBuffer = {
                type = "method",
                description = "Set the vertex buffer to render.\n\n–> MOAIMesh self\n–> MOAIVertexBuffer vertexBuffer\n<– nil",
                args = "(MOAIMesh self, MOAIVertexBuffer vertexBuffer)",
                returns = "nil"
            }
        }
    },
    MOAIMobileAppTrackerIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for WebAppTracker interface.",
        childs = {}
    },
    MOAIMotionSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Gravity/acceleration sensor.",
        childs = {
            getLevel = {
                type = "method",
                description = "Polls the current status of the level sensor.\n\n–> MOAIMotionSensor self\n<– number x\n<– number y\n<– number z",
                args = "MOAIMotionSensor self",
                returns = "(number x, number y, number z)",
                valuetype = "number"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when the level changes.\n\n–> MOAIMotionSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAIMotionSensor self, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAIMoviePlayerAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for simple video playback. Exposed to Lua via MOAIMoviePlayer on all mobile platforms.",
        childs = {
            init = {
                type = "function",
                description = "Initialize the video player with the URL of a video to play.\n\n–> string url: The URL of the video to play.\n<– nil",
                args = "string url",
                returns = "nil"
            },
            pause = {
                type = "function",
                description = "Pause video playback.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            play = {
                type = "function",
                description = "Play the video as soon as playback is ready.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            stop = {
                type = "function",
                description = "Stop video playback and reset the video player.\n\n<– nil",
                args = "()",
                returns = "nil"
            }
        }
    },
    MOAIMoviePlayerIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for simple video playback. Exposed to Lua via MOAIMoviePlayer on all mobile platforms.",
        childs = {}
    },
    MOAIMultiTexture = {
        type = "class",
        inherits = "MOAILuaObject MOAIGfxState",
        description = "Array of textures for multi-texturing.",
        childs = {
            reserve = {
                type = "method",
                description = "Reserve or clears indices for textures.\n\n–> MOAIMultiTexture self\n[–> number total: Default value is 0.]\n<– nil",
                args = "(MOAIMultiTexture self, [number total])",
                returns = "nil"
            },
            setTexture = {
                type = "method",
                description = "Sets of clears a texture for the given index.\n\n–> MOAIMultiTexture self\n–> number index\n[–> MOAITextureBase texture: Default value is nil.]\n<– nil",
                args = "(MOAIMultiTexture self, number index, [MOAITextureBase texture])",
                returns = "nil"
            }
        }
    },
    MOAINode = {
        type = "class",
        inherits = "MOAIInstanceEventSource",
        description = "Base for all attribute bearing Moai objects and dependency graph nodes.",
        childs = {
            clearAttrLink = {
                type = "method",
                description = "Clears an attribute *pull* link - call this from the node receiving the attribute value.\n\n–> MOAINode self\n–> number attrID\n<– nil",
                args = "(MOAINode self, number attrID)",
                returns = "nil"
            },
            clearNodeLink = {
                type = "method",
                description = "Clears a dependency on a foreign node.\n\n–> MOAINode self\n–> MOAINode sourceNode\n<– nil",
                args = "(MOAINode self, MOAINode sourceNode)",
                returns = "nil"
            },
            forceUpdate = {
                type = "method",
                description = "Evaluates the dependency graph for this node. Typically, the entire active dependency graph is evaluated once per frame, but in some cases it may be desirable to force evaluation of a node to make sure source dependencies are propagated to it immediately.\n\n–> MOAINode self\n<– nil",
                args = "MOAINode self",
                returns = "nil"
            },
            getAttr = {
                type = "method",
                description = "Returns the value of the attribute if it exists or nil if it doesn't.\n\n–> MOAINode self\n–> number attrID\n<– number value",
                args = "(MOAINode self, number attrID)",
                returns = "number value",
                valuetype = "number"
            },
            getAttrLink = {
                type = "method",
                description = "Returns the link if it exists or nil if it doesn't.\n\n–> MOAINode self\n–> number attrID\n<– MOAINode sourceNode\n<– number sourceAttrID",
                args = "(MOAINode self, number attrID)",
                returns = "(MOAINode sourceNode, number sourceAttrID)",
                valuetype = "MOAINode"
            },
            moveAttr = {
                type = "method",
                description = "Animate the attribute by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAINode self\n–> number attrID: ID of the attribute to animate.\n–> number delta: Total change to be added to attribute.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAINode self, number attrID, number delta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            scheduleUpdate = {
                type = "method",
                description = "Schedule the node for an update next time the dependency graph is processed. Any dependent nodes will also be updated.\n\n–> MOAINode self\n<– nil",
                args = "MOAINode self",
                returns = "nil"
            },
            seekAttr = {
                type = "method",
                description = "Animate the attribute by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAINode self\n–> number attrID: ID of the attribute to animate.\n–> number goal: Desired resulting value for attribute.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAINode self, number attrID, number goal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            setAttr = {
                type = "method",
                description = "Sets the value of an attribute.\n\n–> MOAINode self\n–> number attrID\n–> number value\n<– nil",
                args = "(MOAINode self, number attrID, number value)",
                returns = "nil"
            },
            setAttrLink = {
                type = "method",
                description = "Sets a *pull* attribute connecting an attribute in the node to an attribute in a foreign node.\n\n–> MOAINode self\n–> number attrID: ID of attribute to become dependent of foreign node.\n–> MOAINode sourceNode: Foreign node.\n[–> number sourceAttrID: Attribute in foreign node to control value of attribue. Default value is attrID.]\n<– nil",
                args = "(MOAINode self, number attrID, MOAINode sourceNode, [number sourceAttrID])",
                returns = "nil"
            },
            setNodeLink = {
                type = "method",
                description = "Creates a dependency between the node and a foreign node without the use of attributes; if the foreign node is updated, the dependent node will be updated after.\n\n–> MOAINode self\n–> MOAINode sourceNode\n<– nil",
                args = "(MOAINode self, MOAINode sourceNode)",
                returns = "nil"
            }
        }
    },
    MOAINotificationsAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for push notification integration on Android devices. Exposed to Lua via MOAINotifications on all mobile platforms.",
        childs = {
            LOCAL_NOTIFICATION_MESSAGE_RECEIVED = {
                type = "value",
                description = "Event code for a local notification message receipt."
            },
            REMOTE_NOTIFICATION_ALERT = {
                type = "value",
                description = "Notification type alerts. Unused."
            },
            REMOTE_NOTIFICATION_BADGE = {
                type = "value",
                description = "Notification type icon badges. Unused."
            },
            REMOTE_NOTIFICATION_MESSAGE_RECEIVED = {
                type = "value",
                description = "Event code for a push notification message receipt."
            },
            REMOTE_NOTIFICATION_NONE = {
                type = "value",
                description = "Notification type none. Unused."
            },
            REMOTE_NOTIFICATION_REGISTRATION_COMPLETE = {
                type = "value",
                description = "Event code for notification registration completion."
            },
            REMOTE_NOTIFICATION_RESULT_ERROR = {
                type = "value",
                description = "Error code for a failed notification registration or deregistration."
            },
            REMOTE_NOTIFICATION_RESULT_REGISTERED = {
                type = "value",
                description = "Error code for a successful notification registration."
            },
            REMOTE_NOTIFICATION_RESULT_UNREGISTERED = {
                type = "value",
                description = "Error code for a successful notification deregistration."
            },
            REMOTE_NOTIFICATION_SOUND = {
                type = "value",
                description = "Notification type sound. Unused."
            },
            getAppIconBadgeNumber = {
                type = "function",
                description = "Get the current icon badge number. Always returns zero.\n\n<– number count",
                args = "()",
                returns = "number count",
                valuetype = "number"
            },
            localNotificationInSeconds = {
                type = "function",
                description = "Schedules a local notification to show a number of seconds after calling.\n\n–> string message\n–> number seconds\n<– nil",
                args = "(string message, number seconds)",
                returns = "nil"
            },
            registerForRemoteNotifications = {
                type = "function",
                description = "Register to receive remote notifications.\n\n–> string sender: The identity of the entity that will send remote notifications. See Google documentation.\n<– nil",
                args = "string sender",
                returns = "nil"
            },
            setAppIconBadgeNumber = {
                type = "function",
                description = "Get the current icon badge number. Does nothing.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            unregisterForRemoteNotifications = {
                type = "function",
                description = "Unregister for remote notifications.\n\n<– nil",
                args = "()",
                returns = "nil"
            }
        }
    },
    MOAINotificationsIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for push notification integration on iOS devices. Exposed to Lua via MOAINotifications on all mobile platforms.",
        childs = {
            REMOTE_NOTIFICATION_ALERT = {
                type = "value",
                description = "Notification type alerts."
            },
            REMOTE_NOTIFICATION_BADGE = {
                type = "value",
                description = "Notification type icon badges."
            },
            REMOTE_NOTIFICATION_MESSAGE_RECEIVED = {
                type = "value",
                description = "Event code for a push notification message receipt."
            },
            REMOTE_NOTIFICATION_NONE = {
                type = "value",
                description = "Notification type none."
            },
            REMOTE_NOTIFICATION_REGISTRATION_COMPLETE = {
                type = "value",
                description = "Event code for notification registration completion."
            },
            REMOTE_NOTIFICATION_RESULT_ERROR = {
                type = "value",
                description = "Error code for a failed notification registration or deregistration."
            },
            REMOTE_NOTIFICATION_RESULT_REGISTERED = {
                type = "value",
                description = "Error code for a successful notification registration."
            },
            REMOTE_NOTIFICATION_RESULT_UNREGISTERED = {
                type = "value",
                description = "Error code for a successful notification deregistration."
            },
            REMOTE_NOTIFICATION_SOUND = {
                type = "value",
                description = "Notification type sound."
            }
        }
    },
    MOAIParser = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Parses strings using a LALR parser. Generates an abstract syntax tree that may then be traversed in Lua. To use, load a CGT file generated by GOLD Parser Builder. Thanks to Devin Cook: http://www.devincook.com/goldparser",
        childs = {
            loadFile = {
                type = "method",
                description = "Parses the contents of a file and builds an abstract syntax tree.\n\n–> MOAIParser self\n–> string filename\n<– table ast",
                args = "(MOAIParser self, string filename)",
                returns = "table ast",
                valuetype = "table"
            },
            loadRules = {
                type = "method",
                description = "Parses the contents of the specified CGT.\n\n–> MOAIParser self\n–> string filename: The path of the file to read the CGT data from.\n<– nil",
                args = "(MOAIParser self, string filename)",
                returns = "nil"
            },
            loadString = {
                type = "method",
                description = "Parses the contents of a string and builds an abstract syntax tree.\n\n–> MOAIParser self\n–> string filename\n<– table ast",
                args = "(MOAIParser self, string filename)",
                returns = "table ast",
                valuetype = "table"
            },
            setCallbacks = {
                type = "method",
                description = "Set Lua syntax tree node handlers for tree traversal.\n\n–> MOAIParser self\n[–> function onStartNonterminal: Default value is nil.]\n[–> function onEndNonterminal: Default value is nil.]\n[–> function onTerminal: Default value is nil.]\n<– nil",
                args = "(MOAIParser self, [function onStartNonterminal, [function onEndNonterminal, [function onTerminal]]])",
                returns = "nil"
            },
            traverse = {
                type = "method",
                description = "Top-down traversal of the abstract syntax tree.\n\n–> MOAIParser self\n<– nil",
                args = "MOAIParser self",
                returns = "nil"
            }
        }
    },
    MOAIParticleCallbackPlugin = {
        type = "class",
        inherits = "MOAIParticlePlugin",
        description = "Allows custom particle processing via C language callbacks.",
        childs = {}
    },
    MOAIParticleDistanceEmitter = {
        type = "class",
        inherits = "MOAIParticleEmitter",
        description = "Particle emitter.",
        childs = {
            reset = {
                type = "method",
                description = "Resets the distance traveled. Use this to avoid large emissions when 'warping' the emitter to a new location.\n\n–> MOAIParticleDistanceEmitter self\n<– nil",
                args = "MOAIParticleDistanceEmitter self",
                returns = "nil"
            },
            setDistance = {
                type = "method",
                description = "Set the travel distance required for new particle emission.\n\n–> MOAIParticleDistanceEmitter self\n–> number min: Minimum distance.\n[–> number max: Maximum distance. Default value is min.]\n<– nil",
                args = "(MOAIParticleDistanceEmitter self, number min, [number max])",
                returns = "nil"
            }
        }
    },
    MOAIParticleEmitter = {
        type = "class",
        inherits = "MOAITransform MOAIAction",
        description = "Particle emitter.",
        childs = {
            setAngle = {
                type = "method",
                description = "Set the size and angle of the emitter.\n\n–> MOAIParticleEmitter self\n–> number min: Minimum angle in degrees.\n–> number max: Maximum angle in degrees.\n<– nil",
                args = "(MOAIParticleEmitter self, number min, number max)",
                returns = "nil"
            },
            setEmission = {
                type = "method",
                description = "Set the size of each emission.\n\n–> MOAIParticleEmitter self\n–> number min: Minimum emission size.\n[–> number max: Maximum emission size. Defaults to min.]\n<– nil",
                args = "(MOAIParticleEmitter self, number min, [number max])",
                returns = "nil"
            },
            setMagnitude = {
                type = "method",
                description = "Set the starting magnitude of particles deltas.\n\n–> MOAIParticleEmitter self\n–> number min: Minimum magnitude.\n[–> number max: Maximum magnitude. Defaults to min.]\n<– nil",
                args = "(MOAIParticleEmitter self, number min, [number max])",
                returns = "nil"
            },
            setRadius = {
                type = "method",
                description = "Set the shape and radius of the emitter.\n\nOverload:\n–> MOAIParticleEmitter self\n–> number radius\n<– nil\n\nOverload:\n–> MOAIParticleEmitter self\n–> number innerRadius\n–> number outerRadius\n<– nil",
                args = "(MOAIParticleEmitter self, (number radius | (number innerRadius, number outerRadius)))",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set the shape and dimensions of the emitter.\n\n–> MOAIParticleEmitter self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIParticleEmitter self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            setSystem = {
                type = "method",
                description = "Attaches the emitter to a particle system.\n\n–> MOAIParticleEmitter self\n–> MOAIParticleSystem system\n<– nil",
                args = "(MOAIParticleEmitter self, MOAIParticleSystem system)",
                returns = "nil"
            },
            surge = {
                type = "method",
                description = "Forces the emission of one or more particles.\n\n–> MOAIParticleEmitter self\n[–> number total: Size of surge. Default value is a random emission value for emitter.]\n<– nil",
                args = "(MOAIParticleEmitter self, [number total])",
                returns = "nil"
            }
        }
    },
    MOAIParticleForce = {
        type = "class",
        inherits = "MOAITransform",
        description = "Particle force.",
        childs = {
            FORCE = {
                type = "value"
            },
            GRAVITY = {
                type = "value"
            },
            OFFSET = {
                type = "value"
            },
            initAttractor = {
                type = "method",
                description = "Greater force is exerted on particles as they approach attractor.\n\n–> MOAIParticleForce self\n–> number radius: Size of the attractor.\n[–> number magnitude: Strength of the attractor.]\n<– nil",
                args = "(MOAIParticleForce self, number radius, [number magnitude])",
                returns = "nil"
            },
            initBasin = {
                type = "method",
                description = "Greater force is exerted on particles as they leave attractor.\n\n–> MOAIParticleForce self\n–> number radius: Size of the attractor.\n[–> number magnitude: Strength of the attractor.]\n<– nil",
                args = "(MOAIParticleForce self, number radius, [number magnitude])",
                returns = "nil"
            },
            initLinear = {
                type = "method",
                description = "A constant linear force will be applied to the particles.\n\n–> MOAIParticleForce self\n–> number x\n[–> number y]\n<– nil",
                args = "(MOAIParticleForce self, number x, [number y])",
                returns = "nil"
            },
            initRadial = {
                type = "method",
                description = "A constant radial force will be applied to the particles.\n\n–> MOAIParticleForce self\n–> number magnitude\n<– nil",
                args = "(MOAIParticleForce self, number magnitude)",
                returns = "nil"
            },
            setType = {
                type = "method",
                description = "Set the type of force. FORCE will factor in the particle's mass. GRAVITY will ignore the particle's mass. OFFSET will ignore both mass and damping.\n\n–> MOAIParticleForce self\n–> number type: One of MOAIParticleForce.FORCE, MOAIParticleForce.GRAVITY, MOAIParticleForce.OFFSET\n<– nil",
                args = "(MOAIParticleForce self, number type)",
                returns = "nil"
            }
        }
    },
    MOAIParticlePexPlugin = {
        type = "class",
        inherits = "MOAIParticlePlugin",
        description = "Allows custom particle processing derived from .pex file via C language callback.",
        childs = {
            getTextureName = {
                type = "method",
                description = "Return the texture name associated with plugin.\n\n–> MOAIParticlePexPlugin self\n<– string textureName",
                args = "MOAIParticlePexPlugin self",
                returns = "string textureName",
                valuetype = "string"
            },
            load = {
                type = "function",
                description = "Create a particle plugin from an XML file\n\n–> string fileName: file to load\n<– MOAIParticlePexPlugin plugin: The plugin object that has been initialized with XML's data",
                args = "string fileName",
                returns = "MOAIParticlePexPlugin plugin",
                valuetype = "MOAIParticlePexPlugin"
            }
        }
    },
    MOAIParticlePlugin = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Allows custom particle processing.",
        childs = {
            getSize = {
                type = "method",
                description = "Return the particle size expected by the plugin.\n\n–> MOAIParticlePlugin self\n<– number size",
                args = "MOAIParticlePlugin self",
                returns = "number size",
                valuetype = "number"
            }
        }
    },
    MOAIParticleScript = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Particle script.",
        childs = {
            PARTICLE_DX = {
                type = "value"
            },
            PARTICLE_DY = {
                type = "value"
            },
            PARTICLE_X = {
                type = "value"
            },
            PARTICLE_Y = {
                type = "value"
            },
            SPRITE_BLUE = {
                type = "value"
            },
            SPRITE_GLOW = {
                type = "value"
            },
            SPRITE_GREEN = {
                type = "value"
            },
            SPRITE_IDX = {
                type = "value"
            },
            SPRITE_OPACITY = {
                type = "value"
            },
            SPRITE_RED = {
                type = "value"
            },
            SPRITE_ROT = {
                type = "value"
            },
            SPRITE_X_LOC = {
                type = "value"
            },
            SPRITE_X_SCL = {
                type = "value"
            },
            SPRITE_Y_LOC = {
                type = "value"
            },
            SPRITE_Y_SCL = {
                type = "value"
            },
            add = {
                type = "method",
                description = "r0 = v0 + v1\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n–> number v1\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1)",
                returns = "nil"
            },
            angleVec = {
                type = "method",
                description = "Load two registers with the X and Y components of a unit vector with a given angle.\n\n–> MOAIParticleScript self\n–> number r0: Register to store result X.\n–> number r1: Register to store result Y.\n–> number v0: Angle of vector (in degrees).\n<– nil",
                args = "(MOAIParticleScript self, number r0, number r1, number v0)",
                returns = "nil"
            },
            cos = {
                type = "method",
                description = "r0 = cos(v0)\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0)",
                returns = "nil"
            },
            cycle = {
                type = "method",
                description = "Cycle v0 between v1 and v2.\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n–> number v1\n–> number v2\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1, number v2)",
                returns = "nil"
            },
            div = {
                type = "method",
                description = "r0 = v0 / v1\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n–> number v1\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1)",
                returns = "nil"
            },
            ease = {
                type = "method",
                description = "Load a register with a value interpolated between two numbers using an ease curve.\n\n–> MOAIParticleScript self\n–> number r0: Register to store result.\n–> number v0: Starting value of the ease.\n–> number v1: Ending value of the ease.\n–> number easeType: See MOAIEaseType for a list of ease types.\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1, number easeType)",
                returns = "nil"
            },
            easeDelta = {
                type = "method",
                description = "Load a register with a value interpolated between two numbers using an ease curve. Apply as a delta.\n\n–> MOAIParticleScript self\n–> number r0: Register to store result.\n–> number v0: Starting value of the ease.\n–> number v1: Ending value of the ease.\n–> number easeType: See MOAIEaseType for a list of ease types.\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1, number easeType)",
                returns = "nil"
            },
            mul = {
                type = "method",
                description = "r0 = v0 * v1\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n–> number v1\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1)",
                returns = "nil"
            },
            norm = {
                type = "method",
                description = "r0 = v0 / |v|\nr1 = v1 / |v|\nWhere |v| == sqrt( v0^2 + v1^2)\n\n–> MOAIParticleScript self\n–> number r0\n–> number r1\n–> number v0\n–> number v1\n<– nil",
                args = "(MOAIParticleScript self, number r0, number r1, number v0, number v1)",
                returns = "nil"
            },
            packConst = {
                type = "function",
                description = "Pack a const value into a particle script param.\n\n–> number const: Const value to pack.\n<– number packed: The packed value.",
                args = "number const",
                returns = "number packed",
                valuetype = "number"
            },
            packReg = {
                type = "function",
                description = "Pack a register index into a particle script param.\n\n–> number regIdx: Register index to pack.\n<– number packed: The packed value.",
                args = "number regIdx",
                returns = "number packed",
                valuetype = "number"
            },
            rand = {
                type = "method",
                description = "Load a register with a random number from a range.\n\n–> MOAIParticleScript self\n–> number r0: Register to store result.\n–> number v0: Range minimum.\n–> number v1: Range maximum.\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1)",
                returns = "nil"
            },
            randVec = {
                type = "method",
                description = "Load two registers with the X and Y components of a vector with randomly chosen direction and length.\n\n–> MOAIParticleScript self\n–> number r0: Register to store result X.\n–> number r1: Register to store result Y.\n–> number v0: Minimum length of vector.\n–> number v1: Maximum length of vector.\n<– nil",
                args = "(MOAIParticleScript self, number r0, number r1, number v0, number v1)",
                returns = "nil"
            },
            set = {
                type = "method",
                description = "Load a value into a register.\n\n–> MOAIParticleScript self\n–> number r0: Register to store result.\n–> number v0: Value to load.\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0)",
                returns = "nil"
            },
            sin = {
                type = "method",
                description = "r0 = sin(v0)\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0)",
                returns = "nil"
            },
            sprite = {
                type = "method",
                description = "Push a new sprite for rendering. To render a particle, first call 'sprite' to create a new sprite at the particle's location. Then modify the sprite's registers to create animated effects based on the age of the particle (normalized to its term).\n\n–> MOAIParticleScript self\n<– nil",
                args = "MOAIParticleScript self",
                returns = "nil"
            },
            sub = {
                type = "method",
                description = "r0 = v0 - v1\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n–> number v1\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1)",
                returns = "nil"
            },
            tan = {
                type = "method",
                description = "r0 = tan(v0)\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0)",
                returns = "nil"
            },
            time = {
                type = "method",
                description = "Load the normalized age of the particle into a register.\n\n–> MOAIParticleScript self\n–> number r0\n<– nil",
                args = "(MOAIParticleScript self, number r0)",
                returns = "nil"
            },
            vecAngle = {
                type = "method",
                description = "Compute angle (in degrees) between v0 and v1.\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n–> number v1\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1)",
                returns = "nil"
            },
            wrap = {
                type = "method",
                description = "Wrap v0 between v1 and v2.\n\n–> MOAIParticleScript self\n–> number r0\n–> number v0\n–> number v1\n–> number v2\n<– nil",
                args = "(MOAIParticleScript self, number r0, number v0, number v1, number v2)",
                returns = "nil"
            }
        }
    },
    MOAIParticleState = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Particle state.",
        childs = {
            clearForces = {
                type = "method",
                description = "Removes all particle forces from the state.\n\n–> MOAIParticleState self\n<– nil",
                args = "MOAIParticleState self",
                returns = "nil"
            },
            pushForce = {
                type = "method",
                description = "Adds a force to the state.\n\n–> MOAIParticleState self\n–> MOAIParticleForce force\n<– nil",
                args = "(MOAIParticleState self, MOAIParticleForce force)",
                returns = "nil"
            },
            setDamping = {
                type = "method",
                description = "Sets damping for particle physics model.\n\n–> MOAIParticleState self\n–> number damping\n<– nil",
                args = "(MOAIParticleState self, number damping)",
                returns = "nil"
            },
            setInitScript = {
                type = "method",
                description = "Sets the particle script to use for initializing new particles.\n\n–> MOAIParticleState self\n[–> MOAIParticleScript script]\n<– nil",
                args = "(MOAIParticleState self, [MOAIParticleScript script])",
                returns = "nil"
            },
            setMass = {
                type = "method",
                description = "Sets range of masses (chosen randomly) for particles initialized by the state.\n\n–> MOAIParticleState self\n–> number minMass\n[–> number maxMass: Default value is minMass.]\n<– nil",
                args = "(MOAIParticleState self, number minMass, [number maxMass])",
                returns = "nil"
            },
            setNext = {
                type = "method",
                description = "Sets the next state (if any).\n\n–> MOAIParticleState self\n[–> MOAIParticleState next: Default value is nil.]\n<– nil",
                args = "(MOAIParticleState self, [MOAIParticleState next])",
                returns = "nil"
            },
            setPlugin = {
                type = "method",
                description = "Sets the particle plugin to use for initializing and updating particles.\n\n–> MOAIParticleState self\n[–> MOAIParticlePlugin plugin]\n<– nil",
                args = "(MOAIParticleState self, [MOAIParticlePlugin plugin])",
                returns = "nil"
            },
            setRenderScript = {
                type = "method",
                description = "Sets the particle script to use for rendering particles.\n\n–> MOAIParticleState self\n[–> MOAIParticleScript script]\n<– nil",
                args = "(MOAIParticleState self, [MOAIParticleScript script])",
                returns = "nil"
            },
            setTerm = {
                type = "method",
                description = "Sets range of terms (chosen randomly) for particles initialized by the state.\n\n–> MOAIParticleState self\n–> number minTerm\n[–> number maxTerm: Default value is minTerm.]\n<– nil",
                args = "(MOAIParticleState self, number minTerm, [number maxTerm])",
                returns = "nil"
            }
        }
    },
    MOAIParticleSystem = {
        type = "class",
        inherits = "MOAIProp MOAIAction",
        description = "Particle system.",
        childs = {
            capParticles = {
                type = "method",
                description = "Controls capping vs. wrapping of particles in overflow situation. Capping will prevent emission of additional particles when system is full. Wrapping will overwrite the oldest particles with new particles.\n\n–> MOAIParticleSystem self\n[–> boolean cap: Default value is true.]\n<– nil",
                args = "(MOAIParticleSystem self, [boolean cap])",
                returns = "nil"
            },
            capSprites = {
                type = "method",
                description = "Controls capping vs. wrapping of sprites.\n\n–> MOAIParticleSystem self\n[–> boolean cap: Default value is true.]\n<– nil",
                args = "(MOAIParticleSystem self, [boolean cap])",
                returns = "nil"
            },
            clearSprites = {
                type = "method",
                description = "Flushes any existing sprites in system.\n\n–> MOAIParticleSystem self\n<– nil",
                args = "MOAIParticleSystem self",
                returns = "nil"
            },
            getState = {
                type = "method",
                description = "Returns a particle state for an index or nil if none exists.\n\n–> MOAIParticleSystem self\n–> number index\n<– MOAIParticleState state",
                args = "(MOAIParticleSystem self, number index)",
                returns = "MOAIParticleState state",
                valuetype = "MOAIParticleState"
            },
            isIdle = {
                type = "method",
                description = "Returns true if the current system is not currently processing any particles.\n\n–> MOAIParticleSystem self\n<– boolean isIdle: Indicates whether the system is currently idle.",
                args = "MOAIParticleSystem self",
                returns = "boolean isIdle",
                valuetype = "boolean"
            },
            pushParticle = {
                type = "method",
                description = "Adds a particle to the system.\n\n–> MOAIParticleSystem self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> number dx: Default value is 0.]\n[–> number dy: Default value is 0.]\n<– boolean result: true if particle was added, false if not.",
                args = "(MOAIParticleSystem self, [number x, [number y, [number dx, [number dy]]]])",
                returns = "boolean result",
                valuetype = "boolean"
            },
            pushSprite = {
                type = "method",
                description = "Adds a sprite to the system. Sprite will persist until particle simulation is begun or 'clearSprites' is called.\n\n–> MOAIParticleSystem self\n–> number x\n–> number y\n[–> number rot: Rotation in degrees. Default value is 0.]\n[–> number xScale: Default value is 1.]\n[–> number yScale: Default value is 1.]\n<– boolean result: true is sprite was added, false if not.",
                args = "(MOAIParticleSystem self, number x, number y, [number rot, [number xScale, [number yScale]]])",
                returns = "boolean result",
                valuetype = "boolean"
            },
            reserveParticles = {
                type = "method",
                description = "Reserve particle capacity of system.\n\n–> MOAIParticleSystem self\n–> number nParticles: Total number of particle records.\n–> number particleSize: Number of parameters reserved for the particle.\n<– nil",
                args = "(MOAIParticleSystem self, number nParticles, number particleSize)",
                returns = "nil"
            },
            reserveSprites = {
                type = "method",
                description = "Reserve sprite capacity of system.\n\n–> MOAIParticleSystem self\n–> number nSprites\n<– nil",
                args = "(MOAIParticleSystem self, number nSprites)",
                returns = "nil"
            },
            reserveStates = {
                type = "method",
                description = "Reserve total number of states for system.\n\n–> MOAIParticleSystem self\n–> number nStates\n<– nil",
                args = "(MOAIParticleSystem self, number nStates)",
                returns = "nil"
            },
            setComputeBounds = {
                type = "method",
                description = "Set the a flag controlling whether the particle system re-computes its bounds every frame.\n\n–> MOAIParticleSystem self\n[–> boolean computBounds: Default value is false.]\n<– nil",
                args = "(MOAIParticleSystem self, [boolean computBounds])",
                returns = "nil"
            },
            setSpriteColor = {
                type = "method",
                description = "Set the color of the most recently added sprite.\n\n–> MOAIParticleSystem self\n–> number r\n–> number g\n–> number b\n–> number a\n<– nil",
                args = "(MOAIParticleSystem self, number r, number g, number b, number a)",
                returns = "nil"
            },
            setSpriteDeckIdx = {
                type = "method",
                description = "Set the sprite's deck index.\n\n–> MOAIParticleSystem self\n–> number index\n<– nil",
                args = "(MOAIParticleSystem self, number index)",
                returns = "nil"
            },
            setState = {
                type = "method",
                description = "Set a particle state.\n\n–> MOAIParticleSystem self\n–> number index\n–> MOAIParticleState state\n<– nil",
                args = "(MOAIParticleSystem self, number index, MOAIParticleState state)",
                returns = "nil"
            },
            surge = {
                type = "method",
                description = "Release a batch emission or particles into the system.\n\n–> MOAIParticleSystem self\n[–> number total: Default value is 1.]\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> number dx: Default value is 0.]\n[–> number dy: Default value is 0.]\n<– nil",
                args = "(MOAIParticleSystem self, [number total, [number x, [number y, [number dx, [number dy]]]]])",
                returns = "nil"
            }
        }
    },
    MOAIParticleTimedEmitter = {
        type = "class",
        inherits = "MOAIParticleEmitter",
        description = "Particle emitter.",
        childs = {
            setFrequency = {
                type = "method",
                description = "Set timer frequency.\n\n–> MOAIParticleTimedEmitter self\n–> number min\n[–> number max: Default value is min.]\n<– nil",
                args = "(MOAIParticleTimedEmitter self, number min, [number max])",
                returns = "nil"
            }
        }
    },
    MOAIPartition = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Class for optimizing spatial queries against sets of primitives. Configure for performance; default behavior is a simple list.",
        childs = {
            PLANE_XY = {
                type = "value"
            },
            PLANE_XZ = {
                type = "value"
            },
            PLANE_YZ = {
                type = "value"
            },
            clear = {
                type = "method",
                description = "Remove all props from the partition.\n\n–> MOAIPartition self\n<– nil",
                args = "MOAIPartition self",
                returns = "nil"
            },
            insertProp = {
                type = "method",
                description = "Inserts a prop into the partition. A prop can only be in one partition at a time.\n\n–> MOAIPartition self\n–> MOAIProp prop\n<– nil",
                args = "(MOAIPartition self, MOAIProp prop)",
                returns = "nil"
            },
            propForPoint = {
                type = "method",
                description = "Returns the prop with the highest priority that contains the given world space point.\n\n–> MOAIPartition self\n–> number x\n–> number y\n–> number z\n[–> number sortMode: One of the MOAILayer sort modes. Default value is SORT_PRIORITY_ASCENDING.]\n[–> number xScale: X scale for vector sort. Default value is 0.]\n[–> number yScale: Y scale for vector sort. Default value is 0.]\n[–> number zScale: Z scale for vector sort. Default value is 0.]\n[–> number priorityScale: Priority scale for vector sort. Default value is 1.]\n<– MOAIProp prop: The prop under the point or nil if no prop found.",
                args = "(MOAIPartition self, number x, number y, number z, [number sortMode, [number xScale, [number yScale, [number zScale, [number priorityScale]]]]])",
                returns = "MOAIProp prop",
                valuetype = "MOAIProp"
            },
            propForRay = {
                type = "method",
                description = "Returns the prop closest to the camera that intersects the given ray\n\n–> MOAIPartition self\n–> number x\n–> number y\n–> number z\n–> number xdirection\n–> number ydirection\n–> number zdirection\n<– MOAIProp prop: The prop under the point in order of depth or nil if no prop found.",
                args = "(MOAIPartition self, number x, number y, number z, number xdirection, number ydirection, number zdirection)",
                returns = "MOAIProp prop",
                valuetype = "MOAIProp"
            },
            propListForPoint = {
                type = "method",
                description = "Returns all props under a given world space point.\n\n–> MOAIPartition self\n–> number x\n–> number y\n–> number z\n[–> number sortMode: One of the MOAILayer sort modes. Default value is SORT_NONE.]\n[–> number xScale: X scale for vector sort. Default value is 0.]\n[–> number yScale: Y scale for vector sort. Default value is 0.]\n[–> number zScale: Z scale for vector sort. Default value is 0.]\n[–> number priorityScale: Priority scale for vector sort. Default value is 1.]\n<– ... props: The props under the point, all pushed onto the stack.",
                args = "(MOAIPartition self, number x, number y, number z, [number sortMode, [number xScale, [number yScale, [number zScale, [number priorityScale]]]]])",
                returns = "... props",
                valuetype = "..."
            },
            propListForRay = {
                type = "method",
                description = "Returns all props under a given world space point.\n\n–> MOAIPartition self\n–> number x\n–> number y\n–> number z\n–> number xdirection\n–> number ydirection\n–> number zdirection\n[–> number sortMode: One of the MOAILayer sort modes. Default value is SORT_KEY_ASCENDING.]\n[–> number xScale: X scale for vector sort. Default value is 0.]\n[–> number yScale: Y scale for vector sort. Default value is 0.]\n[–> number zScale: Z scale for vector sort. Default value is 0.]\n[–> number priorityScale: Priority scale for vector sort. Default value is 1.]\n<– ... props: The props under the point in order of depth, all pushed onto the stack.",
                args = "(MOAIPartition self, number x, number y, number z, number xdirection, number ydirection, number zdirection, [number sortMode, [number xScale, [number yScale, [number zScale, [number priorityScale]]]]])",
                returns = "... props",
                valuetype = "..."
            },
            propListForRect = {
                type = "method",
                description = "Returns all props under a given world space rect.\n\n–> MOAIPartition self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n[–> number sortMode: One of the MOAILayer sort modes. Default value is SORT_NONE.]\n[–> number xScale: X scale for vector sort. Default value is 0.]\n[–> number yScale: Y scale for vector sort. Default value is 0.]\n[–> number zScale: Z scale for vector sort. Default value is 0.]\n[–> number priorityScale: Priority scale for vector sort. Default value is 1.]\n<– ... props: The props under the rect, all pushed onto the stack.",
                args = "(MOAIPartition self, number xMin, number yMin, number xMax, number yMax, [number sortMode, [number xScale, [number yScale, [number zScale, [number priorityScale]]]]])",
                returns = "... props",
                valuetype = "..."
            },
            removeProp = {
                type = "method",
                description = "Removes a prop from the partition.\n\n–> MOAIPartition self\n–> MOAIProp prop\n<– nil",
                args = "(MOAIPartition self, MOAIProp prop)",
                returns = "nil"
            },
            reserveLevels = {
                type = "method",
                description = "Reserves a stack of levels in the partition. Levels must be initialized with setLevel (). This will trigger a full rebuild of the partition if it contains any props.\n\n–> MOAIPartition self\n–> number nLevels\n<– nil",
                args = "(MOAIPartition self, number nLevels)",
                returns = "nil"
            },
            setLevel = {
                type = "method",
                description = "Initializes a level previously created by reserveLevels (). This will trigger a full rebuild of the partition if it contains any props. Each level is a loose grid. Props of a given size may be placed by the system into any level with cells large enough to accommodate them. The dimensions of a level control how many cells the level contains. If an object goes off of the edge of a level, it will wrap around to the other side. It is possible to model a quad tree by initializing levels correctly, but for some simulations better structures may be possible.\n\n–> MOAIPartition self\n–> number levelID\n–> number cellSize: Dimensions of the layer's cells.\n–> number xCells: Width of layer in cells.\n–> number yCells: Height of layer in cells.\n<– nil",
                args = "(MOAIPartition self, number levelID, number cellSize, number xCells, number yCells)",
                returns = "nil"
            },
            setPlane = {
                type = "method",
                description = "Selects the plane the partition will use. If this is different from the current plane then all non-global props will be redistributed. Redistribution works by moving all props to the 'empties' cell and then scheduling them all for a dep node update (which refreshes the prop's bounds and may also flag it as global).\n\n–> MOAIPartition self\n–> number planeID: One of MOAIPartition::PLANE_XY, MOAIPartition::PLANE_XZ, MOAIPartition::PLANE_YZ. Default value is MOAIPartition::PLANE_XY.\n<– nil",
                args = "(MOAIPartition self, number planeID)",
                returns = "nil"
            }
        }
    },
    MOAIPathFinder = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Object for maintaining pathfinding state.",
        childs = {
            findPath = {
                type = "method",
                description = "Attempts to find an efficient path from the start node to the finish node. May be called incrementally.\n\n–> MOAIPathFinder self\n[–> number iterations]\n<– boolean more",
                args = "(MOAIPathFinder self, [number iterations])",
                returns = "boolean more",
                valuetype = "boolean"
            },
            getGraph = {
                type = "method",
                description = "Returns the attached graph (if any).\n\n–> MOAIPathFinder self\n<– MOAIPathGraph graph",
                args = "MOAIPathFinder self",
                returns = "MOAIPathGraph graph",
                valuetype = "MOAIPathGraph"
            },
            getPathEntry = {
                type = "method",
                description = "Returns a path entry. This is a node ID that may be passed back to the graph to get a location.\n\n–> MOAIPathFinder self\n–> number index\n<– number entry",
                args = "(MOAIPathFinder self, number index)",
                returns = "number entry",
                valuetype = "number"
            },
            getPathSize = {
                type = "method",
                description = "Returns the size of the path (in nodes).\n\n–> MOAIPathFinder self\n<– number size",
                args = "MOAIPathFinder self",
                returns = "number size",
                valuetype = "number"
            },
            init = {
                type = "method",
                description = "Specify the ID of the start and target node.\n\n–> MOAIPathFinder self\n–> number startNodeID\n–> number targetNodeID\n<– nil",
                args = "(MOAIPathFinder self, number startNodeID, number targetNodeID)",
                returns = "nil"
            },
            reserveTerrainWeights = {
                type = "method",
                description = "Specify the size of the terrain weight vector.\n\n–> MOAIPathFinder self\n[–> number size: Default value is 0.]\n<– nil",
                args = "(MOAIPathFinder self, [number size])",
                returns = "nil"
            },
            setFlags = {
                type = "method",
                description = "Set flags to use for pathfinding. These are graph specific flags provided by the graph implementation.\n\n–> MOAIPathFinder self\n[–> number heuristic]\n<– nil",
                args = "(MOAIPathFinder self, [number heuristic])",
                returns = "nil"
            },
            setGraph = {
                type = "method",
                description = "Set graph data to use for pathfinding.\n\nOverload:\n–> MOAIPathFinder self\n[–> MOAIGrid grid: Default value is nil.]\n<– nil\n\nOverload:\n–> MOAIPathFinder self\n[–> MOAIGridPathGraph gridPathGraph: Default value is nil.]\n<– nil",
                args = "(MOAIPathFinder self, [MOAIGrid grid | MOAIGridPathGraph gridPathGraph])",
                returns = "nil"
            },
            setHeuristic = {
                type = "method",
                description = "Set heuristic to use for pathfinding. This is a const provided by the graph implementation being used.\n\n–> MOAIPathFinder self\n[–> number heuristic]\n<– nil",
                args = "(MOAIPathFinder self, [number heuristic])",
                returns = "nil"
            },
            setTerrainDeck = {
                type = "method",
                description = "Set terrain deck to use with graph.\n\n–> MOAIPathFinder self\n[–> MOAIPathTerrainDeck terrainDeck: Default value is nil.]\n<– nil",
                args = "(MOAIPathFinder self, [MOAIPathTerrainDeck terrainDeck])",
                returns = "nil"
            },
            setTerrainMask = {
                type = "method",
                description = "Set 32-bit mask to apply to terrain samples.\n\n–> MOAIPathFinder self\n[–> number mask: Default value is 0xffffffff.]\n<– nil",
                args = "(MOAIPathFinder self, [number mask])",
                returns = "nil"
            },
            setTerrainWeight = {
                type = "method",
                description = "Set a component of the terrain weight vector.\n\n–> MOAIPathFinder self\n–> number index\n[–> number deltaScale: Default value is 0.]\n[–> number penaltyScale: Default value is 0.]\n<– nil",
                args = "(MOAIPathFinder self, number index, [number deltaScale, [number penaltyScale]])",
                returns = "nil"
            },
            setWeight = {
                type = "method",
                description = "Sets weights to be applied to G and H.\n\n–> MOAIPathFinder self\n[–> number gWeight: Default value is 1.0.]\n[–> number hWeight: Default value is 1.0.]\n<– nil",
                args = "(MOAIPathFinder self, [number gWeight, [number hWeight]])",
                returns = "nil"
            }
        }
    },
    MOAIPathGraph = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAIPathTerrainDeck = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Terrain specifications for use with pathfinding graphs. Contains indexed terrain types for graph nodes.",
        childs = {
            getMask = {
                type = "method",
                description = "Returns mask for cell.\n\n–> MOAIPathTerrainDeck self\n–> number idx\n<– number mask",
                args = "(MOAIPathTerrainDeck self, number idx)",
                returns = "number mask",
                valuetype = "number"
            },
            getTerrainVec = {
                type = "method",
                description = "Returns terrain vector for cell.\n\n–> MOAIPathTerrainDeck self\n–> number idx\n<– ...",
                args = "(MOAIPathTerrainDeck self, number idx)",
                returns = "...",
                valuetype = "..."
            },
            reserve = {
                type = "method",
                description = "Allocates terrain vectors.\n\n–> MOAIPathTerrainDeck self\n–> number deckSize\n–> number terrainVecSize\n<– nil",
                args = "(MOAIPathTerrainDeck self, number deckSize, number terrainVecSize)",
                returns = "nil"
            },
            setMask = {
                type = "method",
                description = "Returns mask for cell.\n\n–> MOAIPathTerrainDeck self\n–> number idx\n–> number mask\n<– nil",
                args = "(MOAIPathTerrainDeck self, number idx, number mask)",
                returns = "nil"
            },
            setTerrainVec = {
                type = "method",
                description = "Sets terrain vector for cell.\n\n–> MOAIPathTerrainDeck self\n–> number idx\n–> class float : MOAILuaObject... values\n<– nil",
                args = "(MOAIPathTerrainDeck self, number idx, class float... values)",
                returns = "nil"
            }
        }
    },
    MOAIPointerSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Pointer sensor.",
        childs = {
            getLoc = {
                type = "method",
                description = "Returns the location of the pointer on the screen.\n\n–> MOAIPointerSensor self\n<– number x\n<– number y",
                args = "MOAIPointerSensor self",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when the pointer location changes.\n\n–> MOAIPointerSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAIPointerSensor self, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAIProp = {
        type = "class",
        inherits = "MOAITransform MOAIColor MOAIRenderable",
        description = "Base class for props.",
        childs = {
            ATTR_INDEX = {
                type = "value"
            },
            BLEND_ADD = {
                type = "value"
            },
            BLEND_MULTIPLY = {
                type = "value"
            },
            BLEND_NORMAL = {
                type = "value"
            },
            CULL_ALL = {
                type = "value"
            },
            CULL_BACK = {
                type = "value"
            },
            CULL_FRONT = {
                type = "value"
            },
            CULL_NONE = {
                type = "value"
            },
            DEPTH_TEST_ALWAYS = {
                type = "value"
            },
            DEPTH_TEST_DISABLE = {
                type = "value"
            },
            DEPTH_TEST_EQUAL = {
                type = "value"
            },
            DEPTH_TEST_GREATER = {
                type = "value"
            },
            DEPTH_TEST_GREATER_EQUAL = {
                type = "value"
            },
            DEPTH_TEST_LESS = {
                type = "value"
            },
            DEPTH_TEST_LESS_EQUAL = {
                type = "value"
            },
            DEPTH_TEST_NEVER = {
                type = "value"
            },
            DEPTH_TEST_NOTEQUAL = {
                type = "value"
            },
            FRAME_FROM_DECK = {
                type = "value"
            },
            FRAME_FROM_PARENT = {
                type = "value"
            },
            FRAME_FROM_SELF = {
                type = "value"
            },
            GL_DST_ALPHA = {
                type = "value"
            },
            GL_DST_COLOR = {
                type = "value"
            },
            GL_FUNC_ADD = {
                type = "value"
            },
            GL_FUNC_REVERSE_SUBTRACT = {
                type = "value"
            },
            GL_FUNC_SUBTRACT = {
                type = "value"
            },
            GL_ONE = {
                type = "value"
            },
            GL_ONE_MINUS_DST_ALPHA = {
                type = "value"
            },
            GL_ONE_MINUS_DST_COLOR = {
                type = "value"
            },
            GL_ONE_MINUS_SRC_ALPHA = {
                type = "value"
            },
            GL_ONE_MINUS_SRC_COLOR = {
                type = "value"
            },
            GL_SRC_ALPHA = {
                type = "value"
            },
            GL_SRC_ALPHA_SATURATE = {
                type = "value"
            },
            GL_SRC_COLOR = {
                type = "value"
            },
            GL_ZERO = {
                type = "value"
            },
            getBounds = {
                type = "method",
                description = "Return the prop's local bounds or 'nil' if prop bounds is global or missing. The bounds are in model space and will be overridden by the prop's bounds if it's been set (using setBounds ())\n\n–> MOAIProp self\n<– number xMin\n<– number yMin\n<– number zMin\n<– number xMax\n<– number yMax\n<– number zMax",
                args = "MOAIProp self",
                returns = "(number xMin, number yMin, number zMin, number xMax, number yMax, number zMax)",
                valuetype = "number"
            },
            getDims = {
                type = "method",
                description = "Return the prop's width and height or 'nil' if prop rect is global.\n\n–> MOAIProp self\n<– number width: X max - X min\n<– number height: Y max - Y min\n<– number depth: Z max - Z min",
                args = "MOAIProp self",
                returns = "(number width, number height, number depth)",
                valuetype = "number"
            },
            getGrid = {
                type = "method",
                description = "Get the grid currently connected to the prop.\n\n–> MOAIProp self\n<– MOAIGrid grid: Current grid or nil.",
                args = "MOAIProp self",
                returns = "MOAIGrid grid",
                valuetype = "MOAIGrid"
            },
            getIndex = {
                type = "method",
                description = "Gets the value of the deck indexer.\n\n–> MOAIProp self\n<– number index",
                args = "MOAIProp self",
                returns = "number index",
                valuetype = "number"
            },
            getPriority = {
                type = "method",
                description = "Returns the current priority of the node or 'nil' if the priority is uninitialized.\n\n–> MOAIProp self\n<– number priority: The node's priority or nil.",
                args = "MOAIProp self",
                returns = "number priority",
                valuetype = "number"
            },
            getWorldBounds = {
                type = "method",
                description = "Return the prop's world bounds or 'nil' if prop bounds is global or missing.\n\n–> MOAIProp self\n<– number xMin\n<– number yMin\n<– number zMin\n<– number xMax\n<– number yMax\n<– number zMax",
                args = "MOAIProp self",
                returns = "(number xMin, number yMin, number zMin, number xMax, number yMax, number zMax)",
                valuetype = "number"
            },
            inside = {
                type = "method",
                description = "Returns true if the given world space point falls inside the prop's bounds.\n\n–> MOAIProp self\n–> number x\n–> number y\n–> number z\n[–> number pad: Pad the hit bounds (in the prop's local space)]\n<– boolean isInside",
                args = "(MOAIProp self, number x, number y, number z, [number pad])",
                returns = "boolean isInside",
                valuetype = "boolean"
            },
            isVisible = {
                type = "method",
                description = "Returns true if the given prop is visible.\n\n–> MOAIProp self\n<– boolean isVisible",
                args = "MOAIProp self",
                returns = "boolean isVisible",
                valuetype = "boolean"
            },
            setBillboard = {
                type = "method",
                description = "If set, prop will face camera when rendering.\n\n–> MOAIProp self\n[–> boolean billboard: Default value is false.]\n<– nil",
                args = "(MOAIProp self, [boolean billboard])",
                returns = "nil"
            },
            setBlendEquation = {
                type = "method",
                description = "Set the blend equation. This determines how the srcFactor and dstFactor values set with setBlendMode are interpreted.\n\nOverload:\n–> MOAIProp self\n<– nil\n\nOverload:\n–> MOAIProp self\n–> number equation: One of GL_FUNC_ADD, GL_FUNC_SUBTRACT, GL_FUNC_REVERSE_SUBTRACT.\n<– nil",
                args = "(MOAIProp self, [number equation])",
                returns = "nil"
            },
            setBlendMode = {
                type = "method",
                description = "Set the blend mode.\n\nOverload:\n–> MOAIProp self\n<– nil\n\nOverload:\n–> MOAIProp self\n–> number mode: One of MOAIProp.BLEND_NORMAL, MOAIProp.BLEND_ADD, MOAIProp.BLEND_MULTIPLY.\n<– nil\n\nOverload:\n–> MOAIProp self\n–> number srcFactor\n–> number dstFactor\n<– nil",
                args = "(MOAIProp self, [number mode | (number srcFactor, number dstFactor)])",
                returns = "nil"
            },
            setBounds = {
                type = "method",
                description = "Sets or clears the partition bounds override.\n\nOverload:\n–> MOAIProp self\n<– nil\n\nOverload:\n–> MOAIProp self\n–> number xMin\n–> number yMin\n–> number zMin\n–> number xMax\n–> number yMax\n–> number zMax\n<– nil",
                args = "(MOAIProp self, [number xMin, number yMin, number zMin, number xMax, number yMax, number zMax])",
                returns = "nil"
            },
            setCullMode = {
                type = "method",
                description = "Sets and enables face culling.\n\n–> MOAIProp self\n[–> number cullMode: Default value is MOAIProp.CULL_NONE.]\n<– nil",
                args = "(MOAIProp self, [number cullMode])",
                returns = "nil"
            },
            setDeck = {
                type = "method",
                description = "Sets or clears the deck to be indexed by the prop.\n\n–> MOAIProp self\n[–> MOAIDeck deck: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [MOAIDeck deck])",
                returns = "nil"
            },
            setDepthMask = {
                type = "method",
                description = "Disables or enables depth writing.\n\n–> MOAIProp self\n[–> boolean depthMask: Default value is true.]\n<– nil",
                args = "(MOAIProp self, [boolean depthMask])",
                returns = "nil"
            },
            setDepthTest = {
                type = "method",
                description = "Sets and enables depth testing (assuming depth buffer is present).\n\n–> MOAIProp self\n[–> number depthFunc: Default value is MOAIProp.DEPTH_TEST_DISABLE.]\n<– nil",
                args = "(MOAIProp self, [number depthFunc])",
                returns = "nil"
            },
            setExpandForSort = {
                type = "method",
                description = "Used when drawing with a layout scheme (i.e. MOAIGrid). Expanding for sort causes the prop to emit a sub-prim for each component of the layout. For example, when attaching a MOAIGrid to a prop, each cell of the grid will be added to the render queue for sorting against all other props and sub-prims. This is obviously less efficient, but still more efficient then using an separate prop for each cell or object.\n\n–> MOAIProp self\n–> boolean expandForSort: Default value is false.\n<– nil",
                args = "(MOAIProp self, boolean expandForSort)",
                returns = "nil"
            },
            setGrid = {
                type = "method",
                description = "Sets or clears the prop's grid indexer. The grid indexer (if any) will override the standard indexer.\n\n–> MOAIProp self\n[–> MOAIGrid grid: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [MOAIGrid grid])",
                returns = "nil"
            },
            setGridScale = {
                type = "method",
                description = "Scale applied to deck items before rendering to grid cell.\n\n–> MOAIProp self\n[–> number xScale: Default value is 1.]\n[–> number yScale: Default value is 1.]\n<– nil",
                args = "(MOAIProp self, [number xScale, [number yScale]])",
                returns = "nil"
            },
            setIndex = {
                type = "method",
                description = "Set the prop's index into its deck.\n\n–> MOAIProp self\n[–> number index: Default value is 1.]\n<– nil",
                args = "(MOAIProp self, [number index])",
                returns = "nil"
            },
            setParent = {
                type = "method",
                description = "This method has been deprecated. Use MOAINode setAttrLink instead.\n\n–> MOAIProp self\n[–> MOAINode parent: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [MOAINode parent])",
                returns = "nil"
            },
            setPriority = {
                type = "method",
                description = "Sets or clears the node's priority. Clear the priority to have MOAIPartition automatically assign a priority to a node when it is added.\n\n–> MOAIProp self\n[–> number priority: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [number priority])",
                returns = "nil"
            },
            setRemapper = {
                type = "method",
                description = "Set a remapper for this prop to use when drawing deck members.\n\n–> MOAIProp self\n[–> MOAIDeckRemapper remapper: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [MOAIDeckRemapper remapper])",
                returns = "nil"
            },
            setScissorRect = {
                type = "method",
                description = "Set or clear the prop's scissor rect.\n\n–> MOAIProp self\n[–> MOAIScissorRect scissorRect: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [MOAIScissorRect scissorRect])",
                returns = "nil"
            },
            setShader = {
                type = "method",
                description = "Sets or clears the prop's shader. The prop's shader takes precedence over any shader specified by the deck or its elements.\n\n–> MOAIProp self\n[–> MOAIShader shader: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [MOAIShader shader])",
                returns = "nil"
            },
            setTexture = {
                type = "method",
                description = "Set or load a texture for this prop. The prop's texture will override the deck's texture.\n\n–> MOAIProp self\n–> variant texture: A MOAITexture, MOAIMultiTexture, MOAIDataBuffer or a path to a texture file\n[–> number transform: Any bitwise combination of MOAITextureBase.QUANTIZE, MOAITextureBase.TRUECOLOR, MOAITextureBase.PREMULTIPLY_ALPHA]\n<– MOAIGfxState texture",
                args = "(MOAIProp self, variant texture, [number transform])",
                returns = "MOAIGfxState texture",
                valuetype = "MOAIGfxState"
            },
            setUVTransform = {
                type = "method",
                description = "Sets or clears the prop's UV transform.\n\n–> MOAIProp self\n[–> MOAITransformBase transform: Default value is nil.]\n<– nil",
                args = "(MOAIProp self, [MOAITransformBase transform])",
                returns = "nil"
            },
            setVisible = {
                type = "method",
                description = "Sets or clears the prop's visibility.\n\n–> MOAIProp self\n[–> boolean visible: Default value is true.]\n<– nil",
                args = "(MOAIProp self, [boolean visible])",
                returns = "nil"
            }
        }
    },
    MOAIProp2D = {
        type = "class",
        inherits = "MOAITransform2D MOAIColor MOAIRenderable",
        description = "2D prop.",
        childs = {
            getBounds = {
                type = "method",
                description = "Return the prop's local bounds or 'nil' if prop bounds is global or missing. The bounds are in model space and will be overridden by the prop's frame if it's been set (using setFrame ())\n\n–> MOAIProp2D self\n<– number xMin\n<– number yMin\n<– number xMax\n<– number yMax",
                args = "MOAIProp2D self",
                returns = "(number xMin, number yMin, number xMax, number yMax)",
                valuetype = "number"
            },
            getGrid = {
                type = "method",
                description = "Get the grid currently connected to the prop.\n\n–> MOAIProp2D self\n<– MOAIGrid grid: Current grid or nil.",
                args = "MOAIProp2D self",
                returns = "MOAIGrid grid",
                valuetype = "MOAIGrid"
            },
            getIndex = {
                type = "method",
                description = "Gets the value of the deck indexer.\n\n–> MOAIProp2D self\n<– number index",
                args = "MOAIProp2D self",
                returns = "number index",
                valuetype = "number"
            },
            getPriority = {
                type = "method",
                description = "Returns the current priority of the node or 'nil' if the priority is uninitialized.\n\n–> MOAIProp2D self\n<– number priority: The node's priority or nil.",
                args = "MOAIProp2D self",
                returns = "number priority",
                valuetype = "number"
            },
            inside = {
                type = "method",
                description = "Returns true if the given world space point falls inside the prop's bounds.\n\n–> MOAIProp2D self\n–> number x\n–> number y\n–> number z\n[–> number pad: Pad the hit bounds (in the prop's local space)]\n<– boolean isInside",
                args = "(MOAIProp2D self, number x, number y, number z, [number pad])",
                returns = "boolean isInside",
                valuetype = "boolean"
            },
            setBlendMode = {
                type = "method",
                description = "Set the blend mode.\n\nOverload:\n–> MOAIProp2D self\n<– nil\n\nOverload:\n–> MOAIProp2D self\n–> number mode: One of MOAIProp2D.BLEND_NORMAL, MOAIProp2D.BLEND_ADD, MOAIProp2D.BLEND_MULTIPLY.\n<– nil\n\nOverload:\n–> MOAIProp2D self\n–> number srcFactor\n–> number dstFactor\n<– nil",
                args = "(MOAIProp2D self, [number mode | (number srcFactor, number dstFactor)])",
                returns = "nil"
            },
            setCullMode = {
                type = "method",
                description = "Sets and enables face culling.\n\n–> MOAIProp2D self\n[–> number cullMode: Default value is MOAIProp2D.CULL_NONE.]\n<– nil",
                args = "(MOAIProp2D self, [number cullMode])",
                returns = "nil"
            },
            setDeck = {
                type = "method",
                description = "Sets or clears the deck to be indexed by the prop.\n\n–> MOAIProp2D self\n[–> MOAIDeck deck: Default value is nil.]\n<– nil",
                args = "(MOAIProp2D self, [MOAIDeck deck])",
                returns = "nil"
            },
            setDepthMask = {
                type = "method",
                description = "Disables or enables depth writing.\n\n–> MOAIProp2D self\n[–> boolean depthMask: Default value is true.]\n<– nil",
                args = "(MOAIProp2D self, [boolean depthMask])",
                returns = "nil"
            },
            setDepthTest = {
                type = "method",
                description = "Sets and enables depth testing (assuming depth buffer is present).\n\n–> MOAIProp2D self\n[–> number depthFunc: Default value is MOAIProp2D.DEPTH_TEST_DISABLE.]\n<– nil",
                args = "(MOAIProp2D self, [number depthFunc])",
                returns = "nil"
            },
            setExpandForSort = {
                type = "method",
                description = "Used when drawing with a layout scheme (i.e. MOAIGrid). Expanding for sort causes the prop to emit a sub-prim for each component of the layout. For example, when attaching a MOAIGrid to a prop, each cell of the grid will be added to the render queue for sorting against all other props and sub-prims. This is obviously less efficient, but still more efficient then using an separate prop for each cell or object.\n\n–> MOAIProp2D self\n–> boolean expandForSort: Default value is false.\n<– nil",
                args = "(MOAIProp2D self, boolean expandForSort)",
                returns = "nil"
            },
            setFrame = {
                type = "method",
                description = "Sets the fitting frame of the prop.\n\nOverload:\n–> MOAIProp2D self\n<– nil\n\nOverload:\n–> MOAIProp2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIProp2D self, [number xMin, number yMin, number xMax, number yMax])",
                returns = "nil"
            },
            setGrid = {
                type = "method",
                description = "Sets or clears the prop's grid indexer. The grid indexer (if any) will override the standard indexer.\n\n–> MOAIProp2D self\n[–> MOAIGrid grid: Default value is nil.]\n<– nil",
                args = "(MOAIProp2D self, [MOAIGrid grid])",
                returns = "nil"
            },
            setGridScale = {
                type = "method",
                description = "Scale applied to deck items before rendering to grid cell.\n\n–> MOAIProp2D self\n[–> number xScale: Default value is 1.]\n[–> number yScale: Default value is 1.]\n<– nil",
                args = "(MOAIProp2D self, [number xScale, [number yScale]])",
                returns = "nil"
            },
            setIndex = {
                type = "method",
                description = "Set the prop's index into its deck.\n\n–> MOAIProp2D self\n[–> number index: Default value is 1.]\n<– nil",
                args = "(MOAIProp2D self, [number index])",
                returns = "nil"
            },
            setParent = {
                type = "method",
                description = "This method has been deprecated. Use MOAINode setAttrLink instead.\n\n–> MOAIProp2D self\n[–> MOAINode parent: Default value is nil.]\n<– nil",
                args = "(MOAIProp2D self, [MOAINode parent])",
                returns = "nil"
            },
            setPriority = {
                type = "method",
                description = "Sets or clears the node's priority. Clear the priority to have MOAIPartition automatically assign a priority to a node when it is added.\n\n–> MOAIProp2D self\n[–> number priority: Default value is nil.]\n<– nil",
                args = "(MOAIProp2D self, [number priority])",
                returns = "nil"
            },
            setRemapper = {
                type = "method",
                description = "Set a remapper for this prop to use when drawing deck members.\n\n–> MOAIProp2D self\n[–> MOAIDeckRemapper remapper: Default value is nil.]\n<– nil",
                args = "(MOAIProp2D self, [MOAIDeckRemapper remapper])",
                returns = "nil"
            },
            setShader = {
                type = "method",
                description = "Sets or clears the prop's shader. The prop's shader takes precedence over any shader specified by the deck or its elements.\n\n–> MOAIProp2D self\n[–> MOAIShader shader: Default value is nil.]\n<– nil",
                args = "(MOAIProp2D self, [MOAIShader shader])",
                returns = "nil"
            },
            setTexture = {
                type = "method",
                description = "Set or load a texture for this prop. The prop's texture will override the deck's texture.\n\n–> MOAIProp2D self\n–> variant texture: A MOAITexture, MOAIMultiTexture, MOAIDataBuffer or a path to a texture file\n[–> number transform: Any bitwise combination of MOAITextureBase.QUANTIZE, MOAITextureBase.TRUECOLOR, MOAITextureBase.PREMULTIPLY_ALPHA]\n<– MOAIGfxState texture",
                args = "(MOAIProp2D self, variant texture, [number transform])",
                returns = "MOAIGfxState texture",
                valuetype = "MOAIGfxState"
            },
            setUVTransform = {
                type = "method",
                description = "Sets or clears the prop's UV transform.\n\n–> MOAIProp2D self\n[–> MOAITransformBase transform: Default value is nil.]\n<– nil",
                args = "(MOAIProp2D self, [MOAITransformBase transform])",
                returns = "nil"
            },
            setVisible = {
                type = "method",
                description = "Sets or clears the prop's visibility.\n\n–> MOAIProp2D self\n[–> boolean visible: Default value is true.]\n<– nil",
                args = "(MOAIProp2D self, [boolean visible])",
                returns = "nil"
            }
        }
    },
    MOAIRenderable = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Abstract base class for objects that can be rendered by MOAIRenderMgr.",
        childs = {}
    },
    MOAIRenderMgr = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "MOAIRenderMgr is responsible for drawing a list of MOAIRenderable objects. MOAIRenderable is the base class for any object that can be drawn. This includes MOAIProp and MOAILayer. To use MOAIRenderMgr pass a table of MOAIRenderable objects to MOAIRenderMgr.setRenderTable (). The table will usually be a stack of MOAILayer objects. The contents of the table will be rendered the next time a frame is drawn. Note that the table must be an array starting with index 1. Objects will be rendered counting from the base index until 'nil' is encountered. The render table may include other tables as entries. These must also be arrays indexed from 1.",
        childs = {
            clearRenderStack = {
                type = "function",
                description = "Sets the render stack to nil. THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            getPerformanceDrawCount = {
                type = "function",
                description = 'Returns the number of draw calls last frame.\n\n<– number count: Number of underlying graphics "draw" calls last frame.',
                args = "()",
                returns = "number count",
                valuetype = "number"
            },
            getRenderTable = {
                type = "function",
                description = "Returns the table currently being used for rendering.\n\n<– table renderTable",
                args = "()",
                returns = "table renderTable",
                valuetype = "table"
            },
            grabNextFrame = {
                type = "function",
                description = "Save the next frame rendered to\n\n–> MOAIImage image: Image to save the backbuffer to\n–> function callback: The function to execute when the frame has been saved into the image specified\n<– table renderTable",
                args = "(MOAIImage image, function callback)",
                returns = "table renderTable",
                valuetype = "table"
            },
            popRenderPass = {
                type = "function",
                description = "Pops the top renderable from the render stack. THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            pushRenderPass = {
                type = "function",
                description = "Pushes a renderable onto the render stack. THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE.\n\n–> MOAIRenderable renderable\n<– nil",
                args = "MOAIRenderable renderable",
                returns = "nil"
            },
            removeRenderPass = {
                type = "function",
                description = "Removes a renderable from the render stack. THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE. Superseded by setRenderTable.\n\n–> MOAIRenderable renderable\n<– nil",
                args = "MOAIRenderable renderable",
                returns = "nil"
            },
            setRenderTable = {
                type = "function",
                description = "Sets the table to be used for rendering. This should be an array indexed from 1 consisting of MOAIRenderable objects and sub-tables. Objects will be rendered in order starting from index 1 and continuing until 'nil' is encountered.\n\n–> table renderTable\n<– nil",
                args = "table renderTable",
                returns = "nil"
            }
        }
    },
    MOAIScissorRect = {
        type = "class",
        inherits = "MOAITransform",
        description = "Class for clipping props when drawing.",
        childs = {
            getRect = {
                type = "method",
                description = "Return the extents of the scissor rect.\n\n–> MOAIScissorRect self\n<– number xMin\n<– number yMin\n<– number xMax\n<– number yMax",
                args = "MOAIScissorRect self",
                returns = "(number xMin, number yMin, number xMax, number yMax)",
                valuetype = "number"
            },
            setRect = {
                type = "function",
                description = "Sets the extents of the scissor rect.\n\n–> number x1: The X coordinate of the rect's upper-left point.\n–> number y1: The Y coordinate of the rect's upper-left point.\n–> number x2: The X coordinate of the rect's lower-right point.\n–> number y2: The Y coordinate of the rect's lower-right point.\n<– nil",
                args = "(number x1, number y1, number x2, number y2)",
                returns = "nil"
            },
            setScissorRect = {
                type = "method",
                description = "Set or clear the parent scissor rect.\n\n–> MOAIScissorRect self\n[–> MOAIScissorRect parent: Default value is nil.]\n<– nil",
                args = "(MOAIScissorRect self, [MOAIScissorRect parent])",
                returns = "nil"
            }
        }
    },
    MOAIScriptDeck = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Scriptable deck object.",
        childs = {
            setDrawCallback = {
                type = "method",
                description = "Sets the callback to be issued when draw events occur. The callback's parameters are ( number index, number xOff, number yOff, number xScale, number yScale ).\n\n–> MOAIScriptDeck self\n–> function callback\n<– nil",
                args = "(MOAIScriptDeck self, function callback)",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set the model space dimensions of the deck's default rect.\n\n–> MOAIScriptDeck self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIScriptDeck self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            setRectCallback = {
                type = "method",
                description = "Sets the callback to be issued when the size of a deck item needs to be determined. The callback's parameters are ( number index ).\n\n–> MOAIScriptDeck self\n–> function callback\n<– nil",
                args = "(MOAIScriptDeck self, function callback)",
                returns = "nil"
            },
            setTotalRectCallback = {
                type = "method",
                description = "Sets the callback to be issued when the size of a deck item needs to be determined. The callback's parameters are ( ).\n\n–> MOAIScriptDeck self\n–> function callback\n<– nil",
                args = "(MOAIScriptDeck self, function callback)",
                returns = "nil"
            }
        }
    },
    MOAIScriptNode = {
        type = "class",
        inherits = "MOAINode",
        description = "User scriptable dependency node. User may specify Lua callback to handle node updating as well as custom floating point attributes.",
        childs = {
            reserveAttrs = {
                type = "method",
                description = "Reserve memory for custom attributes and initializes them to 0.\n\n–> MOAIScriptNode self\n–> number nAttributes\n<– nil",
                args = "(MOAIScriptNode self, number nAttributes)",
                returns = "nil"
            },
            setCallback = {
                type = "method",
                description = "Sets a Lua function to be called whenever the node is updated.\n\n–> MOAIScriptNode self\n–> function onUpdate\n<– nil",
                args = "(MOAIScriptNode self, function onUpdate)",
                returns = "nil"
            }
        }
    },
    MOAISensor = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Base class for sensors.",
        childs = {}
    },
    MOAISerializer = {
        type = "class",
        inherits = "MOAISerializerBase",
        description = "Manages serialization state of Lua tables and Moai objects. The serializer will produce a Lua script that, when executed, will return the ordered list of objects added to it using the serialize () function.",
        childs = {
            exportToFile = {
                type = "method",
                description = "Exports the contents of the serializer to a file.\n\n–> MOAISerializer self\n–> string filename\n<– nil",
                args = "(MOAISerializer self, string filename)",
                returns = "nil"
            },
            exportToString = {
                type = "method",
                description = "Exports the contents of the serializer to a string.\n\n–> MOAISerializer self\n<– string result",
                args = "MOAISerializer self",
                returns = "string result",
                valuetype = "string"
            },
            serialize = {
                type = "method",
                description = "Adds a table or object to the serializer.\n\nOverload:\n–> MOAISerializer self\n–> table data: The table to serialize.\n<– nil\n\nOverload:\n–> MOAISerializer self\n–> MOAILuaObject data: The object to serialize.\n<– nil",
                args = "(MOAISerializer self, (table data | MOAILuaObject data))",
                returns = "nil"
            },
            serializeToFile = {
                type = "function",
                description = "Serializes the specified table or object to a file.\n\nOverload:\n–> string filename: The file to create.\n–> table data: The table to serialize.\n<– nil\n\nOverload:\n–> string filename: The file to create.\n–> MOAILuaObject data: The object to serialize.\n<– nil",
                args = "(string filename, (table data | MOAILuaObject data))",
                returns = "nil"
            },
            serializeToString = {
                type = "function",
                description = "Serializes the specified table or object to a string.\n\nOverload:\n–> table data: The table to serialize.\n<– string serialized: The serialized string.\n\nOverload:\n–> MOAILuaObject data: The object to serialize.\n<– string serialized: The serialized string.",
                args = "(table data | MOAILuaObject data)",
                returns = "string serialized",
                valuetype = "string"
            }
        }
    },
    MOAISerializerBase = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAIShader = {
        type = "class",
        inherits = "MOAINode MOAIGfxResource",
        description = "Programmable shader class.",
        childs = {
            UNIFORM_COLOR = {
                type = "value"
            },
            UNIFORM_FLOAT = {
                type = "value"
            },
            UNIFORM_INT = {
                type = "value"
            },
            UNIFORM_PEN_COLOR = {
                type = "value"
            },
            UNIFORM_SAMPLER = {
                type = "value"
            },
            UNIFORM_TRANSFORM = {
                type = "value"
            },
            UNIFORM_VIEW_PROJ = {
                type = "value"
            },
            UNIFORM_WORLD = {
                type = "value"
            },
            UNIFORM_WORLD_VIEW = {
                type = "value"
            },
            UNIFORM_WORLD_VIEW_PROJ = {
                type = "value"
            },
            clearUniform = {
                type = "method",
                description = "Clears a uniform mapping.\n\n–> MOAIShader self\n–> number idx\n<– nil",
                args = "(MOAIShader self, number idx)",
                returns = "nil"
            },
            declareUniform = {
                type = "method",
                description = "Declares a uniform mapping.\n\n–> MOAIShader self\n–> number idx\n–> string name\n[–> number type: One of MOAIShader.UNIFORM_COLOR, MOAIShader.UNIFORM_FLOAT, MOAIShader.UNIFORM_INT, MOAIShader.UNIFORM_TRANSFORM, MOAIShader.UNIFORM_PEN_COLOR, MOAIShader.UNIFORM_VIEW_PROJ, MOAIShader.UNIFORM_WORLD, MOAIShader.UNIFORM_WORLD_VIEW, MOAIShader.UNIFORM_WORLD_VIEW_PROJ]\n<– nil",
                args = "(MOAIShader self, number idx, string name, [number type])",
                returns = "nil"
            },
            declareUniformFloat = {
                type = "method",
                description = "Declares an float uniform.\n\n–> MOAIShader self\n–> number idx\n–> string name\n[–> number value: Default value is 0.]\n<– nil",
                args = "(MOAIShader self, number idx, string name, [number value])",
                returns = "nil"
            },
            declareUniformInt = {
                type = "method",
                description = "Declares an integer uniform.\n\n–> MOAIShader self\n–> number idx\n–> string name\n[–> number value: Default value is 0.]\n<– nil",
                args = "(MOAIShader self, number idx, string name, [number value])",
                returns = "nil"
            },
            declareUniformSampler = {
                type = "method",
                description = "Declares an uniform to be used as a texture unit index. This uniform is internally an int, but when loaded into the shader the number one subtracted from its value. This allows the user to maintain consistency with Lua's convention of indexing from one.\n\n–> MOAIShader self\n–> number idx\n–> string name\n[–> number textureUnit: Default value is 1.]\n<– nil",
                args = "(MOAIShader self, number idx, string name, [number textureUnit])",
                returns = "nil"
            },
            load = {
                type = "method",
                description = "Load a shader program.\n\n–> MOAIShader self\n–> string vertexShaderSource\n–> string fragmentShaderSource\n<– nil",
                args = "(MOAIShader self, string vertexShaderSource, string fragmentShaderSource)",
                returns = "nil"
            },
            reserveUniforms = {
                type = "method",
                description = "Reserve shader uniforms.\n\n–> MOAIShader self\n[–> number nUniforms: Default value is 0.]\n<– nil",
                args = "(MOAIShader self, [number nUniforms])",
                returns = "nil"
            },
            setVertexAttribute = {
                type = "method",
                description = "Names a shader vertex attribute.\n\n–> MOAIShader self\n–> number index: Default value is 1.\n–> string name: Name of attribute.\n<– nil",
                args = "(MOAIShader self, number index, string name)",
                returns = "nil"
            }
        }
    },
    MOAIShaderMgr = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Shader presets.",
        childs = {
            DECK2D_SHADER = {
                type = "value"
            },
            DECK2D_TEX_ONLY_SHADER = {
                type = "value"
            },
            FONT_SHADER = {
                type = "value"
            },
            LINE_SHADER = {
                type = "value"
            },
            MESH_SHADER = {
                type = "value"
            },
            getShader = {
                type = "function",
                description = "Return one of the built-in shaders.\n\n–> number shaderID: One of MOAIShaderMgr.DECK2D_SHADER, MOAIShaderMgr.FONT_SHADER, MOAIShaderMgr.LINE_SHADER, MOAIShaderMgr.MESH_SHADER\n<– nil",
                args = "number shaderID",
                returns = "nil"
            }
        }
    },
    MOAISim = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Sim timing and settings class.",
        childs = {
            DEFAULT_BOOST_THRESHOLD = {
                type = "value",
                description = "Value is 3"
            },
            DEFAULT_CPU_BUDGET = {
                type = "value",
                description = "Value is 2"
            },
            DEFAULT_LONG_DELAY_THRESHOLD = {
                type = "value",
                description = "Value is 10"
            },
            DEFAULT_STEP_MULTIPLIER = {
                type = "value",
                description = "Value is 1"
            },
            DEFAULT_STEPS_PER_SECOND = {
                type = "value",
                description = "Value is 60"
            },
            EVENT_FINALIZE = {
                type = "value"
            },
            EVENT_PAUSE = {
                type = "value"
            },
            EVENT_RESUME = {
                type = "value"
            },
            LOOP_FLAGS_DEFAULT = {
                type = "value"
            },
            LOOP_FLAGS_FIXED = {
                type = "value"
            },
            LOOP_FLAGS_MULTISTEP = {
                type = "value"
            },
            SIM_LOOP_ALLOW_BOOST = {
                type = "value"
            },
            SIM_LOOP_ALLOW_SOAK = {
                type = "value"
            },
            SIM_LOOP_ALLOW_SPIN = {
                type = "value"
            },
            SIM_LOOP_FORCE_STEP = {
                type = "value"
            },
            SIM_LOOP_NO_DEFICIT = {
                type = "value"
            },
            SIM_LOOP_NO_SURPLUS = {
                type = "value"
            },
            SIM_LOOP_RESET_CLOCK = {
                type = "value"
            },
            clearLoopFlags = {
                type = "function",
                description = "Uses the mask provided to clear the loop flags.\n\n[–> number mask: Default value is 0xffffffff.]\n<– nil",
                args = "[number mask]",
                returns = "nil"
            },
            clearRenderStack = {
                type = "function",
                description = "Alias for MOAIRenderMgr.clearRenderStack (). THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            crash = {
                type = "function",
                description = "Crashes Moai with a null pointer dereference.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            enterFullscreenMode = {
                type = "function",
                description = "Enters fullscreen mode on the device if possible.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            exitFullscreenMode = {
                type = "function",
                description = "Exits fullscreen mode on the device if possible.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            forceGC = {
                type = "function",
                description = "Runs the garbage collector repeatedly until no more MOAIObjects can be collected.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            framesToTime = {
                type = "function",
                description = "Converts the number of frames to time passed in seconds.\n\n–> number frames: The number of frames.\n<– number time: The equivalent number of seconds for the specified number of frames.",
                args = "number frames",
                returns = "number time",
                valuetype = "number"
            },
            getDeviceTime = {
                type = "function",
                description = "Gets the raw device clock. This is a replacement for Lua's os.time ().\n\n<– number time: The device clock time in seconds.",
                args = "()",
                returns = "number time",
                valuetype = "number"
            },
            getElapsedFrames = {
                type = "function",
                description = "Gets the number of frames elapsed since the application was started.\n\n<– number frames: The number of elapsed frames.",
                args = "()",
                returns = "number frames",
                valuetype = "number"
            },
            getElapsedTime = {
                type = "function",
                description = "Gets the number of seconds elapsed since the application was started.\n\n<– number time: The number of elapsed seconds.",
                args = "()",
                returns = "number time",
                valuetype = "number"
            },
            getHistogram = {
                type = "function",
                description = "Generates a histogram of active MOAIObjects and returns it in a table containing object tallies indexed by object class names.\n\n<– table histogram",
                args = "()",
                returns = "table histogram",
                valuetype = "table"
            },
            getLoopFlags = {
                type = "function",
                description = "Returns the current loop flags.\n\n<– number mask",
                args = "()",
                returns = "number mask",
                valuetype = "number"
            },
            getLuaObjectCount = {
                type = "function",
                description = "Gets the total number of objects in memory that inherit MOAILuaObject. Count includes objects that are not bound to the Lua runtime.\n\n<– number count",
                args = "()",
                returns = "number count",
                valuetype = "number"
            },
            getMemoryUsage = {
                type = "function",
                description = "Get the current amount of memory used by MOAI and its subsystems. This will attempt to return reasonable estimates where exact values cannot be obtained. Some fields represent informational fields (i.e. are not double counted in the total, but present to assist debugging) and may be only available on certain platforms (e.g. Windows, etc). These fields begin with a '_' character.\n\n<– table usage: The breakdown of each subsystem's memory usage, in bytes. There is also a \"total\" field that contains the summed value.",
                args = "()",
                returns = "table usage",
                valuetype = "table"
            },
            getPerformance = {
                type = "function",
                description = "Returns an estimated frames per second based on measurements taken at every render.\n\n<– number fps: Estimated frames per second.",
                args = "()",
                returns = "number fps",
                valuetype = "number"
            },
            getStep = {
                type = "function",
                description = "Gets the amount of time (in seconds) that it takes for one frame to pass.\n\n<– number size: The size of the frame; the time it takes for one frame to pass.",
                args = "()",
                returns = "number size",
                valuetype = "number"
            },
            hideCursor = {
                type = "function",
                description = "Hides system cursor.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            openWindow = {
                type = "function",
                description = "Opens a new window for the application to render on. This must be called before any rendering can be done, and it must only be called once.\n\n–> string title: The title of the window.\n–> number width: The width of the window in pixels.\n–> number height: The height of the window in pixels.\n<– nil",
                args = "(string title, number width, number height)",
                returns = "nil"
            },
            pauseTimer = {
                type = "function",
                description = "Pauses or unpauses the device timer, preventing any visual updates (rendering) while paused.\n\n–> boolean pause: Whether the device timer should be paused.\n<– nil",
                args = "boolean pause",
                returns = "nil"
            },
            popRenderPass = {
                type = "function",
                description = "Alias for MOAIRenderMgr.popRenderPass (). THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            pushRenderPass = {
                type = "function",
                description = "Alias for MOAIRenderMgr.pushRenderPass (). THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE.\n\n–> MOAIRenderable renderable\n<– nil",
                args = "MOAIRenderable renderable",
                returns = "nil"
            },
            removeRenderPass = {
                type = "function",
                description = "Alias for MOAIRenderMgr.removeRenderPass (). THIS METHOD IS DEPRECATED AND WILL BE REMOVED IN A FUTURE RELEASE.\n\n–> MOAIRenderable renderable\n<– nil",
                args = "MOAIRenderable renderable",
                returns = "nil"
            },
            reportHistogram = {
                type = "function",
                description = "Generates a histogram of active MOAIObjects.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            reportLeaks = {
                type = "function",
                description = 'Analyze the currently allocated MOAI objects and create a textual report of where they were declared, and what Lua references (if any) can be found. NOTE: This is incredibly slow, so only use to debug leaking memory issues. This will also trigger a full garbage collection before performing the required report. (Equivalent of collectgarbage("collect").)\n\n–> boolean clearAfter: If true, it will reset the allocation tables (without freeing the underlying objects). This allows this method to be called after a known operation and get only those allocations created since the last call to this function.\n<– nil',
                args = "boolean clearAfter",
                returns = "nil"
            },
            setBoostThreshold = {
                type = "function",
                description = "Sets the boost threshold, a scalar applied to step. If the gap between simulation time and device time is greater than the step size multiplied by the boost threshold and MOAISim.SIM_LOOP_ALLOW_BOOST is set in the loop flags, then the simulation is updated once with a large, variable step to make up the entire gap.\n\n[–> number boostThreshold: Default value is DEFAULT_BOOST_THRESHOLD.]\n<– nil",
                args = "[number boostThreshold]",
                returns = "nil"
            },
            setCpuBudget = {
                type = "function",
                description = "Sets the amount of time (given in simulation steps) to allow for updating the simulation.\n\n–> number budget: Default value is DEFAULT_CPU_BUDGET.\n<– nil",
                args = "number budget",
                returns = "nil"
            },
            setHistogramEnabled = {
                type = "function",
                description = "Enable tracking of every MOAILuaObject so that an object count histogram may be generated.\n\n[–> boolean enable: Default value is false.]\n<– nil",
                args = "[boolean enable]",
                returns = "nil"
            },
            setLeakTrackingEnabled = {
                type = "function",
                description = "Enable extra memory book-keeping measures that allow all MOAI objects to be tracked back to their point of allocation (in Lua). Use together with MOAISim.reportLeaks() to determine exactly where your memory usage is being created. NOTE: This is very expensive in terms of both CPU and the extra memory associated with the stack info book-keeping. Use only when tracking down leaks.\n\n[–> boolean enable: Default value is false.]\n<– nil",
                args = "[boolean enable]",
                returns = "nil"
            },
            setLongDelayThreshold = {
                type = "function",
                description = "Sets the long delay threshold. If the simulation step falls behind the given threshold, the deficit will be dropped: the simulation will neither spin nor boost to catch up.\n\n[–> number longDelayThreshold: Default value is DEFAULT_LONG_DELAY_THRESHOLD.]\n<– nil",
                args = "[number longDelayThreshold]",
                returns = "nil"
            },
            setLoopFlags = {
                type = "function",
                description = "Fine tune behavior of the simulation loop. MOAISim.SIM_LOOP_ALLOW_SPIN will allow the simulation step to run multiple times per update to try and catch up with device time, but will abort if processing the simulation exceeds the configured step time. MOAISim.SIM_LOOP_ALLOW_BOOST will permit a *variable* update step if simulation time falls too far behind device time (based on the boost threshold). Be warned: this can wreak havoc with physics and stepwise animation or game AI. Three presets are provided: MOAISim.LOOP_FLAGS_DEFAULT, MOAISim.LOOP_FLAGS_FIXED, and MOAISim.LOOP_FLAGS_MULTISTEP.\n\n[–> number flags: Mask or a combination of MOAISim.SIM_LOOP_FORCE_STEP, MOAISim.SIM_LOOP_ALLOW_BOOST, MOAISim.SIM_LOOP_ALLOW_SPIN, MOAISim.SIM_LOOP_NO_DEFICIT, MOAISim.SIM_LOOP_NO_SURPLUS, MOAISim.SIM_LOOP_RESET_CLOCK. Default value is 0.]\n<– nil",
                args = "[number flags]",
                returns = "nil"
            },
            setLuaAllocLogEnabled = {
                type = "function",
                description = "Toggles log messages from Lua allocator.\n\n[–> boolean enable: Default value is 'false.']\n<– nil",
                args = "[boolean enable]",
                returns = "nil"
            },
            setStep = {
                type = "function",
                description = "Sets the size of each simulation step (in seconds).\n\n–> number step: The step size. Default value is 1 / DEFAULT_STEPS_PER_SECOND.\n<– nil",
                args = "number step",
                returns = "nil"
            },
            setStepMultiplier = {
                type = "function",
                description = "Runs the simulation multiple times per step (but with a fixed step size). This is used to speed up the simulation without providing a larger step size (which could destabilize physics simulation).\n\n–> number count: Default value is DEFAULT_STEP_MULTIPLIER.\n<– nil",
                args = "number count",
                returns = "nil"
            },
            setTimerError = {
                type = "function",
                description = "Sets the tolerance for timer error. This is a multiplier of step. Timer error tolerance is step * timerError.\n\n–> number timerError: Default value is 0.0.\n<– nil",
                args = "number timerError",
                returns = "nil"
            },
            setTraceback = {
                type = "function",
                description = "Sets the function to call when a traceback occurs in Lua\n\n–> function callback: Function to execute when the traceback occurs\n<– nil",
                args = "function callback",
                returns = "nil"
            },
            showCursor = {
                type = "function",
                description = "Shows system cursor.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            timeToFrames = {
                type = "function",
                description = "Converts the number of time passed in seconds to frames.\n\n–> number time: The number of seconds.\n<– number frames: The equivalent number of frames for the specified number of seconds.",
                args = "number time",
                returns = "number frames",
                valuetype = "number"
            }
        }
    },
    MOAIStaticGlyphCache = {
        type = "class",
        inherits = "MOAIGlyphCacheBase",
        description = "This is the default implementation of a static glyph cache. All is does is accept an image via setImage () and create a set of textures from that image. It does not implement getImage ().",
        childs = {}
    },
    MOAIStream = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Interface for reading/writing binary data.",
        childs = {
            SEEK_CUR = {
                type = "value"
            },
            SEEK_END = {
                type = "value"
            },
            SEEK_SET = {
                type = "value"
            },
            flush = {
                type = "method",
                description = "Forces any remaining buffered data into the stream.\n\n–> MOAIStream self\n<– nil",
                args = "MOAIStream self",
                returns = "nil"
            },
            getCursor = {
                type = "method",
                description = "Returns the current cursor position in the stream.\n\n–> MOAIStream self\n<– number cursor",
                args = "MOAIStream self",
                returns = "number cursor",
                valuetype = "number"
            },
            getLength = {
                type = "method",
                description = "Returns the length of the stream.\n\n–> MOAIStream self\n<– number length",
                args = "MOAIStream self",
                returns = "number length",
                valuetype = "number"
            },
            read = {
                type = "method",
                description = "Reads bytes from the stream.\n\n–> MOAIStream self\n[–> number byteCount: Number of bytes to read. Default value is the length of the stream.]\n<– string bytes: Data read from the stream.\n<– number actualByteCount: Size of data successfully read.",
                args = "(MOAIStream self, [number byteCount])",
                returns = "(string bytes, number actualByteCount)",
                valuetype = "string"
            },
            read16 = {
                type = "method",
                description = "Reads a signed 16-bit value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            read32 = {
                type = "method",
                description = "Reads a signed 32-bit value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            read8 = {
                type = "method",
                description = "Reads a signed 8-bit value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            readDouble = {
                type = "method",
                description = "Reads a 64-bit floating point value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            readFloat = {
                type = "method",
                description = "Reads a 32-bit floating point value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            readFormat = {
                type = "method",
                description = "Reads a series of values from the stream given a format string. Valid tokens for the format string are: u8 u16 u32 f d s8 s16 s32. Tokens may be optionally separated by spaces of commas.\n\n–> MOAIStream self\n–> string format\n<– ... values: Values read from the stream or 'nil'.\n<– number size: Number of bytes successfully read.",
                args = "(MOAIStream self, string format)",
                returns = "(... values, number size)",
                valuetype = "..."
            },
            readU16 = {
                type = "method",
                description = "Reads an unsigned 16-bit value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            readU32 = {
                type = "method",
                description = "Reads an unsigned 32-bit value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            readU8 = {
                type = "method",
                description = "Reads an unsigned 8-bit value from the stream.\n\n–> MOAIStream self\n<– number value: Value from the stream.\n<– number size: Number of bytes successfully read.",
                args = "MOAIStream self",
                returns = "(number value, number size)",
                valuetype = "number"
            },
            seek = {
                type = "method",
                description = "Repositions the cursor in the stream.\n\n–> MOAIStream self\n–> number offset: Value from the stream.\n[–> number mode: One of MOAIStream.SEEK_CUR, MOAIStream.SEEK_END, MOAIStream.SEEK_SET. Default value is MOAIStream.SEEK_SET.]\n<– nil",
                args = "(MOAIStream self, number offset, [number mode])",
                returns = "nil"
            },
            write = {
                type = "method",
                description = "Write binary data to the stream.\n\n–> MOAIStream self\n–> string bytes: Binary data to write.\n[–> number size: Number of bytes to write. Default value is the size of the string.]\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, string bytes, [number size])",
                returns = "number size",
                valuetype = "number"
            },
            write16 = {
                type = "method",
                description = "Writes a signed 16-bit value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            },
            write32 = {
                type = "method",
                description = "Writes a signed 32-bit value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            },
            write8 = {
                type = "method",
                description = "Writes a signed 8-bit value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            },
            writeDouble = {
                type = "method",
                description = "Writes a 64-bit floating point value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            },
            writeFloat = {
                type = "method",
                description = "Writes a 32-bit floating point value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            },
            writeFormat = {
                type = "method",
                description = "Writes a series of values to the stream given a format string. See 'readFormat' for a list of valid format tokens.\n\n–> MOAIStream self\n–> string format\n–> ... values: Values to be written to the stream.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, string format, ... values)",
                returns = "number size",
                valuetype = "number"
            },
            writeStream = {
                type = "method",
                description = "Reads bytes from the given stream into the calling stream.\n\n–> MOAIStream self\n–> MOAIStream stream: Value to write.\n[–> number size: Number of bytes to read/write. Default value is the length of the input stream.]\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, MOAIStream stream, [number size])",
                returns = "number size",
                valuetype = "number"
            },
            writeU16 = {
                type = "method",
                description = "Writes an unsigned 16-bit value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            },
            writeU32 = {
                type = "method",
                description = "Writes an unsigned 32-bit value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            },
            writeU8 = {
                type = "method",
                description = "Writes an unsigned 8-bit value to the stream.\n\n–> MOAIStream self\n–> number value: Value to write.\n<– number size: Number of bytes successfully written.",
                args = "(MOAIStream self, number value)",
                returns = "number size",
                valuetype = "number"
            }
        }
    },
    MOAIStreamReader = {
        type = "class",
        inherits = "MOAIStream",
        description = "MOAIStreamReader may be attached to another stream for the purpose of decoding and/or decompressing bytes read from that stream using a given algorithm (such as base64 or 'deflate').",
        childs = {
            close = {
                type = "method",
                description = "Detach the target stream. (This only detaches the target from the formatter; it does not also close the target stream).\n\n–> MOAIStreamReader self\n<– nil",
                args = "MOAIStreamReader self",
                returns = "nil"
            },
            openBase64 = {
                type = "method",
                description = "Open a base 64 formatted stream for reading (i.e. decode bytes from base64).\n\n–> MOAIStreamReader self\n–> MOAIStream target\n<– boolean success",
                args = "(MOAIStreamReader self, MOAIStream target)",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openDeflate = {
                type = "method",
                description = "Open a 'deflate' formatted stream for reading (i.e. decompress bytes using the 'deflate' algorithm).\n\n–> MOAIStreamReader self\n–> MOAIStream target\n[–> number windowBits: The window bits used in the DEFLATE algorithm.]\n<– boolean success",
                args = "(MOAIStreamReader self, MOAIStream target, [number windowBits])",
                returns = "boolean success",
                valuetype = "boolean"
            }
        }
    },
    MOAIStreamWriter = {
        type = "class",
        inherits = "MOAIStream",
        description = "MOAIStreamWriter may be attached to another stream for the purpose of encoding and/or compressing bytes written to that stream using a given algorithm (such as base64 or 'deflate').",
        childs = {
            close = {
                type = "method",
                description = "Flush any remaining buffered data and detach the target stream. (This only detaches the target from the formatter; it does not also close the target stream).\n\n–> MOAIStreamWriter self\n<– nil",
                args = "MOAIStreamWriter self",
                returns = "nil"
            },
            openBase64 = {
                type = "method",
                description = "Open a base 64 formatted stream for writing (i.e. encode bytes to base64).\n\n–> MOAIStreamWriter self\n–> MOAIStream target\n<– boolean success",
                args = "(MOAIStreamWriter self, MOAIStream target)",
                returns = "boolean success",
                valuetype = "boolean"
            },
            openDeflate = {
                type = "method",
                description = "Open a 'deflate' formatted stream for writing (i.e. compress bytes using the 'deflate' algorithm).\n\n–> MOAIStreamWriter self\n–> MOAIStream target\n[–> number level: The level used in the DEFLATE algorithm.]\n[–> number windowBits: The window bits used in the DEFLATE algorithm.]\n<– boolean success",
                args = "(MOAIStreamWriter self, MOAIStream target, [number level, [number windowBits]])",
                returns = "boolean success",
                valuetype = "boolean"
            }
        }
    },
    MOAIStretchPatch2D = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Moai implementation of a 9-patch. Textured quad with any number of stretchable and non-stretchable 'bands.' Grid drawing not supported.",
        childs = {
            reserveColumns = {
                type = "method",
                description = "Reserve total columns in patch.\n\n–> MOAIStretchPatch2D self\n–> number nColumns\n<– nil",
                args = "(MOAIStretchPatch2D self, number nColumns)",
                returns = "nil"
            },
            reserveRows = {
                type = "method",
                description = "Reserve total rows in patch.\n\n–> MOAIStretchPatch2D self\n–> number nRows\n<– nil",
                args = "(MOAIStretchPatch2D self, number nRows)",
                returns = "nil"
            },
            reserveUVRects = {
                type = "method",
                description = "Reserve total UV rects in patch. When a patch is indexed it will change its UV rects.\n\n–> MOAIStretchPatch2D self\n–> number nUVRects\n<– nil",
                args = "(MOAIStretchPatch2D self, number nUVRects)",
                returns = "nil"
            },
            setColumn = {
                type = "method",
                description = "Set the stretch properties of a patch column.\n\n–> MOAIStretchPatch2D self\n–> number idx\n–> number weight\n–> boolean conStretch\n<– nil",
                args = "(MOAIStretchPatch2D self, number idx, number weight, boolean conStretch)",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set the model space dimensions of the patch.\n\n–> MOAIStretchPatch2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIStretchPatch2D self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            setRow = {
                type = "method",
                description = "Set the stretch properties of a patch row.\n\n–> MOAIStretchPatch2D self\n–> number idx\n–> number weight\n–> boolean conStretch\n<– nil",
                args = "(MOAIStretchPatch2D self, number idx, number weight, boolean conStretch)",
                returns = "nil"
            },
            setUVRect = {
                type = "method",
                description = "Set the UV space dimensions of the patch.\n\n–> MOAIStretchPatch2D self\n–> number idx\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAIStretchPatch2D self, number idx, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            }
        }
    },
    MOAISurfaceDeck2D = {
        type = "class",
        inherits = "MOAIDeck",
        description = "Deck of surface edge lists. Unused in this version of Moai.",
        childs = {
            reserveSurfaceLists = {
                type = "method",
                description = "Reserve surface lists for deck.\n\n–> MOAISurfaceDeck2D self\n–> number nLists\n<– nil",
                args = "(MOAISurfaceDeck2D self, number nLists)",
                returns = "nil"
            },
            reserveSurfaces = {
                type = "method",
                description = "Reserve surfaces for a given list in deck.\n\n–> MOAISurfaceDeck2D self\n–> number idx\n–> number nSurfaces\n<– nil",
                args = "(MOAISurfaceDeck2D self, number idx, number nSurfaces)",
                returns = "nil"
            },
            setSurface = {
                type = "method",
                description = "Set a surface in a surface list.\n\n–> MOAISurfaceDeck2D self\n–> number idx\n–> number surfaceIdx\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n<– nil",
                args = "(MOAISurfaceDeck2D self, number idx, number surfaceIdx, number x0, number y0, number x1, number y1)",
                returns = "nil"
            }
        }
    },
    MOAITapjoyAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for Tapjoy integration on Android devices. Tapjoy provides a turnkey advertising platform that delivers cost-effective, high-value new users and helps apps make money. Exposed to Lua via MOAITapjoy on all mobile platforms.",
        childs = {
            TAPJOY_VIDEO_AD_BEGIN = {
                type = "value",
                description = "Event code for Tapjoy video ad playback begin. Unused."
            },
            TAPJOY_VIDEO_AD_CLOSE = {
                type = "value",
                description = "Event code for Tapjoy video ad playback completion."
            },
            TAPJOY_VIDEO_AD_ERROR = {
                type = "value",
                description = "Event code for Tapjoy video ad playback errors."
            },
            TAPJOY_VIDEO_AD_READY = {
                type = "value",
                description = "Event code for Tapjoy video ad playback availability."
            },
            TAPJOY_VIDEO_STATUS_MEDIA_STORAGE_UNAVAILABLE = {
                type = "value",
                description = "Error code for inadequate storage for video ad."
            },
            TAPJOY_VIDEO_STATUS_NETWORK_ERROR_ON_INIT_VIDEOS = {
                type = "value",
                description = "Error code for network error."
            },
            TAPJOY_VIDEO_STATUS_NO_ERROR = {
                type = "value",
                description = "Error code for success."
            },
            TAPJOY_VIDEO_STATUS_UNABLE_TO_PLAY_VIDEO = {
                type = "value",
                description = "Error code for playback error."
            },
            getUserId = {
                type = "function",
                description = "Gets the Tapjoy user ID.\n\n<– string userId",
                args = "()",
                returns = "string userId",
                valuetype = "string"
            },
            init = {
                type = "function",
                description = "Initializes Tapjoy.\n\n–> string appId: Available in Tapjoy dashboard settings.\n–> string secretKey: Available in Tapjoy dashboard settings.\n<– nil",
                args = "(string appId, string secretKey)",
                returns = "nil"
            },
            initVideoAds = {
                type = "function",
                description = "Initializes Tapjoy to display video ads.\n\n[–> number count: The optional number of ads to cache. Default is Tapjoy dependent.]\n<– nil",
                args = "[number count]",
                returns = "nil"
            },
            showOffers = {
                type = "function",
                description = "Displays the Tapjoy marketplace.\n\n<– nil",
                args = "()",
                returns = "nil"
            }
        }
    },
    MOAITapjoyIOS = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for Tapjoy integration on iOS devices. Tapjoy provides a turnkey advertising platform that delivers cost-effective, high-value new users and helps apps make money. Exposed to Lua via MOAITapjoy on all mobile platforms.",
        childs = {
            TAPJOY_VIDEO_AD_BEGIN = {
                type = "value",
                description = "Event code for Tapjoy video ad playback begin."
            },
            TAPJOY_VIDEO_AD_CLOSE = {
                type = "value",
                description = "Event code for Tapjoy video ad playback completion."
            },
            TAPJOY_VIDEO_AD_ERROR = {
                type = "value",
                description = "Event code for Tapjoy video ad playback errors. Unused."
            },
            TAPJOY_VIDEO_AD_READY = {
                type = "value",
                description = "Event code for Tapjoy video ad playback availability. Unused."
            },
            TAPJOY_VIDEO_STATUS_MEDIA_STORAGE_UNAVAILABLE = {
                type = "value",
                description = "Error code for inadequate storage for video ad. Unused."
            },
            TAPJOY_VIDEO_STATUS_NETWORK_ERROR_ON_INIT_VIDEOS = {
                type = "value",
                description = "Error code for network error. Unused."
            },
            TAPJOY_VIDEO_STATUS_NO_ERROR = {
                type = "value",
                description = "Error code for success. Unused."
            },
            TAPJOY_VIDEO_STATUS_UNABLE_TO_PLAY_VIDEO = {
                type = "value",
                description = "Error code for playback error. Unused."
            }
        }
    },
    MOAITextBox = {
        type = "class",
        inherits = "MOAIProp MOAIAction",
        description = "The text box manages styling, laying out and displaying text. You can attach named styles to the text box to be applied to the text using style escapes. You can also inline style escapes to control color. Style escapes may be nested.\nTo attach a style to a text box use setStyle (). If you provide a name for the style then the style may be applied by name using a style escape. If you do not provide a name then the style will be used as the default style for the text box. The default style is the style that will be used when no style escapes are in effect.\nThe setFont () and setSize () methods are helpers that operate on the text box's default style. If no default style exists when these methods are called, one will be created.\nThere are three kinds of text escapes. The first takes the form of <styleName> where 'styleName' is the name of the style you provided via setStyle (). If there is no match for the name then the default style will be used.\nThe second form of style escape sets text color. It takes the form of <c:XXX> where 'XXX' is a hexadecimal number representing a color value. The hexadecimal number may be have from one up to eight digits, excluding five digit numbers. One and two digit numbers correspond to grayscale values of 4 and 8 bits of precision (16 or 256 levels) respectively. Three and four digit numbers represent RGB and RGBA colors at 4 bit precision. Six digits is RGB at 8 bits of precision. Seven digits is RGBA with 8 bits for RGB and 4 bits for A. Eight digits is RGBA with 8 bits for each component.\nThe final text escapes ends the current escape. It takes the form of </>. Including any additional text in this kind of escape is an error.\nYou may escape the '<' symbol itself by using an additional '<'. For example, '<<' will output '<'. '<<test>' will short circuit the style escape and output '<test>' in the displayed text.\nWhen using MOAITextBox with MOAIFont it's important to understand how and when glyphs are rendered. When you call setText () the text box's style goes to work. The entire string you provide is scanned and a 'style span' is created for each uniquely styled block of text. If you do not use any styles then there will be only one style span.\nOnce the text style has created style spans for your text, the spans themselves are scanned. Each span must specify a font to be used. All of the characters in the span are 'affirmed' by the font: if the glyphs for the characters have already been ripped then nothing happens. If not, the characters are enqueued by the font to have their glyphs ripped.\nFinally, we iterate through all of the fonts used by the text and instruct them to load and render any pending glyphs. If the font is dynamic and has a valid implementation of MOAIFontReader and MOAIGlyphCache attached to it then the glyphs will be rendered and placed in the cache.\nOnce the glyphs have been rendered, we know their metrics and (hopefully) have valid textures for them. We can now lay out an actual page of text. This is done by a separate subsystem known as the text designer. The text designer reads the style spans and uses the associated font, color and size information to place the glyphs into a layout.\nIf the text associated with the textbox doesn't fit, then the textbox will have multiple pages. The only method that deals with pages at this time is nextPage (). Additional methods giving finer control over multi-page text boxes will be provided in a future release.\nThere are some additional ways you can use the text box to style your text. The current implementation supports left, center and right positioning as well as top, center and bottom positioning. A future implementation will include justification in which words and lines of text will be spaced out to align with the edges of the text box.\nYou can also attach MOAIAnimCurves to the text box. The animation curves may be used to offset characters in lines of text. Each curve may have any number of keyframes, but only the span between t0 and t1 is used by the text box, regardless of its width. Curves correspond to lines of text. If there are more lines of text than curves, the curves will simply repeat.\nOnce you've loaded text into the text box you can apply highlight colors. These colors will override any colors specified by style escapes. Highlight spans may be set or cleared using setHighlight (). clearHighlights () will remove all highlights from the text. Highlights will persists from page to page of text, but will be lost if new text is loaded by calling setText ().",
        childs = {
            CENTER_JUSTIFY = {
                type = "value"
            },
            LEFT_JUSTIFY = {
                type = "value"
            },
            RIGHT_JUSTIFY = {
                type = "value"
            },
            affirmStyle = {
                type = "method",
                description = "Returns the textbox's default style. If no default style exists, creates an empty style, sets it as the default and returns it.\n\n–> MOAITextBox self\n<– MOAITextStyle style",
                args = "MOAITextBox self",
                returns = "MOAITextStyle style",
                valuetype = "MOAITextStyle"
            },
            clearHighlights = {
                type = "method",
                description = "Removes all highlights currently associated with the text box.\n\n–> MOAITextBox self\n<– nil",
                args = "MOAITextBox self",
                returns = "nil"
            },
            getAlignment = {
                type = "method",
                description = "Returns the alignment of the text\n\n–> MOAITextBox self\n<– number hAlign: horizontal alignment\n<– number vAlign: vertical alignment",
                args = "MOAITextBox self",
                returns = "(number hAlign, number vAlign)",
                valuetype = "number"
            },
            getGlyphScale = {
                type = "method",
                description = "Returns the current glyph scale.\n\n–> MOAITextBox self\n<– number glyphScale",
                args = "MOAITextBox self",
                returns = "number glyphScale",
                valuetype = "number"
            },
            getLineSpacing = {
                type = "method",
                description = "Returns the spacing between lines (in pixels).\n\n–> MOAITextBox self\n<– number lineScale: The size of the spacing in pixels.",
                args = "MOAITextBox self",
                returns = "number lineScale",
                valuetype = "number"
            },
            getRect = {
                type = "method",
                description = "Returns the two-dimensional boundary of the text box.\n\n–> MOAITextBox self\n<– number xMin\n<– number yMin\n<– number xMax\n<– number yMax",
                args = "MOAITextBox self",
                returns = "(number xMin, number yMin, number xMax, number yMax)",
                valuetype = "number"
            },
            getStringBounds = {
                type = "method",
                description = "Returns the bounding rectangle of a given substring on a single line in the local space of the text box.\n\n–> MOAITextBox self\n–> number index: Index of the first character in the substring.\n–> number size: Length of the substring.\n<– number xMin: Edge of rect or 'nil' is no match found.\n<– number yMin: Edge of rect or 'nil' is no match found.\n<– number xMax: Edge of rect or 'nil' is no match found.\n<– number yMax: Edge of rect or 'nil' is no match found.",
                args = "(MOAITextBox self, number index, number size)",
                returns = "(number xMin, number yMin, number xMax, number yMax)",
                valuetype = "number"
            },
            getStyle = {
                type = "method",
                description = "Returns the style associated with a name or, if no name is given, returns the default style.\n\nOverload:\n–> MOAITextBox self\n<– MOAITextStyle defaultStyle\n\nOverload:\n–> MOAITextBox self\n–> string styleName\n<– MOAITextStyle style",
                args = "(MOAITextBox self, [string styleName])",
                returns = "(MOAITextStyle defaultStyle | MOAITextStyle style)",
                valuetype = "MOAITextStyle"
            },
            more = {
                type = "method",
                description = "Returns whether there are additional pages of text below the cursor position that are not visible on the screen.\n\n–> MOAITextBox self\n<– boolean isMore: If there is additional text below the cursor that is not visible on the screen due to clipping.",
                args = "MOAITextBox self",
                returns = "boolean isMore",
                valuetype = "boolean"
            },
            nextPage = {
                type = "method",
                description = "Advances to the next page of text (if any) or wraps to the start of the text (if at end).\n\n–> MOAITextBox self\n[–> boolean reveal: Default is true]\n<– nil",
                args = "(MOAITextBox self, [boolean reveal])",
                returns = "nil"
            },
            reserveCurves = {
                type = "method",
                description = "Reserves a set of IDs for animation curves to be binding to this text object. See setCurves.\n\n–> MOAITextBox self\n–> number nCurves\n<– nil",
                args = "(MOAITextBox self, number nCurves)",
                returns = "nil"
            },
            revealAll = {
                type = "method",
                description = "Displays as much text as will fit in the text box.\n\n–> MOAITextBox self\n<– nil",
                args = "MOAITextBox self",
                returns = "nil"
            },
            setAlignment = {
                type = "method",
                description = "Sets the horizontal and/or vertical alignment of the text in the text box.\n\n–> MOAITextBox self\n–> number hAlignment: Can be one of LEFT_JUSTIFY, CENTER_JUSTIFY or RIGHT_JUSTIFY.\n–> number vAlignment: Can be one of LEFT_JUSTIFY, CENTER_JUSTIFY or RIGHT_JUSTIFY.\n<– nil",
                args = "(MOAITextBox self, number hAlignment, number vAlignment)",
                returns = "nil"
            },
            setCurve = {
                type = "method",
                description = "Binds an animation curve to the text, where the Y value of the curve indicates the text offset, or clears the curves.\n\nOverload:\n–> MOAITextBox self\n–> number curveID: The ID of the curve within this text object.\n–> MOAIAnimCurve curve: The MOAIAnimCurve to bind to.\n<– nil\n\nOverload:\n–> MOAITextBox self\n<– nil",
                args = "(MOAITextBox self, [number curveID, MOAIAnimCurve curve])",
                returns = "nil"
            },
            setFont = {
                type = "method",
                description = "Sets the font to be used by the textbox's default style. If no default style exists, a default style is created.\n\n–> MOAITextBox self\n–> MOAIFont font\n<– nil",
                args = "(MOAITextBox self, MOAIFont font)",
                returns = "nil"
            },
            setGlyphScale = {
                type = "method",
                description = "Sets the glyph scale. This is a scalar applied to glyphs as they are positioned in the text box.\n\n–> MOAITextBox self\n[–> number glyphScale: Default value is 1.]\n<– number glyphScale",
                args = "(MOAITextBox self, [number glyphScale])",
                returns = "number glyphScale",
                valuetype = "number"
            },
            setHighlight = {
                type = "method",
                description = "Set or clear the highlight color of a sub string in the text. Only affects text displayed on the current page. Highlight will automatically clear when layout or page changes.\n\nOverload:\n–> MOAITextBox self\n–> number index: Index of the first character in the substring.\n–> number size: Length of the substring.\n–> number r\n–> number g\n–> number b\n[–> number a: Default value is 1.]\n<– nil\n\nOverload:\n–> MOAITextBox self\n–> number index: Index of the first character in the substring.\n–> number size: Length of the substring.\n<– nil",
                args = "(MOAITextBox self, number index, number size, [number r, number g, number b, [number a]])",
                returns = "nil"
            },
            setLineSpacing = {
                type = "method",
                description = "Sets additional space between lines in text units. '0' uses the default spacing. Values must be positive.\n\n–> MOAITextBox self\n–> number lineSpacing: Default value is 0.\n<– nil",
                args = "(MOAITextBox self, number lineSpacing)",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Sets the rectangular area for this text box.\n\n–> MOAITextBox self\n–> number x1: The X coordinate of the rect's upper-left point.\n–> number y1: The Y coordinate of the rect's upper-left point.\n–> number x2: The X coordinate of the rect's lower-right point.\n–> number y2: The Y coordinate of the rect's lower-right point.\n<– nil",
                args = "(MOAITextBox self, number x1, number y1, number x2, number y2)",
                returns = "nil"
            },
            setReveal = {
                type = "method",
                description = "Sets the number of renderable characters to be shown. Can range from 0 to any value; values greater than the number of renderable characters in the current text will be ignored.\n\n–> MOAITextBox self\n–> number reveal: The number of renderable characters (i.e. not whitespace) to be shown.\n<– nil",
                args = "(MOAITextBox self, number reveal)",
                returns = "nil"
            },
            setSnapToViewportScale = {
                type = "method",
                description = "If set to true text positions will snap to integers according to the viewport scale. Default value is true.\n\n–> MOAITextBox self\n–> boolean snap: Whether text positions should snap to viewport scale\n<– nil",
                args = "(MOAITextBox self, boolean snap)",
                returns = "nil"
            },
            setSpeed = {
                type = "method",
                description = "Sets the base spool speed used when creating a spooling MOAIAction with the spool() function.\n\n–> MOAITextBox self\n–> number speed: The base spooling speed.\n<– nil",
                args = "(MOAITextBox self, number speed)",
                returns = "nil"
            },
            setString = {
                type = "method",
                description = "Sets the text string to be displayed by this textbox.\n\n–> MOAITextBox self\n–> string newStr: The new text string to be displayed.\n<– nil",
                args = "(MOAITextBox self, string newStr)",
                returns = "nil"
            },
            setStyle = {
                type = "method",
                description = "Attaches a style to the textbox and associates a name with it. If no name is given, sets the default style.\n\nOverload:\n–> MOAITextBox self\n–> MOAITextStyle defaultStyle\n<– nil\n\nOverload:\n–> MOAITextBox self\n–> string styleName\n–> MOAITextStyle style\n<– nil",
                args = "(MOAITextBox self, (MOAITextStyle defaultStyle | (string styleName, MOAITextStyle style)))",
                returns = "nil"
            },
            setTextSize = {
                type = "method",
                description = "Sets the size to be used by the textbox's default style. If no default style exists, a default style is created.\n\n–> MOAITextBox self\n–> number points: The point size to be used by the default style.\n[–> number dpi: The device DPI (dots per inch of device screen). Default value is 72 (points same as pixels).]\n<– nil",
                args = "(MOAITextBox self, number points, [number dpi])",
                returns = "nil"
            },
            setWordBreak = {
                type = "method",
                description = "Sets the rule for breaking words across lines.\n\n–> MOAITextBox self\n[–> number rule: One of MOAITextBox.WORD_BREAK_NONE, MOAITextBox.WORD_BREAK_CHAR. Default is MOAITextBox.WORD_BREAK_NONE.]\n<– nil",
                args = "(MOAITextBox self, [number rule])",
                returns = "nil"
            },
            setYFlip = {
                type = "method",
                description = "Sets the rendering direction for the text. Default assumes a window style screen space (positive Y moves down the screen). Set to true to render text for world style coordinate systems (positive Y moves up the screen).\n\n–> MOAITextBox self\n–> boolean yFlip: Whether the vertical rendering direction should be inverted.\n<– nil",
                args = "(MOAITextBox self, boolean yFlip)",
                returns = "nil"
            },
            spool = {
                type = "method",
                description = "Creates a new MOAIAction which when run has the effect of increasing the amount of characters revealed from 0 to the length of the string currently set. The spool action is automatically added to the root of the action tree, but may be reparented or stopped by the developer. This function also automatically sets the current number of revealed characters to 0 (i.e. MOAITextBox:setReveal(0)).\n\n–> MOAITextBox self\n–> number yFlip: Whether the vertical rendering direction should be inverted.\n<– MOAIAction action: The new MOAIAction which spools the text when run.",
                args = "(MOAITextBox self, number yFlip)",
                returns = "MOAIAction action",
                valuetype = "MOAIAction"
            }
        }
    },
    MOAITextBundle = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "A read-only lookup table of strings suitable for internationalization purposes. This currently wraps a loaded gettext() style MO file (see http://www.gnu.org/software/gettext/manual/gettext.html). So you are going to want to generate the .mo file from one of the existing tools such as poedit or msgfmt, and then load that file using this class. Then you can lookup strings using MOAITextBundle->Lookup().",
        childs = {
            load = {
                type = "method",
                description = "Load a text bundle from a .mo file.\n\nOverload:\n–> MOAITextBundle self\n–> MOAIDataBuffer buffer: A MOAIDataBuffer containing the text bundle.\n<– number size: The number of bytes in this data buffer object.\n\nOverload:\n–> MOAITextBundle self\n–> string filename: The filename to load.\n<– number size: The number of bytes in this data buffer object.",
                args = "(MOAITextBundle self, (MOAIDataBuffer buffer | string filename))",
                returns = "number size",
                valuetype = "number"
            },
            lookup = {
                type = "method",
                description = 'Look up a string in the bundle (defaulting to the lookup string itself). In the case of defaulting, a false value is returned as the second value (useful for falling back to less-specific bundles if desirable).\n\n–> MOAITextBundle self\n–> string key: A text string to use as a "key"\n<– string value: The value if found, otherwise it returns the original string if not found.\n<– boolean found: True if the string was found in the table (regardless of returned value), or false if it couldn\'t be found.',
                args = "(MOAITextBundle self, string key)",
                returns = "(string value, boolean found)",
                valuetype = "string"
            }
        }
    },
    MOAITextStyle = {
        type = "class",
        inherits = "MOAINode MOAITextStyleState",
        description = "Represents a style that may be applied to a text box or a section of text in a text box using a style escape.",
        childs = {
            getColor = {
                type = "method",
                description = "Gets the color of the style.\n\n–> MOAITextStyle self\n<– number r\n<– number g\n<– number b\n<– number a",
                args = "MOAITextStyle self",
                returns = "(number r, number g, number b, number a)",
                valuetype = "number"
            },
            getFont = {
                type = "method",
                description = "Gets the font of the style.\n\n–> MOAITextStyle self\n<– MOAIFont font",
                args = "MOAITextStyle self",
                returns = "MOAIFont font",
                valuetype = "MOAIFont"
            },
            getScale = {
                type = "method",
                description = "Gets the scale of the style.\n\n–> MOAITextStyle self\n<– number scale",
                args = "MOAITextStyle self",
                returns = "number scale",
                valuetype = "number"
            },
            getSize = {
                type = "method",
                description = "Gets the size of the style.\n\n–> MOAITextStyle self\n<– number size",
                args = "MOAITextStyle self",
                returns = "number size",
                valuetype = "number"
            },
            setColor = {
                type = "method",
                description = "Initialize the style's color.\n\n–> MOAITextStyle self\n–> number r: Default value is 0.\n–> number g: Default value is 0.\n–> number b: Default value is 0.\n[–> number a: Default value is 1.]\n<– nil",
                args = "(MOAITextStyle self, number r, number g, number b, [number a])",
                returns = "nil"
            },
            setFont = {
                type = "method",
                description = "Sets or clears the style's font.\n\n–> MOAITextStyle self\n[–> MOAIFont font: Default value is nil.]\n<– nil",
                args = "(MOAITextStyle self, [MOAIFont font])",
                returns = "nil"
            },
            setScale = {
                type = "method",
                description = "Sets the scale of the style. The scale is applied to any glyphs drawn using the style after the glyph set has been selected by size.\n\n–> MOAITextStyle self\n[–> number scale: Default value is 1.]\n<– nil",
                args = "(MOAITextStyle self, [number scale])",
                returns = "nil"
            },
            setSize = {
                type = "method",
                description = "Sets or clears the style's size.\n\n–> MOAITextStyle self\n–> number points: The point size to be used by the style.\n[–> number dpi: The device DPI (dots per inch of device screen). Default value is 72 (points same as pixels).]\n<– nil",
                args = "(MOAITextStyle self, number points, [number dpi])",
                returns = "nil"
            }
        }
    },
    MOAITextStyleState = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    MOAITexture = {
        type = "class",
        inherits = "MOAITextureBase",
        description = "Texture class.",
        childs = {
            load = {
                type = "method",
                description = "Loads a texture from a data buffer or a file. Optionally pass in an image transform (not applicable to PVR textures).\n\nOverload:\n–> MOAITexture self\n–> string filename\n[–> number transform: Any bitwise combination of MOAIImage.QUANTIZE, MOAIImage.TRUECOLOR, MOAIImage.PREMULTIPLY_ALPHA]\n[–> string debugname: Name used when reporting texture debug information]\n<– nil\n\nOverload:\n–> MOAITexture self\n–> MOAIImage image\n[–> string debugname: Name used when reporting texture debug information]\n<– nil\n\nOverload:\n–> MOAITexture self\n–> MOAIDataBuffer buffer\n[–> number transform: Any bitwise combination of MOAIImage.QUANTIZE, MOAIImage.TRUECOLOR, MOAIImage.PREMULTIPLY_ALPHA]\n[–> string debugname: Name used when reporting texture debug information]\n<– nil\n\nOverload:\n–> MOAITexture self\n–> MOAIStream buffer\n[–> number transform: Any bitwise combination of MOAIImage.QUANTIZE, MOAIImage.TRUECOLOR, MOAIImage.PREMULTIPLY_ALPHA]\n[–> string debugname: Name used when reporting texture debug information]\n<– nil"
            }
        }
    },
    MOAITextureBase = {
        type = "class",
        inherits = "MOAILuaObject MOAIGfxResource",
        description = "Base class for texture resources.",
        childs = {
            GL_LINEAR = {
                type = "value"
            },
            GL_LINEAR_MIPMAP_LINEAR = {
                type = "value"
            },
            GL_LINEAR_MIPMAP_NEAREST = {
                type = "value"
            },
            GL_NEAREST = {
                type = "value"
            },
            GL_NEAREST_MIPMAP_LINEAR = {
                type = "value"
            },
            GL_NEAREST_MIPMAP_NEAREST = {
                type = "value"
            },
            getSize = {
                type = "method",
                description = "Returns the width and height of the texture's source image. Avoid using the texture width and height to compute UV coordinates from pixels, as this will prevent texture resolution swapping.\n\n–> MOAITextureBase self\n<– number width\n<– number height",
                args = "MOAITextureBase self",
                returns = "(number width, number height)",
                valuetype = "number"
            },
            release = {
                type = "method",
                description = "Releases any memory associated with the texture.\n\n–> MOAITextureBase self\n<– nil",
                args = "MOAITextureBase self",
                returns = "nil"
            },
            setFilter = {
                type = "method",
                description = "Set default filtering mode for texture.\n\n–> MOAITextureBase self\n–> number min: One of MOAITextureBase.GL_LINEAR, MOAITextureBase.GL_LINEAR_MIPMAP_LINEAR, MOAITextureBase.GL_LINEAR_MIPMAP_NEAREST, MOAITextureBase.GL_NEAREST, MOAITextureBase.GL_NEAREST_MIPMAP_LINEAR, MOAITextureBase.GL_NEAREST_MIPMAP_NEAREST\n[–> number mag: Defaults to value passed to 'min'.]\n<– nil",
                args = "(MOAITextureBase self, number min, [number mag])",
                returns = "nil"
            },
            setWrap = {
                type = "method",
                description = "Set wrapping mode for texture.\n\n–> MOAITextureBase self\n–> boolean wrap: Texture will wrap if true, clamp if not.\n<– nil",
                args = "(MOAITextureBase self, boolean wrap)",
                returns = "nil"
            }
        }
    },
    MOAITileDeck2D = {
        type = "class",
        inherits = "MOAIDeck MOAIGridSpace",
        description = "Subdivides a single texture into uniform tiles enumerated from the texture's left top to right bottom.",
        childs = {
            setQuad = {
                type = "method",
                description = "Set model space quad. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAITileDeck2D self\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAITileDeck2D self, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setRect = {
                type = "method",
                description = "Set the model space dimensions of a single tile. When grid drawing, this should be a unit rect centered at the origin for tiles that fit each grid cell. Growing or shrinking the rect will cause tiles to overlap or leave gaps between them.\n\n–> MOAITileDeck2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAITileDeck2D self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            setSize = {
                type = "method",
                description = "Controls how the texture is subdivided into tiles. Default behavior is to subdivide the texture into N by M tiles, but is tile dimensions are provided (in UV space) then the resulting tile set will be N * tileWidth by M * tileHeight in UV space. This means the tile set does not have to fill all of the texture. The upper left hand corner of the tile set will always be at UV 0, 0.\n\n–> MOAITileDeck2D self\n–> number width: Width of the tile deck in tiles.\n–> number height: Height of the tile deck in tiles.\n[–> number cellWidth: Width of individual tile in UV space. Defaults to 1 / width.]\n[–> number cellHeight: Height of individual tile in UV space. Defaults to 1 / height.]\n[–> number xOff: X offset of the tile from the cell. Defaults to 0.]\n[–> number yOff: Y offset of the tile from the cell. Defaults to 0.]\n[–> number tileWidth: Default value is cellWidth.]\n[–> number tileHeight: Default value is cellHeight.]\n<– nil",
                args = "(MOAITileDeck2D self, number width, number height, [number cellWidth, [number cellHeight, [number xOff, [number yOff, [number tileWidth, [number tileHeight]]]]]])",
                returns = "nil"
            },
            setUVQuad = {
                type = "method",
                description = "Set the UV space dimensions of the quad. Vertex order is clockwise from upper left (xMin, yMax)\n\n–> MOAITileDeck2D self\n–> number x0\n–> number y0\n–> number x1\n–> number y1\n–> number x2\n–> number y2\n–> number x3\n–> number y3\n<– nil",
                args = "(MOAITileDeck2D self, number x0, number y0, number x1, number y1, number x2, number y2, number x3, number y3)",
                returns = "nil"
            },
            setUVRect = {
                type = "method",
                description = "Set the UV space dimensions of the quad.\n\n–> MOAITileDeck2D self\n–> number xMin\n–> number yMin\n–> number xMax\n–> number yMax\n<– nil",
                args = "(MOAITileDeck2D self, number xMin, number yMin, number xMax, number yMax)",
                returns = "nil"
            },
            transform = {
                type = "method",
                description = "Apply the given MOAITransform to all the vertices in the deck.\n\n–> MOAITileDeck2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAITileDeck2D self, MOAITransform transform)",
                returns = "nil"
            },
            transformUV = {
                type = "method",
                description = "Apply the given MOAITransform to all the uv coordinates in the deck.\n\n–> MOAITileDeck2D self\n–> MOAITransform transform\n<– nil",
                args = "(MOAITileDeck2D self, MOAITransform transform)",
                returns = "nil"
            }
        }
    },
    MOAITimer = {
        type = "class",
        inherits = "MOAINode MOAIAction",
        description = "Timer class for driving curves and animations.",
        childs = {
            ATTR_TIME = {
                type = "value"
            },
            CONTINUE = {
                type = "value"
            },
            CONTINUE_REVERSE = {
                type = "value"
            },
            EVENT_TIMER_BEGIN_SPAN = {
                type = "value",
                description = "Called when timer starts or after roll over (if looping). Signature is: nil onBeginSpan ( MOAITimer self, number timesExecuted )"
            },
            EVENT_TIMER_END_SPAN = {
                type = "value",
                description = "Called when timer ends or before roll over (if looping). Signature is: nil onEndSpan ( MOAITimer self, number timesExecuted )"
            },
            EVENT_TIMER_KEYFRAME = {
                type = "value",
                description = "ID of event stop callback. Signature is: nil onKeyframe ( MOAITimer self, number keyframe, number timesExecuted, number time, number value )"
            },
            EVENT_TIMER_LOOP = {
                type = "value",
                description = "ID of event loop callback. Signature is: nil onLoop ( MOAITimer self, number timesExecuted )"
            },
            LOOP = {
                type = "value"
            },
            LOOP_REVERSE = {
                type = "value"
            },
            NORMAL = {
                type = "value"
            },
            PING_PONG = {
                type = "value"
            },
            REVERSE = {
                type = "value"
            },
            getSpeed = {
                type = "method",
                description = "Return the playback speed.\n\n–> MOAITimer self\n<– number speed",
                args = "MOAITimer self",
                returns = "number speed",
                valuetype = "number"
            },
            getTime = {
                type = "method",
                description = "Return the current time.\n\n–> MOAITimer self\n<– number time",
                args = "MOAITimer self",
                returns = "number time",
                valuetype = "number"
            },
            getTimesExecuted = {
                type = "method",
                description = "Gets the number of times the timer has completed a cycle.\n\n–> MOAITimer self\n<– number nTimes",
                args = "MOAITimer self",
                returns = "number nTimes",
                valuetype = "number"
            },
            setCurve = {
                type = "method",
                description = "Set or clear the curve to use for event generation.\n\n–> MOAITimer self\n[–> MOAIAnimCurve curve: Default value is nil.]\n<– nil",
                args = "(MOAITimer self, [MOAIAnimCurve curve])",
                returns = "nil"
            },
            setMode = {
                type = "method",
                description = "Sets the playback mode of the timer.\n\n–> MOAITimer self\n–> number mode: One of: MOAITimer.NORMAL, MOAITimer.REVERSE, MOAITimer.LOOP, MOAITimer.LOOP_REVERSE, MOAITimer.PING_PONG\n<– nil",
                args = "(MOAITimer self, number mode)",
                returns = "nil"
            },
            setSpan = {
                type = "method",
                description = "Sets the playback mode of the timer.\n\nOverload:\n–> MOAITimer self\n–> number endTime\n<– nil\n\nOverload:\n–> MOAITimer self\n–> number startTime\n–> number endTime\n<– nil",
                args = "(MOAITimer self, [number startTime])",
                returns = "nil"
            },
            setSpeed = {
                type = "method",
                description = "Sets the playback speed. This affects only the timer, not its children in the action tree.\n\n–> MOAITimer self\n–> number speed\n<– nil",
                args = "(MOAITimer self, number speed)",
                returns = "nil"
            },
            setTime = {
                type = "method",
                description = "Manually set the current time. This will be wrapped into the current span.\n\n–> MOAITimer self\n[–> number time: Default value is 0.]\n<– nil",
                args = "(MOAITimer self, [number time])",
                returns = "nil"
            }
        }
    },
    MOAITouchSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Multitouch sensor. Tracks up to 16 simultaneous touches.",
        childs = {
            TOUCH_CANCEL = {
                type = "value"
            },
            TOUCH_DOWN = {
                type = "value"
            },
            TOUCH_MOVE = {
                type = "value"
            },
            TOUCH_UP = {
                type = "value"
            },
            down = {
                type = "method",
                description = "Checks to see if the screen was touched during the last iteration.\n\n–> MOAITouchSensor self\n[–> number idx: Index of touch to check.]\n<– boolean wasPressed",
                args = "(MOAITouchSensor self, [number idx])",
                returns = "boolean wasPressed",
                valuetype = "boolean"
            },
            getActiveTouches = {
                type = "method",
                description = "Returns the IDs of all of the touches currently occurring (for use with getTouch).\n\n–> MOAITouchSensor self\n<– number idx1\n<– ...\n<– number idxN",
                args = "MOAITouchSensor self",
                returns = "(number idx1, ..., number idxN)",
                valuetype = "number"
            },
            getTouch = {
                type = "method",
                description = "Checks to see if there are currently touches being made on the screen.\n\n–> MOAITouchSensor self\n–> number id: The ID of the touch.\n<– number x\n<– number y\n<– number tapCount",
                args = "(MOAITouchSensor self, number id)",
                returns = "(number x, number y, number tapCount)",
                valuetype = "number"
            },
            hasTouches = {
                type = "method",
                description = "Checks to see if there are currently touches being made on the screen.\n\n–> MOAITouchSensor self\n<– boolean hasTouches",
                args = "MOAITouchSensor self",
                returns = "boolean hasTouches",
                valuetype = "boolean"
            },
            isDown = {
                type = "method",
                description = "Checks to see if the touch status is currently down.\n\n–> MOAITouchSensor self\n[–> number idx: Index of touch to check.]\n<– boolean isDown",
                args = "(MOAITouchSensor self, [number idx])",
                returns = "boolean isDown",
                valuetype = "boolean"
            },
            setAcceptCancel = {
                type = "method",
                description = "Sets whether or not to accept cancel events ( these happen on iOS backgrounding ), default value is false\n\n–> MOAITouchSensor self\n–> boolean accept: true then touch cancel events will be sent\n<– nil",
                args = "(MOAITouchSensor self, boolean accept)",
                returns = "nil"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued when the pointer location changes.\n\n–> MOAITouchSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAITouchSensor self, [function callback])",
                returns = "nil"
            },
            setTapMargin = {
                type = "method",
                description = "Sets maximum distance between two touches for them to be considered a tap\n\n–> MOAITouchSensor self\n–> number margin: Max difference on x and y between taps\n<– nil",
                args = "(MOAITouchSensor self, number margin)",
                returns = "nil"
            },
            setTapTime = {
                type = "method",
                description = "Sets the time between each touch for it to be counted as a tap\n\n–> MOAITouchSensor self\n–> number time: New time between taps\n<– nil",
                args = "(MOAITouchSensor self, number time)",
                returns = "nil"
            },
            up = {
                type = "method",
                description = "Checks to see if the screen was untouched (is no longer being touched) during the last iteration.\n\n–> MOAITouchSensor self\n[–> number idx: Index of touch to check.]\n<– boolean wasPressed",
                args = "(MOAITouchSensor self, [number idx])",
                returns = "boolean wasPressed",
                valuetype = "boolean"
            }
        }
    },
    MOAITransform = {
        type = "class",
        inherits = "MOAITransformBase",
        description = "Transformation hierarchy node.",
        childs = {
            ATTR_ROTATE_QUAT = {
                type = "value"
            },
            ATTR_TRANSLATE = {
                type = "value"
            },
            ATTR_X_LOC = {
                type = "value"
            },
            ATTR_X_PIV = {
                type = "value"
            },
            ATTR_X_ROT = {
                type = "value"
            },
            ATTR_X_SCL = {
                type = "value"
            },
            ATTR_Y_LOC = {
                type = "value"
            },
            ATTR_Y_PIV = {
                type = "value"
            },
            ATTR_Y_ROT = {
                type = "value"
            },
            ATTR_Y_SCL = {
                type = "value"
            },
            ATTR_Z_LOC = {
                type = "value"
            },
            ATTR_Z_PIV = {
                type = "value"
            },
            ATTR_Z_ROT = {
                type = "value"
            },
            ATTR_Z_SCL = {
                type = "value"
            },
            INHERIT_LOC = {
                type = "value"
            },
            INHERIT_TRANSFORM = {
                type = "value"
            },
            addLoc = {
                type = "method",
                description = "Adds a delta to the transform's location.\n\n–> MOAITransform self\n–> number xDelta\n–> number yDelta\n–> number zDelta\n<– nil",
                args = "(MOAITransform self, number xDelta, number yDelta, number zDelta)",
                returns = "nil"
            },
            addPiv = {
                type = "method",
                description = "Adds a delta to the transform's pivot.\n\n–> MOAITransform self\n–> number xDelta\n–> number yDelta\n–> number zDelta\n<– nil",
                args = "(MOAITransform self, number xDelta, number yDelta, number zDelta)",
                returns = "nil"
            },
            addRot = {
                type = "method",
                description = "Adds a delta to the transform's rotation\n\n–> MOAITransform self\n–> number xDelta: In degrees.\n–> number yDelta: In degrees.\n–> number zDelta: In degrees.\n<– nil",
                args = "(MOAITransform self, number xDelta, number yDelta, number zDelta)",
                returns = "nil"
            },
            addScl = {
                type = "method",
                description = "Adds a delta to the transform's scale\n\n–> MOAITransform self\n–> number xSclDelta\n[–> number ySclDelta: Default value is xSclDelta.]\n[–> number zSclDelta: Default value is 0.]\n<– nil",
                args = "(MOAITransform self, number xSclDelta, [number ySclDelta, [number zSclDelta]])",
                returns = "nil"
            },
            getLoc = {
                type = "method",
                description = "Returns the transform's current location.\n\n–> MOAITransform self\n<– number xLoc\n<– number yLoc\n<– number zLoc",
                args = "MOAITransform self",
                returns = "(number xLoc, number yLoc, number zLoc)",
                valuetype = "number"
            },
            getPiv = {
                type = "method",
                description = "Returns the transform's current pivot.\n\n–> MOAITransform self\n<– number xPiv\n<– number yPiv\n<– number zPiv",
                args = "MOAITransform self",
                returns = "(number xPiv, number yPiv, number zPiv)",
                valuetype = "number"
            },
            getRot = {
                type = "method",
                description = "Returns the transform's current rotation.\n\n–> MOAITransform self\n<– number xRot: Rotation in degrees.\n<– number yRot: Rotation in degrees.\n<– number zRot: Rotation in degrees.",
                args = "MOAITransform self",
                returns = "(number xRot, number yRot, number zRot)",
                valuetype = "number"
            },
            getScl = {
                type = "method",
                description = "Returns the transform's current scale.\n\n–> MOAITransform self\n<– number xScl\n<– number yScl\n<– number zScl",
                args = "MOAITransform self",
                returns = "(number xScl, number yScl, number zScl)",
                valuetype = "number"
            },
            modelToWorld = {
                type = "method",
                description = "Transform a point in model space to world space.\n\n–> MOAITransform self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> number z: Default value is 0.]\n<– number x\n<– number y\n<– number z",
                args = "(MOAITransform self, [number x, [number y, [number z]]])",
                returns = "(number x, number y, number z)",
                valuetype = "number"
            },
            move = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xDelta: Delta to be added to x.\n–> number yDelta: Delta to be added to y.\n–> number zDelta: Delta to be added to z.\n–> number xRotDelta: Delta to be added to x rot (in degrees).\n–> number yRotDelta: Delta to be added to y rot (in degrees).\n–> number zRotDelta: Delta to be added to z rot (in degrees).\n–> number xSclDelta: Delta to be added to x scale.\n–> number ySclDelta: Delta to be added to y scale.\n–> number zSclDelta: Delta to be added to z scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xDelta, number yDelta, number zDelta, number xRotDelta, number yRotDelta, number zRotDelta, number xSclDelta, number ySclDelta, number zSclDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            moveLoc = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xDelta: Delta to be added to x.\n–> number yDelta: Delta to be added to y.\n–> number zDelta: Delta to be added to z.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xDelta, number yDelta, number zDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            movePiv = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xDelta: Delta to be added to xPiv.\n–> number yDelta: Delta to be added to yPiv.\n–> number zDelta: Delta to be added to zPiv.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xDelta, number yDelta, number zDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            moveRot = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xDelta: Delta to be added to xRot (in degrees).\n–> number yDelta: Delta to be added to yRot (in degrees).\n–> number zDelta: Delta to be added to zRot (in degrees).\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xDelta, number yDelta, number zDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            moveScl = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xSclDelta: Delta to be added to x scale.\n–> number ySclDelta: Delta to be added to y scale.\n–> number zSclDelta: Delta to be added to z scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xSclDelta, number ySclDelta, number zSclDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seek = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xGoal: Desired resulting value for x.\n–> number yGoal: Desired resulting value for y.\n–> number zGoal: Desired resulting value for z.\n–> number xRotGoal: Desired resulting value for x rot (in degrees).\n–> number yRotGoal: Desired resulting value for y rot (in degrees).\n–> number zRotGoal: Desired resulting value for z rot (in degrees).\n–> number xSclGoal: Desired resulting value for x scale.\n–> number ySclGoal: Desired resulting value for y scale.\n–> number zSclGoal: Desired resulting value for z scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xGoal, number yGoal, number zGoal, number xRotGoal, number yRotGoal, number zRotGoal, number xSclGoal, number ySclGoal, number zSclGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekLoc = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xGoal: Desired resulting value for x.\n–> number yGoal: Desired resulting value for y.\n–> number zGoal: Desired resulting value for z.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xGoal, number yGoal, number zGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekPiv = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xGoal: Desired resulting value for xPiv.\n–> number yGoal: Desired resulting value for yPiv.\n–> number zGoal: Desired resulting value for zPiv.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xGoal, number yGoal, number zGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekRot = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xRotGoal: Desired resulting value for x rot (in degrees).\n–> number yRotGoal: Desired resulting value for y rot (in degrees).\n–> number zRotGoal: Desired resulting value for z rot (in degrees).\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xRotGoal, number yRotGoal, number zRotGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekScl = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform self\n–> number xSclGoal: Desired resulting value for x scale.\n–> number ySclGoal: Desired resulting value for y scale.\n–> number zSclGoal: Desired resulting value for z scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform self, number xSclGoal, number ySclGoal, number zSclGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            setLoc = {
                type = "method",
                description = "Sets the transform's location.\n\n–> MOAITransform self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> number z: Default value is 0.]\n<– nil",
                args = "(MOAITransform self, [number x, [number y, [number z]]])",
                returns = "nil"
            },
            setParent = {
                type = "method",
                description = "This method has been deprecated. Use MOAINode setAttrLink instead.\n\n–> MOAITransform self\n[–> MOAINode parent: Default value is nil.]\n<– nil",
                args = "(MOAITransform self, [MOAINode parent])",
                returns = "nil"
            },
            setPiv = {
                type = "method",
                description = "Sets the transform's pivot.\n\n–> MOAITransform self\n[–> number xPiv: Default value is 0.]\n[–> number yPiv: Default value is 0.]\n[–> number zPiv: Default value is 0.]\n<– nil",
                args = "(MOAITransform self, [number xPiv, [number yPiv, [number zPiv]]])",
                returns = "nil"
            },
            setRot = {
                type = "method",
                description = "Sets the transform's rotation.\n\n–> MOAITransform self\n[–> number xRot: Default value is 0.]\n[–> number yRot: Default value is 0.]\n[–> number zRot: Default value is 0.]\n<– nil",
                args = "(MOAITransform self, [number xRot, [number yRot, [number zRot]]])",
                returns = "nil"
            },
            setScl = {
                type = "method",
                description = "Sets the transform's scale.\n\n–> MOAITransform self\n–> number xScl\n[–> number yScl: Default value is xScl.]\n[–> number zScl: Default value is 1.]\n<– nil",
                args = "(MOAITransform self, number xScl, [number yScl, [number zScl]])",
                returns = "nil"
            },
            setShearByX = {
                type = "method",
                description = "Sets the shear for the Y and Z axes by X.\n\n–> MOAITransform self\n–> number yx: Default value is 0.\n[–> number zx: Default value is 0.]\n<– nil",
                args = "(MOAITransform self, number yx, [number zx])",
                returns = "nil"
            },
            setShearByY = {
                type = "method",
                description = "Sets the shear for the X and Z axes by Y.\n\n–> MOAITransform self\n–> number xy: Default value is 0.\n[–> number zy: Default value is 0.]\n<– nil",
                args = "(MOAITransform self, number xy, [number zy])",
                returns = "nil"
            },
            setShearByZ = {
                type = "method",
                description = "Sets the shear for the X and Y axes by Z.\n\n–> MOAITransform self\n–> number xz: Default value is 0.\n[–> number yz: Default value is 0.]\n<– nil",
                args = "(MOAITransform self, number xz, [number yz])",
                returns = "nil"
            },
            worldToModel = {
                type = "method",
                description = "Transform a point in world space to model space.\n\n–> MOAITransform self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n[–> number z: Default value is 0.]\n<– number x\n<– number y\n<– number z",
                args = "(MOAITransform self, [number x, [number y, [number z]]])",
                returns = "(number x, number y, number z)",
                valuetype = "number"
            }
        }
    },
    MOAITransform2D = {
        type = "class",
        inherits = "MOAITransformBase",
        description = "2D transformation hierarchy node.",
        childs = {
            ATTR_X_LOC = {
                type = "value"
            },
            ATTR_X_PIV = {
                type = "value"
            },
            ATTR_X_SCL = {
                type = "value"
            },
            ATTR_Y_LOC = {
                type = "value"
            },
            ATTR_Y_PIV = {
                type = "value"
            },
            ATTR_Y_SCL = {
                type = "value"
            },
            ATTR_Z_ROT = {
                type = "value"
            },
            INHERIT_LOC = {
                type = "value"
            },
            INHERIT_TRANSFORM = {
                type = "value"
            },
            addLoc = {
                type = "method",
                description = "Adds a delta to the transform's location.\n\n–> MOAITransform2D self\n–> number xDelta\n–> number yDelta\n<– nil",
                args = "(MOAITransform2D self, number xDelta, number yDelta)",
                returns = "nil"
            },
            addPiv = {
                type = "method",
                description = "Adds a delta to the transform's pivot.\n\n–> MOAITransform2D self\n–> number xDelta\n–> number yDelta\n<– nil",
                args = "(MOAITransform2D self, number xDelta, number yDelta)",
                returns = "nil"
            },
            addRot = {
                type = "method",
                description = "Adds a delta to the transform's rotation\n\n–> MOAITransform2D self\n–> number xDelta: In degrees.\n–> number yDelta: In degrees.\n<– nil",
                args = "(MOAITransform2D self, number xDelta, number yDelta)",
                returns = "nil"
            },
            addScl = {
                type = "method",
                description = "Adds a delta to the transform's scale\n\n–> MOAITransform2D self\n–> number xSclDelta\n[–> number ySclDelta: Default value is xSclDelta.]\n<– nil",
                args = "(MOAITransform2D self, number xSclDelta, [number ySclDelta])",
                returns = "nil"
            },
            getLoc = {
                type = "method",
                description = "Returns the transform's current location.\n\n–> MOAITransform2D self\n<– number xLoc\n<– number yLoc",
                args = "MOAITransform2D self",
                returns = "(number xLoc, number yLoc)",
                valuetype = "number"
            },
            getPiv = {
                type = "method",
                description = "Returns the transform's current pivot.\n\n–> MOAITransform2D self\n<– number xPiv\n<– number yPiv",
                args = "MOAITransform2D self",
                returns = "(number xPiv, number yPiv)",
                valuetype = "number"
            },
            getRot = {
                type = "method",
                description = "Returns the transform's current rotation.\n\n–> MOAITransform2D self\n<– number zRot: Rotation in degrees.",
                args = "MOAITransform2D self",
                returns = "number zRot",
                valuetype = "number"
            },
            getScl = {
                type = "method",
                description = "Returns the transform's current scale.\n\n–> MOAITransform2D self\n<– number xScl\n<– number yScl",
                args = "MOAITransform2D self",
                returns = "(number xScl, number yScl)",
                valuetype = "number"
            },
            modelToWorld = {
                type = "method",
                description = "Transform a point in model space to world space.\n\n–> MOAITransform2D self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n<– number x\n<– number y",
                args = "(MOAITransform2D self, [number x, [number y]])",
                returns = "(number x, number y)",
                valuetype = "number"
            },
            move = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xDelta: Delta to be added to x.\n–> number yDelta: Delta to be added to y.\n–> number zRotDelta: Delta to be added to z rot (in degrees).\n–> number xSclDelta: Delta to be added to x scale.\n–> number ySclDelta: Delta to be added to y scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xDelta, number yDelta, number zRotDelta, number xSclDelta, number ySclDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            moveLoc = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xDelta: Delta to be added to x.\n–> number yDelta: Delta to be added to y.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xDelta, number yDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            movePiv = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xDelta: Delta to be added to xPiv.\n–> number yDelta: Delta to be added to yPiv.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xDelta, number yDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            moveRot = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number zDelta: Delta to be added to zRot (in degrees).\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number zDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            moveScl = {
                type = "method",
                description = "Animate the transform by applying a delta. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xSclDelta: Delta to be added to x scale.\n–> number ySclDelta: Delta to be added to y scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xSclDelta, number ySclDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seek = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xGoal: Desired resulting value for x.\n–> number yGoal: Desired resulting value for y.\n–> number zRotGoal: Desired resulting value for z rot (in degrees).\n–> number xSclGoal: Desired resulting value for x scale.\n–> number ySclGoal: Desired resulting value for y scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xGoal, number yGoal, number zRotGoal, number xSclGoal, number ySclGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekLoc = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xGoal: Desired resulting value for x.\n–> number yGoal: Desired resulting value for y.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xGoal, number yGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekPiv = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xGoal: Desired resulting value for xPiv.\n–> number yGoal: Desired resulting value for yPiv.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xGoal, number yGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekRot = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number zRotGoal: Desired resulting value for z rot (in degrees).\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number zRotGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            seekScl = {
                type = "method",
                description = "Animate the transform by applying a delta. Delta is computed given a target value. Creates and returns a MOAIEaseDriver initialized to apply the delta.\n\n–> MOAITransform2D self\n–> number xSclGoal: Desired resulting value for x scale.\n–> number ySclGoal: Desired resulting value for y scale.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAITransform2D self, number xSclGoal, number ySclGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            setLoc = {
                type = "method",
                description = "Sets the transform's location.\n\n–> MOAITransform2D self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n<– nil",
                args = "(MOAITransform2D self, [number x, [number y]])",
                returns = "nil"
            },
            setParent = {
                type = "method",
                description = "This method has been deprecated. Use MOAINode setAttrLink instead.\n\n–> MOAITransform2D self\n[–> MOAINode parent: Default value is nil.]\n<– nil",
                args = "(MOAITransform2D self, [MOAINode parent])",
                returns = "nil"
            },
            setPiv = {
                type = "method",
                description = "Sets the transform's pivot.\n\n–> MOAITransform2D self\n[–> number xPiv: Default value is 0.]\n[–> number yPiv: Default value is 0.]\n<– nil",
                args = "(MOAITransform2D self, [number xPiv, [number yPiv]])",
                returns = "nil"
            },
            setRot = {
                type = "method",
                description = "Sets the transform's rotation.\n\n–> MOAITransform2D self\n[–> number zRot: Default value is 0.]\n<– nil",
                args = "(MOAITransform2D self, [number zRot])",
                returns = "nil"
            },
            setScl = {
                type = "method",
                description = "Sets the transform's scale.\n\n–> MOAITransform2D self\n–> number xScl\n[–> number yScl: Default value is xScl.]\n<– nil",
                args = "(MOAITransform2D self, number xScl, [number yScl])",
                returns = "nil"
            },
            worldToModel = {
                type = "method",
                description = "Transform a point in world space to model space.\n\n–> MOAITransform2D self\n[–> number x: Default value is 0.]\n[–> number y: Default value is 0.]\n<– number x\n<– number y",
                args = "(MOAITransform2D self, [number x, [number y]])",
                returns = "(number x, number y)",
                valuetype = "number"
            }
        }
    },
    MOAITransformBase = {
        type = "class",
        inherits = "MOAINode",
        description = "Base class for 2D affine transforms.",
        childs = {
            ATTR_WORLD_X_LOC = {
                type = "value"
            },
            ATTR_WORLD_X_SCL = {
                type = "value"
            },
            ATTR_WORLD_Y_LOC = {
                type = "value"
            },
            ATTR_WORLD_Y_SCL = {
                type = "value"
            },
            ATTR_WORLD_Z_LOC = {
                type = "value"
            },
            ATTR_WORLD_Z_ROT = {
                type = "value"
            },
            ATTR_WORLD_Z_SCL = {
                type = "value"
            },
            TRANSFORM_TRAIT = {
                type = "value"
            },
            getWorldDir = {
                type = "method",
                description = "Returns the normalized direction vector of the transform. This value is returned in world space so includes parent transforms (if any).\n\n–> MOAITransformBase self\n<– number xDirection\n<– number yDirection\n<– number zDirection",
                args = "MOAITransformBase self",
                returns = "(number xDirection, number yDirection, number zDirection)",
                valuetype = "number"
            },
            getWorldLoc = {
                type = "method",
                description = "Get the transform's location in world space.\n\n–> MOAITransformBase self\n<– number xLoc\n<– number yLoc\n<– number zLoc",
                args = "MOAITransformBase self",
                returns = "(number xLoc, number yLoc, number zLoc)",
                valuetype = "number"
            },
            getWorldRot = {
                type = "method",
                description = "Get the transform's rotation in world space.\n\n–> MOAITransformBase self\n<– number degrees",
                args = "MOAITransformBase self",
                returns = "number degrees",
                valuetype = "number"
            },
            getWorldScl = {
                type = "method",
                description = "Get the transform's scale in world space.\n\n–> MOAITransformBase self\n<– number xScale\n<– number yScale\n<– number zScale",
                args = "MOAITransformBase self",
                returns = "(number xScale, number yScale, number zScale)",
                valuetype = "number"
            }
        }
    },
    MOAITstoreGamecenterAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {
            authTstore = {
                type = "function",
                description = "Authorizes the app through Tstore\n\n–> boolean wantsLogin\n<– nil",
                args = "boolean wantsLogin",
                returns = "nil"
            },
            checkTstoreInstalled = {
                type = "function",
                description = "Checks if the Tstore app is installed\n\n<– boolean installed",
                args = "()",
                returns = "boolean installed",
                valuetype = "boolean"
            },
            disableGamecenter = {
                type = "function",
                description = "Disables Tstore Gamecenter\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            enableGamecenter = {
                type = "function",
                description = "Enables Tstore Gamecenter\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            getRankList = {
                type = "function",
                description = "Gets the leaderboard list\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            getUserInfo = {
                type = "function",
                description = "Gets the userinfo\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            installGamecenter = {
                type = "function",
                description = "Installs the Tstore Gamecenter app\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            installTstore = {
                type = "function",
                description = "Installs the Tstore app\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            invokeTstoreJoinPage = {
                type = "function",
                description = "Invokes the Tstore join page\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            openGallery = {
                type = "function",
                description = "opens gallery for profile pic selection\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            setPoint = {
                type = "function",
                description = 'Records a point to the leaderboard\n\n–> string score: ( convert number to string in Lua )\n–> string pointName: ( score + "points" or score + "rubies" )\n<– nil',
                args = "(string score, string pointName)",
                returns = "nil"
            },
            setUserInfo = {
                type = "function",
                description = "Records a new user nickname\n\n–> string nickname\n<– nil",
                args = "string nickname",
                returns = "nil"
            },
            startGamecenter = {
                type = "function",
                description = "Starts the gamecenter app\n\n<– number status: Can be GAMECENTER_INSTALLED, GAMECENTER_UPGRADING, GAMECENTER_NOT_INSTALLED",
                args = "()",
                returns = "number status",
                valuetype = "number"
            }
        }
    },
    MOAITstoreWallAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {
            showOfferWall = {
                type = "function",
                description = "Displays the Tstore marketplace.\n\n<– nil",
                args = "()",
                returns = "nil"
            }
        }
    },
    MOAITwitterAndroid = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Wrapper for Twitter integration on Android devices. Twitter provides social integration for sharing on www.twitter.com.",
        childs = {
            SESSION_DID_LOGIN = {
                type = "value",
                description = "Event indicating a successfully completed Twitter login."
            },
            SESSION_DID_NOT_LOGIN = {
                type = "value",
                description = "Event indicating an unsuccessful (or canceled) Twitter login."
            },
            TWEET_CANCELLED = {
                type = "value",
                description = "Event indicating an unsuccessful Tweet."
            },
            TWEET_SUCCESSFUL = {
                type = "value",
                description = "Event indicating a successful Tweet.."
            },
            init = {
                type = "function",
                description = "Initialize Twitter.\n\n–> string consumerKey: OAuth consumer key\n–> string consumerSecret: OAuth consumer secret\n–> string callbackUrl: Available in Twitter developer settings.\n<– nil",
                args = "(string consumerKey, string consumerSecret, string callbackUrl)",
                returns = "nil"
            },
            isLoggedIn = {
                type = "function",
                description = "Determine if twitter is currently authorized.\n\n<– boolean isLoggedIn: True if logged in, false otherwise.",
                args = "()",
                returns = "boolean isLoggedIn",
                valuetype = "boolean"
            },
            login = {
                type = "function",
                description = "Prompt the user to login to Twitter.\n\n<– nil",
                args = "()",
                returns = "nil"
            },
            sendTweet = {
                type = "function",
                description = "Tweet the provided text\n\n[–> string text: The text for the tweet.]\n<– nil",
                args = "[string text]",
                returns = "nil"
            },
            setAccessToken = {
                type = "function",
                description = "Set the access token that authenticates the user.\n\n–> string token: OAuth token\n–> string tokenSecret: OAuth token secret\n<– nil",
                args = "(string token, string tokenSecret)",
                returns = "nil"
            }
        }
    },
    MOAIUntzSampleBuffer = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Uncompressed WAV data held in memory. May be shared between multiple MOAIUntzSound objects.",
        childs = {
            getData = {
                type = "method",
                description = "Retrieve every sample data in buffer\n\n–> MOAIUntzSampleBuffer self\n<– table data: Array of sample data numbers ( -1 ~ 1 as sample level)",
                args = "MOAIUntzSampleBuffer self",
                returns = "table data",
                valuetype = "table"
            },
            getInfo = {
                type = "method",
                description = "Returns attributes of sample buffer.\n\n–> MOAIUntzSampleBuffer self\n<– number bitsPerSample\n<– number channelCount: number of channels (mono=1, stereo=2)\n<– number frameCount: number of total frames contained\n<– number sampleRate: 44100, 22050, etc.\n<– number length: sound length in seconds",
                args = "MOAIUntzSampleBuffer self",
                returns = "(number bitsPerSample, number channelCount, number frameCount, number sampleRate, number length)",
                valuetype = "number"
            },
            load = {
                type = "method",
                description = "Loads a sound from disk.\n\n–> MOAIUntzSampleBuffer self\n–> string filename\n<– nil",
                args = "(MOAIUntzSampleBuffer self, string filename)",
                returns = "nil"
            },
            prepareBuffer = {
                type = "method",
                description = "Allocate internal memory for sample buffer\n\n–> MOAIUntzSampleBuffer self\n–> number channelCount: number of channels (mono=1, stereo=2)\n–> number frameCount: number of total frames of sample\n–> number sampleRate: sample rate in Hz (44100 or else)\n<– nil",
                args = "(MOAIUntzSampleBuffer self, number channelCount, number frameCount, number sampleRate)",
                returns = "nil"
            },
            setData = {
                type = "method",
                description = "Write sample data into buffer\n\n–> MOAIUntzSampleBuffer self\n–> table data: Array of sample data numbers ( -1 ~ 1 as sample level )\n–> number index: Index within sample buffer to start copying from (1 for the first sample)\n<– nil",
                args = "(MOAIUntzSampleBuffer self, table data, number index)",
                returns = "nil"
            }
        }
    },
    MOAIUntzSound = {
        type = "class",
        inherits = "MOAINode",
        description = "Untz sound object.",
        childs = {
            ATTR_VOLUME = {
                type = "value"
            },
            getFilename = {
                type = "method",
                description = "Return the file name of the sound.\n\n–> MOAIUntzSound self\n<– string filename",
                args = "MOAIUntzSound self",
                returns = "string filename",
                valuetype = "string"
            },
            getLength = {
                type = "method",
                description = "Return the duration of the sound.\n\n–> MOAIUntzSound self\n<– number length",
                args = "MOAIUntzSound self",
                returns = "number length",
                valuetype = "number"
            },
            getPosition = {
                type = "method",
                description = "Return the position of the cursor in the sound.\n\n–> MOAIUntzSound self\n<– number position",
                args = "MOAIUntzSound self",
                returns = "number position",
                valuetype = "number"
            },
            getVolume = {
                type = "method",
                description = "Return the volume of the sound.\n\n–> MOAIUntzSound self\n<– number volume",
                args = "MOAIUntzSound self",
                returns = "number volume",
                valuetype = "number"
            },
            isLooping = {
                type = "method",
                description = "Return the looping status if the sound.\n\n–> MOAIUntzSound self\n<– boolean looping",
                args = "MOAIUntzSound self",
                returns = "boolean looping",
                valuetype = "boolean"
            },
            isPaused = {
                type = "method",
                description = "Return the pause status of the sound.\n\n–> MOAIUntzSound self\n<– boolean paused",
                args = "MOAIUntzSound self",
                returns = "boolean paused",
                valuetype = "boolean"
            },
            isPlaying = {
                type = "method",
                description = "Return the playing status of the sound.\n\n–> MOAIUntzSound self\n<– boolean playing",
                args = "MOAIUntzSound self",
                returns = "boolean playing",
                valuetype = "boolean"
            },
            load = {
                type = "method",
                description = "Loads a sound from disk or from a buffer.\n\nOverload:\n–> MOAIUntzSound self\n–> string filename\n[–> boolean loadIntoMemory: Default value is true]\n<– nil\n\nOverload:\n–> MOAIUntzSound self\n–> MOAIUntzSampleBuffer data\n<– nil",
                args = "(MOAIUntzSound self, ((string filename, [boolean loadIntoMemory]) | MOAIUntzSampleBuffer data))",
                returns = "nil"
            },
            moveVolume = {
                type = "method",
                description = "Animation helper for volume attribute,\n\n–> MOAIUntzSound self\n–> number vDelta: Delta to be added to v.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAIUntzSound self, number vDelta, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            pause = {
                type = "method",
                description = "Pause the sound.\n\n–> MOAIUntzSound self\n<– nil",
                args = "MOAIUntzSound self",
                returns = "nil"
            },
            play = {
                type = "method",
                description = "Play the sound.\n\n–> MOAIUntzSound self\n<– nil",
                args = "MOAIUntzSound self",
                returns = "nil"
            },
            seekVolume = {
                type = "method",
                description = "Animation helper for volume attribute,\n\n–> MOAIUntzSound self\n–> number vGoal: Desired resulting value for v.\n–> number length: Length of animation in seconds.\n[–> number mode: The ease mode. One of MOAIEaseType.EASE_IN, MOAIEaseType.EASE_OUT, MOAIEaseType.FLAT MOAIEaseType.LINEAR, MOAIEaseType.SMOOTH, MOAIEaseType.SOFT_EASE_IN, MOAIEaseType.SOFT_EASE_OUT, MOAIEaseType.SOFT_SMOOTH. Defaults to MOAIEaseType.SMOOTH.]\n<– MOAIEaseDriver easeDriver",
                args = "(MOAIUntzSound self, number vGoal, number length, [number mode])",
                returns = "MOAIEaseDriver easeDriver",
                valuetype = "MOAIEaseDriver"
            },
            setLooping = {
                type = "method",
                description = "Set or clear the looping status of the sound.\n\n–> MOAIUntzSound self\n[–> boolean looping: Default value is 'false.']\n<– nil",
                args = "(MOAIUntzSound self, [boolean looping])",
                returns = "nil"
            },
            setLoopPoints = {
                type = "method",
                description = "Sets the start and end looping positions for the sound\n\n–> MOAIUntzSound self\n–> number startTime\n–> number endTime\n<– nil",
                args = "(MOAIUntzSound self, number startTime, number endTime)",
                returns = "nil"
            },
            setPosition = {
                type = "method",
                description = "Sets the position of the sound cursor.\n\n–> MOAIUntzSound self\n[–> number position: Default value is 0.]\n<– nil",
                args = "(MOAIUntzSound self, [number position])",
                returns = "nil"
            },
            setVolume = {
                type = "method",
                description = "Sets the volume of the sound.\n\n–> MOAIUntzSound self\n[–> number volume: Default value is 0.]\n<– nil",
                args = "(MOAIUntzSound self, [number volume])",
                returns = "nil"
            },
            stop = {
                type = "method",
                description = "Stops the sound from playing.\n\n–> MOAIUntzSound self\n<– nil",
                args = "MOAIUntzSound self",
                returns = "nil"
            }
        }
    },
    MOAIUntzSystem = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Untz system singleton.",
        childs = {
            getSampleRate = {
                type = "function",
                description = "Return the system's current sample rate.\n\n<– number sampleRate",
                args = "()",
                returns = "number sampleRate",
                valuetype = "number"
            },
            getVolume = {
                type = "function",
                description = "Return the system's current volume\n\n<– number volume",
                args = "()",
                returns = "number volume",
                valuetype = "number"
            },
            initialize = {
                type = "function",
                description = "Initialize the sound system.\n\n[–> number sampleRate: Default value is 44100.]\n[–> number numFrames: Default value is 8192]\n<– nil",
                args = "[number sampleRate, [number numFrames]]",
                returns = "nil"
            },
            setSampleRate = {
                type = "function",
                description = "Set the system sample rate.\n\n[–> number sampleRate: Default value is 44100.]\n<– nil",
                args = "[number sampleRate]",
                returns = "nil"
            },
            setVolume = {
                type = "function",
                description = "Set the system level volume.\n\n[–> number volume: Valid Range: 0 >= x <= 1.0 (Default value is 1.0)]\n<– nil",
                args = "[number volume]",
                returns = "nil"
            }
        }
    },
    MOAIVecPathGraph = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {
            areNeighbors = {
                type = "method",
                description = "Checks if two nodes are neighbors.\n\n–> MOAIVecPathGraph self\n–> number nodeID1\n–> number nodeID2\n<– boolean",
                args = "(MOAIVecPathGraph self, number nodeID1, number nodeID2)",
                returns = "boolean",
                valuetype = "boolean"
            },
            getNode = {
                type = "method",
                description = "Returns the coordinates of a node.\n\n–> MOAIVecPathGraph self\n–> number nodeID\n<– number x\n<– number y\n<– number z",
                args = "(MOAIVecPathGraph self, number nodeID)",
                returns = "(number x, number y, number z)",
                valuetype = "number"
            },
            getNodeCount = {
                type = "method",
                description = "Returns the number of nodes in the graph.\n\n–> MOAIVecPathGraph self\n<– number count",
                args = "MOAIVecPathGraph self",
                returns = "number count",
                valuetype = "number"
            },
            reserveNodes = {
                type = "method",
                description = "Reserves memory for a given number of nodes.\n\n–> MOAIVecPathGraph self\n–> number nNodes\n<– nil",
                args = "(MOAIVecPathGraph self, number nNodes)",
                returns = "nil"
            },
            setNeighbors = {
                type = "method",
                description = "Sets the neighborhood relation for two nodes.\n\n–> MOAIVecPathGraph self\n–> number nodeID1\n–> number nodeID2\n[–> boolean value: Whether the nodes are neighbors (true) or not (false). Defaults to true.]\n<– nil",
                args = "(MOAIVecPathGraph self, number nodeID1, number nodeID2, [boolean value])",
                returns = "nil"
            },
            setNode = {
                type = "method",
                description = "Sets the coordinates of a node.\n\n–> MOAIVecPathGraph self\n–> number nodeID\n[–> number x: Defaults to 0.]\n[–> number y: Defaults to 0.]\n[–> number z: Defaults to 0.]\n<– nil",
                args = "(MOAIVecPathGraph self, number nodeID, [number x, [number y, [number z]]])",
                returns = "nil"
            }
        }
    },
    MOAIVertexBuffer = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Vertex buffer class.",
        childs = {
            bless = {
                type = "method",
                description = "Call this after initializing the buffer and settings it vertices to prepare it for use.\n\n–> MOAIVertexBuffer self\n<– nil",
                args = "MOAIVertexBuffer self",
                returns = "nil"
            },
            release = {
                type = "method",
                description = "Releases any memory associated with buffer.\n\n–> MOAIVertexBuffer self\n<– nil",
                args = "MOAIVertexBuffer self",
                returns = "nil"
            },
            reserve = {
                type = "method",
                description = "Sets capacity of buffer in bytes.\n\n–> MOAIVertexBuffer self\n–> number size\n<– nil",
                args = "(MOAIVertexBuffer self, number size)",
                returns = "nil"
            },
            reserveVerts = {
                type = "method",
                description = "Sets capacity of buffer in vertices. This function should only be used after attaching a valid MOAIVertexFormat to the buffer.\n\n–> MOAIVertexBuffer self\n–> number size\n<– nil",
                args = "(MOAIVertexBuffer self, number size)",
                returns = "nil"
            },
            reset = {
                type = "method",
                description = "Resets the vertex stream writing to the head of the stream.\n\n–> MOAIVertexBuffer self\n<– nil",
                args = "MOAIVertexBuffer self",
                returns = "nil"
            },
            setFormat = {
                type = "method",
                description = "Sets the vertex format for the buffer.\n\n–> MOAIVertexBuffer self\n–> MOAIVertexFormat format\n<– nil",
                args = "(MOAIVertexBuffer self, MOAIVertexFormat format)",
                returns = "nil"
            },
            writeColor32 = {
                type = "method",
                description = "Write a packed 32-bit color to the vertex buffer.\n\n–> MOAIVertexBuffer self\n[–> number r: Default value is 1.]\n[–> number g: Default value is 1.]\n[–> number b: Default value is 1.]\n[–> number a: Default value is 1.]\n<– nil",
                args = "(MOAIVertexBuffer self, [number r, [number g, [number b, [number a]]]])",
                returns = "nil"
            },
            writeFloat = {
                type = "method",
                description = "Write a 32-bit float to the vertex buffer.\n\n–> MOAIVertexBuffer self\n[–> number f: Default value is 0.]\n<– nil",
                args = "(MOAIVertexBuffer self, [number f])",
                returns = "nil"
            },
            writeInt16 = {
                type = "method",
                description = "Write an 16-bit integer to the vertex buffer.\n\n–> MOAIVertexBuffer self\n[–> number i: Default value is 0.]\n<– nil",
                args = "(MOAIVertexBuffer self, [number i])",
                returns = "nil"
            },
            writeInt32 = {
                type = "method",
                description = "Write an 32-bit integer to the vertex buffer.\n\n–> MOAIVertexBuffer self\n[–> number i: Default value is 0.]\n<– nil",
                args = "(MOAIVertexBuffer self, [number i])",
                returns = "nil"
            },
            writeInt8 = {
                type = "method",
                description = "Write an 8-bit integer to the vertex buffer.\n\n–> MOAIVertexBuffer self\n[–> number i: Default value is 0.]\n<– nil",
                args = "(MOAIVertexBuffer self, [number i])",
                returns = "nil"
            }
        }
    },
    MOAIVertexFormat = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Vertex format class.",
        childs = {
            declareAttribute = {
                type = "method",
                description = "Declare a custom attribute (for use with programmable pipeline).\n\n–> MOAIVertexFormat self\n–> number index: Default value is 1.\n–> number type: Data type of component elements. See OpenGL ES documentation.\n–> number size: Number of elements. See OpenGL ES documentation.\n[–> boolean normalized: See OpenGL ES documentation.]\n<– nil",
                args = "(MOAIVertexFormat self, number index, number type, number size, [boolean normalized])",
                returns = "nil"
            },
            declareColor = {
                type = "method",
                description = "Declare a vertex color.\n\n–> MOAIVertexFormat self\n–> number index\n–> number type: Data type of component elements. See OpenGL ES documentation.\n<– nil",
                args = "(MOAIVertexFormat self, number index, number type)",
                returns = "nil"
            },
            declareCoord = {
                type = "method",
                description = "Declare a vertex coordinate.\n\n–> MOAIVertexFormat self\n–> number index\n–> number type: Data type of coordinate elements. See OpenGL ES documentation.\n–> number size: Number of coordinate elements. See OpenGL ES documentation.\n<– nil",
                args = "(MOAIVertexFormat self, number index, number type, number size)",
                returns = "nil"
            },
            declareNormal = {
                type = "method",
                description = "Declare a vertex normal.\n\n–> MOAIVertexFormat self\n–> number index\n–> number type: Data type of normal elements. See OpenGL ES documentation.\n<– nil",
                args = "(MOAIVertexFormat self, number index, number type)",
                returns = "nil"
            },
            declareUV = {
                type = "method",
                description = "Declare a vertex texture coordinate.\n\n–> MOAIVertexFormat self\n–> number index\n–> number type: Data type of texture coordinate elements. See OpenGL ES documentation.\n–> number size: Number of texture coordinate elements. See OpenGL ES documentation.\n<– nil",
                args = "(MOAIVertexFormat self, number index, number type, number size)",
                returns = "nil"
            }
        }
    },
    MOAIViewport = {
        type = "class",
        inherits = "MOAILuaObject ZLRect",
        description = "Viewport object.",
        childs = {
            setOffset = {
                type = "method",
                description = "Sets the viewport offset in normalized view space (size of viewport is -1 to 1 in both directions).\n\n–> MOAIViewport self\n–> number xOff\n–> number yOff\n<– nil",
                args = "(MOAIViewport self, number xOff, number yOff)",
                returns = "nil"
            },
            setRotation = {
                type = "method",
                description = "Sets global rotation to be added to camera transform.\n\n–> MOAIViewport self\n–> number rotation\n<– nil",
                args = "(MOAIViewport self, number rotation)",
                returns = "nil"
            },
            setScale = {
                type = "method",
                description = "Sets the number of world units visible of the viewport for one or both dimensions. Set 0 for one of the dimensions to use a derived value based on the other dimension and the aspect ratio. Negative values are also OK. It is typical to set the scale to the number of pixels visible in the this-> This practice is neither endorsed nor condemned. Note that the while the contents of the viewport will appear to stretch or shrink to match the dimensions of the viewport given by setSize, the number of world units visible will remain constant.\n\n–> MOAIViewport self\n–> number xScale\n–> number yScale\n<– nil",
                args = "(MOAIViewport self, number xScale, number yScale)",
                returns = "nil"
            },
            setSize = {
                type = "method",
                description = "Sets the dimensions of the this->\n\nOverload:\n–> MOAIViewport self\n–> number width\n–> number height\n<– nil\n\nOverload:\n–> MOAIViewport self\n–> number left\n–> number top\n–> number right\n–> number bottom\n<– nil",
                args = "(MOAIViewport self, ((number width, number height) | (number left, number top, number right, number bottom)))",
                returns = "nil"
            }
        }
    },
    MOAIWebViewIOS = {
        type = "class",
        inherits = "MOAIInstanceEventSource",
        description = "Wrapper for UIWebView interaction on iOS devices.",
        childs = {
            DID_FAIL_LOAD_WITH_ERROR = {
                type = "value",
                description = "Event indicating a failed UIWebView load."
            },
            NAVIGATION_BACK_FORWARD = {
                type = "value",
                description = "Indicates the the navigation was due to a back/forward operation."
            },
            NAVIGATION_FORM_RESUBMIT = {
                type = "value",
                description = "Indicates the the navigation was due to a form resubmit."
            },
            NAVIGATION_FORM_SUBMIT = {
                type = "value",
                description = "Indicates the the navigation was due to a form submit."
            },
            NAVIGATION_LINK_CLICKED = {
                type = "value",
                description = "Indicates the the navigation was due to a link click."
            },
            NAVIGATION_OTHER = {
                type = "value",
                description = "Indicates the the navigation was due to other reasons."
            },
            NAVIGATION_RELOAD = {
                type = "value",
                description = "Indicates the the navigation was due to a page reload."
            },
            SHOULD_START_LOAD_WITH_REQUEST = {
                type = "value",
                description = "Event indicating an attempt to load a UIWebView."
            },
            WEB_VIEW_DID_FINISH_LOAD = {
                type = "value",
                description = "Event indicating a completed UIWebView load."
            },
            WEB_VIEW_DID_START_LOAD = {
                type = "value",
                description = "Event indicating the start of a UIWebView load."
            }
        }
    },
    MOAIWheelSensor = {
        type = "class",
        inherits = "MOAISensor",
        description = "Hardware wheel sensor.",
        childs = {
            getDelta = {
                type = "method",
                description = "Returns the delta of the wheel\n\n–> MOAIWheelSensor self\n<– number delta",
                args = "MOAIWheelSensor self",
                returns = "number delta",
                valuetype = "number"
            },
            getValue = {
                type = "method",
                description = "Returns the current value of the wheel, based on delta events\n\n–> MOAIWheelSensor self\n<– number value",
                args = "MOAIWheelSensor self",
                returns = "number value",
                valuetype = "number"
            },
            setCallback = {
                type = "method",
                description = "Sets or clears the callback to be issued on a wheel delta event\n\n–> MOAIWheelSensor self\n[–> function callback: Default value is nil.]\n<– nil",
                args = "(MOAIWheelSensor self, [function callback])",
                returns = "nil"
            }
        }
    },
    MOAIXmlParser = {
        type = "class",
        inherits = "MOAILuaObject",
        description = "Converts XML DOM to Lua trees. Provided as a convenience; not advised for parsing very large XML documents. (Use of XML not advised at all - use JSON or Lua.)",
        childs = {
            parseFile = {
                type = "method",
                description = "Parses the contents of the specified file as XML.\n\n–> MOAIXmlParser self\n–> string filename: The path of the file to read the XML data from.\n<– table data: A tree of tables representing the XML.",
                args = "(MOAIXmlParser self, string filename)",
                returns = "table data",
                valuetype = "table"
            },
            parseString = {
                type = "method",
                description = "Parses the contents of the specified string as XML.\n\n–> MOAIXmlParser self\n–> string filename: The XML data stored in a string.\n<– table data: A tree of tables representing the XML.",
                args = "(MOAIXmlParser self, string filename)",
                returns = "table data",
                valuetype = "table"
            }
        }
    },
    ZLColorVec = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    },
    ZLRect = {
        type = "class",
        inherits = "MOAILuaObject",
        childs = {}
    }
}