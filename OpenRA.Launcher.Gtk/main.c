/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <gtk/gtk.h>
#include <webkit/webkit.h>
#include <JavaScriptCore/JavaScript.h>

#include "server.h"

#define JS_STR(str) JSStringCreateWithUTF8CString(str)
#define JS_FUNC(ctx, callback) JSObjectMakeFunctionWithCallback(ctx, NULL, \
						  callback)

GtkWindow * window;
WebKitWebView * browser;

gboolean window_delete(GtkWidget * widget, GdkEvent * event, 
		       gpointer user_data)
{
  server_teardown();
  gtk_main_quit();
  return FALSE;
}

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
  return JSValueMakeNumber(ctx, 1);
}

void bind_js_bridge(WebKitWebView * view, WebKitWebFrame * frame,
		    gpointer context, gpointer window_object,
		    gpointer user_data)
{
  JSGlobalContextRef js_ctx;
  JSObjectRef window_obj, external_obj, 
    log_function, exists_in_mod_function;
  
  js_ctx = (JSGlobalContextRef)context;

  external_obj = JSObjectMake(js_ctx, NULL, NULL);
  log_function = JS_FUNC(js_ctx, js_log);
  exists_in_mod_function = JS_FUNC(js_ctx, js_exists_in_mod);
  window_obj = (JSObjectRef)window_object;
  JSObjectSetProperty(js_ctx, window_obj, JS_STR("external"),
		      external_obj, 0, NULL);
  JSObjectSetProperty(js_ctx, external_obj, JS_STR("log"), 
		      log_function, 0, NULL);
  JSObjectSetProperty(js_ctx, external_obj, JS_STR("existsInMod"),
		      exists_in_mod_function, 0, NULL);
} 

int main(int argc, char ** argv)
{
  server_init(48764);
  
  gtk_init(&argc, &argv);

  window = GTK_WINDOW(gtk_window_new(GTK_WINDOW_TOPLEVEL));
  gtk_window_set_title(window, "OpenRA Launcher");
  gtk_window_set_default_size(window, 800, 600);

  browser = WEBKIT_WEB_VIEW(webkit_web_view_new());
  g_signal_connect(browser, "window-object-cleared", 
		   G_CALLBACK(bind_js_bridge), 0);

  gtk_container_add(GTK_CONTAINER(window), GTK_WIDGET(browser));

  webkit_web_view_load_uri(browser, 
			   "http://localhost:48764/mods/cnc/mod.html");

  gtk_widget_show_all(GTK_WIDGET(window));
  g_signal_connect(window, "delete-event", G_CALLBACK(window_delete), 0);
  
  gtk_main();

  return 0;
}
