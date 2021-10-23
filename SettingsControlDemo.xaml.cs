using System.Windows.Controls;

namespace User.PluginProgressBarLeaderboard
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControlDemo : UserControl
    {
        public ProgressBarLeaderboard Plugin { get; }

        public SettingsControlDemo()
        {
            InitializeComponent();
        }

        public SettingsControlDemo(ProgressBarLeaderboard plugin) : this()
        {
            this.Plugin = plugin;
        }


    }
}
