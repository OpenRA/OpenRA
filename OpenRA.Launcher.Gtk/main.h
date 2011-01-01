/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#ifndef OPENRA_MAIN_H
#define OPENRA_MAIN_H

enum
{
  ICON_COLUMN,
  KEY_COLUMN,
  NAME_COLUMN,
  N_COLUMNS
};

enum
{
  RENDERER_GL,
  RENDERER_CG
};

#define MAX_NUM_MODS 64

typedef struct mod_t
{
  gchar * key;
  gchar * title;
  gchar * version;
  gchar * author;
  gchar * description;
  gchar * requires;
  int standalone;
} mod_t;

mod_t * get_mod(gchar const * key);

int get_renderer(void);

#endif
