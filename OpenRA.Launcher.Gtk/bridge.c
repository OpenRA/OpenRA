/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#include <webkit/webkit.h>
#include <JavaScriptCore/JavaScript.h>
#include <glib.h>

#define JS_STR(str) JSStringCreateWithUTF8CString(str)
#define JS_FUNC(ctx, callback) JSObjectMakeFunctionWithCallback(ctx, NULL, \
						  callback)

JSValueRef js_log(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
		  size_t argc, const JSValueRef argv[],
		  JSValueRef * exception)
{
  JSValueRef return_value = JSValueMakeNull(ctx);
  if (argc < 1)
  {
    *exception = JSValueMakeString(ctx, JS_STR("Not enough args"));
    return return_value;
  }

  if (JSValueIsString(ctx, argv[0]))
  {
    char buffer[1024];
    JSStringRef s = JSValueToStringCopy(ctx, argv[0], NULL);
    JSStringGetUTF8CString(s, buffer, 1024);
    g_message("JS Log: %s", buffer);
    return return_value;
  }
  else
  {
    *exception = JSValueMakeString(ctx, JS_STR("Tried to log something other than a string"));
    return return_value;
  }
}

JSValueRef js_exists_in_mod(JSContextRef ctx, JSObjectRef func, 
			  JSObjectRef this, size_t argc, 
			  const JSValueRef argv[], JSValueRef * exception)
{
  if (argc < 2)
  {
    *exception = JSValueMakeString(ctx, JS_STR("Not enough args"));
    return JSValueMakeNull(ctx);
  }

  return JSValueMakeNumber(ctx, 1);
}

void js_add_functions(JSGlobalContextRef ctx, JSObjectRef target, char ** names, 
		      JSObjectCallAsFunctionCallback * callbacks, size_t count)
{
  int i;
  for (i = 0; i < count; i++)
  {
    JSObjectRef func = JS_FUNC(ctx, callbacks[i]);
    JSObjectSetProperty(ctx, target, JS_STR(names[i]), func, kJSPropertyAttributeNone, NULL);
  }
}

void bind_js_bridge(WebKitWebView * view, WebKitWebFrame * frame,
		    gpointer context, gpointer window_object,
		    gpointer user_data)
{
  JSGlobalContextRef js_ctx;
  JSObjectRef window_obj, external_obj;
  
  int func_count = 2;
  char * names[] = { "log", "existsInMod" };
  JSObjectCallAsFunctionCallback callbacks[] = { js_log, js_exists_in_mod };
  
  js_ctx = (JSGlobalContextRef)context;

  external_obj = JSObjectMake(js_ctx, NULL, NULL);

  window_obj = (JSObjectRef)window_object;
  JSObjectSetProperty(js_ctx, window_obj, JS_STR("external"),
		      external_obj, 0, NULL);

  js_add_functions(js_ctx, external_obj, names, callbacks, func_count);
}
