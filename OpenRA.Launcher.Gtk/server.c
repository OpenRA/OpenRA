/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#include <stdio.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/stat.h>
#include <stdint.h>
#include <fcntl.h>
#include <string.h>

#define MHD_PLATFORM_H
#include <microhttpd.h>

#include <glib.h>

struct MHD_Daemon * server;

int http_get_args(void * cls, enum MHD_ValueKind kind, 
		  const char * key, const char * value)
{
  g_message("%s: %s", key, value);
  return MHD_YES;
}

int try_file_response(const char * url, struct MHD_Connection * connection)
{
  int fd, ret;
  struct MHD_Response * response;
  struct stat sbuf;

  g_message("Opening %s", url + 1);
  
  if ((-1 == (fd = open(url + 1, O_RDONLY))) || 
      (0 != fstat(fd, &sbuf)))
  {
    return MHD_NO;
  }

  response = MHD_create_response_from_fd(sbuf.st_size, fd);
  MHD_add_response_header(response, "Content-Type", "text/html");
  ret = MHD_queue_response(connection, MHD_HTTP_OK, response);
  MHD_destroy_response(response);
  return ret;
}

int access_handler_callback(void * userdata, 
			    struct MHD_Connection * connection,
			    const char * url,
			    const char * method,
			    const char * version,
			    const char * upload_data,
			    size_t * upload_data_size,
			    void ** userpointer)
{
  struct MHD_Response * response;
  int ret = MHD_NO;
  char * text = "1";
  g_message(url);

  if ((ret = try_file_response(url, connection)))
    return ret;
  
  text = "<html><head><title>Not found</title></head><body>File not found</body></html>";

  response = MHD_create_response_from_data(strlen(text), (void *)text,
					   MHD_NO, MHD_NO);

  ret = MHD_queue_response(connection, MHD_HTTP_NOT_FOUND, response);
  MHD_destroy_response(response);

  return ret;
}

int server_init(int port)
{
  server = MHD_start_daemon(MHD_USE_DEBUG | MHD_USE_SELECT_INTERNALLY,
			    port, NULL, NULL, 
			    &access_handler_callback, NULL,
			    MHD_OPTION_END);

  if (!server)
  {
    g_critical("Could not start web server.");
    return 0;
  }
  g_message("Server initialised.");
  return 1;
}

void * server_get_daemon(void)
{
  return server;
}

void server_teardown(void)
{
  MHD_stop_daemon(server);
}
