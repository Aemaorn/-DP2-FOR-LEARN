#!/bin/bash

# Define project keys
BACKEND_PROJECT_KEY="GHB-DP2-Backend"
FRONTEND_PROJECT_KEY="GHB-DP2-Frontend"

# Check if we're analyzing backend or frontend
if [ "$1" == "frontend" ]; then
  echo "Analyzing Frontend: $FRONTEND_PROJECT_KEY"

  # Install SonarScanner for JS/TS
  npm install -g sonarqube-scanner

  cd ./Frontend

  # Install dependencies
  npm install

  # Run tests with coverage
  npm run test:unit -- --coverage

  # Run SonarQube analysis
  sonar-scanner \
    -Dsonar.projectKey="$FRONTEND_PROJECT_KEY" \
    -Dsonar.sources=./src \
    -Dsonar.host.url="http://192.168.20.97:9001" \
    -Dsonar.login=$SONARKEY \
    -Dsonar.javascript.lcov.reportPaths=./coverage/lcov.info \
    -Dsonar.typescript.tsconfigPath=./tsconfig.json \
    -Dsonar.exclusions=**/node_modules/**,**/*.spec.ts,**/*.test.ts

  cd ..
else
  # Default to backend analysis
  echo "Analyzing Backend: $BACKEND_PROJECT_KEY"

  # Install .NET tools
  dotnet tool install --global dotnet-sonarscanner
  dotnet tool install --global dotnet-coverage

  # Run SonarQube analysis
  dotnet-sonarscanner begin /k:"$BACKEND_PROJECT_KEY" /d:sonar.host.url="http://192.168.20.97:9001" /d:sonar.login=$SONARKEY /d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml /d:sonar.dotnet.excludeTestProjects=true
  dotnet restore ./Backend/GHB.DP2.sln
  dotnet build ./Backend/GHB.DP2.sln --no-restore --configuration Release /p:ContinuousIntegrationBuild=true
  dotnet-coverage collect 'dotnet test ./Backend/GHB.DP2.sln --no-build --configuration Release' -f xml  -o 'coverage.xml'
  dotnet-sonarscanner end /d:sonar.login=$SONARKEY
fi