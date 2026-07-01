# SonarQube Build Configuration Guide

This guide explains how to use the new SonarQube build configuration that allows you to run builds with or without SonarQube analysis to optimize build times.

## Overview

The project now supports conditional SonarQube analysis through the `EnableSonarQubeAnalysis` MSBuild property. This allows for:

- **Fast builds** (default): Skip SonarQube analysis for faster development cycles
- **Quality builds**: Include SonarQube analysis for code quality checks

## Quick Start

### Local Development

#### Using Build Scripts (Recommended)

**Linux/macOS:**
```bash
# Fast build (default)
./Scripts/build.sh

# Fast build with tests
./Scripts/build.sh --fast --test

# Build with SonarQube analysis
./Scripts/build.sh --sonar

# Build with SonarQube analysis and test coverage
./Scripts/build.sh --coverage

# Clean build with SonarQube analysis
./Scripts/build.sh --clean --sonar
```

**Windows (PowerShell):**
```powershell
# Fast build (default)
.\Scripts\build.ps1

# Fast build with tests
.\Scripts\build.ps1 -Fast -Test

# Build with SonarQube analysis
.\Scripts\build.ps1 -Sonar

# Build with SonarQube analysis and test coverage
.\Scripts\build.ps1 -Coverage

# Clean build with SonarQube analysis
.\Scripts\build.ps1 -Clean -Sonar
```

#### Using dotnet CLI Directly

**Fast build (default):**
```bash
cd Backend
dotnet build GHB.DP2.sln --configuration Release /p:EnableSonarQubeAnalysis=false
```

**Build with SonarQube analysis:**
```bash
cd Backend
dotnet build GHB.DP2.sln --configuration Release /p:EnableSonarQubeAnalysis=true
```

### CI/CD Pipelines

#### Regular Builds (Fast)
The main `azure-pipelines.yml` now runs fast builds by default:
- No SonarQube analysis
- Faster build times
- Suitable for PR validation and regular CI

#### SonarQube Analysis Builds
Use the dedicated `azure-pipelines-sonarqube.yml` pipeline:
- Manual trigger or scheduled runs
- Full SonarQube analysis with coverage
- Generates quality reports

To trigger the SonarQube pipeline:
1. Go to Azure DevOps Pipelines
2. Select "SonarQube Analysis Pipeline"
3. Click "Run pipeline"
4. Choose which components to analyze (Backend/Frontend)

## Configuration Details

### MSBuild Property

The `EnableSonarQubeAnalysis` property controls SonarQube integration:

- `EnableSonarQubeAnalysis=false` (default): Fast build mode
- `EnableSonarQubeAnalysis=true`: SonarQube analysis mode

### What Changes with SonarQube Analysis

When `EnableSonarQubeAnalysis=true`:

1. **Additional packages are included:**
   - SonarAnalyzer.CSharp
   - Microsoft.CodeAnalysis.NetAnalyzers

2. **Code formatting runs:**
   - `dotnet format` executes during PreBuild

3. **Additional analysis:**
   - Static code analysis
   - Code quality metrics
   - Security vulnerability detection

When `EnableSonarQubeAnalysis=false`:

1. **Faster builds:**
   - No additional analyzers
   - No code formatting
   - Minimal analysis overhead

2. **Reduced dependencies:**
   - SonarQube packages not loaded
   - Faster package restore

## Build Time Comparison

| Build Type | Typical Time | Use Case |
|------------|-------------|----------|
| Fast Build | 30-60 seconds | Development, PR validation |
| SonarQube Build | 2-5 minutes | Quality gates, releases |

## Pipeline Structure

### azure-pipelines.yml (Main Pipeline)
- **Trigger:** PR and manual builds
- **Mode:** Fast builds only
- **Purpose:** Quick validation and testing
- **SonarQube:** Disabled for speed

### azure-pipelines-sonarqube.yml (Quality Pipeline)
- **Trigger:** Manual only
- **Mode:** Full SonarQube analysis
- **Purpose:** Code quality assessment
- **Features:** Coverage reports, quality gates

## Best Practices

### For Developers

1. **Daily development:** Use fast builds
   ```bash
   ./Scripts/build.sh --fast --test
   ```

2. **Before committing:** Run SonarQube analysis
   ```bash
   ./Scripts/build.sh --sonar --test
   ```

3. **Code quality check:** Use coverage analysis
   ```bash
   ./Scripts/build.sh --coverage
   ```

### For CI/CD

1. **PR validation:** Use main pipeline (fast builds)
2. **Quality gates:** Use SonarQube pipeline
3. **Release builds:** Include SonarQube analysis
4. **Scheduled quality checks:** Run SonarQube pipeline nightly

## Troubleshooting

### Common Issues

**Build fails with SonarQube enabled:**
1. Ensure SonarQube tools are installed:
   ```bash
   dotnet tool install --global dotnet-sonarscanner
   dotnet tool install --global dotnet-coverage
   ```

**SonarQube packages not found:**
1. Check that `EnableSonarQubeAnalysis=true` is set
2. Restore packages with the flag:
   ```bash
   dotnet restore /p:EnableSonarQubeAnalysis=true
   ```

**Slow builds in development:**
1. Ensure you're using fast mode:
   ```bash
   ./Scripts/build.sh --fast
   ```

### Environment Variables

For SonarQube analysis, ensure these variables are set:
- `SonarServerUrl`: SonarQube server URL
- `SonarQubeKey`: Authentication token
- `ProjectName`: SonarQube project key

## Migration Notes

### From Previous Setup

The previous setup ran SonarQube analysis on every build. The new setup:

1. **Separates concerns:** Fast builds vs. quality analysis
2. **Improves developer experience:** Faster feedback loops
3. **Maintains quality:** Dedicated quality pipeline
4. **Reduces CI costs:** Less compute time for regular builds

### Updating Local Workflows

Replace old commands:
```bash
# Old way
dotnet build

# New way (fast)
./Scripts/build.sh --fast

# New way (with quality analysis)
./Scripts/build.sh --sonar
```

## Support

For issues or questions about the SonarQube build configuration:

1. Check this guide first
2. Review build script help: `./Scripts/build.sh --help`
3. Check pipeline logs in Azure DevOps
4. Contact the development team
