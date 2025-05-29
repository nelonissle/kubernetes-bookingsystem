#!/usr/bin/env bash
# Load .env into environment variables
set -a
source .env
set +a

# Render values.yaml from template
envsubst < values-template.yaml > values.yaml

# Install or upgrade Helm release
microk8s helm3 upgrade skybooker . --create-namespace -n skybooker -f values.yaml

microk8s kubectl get all -n skybooker

microk8s kubectl logs deployment.apps/bookingservice -n skybooker

#kubectl logs -f -l app=bookingservice -n skybooker --all-containers=true
