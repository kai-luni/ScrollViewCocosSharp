using Foundation;
using UIKit;

namespace ScrollViewCocosSharp.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : 
	global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate 
	{
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init ();

			LoadApplication (new ScrollViewCocosSharp.App ());  

			return base.FinishedLaunching (app, options);
		}
	}
}