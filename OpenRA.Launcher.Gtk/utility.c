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

int util_do_command_async(char * command, GChildWatchFunc callback)
{
  GPid child_pid;
  gint * out_fd = (gint *)malloc(sizeof(gint));
  char * spawn_args[] = { "mono", "OpenRA.Utility.exe", command, NULL };
  gboolean result;

  result = g_spawn_async_with_pipes(NULL, spawn_args, NULL,
			     G_SPAWN_SEARCH_PATH | G_SPAWN_DO_NOT_REAP_CHILD,
			     NULL, NULL, &child_pid, NULL, out_fd, NULL, NULL);

  if (!result)
  {
    return FALSE;
  }

  g_child_watch_add(child_pid, callback, out_fd);

  return TRUE;
}

int util_get_mod_list (GChildWatchFunc callback)
{
  return util_do_command_async("-l", callback);
}

int util_do_command_blocking(char * command, GChildWatchFunc callback)
{
  GPid child_pid;
  int status;
  gint * out_fd = (gint *)malloc(sizeof(gint));
  char * spawn_args[] = { "mono", "OpenRA.Utility.exe", command, NULL };
  gboolean result;

  result = g_spawn_async_with_pipes(NULL, spawn_args, NULL,
			     G_SPAWN_SEARCH_PATH | G_SPAWN_DO_NOT_REAP_CHILD,
			     NULL, NULL, &child_pid, NULL, out_fd, NULL, NULL);

  if (!result)
  {
    return FALSE;
  }

  //g_child_watch_add(child_pid, callback, out_fd);
  waitpid(child_pid, &status, 0);

  callback(child_pid, status, out_fd);

  return TRUE;
}

int util_get_mod_metadata(char const * mod, GChildWatchFunc callback)
{
  char * util_args;
  int return_val;

  util_args = (char *)malloc(strlen(mod) + strlen("-i=") + 1);
  sprintf(util_args, "-i=%s", mod);
  return_val = util_do_command_blocking(util_args, callback);
  free(util_args);
  return return_val;
}

int util_get_setting(const char * setting, GChildWatchFunc callback)
{
  char * command;
  int return_val;
  
  command = (char *)malloc(strlen(setting) + strlen("--settings-value=~/.openra,") + 1);
  sprintf(command, "--settings-value=~/.openra,%s", setting);
  return_val = util_do_command_blocking(command, callback);
  free(command);
  return return_val;
}

int util_do_download(const char * url, const char * dest, GPid * pid)
{
  char * command;
  int out_fd;
  gboolean result;

  char * launch_args[] = { "mono", "OpenRA.Utility.exe", NULL, NULL };

  command = (char *)malloc(strlen(url) + strlen(dest) + strlen("--download-url=") + 2);
  sprintf(command, "--download-url=%s,%s", url, dest);

  launch_args[2] = command;

  result = g_spawn_async_with_pipes(NULL, launch_args, NULL, G_SPAWN_SEARCH_PATH,
				    NULL, NULL, pid, NULL, &out_fd, NULL, NULL);
  free(command);

  if (!result)
  {
    return 0;
  }
  return out_fd;
}

char * util_get_output(int fd, int * output_len)
{
  char buffer[1024], * msg = NULL;
  int read_bytes = 0;
  *output_len = 0;
  while (0 != (read_bytes = read(fd, buffer, 1024)))
  {
    if (-1 == read_bytes)
    {
      g_error("Error reading from command output");
      free(msg);
      *output_len = 0;
      return NULL;
    }

    *output_len += read_bytes;

    msg = (char *)realloc(msg, *output_len + 1);

    memcpy(msg + (*output_len - read_bytes), buffer, read_bytes);
  }

  msg[*output_len] = '\0';

  return msg;
}
