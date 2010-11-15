//
//  GameInstall.h
//  OpenRA
//
//  Created by Paul Chote on 15/11/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Cocoa/Cocoa.h>


@interface GameInstall : NSObject {
	NSString *gamePath;
}

-(id)initWithPath:(NSString *)path;
-(void)launchGame;
@end
