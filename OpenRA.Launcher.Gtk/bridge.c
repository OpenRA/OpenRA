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

#define JS_TRUE JSValueMakeBoolean(ctx, TRUE)
#define JS_FALSE JSValueMakeBoolean(ctx, FALSE)
#define JS_NULL JSValueMakeNull(ctx)

GString * sanitize_path(gchar const * path)
{
  gchar * basename = g_path_get_basename(path);
  gchar * dirname = g_path_get_dirname(path);
  gchar ** frags = g_strsplit(dirname, "/", -1);
  gint offset = 0;
  GString * new_path = g_string_new(NULL);
  while (*(frags + offset))
  {
    if ((strcmp(*(frags + offset), "..") == 0) || (strcmp(*(frags + offset), ".") == 0))
    {
      offset++;
      continue;
    }
    g_string_append(new_path, *(frags + offset));
    g_string_append_c(new_path, G_DIR_SEPARATOR);
    offset++;
  }
  g_string_append(new_path, basename);
  g_free(basename);
  g_free(dirname);
  g_strfreev(frags);
  return new_path;
}

int js_check_num_args(JSContextRef ctx, gchar const * func_name, int argc, int num_expected, JSValueRef * exception)
{
  GString * buf;
  if (argc < num_expected)
  {
    buf = g_string_new(NULL);
    g_string_printf(buf, "%s: Not enough args, expected %d got %d", func_name, num_expected, argc);
    *exception = JSValueMakeString(ctx, JS_STR(buf->str));
    g_string_free(buf, TRUE);
    return 0;
  }
  return 1;
}

GString * js_get_cstr_from_val(JSContextRef ctx, JSValueRef val)
{
  gchar * buf;
  GString * ret;
  size_t len;
  JSStringRef str;
  if (!JSValueIsString(ctx, val))
    return NULL;
  str = JSValueToStringCopy(ctx, val, NULL);
  len = JSStringGetMaximumUTF8CStringSize(str);
  buf = (gchar *)g_malloc(len);
  ret = g_string_sized_new(JSStringGetUTF8CString(str, buf, len));
  g_string_assign(ret, buf);
  g_free(buf);
  return ret;
}

JSValueRef js_log(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
        size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  if (!js_check_num_args(ctx, "log", argc, 1, exception))
    return JS_NULL;

  GString * buffer;
  buffer = js_get_cstr_from_val(ctx, argv[0]);
  if (!buffer)
    return JS_NULL;
  g_message("JS Log: %s", buffer->str);
  g_string_free(buffer, TRUE);
  return JS_NULL;
}

JSValueRef js_exists_in_mod(JSContextRef ctx, JSObjectRef func, 
        JSObjectRef this, size_t argc, 
        const JSValueRef argv[], JSValueRef * exception)
{
  GString * mod_buf, * file_buf, * search_path, * search_path_sanitized;
  JSValueRef return_value = JS_FALSE;
  FILE * f;
  if (!js_check_num_args(ctx, "existsInMod", argc, 2, exception))
    return JS_NULL;

  file_buf = js_get_cstr_from_val(ctx, argv[0]);
  if (!file_buf)
    return JS_NULL;
  mod_buf = js_get_cstr_from_val(ctx, argv[1]);
  if (!mod_buf)
    return JS_NULL;
  
  search_path = g_string_new(NULL);
  g_string_printf(search_path, "mods/%s/%s", mod_buf->str, file_buf->str);
  search_path_sanitized = sanitize_path(search_path->str);

  g_string_free(search_path, TRUE);
  g_string_free(file_buf, TRUE);
  g_string_free(mod_buf, TRUE);
 
  g_message("JS ExistsInMod: Looking for %s", search_path_sanitized->str);

  f = fopen(search_path_sanitized->str, "r");

  g_string_free(search_path_sanitized, TRUE);

  if (f != NULL)
  {
    g_message("JS ExistsInMod: Found");
    fclose(f);
    return_value = JS_TRUE;
  }
  else
    g_message("JS ExistsInMod: Not found");

  return return_value;
}

JSValueRef js_launch_mod(JSContextRef ctx, JSObjectRef func, 
			 JSObjectRef this, size_t argc,
			 const JSValueRef argv[], JSValueRef * exception)
{
  GString * mod_key, * mod_list;
  mod_t * mod;

  if (!js_check_num_args(ctx, "launchMod", argc, 1, exception))
    return JS_NULL;

  if (!JSValueIsString(ctx, argv[0]))
  {
    *exception = JSValueMakeString(ctx, JS_STR("One or more args are incorrect types."));
    return JS_NULL;
  }

  mod_key = js_get_cstr_from_val(ctx, argv[0]);

  g_message("JS LaunchMod: %s", mod_key->str);

  mod = get_mod(mod_key->str);

  mod_list = g_string_new(mod_key->str);

  g_string_free(mod_key, TRUE);

  while (strlen(mod->requires) > 0)
  {
    gchar * r = g_strdup(mod->requires), * comma;
    if (NULL != (comma = g_strstr_len(r, -1, ",")))
    {
      *comma = '\0';
    }

    mod = get_mod(r);
    if (mod == NULL)
    {
      GString * exception_msg = g_string_new(NULL);
      g_string_printf(exception_msg, "The mod %s is missing, cannot launch.", r);
      *exception = JSValueMakeString(ctx, JS_STR(exception_msg->str));
      g_string_free(exception_msg, TRUE);
      g_string_free(mod_list, TRUE);
      return JS_NULL;
    }
    g_string_append_printf(mod_list, ",%s", r);
    g_free(r);
  }

  {
    gchar * launch_args[] = { "mono", "OpenRA.Game.exe", NULL, "SupportDir=~/.openra", NULL };
    GString * game_mods_arg = g_string_new(NULL);
    g_string_printf(game_mods_arg, "Game.Mods=%s", mod_list->str);

    launch_args[2] = game_mods_arg->str;
    
    g_spawn_async(NULL, launch_args, NULL, G_SPAWN_SEARCH_PATH, NULL, NULL, NULL, NULL);
    g_string_free(game_mods_arg, TRUE);
  }
  g_string_free(mod_list, TRUE);
  return JS_NULL;
}

typedef struct download_t 
{
  gchar * key;
  gchar * url;
  gchar * dest;
  int current_bytes;
  int total_bytes;
  JSContextGroupRef ctx_group;
  JSObjectRef download_progressed_func;
  JSObjectRef extraction_progressed_func;
  JSValueRef status;
  JSValueRef error;
  GIOChannel * output_channel;
  GPid pid;
} download_t;

#define MAX_DOWNLOADS 16

static download_t downloads[MAX_DOWNLOADS];
static int num_downloads = 0;

download_t * find_download(gchar const * key)
{
  int i;
  for (i = 0; i < num_downloads; i++)
  {
    if (0 == strcmp(downloads[i].key, key))
      return downloads + i;
  }
  return NULL;
}

void set_download_status(JSContextRef ctx, download_t * download, const char * status)
{
  JSValueUnprotect(ctx, download->status);

  download->status = JSValueMakeString(ctx, JS_STR(status));

  JSValueProtect(ctx, download->status);
}

void set_download_error(JSContextRef ctx, download_t * download, const char * error)
{
  JSValueUnprotect(ctx, download->error);

  download->error = JSValueMakeString(ctx, JS_STR(error));

  JSValueProtect(ctx, download->error);
}

void free_download(download_t * download)
{
  g_free(download->key);
  g_free(download->url);
  g_free(download->dest);
}

JSValueRef js_register_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key, * url, * filename;
  download_t * download;
  JSValueRef o;
  FILE * f;
  if (!js_check_num_args(ctx, "registerDownload", argc, 3, exception))
    return JS_NULL;

  key = js_get_cstr_from_val(ctx, argv[0]);

  g_message("JS RegisterDownload: Registering %s", key->str);

  if (NULL == (download = find_download(key->str)))
  {
    download = downloads + num_downloads++;
    if (num_downloads >= MAX_DOWNLOADS)
    {
      num_downloads = MAX_DOWNLOADS - 1;
      return JS_NULL;
    }
  }

  free_download(download);
  memset(download, 0, sizeof(download_t));

  download->ctx_group = JSContextGetGroup(ctx);
  o = JSObjectGetProperty(ctx, JSContextGetGlobalObject(ctx), JS_STR("downloadProgressed"), NULL);
  download->download_progressed_func = JSValueToObject(ctx, o, NULL);
  o = JSObjectGetProperty(ctx, JSContextGetGlobalObject(ctx), JS_STR("extractProgressed"), NULL);
  download->extraction_progressed_func = JSValueToObject(ctx, o, NULL);

  download->key = g_strdup(key->str);

  g_string_free(key, TRUE);

  url = js_get_cstr_from_val(ctx, argv[1]);
  download->url = g_strdup(url->str);
  g_string_free(url, TRUE);

  filename = js_get_cstr_from_val(ctx, argv[2]);
  {
    GString * path = g_string_new(NULL), * sanitized_path;
    g_string_printf(path, "/tmp/%s", filename->str);
    g_string_free(filename, TRUE);
    sanitized_path = sanitize_path(path->str);
    g_string_free(path, TRUE);
    download->dest = g_strdup(sanitized_path->str);
    g_string_free(sanitized_path, TRUE);
  }

  f = fopen(download->dest, "r");

  if (NULL != f)
  {
    fclose(f);
    set_download_status(ctx, download, "DOWNLOADED");
  }
  else
    set_download_status(ctx, download, "AVAILABLE");
  
  return JS_NULL;
}

gboolean update_download_stats(GIOChannel * source, GIOCondition condition, gpointer data)
{
  int ret = TRUE;
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
      if (g_str_has_prefix(line, "Error:"))
      {
        set_download_status(ctx, download, "ERROR");
        set_download_error(ctx, download, line + 7);
      }
      else
      {
	      set_download_status(ctx, download, "DOWNLOADING");
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
      set_download_status(ctx, download, "DOWNLOADED");
    g_io_channel_shutdown(source, FALSE, NULL);
    ret = FALSE;
    break;
  default:
    break;
  }

  args[0] = JSValueMakeString(ctx, JS_STR(download->key));
  JSObjectCallAsFunction(ctx, download->download_progressed_func, NULL, 1, args, NULL);

  return ret;
}

JSValueRef js_start_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key;
  download_t * download;
  int fd;
  GPid pid;

  if (!js_check_num_args(ctx, "startDownload", argc, 1, exception))
    return JS_NULL;  

  key = js_get_cstr_from_val(ctx, argv[0]);

  if (NULL == (download = find_download(key->str)))
  {
    g_string_free(key, TRUE);
    return JS_FALSE;
  }

  g_string_free(key, TRUE);

  g_message("Starting download %s", download->key);

  set_download_status(ctx, download, "DOWNLOADING");

  fd = util_do_download(download->url, download->dest, &pid);

  if (!fd)
    return JS_FALSE;

  download->pid = pid;
  download->output_channel = g_io_channel_unix_new(fd);

  g_io_add_watch(download->output_channel, G_IO_IN | G_IO_HUP, update_download_stats, download);

  return JS_TRUE;
}

JSValueRef js_cancel_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key;
  download_t * download;

  if (!js_check_num_args(ctx, "cancelDownload", argc, 1, exception))
    return JS_NULL;

  key = js_get_cstr_from_val(ctx, argv[0]);

  if (NULL == (download = find_download(key->str)))
  {
    g_string_free(key, TRUE);
    return JS_FALSE;
  }

  if (download->pid)
  {
    set_download_status(ctx, download, "ERROR");
    set_download_error(ctx, download, "Download Cancelled");
    kill(download->pid, SIGTERM);
    remove(download->dest);
  }

  g_string_free(key, TRUE);
  return JS_TRUE;
}

JSValueRef js_download_status(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
			      size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key;
  download_t * download;

  if (!js_check_num_args(ctx, "downloadStatus", argc, 1, exception))
    return JS_NULL;

  key = js_get_cstr_from_val(ctx, argv[0]);

  if (NULL == (download = find_download(key->str)))
  {
    g_string_free(key, TRUE);
    return JSValueMakeString(ctx, JS_STR("NOT_REGISTERED"));
  }

  g_string_free(key, TRUE);

  return download->status;
}

JSValueRef js_download_error(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
			     size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key;
  download_t * download;

  if (!js_check_num_args(ctx, "downloadError", argc, 1, exception))
    return JS_NULL;

  key = js_get_cstr_from_val(ctx, argv[0]);

  g_message("JS DownloadError: Retrieving error message for %s", key->str);

  if (NULL == (download = find_download(key->str)))
  {
    g_string_free(key, TRUE);
    return JSValueMakeString(ctx, JS_STR(""));
  }

  g_string_free(key, TRUE);

  return download->error;
}

JSValueRef js_bytes_completed(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key;
  download_t * download;

  if (!js_check_num_args(ctx, "bytesCompleted", argc, 1, exception))
    return JSValueMakeNull(ctx);

  key = js_get_cstr_from_val(ctx, argv[0]);

  if (NULL == (download = find_download(key->str)))
  {
    g_string_free(key, TRUE);
    return JSValueMakeNumber(ctx, -1);
  }

  g_string_free(key, TRUE);

  return JSValueMakeNumber(ctx, download->current_bytes);
}

JSValueRef js_bytes_total(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key;
  download_t * download;

  if (!js_check_num_args(ctx, "bytesTotal", argc, 1, exception))
    return JS_NULL;

  key = js_get_cstr_from_val(ctx, argv[0]);

  if (NULL == (download = find_download(key->str)))
  {
    g_string_free(key, TRUE);
    return JSValueMakeNumber(ctx, -1);
  }

  g_string_free(key, TRUE);

  return JSValueMakeNumber(ctx, download->total_bytes);
}

gboolean update_extraction_progress(GIOChannel * source, GIOCondition condition, gpointer data)
{
  int ret = TRUE;
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
    if ((G_IO_STATUS_NORMAL == io_status) && (g_str_has_prefix(line, "Error:")))
    {
      set_download_status(ctx, download, "ERROR");
      set_download_error(ctx, download, line + 7);
    }
    free(line);
    break;
  case G_IO_HUP:
    if (!JSStringIsEqualToUTF8CString(JSValueToStringCopy(ctx, download->status, NULL), "ERROR"))
      set_download_status(ctx, download, "EXTRACTED");
    g_io_channel_shutdown(source, FALSE, NULL);
    ret = FALSE;
    break;
  default:
    break;
  }

  args[0] = JSValueMakeString(ctx, JS_STR(download->key));
  JSObjectCallAsFunction(ctx, download->extraction_progressed_func, NULL, 1, args, NULL);

  return ret;  
}

JSValueRef js_extract_download(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * key, * dir, * mod, * status, * dest_path, * sanitized_dest_path;
  download_t * download;
  int fd;
  GPid pid;

  if (!js_check_num_args(ctx, "extractDownload", argc, 3, exception))
    return JS_NULL;

  key = js_get_cstr_from_val(ctx, argv[0]);

  if (NULL == (download = find_download(key->str)))
  {
    g_string_free(key, TRUE);
    return JS_FALSE;
  }

  g_string_free(key, TRUE);

  status = js_get_cstr_from_val(ctx, download->status);

  if (0 != strcmp(status->str, "DOWNLOADED"))
  {
    g_string_free(status, TRUE);
    return JSValueMakeBoolean(ctx, 0);
  }

  g_string_free(status, TRUE);

  set_download_status(ctx, download, "EXTRACTING");

  dir = js_get_cstr_from_val(ctx, argv[1]);
  mod = js_get_cstr_from_val(ctx, argv[2]);

  dest_path = g_string_new(NULL);
  g_string_printf(dest_path, "%s/%s", mod->str, dir->str);
  sanitized_dest_path = sanitize_path(dest_path->str);
  g_string_free(dest_path, TRUE);
  g_string_free(mod, TRUE);
  g_string_free(dir, TRUE);

  fd = util_do_extract(download->dest, sanitized_dest_path->str, &pid);
  g_string_free(sanitized_dest_path, TRUE);

  if (!fd)
    return JS_FALSE;

  download->pid = pid;
  download->output_channel = g_io_channel_unix_new(fd);

  g_io_add_watch(download->output_channel, G_IO_IN | G_IO_HUP, update_extraction_progress, download);

  return JS_TRUE;
}

JSValueRef js_metadata(JSContextRef ctx, JSObjectRef func, JSObjectRef this,
				size_t argc, const JSValueRef argv[], JSValueRef * exception)
{
  GString * field, * mod;

  if (!js_check_num_args(ctx, "metadata", argc, 2, exception))
    return JS_NULL;

  field = js_get_cstr_from_val(ctx, argv[0]);
  if (!field)
    return JS_NULL;

  mod = js_get_cstr_from_val(ctx, argv[1]);
  if (!mod)
  {
    g_string_free(field, TRUE);
    return JS_NULL;
  }

  if (0 == strcmp(field->str, "VERSION"))
  {
    mod_t * m = get_mod(mod->str);
    if (m)
    {
      g_string_free(mod, TRUE);
      g_string_free(field, TRUE);
      return JSValueMakeString(ctx, JS_STR(m->version));
    }
  }

  g_string_free(mod, TRUE);
  g_string_free(field, TRUE);
  return JS_NULL;
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
  
  int func_count = 12;
  char * names[] = { "log", "existsInMod", "launchMod", "registerDownload",
		     "startDownload", "cancelDownload", "downloadStatus",
		     "downloadError", "bytesCompleted", "bytesTotal",
		     "extractDownload", "metadata"};
  JSObjectCallAsFunctionCallback callbacks[] = { js_log, js_exists_in_mod, js_launch_mod,
						 js_register_download, js_start_download,
						 js_cancel_download, js_download_status,
						 js_download_error, js_bytes_completed,
						 js_bytes_total, js_extract_download, js_metadata };
  
  js_ctx = (JSGlobalContextRef)context;

  external_obj = JSObjectMake(js_ctx, NULL, NULL);

  window_obj = (JSObjectRef)window_object;
  JSObjectSetProperty(js_ctx, window_obj, JS_STR("external"),
		      external_obj, 0, NULL);

  js_add_functions(js_ctx, external_obj, names, callbacks, func_count);
}
