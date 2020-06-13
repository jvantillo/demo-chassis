#!/bin/sh
set -e

# Generate CA password
echo "admin" > capass.txt

# Generate root key
openssl genrsa -des3 -passout file:capass.txt -out rootCA.key 4096 -extensions v3_ca

# Generate root
openssl req -x509 -new -nodes -key rootCA.key -passin file:capass.txt -sha256 -days 3650 -out rootCA.crt -config ca.conf
