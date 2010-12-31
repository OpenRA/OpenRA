/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

typedef struct callback_data
{
	gint output_fd;
	gpointer user_data;
} callback_data;

gboolean util_get_mod_list (GChildWatchFunc);
gboolean util_get_mod_metadata(gchar const *, GChildWatchFunc);
gboolean util_get_setting(gchar const *, GChildWatchFunc);
gint util_do_download(gchar const *, gchar const *, GPid *);
gint util_do_extract(gchar const *, gchar const *, GPid *);
gboolean util_do_http_request(gchar const *, GChildWatchFunc, gpointer);
GString * util_get_output(int);
