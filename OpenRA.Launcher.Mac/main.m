//
//  main.m
//  OpenRA
//
//  Created by Paul Chote on 15/11/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Cocoa/Cocoa.h>

extern char **environ;
int main(int argc, char *argv[])
{
	/* When launching a mod, the arguments are of the form 
	 * --launch <game dir> <support dir option> <mod option> */
	if (argc >= 5 && strcmp(argv[1], "--launch") == 0)
	{
		/* Change into the game dir */
		chdir(argv[2]);

		/* Command line args for mono */
		char *args[] = {
			argv[3],
			"--debug",
			"OpenRA.Game.exe",
			argv[4],
			argv[5],
			NULL
		};
		
		/* add game dir to DYLD_LIBRARY_PATH */
		char *old = getenv("DYLD_LIBRARY_PATH");
		if (old == NULL)
			setenv("DYLD_LIBRARY_PATH", argv[2], 1);
		else
		{
			char buf[512];
			int len = strlen(argv[2]) + strlen(old) + 2;
			if (len > 512)
			{
				NSLog(@"Insufficient DYLD_LIBRARY_PATH buffer length. Wanted %d, had 512", len);
				exit(1);
			}
			sprintf(buf,"%s:%s",argv[2], old);
			setenv("DYLD_LIBRARY_PATH", buf, 1);
		}

		/* Exec mono */
		execve(args[0], args, environ);
	}
	
	/* Else, start the launcher */
	return NSApplicationMain(argc,  (const char **) argv);
}
