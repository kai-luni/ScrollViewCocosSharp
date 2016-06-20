using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CocosSharp;

namespace ScrollViewCocosSharp.ScrollView
{
    class ScrollViewImplementation : ScrollView
    {
        public ScrollViewImplementation(CCSize screenSize) : base(screenSize)
        {
            CCDrawNode drawNode = new CCDrawNode();

            drawNode.DrawRect(new CCRect(0,0,200,200), CCColor4B.Blue, 2, CCColor4B.Red);
            drawNode.DrawRect(new CCRect(1800, 1800, 200, 200), CCColor4B.Green, 2, CCColor4B.Red);
            drawNode.DrawRect(new CCRect(1800, 0, 200, 200), CCColor4B.Yellow, 2, CCColor4B.Red);
            drawNode.DrawRect(new CCRect(0, 1800, 200, 200), CCColor4B.Red, 2, CCColor4B.Blue);

            Container.AddChild(drawNode);
        }
    }
}
