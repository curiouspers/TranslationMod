using Storm.ExternalEvent;
using Storm.StardewValley.Event;
using Storm.StardewValley.Wrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using Storm.StardewValley;
using System.Reflection;
using System.Text.RegularExpressions;
using StardewValley.Buildings;
using Storm.StardewValley.Accessor;
using StardewValley;

namespace TranslationMod
{
    [Mod]
    public class TranslationMod : DiskResource
    {
        public Config ModConfig { get; private set; }
        public Dictionary<string, Person> Data { get; set; }
        public Dictionary<string, string> DataExe { get; set; }
        public Dictionary<string, string> Characters { get; set; }
        public Newtonsoft.Json.Linq.JObject DataRandName { get; set; }
        private Dictionary<string,int> _languages;
        private Dictionary<string, string> _languageDescriptions;
        private string _currentLanguage;
        private bool _isConfigLoaded = false;
        public List<String> LoadedResources;
        private bool _isMenuDrawing;

        [Subscribe]
        public void InitializeCallback(InitializeEvent @event)
        {
            if (!_isConfigLoaded) LoadConfig(@event.Root.Content.RootDirectory);
        }

        [Subscribe]
        public void onWindowsSizeChanged(ClientSizeChangedEvent @event)
        {
            if (_isMenuDrawing)
                _isMenuDrawing = false;
        }

        [Subscribe]
        public void onUpdate(PostUpdateEvent @event)
        {
            if (@event.Root.ActiveClickableMenu != null && @event.Root.ActiveClickableMenu is GameMenu)
            {
                var menu = @event.Root.ActiveClickableMenu as GameMenu;
                var optionPage = menu.Pages.FirstOrDefault(p => p is OptionsPage);
                if (optionPage != null)
                {
                    var options = (optionPage as OptionsPage).Options.Cast<StardewValley.Menus.OptionsElement>().ToList();
                    if(!_isMenuDrawing)
                    {
                        var newOptions = new List<StardewValley.Menus.OptionsElement>();
                        var dropdownoption = options.Find(o => o is StardewValley.Menus.OptionsDropDown);
                        foreach (var option in options)
                        {
                            if (option.label == "Sound:")
                            {
                                var languageDropDown = new StardewValley.Menus.OptionsDropDown("Language", 55);
                                languageDropDown.selectedOption = _languages[_currentLanguage];
                                newOptions.Add(languageDropDown);
                            }
                            newOptions.Add(option);
                        }
                        (optionPage as OptionsPage).Options = newOptions;
                        _isMenuDrawing = true;
                    }
                }
            }
            else if(@event.Root.ActiveClickableMenu == null)
            {
                if(_isMenuDrawing)
                    _isMenuDrawing = false;
            }
        }

        [Subscribe]
        public void onChangeLanguage(ChangeDropDownOptionsEvent @event)
        {
            if(@event.Which == 55)
            {
                var selectedLang = _languageDescriptions[@event.Options[@event.Selection]];
                if (selectedLang != _currentLanguage)
                {
                    File.WriteAllBytes(Path.Combine(PathOnDisk, "Config.json"),
                        Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Config { LanguageName = selectedLang })));
                    _currentLanguage = selectedLang;
                }
            }
        }


        [Subscribe]
        public void onSetDropDownPropertyValue(SetDropDownToProperValueEvent @event)
        {
            if(@event.DropDown.Underlying is StardewValley.Menus.OptionsDropDown &&
                (@event.DropDown.Underlying as StardewValley.Menus.OptionsDropDown).whichOption == 55)
            {
                (@event.DropDown.Underlying as StardewValley.Menus.OptionsDropDown).dropDownOptions = _languageDescriptions.Keys.ToList();
            }
        }

        [Subscribe]
        public void PastGameLoadedCallback(PostGameLoadedEvent @event)
        {
            var characters = @event.Root.AllCharacters;
            foreach (var npc in characters)
            {
                if (npc.Dialogue != null)
                {
                    var dialogues = npc.Dialogue.AsEnumerable().ToArray();
                    foreach (var dialog in dialogues)
                    {
                        if(Data.ContainsKey(npc.Name))
                        {
                            var newValue = Data[npc.Name].Dialogues.Where(d => d.Key == dialog.Value).Select(d => d.Value).FirstOrDefault();
                            if (!string.IsNullOrEmpty(newValue))
                                npc.Dialogue[dialog.Key] = newValue;
                        }
                    }
                }
            }
        }

        [Subscribe]
        public void onAssetLoad(AssetLoadEvent @event)
        {
            if (!_isConfigLoaded) LoadConfig(@event.Root.Content.RootDirectory);
        }

        [Subscribe]
        public void onGetRandomName(GetRandomNameEvent @event)
        {
            //ЗДЕСЬ АЛГОРИТМ ГЕНЕРАЦИИ ИМЕН НА РУССКОМ!
            //Возвращаемый объект - string
            @event.ReturnValue = randomName();
            @event.ReturnEarly = true;
        }

        [Subscribe]
        public void onOtherFarmerNames(GetOtherFarmerNamesEvent @event)
        {
            //ЗДЕСЬ АЛГОРИТМ ГЕНЕРАЦИИ СПИСКА ИМЕН ФЕРМЕРОВ НА РУССКОМ!
            //Возвращаемый объект - List<string>
            //@event.ReturnEarly = true;
        }

        //private const string Cyrillic = "AaБбВвГг...";
        //private const string Latin = "A|a|B|b|V|v|G|g|...";
        private const string Cyrillic = "AaBbVvGgDdEeJjZzIiYyKkLlMmNnOoPpRrSsTtUuFfCcWwHh";
        private const string Latin = "А|а|Б|б|В|в|Г|г|Д|д|Е|е|Ж|ж|З|з|И|и|И|и|К|к|Л|л|М|м|Н|н|О|о|П|п|Р|р|С|с|Т|т|У|у|Ф|ф|Ч|ч|В|в|Х|";
        private Dictionary<char, string> mLookup;

        public string Romanize(string russian)
        {
            if (mLookup == null)
            {
                mLookup = new Dictionary<char, string>();
                var replace = Latin.Split('|');
                for (int ix = 0; ix < Cyrillic.Length; ++ix)
                {
                    mLookup.Add(Cyrillic[ix], replace[ix]);
                }
            }
            var buf = new StringBuilder(russian.Length);
            foreach (char ch in russian)
            {
                if (mLookup.ContainsKey(ch)) buf.Append(mLookup[ch]);
                else buf.Append(ch);
            }
            return buf.ToString();
        }

        public string randomName()
        {
            string str;
            string str1 = "";
            int num = Game1.random.Next(3, 6);
            string[] strArrays = new string[] { DataRandName["0"]["B"].ToString(), DataRandName["0"]["Br"].ToString(), DataRandName["0"]["J"].ToString(), DataRandName["0"]["F"].ToString(), DataRandName["0"]["S"].ToString(), DataRandName["0"]["M"].ToString(), DataRandName["0"]["C"].ToString(), DataRandName["0"]["Ch"].ToString(), DataRandName["0"]["L"].ToString(), DataRandName["0"]["P"].ToString(), DataRandName["0"]["K"].ToString(), DataRandName["0"]["W"].ToString(), DataRandName["0"]["G"].ToString(), DataRandName["0"]["Z"].ToString(), DataRandName["0"]["Tr"].ToString(), DataRandName["0"]["T"].ToString(), DataRandName["0"]["Gr"].ToString(), DataRandName["0"]["Fr"].ToString(), DataRandName["0"]["Pr"].ToString(), DataRandName["0"]["N"].ToString(), DataRandName["0"]["Sn"].ToString(), DataRandName["0"]["R"].ToString(), DataRandName["0"]["Sh"].ToString(), DataRandName["0"]["St"].ToString() };
            string[] strArrays1 = strArrays;
            string[] strArrays2 = new string[] { DataRandName["1"]["ll"].ToString(), DataRandName["1"]["tch"].ToString(), DataRandName["1"]["l"].ToString(), DataRandName["1"]["m"].ToString(), DataRandName["1"]["n"].ToString(), DataRandName["1"]["p"].ToString(), DataRandName["1"]["r"].ToString(), DataRandName["1"]["s"].ToString(), DataRandName["1"]["t"].ToString(), DataRandName["1"]["c"].ToString(), DataRandName["1"]["rt"].ToString(), DataRandName["1"]["ts"].ToString() };
            string[] strArrays3 = strArrays2;
            string[] strArrays4 = new string[] { DataRandName["2"]["a"].ToString(), DataRandName["2"]["e"].ToString(), DataRandName["2"]["i"].ToString(), DataRandName["2"]["o"].ToString(), DataRandName["2"]["u"].ToString() };
            string[] strArrays5 = strArrays4;
            string[] strArrays6 = new string[] { DataRandName["3"]["ie"].ToString(), DataRandName["3"]["o"].ToString(), DataRandName["3"]["a"].ToString(), DataRandName["3"]["ers"].ToString(), DataRandName["3"]["ley"].ToString() };
            string[] strArrays7 = strArrays6;
            Dictionary<string, string[]> strs = new Dictionary<string, string[]>();
            Dictionary<string, string[]> strs1 = new Dictionary<string, string[]>();
            string[] strArrays8 = new string[] { DataRandName["4"]["nie"].ToString(), DataRandName["4"]["bell"].ToString(), DataRandName["4"]["bo"].ToString(), DataRandName["4"]["boo"].ToString(), DataRandName["4"]["bella"].ToString(), DataRandName["4"]["s"].ToString() };
            strs.Add(DataRandName["2"]["a"].ToString(), strArrays8);
            string[] strArrays9 = new string[] { DataRandName["5"]["ll"].ToString(), DataRandName["5"]["llo"].ToString(), DataRandName["5"][""].ToString(), DataRandName["5"]["o"].ToString() };
            strs.Add(DataRandName["2"]["e"].ToString(), strArrays9);
            string[] strArrays10 = new string[] { DataRandName["6"]["ck"].ToString(), DataRandName["6"]["e"].ToString(), DataRandName["6"]["bo"].ToString(), DataRandName["6"]["ba"].ToString(), DataRandName["6"]["lo"].ToString(), DataRandName["6"]["la"].ToString(), DataRandName["6"]["to"].ToString(), DataRandName["6"]["ta"].ToString(), DataRandName["6"]["no"].ToString(), DataRandName["6"]["na"].ToString(), DataRandName["6"]["ni"].ToString(), DataRandName["6"]["a"].ToString(), DataRandName["6"]["o"].ToString(), DataRandName["6"]["zor"].ToString(), DataRandName["6"]["que"].ToString(), DataRandName["6"]["ca"].ToString(), DataRandName["6"]["co"].ToString(), DataRandName["6"]["mi"].ToString() };
            strs.Add(DataRandName["2"]["i"].ToString(), strArrays10);
            string[] strArrays11 = new string[] { DataRandName["7"]["nie"].ToString(), DataRandName["7"]["ze"].ToString(), DataRandName["7"]["dy"].ToString(), DataRandName["7"]["da"].ToString(), DataRandName["7"]["o"].ToString(), DataRandName["7"]["ver"].ToString(), DataRandName["7"]["la"].ToString(), DataRandName["7"]["lo"].ToString(), DataRandName["7"]["s"].ToString(), DataRandName["7"]["ny"].ToString(), DataRandName["7"]["mo"].ToString(), DataRandName["7"]["ra"].ToString() };
            strs.Add(DataRandName["2"]["o"].ToString(), strArrays11);
            string[] strArrays12 = new string[] { DataRandName["8"]["rt"].ToString(), DataRandName["8"]["mo"].ToString(), DataRandName["8"][""].ToString(), DataRandName["8"]["s"].ToString() };
            strs.Add(DataRandName["2"]["u"].ToString(), strArrays12);
            string[] strArrays13 = new string[] { DataRandName["9"]["nny"].ToString(), DataRandName["9"]["sper"].ToString(), DataRandName["9"]["trina"].ToString(), DataRandName["9"]["bo"].ToString(), DataRandName["9"]["-bell"].ToString(), DataRandName["9"]["boo"].ToString(), DataRandName["9"]["lbert"].ToString(), DataRandName["9"]["sko"].ToString(), DataRandName["9"]["sh"].ToString(), DataRandName["9"]["ck"].ToString(), DataRandName["9"]["ishe"].ToString(), DataRandName["9"]["rk"].ToString() };
            strs1.Add(DataRandName["2"]["a"].ToString(), strArrays13);
            string[] strArrays14 = new string[] { DataRandName["10"]["lla"].ToString(), DataRandName["10"]["llo"].ToString(), DataRandName["10"]["rnard"].ToString(), DataRandName["10"]["cardo"].ToString(), DataRandName["10"]["ffe"].ToString(), DataRandName["10"]["ppo"].ToString(), DataRandName["10"]["ppa"].ToString(), DataRandName["10"]["tch"].ToString(), DataRandName["10"]["x"].ToString() };
            strs1.Add(DataRandName["2"]["e"].ToString(), strArrays14);
            string[] strArrays15 = new string[] { DataRandName["11"]["llard"].ToString(), DataRandName["11"]["lly"].ToString(), DataRandName["11"]["lbo"].ToString(), DataRandName["11"]["cky"].ToString(), DataRandName["11"]["card"].ToString(), DataRandName["11"]["ne"].ToString(), DataRandName["11"]["nnie"].ToString(), DataRandName["11"]["lbert"].ToString(), DataRandName["11"]["nono"].ToString(), DataRandName["11"]["nano"].ToString(), DataRandName["11"]["nana"].ToString(), DataRandName["11"]["ana"].ToString(), DataRandName["11"]["nsy"].ToString(), DataRandName["11"]["msy"].ToString(), DataRandName["11"]["skers"].ToString(), DataRandName["11"]["rdo"].ToString(), DataRandName["11"]["rda"].ToString(), DataRandName["11"]["sh"].ToString() };
            strs1.Add(DataRandName["2"]["i"].ToString(), strArrays15);
            string[] strArrays16 = new string[] { DataRandName["12"]["nie"].ToString(), DataRandName["12"]["zzy"].ToString(), DataRandName["12"]["do"].ToString(), DataRandName["12"]["na"].ToString(), DataRandName["12"]["la"].ToString(), DataRandName["12"]["la"].ToString(), DataRandName["12"]["ver"].ToString(), DataRandName["12"]["ng"].ToString(), DataRandName["12"]["ngus"].ToString(), DataRandName["12"]["ny"].ToString(), DataRandName["12"]["-mo"].ToString(), DataRandName["12"]["llo"].ToString(), DataRandName["12"]["ze"].ToString(), DataRandName["12"]["ra"].ToString(), DataRandName["12"]["ma"].ToString(), DataRandName["12"]["cco"].ToString(), DataRandName["12"]["z"].ToString() };
            strs1.Add(DataRandName["2"]["o"].ToString(), strArrays16);
            string[] strArrays17 = new string[] { DataRandName["13"]["ssie"].ToString(), DataRandName["13"]["bbie"].ToString(), DataRandName["13"]["ffy"].ToString(), DataRandName["13"]["bba"].ToString(), DataRandName["13"]["rt"].ToString(), DataRandName["13"]["s"].ToString(), DataRandName["13"]["mby"].ToString(), DataRandName["13"]["mbo"].ToString(), DataRandName["13"]["mbus"].ToString(), DataRandName["13"]["ngus"].ToString(), DataRandName["13"]["cky"].ToString() };
            strs1.Add(DataRandName["2"]["u"].ToString(), strArrays17);
            str1 = string.Concat(str1, strArrays1[Game1.random.Next(strArrays1.Count<string>() - 1)]);
            for (int i = 1; i < num - 1; i++)
            {
                str1 = (i % 2 != 0 ? string.Concat(str1, strArrays5[Game1.random.Next((int)strArrays5.Length)]) : string.Concat(str1, strArrays3[Game1.random.Next((int)strArrays3.Length)]));
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
                str1 = (str1.Count<char>() > 3 ? string.Concat(str1, strs[string.Concat(str1.ElementAt<char>(str1.Length - 1))].ElementAt<string>(Game1.random.Next(strs[string.Concat(str1.ElementAt<char>(str1.Length - 1))].Count<string>() - 1))) : string.Concat(str1, strs1[string.Concat(str1.ElementAt<char>(str1.Length - 1))].ElementAt<string>(Game1.random.Next(strs1[string.Concat(str1.ElementAt<char>(str1.Length - 1))].Count<string>() - 1))));
            }
            for (int j = str1.Count<char>() - 1; j > 2; j--)
            {
                if (strArrays5.Contains<string>(str1[j].ToString()) && strArrays5.Contains<string>(str1[j - 2].ToString()))
                {
                    char chr = str1[j - 1];
                    if (chr == DataRandName["14"]["c"].ToString()[0])
                    {
                        str1 = string.Concat(str1.Substring(0, j), DataRandName["14"]["k"].ToString(), str1.Substring(j));
                        j--;
                    }
                    else if (chr == DataRandName["14"]["l"].ToString()[0])
                    {
                        str1 = string.Concat(str1.Substring(0, j - 1), DataRandName["14"]["n"].ToString(), str1.Substring(j));
                        j--;
                    }
                    else if (chr == DataRandName["14"]["r"].ToString()[0])
                    {
                        str1 = string.Concat(str1.Substring(0, j - 1), DataRandName["14"]["k"].ToString(), str1.Substring(j));
                        j--;
                    }
                }
            }
            if (str1.Count<char>() <= 3 && Game1.random.NextDouble() < 0.1)
            {
                str1 = (Game1.random.NextDouble() < 0.5 ? string.Concat(str1, str1) : string.Concat(str1, "-", str1));
            }
            if (str1.Count<char>() <= 2 && str1.Last<char>() == DataRandName["15"]["e"].ToString()[0])
            {
                string str2 = str1;
                if (Game1.random.NextDouble() < 0.3)
                {
                    str = DataRandName["15"]["m"].ToString();
                }
                else
                {
                    str = (Game1.random.NextDouble() < 0.5 ? DataRandName["15"]["p"].ToString() : DataRandName["15"]["b"].ToString());
                }
                str1 = string.Concat(str2, str);
            }
            bool isBad = false;
            for (int i = 0; i < DataRandName["bad"].Count(); i++)
            {
                if (str1.ToLower().Contains(DataRandName["bad"][""+i].ToString()))
                {
                    isBad = true;
                    break;
                }
            }
            if (isBad)
            {
                str1 = (Game1.random.NextDouble() < 0.5 ? DataRandName["badReplace"]["Bobo"].ToString() : DataRandName["badReplace"]["Wumbus"].ToString());
            }
            return str1;
        }

        [Subscribe]
        public void onSetNewDialgue(SetNewDialogueEvent @event)
        {
            var npc = @event.NPC;
            var dialogue = "";
            var original = "";
            if (string.IsNullOrEmpty(@event.Dialogue))
            {
                if (!@event.Add) @event.NPC.CurrentDialogue.Clear();
                var content = StormContentManager.Load<Dictionary<string, string>>(@event.Root.Content,
                    "Characters\\Dialogue\\" + @event.DialogueSheetName);
                string str = @event.NumberToAppend == -1 ? npc.Name : "";
                string key = @event.DialogueSheetKey + (@event.NumberToAppend != -1 ?
                    string.Concat(@event.NumberToAppend) :
                    "") + str;
                if (!content.ContainsKey(key)) return;
                else
                {
                    original = content[key];
                }
            }
            else original = @event.Dialogue;
            var newValue = Data[npc.Name].Dialogues.Where(d => d.Key == original).Select(d => d.Value).FirstOrDefault();
            if (!string.IsNullOrEmpty(newValue))
                dialogue = newValue;
            else dialogue = original;
            @event.NPC.CurrentDialogue.Push(new Dialogue(dialogue, (StardewValley.NPC)@event.NPC.Underlying)
            {
                removeOnNextMove = @event.ClearOnMovement
            });
            @event.ReturnEarly = true;
        }

        [Subscribe]
        public void onDrawSpriteText(PreSpriteTextDrawStringEvent @event)
        {
            WriteToScan(@event.Text);
            if (Characters.ContainsKey(@event.Text))
            {
                @event.Text = Characters[@event.Text];
            }
            drawString(@event.Sprite, @event.Text, @event.X, @event.Y, @event.CharacterPosition,
                @event.Width, @event.Height, @event.Alpha, @event.LayerDepth, @event.JunimoText,
                @event.DrawBGScroll, @event.PlaceHolderScrollWidthText, @event.Color);
            @event.ReturnEarly = true;
        }

        [Subscribe]
        public void onGetWidthSpriteText(SpriteTextGetWidthOfStringEvent @event)
        {
            WriteToScan(@event.Text);
            if (Characters.ContainsKey(@event.Text))
            {
                @event.Text = Characters[@event.Text];
                @event.ReturnValue = @event.Root.GetWidthOfString(@event.Text);
                @event.ReturnEarly = true;
            }
        }

        [Subscribe]
        public void onSpriteBatchDrawString(SpriteBatchDrawStringEvent @event)
        {
            if (@event.Message == "Map" && ModConfig.LanguageName == "RU")
            {
                @event.ReturnValue = "Карта";
            }
            if (Data.ContainsKey("GrandpaStory") && Data["GrandpaStory"].Dialogues.Where(d => d.Key == @event.Message && !string.IsNullOrEmpty(d.Value)).Count()>0)
            {
                @event.ReturnValue = Data["GrandpaStory"].Dialogues.Find(d => d.Key == @event.Message).Value;
            }
            WriteToScan(@event.Message);
        }

        [Subscribe]
        public void onSpriteFontMeasureString(SpriteFontMeasureStringEvent @event)
        {
            if(@event.Message == "Map" && ModConfig.LanguageName == "RU")
            {
                @event.ReturnValue = "Карта";
            }
            if (Data.ContainsKey("GrandpaStory") && Data["GrandpaStory"].Dialogues.Where(d => d.Key == @event.Message && !string.IsNullOrEmpty(d.Value)).Count() > 0)
            {
                @event.ReturnValue = Data["GrandpaStory"].Dialogues.Find(d => d.Key == @event.Message).Value;
            }
            WriteToScan(@event.Message);
        }

        private void LoadConfig(string ContentRoot)
        {
            _languages = new Dictionary<string,int>();
            var jobj = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages", "descriptions.json"))));
            _languageDescriptions = new Dictionary<string, string>();
            foreach (var directory in Directory.GetDirectories(Path.Combine(PathOnDisk, "languages")).Select((o,i)=> new { Value = o, Index = i }))
            {
                var shortName = directory.Value.Split('\\').Last();
                _languageDescriptions.Add(jobj[shortName].ToString(), shortName);
                _languages.Add(shortName, directory.Index);
            }
            LoadedResources = new List<string>();
            var configLocation = Path.Combine(PathOnDisk, "Config.json");
            if (!File.Exists(configLocation))
            {
                ModConfig = new Config();
                ModConfig.LanguageName = "EN";
                File.WriteAllBytes(configLocation, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ModConfig)));
            }
            else
            {
                ModConfig = JsonConvert.DeserializeObject<Config>(Encoding.UTF8.GetString(File.ReadAllBytes(configLocation)));
            }
            _currentLanguage = ModConfig.LanguageName;
            if (Directory.Exists(Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "dictionaries")) && Directory.GetFiles(Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "dictionaries")).Count() > 0)
            {
                Data = JsonConvert.DeserializeObject<Dictionary<string, Person>>(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "dictionaries", "MainDictionary.json"))));
                DataRandName = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName + "nameGen.json"))));
                Characters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "dictionaries", "Characters.json"))));
            }
            else
            {
                Data = new Dictionary<string, Person>();
                Characters = new Dictionary<string, string>();
            }

            #region upload content to the game
            var modeContentFolder = Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "content");
            var gameContentFolder = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), ContentRoot);
            foreach (var directory in Directory.GetDirectories(modeContentFolder))
            {
                var files = Directory.GetFiles(directory).Where(f => Path.GetExtension(f) == ".xnb");
                foreach(var file in files)
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
                }
            }
            #endregion

            _isConfigLoaded = true;
        }

        void WriteToScan(string line)
        {
            string scanFile = "upload.json";
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scanFile)))
            {
                var lines = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(scanFile));
                if (!lines.Contains(line))
                {
                    lines.Add(line);
                    File.WriteAllText(scanFile, JsonConvert.SerializeObject(lines));
                }
            }
            else
            {
                var lines = new List<string>();
                lines.Add(line);
                File.WriteAllText(scanFile, JsonConvert.SerializeObject(lines));
            }
        }

        public void drawString(SpriteBatch b, string s, int x, int y, int characterPosition,
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

        Rectangle getSourceRectForChar(char c, bool junimoText)
        {
            int num = (int)c - 32;
            return new Rectangle(num * 8 % SpriteText.spriteTexture.Width, num * 8 / SpriteText.spriteTexture.Width * 16 + (junimoText ? 96 : 0), 8, 16);
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public List<KeyValuePair<string, string>> Dialogues { get; set; }
    }

    public class Config
    {
        public string LanguageName { get; set; }
    }
}
