/* * * * *
 * A simple JSON Parser / builder
 * ------------------------------
 * 
 * It mainly has been written as a simple JSON parser. It can build a JSON string
 * from the node-tree, or generate a node tree from any valid JSON string.
 * 
 * If you want to use compression when saving to file / stream / B64 you have to include
 * SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ ) in your project and
 * define "USE_SharpZipLib" at the top of the file
 * 
 * Written by Bunny83 
 * 2012-06-09
 * 
 * Modified by oPless, 2014-09-21 to round-trip properly
 * 
 * Features / attributes:
 * - provides strongly typed node classes and lists / dictionaries
 * - provides easy access to class members / array items / data values
 * - the parser ignores data types. Each value is a string.
 * - only double quotes (") are used for quoting strings.
 * - values and names are not restricted to quoted strings. They simply add up and are trimmed.
 * - There are only 3 types: arrays(JSONArray), objects(JSONClass) and values(JSONData)
 * - provides "casting" properties to easily convert to / from those types:
 *   int / float / double / bool / long
 * - provides a common interface for each node so no explicit casting is required.
 * - the parser try to avoid errors, but if malformed JSON is parsed the result is undefined
 * 

 * * * * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SimpleJSON
{
    public enum JSONBinaryTag
    {
        Array = 1,
        Class = 2,
        Value = 3,
        IntValue = 4,
        DoubleValue = 5,
        BoolValue = 6,
        FloatValue = 7,
        LongValue = 8,
        Null = 9
    }

    public abstract class JSONNode
    {
        #region common interface

        public virtual void Add(string aKey, JSONNode aItem)
        {
        }

        public virtual JSONNode this[int aIndex]
        {
            get { return null; }
            set { }
        }

        public virtual JSONNode this[string aKey]
        {
            get { return null; }
            set { }
        }

        public virtual string Value
        {
            get { return ""; }
            set { }
        }

        public virtual int Count
        {
            get { return 0; }
        }

        public virtual void Add(JSONNode aItem)
        {
            Add("", aItem);
        }

        public virtual JSONNode Remove(string aKey)
        {
            return null;
        }

        public virtual JSONNode Remove(int aIndex)
        {
            return null;
        }

        public virtual JSONNode Remove(JSONNode aNode)
        {
            return aNode;
        }

        public virtual IEnumerable<JSONNode> Children
        {
            get { yield break; }
        }

        public IEnumerable<JSONNode> DeepChildren
        {
            get
            {
                foreach (var C in Children)
                {
                    foreach (var D in C.DeepChildren)
                    {
                        yield return D;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "JSONNode";
        }

        public virtual string ToString(string aPrefix)
        {
            return "JSONNode";
        }

        #endregion common interface

        #region typecasting properties

        public JSONBinaryTag Tag { get; set; }

        public virtual bool IsNull
        {
            get { return Tag == JSONBinaryTag.Null; }
        }

        public virtual string AsString
        {
            get
            {
                if (Tag == JSONBinaryTag.Null)
                    return null;
                return Value;
            }
        }

        public virtual int AsInt
        {
            get
            {
                int v;
                return int.TryParse(Value, out v) ? v : 0;
            }
            set
            {
                Value = value.ToString(CultureInfo.InvariantCulture);
                Tag = JSONBinaryTag.IntValue;
            }
        }

        public virtual int? AsNullableInt
        {
            get
            {
                if (Tag == JSONBinaryTag.Null)
                    return null;
                return AsInt;
            }
        }

        public virtual long AsLong
        {
            get
            {
                long v;
                return long.TryParse(Value, out v) ? v : 0;
            }
            set
            {
                Value = value.ToString(CultureInfo.InvariantCulture);
                Tag = JSONBinaryTag.LongValue;
            }
        }

        public virtual float AsFloat
        {
            get
            {
                float v;
                return float.TryParse(Value, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0.0f;
            }
            set
            {
                Value = value.ToString(CultureInfo.InvariantCulture);
                Tag = JSONBinaryTag.FloatValue;
            }
        }

        public virtual double AsDouble
        {
            get
            {
                double v;
                return double.TryParse(Value, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : 0.0;
            }
            set
            {
                Value = value.ToString(CultureInfo.InvariantCulture);
                Tag = JSONBinaryTag.DoubleValue;
            }
        }

        public virtual double? AsNullableDouble
        {
            get
            {
                if (Tag == JSONBinaryTag.Null)
                    return null;
                return AsDouble;
            }
        }

        public virtual decimal? AsNullableDecimal
        {
            get
            {
                if (Tag != JSONBinaryTag.Null &&
                    decimal.TryParse(Value, out decimal f))
                    return f;
                return null;
            }
        }

        public virtual bool AsBool
        {
            get
            {
                bool v;
                if (bool.TryParse(Value, out v))
                    return v;
                return !string.IsNullOrEmpty(Value);
            }
            set
            {
                Value = value ? "true" : "false";
                Tag = JSONBinaryTag.BoolValue;
            }
        }

        public virtual bool? AsNullableBool
        {
            get
            {
                if (Tag == JSONBinaryTag.Null)
                    return null;
                return AsBool;
            }
        }

        public virtual JSONArray AsArray
        {
            get { return this as JSONArray; }
        }

        public virtual JSONClass AsObject
        {
            get { return this as JSONClass; }
        }

        #endregion typecasting properties

        #region operators

        public static implicit operator JSONNode(string s)
        {
            return new JSONData(s);
        }

        public static implicit operator string(JSONNode d)
        {
            return (d == null) ? null : d.Value;
        }

        public static bool operator ==(JSONNode a, object b)
        {
            return ReferenceEquals(a, b);
        }

        public static bool operator !=(JSONNode a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion operators

        internal static string Escape(string aText)
        {
            var result = new StringBuilder("");
            foreach (var c in aText)
            {
                switch (c)
                {
                    case '/':
                        result.Append("\\/");
                        break;
                    case '\\':
                        result.Append("\\\\");
                        break;
                    case '\"':
                        result.Append("\\\"");
                        break;
                    case '\n':
                        result.Append("\\n");
                        break;
                    case '\r':
                        result.Append("\\r");
                        break;
                    case '\t':
                        result.Append("\\t");
                        break;
                    case '\b':
                        result.Append("\\b");
                        break;
                    case '\f':
                        result.Append("\\f");
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }
            return result.ToString();
        }

        private static JSONData Numberize(string token)
        {
            bool flag;
            int integer;
            long longInteger;
            double real;
            float floatingPoint;

            if (token.Equals("null"))
            {
                return new JSONData(null);
            }

            if (int.TryParse(token, out integer))
            {
                return new JSONData(integer);
            }

            if (long.TryParse(token, out longInteger))
            {
                return new JSONData(longInteger);
            }

            // If the token is too long we let it fall through to the double parsing instead
            if (token.Length <= 7 &&
                float.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out floatingPoint))
            {
                return new JSONData(floatingPoint);
            }

            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out real))
            {
                return new JSONData(real);
            }


            if (bool.TryParse(token, out flag))
            {
                return new JSONData(flag);
            }

            throw new NotImplementedException(token);
        }

        private static void AddElement(JSONNode ctx, string token, string tokenName, bool tokenIsString)
        {
            if (tokenIsString)
            {
                if (ctx is JSONArray)
                    ctx.Add(token);
                else
                    ctx.Add(tokenName, token); // assume dictionary/object
            }
            else
            {
                var number = Numberize(token);
                if (ctx is JSONArray)
                    ctx.Add(number);
                else
                    ctx.Add(tokenName, number);
            }
        }

        public static JSONNode Parse(string jsonString)
        {
            var stack = new Stack<JSONNode>();
            JSONNode ctx = null;
            var i = 0;
            var token = new StringBuilder("");
            var tokenName = "";
            var quoteMode = false;
            var tokenIsString = false;
            while (i < jsonString.Length)
            {
                var currentChar = jsonString[i];
                switch (currentChar)
                {
                    case '{':
                        if (quoteMode)
                        {
                            token.Append(currentChar);
                            break;
                        }
                        stack.Push(new JSONClass());
                        if (ctx != null)
                        {
                            tokenName = tokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (tokenName.Length != 0)
                                ctx.Add(tokenName, stack.Peek());
                        }
                        tokenName = "";
                        token = new StringBuilder();
                        ctx = stack.Peek();
                        break;

                    case '[':
                        if (quoteMode)
                        {
                            token.Append(currentChar);
                            break;
                        }

                        stack.Push(new JSONArray());
                        if (ctx != null)
                        {
                            tokenName = tokenName.Trim();

                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (tokenName.Length != 0)
                                ctx.Add(tokenName, stack.Peek());
                        }
                        tokenName = "";
                        token = new StringBuilder();
                        ctx = stack.Peek();
                        break;

                    case '}':
                    case ']':
                        if (quoteMode)
                        {
                            token.Append(currentChar);
                            break;
                        }
                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        AddElement(ctx, token.ToString(), tokenName, tokenIsString);
                        tokenIsString = false;
                        tokenName = "";
                        token = new StringBuilder();
                        if (stack.Count > 0)
                            ctx = stack.Peek();
                        break;

                    case ':':
                        if (quoteMode)
                        {
                            token.Append(currentChar);
                            break;
                        }
                        if (tokenName.Length > 0)
                            throw new Exception("JSON Parse: The json seems to be broken");
                        tokenName = token.ToString();
                        token = new StringBuilder();
                        tokenIsString = false;
                        break;

                    case '"':
                        quoteMode ^= true;
                        tokenIsString = quoteMode ? true : tokenIsString;
                        break;

                    case ',':
                        if (quoteMode)
                        {
                            token.Append(currentChar);
                            break;
                        }
                        AddElement(ctx, token.ToString(), tokenName, tokenIsString);
                        tokenIsString = false;
                        tokenName = "";
                        token = new StringBuilder();
                        tokenIsString = false;
                        break;

                    case '\r':
                    case '\n':
                        break;

                    case ' ':
                    case '\t':
                        if (quoteMode)
                            token.Append(currentChar);
                        break;

                    case '\\':
                        ++i;
                        if (quoteMode)
                        {
                            var c = jsonString[i];
                            // The sequences \/, \" and \\ we remove the backslash from when parsing
                            // for \u we convert it into the character it represents
                            // and all others we leave alone
                            switch (c)
                            {
                                case '/':
                                    token.Append('/');
                                    break;
                                case '"':
                                    token.Append('"');
                                    break;
                                case '\\':
                                    token.Append('\\');
                                    break;
                                case 'u':
                                    {
                                        var s = jsonString.Substring(i + 1, 4);
                                        token.Append((char)int.Parse(
                                            s,
                                            NumberStyles.AllowHexSpecifier));
                                        i += 4;
                                        break;
                                    }
                                default:
                                    token.Append('\\');
                                    token.Append(c);
                                    break;
                            }
                        }
                        break;

                    default:
                        if (!quoteMode)
                        {
                            // We check that we dont have illegal characters outside the quotes
                            switch (currentChar)
                            {
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case '0':
                                case '+':
                                case '-':
                                case 'e':
                                case 'E':
                                case '.':
                                    break;
                                case 'n':
                                    {
                                        var s = jsonString.Substring(i, 4);
                                        if (s == "null")
                                        {
                                            i += 4;
                                            token.Append(s);
                                            continue;
                                        }
                                        throw new Exception("Json format seems invalid");
                                    }
                                case 'f':
                                    {
                                        var s = jsonString.Substring(i, 5);
                                        if (s == "false")
                                        {
                                            i += 5;
                                            token.Append(s);
                                            continue;
                                        }
                                        throw new Exception("Json format seems invalid");
                                    }
                                case 't':
                                    {
                                        var s = jsonString.Substring(i, 4);
                                        if (s == "true")
                                        {
                                            i += 4;
                                            token.Append(s);
                                            continue;
                                        }
                                        throw new Exception("Json format seems invalid");
                                    }
                                default:
                                    throw new Exception("Json format seems invalid");
                            }
                        }

                        token.Append(currentChar);
                        break;
                }
                ++i;
            }
            if (quoteMode)
            {
                throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
            }
            if (stack.Count != 0)
            {
                throw new Exception("There are unclosed {} or [] in the string");
            }
            return ctx;
        }

        public virtual void Serialize(BinaryWriter aWriter)
        {
        }

        public void SaveToStream(Stream aData)
        {
            var w = new BinaryWriter(aData);
            Serialize(w);
        }

        public string SaveToBase64()
        {
            using (var stream = new MemoryStream())
            {
                SaveToStream(stream);
                stream.Position = 0;
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static JSONNode Deserialize(BinaryReader aReader)
        {
            var type = (JSONBinaryTag)aReader.ReadByte();
            switch (type)
            {
                case JSONBinaryTag.Array:
                    {
                        var count = aReader.ReadInt32();
                        var tmp = new JSONArray();
                        for (var i = 0; i < count; i++)
                            tmp.Add(Deserialize(aReader));
                        return tmp;
                    }
                case JSONBinaryTag.Class:
                    {
                        var count = aReader.ReadInt32();
                        var tmp = new JSONClass();
                        for (var i = 0; i < count; i++)
                        {
                            var key = aReader.ReadString();
                            var val = Deserialize(aReader);
                            tmp.Add(key, val);
                        }
                        return tmp;
                    }
                case JSONBinaryTag.Value:
                    {
                        return new JSONData(aReader.ReadString());
                    }
                case JSONBinaryTag.IntValue:
                    {
                        return new JSONData(aReader.ReadInt32());
                    }
                case JSONBinaryTag.DoubleValue:
                    {
                        return new JSONData(aReader.ReadDouble());
                    }
                case JSONBinaryTag.BoolValue:
                    {
                        return new JSONData(aReader.ReadBoolean());
                    }
                case JSONBinaryTag.FloatValue:
                    {
                        return new JSONData(aReader.ReadSingle());
                    }
                case JSONBinaryTag.LongValue:
                    {
                        return new JSONData(aReader.ReadInt64());
                    }
                case JSONBinaryTag.Null:
                    {
                        return new JSONData(null);
                    }
            }

            throw new Exception("JSON Deserialize: Unknown tag in stream");
        }

        public static JSONNode LoadFromStream(Stream aData)
        {
            using (var r = new BinaryReader(aData))
            {
                return Deserialize(r);
            }
        }

        public static JSONNode LoadFromBase64(string aBase64)
        {
            var tmp = Convert.FromBase64String(aBase64);
            var stream = new MemoryStream(tmp) { Position = 0 };
            return LoadFromStream(stream);
        }
    }

    // End of JSONNode

    public class JSONArray : JSONNode, IEnumerable
    {
        private readonly List<JSONNode> _nodeList = new List<JSONNode>();

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= _nodeList.Count)
                    return null;
                return _nodeList[aIndex];
            }
            set
            {
                if (aIndex < 0 || aIndex >= _nodeList.Count)
                    _nodeList.Add(value);
                else
                    _nodeList[aIndex] = value;
            }
        }

        public override JSONNode this[string aKey]
        {
            get { return null; }
            set { _nodeList.Add(value); }
        }

        public override int Count
        {
            get { return _nodeList.Count; }
        }

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (var node in _nodeList)
                    yield return node;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (var node in _nodeList)
                yield return node;
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            _nodeList.Add(aItem);
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= _nodeList.Count)
                return null;
            var tmp = _nodeList[aIndex];
            _nodeList.RemoveAt(aIndex);
            return tmp;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            _nodeList.Remove(aNode);
            return aNode;
        }

        public override string ToString()
        {
            var result = new StringBuilder("[ ");
            foreach (var node in _nodeList)
            {
                if (result.Length > 2)
                    result.Append(", ");
                result.Append(node.ToString());
            }
            result.Append(" ]");
            return result.ToString();
        }

        public override string ToString(string aPrefix)
        {
            var result = new StringBuilder("[ ");
            foreach (var node in _nodeList)
            {
                if (result.Length > 3)
                    result.Append(", ");
                result.Append("\n");
                result.Append(aPrefix);
                result.Append("   ");
                result.Append(node.ToString(string.Format("{0}   ", aPrefix)));
            }
            result.Append("\n");
            result.Append(aPrefix);
            result.Append("]");
            return result.ToString();
        }

        public override void Serialize(BinaryWriter aWriter)
        {
            aWriter.Write((byte)JSONBinaryTag.Array);
            aWriter.Write(_nodeList.Count);
            foreach (JSONNode node in _nodeList)
            {
                node.Serialize(aWriter);
            }
        }
    }

    // End of JSONArray

    public class JSONClass : JSONNode, IEnumerable
    {
        private readonly Dictionary<string, JSONNode> _nodeDict = new Dictionary<string, JSONNode>();

        public override JSONNode this[string aKey]
        {
            get { return _nodeDict.TryGetValue(aKey, out JSONNode nd) ? nd : null; }
            set { _nodeDict[aKey] = value; }
        }

        public override int Count
        {
            get { return _nodeDict.Count; }
        }

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (var nodePair in _nodeDict)
                    yield return nodePair.Value;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (var nodePair in _nodeDict)
                yield return nodePair;
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            if (!string.IsNullOrEmpty(aKey))
            {
                if (_nodeDict.ContainsKey(aKey))
                    _nodeDict[aKey] = aItem;
                else
                    _nodeDict.Add(aKey, aItem);
            }
            else
                _nodeDict.Add(Guid.NewGuid().ToString(), aItem);
        }

        public override JSONNode Remove(string aKey)
        {
            if (!_nodeDict.ContainsKey(aKey))
                return null;
            var tmp = _nodeDict[aKey];
            _nodeDict.Remove(aKey);
            return tmp;
        }

        public override string ToString()
        {
            var result = new StringBuilder("{");
            foreach (var nodePair in _nodeDict)
            {
                if (result.Length > 2)
                    result.Append(", ");
                result.Append("\"");
                result.Append(Escape(nodePair.Key));
                result.Append("\":");
                result.Append(nodePair.Value.ToString());
            }
            result.Append("}");
            return result.ToString();
        }

        public override string ToString(string aPrefix)
        {
            var result = new StringBuilder("{ ");
            foreach (var nodePair in _nodeDict)
            {
                if (result.Length > 3)
                    result.Append(", ");
                result.Append("\n");
                result.Append(aPrefix);
                result.Append("   ");
                result.Append("\"");
                result.Append(Escape(nodePair.Key));
                result.Append("\" : ");
                result.Append(nodePair.Value.ToString(string.Format("{0}   ", aPrefix)));
            }
            result.Append("\n");
            result.Append(aPrefix);
            result.Append("}");
            return result.ToString();
        }

        public override void Serialize(BinaryWriter aWriter)
        {
            aWriter.Write((byte)JSONBinaryTag.Class);
            aWriter.Write(_nodeDict.Count);
            foreach (var nodeKeys in _nodeDict.Keys)
            {
                aWriter.Write(nodeKeys);
                _nodeDict[nodeKeys].Serialize(aWriter);
            }
        }
    }

    // End of JSONClass

    public class JSONData : JSONNode
    {
        private string _data;

        public JSONData(string aData)
        {
            if (aData == null)
            {
                _data = "null";
                Tag = JSONBinaryTag.Null;
                return;
            }
            _data = aData;
            Tag = JSONBinaryTag.Value;
        }

        public JSONData(float aData)
        {
            AsFloat = aData;
        }

        public JSONData(double aData)
        {
            AsDouble = aData;
        }

        public JSONData(bool aData)
        {
            AsBool = aData;
        }

        public JSONData(int aData)
        {
            AsInt = aData;
        }

        public JSONData(long aData)
        {
            AsLong = aData;
        }

        public override string Value
        {
            get { return _data; }
            set
            {
                _data = value;
                Tag = JSONBinaryTag.Value;
            }
        }

        public override string ToString()
        {
            if (Tag == JSONBinaryTag.BoolValue ||
                Tag == JSONBinaryTag.IntValue ||
                Tag == JSONBinaryTag.LongValue ||
                Tag == JSONBinaryTag.FloatValue ||
                Tag == JSONBinaryTag.DoubleValue ||
                Tag == JSONBinaryTag.Null)
            {
                return Escape(_data);
            }
            var result = new StringBuilder("\"");
            result.Append(Escape(_data));
            result.Append("\"");
            return result.ToString();
        }

        public override string ToString(string aPrefix)
        {
            if (Tag == JSONBinaryTag.BoolValue ||
                Tag == JSONBinaryTag.IntValue ||
                Tag == JSONBinaryTag.LongValue ||
                Tag == JSONBinaryTag.FloatValue ||
                Tag == JSONBinaryTag.DoubleValue ||
                Tag == JSONBinaryTag.Null)
            {
                return Escape(_data);
            }
            var result = new StringBuilder("\"");
            result.Append(Escape(_data));
            result.Append("\"");
            return result.ToString();
        }

        public override void Serialize(BinaryWriter aWriter)
        {
            switch (Tag)
            {
                case JSONBinaryTag.Null:
                    aWriter.Write((byte)JSONBinaryTag.Null);
                    break;
                case JSONBinaryTag.LongValue:
                    aWriter.Write((byte)JSONBinaryTag.LongValue);
                    aWriter.Write(AsLong);
                    break;
                case JSONBinaryTag.IntValue:
                    aWriter.Write((byte)JSONBinaryTag.IntValue);
                    aWriter.Write(AsInt);
                    break;
                case JSONBinaryTag.FloatValue:
                    aWriter.Write((byte)JSONBinaryTag.FloatValue);
                    aWriter.Write(AsFloat);
                    break;
                case JSONBinaryTag.DoubleValue:
                    aWriter.Write((byte)JSONBinaryTag.DoubleValue);
                    aWriter.Write(AsDouble);
                    break;
                case JSONBinaryTag.BoolValue:
                    aWriter.Write((byte)JSONBinaryTag.BoolValue);
                    aWriter.Write(AsBool);
                    break;
                default:
                    aWriter.Write((byte)JSONBinaryTag.Value);
                    aWriter.Write(_data);
                    break;
            }
        }
    }

    // End of JSONData

    public static class JSON
    {
        public static JSONNode Parse(string jsonString)
        {
            return JSONNode.Parse(jsonString);
        }

        // Deserializes JSON string into a class.
        // Only shallow (single level) classes supported for now.
        public static T Deserialize<T>(string sJson)
            where T : new()
        {
            JSONNode nd = JSONNode.Parse(sJson);
            T obj = new T();
            int nPropsFound = 0;
            foreach (var prop in typeof(T).GetProperties())
            {
                JSONNode n = nd[prop.Name];
                if (n == null)
                    continue;

                nPropsFound++;

                // Null value, leave property as default created by constructor
                if (n.Tag == JSONBinaryTag.Null)
                    continue;

                // Nullable type?
                Type u = Nullable.GetUnderlyingType(prop.PropertyType);
                prop.SetValue(obj, Convert.ChangeType(n.Value, u ?? prop.PropertyType));
            }

            // No overlap between json and T
            if (nPropsFound == 0)
                throw new Exception($"JSON is not a representation of class {typeof(T).Name}");

            return obj;
        }

    }
}
