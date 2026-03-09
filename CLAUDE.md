# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GCC-PHAT (Generalized Cross-Correlation with Phase Transform) — a C# .NET 5.0 console application for time-delay estimation between two audio channels in a stereo WAV file.

## Build & Run

```bash
# Build (debug)
dotnet build gccphat.csproj

# Build (release)
dotnet build gccphat.csproj -c Release

# Run
gccphat.exe <audioFilePath> <bufferSize> <fmin> <fmax> <outputMode>

# Example (matches launchSettings.json debug profile)
gccphat.exe stereo_noise.wav 4096 200 8000 1
```

**Parameters:**
- `audioFilePath`: Stereo WAV file path
- `bufferSize`: Power-of-2 window size (e.g. 1024, 2048, 4096, 8192)
- `fmin` / `fmax`: Frequency band in Hz for band-pass filtering
- `outputMode`: `0` = console, `1` = CSV file

There is no test framework — manual testing uses the included `stereo_noise.wav`.

## Architecture

Two source files contain the entire logic:

### `Program.cs`
Entry point. Handles CLI argument parsing, loads the stereo WAV via NAudio (`ReadStereoAudio()`), then calls `ComputeTimeDelays()` which processes each buffer in parallel using `ConcurrentBag<>`.

### `gccphat_core.cs`
Two classes:

- **`FFT2`**: In-place complex FFT/IFFT (MIT-licensed, Gerald T. Beauregard 2010). Used internally by `GccPhatCore`.
- **`GccPhatCore`**: The algorithm itself.
  - `GCCPHAT(ch1, ch2, fs, nfft, fmin, fmax)` — main entry point; returns time delay in ms and RMS energy.
  - `COLORE_FREQ()` — band-pass filter in frequency domain.
  - `ComputeSigoutComplex()` — applies phase from one signal with magnitude of another (the PHAT weighting).
  - `ComputeRMSFromFFT()` — RMS via Parseval's theorem.
  - Static lookup table for precomputed exponential values; FFT parameters (nfft, fs) are cached to avoid reinitialization across calls.

### Algorithm flow
1. FFT both channels, apply band-pass filter `[fmin, fmax]`
2. Cross-spectrum: `Pxy = FFT(ch1) × conj(FFT(ch2))`
3. Phase-only normalization (PHAT weighting)
4. IFFT → find peak → convert sample offset to milliseconds
5. Output delay + RMS per buffer

## Dependencies

Managed via NuGet (`packages.config`):
- **NAudio 2.2.1** — WAV file I/O
- **MathNet.Numerics 5.0.0** — numerical utilities
- **System.Numerics** — `Complex` type
