# Demo: conventions, patterns, microservices chassis

**// TODO: write better instructions including pictures**

But summary:

## Prerequisites

* Docker
* Docker Compose

This was created & testing on Windows 10 using WSL 2.


## Steps

* Bring up base platform using the individual docker-compose files. See the `compose/all-up.sh` script.
* (Optional) Bring up .NET containers through Visual Studio and running the `docker-compose` project in the solution.
* (Optional) Use http://demo.localdev (see notes below) to see a list of links, or view `index.html` in the `demolinks` compose folder.

Several ports are exposed:

* `80` - HTTP edge routing
* `5553` - DNS over HTTPS. (see below)
* `9021` - Confluent Control Center
* `8088` - dsqlDB server
* `16686` - Jaeger UI
* `22101` - Grafana
* `9090` - Prometheus

> Note: Other ports are opened. Depending on your system, they may be in use or reserved. In Windows, reserved ports can be found through `netsh interface ipv4 show excludedportrange tcp`


# Accessing from Firefox on your host

You can configure Firefox (on your host) easily, and this means domains like http://demo.localdev are easily accessible. Not critical, but useful for the demo. Where to do this in Firefox:
* Options- General - Network Settings
* Tick 'Enable DNS over HTTPS', 'custom', `https://127.0.0.1:5553/dns-query`

Note that Firefox will still try to use your regular DNS as fallback, if you want to disable this and enforce the setting, you can opt to set `network.trr.mode` via `about:config` to value `3`.


## Useful links

Docker

* https://kubernetes.io/blog/2020/05/21/wsl-docker-kubernetes-on-the-windows-desktop/
* https://github.com/Sinkler/docker-nginx-blue-green
* https://github.com/gliderlabs/registrator


Kafka

* https://github.com/mhowlett/howlett-kafka-extensions