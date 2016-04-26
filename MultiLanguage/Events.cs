using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiLanguage
{
    public class DetourEvent
    {
        private object returnValue;

        public bool ReturnEarly { get; set; }

        public object ReturnValue
        {
            get { return returnValue; }
            set
            {
                returnValue = value;
                ReturnEarly = true;
            }
        }
    }
    public class SpriteTextDrawStringEvent : DetourEvent
    {
        public SpriteTextDrawStringEvent(SpriteBatch b, string s, int x, int y, int characterPosition,
            int width, int height, float alpha, float layerDepth, bool junimoText,
            int drawBGScroll, string placeHolderScrollWidthText, int color)
        {
            Sprite = b;
            Text = s;
            X = x;
            Y = y;
            CharacterPosition = characterPosition;
            Width = width;
            Height = height;
            Alpha = alpha;
            LayerDepth = layerDepth;
            JunimoText = junimoText;
            DrawBGScroll = drawBGScroll;
            PlaceHolderScrollWidthText = placeHolderScrollWidthText;
            Color = color;
        }

        public SpriteBatch Sprite { get; }
        public string Text { get; set; }
        public int X { get; }
        public int Y { get; }
        public int CharacterPosition { get; }
        public int Width { get; }
        public int Height { get; }
        public float Alpha { get; }
        public float LayerDepth { get; }
        public bool JunimoText { get; }
        public int DrawBGScroll { get; }
        public string PlaceHolderScrollWidthText { get; }
        public int Color { get; }
    }
}
