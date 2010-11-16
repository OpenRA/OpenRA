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

@implementation Controller

- (void) awakeFromNib
{
	game = [[GameInstall alloc] initWithURL:[NSURL URLWithString:@"/Users/paul/src/OpenRA"]];
	sidebarItems = [[SidebarEntry headerWithTitle:@""] retain];
	[sidebarItems addChild:[self sidebarModsTree]];
	[sidebarItems addChild:[self sidebarOtherTree]];
	NSTableColumn *col = [outlineView tableColumnWithIdentifier:@"mods"];
	ImageAndTextCell *imageAndTextCell = [[[ImageAndTextCell alloc] init] autorelease];
	[col setDataCell:imageAndTextCell];
	
	[outlineView reloadData];
	[outlineView expandItem:[outlineView itemAtRow:0] expandChildren:YES];
	[outlineView selectRowIndexes:[NSIndexSet indexSetWithIndex:1] byExtendingSelection:NO];
	
	jsbridge = [[JSBridge alloc] initWithController:self];
    [[webView windowScriptObject] setValue:jsbridge forKey:@"Launcher"];
}

- (SidebarEntry *)sidebarModsTree
{
	// Get info for all installed mods
	id modnames = [game installedMods];
	NSArray *allMods = [game infoForMods:modnames];
	
	SidebarEntry *rootItem = [SidebarEntry headerWithTitle:@"MODS"];
	for (id aMod in allMods)
	{	
		if ([aMod standalone])
		{	
			id child = [SidebarEntry entryWithMod:aMod allMods:allMods];
			[rootItem addChild:child];
		}
	}
	
	return rootItem;
}

- (SidebarEntry *)sidebarOtherTree
{
	SidebarEntry *rootItem = [SidebarEntry headerWithTitle:@"OTHER"];
	[rootItem addChild:[SidebarEntry entryWithTitle:@"Support" object:nil icon:nil]];
	[rootItem addChild:[SidebarEntry entryWithTitle:@"Credits" object:nil icon:nil]];
	
	return rootItem;
}


- (void) dealloc
{
	[sidebarItems release]; sidebarItems = nil;
	[super dealloc];
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

- (void)launchGame
{
	[game launchGame];
}
@end
