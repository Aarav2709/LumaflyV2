using Avalonia.Controls;
using Lumafly.Models;
using Lumafly;
using Lumafly.ViewModels;

namespace Lumafly.Views.Pages
{
    public partial class GameLauncherPage : UserControl
    {
        public GameLauncherPage()
        {
            InitializeComponent();
            if (GameSelectCombo is not null)
            {
                GameSelectCombo.ItemsSource = new[] { GameProfiles.HollowKnight.Name, GameProfiles.Silksong.Name };
                var current = Settings.Load()?.CurrentProfile?.Name ?? GameProfiles.HollowKnight.Name;
                GameSelectCombo.SelectedItem = current;
                GameSelectCombo.SelectionChanged += OnGameSelectionChanged;
            }
        }

        private void OnGameSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var selected = GameSelectCombo?.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selected)) return;
            var key = selected == GameProfiles.Silksong.Name ? GameProfiles.SilksongKey : GameProfiles.HollowKnightKey;
            // Use centralized switch to update ManagedFolder and reload app
            _ = MainWindowViewModel.Instance?.SwitchGameAsync(key);
            if (GameTitleText is not null)
                GameTitleText.Text = GameProfiles.GetByKey(key).Name;
        }
    }
}
