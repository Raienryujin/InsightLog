
# 📂 InsightLog.File

File sink for **InsightLog** — rolling logs with retention and concurrency safety.

- 📦 NuGet: [`InsightLog.File`](https://www.nuget.org/packages/InsightLog.File)
- 🧭 Docs: [Main Documentation](https://github.com/Raienryujin/InsightLog)

## Install
```bash
dotnet add package InsightLog.File
```

## Example
```csharp
using InsightLog;
using InsightLog.File;

var log = InsightLogFactory.Create(builder =>
{
    builder.WriteToFile(opts =>
    {
        opts.Path = "logs/app-.log";
        opts.RollingInterval = "Day";
        opts.RetentionDays = 14;
    });
});

log.Warning("File sink configured");
```

🪶 Works seamlessly with InsightLog.Json
📘 [More Info →](https://github.com/Raienryujin/InsightLog/blob/main/README.md)