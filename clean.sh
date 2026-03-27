#!/bin/bash

# Remove compiled sources from bin/ob
find . -type d -name "bin" -exec rm  -rf {} 2> /dev/null \;
find . -type d -name "obj" -exec rm  -rf {} 2> /dev/null \;

# Remove Rider cache
rm -rf .idea
