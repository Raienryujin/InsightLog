# 🧾 InsightLog.Json

JSON formatter for **InsightLog** — emit structured, machine-readable log data.

- 📦 NuGet: [`InsightLog.Json`](https://www.nuget.org/packages/InsightLog.Json)
- 🧭 Docs: [Main Documentation](https://github.com/Raienryujin/InsightLog)

## Install
```bash
dotnet add package InsightLog.Json
```

## Example 
```csharp
using InsightLog;
using InsightLog.Json;

var log = InsightLogFactory.Create(builder =>
{
    builder.UseJson(); // Enable JSON output
});

log.Info("User login event", new { UserId = 42 });
```

✅ Ideal for ELK, Seq, or Datadog pipelines
📘 [Full Docs →](https://github.com/Raienryujin/InsightLog/blob/main/README.md)
