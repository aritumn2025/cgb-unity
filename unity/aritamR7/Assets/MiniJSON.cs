using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Cgb.Unity
{
    /// <summary>
    /// Minimal JSON encoder/decoder for Unity runtime.
    /// Based on the MIT-licensed MiniJSON implementation.
    /// </summary>
    internal static class MiniJSON
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return new Parser(json).ParseValue();
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            internal Parser(string json)
            {
                _json = json;
            }

            internal object ParseValue()
            {
                EatWhitespace();
                if (_index == _json.Length)
                {
                    return null;
                }

                char c = _json[_index];
                switch (c)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return ParseString();
                    case 't':
                        return ParseLiteral("true", true);
                    case 'f':
                        return ParseLiteral("false", false);
                    case 'n':
                        return ParseLiteral("null", null);
                    default:
                        if (IsDigit(c) || c == '-')
                        {
                            return ParseNumber();
                        }
                        break;
                }

                throw new FormatException($"Invalid JSON value at index {_index}.");
            }

            private IDictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                };
                // Skip '{'
                _index++;
                while (true)
                {
                    EatWhitespace();
                    if (_index >= _json.Length)
                    {
                        throw new FormatException("Unterminated object literal.");
                    }
                    if (_json[_index] == '}')
                    {
                        _index++;
                        break;
                    }

                    string key = ParseString();
                    EatWhitespace();
                    if (_index >= _json.Length || _json[_index] != ':')
                    {
                        throw new FormatException("Expected ':' after object key.");
                    }
                    _index++;
                    object value = ParseValue();
                    table[key] = value;

                    EatWhitespace();
                    if (_index >= _json.Length)
                    {
                        throw new FormatException("Unterminated object literal.");
                    }
                    char delimiter = _json[_index++];
                    if (delimiter == '}')
                    {
                        break;
                    }
                    if (delimiter != ',')
                    {
                        throw new FormatException($"Invalid object delimiter '{delimiter}'.");
                    }
                }

                return table;
            }

            private IList<object> ParseArray()
            {
                var array = new List<object>();
                _index++;
                bool parsing = true;
                while (parsing)
                {
                    EatWhitespace();
                    if (_index >= _json.Length)
                    {
                        throw new FormatException("Unterminated array literal.");
                    }
                    if (_json[_index] == ']')
                    {
                        _index++;
                        break;
                    }
                    array.Add(ParseValue());
                    EatWhitespace();
                    if (_index >= _json.Length)
                    {
                        throw new FormatException("Unterminated array literal.");
                    }
                    char delimiter = _json[_index++];
                    switch (delimiter)
                    {
                        case ']':
                            parsing = false;
                            break;
                        case ',':
                            break;
                        default:
                            throw new FormatException($"Invalid array delimiter '{delimiter}'.");
                    }
                }

                return array;
            }

            private string ParseString()
            {
                if (_json[_index] != '"')
                {
                    throw new FormatException("Expected string literal.");
                }
                _index++;
                var sb = new StringBuilder();
                while (_index < _json.Length)
                {
                    char c = _json[_index++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }
                    if (c == '\\')
                    {
                        if (_index == _json.Length)
                        {
                            break;
                        }
                        char escaped = _json[_index++];
                        switch (escaped)
                        {
                            case '"':
                            case '\\':
                            case '/':
                                sb.Append(escaped);
                                break;
                            case 'b':
                                sb.Append('\b');
                                break;
                            case 'f':
                                sb.Append('\f');
                                break;
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case 'u':
                                sb.Append(ParseUnicode());
                                break;
                            default:
                                throw new FormatException($"Invalid escape character '{escaped}'.");
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                throw new FormatException("Unterminated string literal.");
            }

            private char ParseUnicode()
            {
                if (_index + 4 > _json.Length)
                {
                    throw new FormatException("Incomplete unicode escape sequence.");
                }
                string hex = _json.Substring(_index, 4);
                _index += 4;
                if (ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out ushort code))
                {
                    return (char)code;
                }
                throw new FormatException($"Invalid unicode escape '\\u{hex}'.");
            }

            private object ParseLiteral(string literal, object value)
            {
                if (_index + literal.Length > _json.Length)
                {
                    throw new FormatException($"Unexpected end while parsing '{literal}'.");
                }
                for (int i = 0; i < literal.Length; i++)
                {
                    if (_json[_index + i] != literal[i])
                    {
                        throw new FormatException($"Invalid literal starting at index {_index}.");
                    }
                }
                _index += literal.Length;
                return value;
            }

            private object ParseNumber()
            {
                int start = _index;
                while (_index < _json.Length && "0123456789+-.eE".IndexOf(_json[_index]) >= 0)
                {
                    _index++;
                }
                string number = _json.Substring(start, _index - start);
                if (double.TryParse(number, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
                throw new FormatException($"Invalid number literal '{number}'.");
            }

            private void EatWhitespace()
            {
                while (_index < _json.Length)
                {
                    if (!char.IsWhiteSpace(_json, _index))
                    {
                        break;
                    }
                    _index++;
                }
            }

            private static bool IsDigit(char c)
            {
                return c >= '0' && c <= '9';
            }
        }
    }
}
