// Grammar generator: reads catalog metadata from Tokens.All and emits precept.tmLanguage.json.
// Run with: dotnet run [--output path/to/precept.tmLanguage.json]
// Without --output, writes to stdout.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Precept.Language;

string outputPath = args.Length == 2 && args[0] == "--output"
    ? args[1]
    : string.Empty;

var grammar = BuildGrammar();

var json = grammar.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

if (string.IsNullOrEmpty(outputPath))
{
    Console.WriteLine(json);
}
else
{
    File.WriteAllText(outputPath, json);
    Console.Error.WriteLine($"Wrote grammar to {outputPath}");
}

// ── Grammar builder ───────────────────────────────────────────────────────

static JsonObject BuildGrammar()
{
    var repository = new JsonObject();

    // ── Catalog-derived keyword groups ────────────────────────────────────
    // Group tokens by TextMateScope and emit one alternation pattern per scope group.
    // Only emit tokens whose Text is non-null (keyword/operator tokens).

    var keywordsByScope = Tokens.All
        .Where(m => m.Text is not null && m.VisualCategory.HasValue)
        .GroupBy(m => SemanticTokenTypes.GetMeta(m.VisualCategory!.Value).TextMateScope)
        .ToDictionary(g => g.Key, g => g.ToList());

    // Separate keyword tokens (word chars only) from symbol operator tokens
    var keywordScopes = keywordsByScope
        .Where(kvp => kvp.Value.All(m => m.Text!.All(c => char.IsLetterOrDigit(c) || c == '_')))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    var operatorScopes = keywordsByScope
        .Where(kvp => kvp.Value.Any(m => !m.Text!.All(c => char.IsLetterOrDigit(c) || c == '_')))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    // Keyword patterns use word boundaries (\b); longer keywords before shorter to prevent prefix matches
    foreach (var (scope, tokens) in keywordScopes.OrderBy(kv => kv.Key))
    {
        var key = ScopeToRepositoryKey(scope);
        var alternation = string.Join("|", tokens
            .Select(m => Regex.Escape(m.Text!))
            .OrderByDescending(t => t.Length)
            .ThenBy(t => t));
        repository[key] = new JsonObject
        {
            ["name"] = scope,
            ["match"] = $"\\b({alternation})\\b"
        };
    }

    // Operator patterns — no word boundaries; longer tokens must come first
    foreach (var (scope, tokens) in operatorScopes.OrderBy(kv => kv.Key))
    {
        var key = ScopeToRepositoryKey(scope);
        var sorted = tokens.OrderByDescending(m => m.Text!.Length).ThenBy(m => m.Text);
        var alternation = string.Join("|", sorted.Select(m => Regex.Escape(m.Text!)));
        repository[key] = new JsonObject
        {
            ["name"] = scope,
            ["match"] = $"({alternation})"
        };
    }

    // ── Structural patterns ────────────────────────────────────────────────
    AddStructuralPatterns(repository, keywordsByScope);

    // ── Top-level grammar patterns list ────────────────────────────────────
    var patterns = BuildTopLevelPatterns();

    return new JsonObject
    {
        ["$schema"] = "https://raw.githubusercontent.com/martinring/tmlanguage/master/tmlanguage.json",
        ["name"] = "Precept",
        ["scopeName"] = "source.precept",
        ["patterns"] = patterns,
        ["repository"] = repository
    };
}

static void AddStructuralPatterns(JsonObject repo, Dictionary<string, List<TokenMeta>> keywordsByScope)
{
    // ── Derive catalog-driven alternation strings for use in structural patterns ──

    // All type keywords from catalog (scalar + temporal + business + collection)
    var typeAlt = string.Join("|", keywordsByScope.TryGetValue("entity.name.type.precept", out var typeTokens)
        ? typeTokens
            .Select(m => Regex.Escape(m.Text!))
            .OrderByDescending(t => t.Length)
            .ThenBy(t => t)
        : []);

    // Collection type keywords only (subset of typeKeywords)
    var collectionTypes = new HashSet<string> { "set", "queue", "stack", "bag", "list", "log", "lookup" };
    var collTypeAlt = string.Join("|", collectionTypes
        .Select(Regex.Escape)
        .OrderByDescending(t => t.Length)
        .ThenBy(t => t));

    // State modifier keywords from catalog
    var stateModifierAlt = string.Join("|", Tokens.All
        .Where(m => m.Categories.Contains(TokenCategory.StateModifier) && m.Text is not null)
        .Select(m => Regex.Escape(m.Text!))
        .OrderByDescending(t => t.Length)
        .ThenBy(t => t));

    // Function names from Functions catalog (excluding CI variants — handled separately)
    var funcNames = Functions.All
        .Where(f => f.CIVariantOf is null)
        .Select(f => Regex.Escape(f.Name))
        .OrderByDescending(n => n.Length)
        .ThenBy(n => n)
        .ToList();
    var ciVariantNames = Functions.All
        .Where(f => f.CIVariantOf is not null)
        .Select(f => Regex.Escape(f.Name))
        .OrderByDescending(n => n.Length)
        .ThenBy(n => n)
        .ToList();

    // ── comment ───────────────────────────────────────────────────────────
    repo["comment"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject { ["name"] = "comment.line.number-sign.precept", ["match"] = "#.*$" }
        }
    };

    // ── messageStrings — GOLD highlight for catalog-declared message payloads ──
    // MUST precede generic strings pattern to prevent gold strings from being consumed as regular strings.
    // Per Frank spec §2.2 and visual system: string.quoted.double.message.precept → #FBBF24 gold.
    var messageStringPatterns = new JsonArray();

    foreach (var token in Tokens.All
        .Where(m => m.IsMessagePosition && m.Text is not null && m.VisualCategory.HasValue)
        .OrderBy(m => m.Text, StringComparer.Ordinal))
    {
        messageStringPatterns.Add(CreateTokenMessageStringPattern(token));
    }

    foreach (var function in Functions.All
        .Where(f => f.IsMessagePosition)
        .OrderBy(f => f.Name, StringComparer.Ordinal))
    {
        messageStringPatterns.Add(CreateFunctionMessageStringPattern(function));
    }

    repo["messageStrings"] = new JsonObject
    {
        ["patterns"] = messageStringPatterns
    };

    // ── ruleDesugaringModifiers — catalog-derived rule-desugaring subset ──
    // MUST precede generic constraint keyword patterns so catalog-flagged modifiers keep their
    // dedicated first-match hook without changing the shared grammar-keyword color lane.
    var ruleDesugaringModifierAlt = string.Join("|", Modifiers.All
        .Where(m => m.DesugarsToRule && m.Token.Text is not null)
        .Select(m => Regex.Escape(m.Token.Text!))
        .Distinct(StringComparer.Ordinal)
        .OrderByDescending(t => t.Length)
        .ThenBy(t => t, StringComparer.Ordinal));

    var ruleDesugaringModifierPatterns = new JsonArray();
    if (!string.IsNullOrEmpty(ruleDesugaringModifierAlt))
    {
        ruleDesugaringModifierPatterns.Add(new JsonObject
        {
            ["name"] = "keyword.other.grammar.precept",
            ["match"] = $"\\b({ruleDesugaringModifierAlt})\\b"
        });
    }

    repo["ruleDesugaringModifiers"] = new JsonObject
    {
        ["patterns"] = ruleDesugaringModifierPatterns
    };

    // ── strings ──────────────────────────────────────────────────────────
    repo["strings"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "string.quoted.double.precept",
                ["begin"] = "\"",
                ["end"] = "\"",
                ["patterns"] = new JsonArray
                {
                    new JsonObject { ["name"] = "constant.character.escape.precept", ["match"] = "\\\\." }
                }
            }
        }
    };

    // ── typedConstants ────────────────────────────────────────────────────
    repo["typedConstants"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject { ["name"] = "string.quoted.single.precept", ["begin"] = "'", ["end"] = "'" }
        }
    };

    // ── numbers ───────────────────────────────────────────────────────────
    repo["numbers"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject { ["name"] = "constant.numeric.precept", ["match"] = "\\b\\d+(?:\\.\\d+)?\\b" }
        }
    };

    // ── punctuation ───────────────────────────────────────────────────────
    repo["punctuation"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject { ["name"] = "punctuation.precept", ["match"] = "[()\\[\\].,]" }
        }
    };

    // ── preceptHeader ─────────────────────────────────────────────────────
    repo["preceptHeader"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.declaration.precept.precept",
                ["match"] = "^(\\s*)(precept)(\\s+)([A-Za-z_][A-Za-z0-9_]*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["4"] = new JsonObject { ["name"] = "entity.name.type.precept.precept" }
                }
            }
        }
    };

    // ── stateDeclaration — all 7 state modifiers from catalog ────────────
    repo["stateDeclaration"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.declaration.state.precept",
                ["match"] = "^(\\s*)(state)(\\s+)(.*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            // initial is a declaration keyword (keyword.declaration.precept)
                            new JsonObject { ["name"] = "keyword.declaration.precept", ["match"] = "\\binitial\\b" },
                            // terminal, required, irreversible, success, warning, error from catalog
                            string.IsNullOrEmpty(stateModifierAlt) ? null! : new JsonObject
                            {
                                ["name"] = "storage.modifier.state.precept",
                                ["match"] = $"\\b({stateModifierAlt})\\b"
                            },
                            new JsonObject { ["name"] = "entity.name.type.state.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    }
                }
            }
        }
    };

    // ── eventDeclaration — parenthesized args (current syntax) ───────────
    // Replaces stale eventWithArgsDeclaration that used retired `with` syntax.
    repo["eventDeclaration"] = new JsonObject
    {
        ["comment"] = "event Name[, Name, ...] or event Name(Arg as type, ...)",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.declaration.event.precept",
                ["match"] = "^(\\s*)(event)(\\s+)((?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)(\\s*\\(.*)?",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "entity.name.function.event.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    },
                    ["5"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            // argument name before 'as'
                            new JsonObject
                            {
                                ["match"] = "\\b([A-Za-z_][A-Za-z0-9_]*)(?=\\s+as\\b)",
                                ["captures"] = new JsonObject { ["1"] = new JsonObject { ["name"] = "variable.parameter.precept" } }
                            },
                            new JsonObject { ["include"] = "#semanticKeywords" },
                            new JsonObject { ["include"] = "#grammarKeywords" },
                            new JsonObject { ["include"] = "#ruleDesugaringModifiers" },
                            new JsonObject { ["include"] = "#grammarKeywords" },
                            new JsonObject { ["include"] = "#typeKeywords" },
                            new JsonObject { ["include"] = "#numbers" },
                            new JsonObject { ["include"] = "#strings" },
                            new JsonObject { ["include"] = "#booleanLiterals" },
                            new JsonObject { ["name"] = "punctuation.precept", ["match"] = "[()]" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," },
                            new JsonObject { ["include"] = "#identifierReference" }
                        }
                    }
                }
            }
        }
    };

    // ── fieldCollectionDeclaration — all collection types + full inner type list ──
    var innerTypeAlt = string.Join("|", new[] { "string", "number", "integer", "decimal", "boolean" }
        .Select(Regex.Escape)
        .OrderByDescending(t => t.Length)
        .ThenBy(t => t));

    repo["fieldCollectionDeclaration"] = new JsonObject
    {
        ["comment"] = "field Name[, ...] as set|queue|stack|bag|list|log|lookup of [~]type",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.field-declaration.precept",
                ["match"] = $"^(\\s*)(field)(\\s+)((?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)(\\s+)(as)(\\s+)({collTypeAlt})(\\s+)(of)(\\s+)(~?(?:{innerTypeAlt}))(.*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "variable.other.field.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["8"] = new JsonObject { ["name"] = "storage.type.precept" },
                    ["10"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["12"] = new JsonObject { ["name"] = "storage.type.precept" },
                    ["13"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["include"] = "#ruleDesugaringModifiers" },
                            new JsonObject { ["name"] = "keyword.declaration.precept", ["match"] = "\\bdefault\\b" },
                            new JsonObject { ["include"] = "#grammarKeywords" },
                            new JsonObject { ["include"] = "#semanticKeywords" },
                            new JsonObject { ["include"] = "#grammarKeywords" },
                            new JsonObject { ["include"] = "#numbers" },
                            new JsonObject { ["include"] = "#strings" },
                            new JsonObject { ["include"] = "#identifierReference" }
                        }
                    }
                }
            }
        }
    };

    // ── fieldScalarDeclaration — all scalar/temporal/business types from catalog ──
    repo["fieldScalarDeclaration"] = new JsonObject
    {
        ["comment"] = "field Name[, ...] as <type> [modifiers...] [<- expr]",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.field-declaration.precept",
                ["match"] = $"^(\\s*)(field)(\\s+)((?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)(\\s+)(as)(\\s+)({typeAlt})(.*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "variable.other.field.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["8"] = new JsonObject { ["name"] = "storage.type.precept" },
                    ["9"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["include"] = "#symbolOperators" },
                            new JsonObject { ["include"] = "#ruleDesugaringModifiers" },
                            new JsonObject { ["name"] = "keyword.declaration.precept", ["match"] = "\\bdefault\\b" },
                            new JsonObject { ["include"] = "#grammarKeywords" },
                            new JsonObject { ["include"] = "#semanticKeywords" },
                            new JsonObject { ["include"] = "#grammarKeywords" },
                            new JsonObject { ["include"] = "#typeKeywords" },
                            new JsonObject { ["include"] = "#numbers" },
                            new JsonObject { ["include"] = "#strings" },
                            new JsonObject { ["include"] = "#typedConstants" },
                            new JsonObject { ["include"] = "#booleanLiterals" },
                            new JsonObject { ["name"] = "punctuation.precept", ["match"] = "[()\\[\\]]" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," },
                            new JsonObject { ["include"] = "#identifierReference" }
                        }
                    }
                }
            }
        }
    };

    // ── ruleDeclaration — `rule` keyword at line start ───────────────────
    repo["ruleDeclaration"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.rule.precept",
                ["match"] = "^(\\s*)(rule)\\b",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.declaration.precept" }
                }
            }
        }
    };

    // ── stateAction — `to/from State ->` ─────────────────────────────────
    // Must precede stateEnsure (both start with to/from); disambiguated by `->`.
    repo["stateAction"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.action.state.precept",
                ["match"] = "^(\\s*)(to|from)(\\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\\s+)(->)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "keyword.other.quantifier.precept", ["match"] = "\\bany\\b" },
                            new JsonObject { ["name"] = "entity.name.type.state.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.operator.arrow.precept" }
                }
            }
        }
    };

    // ── stateEnsure — `in/to/from State ensure` ───────────────────────────
    repo["stateEnsure"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.ensure.state.precept",
                ["match"] = "^(\\s*)(in|to|from)(\\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\\s+)(ensure)\\b",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "keyword.other.quantifier.precept", ["match"] = "\\bany\\b" },
                            new JsonObject { ["name"] = "entity.name.type.state.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.other.assertion.precept" }
                }
            }
        }
    };

    // ── eventHandler — `on Event ->` ──────────────────────────────────────
    // Must precede eventEnsure (both start with `on`); disambiguated by `->`.
    repo["eventHandler"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.handler.event.precept",
                ["match"] = "^(\\s*)(on)(\\s+)([A-Za-z_][A-Za-z0-9_]*)(\\s+)(->)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject { ["name"] = "entity.name.function.event.precept" },
                    ["6"] = new JsonObject { ["name"] = "keyword.operator.arrow.precept" }
                }
            }
        }
    };

    // ── eventEnsure — `on Event ensure` ───────────────────────────────────
    repo["eventEnsure"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.ensure.event.precept",
                ["match"] = "^(\\s*)(on)(\\s+)([A-Za-z_][A-Za-z0-9_]*)(\\s+)(ensure)\\b",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject { ["name"] = "entity.name.function.event.precept" },
                    ["6"] = new JsonObject { ["name"] = "keyword.other.assertion.precept" }
                }
            }
        }
    };

    // ── accessMode — `in State modify Field editable|readonly` ───────────
    repo["accessMode"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.access-mode.precept",
                ["match"] = "^(\\s*)(in)(\\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\\s+)(modify)(\\s+)((?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*|all)(\\s+)(editable|readonly)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "keyword.other.quantifier.precept", ["match"] = "\\bany\\b" },
                            new JsonObject { ["name"] = "entity.name.type.state.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.other.access-mode.precept" },
                    ["8"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "keyword.other.quantifier.precept", ["match"] = "\\ball\\b" },
                            new JsonObject { ["name"] = "variable.other.field.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    },
                    ["10"] = new JsonObject { ["name"] = "keyword.other.connective.precept" }
                }
            }
        }
    };

    // ── omitDeclaration — `in State omit Field` ───────────────────────────
    repo["omitDeclaration"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.omit.precept",
                ["match"] = "^(\\s*)(in)(\\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\\s+)(omit)(\\s+)([A-Za-z_][A-Za-z0-9_]*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "keyword.other.quantifier.precept", ["match"] = "\\bany\\b" },
                            new JsonObject { ["name"] = "entity.name.type.state.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.other.access-mode.precept" },
                    ["8"] = new JsonObject { ["name"] = "variable.other.field.precept" }
                }
            }
        }
    };

    // ── fromOnHeader — `from State on Event` ─────────────────────────────
    repo["fromOnHeader"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.transition.header.precept",
                ["match"] = "^(\\s*)(from)(\\s+)(any|[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*)(\\s+)(on)(\\s+)([A-Za-z_][A-Za-z0-9_]*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "keyword.other.quantifier.precept", ["match"] = "\\bany\\b" },
                            new JsonObject { ["name"] = "entity.name.type.state.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["8"] = new JsonObject { ["name"] = "entity.name.function.event.precept" }
                }
            }
        }
    };

    // ── noTransition — `no transition` compound keyword ───────────────────
    // Must precede transitionTarget to prevent `no` from consuming `transition`.
    // Both words are non-bold connective — only standalone `transition` is bold.
    repo["noTransition"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["match"] = "\\b(no)(\\s+)(transition)\\b",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "keyword.other.connective.precept" },
                    ["3"] = new JsonObject { ["name"] = "keyword.other.connective.precept" }
                }
            }
        }
    };

    // ── isSetOperator — `is set` / `is not set` null-check ────────────────
    // Must precede actionKeywords to prevent `set` from taking the bold action color.
    // `set` here is a membership operator suffix, not an action verb.
    // `is not set` listed first so it consumes before `is set` can partial-match.
    repo["isSetOperator"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["match"] = "\\b(is)(\\s+)(not)(\\s+)(set)\\b",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "keyword.operator.membership.precept" },
                    ["3"] = new JsonObject { ["name"] = "keyword.operator.membership.precept" },
                    ["5"] = new JsonObject { ["name"] = "keyword.operator.membership.precept" }
                }
            },
            new JsonObject
            {
                ["match"] = "\\b(is)(\\s+)(set)\\b",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "keyword.operator.membership.precept" },
                    ["3"] = new JsonObject { ["name"] = "keyword.operator.membership.precept" }
                }
            }
        }
    };

    // ── transitionTarget — `transition StateName` ─────────────────────────
    repo["transitionTarget"] = new JsonObject
    {
        ["comment"] = "transition StateName",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.transition.target.precept",
                ["match"] = "\\b(transition)(\\s+)([A-Za-z_][A-Za-z0-9_]*)",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "keyword.other.outcome.precept" },
                    ["3"] = new JsonObject { ["name"] = "entity.name.type.state.precept" }
                }
            }
        }
    };

    // ── functionCalls — built-in function names from Functions catalog ────
    // Derives name list from Functions.All; CI variants (~startsWith, ~endsWith) get separate pattern.
    // Entries without IsMessagePosition still participate here; they simply add no messageStrings pattern above.
    if (funcNames.Count > 0)
    {
        var funcAlt = string.Join("|", funcNames);
        repo["functionCalls"] = new JsonObject
        {
            ["comment"] = "Built-in function name calls, derived from Functions catalog",
            ["patterns"] = new JsonArray
            {
                new JsonObject
                {
                    ["match"] = $"\\b({funcAlt})(\\s*\\()",
                    ["captures"] = new JsonObject
                    {
                        ["1"] = new JsonObject { ["name"] = "support.function.precept" },
                        ["2"] = new JsonObject { ["name"] = "punctuation.precept" }
                    }
                }
            }
        };
    }

    if (ciVariantNames.Count > 0)
    {
        var ciAlt = string.Join("|", ciVariantNames.Select(n => Regex.Escape(n)));
        repo["functionCallsCI"] = new JsonObject
        {
            ["comment"] = "CI variant function calls (~startsWith, ~endsWith), derived from Functions catalog",
            ["patterns"] = new JsonArray
            {
                new JsonObject
                {
                    ["match"] = $"({ciAlt})(\\s*\\()",
                    ["captures"] = new JsonObject
                    {
                        ["1"] = new JsonObject { ["name"] = "support.function.precept" },
                        ["2"] = new JsonObject { ["name"] = "punctuation.precept" }
                    }
                }
            }
        };
    }

    // ── collectionMemberAccess — Collection.count, Queue.peek, etc. ───────
    // Must precede eventArgReference to prevent Collection.count → event scope.
    repo["collectionMemberAccess"] = new JsonObject
    {
        ["comment"] = "Collection member access (count, countof, min, max, peek, peekby)",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.collection-member.precept",
                ["match"] = "\\b([A-Za-z_][A-Za-z0-9_]*)(\\.)(count|countof|min|max|peek|peekby)\\b",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "variable.other.field.precept" },
                    ["2"] = new JsonObject { ["name"] = "punctuation.accessor.precept" },
                    ["3"] = new JsonObject { ["name"] = "variable.other.property.precept" }
                }
            }
        }
    };

    // ── eventArgReference — Event.arg dot access ─────────────────────────
    repo["eventArgReference"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.event-arg-ref.precept",
                ["match"] = "\\b([A-Za-z_][A-Za-z0-9_]*)(\\.)([A-Za-z_][A-Za-z0-9_]*)",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "entity.name.function.event.precept" },
                    ["2"] = new JsonObject { ["name"] = "punctuation.accessor.precept" },
                    ["3"] = new JsonObject { ["name"] = "variable.parameter.property.precept" }
                }
            }
        }
    };

    // ── identifierReference — catch-all (MUST be last) ────────────────────
    repo["identifierReference"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["comment"] = "catch-all identifier reference",
                ["name"] = "variable.other.precept",
                ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b"
            }
        }
    };
}

static JsonObject CreateTokenMessageStringPattern(TokenMeta token)
{
    var keyword = token.Text ?? throw new InvalidOperationException("Message-position tokens must declare text.");
    var scope = token.VisualCategory.HasValue 
        ? SemanticTokenTypes.GetMeta(token.VisualCategory.Value).TextMateScope
        : throw new InvalidOperationException("Message-position tokens must have a VisualCategory.");

    return new JsonObject
    {
        ["name"] = $"meta.message.{MessagePatternNameSegment(keyword)}.precept",
        ["match"] = $"\\b({Regex.Escape(keyword)})(\\s+)(\"(?:\\\\.|[^\"\\\\])*\")",
        ["captures"] = new JsonObject
        {
            ["1"] = new JsonObject { ["name"] = scope },
            ["3"] = new JsonObject { ["name"] = "string.quoted.double.message.precept" }
        }
    };
}

static JsonObject CreateFunctionMessageStringPattern(FunctionMeta function)
{
    var functionName = function.Name;
    var prefix = functionName.StartsWith("~", StringComparison.Ordinal)
        ? $"({Regex.Escape(functionName)})"
        : $"\\b({Regex.Escape(functionName)})";

    return new JsonObject
    {
        ["name"] = $"meta.message.{MessagePatternNameSegment(functionName)}.precept",
        ["match"] = $"{prefix}(\\s*\\()(?:[^)]*?,\\s*)*(\"(?:\\\\.|[^\"\\\\])*\")(\\s*\\))",
        ["captures"] = new JsonObject
        {
            ["1"] = new JsonObject { ["name"] = "support.function.precept" },
            ["2"] = new JsonObject { ["name"] = "punctuation.precept" },
            ["3"] = new JsonObject { ["name"] = "string.quoted.double.message.precept" },
            ["4"] = new JsonObject { ["name"] = "punctuation.precept" }
        }
    };
}

static string MessagePatternNameSegment(string name) =>
    Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

static JsonArray BuildTopLevelPatterns()
{
    // Per Frank spec §3 — ordered from most-specific to least-specific.
    string[] includeOrder =
    [
        "#comment",
        "#messageStrings",
        "#strings",
        "#typedConstants",
        "#preceptHeader",
        "#stateDeclaration",
        "#eventDeclaration",
        "#fieldCollectionDeclaration",
        "#fieldScalarDeclaration",
        "#ruleDeclaration",
        "#stateAction",
        "#stateEnsure",
        "#eventHandler",
        "#eventEnsure",
        "#accessMode",
        "#omitDeclaration",
        "#fromOnHeader",
        "#noTransition",
        "#transitionTarget",
        "#functionCalls",
        "#functionCallsCI",
        "#collectionMemberAccess",
        "#eventArgReference",
        "#symbolOperators",
        "#isSetOperator",
        "#ruleDesugaringModifiers",
        "#typeKeywords",
        "#semanticKeywords",
        "#grammarKeywords",
        "#booleanLiterals",
        "#numbers",
        "#punctuation",
        "#identifierReference"
    ];

    var arr = new JsonArray();
    foreach (var inc in includeOrder)
        arr.Add(new JsonObject { ["include"] = inc });
    return arr;
}

static string ScopeToRepositoryKey(string scope) => scope switch
{
    "keyword.other.semantic.precept"  => "semanticKeywords",
    "keyword.other.grammar.precept"   => "grammarKeywords",
    "keyword.operator.precept"        => "symbolOperators",
    "entity.name.type.precept"        => "typeKeywords",
    "constant.language.precept"       => "booleanLiterals",
    "entity.name.precept"             => "nameTokens",
    "entity.name.type.precept.precept" => "nameTokens",
    "comment.line.precept"            => "commentTokens",
    _ => scope.Replace(".precept", "").Replace(".", "_") + "Keywords"
};
