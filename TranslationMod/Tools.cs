using FuzzyString;
using System;
using System.Collections.Generic;

namespace TranslationMod
{
    public class FuzzyStringDictionary
    {
        private Dictionary<string, string> _dictionary;
        private List<string> memoryBuffer;
        public FuzzyStringDictionary(Dictionary<string, string> dictioanry)
        {
            _dictionary = dictioanry;
            memoryBuffer = new List<string>();
        }
        public FuzzyStringDictionary()
        {
            _dictionary = new Dictionary<string, string>();
            memoryBuffer = new List<string>();
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    if(!memoryBuffer.Contains(key))
                        return _dictionary[CompareKey(key)];
                    return "";
                }
                catch
                {
                    if(memoryBuffer.Count > 500)
                    {
                        memoryBuffer.RemoveAt(0);
                    }
                    memoryBuffer.Add(key);
                    return "";
                }
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
            foreach (var item in _dictionary)
            {
                if (source.Contains("@"))
                {
                    if (source != item.Key)
                        continue;
                }

                var strS = source.Split(' ');
                var strI = item.Key.Split(' ');
                if (strS.Length < strI.Length)
                    continue;
                string keyWord = "";
                string keyWordKey = "";
                int prevKeyWordIndex = -1;
                int j = 0;
                double tempScore = 0;
                double exceptKeyWordsScore = 0;
                for (int i = 0; i < strI.Length; i++)
                {

                    if (!string.IsNullOrEmpty(strI[i]) && strI[i][0] == '@')
                    {
                        //if (keyWord.Length && prevKeyWordIndex > -1)
                        //{
                        //      // FOUND KEYWORD, MAYBE REPLACED RIGHT HERE FROM KEYWORDS DICTIONARY!
                        //      // OR CREATE AN ARRAY OF EVERY KEYWORD AND REPLACE AT THE END. 1st case is better though
                        //}
                        keyWordKey = strI[i];
                        keyWord = strS[j];
                        j++;
                        prevKeyWordIndex = i;
                        continue;
                    }
                    else if (strI[i] == strS[j])
                    {
                        tempScore += strI[i].Length;
                        exceptKeyWordsScore += strS[i].Length;
                        prevKeyWordIndex = -1;
                        j++;
                    }
                    else
                    {
                        exceptKeyWordsScore += strS[i].Length;
                        keyWord += " " + strS[i];
                        j++;
                    }
                }

                //var tempScore = Convert.ToDouble((source.LongestCommonSubsequence(item.Key).Length) / Convert.ToDouble(Math.Min(source.Length, item.Key.Length)));
                if (tempScore > 0.80 && tempScore > score)
                {
                    score = tempScore;
                    resultString = item.Key;
                }
            }
            return resultString;
        }
    }
}
