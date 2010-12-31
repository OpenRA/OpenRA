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

#include "main.h"
#include "server.h"
#include "bridge.h"
#include "utility.h"

#define WEBSERVER_PORT 48764

GtkWindow * window;
WebKitWebView * browser;
GtkTreeStore * tree_store;
GtkTreeView * tree;
GdkPixbuf * generic_mod_icon;

static mod_t mods[MAX_NUM_MODS];
static int mod_count = 0;

void free_mod(mod_t * mod)
{
  g_free(mod->key);
  g_free(mod->title);
  g_free(mod->version);
  g_free(mod->author);
  g_free(mod->description);
  g_free(mod->requires);
}

gboolean window_delete(GtkWidget * widget, GdkEvent * event, 
		       gpointer user_data)
{
  int i;
  server_teardown();
  for (i = 0; i < mod_count; i++)
    free_mod(mods + i);
  gtk_main_quit();
  return FALSE;
}

typedef void ( * lines_callback ) (GString const * line, gpointer data);

//Splits console output into lines and passes each one to a callback
void process_lines(GString * lines, lines_callback cb, gpointer data)
{
  int prev = 0, current = 0;
  while (current < lines->len)
  {
    if (lines->str[current] == '\n')
    {
      GString * line = g_string_new_len(lines->str + prev, current - prev);
      cb(line, data);
      g_string_free(line, TRUE);
      prev = current + 1;
    }
    current++;
  }
}

#define ASSIGN_TO_MOD(FIELD) \
  mod->FIELD = g_strdup(val_start)

void mod_metadata_line(GString const * line, gpointer data)
{
  mod_t * mod = (mod_t *)data;
  gchar * val_start = g_strstr_len(line->str, -1, ":") + 2;
  if (g_str_has_prefix(line->str, "Mod:"))
  {
    ASSIGN_TO_MOD(key);
  }
  else if (g_str_has_prefix(line->str, "  Title:"))
  {
    ASSIGN_TO_MOD(title);
  }
  else if (g_str_has_prefix(line->str, "  Version:"))
  {
    ASSIGN_TO_MOD(version);
  }
  else if (g_str_has_prefix(line->str, "  Author:"))
  {
    ASSIGN_TO_MOD(author);
  }
  else if (g_str_has_prefix(line->str, "  Description:"))
  {
    ASSIGN_TO_MOD(description);
  }
  else if (g_str_has_prefix(line->str, "  Requires:"))
  {
    ASSIGN_TO_MOD(requires);
  }
  else if (g_str_has_prefix(line->str, "  Standalone:"))
  {
    if (strcmp(val_start, "True") == 0)
      mod->standalone = TRUE;
    else
      mod->standalone = FALSE;
  }
}

mod_t * get_mod(gchar const * key)
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
		       ICON_COLUMN, generic_mod_icon,
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
  int * out_fd = (int *)data;
  GString * msg = NULL;
  mod_t * mod = mods + mod_count;
  GtkTreeIter iter, mod_iter;

  mod_count = (mod_count + 1) % MAX_NUM_MODS;

  free_mod(mod);
  memset(mod, 0, sizeof(mod_t));

  msg = util_get_output(*out_fd);

  close(*out_fd);
  free(out_fd);

  if (!msg)
    return;

  process_lines(msg, mod_metadata_line, mod);

  g_string_free(msg, TRUE);

  gtk_tree_model_get_iter_first(GTK_TREE_MODEL(tree_store), &mod_iter);

  if (mod->standalone)
  {
    gtk_tree_store_append(tree_store, &iter, &mod_iter);
    gtk_tree_store_set(tree_store, &iter,
		       ICON_COLUMN, generic_mod_icon,
		       KEY_COLUMN, mod->key,
		       NAME_COLUMN, mod->title,
		       -1);
  }
  else if (!strlen(mod->requires))
  {
    GtkTreeIter broken_mods_iter;
    if (!gtk_tree_model_get_iter_from_string(GTK_TREE_MODEL(tree_store), 
					     &broken_mods_iter, "1"))
    {
      gtk_tree_store_append(tree_store, &broken_mods_iter, NULL);
    }
    gtk_tree_store_set(tree_store, &broken_mods_iter,
		       ICON_COLUMN, generic_mod_icon,
		       KEY_COLUMN, mod->key,
		       NAME_COLUMN, mod->title,
		       -1);
  }
  else
  {
    gtk_tree_model_foreach(GTK_TREE_MODEL(tree_store), append_to_mod, mod);
  }
}

typedef struct tree_node
{
  gchar const * key;
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
  int * out_fd = (int *)data;
  gchar * comma_pos = 0, * newline_pos = 0;
  GString * msg = NULL;
  tree_node n;

  memset(&n, 0, sizeof(tree_node));

  msg = util_get_output(*out_fd);

  close(*out_fd);
  free(out_fd);

  if (!msg)
    return;

  if (g_str_has_prefix(msg->str, "Error:"))
  {
    g_string_truncate(msg, 2);
    g_string_overwrite(msg, 0, "ra");
  }
  else if (NULL != (comma_pos = g_strstr_len(msg->str, -1, ",")))
    *comma_pos = '\0';
  else if (NULL != (newline_pos = g_strstr_len(msg->str, -1, "\n")))
    *newline_pos = '\0';

  n.key = msg->str;

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

  g_string_free(msg, TRUE);
}

void mod_list_line(GString const * mod, gpointer user)
{
  util_get_mod_metadata(mod->str, mod_metadata_callback);
}

void mod_list_callback(GPid pid, gint status, gpointer data)
{
  callback_data * d = (callback_data *)data;
  GString * msg = NULL;
  
  msg = util_get_output(d->output_fd);

  close(d->output_fd);
  g_free(d);

  if (!msg)
    return;

  mod_count = 0;

  process_lines(msg, mod_list_line, NULL);

  util_get_setting("Game.Mods", last_mod_callback);

  g_string_free(msg, TRUE);
  
  g_spawn_close_pid(pid);
}

void tree_view_selection_changed(GtkTreeView * tree_view, gpointer data)
{
  GtkTreePath * path;
  GtkTreeIter iter;
  gchar * key;
  GString * url;

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

  url = g_string_new(NULL);

  g_string_printf(url, "http://localhost:%d/mods/%s/mod.html", WEBSERVER_PORT, key);

  webkit_web_view_load_uri(browser, url->str);

  g_free(key);
  gtk_tree_path_free(path);
  g_string_free(url, TRUE);
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

  tree = GTK_TREE_VIEW(gtk_tree_view_new_with_model(
					GTK_TREE_MODEL(tree_store)));
  g_object_set(tree, "headers-visible", FALSE, NULL);
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

  generic_mod_icon = gdk_pixbuf_new_from_file_at_size("soviet-logo.png", 
						      16, 16, NULL);

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
