using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jinhyeong_GameData.Json
{

    public enum EJsonType
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object
    }

    public sealed class JsonValue
    {
        private static readonly List<JsonValue> EmptyArray = new List<JsonValue>(0);

        public static readonly JsonValue Null = new JsonValue { Type = EJsonType.Null };

        public EJsonType Type { get; private set; }

        private bool _bool;
        private double _number;
        private string _string;
        private List<JsonValue> _array;
        private Dictionary<string, JsonValue> _object;

        public bool IsNull { get { return Type == EJsonType.Null; } }
        public bool IsArray { get { return Type == EJsonType.Array; } }
        public bool IsObject { get { return Type == EJsonType.Object; } }

        public int Count
        {
            get
            {
                if (Type == EJsonType.Array)
                {
                    return _array.Count;
                }
                if (Type == EJsonType.Object)
                {
                    return _object.Count;
                }
                return 0;
            }
        }

        public IReadOnlyList<JsonValue> Items
        {
            get { return Type == EJsonType.Array ? (IReadOnlyList<JsonValue>)_array : EmptyArray; }
        }

        public JsonValue this[int index]
        {
            get
            {
                if (Type != EJsonType.Array || index < 0 || index >= _array.Count)
                {
                    return Null;
                }
                return _array[index];
            }
        }

        public JsonValue this[string key]
        {
            get
            {
                if (Type != EJsonType.Object || _object.TryGetValue(key, out JsonValue value) == false)
                {
                    return Null;
                }
                return value;
            }
        }

        public bool Has(string key)
        {
            return Type == EJsonType.Object && _object.ContainsKey(key);
        }

        public IEnumerable<KeyValuePair<string, JsonValue>> Members
        {
            get
            {
                if (Type != EJsonType.Object)
                {
                    yield break;
                }
                foreach (KeyValuePair<string, JsonValue> pair in _object)
                {
                    yield return pair;
                }
            }
        }

        public string AsString(string fallback = "")
        {
            return Type == EJsonType.String ? _string : fallback;
        }

        public float AsFloat(float fallback = 0f)
        {
            return Type == EJsonType.Number ? (float)_number : fallback;
        }

        public int AsInt(int fallback = 0)
        {
            return Type == EJsonType.Number ? (int)Math.Round(_number) : fallback;
        }

        public bool AsBool(bool fallback = false)
        {
            return Type == EJsonType.Bool ? _bool : fallback;
        }

        public string ToRawString()
        {
            switch (Type)
            {
                case EJsonType.String: return _string;
                case EJsonType.Number: return _number.ToString("R", CultureInfo.InvariantCulture);
                case EJsonType.Bool: return _bool ? "true" : "false";
                default: return string.Empty;
            }
        }

        public string GetString(string key, string fallback = "")
        {
            return this[key].AsString(fallback);
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            return this[key].AsFloat(fallback);
        }

        public int GetInt(string key, int fallback = 0)
        {
            return this[key].AsInt(fallback);
        }

        public static JsonValue Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new FormatException("빈 JSON 문자열");
            }

            Parser parser = new Parser(text);
            JsonValue value = parser.ParseValue();
            parser.SkipWhitespace();
            if (parser.AtEnd == false)
            {
                throw parser.Error("최상위 값 뒤에 남은 문자가 있습니다");
            }
            return value;
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _index;

            public Parser(string text)
            {
                _text = text;
                _index = 0;
                if (_text.Length > 0 && _text[0] == '﻿')
                {
                    _index = 1;
                }
            }

            public bool AtEnd { get { return _index >= _text.Length; } }

            public FormatException Error(string message)
            {
                return new FormatException($"JSON 파싱 실패 (offset {_index}): {message}");
            }

            public void SkipWhitespace()
            {
                while (_index < _text.Length)
                {
                    char c = _text[_index];
                    if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                    {
                        return;
                    }
                    _index++;
                }
            }

            public JsonValue ParseValue()
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    throw Error("값이 오기 전에 문자열이 끝났습니다");
                }

                char c = _text[_index];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return new JsonValue { Type = EJsonType.String, _string = ParseString() };
                    case 't': Expect("true"); return new JsonValue { Type = EJsonType.Bool, _bool = true };
                    case 'f': Expect("false"); return new JsonValue { Type = EJsonType.Bool, _bool = false };
                    case 'n': Expect("null"); return Null;
                    default: return ParseNumber();
                }
            }

            private void Expect(string literal)
            {
                if (_index + literal.Length > _text.Length ||
                    string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0)
                {
                    throw Error($"'{literal}' 을 기대했습니다");
                }
                _index += literal.Length;
            }

            private JsonValue ParseObject()
            {
                _index++;
                JsonValue value = new JsonValue
                {
                    Type = EJsonType.Object,
                    _object = new Dictionary<string, JsonValue>(8)
                };

                SkipWhitespace();
                if (AtEnd == false && _text[_index] == '}')
                {
                    _index++;
                    return value;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _text[_index] != '"')
                    {
                        throw Error("객체 키(문자열)를 기대했습니다");
                    }

                    string key = ParseString();

                    SkipWhitespace();
                    if (AtEnd || _text[_index] != ':')
                    {
                        throw Error("':' 를 기대했습니다");
                    }
                    _index++;

                    value._object[key] = ParseValue();

                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("객체가 닫히지 않았습니다");
                    }
                    if (_text[_index] == ',')
                    {
                        _index++;
                        continue;
                    }
                    if (_text[_index] == '}')
                    {
                        _index++;
                        return value;
                    }
                    throw Error("',' 또는 '}' 를 기대했습니다");
                }
            }

            private JsonValue ParseArray()
            {
                _index++;
                JsonValue value = new JsonValue
                {
                    Type = EJsonType.Array,
                    _array = new List<JsonValue>(8)
                };

                SkipWhitespace();
                if (AtEnd == false && _text[_index] == ']')
                {
                    _index++;
                    return value;
                }

                while (true)
                {
                    value._array.Add(ParseValue());

                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("배열이 닫히지 않았습니다");
                    }
                    if (_text[_index] == ',')
                    {
                        _index++;
                        continue;
                    }
                    if (_text[_index] == ']')
                    {
                        _index++;
                        return value;
                    }
                    throw Error("',' 또는 ']' 를 기대했습니다");
                }
            }

            private string ParseString()
            {
                _index++;
                StringBuilder builder = new StringBuilder(32);

                while (true)
                {
                    if (AtEnd)
                    {
                        throw Error("문자열이 닫히지 않았습니다");
                    }

                    char c = _text[_index++];
                    if (c == '"')
                    {
                        return builder.ToString();
                    }
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (AtEnd)
                    {
                        throw Error("이스케이프가 끝나지 않았습니다");
                    }

                    char escaped = _text[_index++];
                    switch (escaped)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                        {
                            if (_index + 4 > _text.Length)
                            {
                                throw Error("\\u 이스케이프가 짧습니다");
                            }
                            string hex = _text.Substring(_index, 4);
                            if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code) == false)
                            {
                                throw Error($"\\u{hex} 를 해석할 수 없습니다");
                            }
                            builder.Append((char)code);
                            _index += 4;
                            break;
                        }
                        default:
                            throw Error($"알 수 없는 이스케이프 '\\{escaped}'");
                    }
                }
            }

            private JsonValue ParseNumber()
            {
                int start = _index;
                if (AtEnd == false && (_text[_index] == '-' || _text[_index] == '+'))
                {
                    _index++;
                }
                while (AtEnd == false)
                {
                    char c = _text[_index];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                    {
                        _index++;
                        continue;
                    }
                    break;
                }

                string raw = _text.Substring(start, _index - start);
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) == false)
                {
                    throw Error($"숫자를 해석할 수 없습니다: '{raw}'");
                }
                return new JsonValue { Type = EJsonType.Number, _number = parsed };
            }
        }
    }
}
