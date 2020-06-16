#!/bin/sh
set -e

(cd demolinks && exec docker-compose down -v)
(cd tools && docker-compose down -v)
(cd traefik && docker-compose down -v)
(cd jaeger-dag && docker-compose down -v)
(cd jaeger && docker-compose down -v)
(cd kafka && docker-compose down -v)
(cd prometheus && docker-compose down -v)
(cd consul && docker-compose down -v)
(cd coredns && docker-compose down -v)
