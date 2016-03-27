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
        public Dictionary<string, string> Characters { get; set; }
        private bool _isConfigLoaded = false;
        public List<String> LoadedResources;

        [Subscribe]
        public void InitializeCallback(InitializeEvent @event)
        {
            if (!_isConfigLoaded) LoadConfig();
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
                        var newValue = Data[npc.Name].Dialogues.Where(d => d.Key == dialog.Value).Select(d => d.Value).FirstOrDefault();
                        if (!string.IsNullOrEmpty(newValue))
                            npc.Dialogue[dialog.Key] = newValue;
                    }
                }
            }
        }

        [Subscribe]
        public void onAssetLoad(AssetLoadEvent @event)
        {
            if (!_isConfigLoaded) LoadConfig();
            if (@event.Name.Contains("Fonts"))
            {
                var font_name = @event.Name.Split('\\').Last();
                var fontFolder = Path.Combine(PathOnDisk,"languages",ModConfig.LanguageName, "content", "Fonts");
                var fonts = Directory.EnumerateFiles(fontFolder).Select(f => Path.GetFileNameWithoutExtension(f));
                foreach (var font in fonts)
                {
                    if (font == font_name + ModConfig.LanguageName)
                    {
                        try
                        {
                            var spriteFont = @event.Root.Content.Load<SpriteFont>(@event.Name + ModConfig.LanguageName);
                            @event.ReturnValue = spriteFont;
                        }
                        catch (Exception e)
                        {
                            if (e.Message.Contains("File not found"))
                            {
                                Uri assetPath = new Uri(Path.Combine(fontFolder, font + ".xnb"), UriKind.Absolute);
                                Uri execPath = new Uri(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory));
                                var contentDir = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), @event.Root.Content.RootDirectory, "Fonts");
                                File.Copy(Path.Combine(fontFolder, font + ".xnb"), Path.Combine(contentDir, font + ".xnb"));
                                var spriteFont = @event.Root.Content.Load<SpriteFont>(@event.Name + ModConfig.LanguageName);
                                @event.ReturnValue = spriteFont;
                            }
                            else throw e;
                        }
                    }
                }
            }
            else
            {
                var splitNames = @event.Name.Split('\\');
                if (splitNames.Length > 1)
                {
                    var sprite_name = splitNames.Last();
                    var sprite_subdirectory = splitNames[splitNames.Length - 2];
                    var spriteFolder = Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "content", sprite_subdirectory);
                    if(sprite_name.Contains("beach"))
                    {

                    }
                    if (Directory.Exists(spriteFolder))
                    {
                        var sprites = Directory.EnumerateFiles(spriteFolder).Select(f => Path.GetFileNameWithoutExtension(f));
                        foreach (var sprite in sprites)
                        {
                            if (sprite == sprite_name)
                            {
                                LoadedResources.Add(sprite);
                                if(sprite != "Cursors")
                                    @event.ReturnValue = @event.Root.LoadResource(Path.Combine(spriteFolder, sprite + ".png"));
                            }
                        }
                    }
                }
            }
        }

        [Subscribe]
        public void onGetRandomName(GetRandomNameEvent @event)
        {
            //ЗДЕСЬ АЛГОРИТМ ГЕНЕРАЦИИ ИМЕН НА РУССКОМ!
            //Возвращаемый объект - string
            //@event.ReturnEarly = true;
        }

        [Subscribe]
        public void onOtherFarmerNames(GetOtherFarmerNamesEvent @event)
        {
            //ЗДЕСЬ АЛГОРИТМ ГЕНЕРАЦИИ СПИСКА ИМЕН ФЕРМЕРОВ НА РУССКОМ! 
            //Возвращаемый объект - List<string>
            //@event.ReturnEarly = true;
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
            if (@event.Message == "Map")
            {
                @event.ReturnValue = "Карта";
            }
            if (Data["GrandpaStory"].Dialogues.Where(d => d.Key == @event.Message && !string.IsNullOrEmpty(d.Value)).Count()>0)
            {
                @event.ReturnValue = Data["GrandpaStory"].Dialogues.Find(d => d.Key == @event.Message).Value;
            }
            WriteToScan(@event.Message);
        }

        [Subscribe]
        public void onSpriteFontMeasureString(SpriteFontMeasureStringEvent @event)
        {
            if(@event.Message == "Map")
            {
                @event.ReturnValue = "Карта";
            }
            if (Data["GrandpaStory"].Dialogues.Where(d => d.Key == @event.Message && !string.IsNullOrEmpty(d.Value)).Count() > 0)
            {
                @event.ReturnValue = Data["GrandpaStory"].Dialogues.Find(d => d.Key == @event.Message).Value;
            }
            WriteToScan(@event.Message);
        }

        private void LoadConfig()
        {
            LoadedResources = new List<string>();
               var configLocation = Path.Combine(PathOnDisk, "Config.json");
            if (!File.Exists(configLocation))
            {
                ModConfig = new Config();
                ModConfig.LanguageName = "RU";               
                File.WriteAllBytes(configLocation, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ModConfig)));
            }
            else
            {
                ModConfig = JsonConvert.DeserializeObject<Config>(Encoding.UTF8.GetString(File.ReadAllBytes(configLocation)));
            }
            Data = JsonConvert.DeserializeObject<Dictionary<string, Person>>(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages", ModConfig.LanguageName, "dictionaries", "MainDictionary.json"))));
            Characters = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(PathOnDisk, "languages",ModConfig.LanguageName, "dictionaries", "Characters.json"))));
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
