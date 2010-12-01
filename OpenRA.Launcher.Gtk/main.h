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

mod_t * get_mod(char const * key);

#endif
