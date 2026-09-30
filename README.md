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

## Tests

Test projects live in [`tests/`](tests), named `<PackageId>.Tests`. `Scaidome.OpcUa.Client` has none yet. Run them all with `dotnet test Scaidome.Packages.slnx`. CI runs them on every push and pull request, and Publish runs them before packing.

## Releasing

All packages share one version, set by `<Version>` in [`Directory.Build.props`](Directory.Build.props), and are released together.

1. Bump `<Version>` in `Directory.Build.props` and merge to `main`.
2. Push a tag named `v<Version>`:

   ```sh
   git tag v1.1.0
   git push origin v1.1.0
   ```

The [Publish](.github/workflows/publish.yml) workflow checks that the tag matches the version, packs every package and pushes them (with symbols) to nuget.org using [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). There are no long-lived API keys. Packages that reference each other depend on the same version, so they are always published together.

## License

[MIT](LICENSE)
