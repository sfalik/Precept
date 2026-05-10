# Kramer Slice 0 complete

- `TextDocumentSyncHandler` constructor injection uses `ILanguageServerFacade` successfully when the handler is registered via `WithHandler<TextDocumentSyncHandler>()`.
- OmniSharp 0.19.9 `TextDocumentSyncHandlerBase.CreateRegistrationOptions(...)` takes `TextSynchronizationCapability`, not the `SynchronizationCapability` spelling from the draft sketch.
- The reusable in-process harness uses `OmniSharp.Extensions.LanguageServer.Server.LanguageServer.PreInit(...)` on the server side and `OmniSharp.Extensions.LanguageServer.Client.LanguageClient.PreInit(...)` on the client side.
- The test project needs the separate `OmniSharp.Extensions.LanguageClient` package for `LanguageClient`; the server package alone is not enough.
- A temporary `LegacyHandlerCompat` bridge restores `Handle(...)` calls over the legacy stub surface so the test project compiles until Slice 0b removes the shim/test layer.
