# 📊 Observability Stack / Monitoring

## What is **Grafana**?
**Grafana** is an **open-source dashboard tool** that allows you to **visualize data**.  
It shows, for example:
- System load (CPU, RAM, etc.)
- Requests per second
- Error rates
- and much more

Key features:
- Creation of visual dashboards (graphs, tables, gauges)
- Connecting Prometheus as a data source
- Configuration of alerts (e.g., for errors or resource shortages)
- User rights management (read/write)
- Default login: `admin/yourpassword`
- Dashboards for:
  - Service health & HTTP metrics
  - Container performance
- Default port 3000

Grafana accesses various **data sources**, e.g.:
- **Prometheus** (metrics)
- **Elasticsearch** (logs)
- **Loki**, **InfluxDB**, etc.


## Monitoring Stack with **Prometheus**
- Prometheus is **pull-based** – it regularly queries metrics.
- **What are metrics?**  
  Time-based measurements like CPU usage, RAM, HTTP requests per second.
- **PromQL**: Query language for analyzing metrics.
- Scrapes `/metrics` from all services
- Default port 9090

### Typical Exporters
- **Node Exporter**: Host system metrics (CPU, RAM, disk)
- **Kube-State-Metrics**: States of Kubernetes objects
- **Custom Exporter**: For your own applications

### Components

- **Prometheus**  
  → Collects **metrics** from services, containers, hosts, etc.

- **Node Exporter / App Exporter**  
  → Small tools that provide system or app data to Prometheus.

- **Grafana**  
  → Uses Prometheus data for **visualization** in dashboards.

### Result

- Live monitoring of your systems
- Early detection of problems
- Better scalability & performance control

## Logging Stack with **Elasticsearch** (ELK)
- **Elasticsearch**: Stores structured logs (indexable, searchable)
- **Logstash**: Processes & transforms logs (input → filter → output)
  → Collects and processes logs (e.g., from containers, servers, applications).
- **Kibana**: Visualizes logs, dashboards & filters
  → Web interface for **searching and analyzing** logs.

### Result
- Central log overview
- Easy error search & filtering
- Visualization of log trends

## Loki + Promtail
- Reads from `/app/logs/*.log`
- Real-time log streaming in Grafana

## Logstash + Filebeat
- Log ingestion and enrichment (with geolocation)
- ElasticSearch-ready output
