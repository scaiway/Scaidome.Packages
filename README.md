# Scaidome NuGet packages

Monorepo for the Scaidome .NET libraries published to [nuget.org](https://www.nuget.org).

| Package | Description |
|---|---|
| [`Scaidome.Abstractions`](src/Scaidome.Abstractions) | Shared types: values and conversions, declared types, `StatusCodes`, `DataValue`, name rules and a metrics abstraction. No dependency on the OPC UA SDK |
| [`Scaidome.Channels`](src/Scaidome.Channels) | Action queues with many producers and one draining loop, publishing their counts as metrics |
| [`Scaidome.Dapper`](src/Scaidome.Dapper) | Dapper type handlers for identities and times stored as text in SQLite |
| [`Scaidome.Json`](src/Scaidome.Json) | Codec and settings-kind registry for typed JSON documents |
| [`Scaidome.Logging`](src/Scaidome.Logging) | Rolling-file logging provider for `Microsoft.Extensions.Logging` |
| [`Scaidome.OpcUa.Client`](src/Scaidome.OpcUa.Client) | OPC UA client wrapping the OPC Foundation SDK |

## Releasing a package

Each package is versioned independently by the `<Version>` in its csproj.

1. Bump `<Version>` in `src/<PackageId>/<PackageId>.csproj` and merge to `main`.
2. Push a tag named `<PackageId>/v<Version>`:

   ```sh
   git tag Scaidome.Abstractions/v1.0.0
   git push origin Scaidome.Abstractions/v1.0.0
   ```

The [Publish](.github/workflows/publish.yml) workflow checks that the tag matches the csproj version, packs that one project and pushes it (with symbols) to nuget.org using [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). There are no long-lived API keys.

`Scaidome.OpcUa.Client` and `Scaidome.Channels` depend on the `Scaidome.Abstractions` version in their csproj, so release Abstractions first when they change together.

## License

[MIT](LICENSE)
