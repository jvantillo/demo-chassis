#!/bin/sh
set -e

# Generate key for CoreDNS
openssl genrsa -out coredns.key 2048

# Create CSR
openssl req -new -nodes -key coredns.key -out coredns.csr -config coredns.conf -extensions v3_req
#-extensions 'v3_req'

# Sign CoreDNS CSR against our root
openssl x509 -req -in coredns.csr -CA rootCA.crt -CAkey rootCA.key -passin file:capass.txt -CAcreateserial -out coredns.pem -days 365 -sha256 -extfile coredns_sign.conf
