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

            bool NEW_APPROACH = false;
            double score = 0;
            string resultString = "";
            string resultingValue = "";
            string key = "";
            string[] strS;
            string[] strI;

            string nl = Environment.NewLine;
            string nlnl = nl + nl;

            //if (!NEW_APPROACH && source.Contains(nl))
            //{
            //    source = source.Replace(nl, " " + nl + " ");
            //}
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

                if (item.Key.IndexOf("@") > 0 && item.Key[0] != source[0])
                    continue;

                if (source.Contains(nl) && (!item.Key.Contains("@newline") && !item.Key.Contains(nl)))
                    //|| Regex.Matches(source, nl).Count < Regex.Matches(item.Key, nl).Count)
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

                    if (source.IndexOf(nlnl) != source.LastIndexOf(nlnl) && source.Split(new string[] { nlnl }, StringSplitOptions.None).Length - 1 == 2 )
                    {
                        return "@key"+nlnl+"@key"+nlnl+"@key";
                    }

                    strS = source.Replace(nl, " " + nl + " ").Split(' ');
                    if (source.Contains(nl))
                        strI = key.Replace(nl, " " + nl + " ").Split(' ');
                    else
                        strI = key.Split(' ');

                    if (strS.Length < strI.Length)
                        continue;
                    string keyWord = "";
                    string keyWordKey = "";
                    int prevKeyWordIndex = -1;
                    int j = 0;
                    double exceptKeyWordsScore = 0;
                    int curKeyIndex = 0;
                    int curSourceIndex = 0;
                    for (int i = 0; i < strS.Length; i++)
                    {
                        if (i != 0)
                        {
                            curKeyIndex++;
                            curSourceIndex++;
                        }

                        if (strI[j] == strS[i])
                        {
                            tempScore += strI[j].Length;
                            if (i > 0)
                            {
                                tempScore++;
                            }
                            //exceptKeyWordsScore += strS[i].Length;
                            prevKeyWordIndex = -1;

                            curKeyIndex += strI[j].Length;
                            curSourceIndex += strS[i].Length;
                            j++;
                            if (j >= strI.Length) j = strI.Length - 1;
                            continue;
                        } else
                        {
                            if (//j < strI.Length && strI[j].Contains("@") && j + 1 < strI.Length && i + 1 < strS.Length && strI[j + 1].IndexOf("@") == -1 && source.Substring(curSourceIndex).IndexOf(strI[j + 1]) == -1 ||
                                //j < strI.Length && prevKeyWordIndex > -1 && j < strI.Length && i < strS.Length && strI[j].IndexOf("@") == -1 && source.Substring(curSourceIndex).IndexOf(strI[j]) == -1 ||
                                strS[i].Length>0 && j < strI.Length && strI[j].IndexOf("@number") > -1 && !Tools.numberArr.Contains(strS[i][0]))
                            {
                                prevKeyWordIndex = -1;
                                break;
                            }
                        }

                        if (j < strI.Length && !string.IsNullOrEmpty(strI[j]) && strI[j].Contains('@'))
                        {

                            keyWordKey = strI[j];
                            keyWord = strS[i];

                            if (string.IsNullOrEmpty(keyWord) || keyWordKey[0] != '@' && keyWordKey[0] != keyWord[0])
                            {
                                prevKeyWordIndex = -1;
                                break;
                            }

                            var tmp = new string[0];
                            if (keyWordKey.Contains("@key") && keyWordKey != "@key")
                                tmp = keyWordKey.Split(new string[] { "@key" }, StringSplitOptions.None);

                            if (tmp.Length == 0 && keyWordKey.Contains("@number") && keyWordKey != "@number")
                                tmp = keyWordKey.Split(new string[] { "@number" }, StringSplitOptions.None);

                            //if (tmp.Length > 1 && j + 1 < strI.Length && i + 1 < strS.Length && strI[j + 1] == strS[i + 1])
                            if (tmp.Length == 2 && !string.IsNullOrEmpty(tmp[1]) && string.IsNullOrEmpty(tmp[0]))
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
                            curKeyIndex += strI[j].Length;
                            curSourceIndex += strS[i].Length;

                            prevKeyWordIndex = j;
                            if (i+1 < strI.Length && strI[j+1] == strS[i+1])
                                j++;
                            if (j >= strI.Length) j = strI.Length-1;
                            continue;
                        }
                        else if (j < strI.Length && strI[j] == strS[i])
                        {
                            tempScore += strI[j].Length;
                            if (i > 0)
                            {
                                tempScore++;
                            }
                            //exceptKeyWordsScore += strS[i].Length;
                            prevKeyWordIndex = -1;

                            curKeyIndex += strI[j].Length;
                            curSourceIndex += strS[i].Length;
                            j++;
                            if (j >= strI.Length) j = strI.Length-1;
                        }
                        else if (prevKeyWordIndex > -1)
                        {
                            exceptKeyWordsScore += strS[i].Length;
                            keyWordsScore += strS[i].Length + 1;
                            keyWord += " " + strS[i];


                            // If word after key do not match AND this was a last @key,
                            // we take last part of item.Key and if it is not substing of source, then 
                            // this is not the droids we are looking for
                            if (//i+1 < strI.Length && j+1 < strS.Length && strI[j+1].IndexOf("@") == -1 && source.IndexOf(strI[j+1]) == -1 ||
                                curKeyIndex < item.Key.Length && item.Key.LastIndexOf("@") < curKeyIndex && source.IndexOf(item.Key.Substring(curKeyIndex)) == -1)
                            {
                                prevKeyWordIndex = -1;
                                break;
                            }
                            
                            curKeyIndex += strI[j].Length;
                            curSourceIndex += strS[i].Length;
                            j++;
                            if (j >= strI.Length) j = strI.Length-1;
                        }
                        else
                        {
                            prevKeyWordIndex = -1;
                            break;
                        }
                    }
                    tempScore /= (source.Length - keyWordsScore);
                }
                //source = source.Replace(" " + nl + " ", nl);

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

        public KeyValuePair<string, string> GetKeyValue(string key)
        {
            if (!memoryBuffer.ContainsKey(key))
            {
                memoryBuffer.Add(key, CompareKey(key));
            }
            return new KeyValuePair<string, string>(memoryBuffer[key], _dictionary[memoryBuffer[key]]);
        }
    }

    public static class Tools
    {
        public static Regex reNumber = new Regex("[\\d +-.,]+", RegexOptions.Compiled);
        public static char[] numberArr = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '+', '-', '.', ',' };
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
