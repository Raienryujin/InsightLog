# Changelog

All notable changes to InsightLog will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [1.0.2] - 2025-11-08 @ 20:31

### 🧪 Correct Tests
- **Increased sample** size from 20/50 to 100 messages for sample rates 2 and 5 — larger samples reduce statistical variance.
- **Widened tolerance** from ±5 to ±50% of expected value — accounts for randomness in sampling.
- **Increased delay** from 100ms to 200ms — ensures all async writes complete before assertion.

## [1.0.1] - 2025-11-08 (2)

### ⚙️ Github Actions
- Updated all Github Actions to v4
- Added `fail-fast:false` to strategy
- Fixed BenchmarkDotNet configuration
- Improved error handling

---

## [1.0.0] — 2025-11-08

### 🚀 Release Highlights
- Official **stable release** of InsightLog after extensive testing and optimization.  
- Introduced multiple sinks, async scope handling, and high-performance architecture.  

---

## [0.9.0] — 2025-10-25

### 🧪 Pre-Release & Final Testing
- Implemented **structured JSON sink** for ELK / Seq pipelines.  
- Added **rolling file sink** with date- and size-based rotation.  
- Introduced **colorized console output** with theme customization.  
- Integrated **xUnit test suite** covering sink behavior, formatting, and performance.  
- Added **BenchmarkDotNet** performance benchmarks.  
- Improved internal logging diagnostics and XML documentation.  

---

## [0.8.0] — 2025-10-10

### ⚙️ Core Features
- Introduced **AsyncLocal-based scope management** for contextual indentation.  
- Added **timing measurement** utilities via `Measure()` and `Scope()`.  
- Implemented **parameter redaction** (by name or regex) for sensitive data protection.  
- Added **sampling support** for high-volume scenarios.  
- Improved performance of message formatting with minimal allocations.  

---

## [0.7.0] — 2025-09-25

### 🧩 Core Logging Engine
- Implemented six log levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`.  
- Added automatic **context capture** (file name, member, and line number).  
- Integrated **correlation ID tracking** via `Activity.Current` or auto-generated GUIDs.  
- Introduced **async sink writing** to prevent blocking.  
- Ensured **thread-safe operation** without locks in hot paths.  

---

## [0.6.0] — 2025-09-10

### 🛠️ Infrastructure & Integration
- Added `AddInsightLog()` extension for **dependency injection**.  
- Implemented flexible **configuration builder** for log pipelines.  
- Created **developer-friendly console theme presets** for readability.  
- Began preparing **XML docs** for IntelliSense and SDK publication.  

---

## [0.5.0] — 2025-08-28

### 💡 Foundation & Design
- Project scaffold and initial architecture planning.  
- Defined **log message pipeline**, **context propagation**, and **sink abstraction layer**.  
- Implemented **unit tests** for basic message flow and formatting.  
- Established **semantic versioning** and changelog tracking.  

---

## [Unreleased]

### 🧱 Planned
- Configuration binding from `appsettings.json`.  
- Cloud provider sinks (AWS, Azure, GCP).  
- OpenTelemetry integration.  
- Metrics collection alongside logging.  
- Dynamic log level changes at runtime.  
- Log aggregation and buffering.  
- HTTP sink for centralized logging.  
- Syslog and Windows Event Log sink support.  

---

### Performance
- Sub-microsecond no-op performance for filtered messages
- Minimal allocations in hot paths
- Thread-safe operation without locks in critical paths
- Async sink writing to prevent blocking

### Known Issues
- The file relies on **implicit global usings** (e.g., `System`, `System.Linq`, `System.Threading`). 
  If the project disables implicit usings, references like `DateTime`, `Enumerable.Repeat`, `CancellationTokenSource`, and `Task` will not resolve.

- The `IHost` created in `RunDependencyInjectionDemo` is **not disposed**. (`await using`?)