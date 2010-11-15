/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "ModEntry.h"


@implementation ModEntry
@synthesize isHeader;
@synthesize title;
@synthesize version;
@synthesize author;
@synthesize description;
@synthesize requires;
@synthesize standalone;
@synthesize children;
@synthesize icon;

+ (id)headerWithTitle:(NSString *)aTitle
{
	id newObject = [[self alloc] initWithFields:[NSDictionary dictionaryWithObject:aTitle forKey:@"Title"] isHeader:YES];
	[newObject autorelease];
	return newObject;
}

+ (id)modWithFields:(id)fields
{
	id newObject = [[self alloc] initWithFields:fields isHeader:NO];
	[newObject autorelease];
	return newObject;
}

- (id)initWithFields:(NSDictionary *)fields isHeader:(BOOL)header
{
	self = [super init];
	if (self)
	{
		isHeader = header;
		title = [fields objectForKey:@"Title"];
		version = [fields objectForKey:@"Version"];
		author = [fields objectForKey:@"Author"];
		description = [fields objectForKey:@"Description"];
		requires = [fields objectForKey:@"Requires"];
		standalone = ([fields objectForKey:@"Standalone"] == @"True");
		icon = [[fields objectForKey:@"Icon"] retain];
		children = [[NSMutableArray alloc] init];
	}
	return self;
}

- (void)addChild:(ModEntry *)child
{
	[children addObject:child];
}

- (void) dealloc
{
	[icon release]; icon = nil;
	[super dealloc];
}

@end
