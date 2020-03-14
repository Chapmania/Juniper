using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Veldrid;

namespace Juniper.VeldridIntegration
{
    public struct ParsedShader : IEquatable<ParsedShader>
    {
        /// <summary>
        /// Optional whitespace
        /// </summary>
        private const string ospace = @"(?:\s|\n)*";

        /// <summary>
        /// Required whitespace
        /// </summary>
        private const string rspace = @"(?:\s|\n)+";

        private const string word = @"\w+";
        private const string number = @"\d+";
        private const string sblock = ospace + "\\{" + ospace;
        private static readonly string comma = $@"{ospace},{ospace}";
        private static readonly string semi = $@"{ospace};{ospace}";
        private static readonly string ident = $@"{word}(?:{ospace}\[{ospace}({number}){ospace}\])?";
        private static readonly string tident = $@"({word}){rspace}({ident}){semi}";
        private static readonly string eblock = ospace + "\\}" + semi;

        private const string qualifierTypes = "binding|set|location|index|component";
        private static readonly string qualifierPatternString = $@"({qualifierTypes}){ospace}={ospace}({number})";
        private static readonly string qualifierInLayoutPatternString = $@"(?:{qualifierTypes}){ospace}={ospace}{number}";
        private static readonly string layoutPatternString = $@"layout{ospace}\({ospace}({qualifierInLayoutPatternString}(?:{comma}{qualifierInLayoutPatternString})*){ospace}\)";
        private static readonly string attributeLayoutPatternString = $@"({layoutPatternString})?{ospace}(in|out){rspace}{tident}";
        private static readonly string resourceBlock = $@"{sblock}(?:{ospace}{tident})+{eblock}";
        private static readonly string resourceDescriptorPatternString = $@"{layoutPatternString}{ospace}uniform{rspace}({tident}|({word})({resourceBlock}))";

        private static readonly Regex qualifierPattern = new Regex(qualifierPatternString, RegexOptions.Compiled);
        private static readonly Regex attributeLayoutPattern = new Regex(attributeLayoutPatternString, RegexOptions.Compiled);
        private static readonly Regex resourceDescriptorPattern = new Regex(resourceDescriptorPatternString, RegexOptions.Compiled);
        private static readonly Regex typedIdentPattern = new Regex(tident, RegexOptions.Compiled);

        public ShaderDescription Description { get; }
        public IReadOnlyList<ShaderAttribute> Attributes { get; }
        public IReadOnlyList<ShaderResource> Resources { get; }

        public ParsedShader(ShaderStages stage, byte[] shaderBytes)
        {
#if DEBUG
            const bool debug = true;
#else
            const bool debug = false;
#endif

            Description = new ShaderDescription(stage, shaderBytes, "main", debug);

            var shaderText = Encoding.UTF8.GetString(shaderBytes);
            Attributes = ParseAttributes(shaderText);
            Resources = ParseShaderResources(stage, shaderText);
        }

        private static ShaderResource[] ParseShaderResources(ShaderStages stage, string shaderText)
        {
            return resourceDescriptorPattern.Matches(shaderText)
                .Cast<Match>()
                .Select(m => ParseResource(stage, m))
                .OrderBy(r => r.Set)
                .ThenBy(r => r.Binding)
                .ToArray();
        }

        private static ShaderResource ParseResource(ShaderStages stage, Match match)
        {
            var parts = match.Groups
                .Cast<Group>()
                .Skip(1)
                .Select(g => g.Value.Trim())
                .Where(v => v.Length > 0)
                .ToArray();

            var qualifiers = parts[0]
                    .Split(',')
                    .Select(q => ParseLayoutQualifier(match.Value, q))
                    .ToArray();

            if (qualifiers.Length == 0)
            {
                throw new FormatException($"No shader attribute qualifiers defined in line '{match.Value}'");
            }

            var isInline = parts.Length == 4;
            if (isInline)
            {
                var dataType = parts[2];
                var kind = dataType switch
                {
                    "texture2D" => ResourceKind.TextureReadOnly,
                    "sampler" => ResourceKind.Sampler,
                    _ => ResourceKind.UniformBuffer
                };
                var name = parts[3];
                return new ShaderResource(name, qualifiers, kind, stage);
            }
            else
            {
                var kind = ResourceKind.UniformBuffer;
                var name = parts[2];
                var block = parts[3];
                var identifiers = ParseIdentifiers(block);
                return new ShaderResource(name, qualifiers, kind, stage, identifiers);
            }
        }

        private static (ShaderDataType type, string name, uint size)[] ParseIdentifiers(string blockText)
        {
            return typedIdentPattern.Matches(blockText)
                .Cast<Match>()
                .Select(ParseIdentifier)
                .ToArray();
        }

        private static (ShaderDataType type, string name, uint size) ParseIdentifier(Match match)
        {
            var parts = match.Groups
                .Cast<Group>()
                .Skip(1)
                .Select(g => g.Value.Trim())
                .ToArray();

            var dataTypeString = parts[0];
            if (!Enum.TryParse<ShaderDataType>(dataTypeString, true, out var dataType))
            {
                throw new FormatException($"Invalid shader attribute type '{dataTypeString}' in line '{match.Value}'.");
            }

            var name = parts[1];
            var sizeString = parts[2];
            var size = 1u;
            if(sizeString.Length > 0)
            {
                if(!uint.TryParse(sizeString, out size))
                {
                    throw new FormatException("Could not parse type size");
                }
            }
            return (dataType, name, size);
        }

        private static ShaderAttribute[] ParseAttributes(string shaderText)
        {
            return attributeLayoutPattern.Matches(shaderText)
                .Cast<Match>()
                .Select((m, i) => ParseAttribute(i, m))
                .OrderBy(a => a.Direction)
                .ThenBy(a => a.Location)
                .ToArray();
        }

        private static ShaderAttribute ParseAttribute(int i, Match match)
        {
            var qualifiers = Array.Empty<ShaderLayoutQualifier>();
            var hasLayout = match.Groups[1].Value.Length > 0;
            if (hasLayout)
            {
                qualifiers = match.Groups[2].Value
                    .Split(',')
                    .Select(q => ParseLayoutQualifier(match.Value, q))
                    .ToArray();

                if (qualifiers.Length == 0)
                {
                    throw new FormatException($"No shader attribute qualifiers defined in line '{match.Value}'");
                }
            }

            var directionString = match.Groups[3].Value;
            if (!Enum.TryParse<ShaderAttributeDirection>(directionString, true, out var direction))
            {
                throw new FormatException($"Invalid shader attribute direction '{directionString}' in line '{match.Value}'.");
            }

            var dataTypeString = match.Groups[4].Value;
            if(!Enum.TryParse<ShaderDataType>(dataTypeString, true, out var dataType))
            {
                throw new FormatException($"Invalid shader attribute type '{dataTypeString}' in line '{match.Value}'.");
            }

            var name = match.Groups[5].Value;
            return new ShaderAttribute(i, name, direction, dataType, qualifiers);
        }

        private static ShaderLayoutQualifier ParseLayoutQualifier(string line, string part)
        {
            var qualifierMatch = qualifierPattern.Match(part);
            if (!qualifierMatch.Success)
            {
                throw new FormatException($"Invalid shader attribute qualifier '{part}' in line '{line}'");
            }

            var qualifierTypeString = qualifierMatch.Groups[1].Value;
            var qualifierValueString = qualifierMatch.Groups[2].Value;
            if (!Enum.TryParse<ShaderLayoutQualifierType>(qualifierTypeString, true, out var qualifierType))
            {
                throw new FormatException($"Invalid shader attribute qualifier type '{qualifierTypeString}' for qualifier '{qualifierMatch.Value}' in line '{line}'");
            }

            if (!uint.TryParse(qualifierValueString, out var qualifierValue))
            {
                throw new FormatException($"Invalid shader attribute qualifier value '{qualifierValueString}' for qualifier '{qualifierMatch.Value}' in line '{line}'");
            }

            return new ShaderLayoutQualifier(qualifierType, qualifierValue);
        }

        public override bool Equals(object obj)
        {
            return obj is ParsedShader shader && Equals(shader);
        }

        public bool Equals(ParsedShader other)
        {
            return Description.Equals(other.Description) &&
                   EqualityComparer<IReadOnlyList<ShaderAttribute>>.Default.Equals(Attributes, other.Attributes) &&
                   EqualityComparer<IReadOnlyList<ShaderResource>>.Default.Equals(Resources, other.Resources);
        }

        public override int GetHashCode()
        {
            var hashCode = 1151065215;
            hashCode = hashCode * -1521134295 + Description.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyList<ShaderAttribute>>.Default.GetHashCode(Attributes);
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyList<ShaderResource>>.Default.GetHashCode(Resources);
            return hashCode;
        }

        public static bool operator ==(ParsedShader left, ParsedShader right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ParsedShader left, ParsedShader right)
        {
            return !(left == right);
        }
    }
}
