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

    /// <summary>
    /// Dnsmasq-style: content before first '#' that is at word start (after whitespace). Consumes to EOL.
    /// Returns the trimmed content (no comment). Use for directive lines that allow inline # comment.
    /// </summary>
    public static readonly TextParser<string> StripCommentContent = span =>
    {
        var source = span.Source ?? "";
        var i = span.Position.Absolute;
        var end = Math.Min(i + span.Length, source.Length);
        var white = true;
        var len = 0;
        while (i + len < end)
        {
            var c = source[i + len];
            if (char.IsWhiteSpace(c))
                white = true;
            else if (white && c == '#')
                break;
            else
                white = false;
            len++;
        }
        var consumed = span.First(len);
        var content = consumed.ToStringValue().TrimEnd();
        var remainder = span.Skip(len);
        return Result.Value(content, consumed, remainder);
    };

    /// <summary>
    /// Key (no '='), optional spaces, optional '=value'. Full line. Reusable for key=value config lines.
    /// </summary>
    public static readonly TextParser<(string key, string value)> KeyValueLine =
        Character.Matching(c => c != '=' && c != '\r' && c != '\n', "key character")
            .AtLeastOnce()
            .Text()
            .Then(k => Character.WhiteSpace.Many()
                .IgnoreThen(Character.EqualTo('=')
                    .IgnoreThen(Character.AnyChar.Many().Text().Select(s => s.Trim()))
                    .OptionalOrDefault(""))
                .AtEnd()
                .Select(v => (k.TrimEnd().Trim(), v)));
}
