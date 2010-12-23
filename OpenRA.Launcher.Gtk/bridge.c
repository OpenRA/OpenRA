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

#include <webkit/webkit.h>
#include <JavaScriptCore/JavaScript.h>
#include <glib.h>

#include "main.h"
#include "utility.h"

#define JS_STR(str) JSStringCreateWithUTF8CString(str)
#define JS_FUNC(ctx, callback) JSObjectMakeFunctionWithCallback(ctx, NULL, \
						  callback)

int js_check_num_args(JSContextRef ctx, char const * func_name, int argc, int num_expected, JSValueRef * exception)
{
  char buf[64];
  if (argc < num_expected)
  {
    sprintf(buf, "%s: Not enough args, expected %d got %d", func_name, num_expected, argc);
    *exception = JSValueMakeString(ctx, JS_STR(buf));
    return 0;
  }
  return 1;
}

char * js_get_cstr_from_val(JSContextRef ctx, JSValueRef val, size_t * size)
{
  char * buf;
  size_t str_size;
  JSStringRef str = JSValueToStringCopy(ctx, val, NULL);
  str_size = JSStringGetMaximumUTF8CStringSize(str);
  buf = (char *)malloc(str_size);
  *size = JSStringGetUTF8CString(str, buf, str_size);
  return buf;
}

JSValueRef js_log(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
		  size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  JSValueRef return_value = JSValueMakeNull(ctx);
  if (!js_check_num_args(ctx, "log", argc, 1, exception))
    return return_value;

  if (JSValueIsString(ctx, argv[0]))
  {
    char * buffer;
    size_t str_size;
    buffer = js_get_cstr_from_val(ctx, argv[0], &str_size);
    g_message("JS Log: %s", buffer);
    free(buffer);
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
  char * mod_buf, * file_buf, search_path[512];
  size_t mod_size, file_size;
  JSValueRef return_value = JSValueMakeNumber(ctx, 0);
  FILE * f;
  if (!js_check_num_args(ctx, "existsInMod", argc, 2, exception))
    return JSValueMakeNull(ctx);

  if (!JSValueIsString(ctx, argv[0]) || !JSValueIsString(ctx, argv[1]))
  {
    *exception = JSValueMakeString(ctx, JS_STR("One or more args are incorrect types."));
    return JSValueMakeNull(ctx);
  }

  file_buf = js_get_cstr_from_val(ctx, argv[0], &file_size);
  mod_buf = js_get_cstr_from_val(ctx, argv[1], &mod_size);
  
  sprintf(search_path, "mods/%s/%s", mod_buf, file_buf);

  free(file_buf);
  free(mod_buf);
 
  g_message("JS ExistsInMod: Looking for %s", search_path);

  f = fopen(search_path, "r");

  if (f != NULL)
  {
    g_message("JS ExistsInMod: Found");
    fclose(f);
    return_value = JSValueMakeNumber(ctx, 1);
  }

  g_message("JS ExistsInMod: Not found");

  return return_value;
}

JSValueRef js_launch_mod(JSContextRef ctx, JSObjectRef func, 
			 JSObjectRef this, size_t argc,
			 const JSValueRef argv[], JSValueRef * exception)
{
  char * mod_key, * mod_list;
  size_t mod_size;
  mod_t * mod;
  int offset;
  JSValueRef return_value = JSValueMakeNull(ctx);

  if (!js_check_num_args(ctx, "launchMod", argc, 1, exception))
    return return_value;

  if (!JSValueIsString(ctx, argv[0]))
  {
    *exception = JSValueMakeString(ctx, JS_STR("One or more args are incorrect types."));
    return return_value;
  }

  mod_key = js_get_cstr_from_val(ctx, argv[0], &mod_size);

  g_message("JS LaunchMod: %s", mod_key);

  mod = get_mod(mod_key);

  offset = strlen(mod_key);
  mod_list = (char *)malloc(offset + 1);
  strcpy(mod_list, mod_key);

  free(mod_key);

  while (strlen(mod->requires) > 0)
  {
    char r[MOD_requires_MAX_LEN], * comma;
    strcpy(r, mod->requires);
    if (NULL != (comma = strchr(r, ',')))
    {
      *comma = '\0';
    }

    mod = get_mod(r);
    if (mod == NULL)
    {
      char exception_msg[64];
      sprintf(exception_msg, "The mod %s is missing, cannot launch.", r);
      *exception = JSValueMakeString(ctx, JS_STR(exception_msg));
      free(mod_list);
      return return_value;
    }

    mod_list = (char *)realloc(mod_list, offset + strlen(r) + 1);
    sprintf(mod_list + offset, ",%s", r);
    offset += strlen(r) + 1;
  }

  {
    char * launch_args[] = { "mono", "OpenRA.Game.exe", NULL, NULL };
    char * game_mods_arg;

    game_mods_arg = (char *)malloc(strlen(mod_list) + strlen("Game.Mods=") + 1);
    sprintf(game_mods_arg, "Game.Mods=%s", mod_list);

    launch_args[2] = game_mods_arg;
    
    g_spawn_async(NULL, launch_args, NULL, G_SPAWN_SEARCH_PATH, NULL, NULL, NULL, NULL);
    free(game_mods_arg);
  }
  free(mod_list);
  return return_value;
}

typedef struct download_t 
{
  char key[32];
  char url[128];
  char dest[128];
  int current_bytes;
  int total_bytes;
  JSContextGroupRef ctx_group;
  JSObjectRef download_progressed_func;
  JSValueRef status;
  JSValueRef error;
  GIOChannel * output_channel;
  GPid pid;
} download_t;

#define MAX_DOWNLOADS 16

static download_t downloads[MAX_DOWNLOADS];
static int num_downloads = 0;

download_t * find_download(char const * key)
{
  int i;
  for (i = 0; i < num_downloads; i++)
  {
    if (0 == strcmp(downloads[i].key, key))
      return downloads + i;
  }
  return NULL;
}

JSValueRef js_register_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  char * key, * url, * filename;
  size_t key_size, url_size, filename_size;
  download_t * download;
  JSValueRef o;
  FILE * f;
  if (!js_check_num_args(ctx, "registerDownload", argc, 3, exception))
    return JSValueMakeNull(ctx);

  key = js_get_cstr_from_val(ctx, argv[0], &key_size);

  g_message("JS RegisterDownload: Registering %s", key);

  if (NULL == (download = find_download(key)))
  {
    download = downloads + num_downloads++;
    if (num_downloads >= MAX_DOWNLOADS)
    {
      num_downloads = MAX_DOWNLOADS - 1;
      return JSValueMakeNull(ctx);
    }
  }

  memset(download, 0, sizeof(download_t));

  download->ctx_group = JSContextGetGroup(ctx);
  o = JSObjectGetProperty(ctx, JSContextGetGlobalObject(ctx), JS_STR("downloadProgressed"), NULL);
  download->download_progressed_func = JSValueToObject(ctx, o, NULL);

  strncpy(download->key, key, 31);
  download->key[31] = '\0';

  free(key);

  url = js_get_cstr_from_val(ctx, argv[1], &url_size);
  strncpy(download->url, url, 127);
  download->url[127] = '\0';
  free(url);

  filename = js_get_cstr_from_val(ctx, argv[2], &filename_size);
  //TODO Clean filename to stop access to locations it shouldn't be allowed to access
  sprintf(download->dest, "/tmp/%s", filename);
  download->dest[127] = '\0';
  free(filename);

  f = fopen(download->dest, "r");

  if (NULL != f)
  {
    fclose(f);
    download->status = JSValueMakeString(ctx, JS_STR("DOWNLOADED"));
  }
  else
    download->status = JSValueMakeString(ctx, JS_STR("AVAILABLE"));
  
  return JSValueMakeNull(ctx);
}

gboolean update_download_stats(GIOChannel * source, GIOCondition condition, gpointer data)
{
  download_t * download = (download_t *)data;
  gchar * line;
  gsize line_length;
  GIOStatus io_status;
  JSValueRef args[1];
  JSContextRef ctx;

  ctx = JSGlobalContextCreateInGroup(download->ctx_group, NULL);
 
  switch(condition)
  {
  case G_IO_IN:
    io_status = g_io_channel_read_line(source, &line, &line_length, NULL, NULL);
    if (G_IO_STATUS_NORMAL == io_status)
    {
      if (0 == memcmp(line, "Error:", 6))
      {
        download->status = JSValueMakeString(ctx, JS_STR("ERROR"));
        download->error = JSValueMakeString(ctx, JS_STR(line + 7));
      }
      else
      {
	      download->status = JSValueMakeString(ctx, JS_STR("DOWNLOADING"));
	      GRegex * pattern = g_regex_new("(\\d{1,3})% (\\d+)/(\\d+) bytes", 0, 0, NULL);
	      GMatchInfo * match;
	      if (g_regex_match(pattern, line, 0, &match))
	      {
	        gchar * current = g_match_info_fetch(match, 2), * total = g_match_info_fetch(match, 3);
	        download->current_bytes = atoi(current);
       	  download->total_bytes = atoi(total);
	        g_free(current);
	        g_free(total);
	      }
	      g_free(match);
      }
    }
    g_free(line);
    break;
  case G_IO_HUP:
    if (!JSStringIsEqualToUTF8CString(JSValueToStringCopy(ctx, download->status, NULL), "ERROR"))
      download->status = JSValueMakeString(ctx, JS_STR("DOWNLOADED"));
    g_io_channel_shutdown(source, FALSE, NULL);
    break;
  default:
    break;
  }

  args[0] = JSValueMakeString(ctx, JS_STR(download->key));
  JSObjectCallAsFunction(ctx, download->download_progressed_func, NULL, 1, args, NULL);

  JSGlobalContextRelease((JSGlobalContextRef)ctx);
  return TRUE;
}

JSValueRef js_start_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  char * key;
  size_t key_size;
  download_t * download;
  int fd;
  GPid pid;

  if (!js_check_num_args(ctx, "startDownload", argc, 1, exception))
    return JSValueMakeNull(ctx);  

  key = js_get_cstr_from_val(ctx, argv[0], &key_size);

  if (NULL == (download = find_download(key)))
  {
    free(key);
    return JSValueMakeBoolean(ctx, 0);
  }

  free(key);

  g_message("Starting download %s", download->key);

  download->status = JSValueMakeString(ctx, JS_STR("DOWNLOADING"));

  fd = util_do_download(download->url, download->dest, &pid);

  if (!fd)
    return JSValueMakeBoolean(ctx, 0);

  download->pid = pid;
  download->output_channel = g_io_channel_unix_new(fd);

  g_io_add_watch(download->output_channel, G_IO_IN | G_IO_HUP, update_download_stats, download);

  return JSValueMakeBoolean(ctx, 1);
}

JSValueRef js_cancel_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  char * key;
  size_t key_size;
  download_t * download;

  if (!js_check_num_args(ctx, "cancelDownload", argc, 1, exception))
    return JSValueMakeNull(ctx);

  key = js_get_cstr_from_val(ctx, argv[0], &key_size);

  if (NULL == (download = find_download(key)))
  {
    free(key);
    return JSValueMakeBoolean(ctx, 0);
  }

  if (download->pid)
  {
    download->status = JSValueMakeString(ctx, JS_STR("ERROR"));
    download->error = JSValueMakeString(ctx, JS_STR("Download Cancelled"));
    kill(download->pid, SIGTERM);
    remove(download->dest);
  }

  free(key);
  return JSValueMakeBoolean(ctx, 1);
}

JSValueRef js_download_status(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
			      size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  char * key;
  size_t key_size;
  download_t * download;

  if (!js_check_num_args(ctx, "downloadStatus", argc, 1, exception))
    return JSValueMakeNull(ctx);

  key = js_get_cstr_from_val(ctx, argv[0], &key_size);

  if (NULL == (download = find_download(key)))
  {
    free(key);
    return JSValueMakeString(ctx, JS_STR("NOT_REGISTERED"));
  }

  free(key);

  return download->status;
}

JSValueRef js_download_error(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
			     size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  char * key;
  size_t key_size;
  download_t * download;

  if (!js_check_num_args(ctx, "downloadError", argc, 1, exception))
    return JSValueMakeNull(ctx);

  key = js_get_cstr_from_val(ctx, argv[0], &key_size);

  g_message("JS DownloadError: Retrieving error message for %s", key);

  if (NULL == (download = find_download(key)))
  {
    free(key);
    return JSValueMakeString(ctx, JS_STR(""));
  }

  free(key);

  return download->error;
}

JSValueRef js_bytes_completed(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  char * key;
  size_t key_size;
  download_t * download;

  if (!js_check_num_args(ctx, "bytesCompleted", argc, 1, exception))
    return JSValueMakeNull(ctx);

  key = js_get_cstr_from_val(ctx, argv[0], &key_size);

  if (NULL == (download = find_download(key)))
  {
    free(key);
    return JSValueMakeNumber(ctx, 0);
  }

  free(key);

  return JSValueMakeNumber(ctx, download->current_bytes);
}

JSValueRef js_bytes_total(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  char * key;
  size_t key_size;
  download_t * download;

  if (!js_check_num_args(ctx, "bytesTotal", argc, 1, exception))
    return JSValueMakeNull(ctx);

  key = js_get_cstr_from_val(ctx, argv[0], &key_size);

  if (NULL == (download = find_download(key)))
  {
    free(key);
    return JSValueMakeNumber(ctx, 0);
  }

  free(key);

  return JSValueMakeNumber(ctx, download->total_bytes);
}

JSValueRef js_extract_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  return JSValueMakeNull(ctx);
}

void js_add_functions(JSGlobalContextRef ctx, JSObjectRef target, char ** names, 
		      JSObjectCallAsFunctionCallback * callbacks, size_t count)
{
  int i;
  for (i = 0; i < count; i++)
  {
    JSObjectRef func = JS_FUNC(ctx, callbacks[i]);
    JSObjectSetProperty(ctx, target, JS_STR(names[i]), func, kJSPropertyAttributeNone, NULL);
  }
}

void bind_js_bridge(WebKitWebView * view, WebKitWebFrame * frame,
		    gpointer context, gpointer window_object,
		    gpointer user_data)
{
  JSGlobalContextRef js_ctx;
  JSObjectRef window_obj, external_obj;
  
  int func_count = 11;
  char * names[] = { "log", "existsInMod", "launchMod", "registerDownload",
		     "startDownload", "cancelDownload", "downloadStatus",
		     "downloadError", "bytesCompleted", "bytesTotal",
		     "extractDownload"};
  JSObjectCallAsFunctionCallback callbacks[] = { js_log, js_exists_in_mod, js_launch_mod,
						 js_register_download, js_start_download,
						 js_cancel_download, js_download_status,
						 js_download_error, js_bytes_completed,
						 js_bytes_total, js_extract_download };
  
  js_ctx = (JSGlobalContextRef)context;

  external_obj = JSObjectMake(js_ctx, NULL, NULL);

  window_obj = (JSObjectRef)window_object;
  JSObjectSetProperty(js_ctx, window_obj, JS_STR("external"),
		      external_obj, 0, NULL);

  js_add_functions(js_ctx, external_obj, names, callbacks, func_count);
}
