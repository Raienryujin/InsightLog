
# 🖥️ InsightLog.Console

Console sink for **InsightLog** — colorized and templated output for dev and CI.

- 📦 NuGet: [`InsightLog.Console`](https://www.nuget.org/packages/InsightLog.Console)
- 🧭 Docs: [Main Documentation](https://github.com/Raienryujin/InsightLog)

## Install
```bash
dotnet add package InsightLog.Console
```

## Example
```csharp
using InsightLog;
using InsightLog.Console;

var log = InsightLogFactory.Create(builder =>
{
    builder.WriteToConsole(opts =>
    {
        opts.UseAnsiColors = true;
        opts.Template = "[{Level:u3}] {Message}{NewLine}{Exception}";
    });
});

log.Error("Something went wrong");
```

🎨 Great for local dev and GitHub Actions logs
📘 [Learn More →](https://github.com/Raienryujin/InsightLog)



