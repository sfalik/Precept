// Targeted aliases so ported v1 tests compile without importing the whole Precept.Language
// namespace (which would conflict with OmniSharp's Diagnostic type).
global using TokenCategory = Precept.Language.TokenCategory;
global using TokenMeta = Precept.Language.TokenMeta;
