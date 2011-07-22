/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#import <Cocoa/Cocoa.h>

@interface Controller : NSObject
{
	NSString *monoPath;
	NSString *gamePath;

	IBOutlet NSWindow *window;
}
- (void)launch;
- (BOOL)initMono;
- (BOOL)shouldHideMenubar;
@end
