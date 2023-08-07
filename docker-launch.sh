#!/bin/sh
docker run --rm --interactive --tty \
    --volume $(pwd):/openra \
    --volume $(pwd)/in-container-config:/root/.config/openra \
    --volume "${HOME}/.Xauthority:/root/.Xauthority" \
    --env DISPLAY=unix${DISPLAY} \
    --device /dev/dri \
    --device /dev/snd \
    --net host \
    openra:latest $@
