using Avalonia.Markup.Xaml;
using Needlelight.ViewModels;

namespace Needlelight.Views.Pages
{
    public partial class SettingsView : View<SettingsViewModel>
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

