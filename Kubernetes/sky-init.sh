#!/usr/bin/env bash

if [ ! -f .env ]; then
   cp ../SkyBooker/env-template.txt .env
   # Remove blank lines and lines starting with "#" from .env
   if [[ "$OSTYPE" == "darwin"* ]]; then
      sed -i '' '/^\s*$/d;/^\s*#/d' .env
   else
      sed -i '/^\s*$/d;/^\s*#/d' .env
   fi
fi

# Load .env into environment variables
set -a
source .env
set +a

# Render values.yaml from template
envsubst < values-template.yaml > values.yaml

# Install or upgrade Helm release
#microk8s kubectl apply -f templates/namespace.yaml
echo microk8s helm3 upgrade --install skybooker . --create-namespace -n skybooker
#microk8s helm3 upgrade skybooker . --create-namespace -n skybooker
echo microk8s helm3 install skybooker . --create-namespace -n skybooker -f values.yaml

echo "run: kubectl get all -n skybooker"
