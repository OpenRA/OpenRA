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

#import "Settings.h"


@implementation Settings

- (id)init
{
	self = [super init];
	if (self != nil)
	{
		settings = [[NSMutableDictionary alloc] init];
		filePath = nil;
	}
	return self;
}

- (NSString *)valueForSetting:(NSString *)key
{
	return [settings valueForKey:key];
}

- (void)setValue:(NSString *)value forSetting:(NSString *)key
{
	[settings setValue:value forKey:key];
}

- (void)loadSettingsFile:(NSURL *)file
{
	[filePath autorelease];
	filePath = file;
	[filePath retain];
		
	NSError *error;
	NSString *data = [NSString stringWithContentsOfURL:filePath encoding:NSUTF8StringEncoding error:&error];
	NSArray *lines = [data componentsSeparatedByString:@"\n"];
	
	for (id line in lines)
	{
		NSArray *cmp = [line componentsSeparatedByString:@"="];
		if ([cmp count] != 2)
			continue;
		
		NSString *key = [[cmp objectAtIndex:0] stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
		NSString *value = [[cmp objectAtIndex:1] stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
			
		[settings setObject:value forKey:key];
	}
	NSLog(@"Loaded settings: %@",settings);
}

- (void)save
{
	[self saveToFile:filePath];
}

- (void)saveToFile:(NSURL *)file
{
	NSMutableString *data = [NSMutableString stringWithString:@"[Settings]\n"];
	for (id key in settings)
	{
		[data appendFormat:@"%@=%@\n",key, [settings valueForKey:key]];
	}
	NSError *error;
	[data writeToURL:file atomically:YES encoding:NSUTF8StringEncoding error:&error];
}

- (void) dealloc
{
	[settings release]; settings = nil;
	[super dealloc];
}


@end
