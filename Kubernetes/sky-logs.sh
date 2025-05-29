#!/usr/bin/env bash

microk8s kubectl get services -n skybooker

# read the pod name form command line to be used in next kubectl exec command
read -p "Enter the service name: " ser_name

microk8s kubectl logs service/$ser_name -n skybooker
