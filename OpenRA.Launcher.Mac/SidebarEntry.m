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
	id newObject = [[self alloc] initWithTitle:aTitle object:nil icon:nil isHeader:YES];
	[newObject autorelease];
	return newObject;
}

+ (id)entryWithTitle:(NSString *)aTitle object:(id)anObject icon:(id)anIcon
{
	id newObject = [[self alloc] initWithTitle:aTitle object:anObject icon:anIcon isHeader:NO];
	[newObject autorelease];
	return newObject;
}

+ (id)entryWithMod:(Mod *)baseMod allMods:(NSArray *)allMods
{
	// TODO: Get the mod icon from the Mod
	// Temporary hack until mods define an icon
	NSString* imageName = [[NSBundle mainBundle] pathForResource:@"OpenRA" ofType:@"icns"];
	id icon = [[[NSImage alloc] initWithContentsOfFile:imageName] autorelease];
	id ret = [SidebarEntry entryWithTitle:[baseMod title] object:baseMod icon:icon];
	
	for (id aMod in allMods)
	{
		if (![[aMod requires] isEqualToString:[baseMod mod]])
			continue;
		
		id child = [SidebarEntry entryWithMod:aMod allMods:allMods];
		[ret addChild:child];
	}
	return ret;
}

- (id)initWithTitle:(NSString *)aTitle object:(id)anObject icon:(id)anIcon isHeader:(BOOL)isaHeader
{
	self = [super init];
	if (self)
	{
		isHeader = isaHeader;
		title = [aTitle retain];
		object = [anObject retain];
		icon = [anIcon retain];
		children = [[NSMutableArray alloc] init];
	}
	return self;
}

- (void)addChild:(Mod *)child
{
	NSLog(@"Adding sidebar child %@ to %@",[child title], title);
	[children addObject:child];
}

- (BOOL)shouldSelect
{	
	return [object shouldSelect];
}

- (NSURL *)url
{
	if (object == nil)
	{	
		NSLog(@"object is nil");
		return nil;
	}
	
	return [object pageURL];
}

- (void) dealloc
{
	[title release]; title = nil;
	[object release]; object = nil;	
	[icon release]; icon = nil;
	[super dealloc];
}
@end
