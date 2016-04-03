using FuzzyString;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public void AddRange(IEnumerable<KeyValuePair<string,string>> pairs)
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
            else return true;
        }

        public bool Remove(string key)
        {
            var removeItem = CompareKey(key);
            if (string.IsNullOrEmpty(removeItem)) return false;
            return _dictionary.Remove(removeItem);
        }

        private string CompareKey(string source)
        {
            double score = 0;
            string resultString = "";
            string key = "";
            string[] strS;
            string[] strI;
            if (source.Contains(Environment.NewLine))
            {
                source = source.Replace(Environment.NewLine, " " + Environment.NewLine + " ");
            }
            foreach (var item in _dictionary)
            {
                key = item.Key;
                if (source.Contains("@"))
                {
                    if (source != item.Key)
                        continue;
                    else
                        return item.Key;
                }
                if (source.Contains(Environment.NewLine) && !item.Key.Contains("@newline"))
                {
                    continue;
                }
                strS = source.Split(' ');
                strI = key.Split(' ');
                
                if (strS.Length < strI.Length)
                    continue;
                string keyWord = "";
                string keyWordKey = "";
                int prevKeyWordIndex = -1;
                int j = 0;
                double tempScore = 0;
                double exceptKeyWordsScore = 0;
                double keyWordsScore = 0;
                for (int i = 0; i < strS.Length; i++)
                {

                    if (i < strI.Length && !string.IsNullOrEmpty(strI[i]) && strI[i].Contains('@'))
                    {
                        //if (keyWord.Length && prevKeyWordIndex > -1)
                        //{
                        //      // FOUND KEYWORD, MAYBE REPLACED RIGHT HERE FROM KEYWORDS DICTIONARY!
                        //      // OR CREATE AN ARRAY OF EVERY KEYWORD AND REPLACE AT THE END. 1st case is better though
                        //      _dictionary[item.Key] = "hey";
                        //}
                        keyWordKey = strI[i];
                        keyWord = strS[j];
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
                        keyWordsScore += strS[j].Length+1;
                        keyWord += " " + strS[j];
                        j++;
                    } else
                    {
                        break;
                    }
                }
                tempScore /= (source.Length - keyWordsScore);

                //var tempScore = Convert.ToDouble((source.LongestCommonSubsequence(item.Key).Length) / Convert.ToDouble(Math.Min(source.Length, item.Key.Length)));
                if (tempScore > 0.80 && tempScore > score)
                {
                    score = exceptKeyWordsScore;
                    resultString = item.Key;
                }
            }
            return resultString;
        }
    }
}
