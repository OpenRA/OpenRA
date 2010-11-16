/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "ModEntry.h"


@implementation ModEntry
@synthesize mod;
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
	id newObject = [[self alloc] initWithId:@"title" fields:[NSDictionary dictionaryWithObject:aTitle forKey:@"Title"] isHeader:YES];
	[newObject autorelease];
	return newObject;
}

+ (id)errorWithTitle:(NSString *)aTitle
{
	id newObject = [[self alloc] initWithId:@"error" fields:[NSDictionary dictionaryWithObject:aTitle forKey:@"Title"] isHeader:NO];
	[newObject autorelease];
	return newObject;
}

+ (id)modWithId:(NSString *)mod fields:(id)fields
{
	id newObject = [[self alloc] initWithId:mod fields:fields isHeader:NO];
	[newObject autorelease];
	return newObject;
}

- (id)initWithId:(NSString *)anId fields:(NSDictionary *)fields isHeader:(BOOL)header
{
	self = [super init];
	if (self)
	{
		mod = anId;
		isHeader = header;
		title = [[fields objectForKey:@"Title"] retain];
		version = [[fields objectForKey:@"Version"] retain];
		author = [[fields objectForKey:@"Author"] retain];
		description = [[fields objectForKey:@"Description"] retain];
		requires = [[fields objectForKey:@"Requires"] retain];
		standalone = ([[fields objectForKey:@"Standalone"] isEqualToString:@"True"]);
		
		if (!isHeader)
		{
			NSString* imageName = [[NSBundle mainBundle] pathForResource:@"OpenRA" ofType:@"icns"];
			icon = [[NSImage alloc] initWithContentsOfFile:imageName];
		}
		children = [[NSMutableArray alloc] init];
	}
	return self;
}

- (void)addChild:(ModEntry *)child
{
	NSLog(@"Adding child %@ to %@",[child mod], mod);
	[children addObject:child];
}

- (void)buildChildTree:(NSArray *)allMods
{
	for (id aMod in allMods)
	{
		if (![[aMod requires] isEqualToString:mod])
			continue;
		
		[self addChild:aMod];
		[aMod buildChildTree:allMods];
	}
}

- (void) dealloc
{
	[title release]; title = nil;
	[version release]; version = nil;
	[author release]; author = nil;
	[description release]; description = nil;	
	[requires release]; requires = nil;	
	[icon release]; icon = nil;
	[super dealloc];
}

@end
