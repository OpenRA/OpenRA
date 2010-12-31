/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>
#include <glib.h>
#include <sys/wait.h>

#include "utility.h"

gboolean util_do_command_async(gchar * command, GChildWatchFunc callback, gpointer user_data)
{
  GPid child_pid;
  gchar * spawn_args[] = { "mono", "OpenRA.Utility.exe", command, NULL };
  callback_data * d = (callback_data *)g_malloc(sizeof(callback_data));
  gboolean result;

  result = g_spawn_async_with_pipes(NULL, spawn_args, NULL,
			     G_SPAWN_SEARCH_PATH | G_SPAWN_DO_NOT_REAP_CHILD,
			     NULL, NULL, &child_pid, NULL, &(d->output_fd), NULL, NULL);

  if (!result)
  {
    g_free(d);
    return FALSE;
  }

  d->user_data = user_data;
  g_child_watch_add(child_pid, callback, d);

  return TRUE;
}

gboolean util_get_mod_list (GChildWatchFunc callback)
{
  return util_do_command_async("-l", callback, NULL);
}

gboolean util_do_command_blocking(gchar * command, GChildWatchFunc callback)
{
  GPid child_pid;
  int status;
  gint * out_fd = (gint *)g_malloc(sizeof(gint));
  gchar * spawn_args[] = { "mono", "OpenRA.Utility.exe", command, NULL };
  gboolean result;

  result = g_spawn_async_with_pipes(NULL, spawn_args, NULL,
			     G_SPAWN_SEARCH_PATH | G_SPAWN_DO_NOT_REAP_CHILD,
			     NULL, NULL, &child_pid, NULL, out_fd, NULL, NULL);

  if (!result)
  {
    return FALSE;
  }

  waitpid(child_pid, &status, 0);

  callback(child_pid, status, out_fd);

  return TRUE;
}

gboolean util_get_mod_metadata(gchar const * mod, GChildWatchFunc callback)
{
  GString * util_args = g_string_new(NULL);
  gboolean return_val;

  g_string_printf(util_args, "-i=%s", mod);
  return_val = util_do_command_blocking(util_args->str, callback);
  g_string_free(util_args, TRUE);
  return return_val;
}

gboolean util_get_setting(gchar const * setting, GChildWatchFunc callback)
{
  GString * command = g_string_new(NULL);
  gboolean return_val;
  
  g_string_printf(command, "--settings-value=~/.openra,%s", setting);
  return_val = util_do_command_blocking(command->str, callback);
  g_string_free(command, TRUE);
  return return_val;
}

gint util_spawn_with_command(gchar const * command, gchar const * arg1, gchar const * arg2, GPid * pid)
{
  GString * complete_command = g_string_new(NULL);
  gint out_fd;
  gboolean result;

  gchar * launch_args[] = { "mono", "OpenRA.Utility.exe", NULL, NULL };

  if (arg2 == NULL)
    g_string_printf(complete_command, "%s%s", command, arg1);
  else
    g_string_printf(complete_command, "%s%s,%s", command, arg1, arg2);

  launch_args[2] = complete_command->str;

  result = g_spawn_async_with_pipes(NULL, launch_args, NULL, G_SPAWN_SEARCH_PATH,
            NULL, NULL, pid, NULL, &out_fd, NULL, NULL);

  g_string_free(complete_command, TRUE);

  if (!result)
  {
    return 0;
  }

  return out_fd;
}

gint util_do_download(gchar const * url, gchar const * dest, GPid * pid)
{
  return util_spawn_with_command("--download-url=", url, dest, pid);
}

gint util_do_extract(gchar const * target, gchar const * dest, GPid * pid)
{
  return util_spawn_with_command("--extract-zip=", target, dest, pid);
}

gboolean util_do_http_request(gchar const * url, GChildWatchFunc callback, gpointer user_data)
{
  gboolean b;
  GString * command = g_string_new(NULL);
  g_string_printf(command, "--download-url=%s", url);
  b = util_do_command_async(command->str, callback, user_data);
  g_string_free(command, TRUE);
  return b;
}

GString * util_get_output(int fd)
{
  char buffer[1024];
  GString * msg = g_string_new(NULL);
  int read_bytes = 0;
  while (0 != (read_bytes = read(fd, buffer, 1024)))
  {
    if (-1 == read_bytes)
    {
      g_error("Error reading from command output");
      g_string_free(msg, TRUE);
      return NULL;
    }

    g_string_append_len(msg, buffer, read_bytes);
  }

  g_string_append_c(msg, '\0');

  return msg;
}
