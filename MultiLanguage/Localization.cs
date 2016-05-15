﻿using Cyriller;
using Cyriller.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiLanguage
{
    public class Localization
    {
        public Config Config { get; private set; }
        public Dictionary<string, string> Characters { get; set; }

        private JObject _dataRandName { get; set; }
        private Dictionary<string, int> _languages;
        private Dictionary<string, string> _languageDescriptions;
        private List<KeyValuePair<string, string>> _nameEnToRu = new List<KeyValuePair<string, string>>();
        private List<KeyValuePair<string, string>> _nameRuToEn = new List<KeyValuePair<string, string>>();
        private static FuzzyStringDictionary _fuzzyDictionary;
        private string _currentLanguage;
        private bool _isMenuDrawing;
        private static Regex reToSkip = new Regex("^[0-9: -=.g]+$", RegexOptions.Compiled);
        private CyrPhrase cyrPhrase;
        private int IsTranslated;
        private Dictionary<string, string> _memoryBuffer;
        private List<string> _translatedStrings;
        private int _characterPosition;
        private bool _isGameLoaded = false;
        private bool _isKeyReplaced = false;
        private int _currentUpdate = 0;
        private int _updatesBeforeReplace = 60;
        private string currentName;
        private string PathOnDisk;

        public Localization()
        {
            PathOnDisk = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configLocation = Path.Combine(PathOnDisk, "languages", "Config.json");
            if (!File.Exists(configLocation))
            {
                Config = new Config();
                Config.LanguageName = "EN";
                File.WriteAllBytes(configLocation, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Config)));
            }
            else
            {
                Config = JsonConvert.DeserializeObject<Config>(Encoding.UTF8.GetString(File.ReadAllBytes(configLocation)));
            }
            LoadDictionary();
        }

        public void OnWindowsSizeChanged()
        {
            if (_isMenuDrawing) _isMenuDrawing = false;
        }

        public void OnUpdate()
        {
            if (!string.IsNullOrEmpty(Game1.player.Name) && currentName != Game1.player.Name)
            {
                if (_isGameLoaded && !_isKeyReplaced)
                {
                    if (_currentUpdate > _updatesBeforeReplace)
                    {
                        currentName = Game1.player.Name;
                        KeyReplace(Game1.player.Name, Game1.player.farmName);

                        // uncomment to test
                        /*
                        var name = Game1.player.Name;
                        var nameEn = translitName(name, true);
                        var nameEnRu = translitName(nameEn);
                        Console.WriteLine(name + " " + nameEn + " " + nameEnRu);

                        var farmName = Game1.player.farmName;
                        var farmNameEn = translitName(farmName, true);
                        var farmNameEnRu = translitName(farmNameEn);
                        Console.WriteLine(farmName + " " + farmNameEn + " " + farmNameEnRu);

                        name = Game1.player.favoriteThing;
                        nameEn = translitName(name, true);
                        nameEnRu = translitName(nameEn);
                        Console.WriteLine(name + " " + nameEn + " " + nameEnRu);

                        for (int i = 0; i < 10000; i++)
                        {
                            name = StardewValley.Dialogue.randomName();
                            var newName = translitName(name);
                            var newEnName = translitName(newName, true);
                            var newRuName = translitName(newEnName);
                            if (newName != newRuName)
                                Console.WriteLine(name + " " + newName + " " + newEnName + " " + newRuName);
                        }


                        var n = StardewValley.Utility.getOtherFarmerNames();
                        Console.WriteLine(n[0] + " " + Translate(n[0]));
                        Console.WriteLine(n[1] + " " + Translate(n[1]));
                        Console.WriteLine(n[2] + " " + Translate(n[2]));
                         */

                    } else
                    {
                        _currentUpdate++;
                    }
                }
            }
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is GameMenu)
            {
                var menu = Game1.activeClickableMenu as GameMenu;
                var pages = Tools.GetInstanceField(typeof(GameMenu), menu, "pages") as List<IClickableMenu>;


                var optionPage = pages.FirstOrDefault(p => p is OptionsPage);
                if (optionPage != null)
                {
                    var options = Tools.GetInstanceField(typeof(OptionsPage), optionPage, "options") as List<OptionsElement>;
                    if (!_isMenuDrawing)
                    {
                        var newOptions = new List<OptionsElement>();
                        var dropdownoption = options.Find(o => o is OptionsDropDown);
                        foreach (var option in options)
                        {
                            if (option.label == "Sound:")
                            {
                                var languageDropDown = new OptionsDropDown("Language", 55);
                                languageDropDown.selectedOption = _languages[_currentLanguage];
                                newOptions.Add(languageDropDown);
                            }
                            newOptions.Add(option);
                        }
                        Tools.SetInstanceField(typeof(OptionsPage), optionPage, "options", newOptions);
                        _isMenuDrawing = true;
                    }
                }
            }
            else if (Game1.activeClickableMenu == null)
            {
                if (_isMenuDrawing)
                    _isMenuDrawing = false;
            }
        }

        public void OnChangeLanguage(int which, int selection, List<string> option)
        {
            if (which == 55)
            {
                var selectedLang = _languageDescriptions[option[selection]];
                if (selectedLang != _currentLanguage)
                {
                    Config.LanguageName = selectedLang;
                    _currentLanguage = selectedLang;
                    //Game1.showGlobalMessage("This change will not take effect until you restart the game");
                }
            }
        }

        public void OnSetDropDownPropertyValue(OptionsDropDown dropDown)
        {
            if (dropDown.whichOption == 55)
            {
                dropDown.dropDownOptions = _languageDescriptions.Keys.ToList();
            }
        }

        public void OnGameLoaded()
        {
            _isGameLoaded = true;
        }

        public string OnGetRandomName()
        {
            if (Config.LanguageName != "EN")
            {
                return randomName();
            }
            else return "";
        }

        public List<string> OnGetOtherFarmerNames()
        {
            if (Config.LanguageName != "EN")
            {
                return getOtherFarmerNames();
            }
            else return null;
        }

        public string OnParseText(string text, SpriteFont whichFont, int width)
        {
            if (Config.LanguageName != "EN")
            {
                var _text = text;
                if (Environment.NewLine != "\n" && text.Contains("\n") && !text.Contains(Environment.NewLine))
                    _text = text.Replace("\n", Environment.NewLine);
                var translateMessage = Translate(_text);
                if (!string.IsNullOrEmpty(translateMessage))
                {
                    _text = translateMessage;
                }

                if (_text == null)
                {
                    return string.Empty;
                }
                string str1 = string.Empty;
                string str2 = string.Empty;
                string str3 = _text;
                foreach (string str4 in str3.Split(' '))
                {
                    if (whichFont.MeasureString(str1 + str4).Length() > width ||
                        str4.Equals(Environment.NewLine))
                    {
                        str2 = str2 + str1 + Environment.NewLine;
                        str1 = string.Empty;
                    }
                    str1 = str1 + str4 + " ";
                }
                return str2 + str1;
            }
            else return string.Empty;
        }

        public void OnDrawStringSpriteText(SpriteTextDrawStringEvent @event)
        {
            if (Config.LanguageName != "EN")
            {
                if (_translatedStrings.Contains(@event.Text))
                    return;
                var originalText = @event.Text;
                var translateText = @event.Text;
                if (Characters.ContainsKey(@event.Text))
                {
                    translateText = Characters[@event.Text];
                    if (!_translatedStrings.Contains(translateText)) _translatedStrings.Add(translateText);
                }
                else
                {
                    var translateMessage = Translate(@event.Text);
                    if (!string.IsNullOrEmpty(translateMessage))
                    {
                        translateText = translateMessage;
                    }
                }
                if (originalText.Length > @event.CharacterPosition || @event.CharacterPosition == 999999)
                {
                    _characterPosition = @event.CharacterPosition;
                }
                else if (_characterPosition < translateText.Length)
                {
                    _characterPosition++;
                }
                drawString(@event.Sprite, translateText, @event.X, @event.Y, _characterPosition,
                    @event.Width, @event.Height, @event.Alpha, @event.LayerDepth, @event.JunimoText,
                    @event.DrawBGScroll, @event.PlaceHolderScrollWidthText, @event.Color);
                @event.ReturnEarly = true;
            }
        }

        public int OnGetWidthSpriteText(string text)
        {
            if (Config.LanguageName != "EN")
            {
                if (IsTranslated > 0)
                {
                    IsTranslated = 0;
                    return -1;
                }
                var translateMessage = Translate(text, false);

                if (!string.IsNullOrEmpty(translateMessage))
                {
                    IsTranslated++;
                    return SpriteText.getWidthOfString(translateMessage);
                }
                else if (Characters.ContainsKey(text))
                {
                    IsTranslated++;
                    return SpriteText.getWidthOfString(Characters[text]);
                }
                else return -1;
            }
            else return -1;
        }

        public string OnSpriteBatchDrawString(string message)
        {
            if (Config.LanguageName != "EN")
            {
                if (_translatedStrings.Contains(message))
                    return message;
                var translateMessage = Translate(message);
                return translateMessage;
            }
            else return string.Empty;
        }

        public string OnSpriteFontMeasureString(string message)
        {
            if (Config.LanguageName != "EN")
            {
                if (reToSkip.IsMatch(message) || string.IsNullOrEmpty(message))
                    return string.Empty;
                if (_translatedStrings.Contains(message))
                    return message;
                var translateMessage = Translate(message);
                return translateMessage;
            }
            else return string.Empty;
        }

        public List<string> OnStringBrokeIntoSections(string letter, int width, int height)
        {
            if (Config.LanguageName != "EN")
            {
                if (!_isKeyReplaced)
                {
                    KeyReplace(Game1.player.Name, Game1.player.farmName);
                }
                var translateMessage = Translate(letter);

                if (!string.IsNullOrEmpty(translateMessage))
                {
                    List<string> list = new List<string>();
                    var s = translateMessage;
                    for (; s.Length > 0; s = s.Substring(list.Last().Length))
                    {
                        string thisHeightCutoff = SpriteText.getStringPreviousToThisHeightCutoff(s, width, height);
                        if (thisHeightCutoff.Length > 0)
                            list.Add(thisHeightCutoff);
                        else
                            break;
                    }
                    return list;
                }
                else return null;
            }
            else return null;
        }

        public string OnSparklingTextCallback(string text)
        {
            if (Config.LanguageName != "EN")
            {
                var translateMessage = Translate(text);
                if (!string.IsNullOrEmpty(translateMessage))
                {
                    return translateMessage;
                }
            }
            return string.Empty;
        }

        private string Translate(string message, bool needTrim = true)
        {
            if (Config.LanguageName != "EN")
            {
                if (needTrim)
                    message = message.Trim();

                if (string.IsNullOrEmpty(message) || reToSkip.IsMatch(message) || _translatedStrings.Contains(message.Replace("\r\n", "").Replace("\n", "")))
                {
                    return message;
                }
                if (_memoryBuffer.ContainsKey(message))
                {
                    if (!_memoryBuffer[message].IsNullOrEmpty())
                        return _memoryBuffer[message];
                    else
                        return message;
                }
                if (_fuzzyDictionary.ContainsKey(message))
                {
                    var resultTranslate = message;
                    var fval = _fuzzyDictionary.GetFuzzyKeyValue(message);

                    var tempFKey = fval.Key;
                    var tempFValue = fval.Value;

                    if (tempFValue.Contains("^") && !tempFKey.Contains("^"))
                    {
                        if (Game1.player.IsMale)
                        {
                            tempFValue = tempFValue.Split('^')[0];
                        }
                        else tempFValue = tempFValue.Split('^')[1];
                    }

                    if (tempFValue.Contains("/"))
                    {
                        var genderSplit = tempFValue.Split(' ', ',', '.', '!', '?', '<', '=', '-', ':', '^')
                            .Where(s => s.Contains('/'))
                            .Select(s => new KeyValuePair<string, string>(s, Game1.player.IsMale ? s.Split('/')[0] : s.Split('/')[1]));
                        foreach (var gend in genderSplit)
                        {
                            tempFValue = tempFValue.Replace(gend.Key, gend.Value);
                        }
                    }
                    if (tempFValue.Contains("|"))
                    {
                        NPC npc = null;
                        if (Game1.currentSpeaker != null)
                        {
                            npc = Game1.currentSpeaker;
                        }
                        else if (string.IsNullOrEmpty(Game1.player.spouse))
                        {
                            npc = Game1.getCharacterFromName(Game1.player.spouse);
                        }
                        if (npc != null)
                        {
                            var genderSplit = tempFValue.Split(' ', ',', '.', '!', '?', '<', '=', '-', ':', '^')
                                .Where(s => s.Contains('|'))
                                .Select(s => new KeyValuePair<string, string>(s, npc.gender == 1 ? s.Split('|')[1] : s.Split('|')[0]));
                            foreach (var gend in genderSplit)
                            {
                                tempFValue = tempFValue.Replace(gend.Key, gend.Value);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(tempFKey) && !string.IsNullOrEmpty(tempFValue))
                    {
                        if (tempFKey.Contains("@"))
                        {
                            var diff = GetKeysValue(tempFKey, message);
                            resultTranslate = StringFormatWithKeys(tempFValue, diff.Select(d => d.Value).ToList());
                        }
                        else
                        {
                            resultTranslate = tempFValue;
                        }
                    }

                    if (_memoryBuffer.Count > 500)
                    {
                        //_memoryBuffer.Remove(_memoryBuffer.First().Key);
                        _memoryBuffer.Remove(_memoryBuffer.ElementAt(Tools.rand.Next(0, _memoryBuffer.Count)).Key);
                    }
                    if (!_memoryBuffer.ContainsKey(message))
                    {
                        _memoryBuffer.Add(message, resultTranslate);
                    }
                    if (!_translatedStrings.Contains(resultTranslate))
                    {
                        _translatedStrings.Add(resultTranslate);
                        if (_translatedStrings.Count > 500)
                            _translatedStrings.RemoveAt(0);
                    }
                    return resultTranslate;
                }
                else
                {
                    if (!_memoryBuffer.ContainsKey(message))
                    {
                        _memoryBuffer.Add(message, string.Empty);
                        if (_memoryBuffer.Count > 500)
                        {
                            //_memoryBuffer.Remove(_memoryBuffer.First().Key);
                            _memoryBuffer.Remove(_memoryBuffer.ElementAt(Tools.rand.Next(0, _memoryBuffer.Count)).Key);
                        }
                    }
                    return message;
                }
            }
            return "";
        }

        private static List<KeyValuePair<string, string>> GetKeysValue(string template, string str)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            string pattern = Regex.Escape(template);
            MatchCollection matches;
            int count = 0;
            while ((matches = Regex.Matches(pattern, "@key|@number|@farm|@player|@playerChild")).Count != 0)
            {
                pattern = pattern.Remove(matches[0].Index, matches[0].Length);
                pattern = pattern.Insert(matches[0].Index, "(.+?)");
                result.Add(new KeyValuePair<string, string>(matches[0].Value, ""));
                count++;
            }

            pattern += "$";
        TryAgain:
            Regex r = new Regex(pattern, RegexOptions.Singleline);
            Match m = r.Match(str);
            for (int i = 1; i < m.Groups.Count; i++)
            {
                var key = result[i - 1].Key;
                result[i - 1] = new KeyValuePair<string, string>(key, m.Groups[i].Value);
                //if we have (.+?)\ (.+?) in pattern, on second (.+?) we need to this check:
                if (pattern.IndexOf(@"(.+?)\ (.+?)") > -1 && !reToSkip.IsMatch(m.Groups[i].Value) && !_fuzzyDictionary.ContainsKey(m.Groups[i].Value)) {
                    // change first "(.+?)" to "(.+?\ .+?)" and start from begining of cycle
                    pattern = pattern.ReplaceFirst("(.+?)", "(.+? .+?)");
                    goto TryAgain;
                }
            }
            return result;
        }

        private string StringFormatWithKeys(string format, List<string> args)
        {
            string result = format;
            MatchCollection matches;
            int i = 0;
            while ((matches = Regex.Matches(result, "@key[RDVTP]{0,1}|@number|@farm|@player|@playerChild")).Count != 0)
            {
                var value = args[i];
                if (matches[0].Value.Contains("@key"))
                {
                    if (i == 2 &&
                        format == "@key" + Environment.NewLine + Environment.NewLine + "@key" + Environment.NewLine + Environment.NewLine + "@key"
                        && value.Contains(Environment.NewLine))
                    {
                        string newValue = "";
                        foreach (var item in value.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                        {
                            newValue += Translate(item) + Environment.NewLine;
                        }
                        value = newValue.Substring(0, newValue.Length - Environment.NewLine.Length);
                    }
                    else {
                        var tmp = Translate(value);
                        if (!string.IsNullOrEmpty(tmp))
                            value = tmp;
                    }

                    if (matches[0].Value.Length == 5 && Config.LanguageName == "RU")
                    {
                        value = Decline(value, matches[0].Value.Last().ToString());
                    }
                }
                result = result.Remove(matches[0].Index, matches[0].Length);
                result = result.Insert(matches[0].Index, value);
                i++;
            }
            return result;
        }

        private string Decline(string message, string _case)
        {
            try
            {

                var result = cyrPhrase.Decline(message, GetConditionsEnum.Similar);
                string res = "";
                switch (_case)
                {
                    case "R":
                        res = result.Genitive;
                        break;
                    case "D":
                        res = result.Dative;
                        break;
                    case "V":
                        res = result.Accusative;
                        break;
                    case "T":
                        res = result.Instrumental;
                        break;
                    case "P":
                        res = result.Prepositional;
                        break;
                    case "N":
                    default:
                        res = result.Nominative;
                        break;
                }
                return res;
            }
            catch (Exception)
            {
                Console.WriteLine("Decline Exception, try to add a word " + message);
                return message;
                throw;
            }
        }

        private void AddToDictionary(string key, string value)
        {
            if (key != "__comment")
            {
                _fuzzyDictionary.Add(key.Trim(), value);
            }
        }

        private void KeyReplace(string playerName, string farm)
        {
            // we need to cache the keys to update since we can't
            // modify the collection during enumeration
            var keysToUpdate = new List<string>();
            foreach (var row in _fuzzyDictionary)
            {
                if (row.Key.Contains("@player") || row.Key.Contains("@farm"))// || row.Key.Contains("%farm") || row.Key.Contains("@"))
                {
                    keysToUpdate.Add(row.Key);
                }
            }
            foreach (var keyToUpdate in keysToUpdate)
            {
                var value = _fuzzyDictionary[keyToUpdate];

                var newKey = keyToUpdate.Replace("@player", playerName).Replace("@farm", farm);//.Replace("%farm", farm).Replace("@", playerName);
                var newValue = value.Replace("@player", playerName).Replace("@farm", farm);//.Replace("%farm", farm).Replace("@", playerName);

                _fuzzyDictionary.Remove(keyToUpdate);
                _fuzzyDictionary.Add(newKey, newValue);
            }

            _isKeyReplaced = true;
        }

        private string translitName(string name, bool RuToEn = false)
        {
            var newName = name;
            if (!RuToEn)
            {
                for (int i = _nameEnToRu.Count()-1; i >= 0; i--)
                {
                    var key = _nameEnToRu[i].Key;
                    var val = _nameEnToRu[i].Value;
                    if (newName.Contains(key))
                    {
                        newName = newName.Replace(key, val);
                    }
                }
            } else
            {
                for (int i = _nameRuToEn.Count() - 1; i >= 0; i--)
                {
                    var key = _nameRuToEn[i].Key;
                    var val = _nameRuToEn[i].Value;
                    if (newName.Contains(key))
                    {
                        newName = newName.Replace(key, val);
                    }
                }
            }
            return newName;
        }

        private string randomName()
        {
            string str;
            string str1 = "";
            int num = Game1.random.Next(3, 6);
            string[] strArrays = new string[] { _dataRandName["0"]["B"].ToString(),_dataRandName["0"]["Br"].ToString(), _dataRandName["0"]["J"].ToString(),
                _dataRandName["0"]["F"].ToString(), _dataRandName["0"]["S"].ToString(), _dataRandName["0"]["M"].ToString(), _dataRandName["0"]["C"].ToString(),
                _dataRandName["0"]["Ch"].ToString(), _dataRandName["0"]["L"].ToString(), _dataRandName["0"]["P"].ToString(), _dataRandName["0"]["K"].ToString(),
                _dataRandName["0"]["W"].ToString(), _dataRandName["0"]["G"].ToString(), _dataRandName["0"]["Z"].ToString(), _dataRandName["0"]["Tr"].ToString(),
                _dataRandName["0"]["T"].ToString(), _dataRandName["0"]["Gr"].ToString(), _dataRandName["0"]["Fr"].ToString(), _dataRandName["0"]["Pr"].ToString(),
                _dataRandName["0"]["N"].ToString(), _dataRandName["0"]["Sn"].ToString(), _dataRandName["0"]["R"].ToString(), _dataRandName["0"]["Sh"].ToString(),
                _dataRandName["0"]["St"].ToString() };
            string[] strArrays1 = strArrays;
            string[] strArrays2 = new string[] { _dataRandName["1"]["ll"].ToString(), _dataRandName["1"]["tch"].ToString(), _dataRandName["1"]["l"].ToString(),
                _dataRandName["1"]["m"].ToString(), _dataRandName["1"]["n"].ToString(), _dataRandName["1"]["p"].ToString(), _dataRandName["1"]["r"].ToString(),
                _dataRandName["1"]["s"].ToString(), _dataRandName["1"]["t"].ToString(), _dataRandName["1"]["c"].ToString(), _dataRandName["1"]["rt"].ToString(),
                _dataRandName["1"]["ts"].ToString() };
            string[] strArrays3 = strArrays2;
            string[] strArrays4 = new string[] { _dataRandName["2"]["a"].ToString(), _dataRandName["2"]["e"].ToString(), _dataRandName["2"]["i"].ToString(),
                _dataRandName["2"]["o"].ToString(), _dataRandName["2"]["u"].ToString() };
            string[] strArrays5 = strArrays4;
            string[] strArrays6 = new string[] { _dataRandName["3"]["ie"].ToString(), _dataRandName["3"]["o"].ToString(), _dataRandName["3"]["a"].ToString(),
                _dataRandName["3"]["ers"].ToString(), _dataRandName["3"]["ley"].ToString() };
            string[] strArrays7 = strArrays6;
            Dictionary<string, string[]> strs = new Dictionary<string, string[]>();
            Dictionary<string, string[]> strs1 = new Dictionary<string, string[]>();
            string[] strArrays8 = new string[] { _dataRandName["4"]["nie"].ToString(), _dataRandName["4"]["bell"].ToString(), _dataRandName["4"]["bo"].ToString(),
                _dataRandName["4"]["boo"].ToString(), _dataRandName["4"]["bella"].ToString(), _dataRandName["4"]["s"].ToString() };
            strs.Add(_dataRandName["2"]["a"].ToString(), strArrays8);
            string[] strArrays9 = new string[] { _dataRandName["5"]["ll"].ToString(), _dataRandName["5"]["llo"].ToString(), _dataRandName["5"][""].ToString(),
                _dataRandName["5"]["o"].ToString() };
            strs.Add(_dataRandName["2"]["e"].ToString(), strArrays9);
            string[] strArrays10 = new string[] { _dataRandName["6"]["ck"].ToString(), _dataRandName["6"]["e"].ToString(), _dataRandName["6"]["bo"].ToString(),
                _dataRandName["6"]["ba"].ToString(), _dataRandName["6"]["lo"].ToString(), _dataRandName["6"]["la"].ToString(), _dataRandName["6"]["to"].ToString(),
                _dataRandName["6"]["ta"].ToString(), _dataRandName["6"]["no"].ToString(), _dataRandName["6"]["na"].ToString(), _dataRandName["6"]["ni"].ToString(),
                _dataRandName["6"]["a"].ToString(), _dataRandName["6"]["o"].ToString(), _dataRandName["6"]["zor"].ToString(), _dataRandName["6"]["que"].ToString(),
                _dataRandName["6"]["ca"].ToString(), _dataRandName["6"]["co"].ToString(), _dataRandName["6"]["mi"].ToString() };
            strs.Add(_dataRandName["2"]["i"].ToString(), strArrays10);
            string[] strArrays11 = new string[] { _dataRandName["7"]["nie"].ToString(), _dataRandName["7"]["ze"].ToString(), _dataRandName["7"]["dy"].ToString(),
                _dataRandName["7"]["da"].ToString(), _dataRandName["7"]["o"].ToString(), _dataRandName["7"]["ver"].ToString(), _dataRandName["7"]["la"].ToString(),
                _dataRandName["7"]["lo"].ToString(), _dataRandName["7"]["s"].ToString(), _dataRandName["7"]["ny"].ToString(), _dataRandName["7"]["mo"].ToString(),
                _dataRandName["7"]["ra"].ToString() };
            strs.Add(_dataRandName["2"]["o"].ToString(), strArrays11);
            string[] strArrays12 = new string[] { _dataRandName["8"]["rt"].ToString(), _dataRandName["8"]["mo"].ToString(), _dataRandName["8"][""].ToString(),
                _dataRandName["8"]["s"].ToString() };
            strs.Add(_dataRandName["2"]["u"].ToString(), strArrays12);
            string[] strArrays13 = new string[] { _dataRandName["9"]["nny"].ToString(), _dataRandName["9"]["sper"].ToString(), _dataRandName["9"]["trina"].ToString(),
                _dataRandName["9"]["bo"].ToString(), _dataRandName["9"]["-bell"].ToString(), _dataRandName["9"]["boo"].ToString(), _dataRandName["9"]["lbert"].ToString(),
                _dataRandName["9"]["sko"].ToString(), _dataRandName["9"]["sh"].ToString(), _dataRandName["9"]["ck"].ToString(), _dataRandName["9"]["ishe"].ToString(),
                _dataRandName["9"]["rk"].ToString() };
            strs1.Add(_dataRandName["2"]["a"].ToString(), strArrays13);
            string[] strArrays14 = new string[] { _dataRandName["10"]["lla"].ToString(), _dataRandName["10"]["llo"].ToString(), _dataRandName["10"]["rnard"].ToString(),
                _dataRandName["10"]["cardo"].ToString(), _dataRandName["10"]["ffe"].ToString(), _dataRandName["10"]["ppo"].ToString(), _dataRandName["10"]["ppa"].ToString(),
                _dataRandName["10"]["tch"].ToString(), _dataRandName["10"]["x"].ToString() };
            strs1.Add(_dataRandName["2"]["e"].ToString(), strArrays14);
            string[] strArrays15 = new string[] { _dataRandName["11"]["llard"].ToString(), _dataRandName["11"]["lly"].ToString(), _dataRandName["11"]["lbo"].ToString(),
                _dataRandName["11"]["cky"].ToString(), _dataRandName["11"]["card"].ToString(), _dataRandName["11"]["ne"].ToString(), _dataRandName["11"]["nnie"].ToString(),
                _dataRandName["11"]["lbert"].ToString(), _dataRandName["11"]["nono"].ToString(), _dataRandName["11"]["nano"].ToString(), _dataRandName["11"]["nana"].ToString(),
                _dataRandName["11"]["ana"].ToString(), _dataRandName["11"]["nsy"].ToString(), _dataRandName["11"]["msy"].ToString(), _dataRandName["11"]["skers"].ToString(),
                _dataRandName["11"]["rdo"].ToString(), _dataRandName["11"]["rda"].ToString(), _dataRandName["11"]["sh"].ToString() };
            strs1.Add(_dataRandName["2"]["i"].ToString(), strArrays15);
            string[] strArrays16 = new string[] { _dataRandName["12"]["nie"].ToString(), _dataRandName["12"]["zzy"].ToString(), _dataRandName["12"]["do"].ToString(),
                _dataRandName["12"]["na"].ToString(), _dataRandName["12"]["la"].ToString(), _dataRandName["12"]["la"].ToString(), _dataRandName["12"]["ver"].ToString(),
                _dataRandName["12"]["ng"].ToString(), _dataRandName["12"]["ngus"].ToString(), _dataRandName["12"]["ny"].ToString(), _dataRandName["12"]["-mo"].ToString(),
                _dataRandName["12"]["llo"].ToString(), _dataRandName["12"]["ze"].ToString(), _dataRandName["12"]["ra"].ToString(), _dataRandName["12"]["ma"].ToString(),
                _dataRandName["12"]["cco"].ToString(), _dataRandName["12"]["z"].ToString() };
            strs1.Add(_dataRandName["2"]["o"].ToString(), strArrays16);
            string[] strArrays17 = new string[] { _dataRandName["13"]["ssie"].ToString(), _dataRandName["13"]["bbie"].ToString(), _dataRandName["13"]["ffy"].ToString(),
                _dataRandName["13"]["bba"].ToString(), _dataRandName["13"]["rt"].ToString(), _dataRandName["13"]["s"].ToString(), _dataRandName["13"]["mby"].ToString(),
                _dataRandName["13"]["mbo"].ToString(), _dataRandName["13"]["mbus"].ToString(), _dataRandName["13"]["ngus"].ToString(), _dataRandName["13"]["cky"].ToString() };
            strs1.Add(_dataRandName["2"]["u"].ToString(), strArrays17);
            str1 = string.Concat(str1, strArrays1[Game1.random.Next(strArrays1.Count<string>() - 1)]);
            for (int i = 1; i < num - 1; i++)
            {
                str1 = (i % 2 != 0 ? string.Concat(str1, strArrays5[Game1.random.Next(strArrays5.Length)]) : string.Concat(str1, strArrays3[Game1.random.Next(strArrays3.Length)]));
                if (str1.Count<char>() >= num)
                {
                    break;
                }
            }
            if (Game1.random.NextDouble() < 0.5 && !strArrays5.Contains<string>(string.Concat(str1.ElementAt<char>(str1.Length - 1))))
            {
                str1 = string.Concat(str1, strArrays7[Game1.random.Next((int)strArrays7.Length)]);
            }
            else if (!strArrays5.Contains<string>(string.Concat(str1.ElementAt<char>(str1.Length - 1))))
            {
                str1 = string.Concat(str1, strArrays5[Game1.random.Next((int)strArrays5.Length)]);
            }
            else if (Game1.random.NextDouble() < 0.8)
            {
                str1 = (str1.Count() > 3 ?
                    string.Concat(str1, strs[string.Concat(str1.ElementAt(str1.Length - 1))].ElementAt(Game1.random.Next(strs[string.Concat(str1.ElementAt(str1.Length - 1))].Count() - 1))) :
                    string.Concat(str1, strs1[string.Concat(str1.ElementAt(str1.Length - 1))].ElementAt(Game1.random.Next(strs1[string.Concat(str1.ElementAt(str1.Length - 1))].Count() - 1))));
            }
            for (int j = str1.Count<char>() - 1; j > 2; j--)
            {
                if (strArrays5.Contains<string>(str1[j].ToString()) && strArrays5.Contains<string>(str1[j - 2].ToString()))
                {
                    char chr = str1[j - 1];
                    if (chr == _dataRandName["14"]["c"].ToString()[0])
                    {
                        str1 = string.Concat(str1.Substring(0, j), _dataRandName["14"]["k"].ToString(), str1.Substring(j));
                        j--;
                    }
                    else if (chr == _dataRandName["14"]["l"].ToString()[0])
                    {
                        str1 = string.Concat(str1.Substring(0, j - 1), _dataRandName["14"]["n"].ToString(), str1.Substring(j));
                        j--;
                    }
                    else if (chr == _dataRandName["14"]["r"].ToString()[0])
                    {
                        str1 = string.Concat(str1.Substring(0, j - 1), _dataRandName["14"]["k"].ToString(), str1.Substring(j));
                        j--;
                    }
                }
            }
            if (str1.Count<char>() <= 3 && Game1.random.NextDouble() < 0.1)
            {
                str1 = (Game1.random.NextDouble() < 0.5 ? string.Concat(str1, str1) : string.Concat(str1, "-", str1));
            }
            if (str1.Count<char>() <= 2 && str1.Last<char>() == _dataRandName["15"]["e"].ToString()[0])
            {
                string str2 = str1;
                if (Game1.random.NextDouble() < 0.3)
                {
                    str = _dataRandName["15"]["m"].ToString();
                }
                else
                {
                    str = (Game1.random.NextDouble() < 0.5 ? _dataRandName["15"]["p"].ToString() : _dataRandName["15"]["b"].ToString());
                }
                str1 = string.Concat(str2, str);
            }
            bool isBad = false;
            for (int i = 0; i < _dataRandName["bad"].Count(); i++)
            {
                if (str1.ToLower().Contains(_dataRandName["bad"]["" + i].ToString()))
                {
                    isBad = true;
                    break;
                }
            }
            if (isBad)
            {
                str1 = (Game1.random.NextDouble() < 0.5 ? _dataRandName["badReplace"]["Bobo"].ToString() : _dataRandName["badReplace"]["Wumbus"].ToString());
            }
            return str1;
        }

        private List<string> getOtherFarmerNames()
        {
            List<string> strs = new List<string>();
            Random random = new Random((int)Game1.uniqueIDForThisGame);
            Random random1 = new Random((int)((int)Game1.uniqueIDForThisGame + Game1.stats.DaysPlayed));
            string[] strArrays = new string[] { _dataRandName["n1"]["Ron"].ToString(), _dataRandName["n1"]["Desmond"].ToString(),
                _dataRandName["n1"]["Gary"].ToString(), _dataRandName["n1"]["Bart"].ToString(), _dataRandName["n1"]["Willy"].ToString(),
                _dataRandName["n1"]["Tex"].ToString(), _dataRandName["n1"]["Chris"].ToString(), _dataRandName["n1"]["Lenny"].ToString(),
                _dataRandName["n1"]["Patrick"].ToString(), _dataRandName["n1"]["Marty"].ToString(), _dataRandName["n1"]["Jared"].ToString(),
                _dataRandName["n1"]["Kyle"].ToString(), _dataRandName["n1"]["Mitch"].ToString(), _dataRandName["n1"]["Dale"].ToString(),
                _dataRandName["n1"]["Leland"].ToString(), _dataRandName["n1"]["Hunt"].ToString(), _dataRandName["n1"]["Curtis"].ToString(),
                _dataRandName["n1"]["Leone"].ToString(), _dataRandName["n1"]["Andy"].ToString(), _dataRandName["n1"]["Steve"].ToString(),
                _dataRandName["n1"]["Frank"].ToString(), _dataRandName["n1"]["Zach"].ToString(), _dataRandName["n1"]["Bert"].ToString(),
                _dataRandName["n1"]["Lucas"].ToString(), _dataRandName["n1"]["Logan"].ToString(), _dataRandName["n1"]["Stu"].ToString(),
                _dataRandName["n1"]["Mike"].ToString(), _dataRandName["n1"]["Jake"].ToString(), _dataRandName["n1"]["Nick"].ToString(),
                _dataRandName["n1"]["Ben"].ToString(), _dataRandName["n1"]["Daniel"].ToString(), _dataRandName["n1"]["Bubs"].ToString(),
                _dataRandName["n1"]["Jack"].ToString() };
            string[] strArrays1 = strArrays;
            string[] strArrays2 = new string[] { _dataRandName["n2"]["Susan"].ToString(), _dataRandName["n2"]["Danielle"].ToString(),
                _dataRandName["n2"]["Rosie"].ToString(), _dataRandName["n2"]["Joanie"].ToString(), _dataRandName["n2"]["Emma"].ToString(),
                _dataRandName["n2"]["Kate"].ToString(), _dataRandName["n2"]["Pauline"].ToString(), _dataRandName["n2"]["Bev"].ToString(),
                _dataRandName["n2"]["Melissa"].ToString(), _dataRandName["n2"]["Penny"].ToString(), _dataRandName["n2"]["Nancy"].ToString(),
                _dataRandName["n2"]["Betty"].ToString(), _dataRandName["n2"]["Minnie"].ToString(), _dataRandName["n2"]["Rebecca"].ToString(),
                _dataRandName["n2"]["Holly"].ToString(), _dataRandName["n2"]["Ashley"].ToString(), _dataRandName["n2"]["Jasmine"].ToString(),
                _dataRandName["n2"]["Nina"].ToString(), _dataRandName["n2"]["Carly"].ToString(), _dataRandName["n2"]["Jessica"].ToString(),
                _dataRandName["n2"]["Samantha"].ToString(), _dataRandName["n2"]["Amanda"].ToString(), _dataRandName["n2"]["Brittany"].ToString(),
                _dataRandName["n2"]["Liz"].ToString(), _dataRandName["n2"]["Taylor"].ToString(), _dataRandName["n2"]["Megan"].ToString(),
                _dataRandName["n2"]["Hannah"].ToString(), _dataRandName["n2"]["Lauren"].ToString(), _dataRandName["n2"]["Stephanie"].ToString() };
            string[] strArrays3 = strArrays2;
            string[] strArrays4 = new string[] { _dataRandName["n3"]["Farmer"].ToString(), _dataRandName["n3"]["Prospector"].ToString(),
                _dataRandName["n3"]["Fisherman"].ToString(), _dataRandName["n3"]["Woodsman"].ToString(), _dataRandName["n3"]["Lumberjack"].ToString(),
                _dataRandName["n3"]["Explorer"].ToString(), _dataRandName["n3"]["Swordsman"].ToString(), _dataRandName["n3"]["Rancher"].ToString(),
                _dataRandName["n3"]["Cowboy"].ToString(), _dataRandName["n3"]["Slick"].ToString(), _dataRandName["n3"]["'King'"].ToString(),
                _dataRandName["n3"]["Professor"].ToString(), _dataRandName["n3"]["Seafarer"].ToString(), _dataRandName["n3"]["Sailor"].ToString(),
                _dataRandName["n3"]["Hotshot"].ToString(), _dataRandName["n3"]["Hunter"].ToString(), _dataRandName["n3"]["Warlock"].ToString() };
            string[] strArrays5 = strArrays4;
            string[] strArrays6 = new string[] { _dataRandName["n4"]["Farmer"].ToString(), _dataRandName["n4"]["Prospector"].ToString(),
                _dataRandName["n4"]["Seafarer"].ToString(), _dataRandName["n4"]["Herbalist"].ToString(), _dataRandName["n4"]["Explorer"].ToString(),
                _dataRandName["n4"]["Swordmaiden"].ToString(), _dataRandName["n4"]["Rancher"].ToString(), _dataRandName["n4"]["Cowgirl"].ToString(),
                _dataRandName["n4"]["Sweet"].ToString(), _dataRandName["n4"]["Cheerleader"].ToString(), _dataRandName["n4"]["Sorceress"].ToString(),
                _dataRandName["n4"]["Floralist"].ToString() };
            string[] strArrays7 = strArrays6;
            string[] strArrays8 = new string[] { _dataRandName["n5"]["Geezer"].ToString(), _dataRandName["n5"]["'Daddy'"].ToString(),
                _dataRandName["n5"]["Big"].ToString(), _dataRandName["n5"]["Lil'"].ToString(), _dataRandName["n5"]["Plumber"].ToString(),
                _dataRandName["n5"]["Great-Grandpa"].ToString(), _dataRandName["n5"]["Bubba"].ToString(), _dataRandName["n5"]["Doughboy"].ToString(),
                _dataRandName["n5"]["Bag Boy"].ToString(), _dataRandName["n5"]["Courtesy Clerk"].ToString(), _dataRandName["n5"]["Banker"].ToString(),
                _dataRandName["n5"]["Grocer"].ToString(), _dataRandName["n5"]["Golf Pro"].ToString(), _dataRandName["n5"]["Pirate"].ToString(),
                _dataRandName["n5"]["Burglar"].ToString(), _dataRandName["n5"]["Hamburger"].ToString(), _dataRandName["n5"]["Cool Guy"].ToString(),
                _dataRandName["n5"]["Simple"].ToString(), _dataRandName["n5"]["Good Guy"].ToString(), _dataRandName["n5"]["'Garbage'"].ToString(),
                _dataRandName["n5"]["Math Whiz"].ToString(), _dataRandName["n5"]["'Lucky'"].ToString(), _dataRandName["n5"]["Middle Aged"].ToString(),
                _dataRandName["n5"]["Software Developer"].ToString(), _dataRandName["n5"]["Baker"].ToString(), _dataRandName["n5"]["Business Major"].ToString(),
                _dataRandName["n5"]["Pony Master"].ToString(), _dataRandName["n5"]["Ol'"].ToString() };
            string[] strArrays9 = strArrays8;
            string[] strArrays10 = new string[] { _dataRandName["n6"]["Granny"].ToString(), _dataRandName["n6"]["Old Mother"].ToString(),
                _dataRandName["n6"]["Tiny"].ToString(), _dataRandName["n6"]["Simple"].ToString(), _dataRandName["n6"]["Scrapbook"].ToString(),
                _dataRandName["n6"]["Log Lady"].ToString(), _dataRandName["n6"]["Miss"].ToString(), _dataRandName["n6"]["Clever"].ToString(),
                _dataRandName["n6"]["Gossiping"].ToString(), _dataRandName["n6"]["Prom Queen"].ToString(), _dataRandName["n6"]["Diva"].ToString(),
                _dataRandName["n6"]["Sweet Lil'"].ToString(), _dataRandName["n6"]["Blushing"].ToString(), _dataRandName["n6"]["Bashful"].ToString(),
                _dataRandName["n6"]["Cat Lady"].ToString(), _dataRandName["n6"]["Astronomer"].ToString(), _dataRandName["n6"]["Housewife"].ToString(),
                _dataRandName["n6"]["Gardener"].ToString(), _dataRandName["n6"]["Computer Whiz"].ToString(), _dataRandName["n6"]["Lunch Lady"].ToString(),
                _dataRandName["n6"]["Bumpkin"].ToString() };
            string[] strArrays11 = strArrays10;
            string[] strArrays12 = new string[] { _dataRandName["n7"]["'The Meatloaf'"].ToString(), _dataRandName["n7"]["'The Boy Wonder'"].ToString(),
                _dataRandName["n7"]["'The Wiz'"].ToString(), _dataRandName["n7"]["'Super Legs'"].ToString(), _dataRandName["n7"]["'The Nose'"].ToString(),
                _dataRandName["n7"]["'The Duck'"].ToString(), _dataRandName["n7"]["'Spoonface'"].ToString(), _dataRandName["n7"]["'The Brain'"].ToString(),
                _dataRandName["n7"]["'The Shark'"].ToString() };
            string[] strArrays13 = strArrays12;
            string[] strArrays14 = new string[] { _dataRandName["n8"]["Farmer"].ToString(), _dataRandName["n8"]["Rancher"].ToString(),
                _dataRandName["n8"]["Cowboy"].ToString(), _dataRandName["n8"]["Farmboy"].ToString() };
            string[] strArrays15 = new string[] { _dataRandName["n9"]["Farmer"].ToString(), _dataRandName["n9"]["Rancher"].ToString(),
                _dataRandName["n9"]["Cowgirl"].ToString(), _dataRandName["n9"]["Farmgirl"].ToString() };
            string str = "";
            if (!Game1.player.isMale)
            {
                str = strArrays3[random.Next(strArrays3.Count<string>())];
                for (int i = 0; i < 2; i++)
                {
                    while (strs.Contains(str) || Game1.player.name.Equals(str))
                    {
                        str = (i != 0 ? strArrays3[random1.Next(strArrays3.Count<string>())] :
                            strArrays3[random.Next(strArrays3.Count<string>())]);
                    }
                    str = (i != 0 ? string.Concat(strArrays7[random1.Next(strArrays7.Count<string>())], " ", str) :
                        string.Concat(strArrays15[random.Next(strArrays15.Count<string>())], " ", str));
                    strs.Add(str);
                }
            }
            else
            {
                str = strArrays1[random.Next(strArrays1.Count<string>())];
                for (int j = 0; j < 2; j++)
                {
                    while (strs.Contains(str) || Game1.player.name.Equals(str))
                    {
                        str = (j != 0 ? strArrays1[random1.Next(strArrays1.Count<string>())] : strArrays1[random.Next(strArrays1.Count<string>())]);
                    }
                    str = (j != 0 ? string.Concat(strArrays5[random1.Next(strArrays5.Count<string>())], " ", str) :
                        string.Concat(strArrays14[random.Next(strArrays14.Count<string>())], " ", str));
                    strs.Add(str);
                }
            }
            if (random1.NextDouble() >= 0.5)
            {
                str = strArrays3[random1.Next(strArrays3.Count<string>())];
                while (Game1.player.name.Equals(str))
                {
                    str = strArrays3[random1.Next(strArrays3.Count<string>())];
                }
                str = string.Concat(strArrays11[random1.Next(strArrays11.Count<string>())], " ", str);
            }
            else
            {
                str = strArrays1[random1.Next(strArrays1.Count<string>())];
                while (Game1.player.name.Equals(str))
                {
                    str = strArrays1[random1.Next(strArrays1.Count<string>())];
                }
                str = (random1.NextDouble() >= 0.5 ? string.Concat(str, " ", strArrays13[random1.Next(strArrays13.Count<string>())]) :
                    string.Concat(strArrays9[random1.Next(strArrays9.Count<string>())], " ", str));
            }
            strs.Add(str);
            return strs;
        }

        private void drawString(SpriteBatch b, string s, int x, int y, int characterPosition,
                                int width, int height, float alpha, float layerDepth, bool junimoText,
                                int drawBGScroll, string placeHolderScrollWidthText, int color)
        {
            if (width == -1)
            {
                width = Game1.graphics.GraphicsDevice.Viewport.Width - x;
                if (drawBGScroll == 1)
                {
                    width = SpriteText.getWidthOfString(s) * 2;
                }
            }
            if (SpriteText.fontPixelZoom < 4)
            {
                y = y + (4 - SpriteText.fontPixelZoom) * Game1.pixelZoom;
            }
            Vector2 position = new Vector2((float)x, (float)y);
            int accumulatedHorizontalSpaceBetweenCharacters = 0;
            if (drawBGScroll != 1)
            {
                if (position.X + (float)width >
                    (float)(Game1.graphics.GraphicsDevice.Viewport.Width - Game1.pixelZoom))
                {
                    Viewport viewport = Game1.graphics.GraphicsDevice.Viewport;
                    position.X = (float)(viewport.Width - width - Game1.pixelZoom);
                }
                if (position.X < 0f)
                {
                    position.X = 0f;
                }
            }
            if (drawBGScroll == 0)
            {
                b.Draw(Game1.mouseCursors, position + (new Vector2(-12f, -3f) * (float)Game1.pixelZoom),
                    new Rectangle?(new Rectangle(325, 318, 12, 18)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)Game1.pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(Game1.mouseCursors, position + (new Vector2(0f, -3f) * (float)Game1.pixelZoom),
                    new Rectangle?(new Rectangle(337, 318, 1, 18)), Color.White * alpha, 0f, Vector2.Zero,
                    new Vector2(
                        (float)
                            SpriteText.getWidthOfString((placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s)), (float)Game1.pixelZoom), SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(Game1.mouseCursors,
                    position +
                    new Vector2(
                        (float)
                            SpriteText.getWidthOfString((placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s)), (float)(-3 * Game1.pixelZoom)), new Rectangle?(new Rectangle(338, 318, 12, 18)),
                    Color.White * alpha, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None,
                    layerDepth - 0.001f);
                if (placeHolderScrollWidthText.Count<char>() > 0)
                {
                    x = x +
                        (SpriteText.getWidthOfString(placeHolderScrollWidthText) / 2 - SpriteText.getWidthOfString(s) / 2);
                    position.X = (float)x;
                }
                position.Y = position.Y + (float)((4 - SpriteText.fontPixelZoom) * Game1.pixelZoom);
            }
            else if (drawBGScroll == 1)
            {
                b.Draw(Game1.mouseCursors, position + (new Vector2(-7f, -3f) * (float)Game1.pixelZoom),
                    new Rectangle?(new Rectangle(324, 299, 7, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)Game1.pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(Game1.mouseCursors, position + (new Vector2(0f, -3f) * (float)Game1.pixelZoom),
                    new Rectangle?(new Rectangle(331, 299, 1, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    new Vector2(
                        (float)
                            SpriteText.getWidthOfString((placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s)), (float)Game1.pixelZoom), SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(Game1.mouseCursors,
                    position +
                    new Vector2(
                        (float)
                            SpriteText.getWidthOfString((placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s)), (float)(-3 * Game1.pixelZoom)), new Rectangle?(new Rectangle(332, 299, 7, 17)),
                    Color.White * alpha, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None,
                    layerDepth - 0.001f);
                b.Draw(Game1.mouseCursors,
                    position +
                    new Vector2(
                        (float)
                            (SpriteText.getWidthOfString((placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s)) / 2), (float)(13 * Game1.pixelZoom)), new Rectangle?(new Rectangle(341, 308, 6, 5)),
                    Color.White * alpha, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None,
                    layerDepth - 0.0001f);
                if (placeHolderScrollWidthText.Count<char>() > 0)
                {
                    x = x +
                        (SpriteText.getWidthOfString(placeHolderScrollWidthText) / 2 - SpriteText.getWidthOfString(s) / 2);
                    position.X = (float)x;
                }
                position.Y = position.Y + (float)((4 - SpriteText.fontPixelZoom) * Game1.pixelZoom);
            }
            else if (drawBGScroll == 2)
            {
                b.Draw(Game1.mouseCursors, position + (new Vector2(-3f, -3f) * (float)Game1.pixelZoom),
                    new Rectangle?(new Rectangle(327, 281, 3, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)Game1.pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                b.Draw(Game1.mouseCursors, position + (new Vector2(0f, -3f) * (float)Game1.pixelZoom),
                    new Rectangle?(new Rectangle(330, 281, 1, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    new Vector2(
                        (float)
                            (SpriteText.getWidthOfString((placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s)) + Game1.pixelZoom), (float)Game1.pixelZoom), SpriteEffects.None,
                    layerDepth - 0.001f);
                b.Draw(Game1.mouseCursors,
                    position +
                    new Vector2(
                        (float)
                            (SpriteText.getWidthOfString((placeHolderScrollWidthText.Count<char>() > 0
                                ? placeHolderScrollWidthText
                                : s)) + Game1.pixelZoom), (float)(-3 * Game1.pixelZoom)),
                    new Rectangle?(new Rectangle(333, 281, 3, 17)), Color.White * alpha, 0f, Vector2.Zero,
                    (float)Game1.pixelZoom, SpriteEffects.None, layerDepth - 0.001f);
                if (placeHolderScrollWidthText.Count<char>() > 0)
                {
                    x = x +
                        (SpriteText.getWidthOfString(placeHolderScrollWidthText) / 2 - SpriteText.getWidthOfString(s) / 2);
                    position.X = (float)x;
                }
                position.Y = position.Y + (float)((4 - SpriteText.fontPixelZoom) * Game1.pixelZoom);
            }
            s = s.Replace(Environment.NewLine, "");
            for (int i = 0; i < Math.Min(s.Length, characterPosition); i++)
            {
                if (s[i] != '\u005E')
                {
                    if (i > 0)
                    {
                        position.X = position.X +
                                     (float)
                                         (8 * SpriteText.fontPixelZoom + accumulatedHorizontalSpaceBetweenCharacters +
                                          (SpriteText.getWidthOffsetForChar(s[i]) +
                                           SpriteText.getWidthOffsetForChar(s[i - 1])) * SpriteText.fontPixelZoom);
                    }
                    int num = SpriteText.fontPixelZoom;
                    accumulatedHorizontalSpaceBetweenCharacters = 0;
                    if (
                        SpriteText.positionOfNextSpace(s, i, (int)position.X,
                            accumulatedHorizontalSpaceBetweenCharacters) >= x + width - Game1.pixelZoom)
                    {
                        position.Y = position.Y + (float)(18 * SpriteText.fontPixelZoom);
                        accumulatedHorizontalSpaceBetweenCharacters = 0;
                        position.X = (float)x;
                    }
                    b.Draw((color != -1 ? SpriteText.coloredTexture : SpriteText.spriteTexture), position,
                        new Rectangle?(getSourceRectForChar(s[i], junimoText)),
                        SpriteText.getColorFromIndex(color) * alpha, 0f, Vector2.Zero, (float)SpriteText.fontPixelZoom,
                        SpriteEffects.None, layerDepth);
                }
                else
                {
                    position.Y = position.Y + (float)(18 * SpriteText.fontPixelZoom);
                    position.X = (float)x;
                    accumulatedHorizontalSpaceBetweenCharacters = 0;
                }
            }
        }

        private Rectangle getSourceRectForChar(char c, bool junimoText)
        {
            int num = (int)c - 32;
            return new Rectangle(num * 8 % SpriteText.spriteTexture.Width, num * 8 / SpriteText.spriteTexture.Width * 16 + (junimoText ? 96 : 0), 8, 16);
        }

        private void LoadDictionary()
        {
            _memoryBuffer = new Dictionary<string, string>();
            _translatedStrings = new List<string>();
            _languages = new Dictionary<string, int>();
            _fuzzyDictionary = new FuzzyStringDictionary();
            Characters = new Dictionary<string, string>();
            var jobj = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages", "descriptions.json"))));
            _languageDescriptions = new Dictionary<string, string>();
            foreach (var directory in Directory.GetDirectories(Path.Combine(PathOnDisk, "languages")).Select((o, i) => new { Value = o, Index = i }))
            {
                var shortName = directory.Value.Split('\\').Last();
                _languageDescriptions.Add(jobj[shortName].ToString(), shortName);
                _languages.Add(shortName, directory.Index);
            }

            _currentLanguage = Config.LanguageName;
            var dictionariesFolder = Path.Combine(PathOnDisk, "languages", Config.LanguageName, "dictionaries");
            if (Directory.Exists(dictionariesFolder) && Directory.GetFiles(dictionariesFolder).Count() > 0)
            {
                foreach (var dict in Directory.GetFiles(dictionariesFolder))
                {
                    var dictName = Path.GetFileName(dict);
                    if (dictName == "Dialogues.json")
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var val in jo)
                        {
                            var pair = JObject.Parse(val.Value.ToString());
                            foreach (var row in pair)
                            {
                                AddToDictionary(row.Key, row.Value.ToString());
                            }
                        }
                    }
                    if (dictName == "Items.json")
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var val in jo)
                        {
                            var pair = JObject.Parse(val.Value.ToString());
                            foreach (var row in pair)
                            {
                                AddToDictionary(row.Key, row.Value.ToString());
                            }
                        }
                    }
                    else if (dictName == "Characters.json")
                    {
                        Characters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var pair in Characters)
                        {
                            AddToDictionary(pair.Key, pair.Value);
                        }
                    }
                    else if (dictName == "Achievements.json" || dictName == "animationDescription.json" || dictName == "EngagementDialogue.json" ||
                        dictName == "Events.json" || dictName == "Festivals.json" ||
                        dictName == "Mails.json" || dictName == "Quests.json" ||
                        dictName == "NPCGiftTastes.json"  || dictName == "schedules.json" ||
                        dictName == "TV.json")
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var val in jo)
                        {
                            AddToDictionary(val.Key, val.Value.ToString());
                        }
                    }
                    else if (dictName == "_NameGen.json")
                    {
                        _dataRandName = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));

                        foreach (var row in _dataRandName)
                        {
                            if (row.Value.ToList().Count > 0)
                            {
                                var pair = JObject.Parse(row.Value.ToString());
                                foreach (var item in pair)
                                {
                                    if (item.Key != "" && item.Value.ToString() != "")
                                    {
                                        if (row.Key[0] == 'n')
                                        {
                                            AddToDictionary(item.Key, item.Value.ToString());
                                        }
                                        else
                                        {
                                            if (row.Key != "bad")
                                            {
                                                if (_nameEnToRu.Where(kvp => kvp.Key == item.Key).Count() == 0)
                                                {
                                                    _nameEnToRu.Add(new KeyValuePair<string, string>(item.Key, item.Value.ToString()));
                                                }
                                                if (_nameRuToEn.Where(kvp => kvp.Key == item.Value.ToString()).Count() == 0)
                                                {
                                                    _nameRuToEn.Add(new KeyValuePair<string, string>(item.Value.ToString(), item.Key));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        _nameEnToRu.Sort((x, y) => x.Key.Length.CompareTo(y.Key.Length));
                        _nameRuToEn.Sort((x, y) => x.Value.Length.CompareTo(y.Value.Length));
                    }
                    else
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)).Replace("@newline", Environment.NewLine));
                        foreach (var pair in jo)
                        {
                            AddToDictionary(pair.Key, pair.Value.ToString());
                        }
                    }
                }
            }

            #region upload content to the game
            var modeContentFolder = Path.Combine(PathOnDisk, "languages", Config.LanguageName, "content");
            var gameContentFolder = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "Content");// Game1.content.RootDirectory);
            foreach (var directory in Directory.GetDirectories(modeContentFolder))
            {
                var files = Directory.GetFiles(directory).Where(f => Path.GetExtension(f) == ".xnb");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var gameFile = new FileInfo(Path.Combine(gameContentFolder, directory.Split('\\').Last(), fileName));
                    var modeFile = new FileInfo(Path.Combine(directory, fileName));
                    if (gameFile.Exists)
                    {
                        if (gameFile.LastWriteTime != modeFile.LastWriteTime)
                        {
                            modeFile.CopyTo(gameFile.FullName, true);
                        }
                    }
                    if (fileName == "townInterior.xnb" || 
                         fileName == "HospitalTiles.xnb")
                     {
                         gameFile = new FileInfo(Path.Combine(gameContentFolder, fileName));
                         if (gameFile.Exists)
                         {
                             if (gameFile.LastWriteTime != modeFile.LastWriteTime)
                             {
                                 modeFile.CopyTo(gameFile.FullName, true);
                             }
                         }
                     }
                }
            }
            #endregion

            if (Config.LanguageName == "RU")
            {
                var collection = new CyrNounCollection();
                var adjectives = new CyrAdjectiveCollection();
                cyrPhrase = new CyrPhrase(collection, adjectives);
            }
        }
    }
}
