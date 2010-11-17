/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "SidebarEntry.h"
#import "Mod.h"

@implementation SidebarEntry
@synthesize isHeader;
@synthesize title;
@synthesize children;
@synthesize icon;

+ (id)headerWithTitle:(NSString *)aTitle;
{
	id newObject = [[self alloc] initWithTitle:aTitle url:nil icon:nil isHeader:YES];
	[newObject autorelease];
	return newObject;
}

+ (id)entryWithTitle:(NSString *)aTitle url:(NSURL *)aURL icon:(id)anIcon
{
	id newObject = [[self alloc] initWithTitle:aTitle url:aURL icon:anIcon isHeader:NO];
	[newObject autorelease];
	return newObject;
}

+ (id)entryWithMod:(Mod *)baseMod allMods:(NSDictionary *)allMods baseURL:(NSURL *)baseURL
{
	// TODO: Get the mod icon from the Mod
	// Temporary hack until mods define an icon
	NSString* imageName = [[NSBundle mainBundle] pathForResource:@"OpenRA" ofType:@"icns"];
	id icon = [[[NSImage alloc] initWithContentsOfFile:imageName] autorelease];
	id url = [[baseURL URLByAppendingPathComponent:[baseMod mod]]
					   URLByAppendingPathComponent:@"mod.html"];
	
	id ret = [SidebarEntry entryWithTitle:[baseMod title] url:url icon:icon];
	
	for (id key in allMods)
	{
		id aMod = [allMods objectForKey:key];
		if (![[aMod requires] isEqualToString:[baseMod mod]])
			continue;
		
		id child = [SidebarEntry entryWithMod:aMod allMods:allMods baseURL:baseURL];
		[ret addChild:child];
	}
	return ret;
}

- (id)initWithTitle:(NSString *)aTitle url:(NSURL *)aURL icon:(id)anIcon isHeader:(BOOL)isaHeader
{
	self = [super init];
	if (self)
	{
		isHeader = isaHeader;
		title = [aTitle retain];
		url = [aURL retain];
		icon = [anIcon retain];
		children = [[NSMutableArray alloc] init];
	}
	return self;
}

- (void)addChild:(Mod *)child
{
	[children addObject:child];
}

- (NSURL *)url
{
	return url;
}

- (void) dealloc
{
	[title release]; title = nil;
	[url release]; url = nil;
	[icon release]; icon = nil;
	[super dealloc];
}
@end
