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

int util_get_mod_list (GChildWatchFunc callback)
{
  GPid child_pid;
  gint * out_fd = (gint *)malloc(sizeof(gint));
  char * spawn_args[] = { "mono", "OpenRA.Utility.exe", "-l", NULL };
  gboolean result = g_spawn_async_with_pipes(NULL, spawn_args, NULL, 
			   G_SPAWN_SEARCH_PATH | G_SPAWN_DO_NOT_REAP_CHILD,
			   NULL, NULL, &child_pid, NULL, out_fd, NULL, NULL);

  if (!result)
  {
    return FALSE;
  }

  g_child_watch_add(child_pid, callback, out_fd);

  return TRUE;
}

int util_get_mod_metadata(char const * mod, GChildWatchFunc callback)
{
  GPid child_pid;
  int status;
  gint * out_fd = (gint *)malloc(sizeof(gint));
  char * spawn_args[] = { "mono", "OpenRA.Utility.exe", NULL, NULL };
  char util_args[32];
  gboolean result;
  sprintf(util_args, "-i=%s", mod);
  spawn_args[2] = util_args;

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
