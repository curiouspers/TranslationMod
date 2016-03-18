using StardewValley;
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

namespace TranslationMod
{
    [Mod]
    public class TranslationMod : DiskResource
    {
        public Dictionary<string, Person> Data { get; private set; }

        [Subscribe]
        public void InitializeCallback(InitializeEvent @event)
        {
            var configLocation = Path.Combine(PathOnDisk, "Data.json");
            Data = JsonConvert.DeserializeObject<Dictionary<string, Person>>(Encoding.UTF8.GetString(File.ReadAllBytes(configLocation)));
        }

        [Subscribe]
        public void PastGameLoadedCallback(PostGameLoadedEvent @event)
        {
            var characters = ((Game1)@event.Root.Underlying)._GetLocations().OfType<StardewValley.GameLocation>().SelectMany(l => l.getCharacters());
            foreach (var npc in characters)
            {
                if (npc.Dialogue != null)
                {
                    var dialogues = npc.Dialogue.AsEnumerable().ToArray();
                    foreach (var dialog in dialogues)
                    {
                        var newValue = Data[npc.name].Dialogues.Where(d => d.Key == dialog.Value).Select(d => d.Value).FirstOrDefault();
                        if (!string.IsNullOrEmpty(newValue))
                            npc.Dialogue[dialog.Key] = newValue;
                    }
                }
            }
        }

        [Subscribe]
        public void onAssetLoad(AssetLoadEvent @event)
        {
            if (@event.Name.Contains("Fonts"))
            {
                var font_name = @event.Name.Split('\\').Last();
                var fontFolder = Path.Combine(PathOnDisk, "content\\fonts");
                foreach (var font in Directory.EnumerateFiles(fontFolder).Select(f => Path.GetFileNameWithoutExtension(f)))
                {
                    if (font == font_name)
                    {
                        @event.ReturnValue = @event.Root.LoadResource(Path.Combine(fontFolder, font + ".png"));
                    }
                }
            }
            else if (@event.Name == "LooseSprites\\font_bold")
            {
                @event.ReturnValue = @event.Root.LoadResource(Path.Combine(Path.Combine(PathOnDisk, "content\\fonts"), "font_bold.png"));
            }
        }

        [Subscribe]
        public void onAddHUDMeesage(AddHUDMessageEvent @event)
        {

        }

        [Subscribe]
        public void onDrawWithBorder(DrawWithBorderEvent @event)
        {
            if (@event.Message.Length > 1)
            {

            }
            #region game function drawWithBorder
            var message = @event.Message;
            var borderColor = @event.BorderColor;
            var insideColor = @event.InsideColor;
            var position = @event.Position;
            var rotate = @event.Rotate;
            var scale = @event.Scale;
            var layerDepth = @event.LayerDepth;
            var tiny = @event.Tiny;
            string[] strArray = message.Split(' ');
            int num1 = 0;
            int num2 = 0;
            for (int index = 0; index < Enumerable.Count<string>((IEnumerable<string>)strArray); ++index)
            {
                if (strArray[index].Contains("="))
                {
                    Game1.spriteBatch.DrawString(tiny ? Game1.tinyFont : Game1.dialogueFont, strArray[index],
                        new Vector2(position.X + (float)num1, position.Y), Color.Purple, rotate, Vector2.Zero, scale,
                        SpriteEffects.None, layerDepth);
                    num1 +=
                        (int)
                            ((double)(tiny ? Game1.tinyFont : Game1.dialogueFont).MeasureString(strArray[index]).X +
                             8.0);
                }
                else
                {
                    if (index == 0)
                        Game1.spriteBatch.DrawString(tiny ? Game1.tinyFontBorder : Game1.borderFont, strArray[index],
                            new Vector2(
                                (float)
                                    ((double)position.X + (double)num1 + (double)num2 +
                                     (tiny ? -2.0 * (double)scale : 0.0)), position.Y - (tiny ? 0.0f : 1f)), borderColor,
                            rotate, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
                    else
                        Game1.spriteBatch.DrawString(tiny ? Game1.tinyFontBorder : Game1.borderFont, strArray[index],
                            new Vector2(
                                (float)((double)position.X + (double)num1 + (tiny ? -2.0 * (double)scale : 0.0)),
                                position.Y - (tiny ? 0.0f : 1f)), borderColor, rotate, Vector2.Zero, scale,
                            SpriteEffects.None, layerDepth);
                    Game1.spriteBatch.DrawString(tiny ? Game1.tinyFont : Game1.dialogueFont, strArray[index],
                        new Vector2(position.X + (float)num1, position.Y), insideColor, rotate, Vector2.Zero, scale,
                        SpriteEffects.None, layerDepth);
                    num1 +=
                        (int)
                            ((double)(tiny ? Game1.tinyFont : Game1.dialogueFont).MeasureString(strArray[index]).X +
                             8.0);
                }
            }
            #endregion
            @event.ReturnEarly = true;
        }

        [Subscribe]
        public void onParseText(ParseTextEvent @event)
        {
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
            #endregion
            @event.ReturnEarly = true;
        }

        [Subscribe]
        public void onDrawSpriteText(PreSpriteTextDrawStringEvent @event)
        {
            if(@event.Text.Contains("Loading"))
            {
                @event.Text = @event.Text.Replace("Loading", "Загрузка");
            }
            else if(@event.Text.Contains("Anton"))
            {

            }
            drawString(@event.Sprite, @event.Text, @event.X, @event.Y, @event.CharacterPosition, 
                @event.Width, @event.Height, @event.Alpha, @event.LayerDepth, @event.JunimoText, 
                @event.DrawBGScroll, @event.PlaceHolderScrollWidthText, @event.Color);
            @event.ReturnEarly = true;
        }

        [Subscribe]
        public void DialogueCallback(DrawDialogueEvent @event)
        {
            //var speaker = @event.NPC;
            //var speakerCOntent = new List<KeyValuePair<string, string>>();
            //foreach (var dialog in Data[speaker.Name].Dialogues)
            //{
            //    var speakerUnderline = speaker.Underlying as StardewValley.NPC;
            //    var originalSpeacker = new StardewValley.NPC(speakerUnderline.Sprite, speakerUnderline.Position, speakerUnderline.FacingDirection, speakerUnderline.name);
            //    var translateSpeaker = new StardewValley.NPC(speakerUnderline.Sprite, speakerUnderline.Position, speakerUnderline.FacingDirection, speakerUnderline.name);
            //    var originalDialog = new Dialogue(dialog.Key, originalSpeacker);
            //    originalDialog.getCurrentDialogue();
            //    var translateDialog = new Dialogue(dialog.Value, translateSpeaker);
            //    if (!string.IsNullOrEmpty(translateDialog.getCurrentDialogue()))
            //        speakerCOntent.Add(new KeyValuePair<string, string>(originalDialog.getCurrentDialogue(), translateDialog.getCurrentDialogue()));
            //}
            //try
            //{
            //    var count = speaker.CurrentDialogue.Count;
            //    var translateDialogues = new List<Dialogue>();
            //    foreach (var dialogue in speaker.CurrentDialogue.Cast<Dialogue>().Select((o, i) => new { Value = o, Index = i }))
            //    {
            //        //if(speakerCOntent.ContainsKey(speaker.Name))
            //        //{
            //        var translation = speakerCOntent.Where(d => d.Key == dialogue.Value.getCurrentDialogue()).Select(d => d.Value).FirstOrDefault();
            //        if (!string.IsNullOrEmpty(translation))
            //            dialogue.Value.setCurrentDialogue(translation);
            //        translateDialogues.Add(dialogue.Value);
            //        //}
            //    }
            //    speaker.CurrentDialogue = new System.Collections.Stack(translateDialogues);
            //    //speaker.Name = "Линус";
            //}
            //catch (Exception exc)
            //{

            //}
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
}
