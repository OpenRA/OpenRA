/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#include <stdlib.h>
#include <stdio.h>

#include <webkit/webkit.h>
#include <JavaScriptCore/JavaScript.h>
#include <glib.h>

#define JS_STR(str) JSStringCreateWithUTF8CString(str)
#define JS_FUNC(ctx, callback) JSObjectMakeFunctionWithCallback(ctx, NULL, \
						  callback)

int js_check_num_args(JSContextRef ctx, char const * func_name, int argc, int num_expected, JSValueRef * exception)
{
  char buf[64];
  if (argc < num_expected)
  {
    sprintf(buf, "%s: Not enough args, expected %d got %d", func_name, num_expected, argc);
    *exception = JSValueMakeString(ctx, JS_STR(buf));
    return 0;
  }
  return 1;
}

char * js_get_cstr_from_val(JSContextRef ctx, JSValueRef val, size_t * size)
{
  char * buf;
  size_t str_size;
  JSStringRef str = JSValueToStringCopy(ctx, val, NULL);
  str_size = JSStringGetMaximumUTF8CStringSize(str);
  buf = (char *)malloc(str_size);
  *size = JSStringGetUTF8CString(str, buf, str_size);
  return buf;
}

JSValueRef js_log(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
		  size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  JSValueRef return_value = JSValueMakeNull(ctx);
  if (!js_check_num_args(ctx, "log", argc, 1, exception))
    return return_value;

  if (JSValueIsString(ctx, argv[0]))
  {
    char * buffer;
    size_t str_size;
    buffer = js_get_cstr_from_val(ctx, argv[0], &str_size);
    g_message("JS Log: %s", buffer);
    free(buffer);
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
  char * mod_buf, * file_buf, search_path[512];
  size_t mod_size, file_size;
  JSValueRef return_value = JSValueMakeNumber(ctx, 0);
  FILE * f;
  if (!js_check_num_args(ctx, "existsInMod", argc, 2, exception))
    return JSValueMakeNull(ctx);

  if (!JSValueIsString(ctx, argv[0]) || !JSValueIsString(ctx, argv[1]))
  {
    *exception = JSValueMakeString(ctx, JS_STR("One or more args are incorrect types."));
    return JSValueMakeNull(ctx);
  }

  file_buf = js_get_cstr_from_val(ctx, argv[0], &file_size);
  mod_buf = js_get_cstr_from_val(ctx, argv[1], &mod_size);
  
  sprintf(search_path, "mods/%s/%s", mod_buf, file_buf);

  free(file_buf);
  free(mod_buf);
 
  g_message("JS ExistsInMod: Looking for %s", search_path);

  f = fopen(search_path, "r");

  if (f != NULL)
  {
    g_message("JS ExistsInMod: Found");
    fclose(f);
    return_value = JSValueMakeNumber(ctx, 1);
  }

  return return_value;
}

JSValueRef js_launch_mod(JSContextRef ctx, JSObjectRef func, 
			 JSObjectRef this, size_t argc,
			 const JSValueRef argv[], JSValueRef * exception)
{
  char * mod;
  size_t mod_size;
  JSValueRef return_value = JSValueMakeNull(ctx);

  if (!js_check_num_args(ctx, "launchMod", argc, 1, exception))
    return return_value;

  if (!JSValueIsString(ctx, argv[0]))
  {
    *exception = JSValueMakeString(ctx, JS_STR("One or more args are incorrect types."));
    return return_value;
  }

  mod = js_get_cstr_from_val(ctx, argv[0], &mod_size);

  g_message("JS LaunchMod: %s", mod);

  free(mod);

  return return_value;
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
  
  int func_count = 3;
  char * names[] = { "log", "existsInMod", "launchMod" };
  JSObjectCallAsFunctionCallback callbacks[] = { js_log, js_exists_in_mod, js_launch_mod };
  
  js_ctx = (JSGlobalContextRef)context;

  external_obj = JSObjectMake(js_ctx, NULL, NULL);

  window_obj = (JSObjectRef)window_object;
  JSObjectSetProperty(js_ctx, window_obj, JS_STR("external"),
		      external_obj, 0, NULL);

  js_add_functions(js_ctx, external_obj, names, callbacks, func_count);
}
