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

#include "server.h"
#include "bridge.h"

#define WEBSERVER_PORT 48764

GtkWindow * window;
WebKitWebView * browser;

gboolean window_delete(GtkWidget * widget, GdkEvent * event, 
		       gpointer user_data)
{
  server_teardown();
  gtk_main_quit();
  return FALSE;
}

int main(int argc, char ** argv)
{
  server_init(WEBSERVER_PORT);
  
  gtk_init(&argc, &argv);

  window = GTK_WINDOW(gtk_window_new(GTK_WINDOW_TOPLEVEL));
  gtk_window_set_title(window, "OpenRA Launcher");
  gtk_window_set_default_size(window, 800, 600);

  browser = WEBKIT_WEB_VIEW(webkit_web_view_new());
  g_signal_connect(browser, "window-object-cleared", 
		   G_CALLBACK(bind_js_bridge), 0);

  gtk_container_add(GTK_CONTAINER(window), GTK_WIDGET(browser));

  //TODO: Load the mod html file based on selected mod in launcher
  webkit_web_view_load_uri(browser, 
			   "http://localhost:48764/mods/cnc/mod.html");

  gtk_widget_show_all(GTK_WIDGET(window));
  g_signal_connect(window, "delete-event", G_CALLBACK(window_delete), 0);
  
  gtk_main();

  return 0;
}
