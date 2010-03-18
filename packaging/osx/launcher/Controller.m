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

#import "Controller.h"


@implementation Controller

-(IBAction)launchApp:(id)sender
{
	[[NSWorkspace sharedWorkspace] launchApplication:[[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"OpenRA.app"]];
	[NSApp terminate: nil];
}

-(IBAction)showDownloadSheet:(id)sender
{
	[NSApp beginSheet:downloadSheet modalForWindow:mainWindow
		modalDelegate:self didEndSelector:NULL contextInfo:nil];
}

- (IBAction)dismissDownloadSheet:(id)sender
{
	[NSApp endSheet:downloadSheet];
	[downloadSheet orderOut:self];
	[downloadSheet performClose:self];
}

- (IBAction)startDownload:(id)sender
{
	// Change the sheet items
	[downloadBar setHidden:NO];
	[abortButton setHidden:NO];
	[statusText setHidden:NO];
	[infoText setHidden:YES];
	[downloadButton setHidden:YES];
	[cancelButton setHidden:YES];
	
	// Create a request
	NSURL *remoteURL = [NSURL URLWithString:@"http://open-ra.org/packages/ra-packages.zip"];
	localDownloadPath = [NSTemporaryDirectory() stringByAppendingPathComponent:@"ra-packages.zip"];
	packageDirectory = [[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"mods/ra/packages/"];
	
	NSLog(@"Downloading to %@",localDownloadPath);
    NSURLRequest *theRequest=[NSURLRequest requestWithURL:remoteURL
                                              cachePolicy:NSURLRequestUseProtocolCachePolicy
                                          timeoutInterval:60.0];
	
    // Create a download object
	currentDownload = [[NSURLDownload alloc] initWithRequest:theRequest delegate:self];
	
    if (currentDownload)
	{
        downloading = YES;
		[currentDownload setDestination:localDownloadPath allowOverwrite:YES];
		[statusText setStringValue:@"Connecting..."];
    }
	else
		[statusText setStringValue:@"Cannot connect to server"];
}

- (IBAction)stopDownload:(id)sender
{
	// Stop the download
	if (downloading)
		[currentDownload cancel]; 
	
	// Update the sheet status
	[downloadBar setHidden:YES];
	[abortButton setHidden:YES];
	[statusText setHidden:YES];
	[infoText setHidden:NO];
	[downloadButton setHidden:NO];
	[cancelButton setHidden:NO];
}

#pragma mark === Download Delegate Methods ===

- (void)download:(NSURLDownload *)download didFailWithError:(NSError *)error
{
	[download release]; downloading = NO;
	[statusText setStringValue:@"Error downloading file"];
}

- (void)downloadDidFinish:(NSURLDownload *)download
{
    [download release]; downloading = NO;
	[self extractPackages];
}

- (void)extractPackages
{
	[abortButton setEnabled:NO];
	[downloadBar setDoubleValue:0];
	[downloadBar setMaxValue:1];
	[downloadBar setIndeterminate:YES];
	[statusText setStringValue:@"Extracting..."];
	
	// TODO: Extract and copy files
}

- (void)download:(NSURLDownload *)download didReceiveResponse:(NSURLResponse *)response
{
	expectedData = [response expectedContentLength];
    if (expectedData > 0.0)
	{
		downloadedData = 0;
		[downloadBar setIndeterminate:NO];
		[downloadBar setMaxValue:expectedData];
        [downloadBar setDoubleValue:downloadedData];
    }
}

- (void)download:(NSURLDownload *)download didReceiveDataOfLength:(NSUInteger)length
{
    downloadedData += length;
    if (downloadedData >= expectedData)
	{
		[downloadBar setIndeterminate:YES];
		[statusText setStringValue:@"Downloading..."];
    }
	else
	{
		[downloadBar setDoubleValue:downloadedData];
		[statusText setStringValue:[NSString stringWithFormat:@"Downloading %.1f of %f",downloadedData,expectedData]];
	}
}

@end
