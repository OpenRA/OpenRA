/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "Controller.h"
#import "Mod.h"
#import "SidebarEntry.h"
#import "GameInstall.h"
#import "ImageAndTextCell.h"
#import "JSBridge.h"
#import "Download.h"

@implementation Controller
@synthesize allMods;
@synthesize webView;

- (void)awakeFromNib
{
	game = [[GameInstall alloc] initWithURL:[NSURL URLWithString:@"/Users/paul/src/OpenRA"]];
	[[JSBridge sharedInstance] setController:self];
	downloads = [[NSMutableDictionary alloc] init];
	
	NSTableColumn *col = [outlineView tableColumnWithIdentifier:@"mods"];
	ImageAndTextCell *imageAndTextCell = [[[ImageAndTextCell alloc] init] autorelease];
	[col setDataCell:imageAndTextCell];
	
	sidebarItems = [[SidebarEntry headerWithTitle:@""] retain];
	[self populateModInfo];
	id modsRoot = [self sidebarModsTree];
	[sidebarItems addChild:modsRoot];
	id otherRoot = [self sidebarOtherTree];
	[sidebarItems addChild:otherRoot];
	
	
	[outlineView reloadData];
	[outlineView expandItem:modsRoot expandChildren:YES];
	
	if ([[modsRoot children] count] > 0)
	{
		id firstMod = [[modsRoot children] objectAtIndex:0];
		int row = [outlineView rowForItem:firstMod];
		[outlineView selectRowIndexes:[NSIndexSet indexSetWithIndex:row] byExtendingSelection:NO];
		[[webView mainFrame] loadRequest:[NSURLRequest requestWithURL: [firstMod url]]];
	}
	
	[outlineView expandItem:otherRoot expandChildren:YES];
}

- (void)dealloc
{
	[sidebarItems release]; sidebarItems = nil;
	[downloads release]; downloads = nil;
	[super dealloc];
}

- (void)populateModInfo
{
	// Get info for all installed mods
	[allMods autorelease];
	allMods = [[game infoForMods:[game installedMods]] retain];	
}

- (SidebarEntry *)sidebarModsTree
{
	SidebarEntry *rootItem = [SidebarEntry headerWithTitle:@"MODS"];
	for (id key in allMods)
	{	
		id aMod = [allMods objectForKey:key];
		if ([aMod standalone])
		{
			id child = [SidebarEntry entryWithMod:aMod allMods:allMods baseURL:[[game gameURL] URLByAppendingPathComponent:@"mods"]];
			[rootItem addChild:child];
		}
	}
	
	return rootItem;
}

- (SidebarEntry *)sidebarOtherTree
{
	SidebarEntry *rootItem = [SidebarEntry headerWithTitle:@"OTHER"];
	[rootItem addChild:[SidebarEntry entryWithTitle:@"Support" url:nil icon:nil]];
	[rootItem addChild:[SidebarEntry entryWithTitle:@"Credits" url:nil icon:nil]];
	
	return rootItem;
}

- (void)launchMod:(NSString *)mod
{
	[game launchMod:mod];
}

- (BOOL)downloadUrl:(NSString *)url toFile:(NSString *)path withId:(NSString *)key
{
	if ([downloads objectForKey:key] != nil)
	{
		NSLog(@"Download already in progress for %@",key);
		return NO;
	}
	
	Download *download = [Download downloadWithURL:url filename:path key:key game:game];
	[downloads setObject:download forKey:key];
	return YES;
}

- (void)cancelDownload:(NSString *)key
{
	[[downloads objectForKey:key] cancel];
	[downloads removeObjectForKey:key];
}

- (Download *)downloadWithKey:(NSString *)key
{
	return [downloads objectForKey:key];
}

#pragma mark Sidebar Datasource and Delegate
- (int)outlineView:(NSOutlineView *)anOutlineView numberOfChildrenOfItem:(id)item
{
	// Can be called before awakeFromNib; return nothing
	if (sidebarItems == nil)
		return 0;
	
	// Root item
	if (item == nil)
		return [[sidebarItems children] count];

	return [[item children] count];
}

- (BOOL)outlineView:(NSOutlineView *)outlineView isItemExpandable:(id)item
{
	return (item == nil) ? YES : [[item children] count] != 0;
}

- (id)outlineView:(NSOutlineView *)outlineView
			child:(int)index
		   ofItem:(id)item
{
	if (item == nil)
		return [[sidebarItems children] objectAtIndex:index];
	
	return [[item children] objectAtIndex:index];
}

-(BOOL)outlineView:(NSOutlineView*)outlineView isGroupItem:(id)item
{	
	if (item == nil)
		return NO;
	
	return [item isHeader];
}

- (id)outlineView:(NSOutlineView *)outlineView
objectValueForTableColumn:(NSTableColumn *)tableColumn
		   byItem:(id)item
{
	return [item title];
}

- (BOOL)outlineView:(NSOutlineView *)outlineView shouldSelectItem:(id)item;
{	
	// don't allow headers to be selected
	if ([item isHeader] || [item url] == nil)
		return NO;
	
	[[webView mainFrame] loadRequest:[NSURLRequest requestWithURL:[item url]]];

	return YES;
}

- (void)outlineView:(NSOutlineView *)olv willDisplayCell:(NSCell*)cell forTableColumn:(NSTableColumn *)tableColumn item:(id)item
{	 
	if ([[tableColumn identifier] isEqualToString:@"mods"])
	{
		if ([cell isKindOfClass:[ImageAndTextCell class]])
		{
			[(ImageAndTextCell*)cell setImage:[item icon]];
		}
	}
}

#pragma mark WebView delegates
- (void)webView:(WebView *)sender didClearWindowObject:(WebScriptObject *)windowObject forFrame:(WebFrame *)frame
{
	[windowObject setValue:[JSBridge sharedInstance] forKey:@"external"];
}

- (void)webView:(WebView *)webView addMessageToConsole:(NSDictionary *)dictionary
{
	NSLog(@"%@",dictionary);
}
@end
