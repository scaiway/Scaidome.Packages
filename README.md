# Scaidome NuGet packages

Monorepo for the Scaidome .NET libraries published to [nuget.org](https://www.nuget.org).

| Package | Description |
|---|---|
| [`Scaidome.Abstractions`](src/Scaidome.Abstractions) | Shared types (`DataValue`, `StatusCodes`, messages) that don't depend on the OPC UA SDK |
| [`Scaidome.OpcUa.Client`](src/Scaidome.OpcUa.Client) | OPC UA client wrapping the OPC Foundation SDK |

## Build

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet build Scaidome.slnx
dotnet pack Scaidome.slnx -o artifacts
```

## Releasing a package

Each package is versioned independently by the `<Version>` in its csproj.

1. Bump `<Version>` in `src/<PackageId>/<PackageId>.csproj` and merge to `main`.
2. Push a tag named `<PackageId>/v<Version>`:

   ```sh
   git tag Scaidome.Abstractions/v1.0.0
   git push origin Scaidome.Abstractions/v1.0.0
   ```

The [Publish](.github/workflows/publish.yml) workflow checks that the tag matches the csproj version, packs that one project and pushes it (with symbols) to nuget.org using [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). There are no long-lived API keys.

`Scaidome.OpcUa.Client` depends on the `Scaidome.Abstractions` version in its csproj, so release Abstractions first when both change.

### One-time setup

1. On nuget.org, open **Account > Trusted Publishing** and add a policy:
   - Repository owner: your GitHub user or organization
   - Repository: this repository's name
   - Workflow file: `publish.yml`
   - Environment: `nuget`
2. In the GitHub repository, create an environment named `nuget` (**Settings > Environments**). Add a secret `NUGET_USER` containing your nuget.org **username** (the profile name, not your email).

## License

[MIT](LICENSE)
