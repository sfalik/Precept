using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

using DiagnosticsCatalog = Precept.Language.Diagnostics;

public static partial class Parser
{
    private sealed partial class ParserState
    {
        // ── TypeExpression: "as TypeKeyword [of InnerType] [Qualifiers]" ────────────

        private SlotValue ParseTypeExpression(ConstructSlot slot)
        {
            if (Peek().Kind != TokenKind.As)
            {
                if (!slot.IsRequired)
                    return MakeSentinel(slot);
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, Peek().Span, "as", Peek().Text));
                return new TypeExpressionSlot(new MissingTypeReference(SourceSpan.Missing), SourceSpan.Missing);
            }

            var asToken = Advance(); // consume 'as'
            var typeRef = ParseTypeReference(asToken.Span);
            var span = SourceSpan.Covering(asToken.Span, typeRef.Span);
            return new TypeExpressionSlot(typeRef, span);
        }

        /// <summary>
        /// Parses a type reference after 'as' has been consumed.
        /// Handles: simple types, ~type (CI), collection types with inner types,
        /// choice types with domain, and keyed collections (log by, queue by, lookup).
        /// </summary>
        private ParsedTypeReference ParseTypeReference(SourceSpan startSpan)
        {
            var peekToken = Peek();

            // Handle CI type prefix: ~string
            if (peekToken.Kind == TokenKind.Tilde)
            {
                var tildeToken = Advance();
                var innerToken = Peek();
                var lookupKind = innerToken.Kind == TokenKind.Set ? TokenKind.SetType : innerToken.Kind;
                if (Types.ByToken.TryGetValue(lookupKind, out var innerType))
                {
                    Advance();
                    var span = SourceSpan.Covering(tildeToken.Span, innerToken.Span);
                    return new CITypeReference(innerType, span);
                }
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, innerToken.Span, "type keyword", innerToken.Text));
                return new MissingTypeReference(tildeToken.Span);
            }

            // Handle dual-use 'set' token: in type context, treat Set as SetType
            var lookupTokenKind = peekToken.Kind == TokenKind.Set ? TokenKind.SetType : peekToken.Kind;

            if (!Types.ByToken.TryGetValue(lookupTokenKind, out var typeMeta))
            {
                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, peekToken.Span, "type keyword", peekToken.Text));
                return new MissingTypeReference(startSpan);
            }

            var typeToken = Advance();

            // Check if this is a choice type: choice of T(...)
            if (typeMeta.Kind == TypeKind.Choice)
            {
                return ParseChoiceType(typeMeta, typeToken.Span);
            }

            // Check if this is a collection type (needs "of" inner type)
            if (typeMeta.Category == TypeCategory.Collection)
            {
                return ParseCollectionType(typeMeta, typeToken.Span);
            }

            // Simple type — try qualifier extensions ('in', 'of', 'to')
            var simpleRef = new SimpleTypeReference(typeMeta, typeToken.Span);
            return TryParseQualifiers(simpleRef, typeMeta);
        }

        private ParsedTypeReference ParseChoiceType(TypeMeta choiceMeta, SourceSpan typeSpan)
        {
            TypeMeta? elementType = null;
            var domain = ImmutableArray.CreateBuilder<string>();
            var lastSpan = typeSpan;

            // choice of T(...) - look for 'of' and element type
            if (Peek().Kind == TokenKind.Of)
            {
                Advance(); // consume 'of'
                var elemToken = Peek();
                var elemLookup = elemToken.Kind == TokenKind.Set ? TokenKind.SetType : elemToken.Kind;
                if (Types.ByToken.TryGetValue(elemLookup, out var elemMeta))
                {
                    Advance();
                    elementType = elemMeta;
                    lastSpan = elemToken.Span;
                }
            }

            // Parse domain: (value, value, ...)
            if (Peek().Kind == TokenKind.LeftParen)
            {
                Advance(); // consume '('
                while (Peek().Kind != TokenKind.RightParen && !IsAtEnd && !IsAtConstructBoundary())
                {
                    // Accept sign prefix for numeric choices
                    var signPrefix = "";
                    if (Peek().Kind == TokenKind.Minus || Peek().Kind == TokenKind.Plus)
                    {
                        signPrefix = Peek().Text;
                        Advance();
                    }

                    var valueToken = Peek();
                    if (valueToken.Kind is TokenKind.StringLiteral or TokenKind.NumberLiteral
                        or TokenKind.True or TokenKind.False or TokenKind.Identifier)
                    {
                        domain.Add(signPrefix + valueToken.Text);
                        lastSpan = Advance().Span;
                    }
                    else
                    {
                        break;
                    }

                    if (Peek().Kind == TokenKind.Comma)
                        Advance();
                    else
                        break;
                }

                if (Peek().Kind == TokenKind.RightParen)
                    lastSpan = Advance().Span;
            }

            var span = SourceSpan.Covering(typeSpan, lastSpan);
            return new ChoiceTypeReference(choiceMeta, elementType, domain.ToImmutable(), span);
        }

        private ParsedTypeReference ParseCollectionType(TypeMeta collectionMeta, SourceSpan typeSpan)
        {
            var lastSpan = typeSpan;
            ParsedTypeReference? elementType = null;
            ParsedTypeReference? keyType = null;

            // Collections require "of" for element type
            if (Peek().Kind == TokenKind.Of)
            {
                Advance(); // consume 'of'
                elementType = ParseInnerTypeReference();
                lastSpan = elementType.Span;
            }

            // Keyed collections (log by, queue by, lookup ... to ...) have a key/ordering type
            // log of T by K, queue of T by K, lookup of K to V
            if (collectionMeta.Kind == TypeKind.Lookup && Peek().Kind == TokenKind.To)
            {
                Advance(); // consume 'to'
                keyType = elementType; // In lookup, the first type is the key
                elementType = ParseInnerTypeReference(); // second is value
                lastSpan = elementType.Span;
            }
            else if ((collectionMeta.Kind is TypeKind.LogBy or TypeKind.QueueBy) && Peek().Kind == TokenKind.By)
            {
                Advance(); // consume 'by'
                keyType = ParseInnerTypeReference();
                lastSpan = keyType.Span;
            }
            else if (Peek().Kind == TokenKind.By)
            {
                // Regular log or queue gets promoted to log-by or queue-by
                Advance(); // consume 'by'
                keyType = ParseInnerTypeReference();
                lastSpan = keyType.Span;
            }

            TokenKind? orderingModifier = null;
            if (Peek().Kind is TokenKind.Ascending or TokenKind.Descending)
            {
                var orderingToken = Advance();
                orderingModifier = orderingToken.Kind;
                lastSpan = orderingToken.Span;
            }

            if (elementType == null)
            {
                elementType = new MissingTypeReference(typeSpan);
            }

            var span = SourceSpan.Covering(typeSpan, lastSpan);
            return new CollectionTypeReference(collectionMeta, elementType, keyType, orderingModifier, span);
        }

        /// <summary>
        /// Parses a simple inner type (no nested collections) for collection element types.
        /// </summary>
        private ParsedTypeReference ParseInnerTypeReference()
        {
            var peekToken = Peek();

            // Handle CI type prefix: ~string
            if (peekToken.Kind == TokenKind.Tilde)
            {
                var tildeToken = Advance();
                var innerToken = Peek();
                var lookupKind = innerToken.Kind == TokenKind.Set ? TokenKind.SetType : innerToken.Kind;
                if (Types.ByToken.TryGetValue(lookupKind, out var innerType))
                {
                    Advance();
                    var span = SourceSpan.Covering(tildeToken.Span, innerToken.Span);
                    return new CITypeReference(innerType, span);
                }
                return new MissingTypeReference(tildeToken.Span);
            }

            var lookupTokenKind = peekToken.Kind == TokenKind.Set ? TokenKind.SetType : peekToken.Kind;
            if (Types.ByToken.TryGetValue(lookupTokenKind, out var typeMeta))
            {
                var typeToken = Advance();
                return new SimpleTypeReference(typeMeta, typeToken.Span);
            }

            return new MissingTypeReference(peekToken.Span);
        }

        /// <summary>
        /// Attempts to parse qualifier slots that follow a simple type reference.
        /// Iterates over <see cref="QualifierShape.Slots"/> from the catalog — no hardcoded token sets.
        /// Returns the original <paramref name="typeRef"/> unchanged if the type has no qualifier shape
        /// or if no preposition tokens are found.
        /// </summary>
        private ParsedTypeReference TryParseQualifiers(
            ParsedTypeReference typeRef, TypeMeta typeMeta)
        {
            if (typeMeta.QualifierShape is null)
                return typeRef;

            var qualifiers = ImmutableArray.CreateBuilder<ParsedQualifier>();
            var lastSpan = typeRef.Span;

            foreach (var slot in typeMeta.QualifierShape.Slots)
            {
                if (Peek().Kind != slot.Preposition)
                    continue;

                Advance(); // consume preposition ('in' / 'of' / 'to')

                if (Peek().Kind == TokenKind.TypedConstant)
                {
                    var valueToken = Advance();
                    qualifiers.Add(new LiteralParsedQualifier(
                        slot.Preposition, slot.Axis,
                        valueToken.Text, valueToken.Span));
                    lastSpan = valueToken.Span;
                    continue;
                }

                if (Peek().Kind == TokenKind.TypedConstantStart)
                {
                    var expression = ParseInterpolatedTypedConstant();
                    qualifiers.Add(new InterpolatedParsedQualifier(
                        slot.Preposition, slot.Axis,
                        expression, expression.Span));
                    lastSpan = expression.Span;
                    continue;
                }

                _diagnostics.Add(DiagnosticsCatalog.Create(
                    DiagnosticCode.ExpectedToken, Peek().Span,
                    "typed constant", Peek().Text));
            }

            if (qualifiers.Count == 0)
                return typeRef;

            return new QualifiedTypeReference(
                typeRef, qualifiers.ToImmutable(),
                SourceSpan.Covering(typeRef.Span, lastSpan));
        }
    }
}
