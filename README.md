# Demo: conventions, patterns, microservices chassis

**// TODO: write better instructions including pictures**

But summary:

Prerequisites:

* Docker
* Docker Compose

This was created & testing on Windows 10 using WSL 2.

Steps:

* Bring up base platform using the individual docker-compose files (order: `coredns, consul, traefik, jaeger, kafka, prometheus, demolinks`) using `docker-compose up -d --build`
* (Optional) Bring up .NET containers through Visual Studio and running the `docker-compose` project in the solution.
* (Optional) Use http://demo.localdev (see notes below) to see a list of links, or view `index.html` in the `demolinks` compose folder.

Several ports are exposed, including:

* `80` - HTTP edge routing
* `53` - DNS, including resolvers for `*.consul` and `*.localdev`
* `5553` - DNS over HTTPS. You can configure Firefox (on your host) to use this using the address https://127.0.0.1:5553/dns-query This means domains like http://demo.localdev are easily accessible. Not critical, but useful for the demo.


> note: Other ports are opened. Depending on your system, they may be in use or reserved. In Windows, reserved ports can be found through `netsh interface ipv4 show excludedportrange tcp`


Useful links:

Docker

* https://kubernetes.io/blog/2020/05/21/wsl-docker-kubernetes-on-the-windows-desktop/
* https://github.com/Sinkler/docker-nginx-blue-green
* https://github.com/gliderlabs/registrator


Kafka

* https://github.com/mhowlett/howlett-kafka-extensions