#!/bin/bash
set -e

if ls /usr/local/share/ca-certificates/*.crt 1>/dev/null 2>&1; then
    update-ca-certificates
fi

exec gosu appuser dotnet GHB.DP2.Api.dll
