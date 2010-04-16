/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */

#import <Cocoa/Cocoa.h>

@class Settings;
@interface Controller : NSObject {

	// Main Window
	IBOutlet NSWindow *mainWindow;
	Settings *settings;
	IBOutlet id modsList;
	NSArray *mods;
	
	// Package Downloader
	NSString *localDownloadPath;
	NSString *packageDirectory;
	NSURLDownload *currentDownload;
    long long expectedData;
    long long downloadedData;
	BOOL downloading;
	
	// Download Sheet
	IBOutlet NSWindow *downloadSheet;
	IBOutlet id infoText;
	IBOutlet id downloadButton;
	IBOutlet id cancelButton;

	IBOutlet id downloadBar;
	IBOutlet id statusText;
	IBOutlet id abortButton;
}

-(IBAction)showDownloadSheet:(id)sender;
- (IBAction)dismissDownloadSheet:(id)sender;


-(IBAction)launchApp:(id)sender;
- (IBAction)startDownload:(id)sender;
- (IBAction)stopDownload:(id)sender;
- (void)extractPackages;

@end
