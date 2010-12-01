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
GtkTreeStore * tree_store;
GtkTreeView * tree;

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
  KEY_COLUMN,
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
  int prev = 0, current = 0;
  while (current < len)
  {
    if (lines[current] == '\n')
    {
      char * line = (char *)malloc(current - prev + 1);
      memcpy(line, lines + prev, current - prev);
      line[current - prev] = '\0';
      cb(line, data);
      free(line);
      prev = current + 1;
    }
    current++;
  }
}

#define ASSIGN_TO_MOD(FIELD) \
  strncpy(mod->FIELD, val_start + 2, MOD_##FIELD##_MAX_LEN - 1); \
  mod->FIELD[MOD_##FIELD##_MAX_LEN - 1] = '\0'

#define min(X, Y) X < Y ? X : Y

void mod_metadata_line(char const * line, gpointer data)
{
  mod_t * mod = (mod_t *)data;
  char * val_start = strchr(line, ':');
  if (memcmp(line, "Mod:", 4) == 0)
  {
    ASSIGN_TO_MOD(key);
  }
  else if (memcmp(line, "  Title:", min(strlen(line), 8)) == 0)
  {
    ASSIGN_TO_MOD(title);
  }
  else if (memcmp(line, "  Version:", min(strlen(line), 10)) == 0)
  {
    ASSIGN_TO_MOD(version);
  }
  else if (memcmp(line, "  Author:", min(strlen(line), 9)) == 0)
  {
    ASSIGN_TO_MOD(author);
  }
  else if (memcmp(line, "  Description:", min(strlen(line), 14)) == 0)
  {
    ASSIGN_TO_MOD(description);
  }
  else if (memcmp(line, "  Requires:", min(strlen(line), 11)) == 0)
  {
    ASSIGN_TO_MOD(requires);
  }
  else if (memcmp(line, "  Standalone:", min(strlen(line), 13)) == 0)
  {
    if (strcmp(val_start + 2, "True") == 0)
      mod->standalone = TRUE;
    else
      mod->standalone = FALSE;
  }
}

mod_t * get_mod(char const * key)
{
  int i;
  for (i = 0; i < mod_count; i++)
  {
    if (strcmp(mods[i].key, key) == 0)
      return mods + i;
  }
  return NULL;
}

gboolean append_to_mod(GtkTreeModel * model, GtkTreePath * path, 
		       GtkTreeIter * iter, gpointer data)
{
  mod_t * mod = (mod_t *)data;
  gchar * key;
  GtkTreeIter new_iter;

  gtk_tree_model_get(model, iter, KEY_COLUMN, &key, -1);

  if (!key)
  {
    return FALSE;
  }

  if (strcmp(mod->requires, key) == 0)
  {
    gtk_tree_store_append(GTK_TREE_STORE(model), &new_iter, iter);
    gtk_tree_store_set(GTK_TREE_STORE(model), &new_iter,
		       KEY_COLUMN, mod->key,
		       NAME_COLUMN, mod->title,
		       -1);
    g_free(key); 
    return TRUE;
  }
  g_free(key);
  return FALSE;
}

void mod_metadata_callback(GPid pid, gint status, gpointer data)
{
  int out_len, * out_fd = (int *)data;
  char * msg = NULL;
  mod_t mod = mods[mod_count];
  GtkTreeIter iter, mod_iter;

  mod_count = (mod_count + 1) % MAX_NUM_MODS;

  memset(&mod, 0, sizeof(mod_t));

  msg = util_get_output(*out_fd, &out_len);

  process_lines(msg, out_len, mod_metadata_line, &mod);

  free(msg);

  gtk_tree_model_get_iter_first(GTK_TREE_MODEL(tree_store), &mod_iter);

  if (mod.standalone)
  {
    gtk_tree_store_append(tree_store, &iter, &mod_iter);
    gtk_tree_store_set(tree_store, &iter,
		     KEY_COLUMN, mod.key,
		     NAME_COLUMN, mod.title,
		     -1);
  }
  else if (!strlen(mod.requires))
  {
    GtkTreeIter broken_mods_iter;
    if (!gtk_tree_model_get_iter_from_string(GTK_TREE_MODEL(tree_store), 
					     &broken_mods_iter, "1"))
    {
      gtk_tree_store_append(tree_store, &broken_mods_iter, NULL);
    }
    gtk_tree_store_set(tree_store, &broken_mods_iter,
		       KEY_COLUMN, mod.key,
		       NAME_COLUMN, mod.title,
		       -1);
  }
  else
  {
    gtk_tree_model_foreach(GTK_TREE_MODEL(tree_store), append_to_mod, &mod);
  }
  
  close(*out_fd);
  free(out_fd);
}

typedef struct tree_node
{
  char const * key;
  gchar * node_path;
} tree_node;

gboolean find_mod(GtkTreeModel * model, GtkTreePath * path, 
		  GtkTreeIter * iter, gpointer data)
{
  tree_node * n = (tree_node *)data;
  gchar * key;

  gtk_tree_model_get(model, iter,
		     KEY_COLUMN, &key,
		     -1);

  if (!key)
    return FALSE;

  if (0 == strcmp(n->key, key))
  {
    n->node_path = gtk_tree_path_to_string(path);
    g_free(key);
    return TRUE;
  }

  g_free(key);
  return FALSE;
}

void last_mod_callback(GPid pid, gint status, gpointer data)
{
  int out_len, * out_fd = (int *)data;
  char * comma_pos = 0;
  char * msg = NULL;
  tree_node n;

  memset(&n, 0, sizeof(tree_node));

  msg = util_get_output(*out_fd, &out_len);

  if (0 == memcmp(msg, "Error:", 6))
    memcpy(msg, "ra", 3);
  else if (NULL != (comma_pos = strchr(msg, ',')))
  {
    *comma_pos = '\0';
  }

  n.key = msg;

  gtk_tree_model_foreach(GTK_TREE_MODEL(tree_store), find_mod, &n);

  if (n.node_path)
  {
    GtkTreePath * path;
    path = gtk_tree_path_new_from_string(n.node_path);
    if (path == NULL)
      g_warning("Invalid Path");
    gtk_tree_view_expand_to_path(tree, path);
    gtk_tree_view_set_cursor(tree, path, NULL, FALSE);
    gtk_tree_path_free(path);
    g_free(n.node_path);
  }

  free(msg);
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

  util_get_setting("Game.Mods", last_mod_callback);

  free(msg);

  close(*out_fd);
  free(out_fd);
  g_spawn_close_pid(pid);
}

void tree_view_selection_changed(GtkTreeView * tree_view, gpointer data)
{
  GtkTreePath * path;
  GtkTreeIter iter;
  gchar * key;
  char url[256];

  gtk_tree_view_get_cursor(tree_view, &path, NULL);

  if (path == NULL)
    return;

  gtk_tree_model_get_iter(GTK_TREE_MODEL(tree_store), &iter, path);

  gtk_tree_model_get(GTK_TREE_MODEL(tree_store), &iter, 
		     KEY_COLUMN, &key,
		     -1);

  if (!key)
  {
    gtk_tree_path_free(path);
    return;
  }

  sprintf(url, "http://localhost:%d/mods/%s/mod.html", WEBSERVER_PORT, key);

  webkit_web_view_load_uri(browser, url);

  g_free(key);
  gtk_tree_path_free(path);
}

void make_tree_view(void)
{
  GtkTreeIter iter;
  GtkCellRenderer * pixbuf_renderer, * text_renderer;
  GtkTreeViewColumn * icon_column, * name_column;

  tree_store = gtk_tree_store_new(N_COLUMNS, GDK_TYPE_PIXBUF, 
				  G_TYPE_STRING, G_TYPE_STRING);

  gtk_tree_store_append(tree_store, &iter, NULL);
  gtk_tree_store_set(tree_store, &iter, 
		     NAME_COLUMN, "MODS",
		     -1);

  tree = GTK_TREE_VIEW(gtk_tree_view_new_with_model(GTK_TREE_MODEL(tree_store)));
  g_object_set(tree, "headers-visible", FALSE, NULL);
  g_object_set(tree, "level-indentation", 1, NULL);
  g_signal_connect(tree, "cursor-changed", 
		   G_CALLBACK(tree_view_selection_changed), NULL);

  pixbuf_renderer = gtk_cell_renderer_pixbuf_new();
  text_renderer = gtk_cell_renderer_text_new();

  icon_column = gtk_tree_view_column_new_with_attributes
    ("Icon", pixbuf_renderer,
     "pixbuf", ICON_COLUMN,
     NULL);

  gtk_tree_view_column_set_sizing(icon_column, GTK_TREE_VIEW_COLUMN_AUTOSIZE);

  name_column = gtk_tree_view_column_new_with_attributes
    ("Name", text_renderer,
     "text", NAME_COLUMN,
     NULL);

  gtk_tree_view_column_set_sizing(name_column, GTK_TREE_VIEW_COLUMN_AUTOSIZE);

  gtk_tree_view_append_column(tree, icon_column);
  gtk_tree_view_append_column(tree, name_column);
}

int main(int argc, char ** argv)
{

  GtkWidget * hbox;
  server_init(WEBSERVER_PORT);
  
  gtk_init(&argc, &argv);

  window = GTK_WINDOW(gtk_window_new(GTK_WINDOW_TOPLEVEL));
  gtk_window_set_title(window, "OpenRA Launcher");
  gtk_window_set_default_size(window, 800, 600);

  browser = WEBKIT_WEB_VIEW(webkit_web_view_new());
  g_signal_connect(browser, "window-object-cleared", 
		   G_CALLBACK(bind_js_bridge), 0);


  make_tree_view();
 
  util_get_mod_list(mod_list_callback);

  hbox = gtk_hbox_new(FALSE, 0);

  gtk_widget_set_size_request(GTK_WIDGET(tree), 250, 0);

  gtk_box_pack_end(GTK_BOX(hbox), GTK_WIDGET(browser), TRUE, TRUE, 0);
  gtk_box_pack_start(GTK_BOX(hbox), GTK_WIDGET(tree), TRUE, TRUE, 0);

  gtk_container_add(GTK_CONTAINER(window), hbox);

  gtk_widget_show_all(GTK_WIDGET(window));
  g_signal_connect(window, "delete-event", G_CALLBACK(window_delete), 0);
  
  gtk_main();

  return 0;
}
