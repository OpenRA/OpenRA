/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "Controller.h"
#import "ModEntry.h"
#import "GameInstall.h"
#import "ImageAndTextCell.h"

@implementation Controller

- (void) awakeFromNib
{
	sidebarItems = [[ModEntry headerWithTitle:@""] retain];
	[sidebarItems addChild:[self modTree]];

	NSTableColumn *col = [outlineView tableColumnWithIdentifier:@"mods"];
	ImageAndTextCell *imageAndTextCell = [[[ImageAndTextCell alloc] init] autorelease];
	[col setDataCell:imageAndTextCell];
	
	[outlineView reloadData];
	[outlineView expandItem:[outlineView itemAtRow:1] expandChildren:YES];
	[outlineView selectRowIndexes:[NSIndexSet indexSetWithIndex:1] byExtendingSelection:NO];
	
	game = [[GameInstall alloc] initWithPath:@"/Users/paul/src/OpenRA"];
}

- (void) dealloc
{
	[sidebarItems release]; sidebarItems = nil;
	[super dealloc];
}

- (ModEntry *)modTree
{
	// Create root item
	ModEntry *rootItem = [ModEntry headerWithTitle:@"MODS"];
	
	NSString* imageName = [[NSBundle mainBundle] pathForResource:@"OpenRA" ofType:@"icns"];
	NSImage* imageObj = [[NSImage alloc] initWithContentsOfFile:imageName];
	
	NSDictionary *foo = [NSDictionary dictionaryWithObjectsAndKeys:
						 @"Test mod", @"Title",
						 @"Foobar", @"Author",
						 imageObj, @"Icon",
						 nil];
	[imageObj release];
	[rootItem addChild:[ModEntry modWithFields:foo]];
	return rootItem;
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
	return ![item isHeader];
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

- (IBAction)launchGame:(id)sender
{
	[game launchGame];
}
@end
