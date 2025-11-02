# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Commands

- Prerequisite check
  - dotnet --version
- Restore packages
  - dotnet restore
- Build (Debug/Release)
  - dotnet build -c Debug
  - dotnet build -c Release
- Watch build on change
  - dotnet watch build
- Lint/format (C#)
  - dotnet format
- Tests
  - There are no test projects in this repo currently.
  - If/when tests are added: dotnet test
  - Run a single test (example): dotnet test --filter "FullyQualifiedName~Namespace.ClassName.TestMethod"
- Run the app (do not run in this terminal; let the user run it in their own terminal)
  - dotnet run --project AudioVisualizer.csproj
- Publish (Windows x64)
  - dotnet publish -c Release -r win-x64 --self-contained false

## Architecture and Structure (high level)

- Platform and project
  - WPF application targeting net9.0-windows; WPF enabled via UseWPF in AudioVisualizer.csproj
  - NuGet dependencies: ModernWpfUI (theming), NAudio (audio capture + DSP)
- Entry points and UI composition
  - App.xaml: application definition, ModernWpf theme resources (Dark theme)
  - MainWindow.xaml/.cs: single-window UI with a Canvas (VisualizerCanvas) that hosts a set of Rectangle bars
- Rendering model
  - On load and on window resize, bars are (re)created adaptively: ~1 bar per ~10px of window width
  - Each bar is a Rectangle placed along the X axis; colors derive from an HSV→RGB gradient spanning 0–360° (rainbow)
  - A DispatcherTimer (~60 FPS) drives smooth animation: current height eases toward a target height with extra "gravity" on fall
- Audio pipeline (NAudio)
  - WasapiLoopbackCapture captures system playback (no microphone needed)
  - Samples are buffered into a Complex[] ring and processed with a 2048-point FFT (NAudio.Dsp.FastFourierTransform)
  - Magnitudes (sqrt(real^2+imag^2)) are computed for the first half of the spectrum and emitted via FftCalculated
- Data flow
  - Capture → buffer → FFT → magnitude array → UI target bar heights → animation tick updates Rectangle heights and Canvas positions
- Notable behaviors and extension points
  - Sensitivity: MainWindow.xaml.cs (sensitivity constant) scales visual response
  - Color mapping: GetBarBrush/HsvToRgb in MainWindow.xaml.cs controls per-bar color
  - Bar layout: InitializeBars controls bar count, width/spacing, and rounded corners
  - Frequency mapping: OnFftCalculated mirrors spectrum from center outward and uses a nonlinear (quadratic) index mapping to emphasize lows

## Key files

- AudioVisualizer.csproj: target framework, WPF enablement, NuGet dependencies (ModernWpfUI, NAudio)
- App.xaml / App.xaml.cs: WPF app definition and theme resources
- MainWindow.xaml / MainWindow.xaml.cs: UI canvas and visualization logic (bars, colors, animation loop)
- AudioCapture.cs: audio loopback capture, buffering, FFT, and FftCalculated event
- AssemblyInfo.cs: theme resource dictionary locations
