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

GtkWindow * window;
WebKitWebView * browser;

gboolean window_delete(GtkWidget * widget, GdkEvent * event, gpointer user_data)
{
  server_teardown();
  gtk_main_quit();
  return FALSE;
}

int get_file_uri(char const * filepath, char ** uri)
{
  FILE * output;
  char buffer[1024];
  size_t buffer_len;

  sprintf(buffer, "readlink -f %s", filepath);

  output = popen(buffer, "r");

  if (!output)
  {
    g_warning("Could not find absolute path for %s", filepath);
    return FALSE;
  }

  fgets(buffer, sizeof(buffer), output);

  pclose(output);

  buffer_len = strlen(buffer);
  buffer[buffer_len - 1] = '\0';

  *uri = g_filename_to_uri(buffer, NULL, NULL);

  if (!*uri)
  {
    g_warning("Could not convert %s to URI", buffer);
    return FALSE;
  }

  return TRUE;
}

int get_bridge_script(char ** script)
{
  FILE * f;
  long fileSize;
  char * buffer;
  size_t result;

  f = fopen("bridge.js", "r");

  if (!f) 
  { 
    g_critical("Could not open bridge.js"); 
    return FALSE; 
  }

  fseek(f, 0, SEEK_END);
  fileSize = ftell(f);
  rewind(f);

  buffer = (char *) malloc(sizeof(char) * fileSize);
  result = fread(buffer, 1, fileSize, f);

  fclose(f);

  *script = buffer;
  return TRUE;
}

int main(int argc, char ** argv)
{
  char * uri, * script;
  
  server_init(48764);
  
  gtk_init(&argc, &argv);

  window = GTK_WINDOW(gtk_window_new(GTK_WINDOW_TOPLEVEL));
  gtk_window_set_title(window, "OpenRA Launcher");
  gtk_window_set_default_size(window, 800, 600);

  browser = WEBKIT_WEB_VIEW(webkit_web_view_new());

  gtk_container_add(GTK_CONTAINER(window), GTK_WIDGET(browser));

  if (!get_bridge_script(&script))
    return 1;

  webkit_web_view_execute_script(browser, script);

  free(script);

  if (!get_file_uri("../mods/cnc/mod.html", &uri))
  {
    return 1;
  }

  webkit_web_view_load_uri(browser, uri);

  free(uri);

  gtk_widget_show_all(GTK_WIDGET(window));
  g_signal_connect(window, "delete-event", G_CALLBACK(window_delete), 0);
  
  gtk_main();

  return 0;
}
