# Superdev.AspNetCore
[![Version](https://img.shields.io/nuget/v/Superdev.AspNetCore.svg)](https://www.nuget.org/packages/Superdev.AspNetCore) [![Downloads](https://img.shields.io/nuget/dt/Superdev.AspNetCore.svg)](https://www.nuget.org/packages/Superdev.AspNetCore) [![Buy Me a Coffee](https://img.shields.io/badge/support-buy%20me%20a%20coffee-FFDD00)](https://buymeacoffee.com/thomasgalliker)

Superdev.AspNetCore provides reusable, low-dependency building blocks for ASP.NET Core applications.
It focuses on pragmatic infrastructure code which can be shared across projects.

## Download and Install Superdev.AspNetCore
This library is available on NuGet: https://www.nuget.org/packages/Superdev.AspNetCore
Use the following command to install Superdev.AspNetCore using NuGet package manager console:

    PM> Install-Package Superdev.AspNetCore

You can use this library in ASP.NET Core projects compatible to .NET 9 and higher.

## App Setup
`tbd`

## API Usage
The following documentation covers the reusable building blocks that are already available in this package.

### Security

#### Use claim-based authorization
`AuthorizeClaimAttribute` allows you to protect endpoints based on the existence or value of claims.

Require a claim to exist:
```csharp
using Superdev.AspNetCore.Security;

[AuthorizeClaim("permission")]
[HttpGet("profile")]
public IActionResult GetProfile()
{
    return this.Ok();
}
```

Require a specific claim value:
```csharp
using Superdev.AspNetCore.Security;

[AuthorizeClaim("permission", "admin")]
[HttpDelete("{id}")]
public IActionResult Delete(int id)
{
    return this.NoContent();
}
```

Use more advanced matching with `ClaimRequirementType`:
```csharp
using Superdev.AspNetCore.Security;

[AuthorizeClaim(ClaimRequirementType.Any, "permission", "read", "write")]
[HttpGet]
public IActionResult Get()
{
    return this.Ok();
}
```

### Application Model Conventions

#### Restrict endpoints to specific environments
`EnvironmentRestrictedAttribute` lets you expose a controller or action only in selected ASP.NET Core environments (e.g. `Development`, `Staging`, `Production`). Restricted endpoints are removed from the application model in all other environments, so they are not routed and do not show up in API metadata such as OpenAPI.

Register `EnvironmentRestrictedApplicationModelConvention` once when configuring MVC:
```csharp
using Superdev.AspNetCore.ApplicationModelConventions;

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(
        new EnvironmentRestrictedApplicationModelConvention(builder.Environment));
});
```

Restrict a whole controller to the development environment:
```csharp
using Microsoft.AspNetCore.Mvc;
using Superdev.AspNetCore.ApplicationModelConventions;

[ApiController]
[Route("api/[controller]")]
[EnvironmentRestricted("Development")]
public class DiagnosticsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => this.Ok();
}
```

Restrict a single action to multiple environments:
```csharp
using Superdev.AspNetCore.ApplicationModelConventions;

[HttpGet("seed")]
[EnvironmentRestricted("Development", "Staging")]
public IActionResult Seed()
{
    return this.Ok();
}
```

Environment names are matched case-insensitively against the names defined by `Microsoft.Extensions.Hosting.Environments`. Because attribute arguments must be compile-time constants, pass them as string literals (or your own `const` values). Controllers and actions without the attribute remain available in every environment.

> [!NOTE]
> The application model is built once at start-up, so the set of available endpoints reflects the environment at start-up and does not change at runtime.

### Options

#### Use writable options
Use writable options when a configuration section should be available through the regular options pipeline and should also be updateable at runtime.

`ConfigureWritable<T>`:
- binds the configuration section to the standard options infrastructure
- registers `IWritableOptions<T>` for the same section
- persists updates back to `appsettings.json` by default

`IWritableOptions<T>` extends `IOptionsSnapshot<T>`, so you can still read the current value via `Value` or `Get(name)`, and you also get write operations such as `UpdateAsync(...)` and `UpdatePropertyAsync(...)`.

Register writable options:
```csharp
using Superdev.AspNetCore.Options;

builder.Services.ConfigureWritable<MyFeatureOptions>(
    builder.Configuration.GetSection("MyFeature"));
```

Use a different file if needed:
```csharp
using Superdev.AspNetCore.Options;

builder.Services.ConfigureWritable<MyFeatureOptions>(
    builder.Configuration.GetSection("MyFeature"),
    "appsettings.Development.json");
```

Update options at runtime:
```csharp
using Microsoft.AspNetCore.Mvc;
using Superdev.AspNetCore.Options;

public class MyController : ControllerBase
{
    private readonly IWritableOptions<MyFeatureOptions> options;

    public MyController(IWritableOptions<MyFeatureOptions> options)
    {
        this.options = options;
    }

    [HttpPost("enable")]
    public async Task<IActionResult> EnableAsync()
    {
        await this.options.UpdatePropertyAsync(x => x.Enabled, true);
        return this.Ok();
    }
}
```

Update multiple values in one operation:
```csharp
await this.options.UpdateAsync(current =>
{
    current.Enabled = true;
    current.RefreshIntervalInMinutes = 5;
});
```

Use regular options for read-only scenarios and `IWritableOptions<T>` only where runtime persistence is actually required.

> [!WARNING]
> Writable options modify the configured JSON file on disk. Use this feature intentionally and avoid exposing it through unprotected endpoints.

#### Concurrency and limitations
Writable options persist to a JSON file and are meant for occasional configuration changes, not as a high-throughput or transactional data store. Each update rewrites the configured section as a whole, so concurrent updates follow last-writer-wins semantics.

Concurrent writes from within the same process are serialized by an in-process lock keyed by the resolved file path, so they neither corrupt the file nor fail with sharing violations. Writes from other processes or application instances that target the same file are *not* coordinated.

Opening the file for writing can transiently fail with `IOException: ... because it is being used by another process` — most commonly when the same file is registered with `reloadOnChange: true` and its file watcher re-reads the file right after a previous write (other readers such as antivirus or backup agents can cause the same thing). Updates therefore retry a few times with a short backoff (~200 ms total) before surfacing the exception. A file that stays locked for longer than that window will still fail.

> [!NOTE]
> Writable options already push the updated value into the options cache and reload configuration after each write, so the `reloadOnChange` watcher reload is largely redundant for the writable file. If you do not need external edits to that file to be picked up at runtime, registering it with `reloadOnChange: false` removes the watcher entirely and avoids the transient sharing violations described above.

### System Abstractions

#### Use system abstractions
This package contains lightweight abstractions for system services which make business code easier to test.

Inject `IDateTime`:
```csharp
using Superdev.AspNetCore.Services;

public class TokenService
{
    private readonly IDateTime dateTime;

    public TokenService(IDateTime dateTime)
    {
        this.dateTime = dateTime;
    }

    public DateTime GetExpirationUtc()
    {
        return this.dateTime.UtcNow.AddHours(1);
    }
}
```

Inject `IFileSystem`:
```csharp
using Superdev.AspNetCore.Services;

public class DocumentService
{
    private readonly IFileSystem fileSystem;

    public DocumentService(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public Task<string> ReadAsync(string path)
    {
        return this.fileSystem.ReadAllTextAsync(path);
    }
}
```

### Exception Handling

#### Use problem details exception handling
`ProblemDetailsExceptionHandler` converts unhandled exceptions into RFC-style problem details responses.

Register it in `Program.cs`:
```csharp
using Superdev.AspNetCore.ExceptionHandling;

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
```

If you also want MVC/ObjectResult responses with status codes `>= 400` to be normalized to `ProblemDetails`, add `ProblemDetailsResultFilter`:
```csharp
using Superdev.AspNetCore.ExceptionHandling;

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ProblemDetailsResultFilter>();
});
```

## Design Goals
- Keep dependencies minimal and explicit.
- Prefer framework-native ASP.NET Core primitives over large abstraction layers.
- Move only code that is broadly reusable across multiple projects.
- Keep application-specific controllers, DTOs, mappings, secrets and business rules outside this package.

## Contribution
Contributors welcome! If you find a bug or you want to propose a new feature, feel free to do so by opening a new issue on github.com.

## Links
- https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api
- https://github.com/dotnet/aspnet-api-versioning
