using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;
using CocosSharp;
using ScrollViewCocosSharp.ScrollView;


namespace ScrollViewCocosSharp
{
    public class GamePage : ContentPage
    {
        CocosSharpView _gameView;

        private double width = 0;
        private double height = 0;

        public GamePage()
        {
            //_gameView = new CocosSharpView()
            //{
            //    HorizontalOptions = LayoutOptions.FillAndExpand,
            //    VerticalOptions = LayoutOptions.FillAndExpand,
            //    // Set the game world dimensions
            //    //DesignResolution = new Size(1024, 768),
            //    // Set the method to call once the view has been initialised
            //    ViewCreated = LoadGame
            //};

            //Content = _gameView;
        }

        protected override void OnDisappearing()
        {
            if (_gameView != null)
            {
                _gameView.Paused = true;
            }

            base.OnDisappearing();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_gameView != null)
                _gameView.Paused = false;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height); //must be called
            if (this.width != width || this.height != height)
            {
                this.width = width;
                this.height = height;
                CreateNewGameView();
            }
            
        }

        void CreateNewGameView()
        {
            _gameView = new CocosSharpView()
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                // Set the game world dimensions
                //DesignResolution = new Size(1024, 768),
                // Set the method to call once the view has been initialised
                ViewCreated = LoadGame
            };

            Content = _gameView;
        }

        void LoadGame(object sender, EventArgs e)
        {
            var nativeGameView = sender as CCGameView;

            if (nativeGameView != null)
            {
                var contentSearchPaths = new List<string>() { "Fonts", "Sounds" };
                CCSizeI viewSize = nativeGameView.ViewSize;
                CCSizeI designResolution = nativeGameView.DesignResolution;

                _gameView.DesignResolution = new Size(viewSize.Width, viewSize.Height);

                nativeGameView.ContentManager.SearchPaths = contentSearchPaths;

                //create a scrollview with the correct viewsize and an area to move in the size of 3000 by 3000
                var scrollView = new ScrollViewInAction(nativeGameView.ViewSize);
                scrollView.BouncingRectSize = new CCSize(3000, 3000);

                var scrollLayer = new CCLayerColor(CCColor4B.White);
                scrollLayer.AddChild(scrollView);

                CCScene gameScene = new CCScene(nativeGameView);
                gameScene.AddLayer(scrollLayer);
                nativeGameView.RunWithScene(gameScene);
            }
        }
    }
}
