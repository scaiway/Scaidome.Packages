# Scaidome.Json

The JSON rules for typed documents: settings stored as JSON beside a stored name that says which settings kind the JSON
holds. The library has two parts, one codec that reads and writes the documents, and the registry of stored names.

Depends on `Microsoft.Extensions.DependencyInjection.Abstractions`. Namespace: `Scaidome.Json` (the `Core` and `Registry`
folders only sort files).

## Registration

```csharp
services.AddScaiJson();
```

This adds the registry as `ISettingsKindRegistry`, and one codec that resolves as `JsonDocumentCodec`,
`IJsonDocumentReader` and `IJsonDocumentWriter`, all the same instance. A caller without a container uses
`JsonCodecFactory.Shared`.

## The codec

`JsonDocumentCodec` both reads and writes, so the two directions can't drift apart. Every reader and writer of a typed
document uses the same rules; if two writers used different rules, the same field would be spelled two ways.

`JsonCodecFactory` is the one place that chooses those rules:

- Field names are camel case, the same names the API uses.
- Fields are read back without regard to case.
- Enumerated settings stay numbers.
- Every other option stays at the serializer's default.

`JsonCodecBuilder` assembles a codec from individual options. It exists for the factory and for tests; production code
takes the codec the factory builds.

- **Writing** uses the settings object's own runtime type, never a more general declared type, so every field of that
  type is kept. The document carries no kind field: the stored name kept beside it decides the kind.
- **Reading** throws `JsonDocumentException` in these cases: the document is null or the JSON literal `null`, it isn't
  valid JSON for the kind, or the settings constructor refuses a value.

## The registry

`ISettingsKindRegistry` maps each stored name to a settings type and back.

- Stored names are unique without regard to case, and a type can be registered under only one name. Registering either
  twice throws, which stops the host from starting.
- Registrations happen while the host starts; reads happen on every request. The registry locks its writes and copies
  on write, so readers never take a lock.
- `GetKind` on an unknown name throws `JsonDocumentException` and lists the names that are registered.
- `ListRegistrations` returns every registration in the order it was registered.

To read a document under its stored name:

```csharp
var settings = reader.ReadStored<SourceSettings>(registry, storedName, json);
```

`ReadStored` throws `JsonDocumentException` for an unregistered name, for a document that isn't valid for the kind, and
when the name's kind is not the type asked for.

## Errors

`JsonDocumentException` derives from `JsonException`. It names the kind (or the stored name) that was being read and
carries the document, cut to its first 500 characters so that a large document can't flood a log.
