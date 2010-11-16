/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "JSBridge.h"
#import "Controller.h"

@implementation JSBridge

-(id)initWithController:(Controller *)aController
{
	self = [super init];
	if (self != nil)
	{
		controller = [aController retain];
	}
	return self;
}

- (void) dealloc
{
	[controller release]; controller = nil;
	[super dealloc];
}

- (void)launchCurrentMod
{
	NSLog(@"launchcurrent");
	[controller launchGame];
}

+ (BOOL)isSelectorExcludedFromWebScript:(SEL)aSelector { return NO; }
@end
