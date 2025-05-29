# Kubernetes

**Kubernetes (short: K8s)** is a **tool** that helps to **automatically start, manage, and scale many applications in containers** – across multiple servers.

## What does Kubernetes do?
- Starts and monitors **containers** (e.g., Docker)
- Ensures they **always run**
- **Automatically distributes** them across servers
- **Scales them up or down** depending on load
- Enables **updates without downtime**

## Why do you need it?
- If you have **many microservices** or web apps
- For **reliability and fault tolerance**
- For **automated deployment**
- For **cloud-native applications**

## Important Tools & Concepts

### kubectl

The command-line tool for Kubernetes.  
With it, you control everything in the cluster (e.g., start pods, view logs, roll out deployments).

Examples:

```bash
kubectl get pods
kubectl apply -f app.yaml
kubectl logs my-pod
```

### Helm

**A package manager for Kubernetes** (like apt or npm, but for clusters).  
With Helm, you can **install entire apps or services with a few commands, including configuration** – e.g., Prometheus + Grafana + database in one go.

- **Helm packages = Charts**
- **You define settings in a `values.yaml` file**

#### 🗂️ Helm Structure

```text
helm/
├── charts/
│   ├── authservice/
│   │   ├── Chart.yaml
│   │   ├── templates/
│   │   │   ├── configmap.yaml
│   │   │   ├── deployment.yaml
│   │   │   ├── service.yaml
│   ├── bookingservice/
│   ├── flightservice/
│   ├── messagingservice/
│   ├── ocelotgateway/
│   └── ...
├── templates/
│   ├── ingress.yaml
│   ├── namespace.yaml
│   ├── secrets.yaml
│   ├── configmap.yaml
├── Chart.yaml
├── values.yaml
```


### Ingress

Allows **external users to access your services via a URL** in the cluster.  
Ingress = **"gateway"** for web access (e.g., myservice.com/login → points to a specific service in the cluster).  
Ingress works via **Ingress Controllers** (e.g., NGINX, Traefik).

---

# Basics & Architecture

## Overview of Components

- **Pod**: Smallest deployable unit (consists of 1+ containers)
- **Node**: Physical or virtual machine in the cluster
- **Deployment**: Manages replication, updates, rollbacks
- **Service**: Internal or external accessibility
  - Types: `ClusterIP`, `NodePort`, `LoadBalancer`
- **ConfigMap / Secret**: External configuration data / sensitive data
- **Ingress / Ingress Controller**: Routing of HTTP/HTTPS requests
- **Namespaces**: Logical separation of resources
- **RBAC (Role-Based Access Control)**: Permission management for users & services

## Tool Knowledge

- **kubectl**: CLI for controlling Kubernetes clusters
- **Helm**: Package manager for Kubernetes (templates, values.yaml)
- **Docker**: Container concepts must be understood beforehand

## Network understanding

- **Cluster Networking (CNI)**: Communication between pods
- **DNS in Kubernetes**: Service names are automatically resolved
- **Service Discovery**: Automatic discovery of services in the cluster

---

# 🚀 Deployment Instructions

The first step is to define your namespace. In this example the namespace is called **"skybooker"**.

You need to install a kubernetes, in our example we use **microk8s**.

## ✅ Installation
For our setup I used a LTS Ubuntu server.

First install MicroK8s:
```bash
sudo snap install microk8s --classic
microk8s enable dns helm3 ingress storage
# Check if it is running. 
microk8s status --wait-ready
# Activate Helm:
microk8s enable helm3
# Set aliases, do that as well in your shell (.profile):
alias kubectl='microk8s kubectl'
alias helm='microk8s helm3'
```

Clone your git repository.

Deploy using Helm:
```bash
microk8s helm3 install skybooker .
# set namespace
kubectl apply -f templates/namespace.yaml
```

Check status:
```bash
microk8s kubectl get all -n skybooker
```

### 🚀 Install Helm charts
```bash
# install charts into skybooker:
helm install skybooker . -n skybooker
helm install skybooker . --create-namespace -n skybooker
```
If you change a chart in helm, you have to update it on your kubernetes:
```bash
# update cluster 
helm upgrade skybooker . --create-namespace -n skybooker
```

### Kubernetes environment
Depending on your network setup, you use local ip addresses or FQDN. Usually the local kubernetes IP address is set in the environment of all services within your namespace. This makes it very easy to access depending services within a pod.

Example:
MONGO_SERVICE_HOST='10.152.183.57'
MONGO_SERVICE_PORT='27017'


## Work with your Kubernetes cluster

```bash
# Status of cluster 
kubectl get all -n skybooker
```

```bash
# logs of one service 
kubectl logs deployment/authservice -n skybooker
```

```bash
# Access to one container 
./sky-shell.sh
```

## 🌐 Access to your services
You need to find out the right IP address of your services (of FQDM if you use DNS):
```
http://<SERVER-IP>:<NodePort>
```

Use the script `./sky-ports.sh` to see all available node ports.

## Delete Kubernetes Services
```bash
# delete services
./sky-del.sh 
kubectl logs deployment/authservice -n skybooker
```

## ✏️ Scripts

```bash
./sky-del.sh    # Delets the pods and Clears all Images
./sky-init.sh   # Initialze the pods / Loads Configs 
./sky-start.sh  # Pulls all the Config files and builds the Pods
./sky-ports.sh  # Forwarders the Ports of ocelot service so it will work propper 
./sky-logs.sh   # You can select wich logs of wich services you want to see
./sky-shell.sh  # You can select wich service you want to exec for the shell inside of it 
./sky-tests.sh  # Runs a CURL test for the services (only works when ports forwarded)
```

## 🔐 Secrets & Configurations
- Sensitive keys (e.g., Twilio Auth, JWT secrets) stored in `secrets.yaml`
- ConfigMaps used for environment-specific variables like DB URLs
- All configurations centralized in the main `values.yaml`

The secrets.yaml is generated from the .env file by the script `sky-init.sh`.

## 📈 Monitoring & Logging
- **Prometheus** scrapes metrics
- **Grafana** dashboards visualize data
- **Loki + Promtail** collect logs from pods
- **Elasticsearch + Logstash** provide full-text search and storage

Dashboards and logs are accessible via Ingress routes once deployed.

See [Monitoring README](../SkyBooker/Monitoring/README.md)

---

# Important Kubernetes & Ubuntu Commands

This document contains a curated list of 100+ essential commands for working with Ubuntu, MicroK8s, Kubernetes, Git, and the `vi` text editor. Each command includes a short description.

## 🔧 Ubuntu System Commands

```bash
1. top                        # Show running processes
2. htop                       # Interactive process viewer (install with `sudo apt install htop`)
3. ps aux                    # Detailed process list
4. netstat -a                # Show all network connections
5. netstat -tuln             # Show listening ports
6. ss -tuln                  # Modern netstat replacement
7. ip a                      # Show all network interfaces
8. ping <host>               # Ping a host
9. curl <url>                # Send request to URL
10. wget <url>               # Download file from URL
11. journalctl -xe           # View system logs
12. df -h                    # Show disk usage
13. du -sh *                 # Show size of folders
14. free -m                  # Show RAM usage
15. uname -a                 # Show system info
16. whoami                   # Show current user
17. uptime                   # Show system uptime
18. reboot                   # Reboot the system
19. sudo apt update          # Update package index
20. sudo apt upgrade         # Upgrade packages
```

## 🧱 MicroK8s Commands

```bash
21. microk8s status                    # Check MicroK8s status
22. microk8s enable <addon>           # Enable addons (e.g., dns, ingress)
23. microk8s disable <addon>          # Disable addon
24. microk8s kubectl get all          # Show all resources in default namespace
25. microk8s kubectl get all -n <ns>  # All resources in specific namespace
26. microk8s kubectl get pods         # List pods
27. microk8s kubectl get svc          # List services
28. microk8s kubectl describe pod <name> # Pod details
29. microk8s kubectl logs <pod>       # Show logs of pod
30. microk8s reset                    # Reset MicroK8s state
31. microk8s stop                     # Stop MicroK8s
32. microk8s start                    # Start MicroK8s
33. microk8s helm3 repo add <repo> <url>   # Add Helm repo
34. microk8s helm3 install <name> .       # Install Helm chart
35. microk8s kubectl create ns <ns>       # Create namespace
36. microk8s kubectl delete ns <ns>       # Delete namespace
37. microk8s kubectl exec -it <pod> -- bash # Shell into a pod
38. microk8s kubectl port-forward <pod> <local>:<remote> # Port forward
39. microk8s kubectl apply -f <file>      # Apply YAML manifest
40. microk8s kubectl delete -f <file>     # Delete YAML resources
```

## 🌀 General Kubernetes Commands

```bash
41. kubectl get nodes                     # Show all cluster nodes
42. kubectl get deployments               # Show deployments
43. kubectl rollout status deploy/<name> # Check deployment status
44. kubectl describe deployment <name>   # Deployment details
45. kubectl scale deploy <name> --replicas=3 # Scale deployment
46. kubectl get events                   # Show recent events
47. kubectl get ingress                  # Show ingress rules
48. kubectl get secrets                  # List secrets
49. kubectl get configmaps               # List configmaps
50. kubectl explain <resource>           # Get info on a resource type
```

## 📦 Helm Commands

```bash
51. helm repo add <name> <url>           # Add a Helm repo
52. helm repo update                     # Update repos
53. helm search repo <keyword>           # Search for charts
54. helm install <release> <chart>       # Install chart
55. helm upgrade <release> <chart>       # Upgrade release
56. helm uninstall <release>             # Remove release
57. helm list                            # List installed releases
58. helm get values <release>            # Show release values
59. helm template <chart>                # Render chart to YAML
60. helm show values <chart>             # Show default chart values
```

## 🧪 Debugging & Monitoring

```bash
61. kubectl top pod                      # Show pod CPU/memory usage
62. kubectl top node                     # Show node metrics
63. kubectl logs <pod> --follow          # Follow logs
64. kubectl describe <resource> <name>   # Resource details
65. kubectl get pod -o wide              # Show pod IPs and nodes
66. kubectl cp <pod>:<path> <local>      # Copy file from pod
67. watch kubectl get pods               # Live pod updates
68. kubectl get events --sort-by=.metadata.creationTimestamp # Sort events
69. kubectl get endpoints                # Show service endpoints
70. kubectl get pvc                      # Show persistent volume claims
```

## 🔒 RBAC & Security

```bash
71. kubectl create serviceaccount <name>         # Create service account
72. kubectl get serviceaccounts                  # List service accounts
73. kubectl create role <name> --verb=get,list --resource=pods # Create role
74. kubectl create rolebinding <name> --role=<role> --serviceaccount=<ns>:<sa> # Bind role
75. kubectl auth can-i <action> <resource>       # Check permissions
```

## 🧹 Cluster Cleanup & Utilities

```bash
76. kubectl delete pod <name>                    # Delete pod
77. kubectl delete svc <name>                    # Delete service
78. kubectl delete deploy <name>                 # Delete deployment
79. kubectl delete ingress <name>                # Delete ingress
80. kubectl delete all --all                     # Delete everything in namespace
81. kubectl drain <node> --ignore-daemonsets     # Prepare node for maintenance
82. kubectl cordon <node>                        # Mark node unschedulable
83. kubectl uncordon <node>                      # Re-enable scheduling
84. kubectl taint nodes <node> key=value:NoSchedule # Taint node
```

## 📁 YAML & Manifest Tips

```bash
85. kubectl apply -k <dir>            # Apply Kustomize manifests
86. kubectl kustomize <dir>           # Show merged YAML
87. helm lint                         # Check Helm chart for errors
88. helm dependency update            # Update subcharts
89. kubectl diff -f <file>            # Show changes before applying
```

## 📄 File Management & System Services

```bash
90. ls -l                             # List files with details
91. tail -f <file>                    # Follow file output
92. nano <file>                       # Simple text editor
93. cat <file>                        # Show file contents
94. less <file>                       # Scrollable file view
95. grep <pattern> <file>            # Search in files
96. find . -name <filename>          # Find file by name
97. chmod +x <file>                  # Make file executable
98. chown <user>:<group> <file>      # Change file ownership
99. systemctl restart <service>      # Restart system service
100. systemctl status <service>      # Check service status
```

## 🧾 Git Basics

```bash
101. git init                         # Initialize a new Git repo
102. git clone <repo-url>             # Clone a remote repository
103. git status                       # Show changed files
104. git add .                        # Stage all changes
105. git commit -m "message"         # Commit with message
106. git pull                         # Pull latest changes
107. git push                         # Push local changes
108. git log                          # Show commit history
109. git branch                       # List branches
110. git checkout -b <branch>         # Create and switch to new branch
```

## ✏️ vi / vim Editor Essentials

```bash
:x     # Save and quit
:q     # Quit (fails if changes)
:q!    # Force quit without saving
:w     # Save file
i      # Enter insert mode
Esc    # Exit insert mode
/dd    # Delete current line
u      # Undo
/word  # Search for 'word'
:n     # Go to next search match
```

---

# 📌 Future Enhancements

- GitOps deployment via ArgoCD or Flux
- Horizontal Pod Autoscaling (HPA)
- CI/CD integration (GitHub Actions or GitLab)
- SSL/TLS setup with Cert-Manager

> 💡 Pro Tip: Keep this file open in VS Code for fast access during troubleshooting.
> For YAML validation and testing, use tools like [kubelint](https://github.com/keikoproj/kube-linter) or online validators.

---

# TODO

- ELK configmaps - use env variables for service hostnames!