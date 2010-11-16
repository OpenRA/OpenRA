/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import <Cocoa/Cocoa.h>
#import <WebKit/WebKit.h>
@class Mod;
@class SidebarEntry;
@class GameInstall;
@class JSBridge;
@interface Controller : NSObject
{
	SidebarEntry *sidebarItems;
	GameInstall *game;
	IBOutlet NSOutlineView *outlineView;
	IBOutlet WebView *webView;
}
- (void)launchGame;
- (SidebarEntry *)sidebarModsTree;
- (SidebarEntry *)sidebarOtherTree;

@end
