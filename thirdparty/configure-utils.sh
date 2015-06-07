#!/bin/sh

####
# This file must stay /bin/sh and POSIX compliant for BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

# We configure the default download agent.
if hash curl >/dev/null 2>&1; then
    curl()
    {
        command curl --silent --location --output "$1" "$2"
    }
elif hash fetch >/dev/null 2>&1; then
    curl()
    {
        command fetch --quiet --output "$1" "$2"
    }
else
    curl()
    {
        echo "Can't download: didn't find curl(1), nor fetch(1)"
    }
fi
