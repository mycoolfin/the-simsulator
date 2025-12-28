# Headless Mode Guide

The Simsulator supports headless mode for running evolutionary simulations without graphics, ideal for batch processing, research, and automated experiments on servers or clusters.

## Table of Contents

- [Overview](#overview)
- [Basic Usage](#basic-usage)
- [Command-Line Parameters](#command-line-parameters)
- [Examples](#examples)
- [Output Format](#output-format)
- [Performance Considerations](#performance-considerations)
- [Automation & Batch Processing](#automation--batch-processing)
- [Troubleshooting](#troubleshooting)

## Overview

Headless mode runs simulations at full speed without rendering graphics, allowing you to:
- Run multiple simulations in parallel
- Execute long-running evolutionary experiments
- Automate parameter sweeps and research workflows
- Run simulations on servers without displays
- Generate datasets for analysis

The headless runner automatically detects batch mode and loads a simplified simulation environment optimised for performance.

## Basic Usage

### macOS

```bash
./TheSimsulator.app/Contents/MacOS/The Simsulator -batchmode -nographics \
  -logfile - \
  -populationSize 500 \
  -maxGenerations 200 \
  -outputDir ./results
```

### Linux

```bash
./TheSimsulator.x86_64 -batchmode -nographics \
  -logfile - \
  -populationSize 500 \
  -maxGenerations 200 \
  -outputDir ./results
```

### Windows

```cmd
TheSimsulator.exe -batchmode -nographics ^
  -logfile - ^
  -populationSize 500 ^
  -maxGenerations 200 ^
  -outputDir .\results
```

### Required Unity Arguments

- `-batchmode` - Run Unity in batch mode without graphics
- `-nographics` - Disable graphics device initialization

### Optional Arguments

You can add `-logFile -` to print logs to stdout, or `-logFile <path>` to redirect to a file.

## Command-Line Parameters

All evolution parameters can be configured via command-line arguments:

### Population & Evolution

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `populationSize` | int | 100 | Number of creatures in each generation |
| `maxGenerations` | int | 100 | Maximum number of generations to evolve |
| `survivalRate` | float | 0.2 | Fraction of population that survives each generation (0.0-1.0) |
| `mutationRate` | float | 1.0 | Average mutations per offspring (Poisson-distributed) |

### Simulation Timing

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `settleSeconds` | float | 10.0 | Time to let creature settle before assessment |
| `assessmentSeconds` | float | 10.0 | Duration to evaluate creature fitness |

### Trial Types

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `trialType` | enum | GroundDistance | Type of fitness trial to perform |

Available trial types:
- `GroundDistance` - Maximize distance traveled on ground
- `WaterDistance` - Maximize distance traveled in water
- `GroundLightFollowing` - Follow light source on ground
- `WaterLightFollowing` - Follow light source in water

### Output

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `outputDir` | string | none | Directory to save CSV results (optional) |

## Examples

### Basic Evolution Run

```bash
./TheSimsulator.app/Contents/MacOS/The Simsulator -batchmode -nographics \
  -populationSize 200 \
  -maxGenerations 50 \
  -outputDir ~/evolution_results
```

### Water Evolution with Custom Parameters

```bash
./TheSimsulator.app/Contents/MacOS/The Simsulator -batchmode -nographics \
  -trialType WaterDistance \
  -populationSize 400 \
  -maxGenerations 200 \
  -survivalRate 0.3 \
  -mutationRate 2.5 \
  -settleSeconds 2.0 \
  -assessmentSeconds 15.0 \
  -outputDir ~/water_evolution
```

### Light Following Experiment

```bash
./TheSimsulator.app/Contents/MacOS/The Simsulator -batchmode -nographics \
  -trialType GroundLightFollowing \
  -populationSize 500 \
  -assessmentSeconds 30.0 \
  -outputDir ~/light_following_results
```

## Output Format

When you specify `-outputDir`, the simulation generates a CSV file with the following naming convention:

```
evolution_P{pop}_G{gens}_S{survival}_M{mutation}_SS{settle}_AS{assess}_T{trial}_{timestamp}.csv
```

Example: `evolution_P50_G100_S0.5_M0.1_SS1.0_AS10.0_TGroundDistance_20251227_143022.csv`

### CSV Structure

```csv
Generation,Best Fitness,Average Fitness,Elapsed Time
1,1.234,0.567,0.123
2,2.345,0.789,0.245
3,3.456,1.012,0.367
...
```

**Columns:**
- `Generation` - Generation number (0-indexed)
- `Best Fitness` - Fitness of the best creature in this generation
- `Average Fitness` - Mean fitness across all creatures
- `Elapsed Time` - Cumulative simulation time in seconds

## Performance Considerations

### Computational Resources

- **CPU**: Simulations are CPU-intensive. Each creature's physics is simulated independently.
- **Memory**: Memory usage scales with population size and creature complexity
- **Disk**: CSV output files are small (typically <100KB for 100 generations)

### Optimisation Tips

1. **Population Size**: Larger populations improve evolution quality but increase runtime linearly
2. **Assessment Time**: Longer assessments give more accurate fitness but increase generation time
3. **Parallel Runs**: Run multiple independent simulations with different parameters simultaneously
4. **Resource Limits**: Consider using process limits (`ulimit` on Unix) for controlled resource usage

## Automation & Batch Processing

### Parameter Sweep Script (Bash)

```bash
#!/bin/bash

APP="./TheSimsulator.app/Contents/MacOS/The Simsulator"
OUTPUT_DIR="~/experiment_results/$(date +%Y%m%d_%H%M%S)"
mkdir -p "$OUTPUT_DIR"

# Sweep over different population sizes
for POP_SIZE in 20 30 40 50; do
    for TRIAL in GroundDistance WaterDistance; do
        echo "Running: PopSize=$POP_SIZE, Trial=$TRIAL"
        
        "$APP" -batchmode -nographics \
            -populationSize $POP_SIZE \
            -maxGenerations 100 \
            -trialType $TRIAL \
            -outputDir "$OUTPUT_DIR" \
            -logFile "$OUTPUT_DIR/log_P${POP_SIZE}_${TRIAL}.txt"
    done
done

echo "All simulations complete! Results in $OUTPUT_DIR"
```

### Parameter Sweep Script (PowerShell)

```powershell
$APP = ".\TheSimsulator.exe"
$OUTPUT_DIR = ".\experiment_results\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $OUTPUT_DIR -Force

$PopSizes = @(20, 30, 40, 50)
$Trials = @("GroundDistance", "WaterDistance")

foreach ($PopSize in $PopSizes) {
    foreach ($Trial in $Trials) {
        Write-Host "Running: PopSize=$PopSize, Trial=$Trial"
        
        & $APP -batchmode -nographics `
            -populationSize $PopSize `
            -maxGenerations 100 `
            -trialType $Trial `
            -outputDir $OUTPUT_DIR `
            -logFile "$OUTPUT_DIR\log_P${PopSize}_${Trial}.txt"
    }
}

Write-Host "All simulations complete! Results in $OUTPUT_DIR"
```

### Running Multiple Simulations in Parallel

```bash
#!/bin/bash

APP="./TheSimsulator.app/Contents/MacOS/The Simsulator"
OUTPUT_DIR="~/parallel_experiments"
mkdir -p "$OUTPUT_DIR"

# Run 4 different trials in parallel
"$APP" -batchmode -nographics -trialType GroundDistance -populationSize 30 -maxGenerations 100 -outputDir "$OUTPUT_DIR" &
"$APP" -batchmode -nographics -trialType WaterDistance -populationSize 30 -maxGenerations 100 -outputDir "$OUTPUT_DIR" &
"$APP" -batchmode -nographics -trialType GroundLightFollowing -populationSize 30 -maxGenerations 100 -outputDir "$OUTPUT_DIR" &
"$APP" -batchmode -nographics -trialType WaterLightFollowing -populationSize 30 -maxGenerations 100 -outputDir "$OUTPUT_DIR" &

# Wait for all to complete
wait
echo "All parallel simulations complete!"
```

## Troubleshooting

### Application Doesn't Start

**Issue**: Application exits immediately or crashes on startup.

**Solutions**:
- Ensure you're using both `-batchmode` and `-nographics` flags
- Check Unity log file for errors: `-logFile /path/to/log.txt`
- Verify executable has proper permissions: `chmod +x TheSimsulator`

### No Output File Generated

**Issue**: Simulation completes but no CSV file is created.

**Solutions**:
- Verify `-outputDir` path exists and is writable
- Use absolute paths instead of relative paths
- Check application logs for permission errors

### Simulation Runs Forever

**Issue**: Process doesn't terminate after expected time.

**Solutions**:
- Ensure `-maxGenerations` parameter is set
- Check that the application isn't waiting for input
- Use `-logFile -` to monitor progress in real-time

### Out of Memory

**Issue**: Process crashes with memory errors.

**Solutions**:
- Reduce `-populationSize` parameter
- Monitor memory usage with `top` or Task Manager
- Run fewer parallel simulations

### Performance is Slower Than Expected

**Solutions**:
- Reduce `-settleSeconds` and `-assessmentSeconds` if possible
- Lower `-populationSize` for faster iterations
- Ensure no other CPU-intensive processes are running
- Check that your system isn't thermal throttling

---
