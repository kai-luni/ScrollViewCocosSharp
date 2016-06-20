using System;
using System.Collections.Generic;
using System.Diagnostics;
using CocosSharp;

namespace ScrollViewCocosSharp.ScrollView
{
    #region Enums

    public enum CcScrollViewDirection
    {
        None = -1,
        Horizontal = 0,
        Vertical,
        Both
    }

    #endregion Enums


    public interface ICcScrollViewDelegate
    {
        void ScrollViewDidScroll(ScrollView view);
        void ScrollViewDidZoom(ScrollView view);
    }

    /// <summary>
    /// Scrollview implementation for CocosSharp
    /// </summary>
    public class ScrollView : CCNode
    {
        const float ScrollDeaccelRate = 0.95f;
        const float ScrollDeaccelDist = 1.0f;
        const float BounceDuration = 0.15f;
        const float InsetRatio = 0.2f;
        const float MoveInch = 7.0f / 160.0f;

        bool _clippingToBounds;
        bool _isTouchEnabled;
        float _touchLength;

        float _minScale;
        float _maxScale;
        CCPoint _minInset;
        CCPoint _maxInset;

        readonly List<CCTouch> _touches;
        CCPoint _scrollDistance;

        CCPoint _touchPoint;
        CCSize _viewSize;

        CCNode _container;

        CCEventListener _touchListener;


        #region Properties

        public bool Bounceable { get; set; }
        public bool Dragging { get; private set; }
        //dragging gets sometimes activated too easy, with the starttime some delay cen be implemented
        private DateTime _dragginStartTime = DateTime.Now;  
        public bool IsTouchMoved { get; private set; }
        public CcScrollViewDirection Direction { get; set; }
        public ICcScrollViewDelegate Delegate { get; set; }

        public bool ClippingToBounds
        {
            get { return _clippingToBounds; }
            set
            {
                _clippingToBounds = value;
                if (Layer != null)
                    Layer.ChildClippingMode = _clippingToBounds ? CCClipMode.Bounds : CCClipMode.None;
            }
        }

        public bool TouchEnabled
        {
            get { return _isTouchEnabled; }
            set
            {

                if (value != _isTouchEnabled)
                {
                    _isTouchEnabled = value;

                    if (_isTouchEnabled)
                    {
                        // Register Touch Event
                        var touchListener = new CCEventListenerTouchOneByOne
                        {
                            IsSwallowTouches = true,
                            OnTouchBegan = TouchBegan,
                            OnTouchMoved = TouchMoved,
                            OnTouchEnded = TouchEnded,
                            OnTouchCancelled = TouchCancelled
                        };


                        AddEventListener(touchListener);

                        _touchListener = touchListener;
                    }
                    else
                    {
                        Dragging = false;
                        IsTouchMoved = false;
                        _touches.Clear();
                        RemoveEventListener(_touchListener);
                        _touchListener = null;
                    }
                }

            }
        }

        public float MinScale
        {
            get { return _minScale; }
            set
            {
                _minScale = value;
                ZoomScale = ZoomScale;
            }
        }

        public float MaxScale
        {
            get { return _maxScale; }
            set
            {
                _maxScale = value;
                ZoomScale = ZoomScale;
            }
        }

        public float ZoomScale
        {
            get { return _container.ScaleX; }
            set
            {
                if (Math.Abs(_container.ScaleX - value) > 0.01)
                {
                    CCPoint center;

                    center = _touchLength < 0.001f ? new CCPoint(_viewSize.Width * 0.5f, _viewSize.Height * 0.5f) : _touchPoint;


                    //keep the same absolue position on screen after zoom
                    var absX = (-ContentOffset.X + center.X) / ZoomScale;
                    var absY = (-ContentOffset.Y + center.Y) / ZoomScale;
                    var newZoom = Math.Max(_minScale, Math.Min(_maxScale, value));
                    absX *= newZoom;
                    absY *= newZoom;
                    absX -= center.X;
                    absY -= center.Y;
                    absX = -absX;
                    absY = -absY;

                    _container.Scale = Math.Max(_minScale, Math.Min(_maxScale, value));
                    var offset = new CCPoint(absX, absY);

                    if (Delegate != null)
                    {
                        Delegate.ScrollViewDidZoom(this);
                    }

                    SetContentOffset(offset);
                }
            }
        }

        public CCPoint ContentOffset
        {
            get { return _container.Position; }
            set { SetContentOffset(value); }
        }

        public CCPoint MinContainerOffset
        {
            get
            {
                return new CCPoint(_viewSize.Width - _container.ContentSize.Width * _container.ScaleX,
                    _viewSize.Height - _container.ContentSize.Height * _container.ScaleY);
            }
        }

        public CCPoint MaxContainerOffset
        {
            get { return CCPoint.Zero; }
        }

        public override CCSize ContentSize
        {
            get { return _container.ContentSize; }
            set
            {
                if (Container != null)
                {
                    Container.ContentSize = value;
                    UpdateInset();
                }
            }
        }

        /**
        * size to clip. CCNode boundingBox uses contentSize directly.
        * It's semantically different what it actually means to common scroll views.
        * Hence, this scroll view will use a separate size property.
        */

        public CCSize ViewSize
        {
            get { return _viewSize; }
            set
            {
                _viewSize = value;
                base.ContentSize = value;
            }
        }

        CCRect ViewRect
        {
            get
            {
                var rect = new CCRect(0, 0, _viewSize.Width, _viewSize.Height);
                return CCAffineTransform.Transform(rect, AffineWorldTransform);
            }
        }

        public CCNode Container
        {
            get { return _container; }
            set
            {
                if (value == null)
                {
                    return;
                }

                RemoveAllChildren();
                _container = value;

                _container.IgnoreAnchorPointForPosition = false;
                _container.AnchorPoint = CCPoint.Zero;

                AddChild(_container);

                ViewSize = _viewSize;
            }
        }

        #endregion Properties


        #region Constructors

        public ScrollView()
            : this(new CCSize(200, 200), null)
        {
        }

        public ScrollView(CCSize size)
            : this(size, null)
        {
        }

        public ScrollView(CCSize size, CCNode container)
        {
            if (container == null)
            {
                container = new CCLayer
                {
                    IgnoreAnchorPointForPosition = false,
                    AnchorPoint = CCPoint.Zero
                };
            }
            container.Position = new CCPoint(0.0f, 0.0f);

            _container = container;

            ViewSize = size;
            TouchEnabled = true;
            Delegate = null;
            Bounceable = true;
            ClippingToBounds = true;
            Direction = CcScrollViewDirection.Both;
            MinScale = MaxScale = 1.0f;
            _touches = new List<CCTouch>();
            _touchLength = 0.0f;
            _minScale = 0.5f;
            _maxScale = 6.5f;


            AddChild(container);
        }


        #endregion Constructors


        static float ConvertDistanceFromPointToInch(float pointDis)
        {
            const float factor = 1.0f; //(CCDrawManager.SharedDrawManager.ScaleX + CCDrawManager.SharedDrawManager.ScaleY) / 2;
            return pointDis * factor / CCDevice.DPI;
        }

        /**
		* Determines if a given node's bounding box is in visible bounds
		*
		* @return YES if it is in visible bounds
		*/

        public bool IsNodeVisible(CCNode node)
        {
            CCPoint offset = ContentOffset;
            CCSize size = ViewSize;
            float scale = ZoomScale;

            var viewRect = new CCRect(-offset.X / scale, -offset.Y / scale, size.Width / scale, size.Height / scale);

            return viewRect.IntersectsRect(node.BoundingBox);
        }

        public void UpdateInset()
        {
            if (Container != null)
            {
                _maxInset = MaxContainerOffset;
                _maxInset = new CCPoint(_maxInset.X + _viewSize.Width * InsetRatio, _maxInset.Y + _viewSize.Height * InsetRatio);
                _minInset = MinContainerOffset;
                _minInset = new CCPoint(_minInset.X - _viewSize.Width * InsetRatio, _minInset.Y - _viewSize.Height * InsetRatio);
            }
        }

        public void SetContentOffset(CCPoint offset, bool animated = false)
        {
            if (animated)
            {
                //animate scrolling
                SetContentOffsetInDuration(offset, BounceDuration);
            }
            else
            {
                //set the _container position directly
                if (!Bounceable)
                {
                    CCPoint minOffset = MinContainerOffset;
                    CCPoint maxOffset = MaxContainerOffset;

                    offset.X = Math.Max(minOffset.X, Math.Min(maxOffset.X, offset.X));
                    offset.Y = Math.Max(minOffset.Y, Math.Min(maxOffset.Y, offset.Y));
                }

                _container.Position = offset;


                if (Delegate != null)
                {
                    Delegate.ScrollViewDidScroll(this);
                }
            }
        }

        /**
        * Sets a new content offset. It ignores max/min offset. It just sets what's given. (just like UIKit's UIScrollView)
        * You can override the animation duration with this method.
        *
        * @param offset new offset
        * @param animation duration
        */

        public void SetContentOffsetInDuration(CCPoint offset, float dt)
        {
            var scroll = new CCMoveTo(dt, offset);
            var expire = new CCCallFuncN(StoppedAnimatedScroll);
            _container.RunAction(new CCSequence(scroll, expire));
            Schedule(PerformedAnimatedScroll);
        }

        public void SetZoomScale(float value, bool animated = false)
        {
            if (animated)
            {
                SetZoomScaleInDuration(value, BounceDuration);
            }
            else
            {
                ZoomScale = value;
            }
        }

        public void SetZoomScaleInDuration(float s, float dt)
        {
            if (dt > 0)
            {
                if (Math.Abs(_container.ScaleX - s) > 0.1)
                {
                    var scaleAction = new CCActionTween(dt, "zoomScale", _container.ScaleX, s);
                    RunAction(scaleAction);
                }
            }
            else
            {
                ZoomScale = s;
            }
        }

        /**
        * Provided to make scroll view compatible with SWLayer's pause method
        */

        public void Pause(object sender)
        {
            _container.Pause();

            var children = _container.Children;

            if (children != null)
            {
                foreach (CCNode child in children)
                {
                    child.Pause();
                }
            }
        }

        /**
        * Provided to make scroll view compatible with SWLayer's resume method
        */

        public void Resume(object sender)
        {
            var children = _container.Children;

            if (children != null)
            {
                foreach (CCNode child in children)
                {
                    child.Resume();
                }
            }

            _container.Resume();
        }

        #region Event handling

        /** override functions */
        public virtual bool TouchBegan(CCTouch pTouch, CCEvent touchEvent)
        {

            if (!Visible)
            {
                return false;
            }

            var frame = ViewRect;

            //dispatcher does not know about clipping. reject _touches outside visible bounds.
            if (_touches.Count > 2 ||
                IsTouchMoved ||
                !frame.ContainsPoint(_container.Layer.ScreenToWorldspace(pTouch.LocationOnScreen)))
            {
                return false;
            }

            if (!_touches.Contains(pTouch))
            {
                _touches.Add(pTouch);
            }

            if (_touches.Count == 1)
            {
                // scrolling
                _touchPoint = Layer.ScreenToWorldspace(pTouch.LocationOnScreen);
                IsTouchMoved = false;
                Dragging = true; //Dragging started
                _dragginStartTime = DateTime.Now;
                _scrollDistance = CCPoint.Zero;
                _touchLength = 0.0f;
            }
            else if (_touches.Count == 2)
            {
                _touchPoint = CCPoint.Midpoint(Layer.ScreenToWorldspace(_touches[0].LocationOnScreen), Layer.ScreenToWorldspace(_touches[1].LocationOnScreen));
                _touchLength = CCPoint.Distance(_container.Layer.ScreenToWorldspace(_touches[0].LocationOnScreen), _container.Layer.ScreenToWorldspace(_touches[1].LocationOnScreen));
                Dragging = false;
            }
            return true;
        }

        #region overwrite

        /// <summary>
        /// takes care of dragging and zoom
        /// </summary>
        /// <param name="touch"></param>
        /// <param name="touchEvent"></param>
        public virtual void TouchMoved(CCTouch touch, CCEvent touchEvent)
        {
            if (!Visible)
            {
                return;
            }

            if (_touches.Contains(touch))
            {


                if (_touches.Count == 1 && Dragging)
                {// scrolling
                    CCPoint moveDistance, newPoint; //, _maxInset, _minInset;

                    var frame = ViewRect;

                    newPoint = Layer.ScreenToWorldspace(_touches[0].LocationOnScreen);
                    moveDistance = newPoint - _touchPoint;

                    float dis;
                    if (Direction == CcScrollViewDirection.Vertical)
                    {
                        dis = moveDistance.Y;
                    }
                    else if (Direction == CcScrollViewDirection.Horizontal)
                    {
                        dis = moveDistance.X;
                    }
                    else
                    {
                        dis = (float)Math.Sqrt(moveDistance.X * moveDistance.X + moveDistance.Y * moveDistance.Y);
                    }

                    if (!IsTouchMoved && Math.Abs(ConvertDistanceFromPointToInch(dis)) < MoveInch)
                    {
                        //CCLOG("Invalid movement, distance = [%f, %f], disInch = %f", moveDistance.x, moveDistance.y);
                        return;
                    }

                    if (!IsTouchMoved)
                    {
                        moveDistance = CCPoint.Zero;
                    }

                    _touchPoint = newPoint;
                    IsTouchMoved = true;

                    if (frame.ContainsPoint(_touchPoint))
                    {
                        switch (Direction)
                        {
                            case CcScrollViewDirection.Vertical:
                                moveDistance = new CCPoint(0.0f, moveDistance.Y);
                                break;
                            case CcScrollViewDirection.Horizontal:
                                moveDistance = new CCPoint(moveDistance.X, 0.0f);
                                break;
                        }

                        float newX = _container.Position.X + moveDistance.X;
                        float newY = _container.Position.Y + moveDistance.Y;

                        Debug.WriteLine("Scroll Distance: " + _scrollDistance);
                        _scrollDistance = moveDistance;
                        SetContentOffset(new CCPoint(newX, newY));
                    }
                }
                else if (_touches.Count == 2 && !Dragging)
                {
                    //2 fingers no dragging? zoom

                    float len = CCPoint.Distance(Layer.ScreenToWorldspace(_touches[0].LocationOnScreen),
                        Layer.ScreenToWorldspace(_touches[1].LocationOnScreen));



                    var differenceTouchLength = (len / _touchLength);
                    ZoomScale = (differenceTouchLength < 1)
                        ? ZoomScale * (1 - (1 - differenceTouchLength) * 0.5f)
                        : ZoomScale * (1 + (differenceTouchLength - 1) * 0.5f);

                    _touchLength = len;

                }


            }
        }

        public virtual void TouchEnded(CCTouch touch, CCEvent touchEvent)
        {
            if (!Visible)
            {
                return;
            }

            if (_touches.Contains(touch))
            {
                if (_touches.Count == 1 && IsTouchMoved)
                {
                    Schedule(DeaccelerateScrolling);
                }
                _touches.Remove(touch);

            }

            if (_touches.Count == 0)
            {
                Dragging = false;
                IsTouchMoved = false;
            }
        }

        public virtual void TouchCancelled(CCTouch touch, CCEvent touchEvent)
        {
            if (!Visible)
            {
                return;
            }
            _touches.Remove(touch);
            if (_touches.Count == 0)
            {
                Dragging = false;
                IsTouchMoved = false;
            }
        }

        #endregion overwrite

        #endregion Event handling

        protected override void AddedToScene()
        {
            base.AddedToScene();

            // We set our Child Clipping Mode here
            Layer.ChildClippingMode = _clippingToBounds ? CCClipMode.Bounds : CCClipMode.None;
        }

        public override void AddChild(CCNode child, int zOrder, int tag)
        {
            child.IgnoreAnchorPointForPosition = false;
            // child.AnchorPoint = CCPoint.Zero;
            if (_container != child)
            {
                _container.AddChild(child, zOrder, tag);
            }
            else
            {
                base.AddChild(child, zOrder, tag);
            }
        }


        /**
        * Relocates the _container at the proper offset, in bounds of max/min offsets.
        *
        * @param animated If YES, relocation is animated
        */

        void RelocateContainer(bool animated)
        {
            CCPoint min = MinContainerOffset;
            CCPoint max = MaxContainerOffset;

            CCPoint oldPoint = _container.Position;

            float newX = oldPoint.X;
            float newY = oldPoint.Y;
            if (Direction == CcScrollViewDirection.Both || Direction == CcScrollViewDirection.Horizontal)
            {
                newX = Math.Min(newX, max.X);
                newX = Math.Max(newX, min.X);
            }

            if (Direction == CcScrollViewDirection.Both || Direction == CcScrollViewDirection.Vertical)
            {
                newY = Math.Min(newY, max.Y);
                newY = Math.Max(newY, min.Y);
            }

            if (Math.Abs(newY - oldPoint.Y) > 0.1 || Math.Abs(newX - oldPoint.X) > 0.1)
            {
                SetContentOffset(new CCPoint(newX, newY), animated);
            }
        }


        /// <summary>
        /// implements auto-scrolling behavior. change ScrollDeaccelRate as needed to choose
        /// deacceleration speed. it must be less than 1.0f.
        /// 
        /// I took the inset bounds out for now, it did not work (minInset and maxInset were 0)
        /// </summary>
        /// <param name="dt">delta</param>
        void DeaccelerateScrolling(float dt)
        {
            //make the cancellation less sensitive
            if (Dragging && (_dragginStartTime-DateTime.Now).TotalMilliseconds < 50)
            {
                Unschedule(DeaccelerateScrolling);
                return;
            }

            CCPoint maxInset, minInset;

            _container.Position = _container.Position + _scrollDistance;

            if (Bounceable)
            {
                maxInset = _maxInset;
                minInset = _minInset;
            }
            else
            {
                maxInset = MaxContainerOffset;
                minInset = MinContainerOffset;
            }

            //check to see if offset lies within the inset bounds
            //float newX = Math.Min(_container.Position.X, maxInset.X);
            //newX = Math.Max(newX, minInset.X);
            //float newY = Math.Min(_container.Position.Y, maxInset.Y);
            //newY = Math.Max(newY, minInset.Y);

            float newX = _container.Position.X;
            float newY = _container.Position.Y;

            //_scrollDistance = _scrollDistance - new CCPoint(newX - _container.Position.X, newY - _container.Position.Y);
            _scrollDistance = _scrollDistance * ScrollDeaccelRate;
            SetContentOffset(new CCPoint(newX, newY));

            Debug.WriteLine("ScrollDistance: " + _scrollDistance);

            //if ((Math.Abs(_scrollDistance.X) <= ScrollDeaccelDist &&
            //     Math.Abs(_scrollDistance.Y) <= ScrollDeaccelDist) ||
            //    newY > maxInset.Y || newY < minInset.Y ||
            //    newX > maxInset.X || newX < minInset.X ||
            //    newX == maxInset.X || newX == minInset.X ||
            //    newY == maxInset.Y || newY == minInset.Y)
            //{
            //    Unschedule(DeaccelerateScrolling);
            //    //RelocateContainer(true);
            //}

            if ((Math.Abs(_scrollDistance.X) <= ScrollDeaccelDist &&
                 Math.Abs(_scrollDistance.Y) <= ScrollDeaccelDist))
            {
                Unschedule(DeaccelerateScrolling);
            }
        }

        /**
        * This method makes sure auto scrolling causes delegate to invoke its method
        */

        void PerformedAnimatedScroll(float dt)
        {
            if (Dragging)
            {
                Unschedule(PerformedAnimatedScroll);
                return;
            }

            if (Delegate != null)
            {
                Delegate.ScrollViewDidScroll(this);
            }
        }

        /**
        * Expire animated scroll delegate calls
        */

        void StoppedAnimatedScroll(CCNode node)
        {
            Unschedule(PerformedAnimatedScroll);
            // After the animation stopped, "scrollViewDidScroll" should be invoked, this could fix the bug of lack of tableview cells.
            if (Delegate != null)
            {
                Delegate.ScrollViewDidScroll(this);
            }
        }
    }
}
