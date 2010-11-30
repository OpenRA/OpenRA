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
#include "utility.h"

#define WEBSERVER_PORT 48764

GtkWindow * window;
WebKitWebView * browser;
GtkTreeStore * treeStore;

gboolean window_delete(GtkWidget * widget, GdkEvent * event, 
		       gpointer user_data)
{
  server_teardown();
  gtk_main_quit();
  return FALSE;
}

enum
{
  ICON_COLUMN,
  NAME_COLUMN,
  N_COLUMNS
};

#define MOD_key_MAX_LEN 16
#define MOD_title_MAX_LEN 32
#define MOD_version_MAX_LEN 16
#define MOD_author_MAX_LEN 32
#define MOD_description_MAX_LEN 128
#define MOD_requires_MAX_LEN 32

#define MAX_NUM_MODS 64

typedef struct mod_t
{
  char key[MOD_key_MAX_LEN];
  char title[MOD_title_MAX_LEN];
  char version[MOD_version_MAX_LEN];
  char author[MOD_author_MAX_LEN];
  char description[MOD_description_MAX_LEN];
  char requires[MOD_requires_MAX_LEN];
  int standalone;
} mod_t;



static mod_t mods[MAX_NUM_MODS];
static int mod_count = 0;

typedef void ( * lines_callback ) (char const * line, gpointer data);

//Splits console output into lines and passes each one to a callback
void process_lines(char * const lines, int len, lines_callback cb, gpointer data)
{
  char * c;
  c = strtok(lines, "\n");
  while (c != NULL)
  {
    cb(c, data);
    c = strtok(NULL, "\n");
  }
}

#define ASSIGN_TO_MOD(FIELD, VAL_OFF) \
  strncpy(mod->FIELD, val_start + VAL_OFF, MOD_##FIELD##_MAX_LEN - 1); \
  mod->FIELD[MOD_##FIELD##_MAX_LEN - 1] = '\0';

#define min(X, Y) X < Y ? X : Y

void mod_metadata_line(char const * line, gpointer data)
{
  mod_t * mod = (mod_t *)data;
  char * val_start = strchr(line, ':');
  if (memcmp(line, "Mod:", 4) == 0)
  {
    ASSIGN_TO_MOD(key, 1)
  }
  else if (memcmp(line, "  Title:", min(strlen(line), 8)) == 0)
  {
    ASSIGN_TO_MOD(title, 2)
  }
  else if (memcmp(line, "  Version:", min(strlen(line), 10)) == 0)
  {
    ASSIGN_TO_MOD(version, 2)
  }
  else if (memcmp(line, "  Author:", min(strlen(line), 9)) == 0)
  {
    ASSIGN_TO_MOD(author, 2)
  }
  else if (memcmp(line, "  Description:", min(strlen(line), 14)) == 0)
  {
    ASSIGN_TO_MOD(description, 2)
  }
  else if (memcmp(line, "  Requires:", min(strlen(line), 11)) == 0)
  {
    ASSIGN_TO_MOD(requires, 2)
  }
  else if (memcmp(line, "  Standalone:", min(strlen(line), 13)) == 0)
  {
    if (strcmp(val_start + 2, "True") == 0)
      mod->standalone = TRUE;
    else
      mod->standalone = FALSE;

    g_message("Mod standalone: %d", mod->standalone);
  }
}

void mod_metadata_callback(GPid pid, gint status, gpointer data)
{
  int out_len, * out_fd = (int *)data;
  char * msg = NULL;
  mod_t mod = mods[mod_count];

  mod_count = (mod_count + 1) % MAX_NUM_MODS;

  memset(&mod, 0, sizeof(mod_t));

  msg = util_get_output(*out_fd, &out_len);

  process_lines(msg, out_len, mod_metadata_line, &mod);

  free(msg);

  close(*out_fd);
  free(out_fd);
}

void mod_list_line(char const * mod, gpointer user)
{
  util_get_mod_metadata(mod, mod_metadata_callback);
}

void mod_list_callback(GPid pid, gint status, gpointer data)
{
  int out_len, * out_fd = (int *)data;
  char * msg = NULL;
  
  msg = util_get_output(*out_fd, &out_len);

  mod_count = 0;

  process_lines(msg, out_len, mod_list_line, NULL);

  free(msg);

  close(*out_fd);
  free(out_fd);
  g_spawn_close_pid(pid);
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

  treeStore = gtk_tree_store_new(N_COLUMNS, GDK_TYPE_PIXBUF, G_TYPE_STRING);

  util_get_mod_list(mod_list_callback);

  gtk_container_add(GTK_CONTAINER(window), GTK_WIDGET(browser));

  //TODO: Load the mod html file based on selected mod in launcher
  webkit_web_view_load_uri(browser, 
			   "http://localhost:48764/mods/cnc/mod.html");

  gtk_widget_show_all(GTK_WIDGET(window));
  g_signal_connect(window, "delete-event", G_CALLBACK(window_delete), 0);
  
  gtk_main();

  return 0;
}
