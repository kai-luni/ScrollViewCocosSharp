using CocosSharp;
using ScrollViewCocosSharp.SpriteSheet;

namespace ScrollViewCocosSharp.ScrollView
{
    /// <summary>
    /// An example of how to use the ScrollView Class
    /// </summary>
    class ScrollViewInAction : ScrollView
    {
        public ScrollViewInAction(CCSize screenSize) : base(screenSize)
        {
            var spCreator = new SpriteSheetCreator();

            CCDrawNode drawNode = new CCDrawNode();
            drawNode.DrawRect(new CCRect(0,0,400,400), CCColor4B.Blue);
            spCreator.AddDrawnode("blueRect", drawNode);

            drawNode = new CCDrawNode();
            drawNode.DrawRect(new CCRect(0, 0, 400, 400), CCColor4B.Green);
            spCreator.AddDrawnode("greenRect", drawNode);

            drawNode = new CCDrawNode();
            drawNode.DrawRect(new CCRect(0, 0, 400, 400), CCColor4B.Yellow);
            spCreator.AddDrawnode("yellowRect", drawNode);

            drawNode = new CCDrawNode();
            drawNode.DrawRect(new CCRect(0, 0, 400, 400), CCColor4B.Red);
            spCreator.AddDrawnode("redRect", drawNode);

            //render the graphics, if false do nothing
            if (!spCreator.RenderGraphics())
                return;

            //draw the large squares in the cornes relatives to the screen edges
            var blue1 = new CCSprite(spCreator.GetSpriteFrame("blueRect", new CCSize(200, 200)))
            {
                Position = new CCPoint(100, 100)
            };
            var green1 = new CCSprite(spCreator.GetSpriteFrame("greenRect", new CCSize(200, 200)))
            {
                Position = new CCPoint(screenSize.Width - 100, screenSize.Height - 100)
            };
            var yellow1 = new CCSprite(spCreator.GetSpriteFrame("yellowRect", new CCSize(200, 200)))
            {
                Position = new CCPoint(100, screenSize.Height - 100)
            };
            var red1 = new CCSprite(spCreator.GetSpriteFrame("redRect", new CCSize(200, 200)))
            {
                Position = new CCPoint(screenSize.Width - 100, 100)
            };


            //draw the small squares relative to the large squares
            var blue2 = new CCSprite(spCreator.GetSpriteFrame("blueRect", new CCSize(100, 100)))
            {
                Position = new CCPoint(red1.Position.X-150, red1.Position.Y+150)
            };
            var green2 = new CCSprite(spCreator.GetSpriteFrame("greenRect", new CCSize(100, 100)))
            {
                Position = new CCPoint(yellow1.Position.X + 150, yellow1.Position.Y - 150)
            };
            var yellow2 = new CCSprite(spCreator.GetSpriteFrame("yellowRect", new CCSize(100, 100)))
            {
                Position = new CCPoint(blue1.Position.X + 150, blue1.Position.Y + 150)
            };
            var red2 = new CCSprite(spCreator.GetSpriteFrame("redRect", new CCSize(100, 100)))
            {
                Position = new CCPoint(green1.Position.X - 150, green1.Position.Y - 150)
            };

            Container.AddChild(blue1);
            Container.AddChild(green1);
            Container.AddChild(yellow1);
            Container.AddChild(red1);
            Container.AddChild(blue2);
            Container.AddChild(green2);
            Container.AddChild(yellow2);
            Container.AddChild(red2);
        }
    }
}
