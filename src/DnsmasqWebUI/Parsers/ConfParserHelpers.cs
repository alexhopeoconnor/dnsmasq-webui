using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Shared Superpower text-parser helpers used by .conf and related parsers.
/// Keeps grammars consistent and easier to read.
/// </summary>
public static class ConfParserHelpers
{
    /// <summary>
    /// Optional whitespace before and after a parser (Superpower has no built-in Token for text parsers).
    /// Use for comma, equals, or literal tokens where config allows spaces around them.
    /// </summary>
    public static TextParser<T> Token<T>(TextParser<T> parser) =>
        Character.WhiteSpace.Many().IgnoreThen(parser).Then(x =>
            Character.WhiteSpace.Many().IgnoreThen(Parse.Return(x)));

    /// <summary>
    /// Optional <c>##</c> or <c>#</c> at the start of a line (consumed, returns Unit).
    /// Use when recognizing lines that may be commented or "deleted" (##) but you only need to consume the prefix.
    /// </summary>
    public static readonly TextParser<Unit> OptionalCommentPrefix =
        Character.EqualTo('#').Repeat(2).Value(Unit.Value).Try()
            .Or(Character.EqualTo('#').Value(Unit.Value))
            .OptionalOrDefault(Unit.Value)
            .Select(_ => Unit.Value);
}
