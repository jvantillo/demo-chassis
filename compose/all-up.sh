#!/bin/sh
set -e

#!/bin/sh
set -e

(cd coredns && docker-compose up -d --build)
(cd consul && docker-compose up -d --build)

echo "Waiting 40 seconds for Consul to converge" && sleep 40

(cd prometheus && docker-compose up -d --build)
(cd jaeger && docker-compose up -d --build)
(cd traefik && docker-compose up -d --build)
(cd tools && docker-compose up -d --build)
(cd demolinks && exec docker-compose up -d --build)
(cd jaeger-dag && docker-compose up --no-start --build)
(cd kafka && docker-compose up -d --build)

echo "Note 1: Kafka may need a few seconds to spin up before it is ready"
echo "Note 2: jaeger-dag has been created, but will not be started - start it manually when you have data in Jaeger and want to visualize the DAG of services"
