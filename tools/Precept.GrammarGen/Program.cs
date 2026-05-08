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
        .Where(m => m.Text is not null && m.TextMateScope is not null)
        .GroupBy(m => m.TextMateScope!)
        .ToDictionary(g => g.Key, g => g.ToList());

    // Separate keyword tokens (word chars only) from symbol operator tokens
    var keywordScopes = keywordsByScope
        .Where(kvp => kvp.Value.All(m => m.Text!.All(c => char.IsLetterOrDigit(c) || c == '_')))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    var operatorScopes = keywordsByScope
        .Where(kvp => kvp.Value.Any(m => !m.Text!.All(c => char.IsLetterOrDigit(c) || c == '_')))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    // Keyword patterns use word boundaries (\b); operators use literal escape
    foreach (var (scope, tokens) in keywordScopes.OrderBy(kv => kv.Key))
    {
        var key = ScopeToRepositoryKey(scope);
        var alternation = string.Join("|", tokens.Select(m => Regex.Escape(m.Text!)).OrderBy(t => t));
        repository[key] = new JsonObject
        {
            ["name"] = scope,
            ["match"] = $"\\b({alternation})\\b"
        };
    }

    // Operator patterns — no word boundaries; longer tokens must come first
    // Group operator tokens by scope into their respective repo entries.
    // Different operator scopes get separate entries; same scope tokens are merged.
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

    // ── Structural patterns (grammar-level, not catalog-driven) ────────────
    // These patterns cover constructs that require context beyond a single token.

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
    // Comment: # to end of line
    repo["comment"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "comment.line.number-sign.precept",
                ["match"] = "#.*$"
            }
        }
    };

    // Message strings — GOLD highlighting for "because "..." and "reject "..." (MUST precede strings)
    repo["messageStrings"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.message.because.precept",
                ["match"] = "\\b(because)(\\s+)(\"(?:\\\\.|[^\"\\\\])*\")",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "keyword.declaration.precept" },
                    ["3"] = new JsonObject { ["name"] = "string.quoted.double.message.precept" }
                }
            },
            new JsonObject
            {
                ["name"] = "meta.message.reject.precept",
                ["match"] = "\\b(reject)(\\s+)(\"(?:\\\\.|[^\"\\\\])*\")",
                ["captures"] = new JsonObject
                {
                    ["1"] = new JsonObject { ["name"] = "keyword.other.outcome.precept" },
                    ["3"] = new JsonObject { ["name"] = "string.quoted.double.message.precept" }
                }
            }
        }
    };

    // Double-quoted strings (message strings, default values)
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
                    new JsonObject
                    {
                        ["name"] = "constant.character.escape.precept",
                        ["match"] = "\\\\."
                    }
                }
            }
        }
    };

    // Single-quoted typed constants ('USD', 'kg', etc.)
    repo["typedConstants"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "string.quoted.single.precept",
                ["begin"] = "'",
                ["end"] = "'"
            }
        }
    };

    // Numbers (integer and decimal)
    repo["numbers"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "constant.numeric.precept",
                ["match"] = "\\b\\d+(?:\\.\\d+)?\\b"
            }
        }
    };

    // Precept declaration (header line) — highlights name as entity.name
    repo["machineDeclaration"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.declaration.precept.precept",
                ["match"] = "^(\\s*)(precept)(\\s+)([A-Za-z_][A-Za-z0-9_]*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject { ["name"] = "entity.name.precept.message.precept" }
                }
            }
        }
    };

    // State declaration — highlights state names and modifiers
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
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["name"] = "keyword.control.precept",
                                ["match"] = "\\binitial\\b"
                            },
                            new JsonObject
                            {
                                ["name"] = "entity.name.type.state.precept",
                                ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b"
                            },
                            new JsonObject
                            {
                                ["name"] = "punctuation.separator.comma.precept",
                                ["match"] = ","
                            }
                        }
                    }
                }
            }
        }
    };

    // Event declarations — with and without args
    repo["eventWithArgsDeclaration"] = new JsonObject
    {
        ["comment"] = "event Name[, Name, ...] with Arg as type, Arg2 as type",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.declaration.event.precept",
                ["match"] = "^(\\s*)(event)(\\s+)((?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)(\\s+)(with)(\\s+)(.*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "entity.name.function.event.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    },
                    ["6"] = new JsonObject { ["name"] = "keyword.other.precept" },
                    ["8"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["$ref"] = "#/repository/storage.type.precept" },
                            new JsonObject { ["include"] = "#numbers" },
                            new JsonObject { ["include"] = "#strings" },
                            new JsonObject
                            {
                                ["comment"] = "argument name before 'as'",
                                ["match"] = "\\b([A-Za-z_][A-Za-z0-9_]*)(?=\\s+as\\b)",
                                ["captures"] = new JsonObject { ["1"] = new JsonObject { ["name"] = "variable.parameter.precept" } }
                            },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    }
                }
            }
        }
    };

    repo["eventDeclaration"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.declaration.event.precept",
                ["match"] = "^(\\s*)(event)(\\s+)(.*)",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "entity.name.function.event.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    }
                }
            }
        }
    };

    // Field declarations — collection and scalar
    repo["fieldCollectionDeclaration"] = new JsonObject
    {
        ["comment"] = "field Name[, ...] as set|queue|stack|bag|list|log|lookup of type",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.field-declaration.collection.precept",
                ["match"] = "^(\\s*)(field)(\\s+)((?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)(\\s+)(as)(\\s+)(set|queue|stack|bag|list|log|lookup)(\\s+)(of)(\\s+)(string|number|integer|decimal|boolean|choice|~?string|~?number)",
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
                    ["12"] = new JsonObject { ["name"] = "storage.type.precept" }
                }
            }
        }
    };

    repo["fieldScalarDeclaration"] = new JsonObject
    {
        ["comment"] = "field Name[, ...] as string|number|integer|decimal|boolean|choice(...)",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.field-declaration.scalar.precept",
                ["match"] = "^(\\s*)(field)(\\s+)((?:[A-Za-z_][A-Za-z0-9_]*\\s*,\\s*)*[A-Za-z_][A-Za-z0-9_]*)(\\s+)(as)(\\s+)(string|number|integer|decimal|boolean|choice)(.*)",
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
                            new JsonObject { ["include"] = "#numbers" },
                            new JsonObject { ["include"] = "#strings" }
                        }
                    }
                }
            }
        }
    };

    // root-level edit declaration (stateless precepts)
    repo["rootEditDeclaration"] = new JsonObject
    {
        ["comment"] = "Root-level edit declaration (stateless precepts): edit all | edit Field1, Field2",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.declaration.edit.root.precept",
                ["match"] = "^(\\s*)(edit)(\\s+)(all|(?:[A-Za-z_][A-Za-z0-9_]*(?:\\s*,\\s*[A-Za-z_][A-Za-z0-9_]*)*))",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.other.precept" },
                    ["4"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "keyword.control.precept", ["match"] = "\\ball\\b" },
                            new JsonObject { ["name"] = "variable.other.field.precept", ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b" },
                            new JsonObject { ["name"] = "punctuation.separator.comma.precept", ["match"] = "," }
                        }
                    }
                }
            }
        }
    };

    // from/on transition header
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
                    ["4"] = new JsonObject { ["name"] = "entity.name.type.state.precept" },
                    ["6"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["8"] = new JsonObject { ["name"] = "entity.name.function.event.precept" }
                }
            }
        }
    };

    // transition StateName
    repo["transitionTarget"] = new JsonObject
    {
        ["comment"] = "transition StateName — highlights the state name after transition keyword",
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

    // assert statement on EventName assert Expr
    repo["assertStatement"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "meta.assert.precept",
                ["match"] = "^(\\s*)(on)(\\s+)([A-Za-z_][A-Za-z0-9_]*)(\\s+)(assert)\\b",
                ["captures"] = new JsonObject
                {
                    ["2"] = new JsonObject { ["name"] = "keyword.control.precept" },
                    ["4"] = new JsonObject { ["name"] = "entity.name.function.event.precept" },
                    ["6"] = new JsonObject { ["name"] = "keyword.declaration.precept" }
                }
            }
        }
    };

    // Event.arg dot access
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
                    ["3"] = new JsonObject { ["name"] = "variable.other.property.precept" }
                }
            }
        }
    };

    // Collection member access: Coll.count, Coll.min, etc.
    repo["collectionMemberAccess"] = new JsonObject
    {
        ["comment"] = "Collection.count, Collection.min, etc.",
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
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

    // Catch-all identifier
    repo["identifierReference"] = new JsonObject
    {
        ["patterns"] = new JsonArray
        {
            new JsonObject
            {
                ["comment"] = "catch-all for bare identifier references in expression positions",
                ["name"] = "variable.other.precept",
                ["match"] = "\\b[A-Za-z_][A-Za-z0-9_]*\\b"
            }
        }
    };
}

static JsonArray BuildTopLevelPatterns()
{
    // Ordering matters: more specific patterns before catch-alls
    string[] includeOrder =
    [
        "#comment",
        "#strings",
        "#typedConstants",
        "#machineDeclaration",
        "#stateDeclaration",
        "#eventWithArgsDeclaration",
        "#eventDeclaration",
        "#fieldCollectionDeclaration",
        "#fieldScalarDeclaration",
        "#rootEditDeclaration",
        "#fromOnHeader",
        "#transitionTarget",
        "#assertStatement",
        "#eventArgReference",
        "#collectionMemberAccess",
        "#keyword.operator.arrow.preceptOperators",
        "#keyword.operator.logical.preceptKeywords",
        "#keyword.operator.membership.preceptKeywords",
        "#storage.modifier.state.preceptKeywords",
        "#keyword.other.constraint.preceptKeywords",
        "#storage.type.preceptKeywords",
        "#keyword.control.preceptKeywords",
        "#keyword.declaration.preceptKeywords",
        "#keyword.other.action.preceptKeywords",
        "#keyword.other.outcome.preceptKeywords",
        "#keyword.other.access-mode.preceptKeywords",
        "#keyword.other.quantifier.preceptKeywords",
        "#keyword.other.preceptKeywords",
        "#constant.language.boolean.preceptKeywords",
        "#keyword.operator.preceptOperators",
        "#numbers",
        "#identifierReference"
    ];

    var arr = new JsonArray();
    foreach (var inc in includeOrder)
        arr.Add(new JsonObject { ["include"] = inc });
    return arr;
}

static string ScopeToRepositoryKey(string scope)
{
    // Convert scope like "keyword.control.precept" to descriptive name like "controlKeywords"
    // This provides readable repository keys that match the hand-authored grammar conventions.
    return scope switch
    {
        "keyword.declaration.precept" => "declarationKeywords",
        "keyword.control.precept" => "controlKeywords",
        "keyword.other.action.precept" => "actionKeywords",
        "keyword.other.outcome.precept" => "outcomeKeywords",
        "keyword.other.access-mode.precept" => "accessModeKeywords",
        "keyword.other.quantifier.precept" => "quantifierKeywords",
        "keyword.other.constraint.precept" => "constraintKeywords",
        "keyword.operator.logical.precept" => "logicalOperators",
        "keyword.operator.membership.precept" => "membershipOperators",
        "storage.modifier.state.precept" => "stateModifiers",
        "storage.type.precept" => "typeKeywords",
        "constant.language.boolean.precept" => "booleanLiterals",
        "keyword.operator.precept" => "symbolOperators",
        "keyword.operator.arrow.precept" => "arrowOperators",
        "keyword.other.precept" => "memberNameKeywords",
        _ => scope.Replace(".precept", "").Replace(".", "_") + "Keywords"
    };
}
