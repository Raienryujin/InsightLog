# Contributing to InsightLog

First off, thank you for considering contributing to InsightLog! It's people like you that make InsightLog such a great tool.

## Code of Conduct

This project and everyone participating in it is governed by the [InsightLog Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

* Use a clear and descriptive title
* Describe the exact steps which reproduce the problem
* Provide specific examples to demonstrate the steps
* Describe the behavior you observed after following the steps
* Explain which behavior you expected to see instead and why
* Include logs with appropriate log level (Debug or Trace)
* Include your environment details (.NET version, OS, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

* Use a clear and descriptive title
* Provide a step-by-step description of the suggested enhancement
* Provide specific examples to demonstrate the steps
* Describe the current behavior and explain which behavior you expected to see instead
* Explain why this enhancement would be useful

### Pull Requests

1. Fork the repo and create your branch from `main`
2. If you've added code that should be tested, add tests
3. If you've changed APIs, update the documentation
4. Ensure the test suite passes
5. Make sure your code follows the existing code style
6. Issue that pull request!

## Development Process

### Setting Up Your Environment

1. Install .NET 8 SDK or later
2. Clone the repository
3. Run `dotnet restore` in the root directory
4. Run `dotnet build` to ensure everything compiles
5. Run `dotnet test` to run the test suite

### Code Style

* Use 4 spaces for indentation (no tabs)
* Use `_camelCase` for private fields
* Use `PascalCase` for public properties and methods
* Use `camelCase` for parameters and local variables
* Always use braces for if/for/while/etc., even for single lines
* Keep lines under 120 characters when possible
* Add XML documentation comments for all public APIs

### Testing

* Write unit tests for all new functionality
* Ensure all tests pass before submitting PR
* Aim for >80% code coverage on new code
* Include integration tests for sink implementations
* Add benchmark tests for performance-critical code

### Commit Messages

* Use the present tense ("Add feature" not "Added feature")
* Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
* Limit the first line to 72 characters or less
* Reference issues and pull requests liberally after the first line

Example:
```
Add parameter redaction for credit card patterns

- Add regex pattern for common credit card formats
- Update MessageTemplateFormatter to handle new patterns
- Add unit tests for credit card redaction
- Update documentation with examples

Fixes #123
```

### Documentation

* Update README.md if you're adding new features
* Add XML documentation comments for public APIs
* Update CHANGELOG.md following Keep a Changelog format
* Include examples in documentation where appropriate

## Project Structure

```
InsightLog/
├── src/
│   ├── InsightLog/           # Core library
│   ├── InsightLog.Console/   # Console sink
│   ├── InsightLog.File/      # File sink
│   └── InsightLog.Json/      # JSON sink
├── tests/
│   └── InsightLog.Tests/     # Unit tests
├── bench/
│   └── InsightLog.Benchmarks/ # Performance benchmarks
└── samples/
    └── InsightLog.Demo/      # Demo application
```

## Release Process

1. Update version numbers in Directory.Build.props
2. Update CHANGELOG.md with release notes
3. Create a GitHub release with tag `v{version}`
4. CI/CD will automatically publish to NuGet

## Getting Help

* Check the [README](README.md) for usage examples
* Look through existing [issues](https://github.com/InsightLog/issues)
* Create a new issue if your problem isn't already covered
* Join our discussions in the [Discussions](https://github.com/insightlog/insightlog/discussions) tab

## Recognition

Contributors will be recognized in the README.md file and in release notes.

Thank you for contributing to InsightLog! 🎉
