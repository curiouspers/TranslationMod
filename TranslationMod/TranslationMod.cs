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
using Cyriller;
using Cyriller.Model;

namespace TranslationMod
{
    [Mod]
    public class TranslationMod : DiskResource
    {
        public Config ModConfig { get; private set; }
        public Dictionary<string, Person> Data { get; set; }
        public Dictionary<string, string> DataExe { get; set; }
        public Dictionary<string, string> Characters { get; set; }
        public JObject DataRandName { get; set; }
        private Dictionary<string,int> _languages;
        private Dictionary<string, string> _languageDescriptions;
        private static FuzzyStringDictionary _fuzzyDictionary;
        public static Dictionary<string, string> _mainDictionary;
        private Dictionary<string, string> _keyWords;
        private string _currentLanguage;
        private bool _isConfigLoaded = false;
        public List<String> LoadedResources;
        private bool _isMenuDrawing;
        private static Regex reToSkip = new Regex("^[0-9А-Яа-я: -=.g]+$", RegexOptions.Compiled);
        private static CyrPhrase cyrPhrase;
        private static int IsTranslated;

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
                    Game1.showGlobalMessage("This change will not take effect until you restart the game");
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
            if(ModConfig.LanguageName != "EN")
            {
                //ЗДЕСЬ АЛГОРИТМ ГЕНЕРАЦИИ ИМЕН НА РУССКОМ!
                //Возвращаемый объект - string
                @event.ReturnValue = randomName();
                @event.ReturnEarly = true;
            }
        }

        [Subscribe]
        public void onOtherFarmerNames(GetOtherFarmerNamesEvent @event)
        {
            if (ModConfig.LanguageName != "EN")
            {
                //ЗДЕСЬ АЛГОРИТМ ГЕНЕРАЦИИ СПИСКА ИМЕН ФЕРМЕРОВ НА РУССКОМ!
                //Возвращаемый объект - List<string>
                @event.ReturnValue = getOtherFarmerNames();
                @event.ReturnEarly = true;
            }

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
            if (Data.ContainsKey(npc.Name))
            {
                var newValue = Data[npc.Name].Dialogues.Where(d => d.Key == original).Select(d => d.Value).FirstOrDefault();
                if (!string.IsNullOrEmpty(newValue))
                    dialogue = newValue;
            }
            else if ((dialogue = Translate(@event.Dialogue)) != "") { }
            else dialogue = original;
            ((StardewValley.NPC)@event.NPC.Underlying).CurrentDialogue.Push(new Dialogue(dialogue, (StardewValley.NPC)@event.NPC.Underlying)
            {
                removeOnNextMove = @event.ClearOnMovement
            });
            @event.ReturnEarly = true;
        }

        private int _characterPosition;
        [Subscribe]
        public void onDrawSpriteText(PreSpriteTextDrawStringEvent @event)
        {
            var originalText = @event.Text;
            var translateText = @event.Text;
            if (Characters.ContainsKey(@event.Text))
            {
                translateText = Characters[@event.Text];
            }
            else
            {
                //WriteToScan(@event.Text);var translateMessage = Translate(@event.Text);

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

        [Subscribe]
        public void onGetWidthSpriteText(SpriteTextGetWidthOfStringEvent @event)
        {
            if (IsTranslated > 0)
            {
                IsTranslated = 0;
                return;
            }
            //WriteToScan(@event.Text);
            var translateMessage = Translate(@event.Text);

            if (!string.IsNullOrEmpty(translateMessage))// && translateMessage != @event.Text)
            {
                IsTranslated++;
                @event.Text = translateMessage;
                @event.ReturnValue = @event.Root.GetWidthOfString(translateMessage);
                @event.ReturnEarly = true;
            }
            else if (Characters.ContainsKey(@event.Text))
            {
                IsTranslated++;
                @event.Text = Characters[@event.Text];
                @event.ReturnValue = @event.Root.GetWidthOfString(@event.Text);
                @event.ReturnEarly = true;
            }
        }


        [Subscribe]
        public void onSpriteBatchDrawString(SpriteBatchDrawStringEvent @event)
        {
            var translateMessage = Translate(@event.Message);
            if (!string.IsNullOrEmpty(translateMessage))
            {
                @event.ReturnValue = translateMessage;
            }
            //if (@event.Message == "Map" && ModConfig.LanguageName == "RU")
            //{
            //    @event.ReturnValue = "Карта";
            //}
            //if (Data.ContainsKey("GrandpaStory") && Data["GrandpaStory"].Dialogues.Where(d => d.Key == @event.Message && !string.IsNullOrEmpty(d.Value)).Count()>0)
            //{
            //    @event.ReturnValue = Data["GrandpaStory"].Dialogues.Find(d => d.Key == @event.Message).Value;
            //}
            //WriteToScan(@event.Message);
        }

        [Subscribe]
        public void onParseText(ParseTextEvent @event)
        {
            var translateMessage = Translate(@event.Text);
            if (!string.IsNullOrEmpty(translateMessage))
            {
                @event.Text = translateMessage;
                //@event.ReturnEarly = true;
            }
            #region game function parseText
            var text = @event.Text;
            var whichFont = @event.WhichFont;
            var width = @event.Width;

            if (text == null)
            {
                @event.ReturnValue = "";
                return;
            }
            string str1 = string.Empty;
            string str2 = string.Empty;
            string str3 = text;
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
            @event.ReturnValue = str2 + str1;

            //WriteToScan(@event.ReturnValue.ToString());
            #endregion
            @event.ReturnEarly = true;
        }

        [Subscribe]
        public void onSpriteFontMeasureString(SpriteFontMeasureStringEvent @event)
        {
            //if (reToSkip.IsMatch(@event.Message) || string.IsNullOrEmpty(@event.Message))
            if (string.IsNullOrEmpty(@event.Message))
                    return;
            var translateMessage = Translate(@event.Message);
            if (!string.IsNullOrEmpty(translateMessage))
            {
                @event.ReturnValue = translateMessage;
            }
            //if(@event.Message == "Map" && ModConfig.LanguageName == "RU")
            //{
            //    @event.ReturnValue = "Карта";
            //}
            //if (Data.ContainsKey("GrandpaStory") && Data["GrandpaStory"].Dialogues.Where(d => d.Key == @event.Message && !string.IsNullOrEmpty(d.Value)).Count() > 0)
            //{
            //    @event.ReturnValue = Data["GrandpaStory"].Dialogues.Find(d => d.Key == @event.Message).Value;
            //}
            //WriteToScan(@event.Message);
        }

        private string Translate(string message)
        {
            if (ModConfig.LanguageName != "EN")
            {
                if (string.IsNullOrEmpty(message) || reToSkip.IsMatch(message))
                    return "";
                if (_mainDictionary.ContainsKey(message))
                {
                    return _mainDictionary[message];
                }
                else if (_fuzzyDictionary.ContainsKey(message))
                {
                    return _fuzzyDictionary[message];
                }
            }
            return "";
        }

        public static string Decline(string message, string _case) {
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


        private void LoadConfig(string ContentRoot)
        {
            _languages = new Dictionary<string,int>();
            _fuzzyDictionary = new FuzzyStringDictionary();
            _mainDictionary = new Dictionary<string, string>();
            _keyWords = new Dictionary<string, string>();
            Data = new Dictionary<string, Person>();
            Characters = new Dictionary<string, string>();
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
            var dictionariesFolder = Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "dictionaries");
            if (Directory.Exists(dictionariesFolder) && Directory.GetFiles(dictionariesFolder).Count() > 0)
            {
                foreach (var dict in Directory.GetFiles(dictionariesFolder))
                {
                    var dictName = Path.GetFileName(dict);
                    if (dictName == "KeyWords.json")
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var val in jo)
                        {
                            var pair = JObject.Parse(val.Value.ToString());
                            foreach (var row in pair)
                            {
                                //Console.WriteLine(row.Key);
                                AddPairToDict(row.Key, row.Value.ToString(), _mainDictionary);
                            }
                            if (!_keyWords.ContainsKey(val.Key))
                            {
                                _keyWords.Add(val.Key, val.Value.ToString());
                            }
                        }
                    }
                    else if (dictName == "MainDictionary.json")
                    {
                        Data = JsonConvert.DeserializeObject<Dictionary<string, Person>>(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach (var pair in Data)
                        {
                            if (pair.Key == "BigCraftablesInformation")
                            {
                                foreach (var row in pair.Value.Dialogues)
                                {
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 0); // name
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 4); // desc
                                }
                            }
                            if (pair.Key == "Blueprints")
                            {
                                foreach (var row in pair.Value.Dialogues)
                                {
                                    if (row.Key.Split('/').Length > 9)
                                    {
                                        AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 8); // desc
                                        AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 9); // type
                                    }
                                    else
                                    {
                                        AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 4); // desc
                                    }
                                }
                            }
                            if (pair.Key == "Boots" || pair.Key == "CookingChannel" || pair.Key == "Fish" || pair.Key == "weapons")
                            {
                                foreach (var row in pair.Value.Dialogues)
                                {
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 0); // name
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 1); // desc
                                }
                            }
                            if (pair.Key == "Bundles")
                            {
                                foreach (var row in pair.Value.Dialogues)
                                {
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 0); // name
                                }
                            }
                            if (pair.Key == "ObjectInformation")
                            {
                                foreach (var row in pair.Value.Dialogues)
                                {
                                    if (row.Key.Split('/').Length > 4)
                                    {
                                        AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 0); // name
                                        AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 4); // desc
                                    }
                                    else
                                    {
                                        AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 0); // name
                                        AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 1); // desc
                                    }
                                }
                            }
                            if (pair.Key == "Quests")
                            {
                                foreach (var row in pair.Value.Dialogues)
                                {
                                    
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 1); // name
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 2); // desc
                                    AddPairToDictFromIndex(row.Key, row.Value, _mainDictionary, 3); // goal
                                }
                            }
                        }

                    }
                    else if (dictName == "Characters.json")
                    {
                        Characters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                        foreach(var pair in Characters)
                            AddPairToDict(pair.Key, pair.Value.ToString(), _mainDictionary);
                    }
                    else if (dictName == "nameGen.json")
                    {
                        DataRandName = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)));
                    }
                    else
                    {
                        var jo = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(dict)).Replace("@newline", Environment.NewLine));
                        foreach(var pair in jo)
                        {
                            if(pair.Key.Contains("@"))
                            {
                                if (!_fuzzyDictionary.ContainsKey(pair.Key))
                                    _fuzzyDictionary.Add(pair.Key,pair.Value.ToString());
                                else if (_fuzzyDictionary[pair.Key] == "" && pair.Value.ToString() != "")
                                    _fuzzyDictionary[pair.Key] = pair.Value.ToString();
                            }
                            else
                            {
                                AddPairToDict(pair.Key, pair.Value.ToString(), _mainDictionary);
                            }
                        }

                    }
                }
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

            var collection = new CyrNounCollection();
            var adjectives = new CyrAdjectiveCollection();
            cyrPhrase = new CyrPhrase(collection, adjectives);
            _isConfigLoaded = true;

        }

        private void AddPairToDictFromIndex(string key, string value, Dictionary<string, string> dict, int index)
        {
            key = !string.IsNullOrEmpty(key) ? key.Split('/')[index] : "";
            value = !string.IsNullOrEmpty(value) ? value.Split('/')[index] : "";
            AddPairToDict(key, value, _mainDictionary);
        }

        private void AddPairToDict(string key, string value, Dictionary<string, string> dict) {
            if (!dict.ContainsKey(key))
                dict.Add(key, value);
            else if (dict[key] == "" && value != "")
                dict[key] = value;
        }

        void WriteToScan(string line)
        {
            if (string.IsNullOrEmpty(line) || reToSkip.IsMatch(line))
                return;
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
