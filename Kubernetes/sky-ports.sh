#!/usr/bin/env bash

microk8s kubectl get pods -n skybooker

# put the cluster ip port 8080 of ocelot to local 8081
microk8s kubectl port-forward service/ocelotapigateway 8081:8080 -n skybooker
#microk8s kubectl port-forward service/grafana 3000:3000 -n skybooker
#nohup kubectl port-forward service/my-service 8080:80 > port-forward.log 2>&1 &
