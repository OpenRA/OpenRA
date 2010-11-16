/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */


#import <Cocoa/Cocoa.h>

@class Controller;
@interface JSBridge : NSObject {
	Controller *controller;
}

-(id)initWithController:(Controller *)aController;
@end
