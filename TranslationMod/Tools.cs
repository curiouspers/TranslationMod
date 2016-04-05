using FuzzyString;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TranslationMod
{
    public class FuzzyStringDictionary
    {
        private Dictionary<string, string> _dictionary;
        private Dictionary<string, string> memoryBuffer;
        public FuzzyStringDictionary(Dictionary<string, string> dictioanry)
        {
            _dictionary = dictioanry;
            memoryBuffer = new Dictionary<string, string>();
        }
        public FuzzyStringDictionary()
        {
            _dictionary = new Dictionary<string, string>();
            memoryBuffer = new Dictionary<string, string>();
        }

        public string this[string key]
        {
            get
            {
                //try
                //{
                if (!memoryBuffer.ContainsKey(key))
                {
                    var translate = _dictionary[CompareKey(key)];
                    if (memoryBuffer.Count > 500)
                    {
                        memoryBuffer.Remove(memoryBuffer.First().Key);
                    }
                    if (!key.Contains("@") && !memoryBuffer.ContainsKey(key))
                        memoryBuffer.Add(key, translate);
                    return translate;
                }
                else return memoryBuffer[key];
                //}
                //catch
                //{
                //    if(memoryBuffer.Count > 500)
                //    {
                //        memoryBuffer.Remove(memoryBuffer.First().Key);
                //    }
                //    memoryBuffer.Add(key,translate);
                //    return "";
                //}
            }
            set
            {
                _dictionary[CompareKey(key)] = value;
            }
        }

        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return _dictionary.Keys;
            }
        }

        public ICollection<string> Values
        {
            get
            {
                return _dictionary.Values;
            }
        }

        public void Add(string key, string value)
        {
            _dictionary.Add(key, value);
        }

        /// <summary>
        /// Ignore all duplicate keys
        /// </summary>
        /// <param name="pairs"></param>
        public void AddRange(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            foreach (var pair in pairs)
            {
                try
                {
                    _dictionary.Add(pair.Key, pair.Value);
                }
                catch (ArgumentException e) { }
            }
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool ContainsKey(string key)
        {
            if (memoryBuffer.ContainsKey(key))
                return true;
            var removeItem = CompareKey(key);
            if (string.IsNullOrEmpty(removeItem)) return false;
            else {
                if (!memoryBuffer.ContainsKey(key))
                    memoryBuffer.Add(key, removeItem);
                return true;
            }
        }

        public bool Remove(string key)
        {
            var removeItem = CompareKey(key);
            if (string.IsNullOrEmpty(removeItem)) return false;
            return _dictionary.Remove(removeItem);
        }

        private string CompareKey(string source)
        {
            if (memoryBuffer.ContainsKey(source))
                return memoryBuffer[source];

            bool NEW_APPROACH = true;
            double score = 0;
            string resultString = "";
            string resultingValue = "";
            string key = "";
            string[] strS;
            string[] strI;
            if (!NEW_APPROACH && source.Contains(Environment.NewLine))
            {
                source = source.Replace(Environment.NewLine, " " + Environment.NewLine + " ");
            }
            foreach (var item in _dictionary)
            {
                bool noMatch = false;
                key = item.Key;
                if (source.Contains("@"))
                {
                    if (source != item.Key)
                        continue;
                    else
                        return item.Key;
                }
                if (source.Contains(Environment.NewLine) && (!item.Key.Contains("@newline") && !item.Key.Contains(Environment.NewLine)) ||
                    Regex.Matches(source, Environment.NewLine).Count < Regex.Matches(item.Key, Environment.NewLine).Count)
                {
                    continue;
                }
                double tempScore = 0;
                double keyWordsScore = 0;

                if (NEW_APPROACH)
                {
                    // new approach ?
                    List<KeyValuePair<string, string>> kvs = TranslationMod.GetKeysValue(key, source);
                    string exceptKeysKey = key;
                    string exceptKeysSource = source;
                    if (kvs.Count == 0 || string.IsNullOrEmpty(kvs[0].Value))
                        continue;

                    foreach (var kv in kvs)
                    {
                        if (kv.Key == "@number" && Tools.reNumber.Match(kv.Value).ToString() == "")
                        {
                            noMatch = true;
                            break;
                        }
                        exceptKeysKey = exceptKeysKey.Replace(kv.Key, "");
                        exceptKeysSource = exceptKeysSource.Replace(kv.Value, "");
                        keyWordsScore += kv.Value.Length;
                    }
                    if (noMatch)
                        continue;
                    double newExceptKeyWordsScore = 0;
                    newExceptKeyWordsScore = exceptKeysKey.Length;
                    tempScore = exceptKeysKey.Length / exceptKeysSource.Length + newExceptKeyWordsScore / 10;
                }
                else {

                    strS = source.Split(' ');
                    if (source.Contains(Environment.NewLine))
                        strI = key.Replace(Environment.NewLine, " " + Environment.NewLine + " ").Split(' ');
                    else
                        strI = key.Split(' ');

                    if (strS.Length < strI.Length)
                        continue;
                    string keyWord = "";
                    string keyWordKey = "";
                    int prevKeyWordIndex = -1;
                    int j = 0;
                    double exceptKeyWordsScore = 0;
                    for (int i = 0; i < strS.Length; i++)
                    {

                        if (i < strI.Length && !string.IsNullOrEmpty(strI[i]) && strI[i].Contains('@'))
                        {

                            keyWordKey = strI[i];
                            keyWord = strS[j];

                            if (string.IsNullOrEmpty(keyWord) || keyWordKey[0] != '@' && keyWordKey[0] != keyWord[0])
                                break;

                            var tmp = new string[0];
                            if (keyWordKey.Contains("@key") && keyWordKey != "@key")
                                tmp = keyWordKey.Split(new string[] { "@key" }, StringSplitOptions.None);

                            if (tmp.Length == 0 && keyWordKey.Contains("@number") && keyWordKey != "@number")
                                tmp = keyWordKey.Split(new string[] { "@number" }, StringSplitOptions.None);

                            if (tmp.Length > 1 && i + 1 < strI.Length && strI[i + 1] == strS[j + 1])
                            {
                                if (string.IsNullOrEmpty(tmp[0]) && !string.IsNullOrEmpty(tmp[1]))
                                {
                                    var ind = keyWord.IndexOf(tmp[1]);
                                    if (ind > 0 && keyWord.Length == ind + tmp[1].Length)
                                    {
                                        keyWord = keyWord.Substring(0, keyWord.Length - tmp[1].Length);
                                        tempScore += tmp[1].Length;
                                    }
                                    else
                                    {
                                        tempScore -= tmp[1].Length;
                                    }
                                }
                            }
                            if (tmp.Length == 3 && string.IsNullOrEmpty(tmp[1]) && !string.IsNullOrEmpty(tmp[0]) && !string.IsNullOrEmpty(tmp[2]))
                            {
                                Console.WriteLine("hey2");
                            }
                            if (tmp.Length == 2 && string.IsNullOrEmpty(tmp[1]) && !string.IsNullOrEmpty(tmp[0]))
                            {
                                Console.WriteLine("hey3");
                            }

                            keyWordsScore += keyWord.Length;
                            j++;
                            prevKeyWordIndex = i;
                            continue;
                        }
                        else if (i < strI.Length && strI[i] == strS[j])
                        {
                            tempScore += strI[i].Length;
                            if (i > 0)
                            {
                                tempScore++;
                            }
                            //exceptKeyWordsScore += strS[i].Length;
                            prevKeyWordIndex = -1;
                            j++;
                        }
                        else if (prevKeyWordIndex > 0)
                        {
                            exceptKeyWordsScore += strS[j].Length;
                            keyWordsScore += strS[j].Length + 1;
                            keyWord += " " + strS[j];
                            j++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    tempScore /= (source.Length - keyWordsScore);
                }
                source = source.Replace(" " + Environment.NewLine + " ", Environment.NewLine);

                //var tempScore = Convert.ToDouble((source.LongestCommonSubsequence(item.Key).Length) / Convert.ToDouble(Math.Min(source.Length, item.Key.Length)));
                if (tempScore >= 0.75 && tempScore > score)
                {
                    score = tempScore;
                    resultString = item.Key;
                    resultingValue = item.Value;
                }
            }

            return resultString;
        }

        public KeyValuePair<string, string> getKeyValue(string key)
        {
            if (memoryBuffer.ContainsKey(key))
                return new KeyValuePair<string, string>(memoryBuffer[key], _dictionary[memoryBuffer[key]]);
            else
                return new KeyValuePair<string, string>(_dictionary[CompareKey(key)], _dictionary[memoryBuffer[key]]);
        }
    }

    public static class Tools
    {
        public static Regex reNumber = new Regex("[\\d +-.,]+", RegexOptions.Compiled);
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

    }
}
