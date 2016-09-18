using System;
using System.Collections.Generic;
using System.Diagnostics;
using CocosSharp;

namespace ScrollViewCocosSharp.SpriteSheet
{
    /// <summary>
    /// Offers you the functionality to store graphics in a spritesheet dynamically
    /// </summary>
    public class SpriteSheetCreator
    {
        private Dictionary<object, CCRect> _rectsForSprites = new Dictionary<object, CCRect>();

        private CCRenderTexture _renderTexture;
        private const int RenderTextureMaxWidthAndHeight = 4000; //crashes when texture bigger than 4000x4000

        private readonly Dictionary<object, CCDrawNode> _drawNodes = new Dictionary<object, CCDrawNode>();
        private readonly Dictionary<object, CCLabel> _labels = new Dictionary<object, CCLabel>();

        private bool _successfullyRendered;

        #region Render

        /// <summary>
        /// Render all sprites, execute this method when all objects are added
        /// </summary>
        /// <returns></returns>
        public bool RenderGraphics()
        {
            try
            {
                var actualPosition = new CCPoint(0, 0);
                int highestY = 0;

                foreach (var keyAndDrawNode in _drawNodes)
                {
                    var drawNode = keyAndDrawNode.Value;

                    //new line
                    if ((actualPosition.X + drawNode.BoundingBox.Size.Width) > RenderTextureMaxWidthAndHeight)
                    {
                        actualPosition.X = 0;
                        actualPosition.Y = highestY;
                    }

                    //remember highest y, to avoid intersection
                    if ((actualPosition.Y + drawNode.BoundingBox.Size.Height) > highestY)
                    {
                        highestY = (int)(actualPosition.Y + drawNode.BoundingBox.Size.Height + 1);
                    }

                    var rectPosition = new CCRect(actualPosition.X, actualPosition.Y, drawNode.BoundingBox.Size.Width, drawNode.BoundingBox.Size.Height);

                    drawNode.Position = new CCPoint(rectPosition.MinX, rectPosition.MinY);

                    _rectsForSprites.Add(keyAndDrawNode.Key, rectPosition);

                    actualPosition.X += (drawNode.BoundingBox.Size.Width + 1);
                }

                foreach (var keyAndLabel in _labels)
                {
                    var label = keyAndLabel.Value;

                    //new line
                    if ((actualPosition.X + label.BoundingBox.Size.Width) > RenderTextureMaxWidthAndHeight)
                    {
                        actualPosition.X = 0;
                        actualPosition.Y = highestY;
                    }

                    //remember highest y, to avoid intersection
                    if ((actualPosition.Y + label.BoundingBox.Size.Height) > highestY)
                    {
                        highestY = (int)(actualPosition.Y + label.BoundingBox.Size.Height + 1);
                    }

                    var rectPosition = new CCRect(actualPosition.X, actualPosition.Y, label.BoundingBox.Size.Width, label.BoundingBox.Size.Height);

                    label.Position = rectPosition.Center;

                    _rectsForSprites.Add(keyAndLabel.Key, rectPosition);

                    actualPosition.X += (label.BoundingBox.Size.Width + 1);
                }

                //render the drawn graphic in one big texture
                _renderTexture = new CCRenderTexture(new CCSize(RenderTextureMaxWidthAndHeight, highestY), new CCSize(RenderTextureMaxWidthAndHeight, highestY));
                _renderTexture.Begin();

                foreach (var keyAndDrawNode in _drawNodes)
                {
                    keyAndDrawNode.Value.Visit();
                }

                foreach (var keyAndLabel in _labels)
                {
                    keyAndLabel.Value.Visit();
                }

                _renderTexture.End();


                //mirror all cordinates vertical...nessecary as the texture use another coordinate system
                Dictionary<object, CCRect> verticalMirrorCoordsDrawNode = new Dictionary<object, CCRect>();
                foreach (var keyAndDrawRect in _rectsForSprites)
                {
                    CCRect spriteRect = new CCRect(keyAndDrawRect.Value.MinX, highestY - keyAndDrawRect.Value.MaxY, keyAndDrawRect.Value.Size.Width, keyAndDrawRect.Value.Size.Height);
                    verticalMirrorCoordsDrawNode.Add(keyAndDrawRect.Key, spriteRect);
                }

                _rectsForSprites = verticalMirrorCoordsDrawNode;

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }

            _successfullyRendered = true;
            return true;
        }

        #endregion Render

        #region publicGetter

        /// <summary>
        /// try go create spriteframe
        /// </summary>
        /// <param name="key">key which was entered earlier</param>
        /// <returns>null: not rendered yet or key not found</returns>
        public CCSpriteFrame GetSpriteFrame(object key)
        {
            if (!_successfullyRendered)
                return null;

            if (!_rectsForSprites.ContainsKey(key))
            {
                return null;
            }

            return new CCSpriteFrame(_renderTexture.Sprite.Texture, _rectsForSprites[key]);
        }

        /// <summary>
        /// try go create sprite
        /// </summary>
        /// <param name="key">key which was entered earlier</param>
        /// <param name="requestedSize">requested size, just works if smaller than orignal size (creates the size out of the center)</param>
        /// <returns>null: not rendered yet, rendered size smaller than requested size or key not found</returns>
        public CCSpriteFrame GetSpriteFrame(object key, CCSize requestedSize)
        {
            if (!_successfullyRendered)
            {
                Debug.WriteLine("(11031335): Sprites not rendered yet.");
                return null;
            }

            if (!_rectsForSprites.ContainsKey(key))
            {
                Debug.WriteLine("(11441605): Sprite not found.");
                return null;
            }

            var rect = _rectsForSprites[key];

            if (requestedSize.Width > rect.Size.Width || requestedSize.Height > rect.Size.Height)
            {
                Debug.WriteLine("(11031605): The requested sprite size is too big.");
                return null;
            }

            var finalRect = new CCRect(rect.Center.X - (requestedSize.Width / 2), rect.Center.Y - (requestedSize.Height / 2), requestedSize.Width, requestedSize.Height);

            return new CCSpriteFrame(_renderTexture.Sprite.Texture, finalRect);
        }

        /// <summary>
        /// get rect that shows where on the texture the sprite for the given key is
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public CCRect GetRectOnTexture(object key)
        {
            if (!_rectsForSprites.ContainsKey(key))
            {
                Debug.WriteLine("(23011605): Key for Sprite not found.");
                return new CCRect();
            }

            return _rectsForSprites[key];
        }

        #endregion publicGetter

        #region privateGetter

        /// <summary>
        /// does the key exist in any of the dictionaries?
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool KeyExist(object key)
        {
            return _drawNodes.ContainsKey(key) || _labels.ContainsKey(key);
        }

        #endregion privateGetter

        #region AddGraphics
        /// <summary>
        /// add drawnode
        /// </summary>
        /// <param name="key">any kind of object can be used as a key</param>
        /// <param name="drawnode"></param>
        public bool AddDrawnode(object key, CCDrawNode drawnode)
        {
            if (KeyExist(key))
                return false;

            _drawNodes.Add(key, drawnode);
            return true;
        }

        /// <summary>
        /// add label
        /// </summary>
        /// <param name="key">any kind of object can be used as a key</param>
        /// <param name="label"></param>
        public bool AddLabel(object key, CCLabel label)
        {
            if (KeyExist(key))
                return false;

            _labels.Add(key, label);
            return true;
        }
        #endregion AddGraphics




    }
}
