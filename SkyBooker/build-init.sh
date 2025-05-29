#!/usr/bin/env bash

if [ ! -f .env ]; then
   cp env-template.txt .env
   # Remove blank lines and lines starting with "#" from .env
   if [[ "$OSTYPE" == "darwin"* ]]; then
      sed -i '' '/^\s*$/d;/^\s*#/d' .env
   else
      sed -i '/^\s*$/d;/^\s*#/d' .env
   fi
fi

# set the environment variables reading .env file
set -o allexport
source .env
set +o allexport

# for zsh setopt allexport or noallexport
echo "for zsh execute these commands:"
echo "setopt allexport; source .env; setopt noallexport"

echo "run build now"