#!/usr/bin/python
"""A simple GTK3 graphical dialog helper that can be used as a fallback if zenity is not available
   Compatible with python 2 or 3 with the gi bindings.

   Three modes are available:
       test: accepts no arguments, returns 0 if the python dependencies are available, or 1 if not
       error: show a gtk-3 error dialog
              accepts arguments --title and --text
       question: show a gtk-3 question dialog
              accepts arguments --title and --text
              returns 0 on Yes, or 1 on No
"""

import sys

try:
    import argparse
    import gi
    gi.require_version('Gtk', '3.0')
    from gi.repository import Gtk
except ImportError:
    sys.exit(1)

class Error():
    def __init__(self, title, text):
        dialog = Gtk.MessageDialog(None, 0, Gtk.MessageType.ERROR, Gtk.ButtonsType.OK, title)
        dialog.format_secondary_text(text)
        dialog.run()
        dialog.destroy()

class Question():
    def __init__(self, title, text):
        dialog = Gtk.MessageDialog(None, 0, Gtk.MessageType.QUESTION, Gtk.ButtonsType.YES_NO, title)
        dialog.format_secondary_text(text)
        response = dialog.run()
        dialog.destroy()
        sys.exit(0 if response == Gtk.ResponseType.YES else 1)

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('type', choices=['error', 'question', 'test'])
    parser.add_argument('--title', type=str, required=False, default='')
    parser.add_argument('--text', type=str, required=False, default='')
    args = parser.parse_args()
    if args.type == 'question':
        Question(args.title, args.text.replace('\\n', '\n'))
    elif args.type == 'error':
        Error(args.title, args.text.replace('\\n', '\n'))
