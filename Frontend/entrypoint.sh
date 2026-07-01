#!/bin/sh

for var in $(env | grep '^VITE_' | cut -d= -f1); do
  value=$(printenv $var)
  echo "🔁 Replacing REPLACE_${var} with ${value}"
  find /usr/share/nginx/html/ -type f -name "*.js" -exec sed -i "s|REPLACE_${var}|${value}|g" {} +
done

echo "🚀 Starting Nginx..."
nginx -g 'daemon off;'