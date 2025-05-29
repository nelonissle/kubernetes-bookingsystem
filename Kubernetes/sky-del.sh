#!/usr/bin/env bash

microk8s helm3 uninstall skybooker -n skybooker

microk8s kubectl delete namespace skybooker --wait
microk8s kubectl get namespace skybooker

echo "delete images"
sudo microk8s ctr images ls | awk {'print $1'} > image_ls
cat image_ls | while read line || [[ -n $line ]];
do
	microk8s ctr images rm $line;
done;
