
using BLE.Client.ViewModels;
using MvvmCross.Forms.Views;
using Xamarin.Forms;

namespace BLE.Client.Pages
{
    public partial class PageMainMenu : MvxContentPage<ViewModelMainMenu>
    {
        public PageMainMenu()
        {
            InitializeComponent();
            //this.Title = "CSL RFID Reader (C# " + DependencyService.Get<IAppVersion>().GetVersion() + ")";
            
            // Subscribe to size changed event to detect orientation changes
            this.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, System.EventArgs e)
        {
            // Determine if device is in landscape or portrait mode
            bool isLandscape = this.Width > this.Height;
            
            // Toggle visibility for all menu items based on orientation
            ToggleLayoutVisibility(isLandscape);
        }

        private void ToggleLayoutVisibility(bool isLandscape)
        {
            // Toggle visibility for all 8 menu items
            PortraitLayout1.IsVisible = !isLandscape;
            LandscapeLayout1.IsVisible = isLandscape;
            
            PortraitLayout2.IsVisible = !isLandscape;
            LandscapeLayout2.IsVisible = isLandscape;
            
            PortraitLayout3.IsVisible = !isLandscape;
            LandscapeLayout3.IsVisible = isLandscape;
            
            PortraitLayout4.IsVisible = !isLandscape;
            LandscapeLayout4.IsVisible = isLandscape;
            
            PortraitLayout5.IsVisible = !isLandscape;
            LandscapeLayout5.IsVisible = isLandscape;
            
            PortraitLayout6.IsVisible = !isLandscape;
            LandscapeLayout6.IsVisible = isLandscape;
            
            PortraitLayout7.IsVisible = !isLandscape;
            LandscapeLayout7.IsVisible = isLandscape;
            
            PortraitLayout8.IsVisible = !isLandscape;
            LandscapeLayout8.IsVisible = isLandscape;
        }
    }
}