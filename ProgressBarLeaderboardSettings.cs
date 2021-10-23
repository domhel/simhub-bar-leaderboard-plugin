namespace User.PluginProgressBarLeaderboard
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class ProgressBarLeaderboardSettings
    {
        public int SpeedWarningLevel = 100;
        public int NumberOfDrivers = 30;
    }
}