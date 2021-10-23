using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;


namespace User.PluginProgressBarLeaderboard
{
    // Data that will be sent as a simhub property
    class Driver
    {
        public string Name;
        public double GapToLeader;
        public double ProgressBarValue; // between 0.1 and 1.0
        public double DeltaToDriverAhead;
    }



    [PluginDescription("Visualize the current position of all drivers with a bar chart")]
    [PluginAuthor("Dominik Helfenstein")]
    [PluginName("Progress Bar Leaderboard")]
    public class ProgressBarLeaderboard : IPlugin, IDataPlugin, IWPFSettings
    {

        public ProgressBarLeaderboardSettings Settings;


        /// <summary>
        /// Instance of the current plugin manager
        /// </summary>
        public PluginManager PluginManager { get; set; }

        List<Opponent> opponentsWithGapJump { get; set; } = new List<Opponent>();
        List<Driver> LastValidDrivers { get; set; } = new List<Driver>();

        /// <summary>
        /// Called one time per game data update, contains all normalized game data, 
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        /// 
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // Define the value of our property (declared in init)
            DateTime now = DateTime.Now;
            pluginManager.SetPropertyValue("FrameTime", this.GetType(), data.FrameTime);

            if (data.GameRunning)
            {
                if (data.OldData != null && data.NewData != null)
                {
                    //double[] cars = new double[data.NewData.Opponents.Count];
                    List<Driver> drivers = new List<Driver>();
                    double max = 0.0;
                    double min = double.MaxValue;
                    bool driversMustBeSorted = false;
                    for (int i = 0; i < data.NewData.Opponents.Count; ++i)
                    {
                        var opponent = data.NewData.Opponents[i];
                        if (opponent.GaptoLeader == null) continue;
                        var oldDataOpponent = data.OldData.Opponents.Find(o => o.Name == opponent.Name);
                        if (oldDataOpponent == null) continue;
                        Driver driver = new Driver();
                        driver.Name = opponent.Name;
                        drivers.Add(driver);

                        // check for jumps in gap times.
                        // this always occurs when the car passes start/finish line
                        int oppoGapIndex = opponentsWithGapJump.FindIndex(o => o.Name == opponent.Name);
                        double gapDiff = (double)opponent.GaptoLeader - (double)oldDataOpponent.GaptoLeader;
                        if (Math.Abs(gapDiff) > 10.0)
                        {
                            // big jump in gap
                            // jump up -> bug started. not in list. add to list
                            // jump down -> bug stopped. in list. remove from list
                            //SimHub.Logging.Current.Info($"Jump in gap for {opponent.Name}! Gap is {(double)opponent.GaptoLeader}; old gap: {(double)oldDataOpponent.GaptoLeader}; Lap old: {oldDataOpponent.CurrentLap}, new: {opponent.CurrentLap}. high precision: {oldDataOpponent.CurrentLapHighPrecision} & {opponent.CurrentLapHighPrecision}");

                            if (oppoGapIndex == -1)
                            {
                                // add to list. but only if its a jump upwards
                                if (gapDiff > 0.0)
                                {
                                    opponentsWithGapJump.Add(oldDataOpponent);
                                    // can't use this data. use old instead
                                    drivers[i].GapToLeader = (double)oldDataOpponent.GaptoLeader;
                                    driversMustBeSorted = true;
                                }
                                else
                                {
                                    // jump down. use new data
                                    //cars[i] = (double)opponent.GaptoLeader;
                                    drivers[i].GapToLeader = (double)opponent.GaptoLeader;
                                }
                                //SimHub.Logging.Current.Info($"Added {oldDataOpponent.Name} to the list.");
                            }
                            else
                            {
                                // remove from list
                                // gap can be used to update car position property value
                                opponentsWithGapJump.RemoveAt(oppoGapIndex);
                                //cars[i] = (double)opponent.GaptoLeader;
                                drivers[i].GapToLeader = (double)opponent.GaptoLeader;
                                //SimHub.Logging.Current.Info($"Removed {opponent.Name} from the list.");
                            }
                        }
                        else
                        {
                            // gap can still have a bad value because it stays bad for multiple ticks.
                            if (oppoGapIndex == -1)
                            {
                                // all good
                                //cars[i] = (double)opponent.GaptoLeader;
                                drivers[i].GapToLeader = (double)opponent.GaptoLeader;
                                //SimHub.Logging.Current.Info($"All good for {opponent.Name}: {cars[i]}");
                            }
                            else
                            {
                                // bad gap. use old saved one
                                //cars[i] = (double)opponentsWithGapJump[oppoGapIndex].GaptoLeader;
                                drivers[i].GapToLeader = (double)opponentsWithGapJump[oppoGapIndex].GaptoLeader;
                                //SimHub.Logging.Current.Info($"Bad gap for {opponent.Name}: {cars[i]}");
                                driversMustBeSorted = true;
                            }


                        }
                        
                        if (drivers[i].GapToLeader > max) max = drivers[i].GapToLeader;
                        else if (drivers[i].GapToLeader < min) min = drivers[i].GapToLeader;

                    }
                    if (max == min) return;

                    //SimHub.Logging.Current.Info(("Raw cars: ", string.Join(", ", cars)));

                    /*for (int i = 0; i < cars.Length; ++i)
                    {
                        cars[i] = (1.0 - (cars[i] - min) / (max-min)) * 0.9 + 0.1;
                    }*/
                    // sort ascending
                    if (driversMustBeSorted) drivers = drivers.OrderBy(d => d.GapToLeader).ToList();

                    for (int i = 0; i < drivers.Count; ++i)
                    {
                        drivers[i].ProgressBarValue = (1.0 - (drivers[i].GapToLeader - min) / (max - min)) * 0.9 + 0.1;
                        if (i == 0) {
                            drivers[0].DeltaToDriverAhead = 0.0;
                        } else {
                            drivers[i].DeltaToDriverAhead = drivers[i].GapToLeader - drivers[i-1].GapToLeader;
                        }
                    }

                    // sort descending
                    
                    //SimHub.Logging.Current.Info(("Array before sort: ", string.Join(", ", cars)));
                    /*for (int i = 0; i < cars.Length-1; ++i)
                    {
                        for (int j = i; j < cars.Length-1; ++j)
                        {
                            if (cars[i] < cars[j+1])
                            {
                                double tmp = cars[i];
                                cars[i] = cars[j+1];
                                cars[j+1] = tmp;
                            }
                        }
                    }*/
                    //SimHub.Logging.Current.Info(("Array after sort: ", string.Join(", ", cars)));


                    for (int i = 0; i < drivers.Count; ++i)
                    {
                        //pluginManager.SetPropertyValue($"CarProgress{i}", this.GetType(), cars[i]);
                        pluginManager.SetPropertyValue($"CarProgress{i}", this.GetType(), drivers[i].ProgressBarValue);
                        pluginManager.SetPropertyValue($"GapToLeader{i}", this.GetType(), drivers[i].GapToLeader);
                        pluginManager.SetPropertyValue($"DeltaToDriverAhead{i}", this.GetType(), drivers[i].DeltaToDriverAhead);
                        pluginManager.SetPropertyValue($"DriverName{i}", this.GetType(), drivers[i].Name);
                    }

                    if (data.OldData.SpeedKmh < Settings.SpeedWarningLevel && data.OldData.SpeedKmh >= Settings.SpeedWarningLevel)
                    {
                        // Trigger an event
                        pluginManager.TriggerEvent("SpeedWarning", this.GetType());
                    }
                }
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here ! 
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControlDemo(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {

            //SimHub.Logging.Current.Info("Starting plugin");


            // Load settings
            Settings = this.ReadCommonSettings<ProgressBarLeaderboardSettings>("GeneralSettings", () => new ProgressBarLeaderboardSettings());


            // Declare a property available in the property list
            pluginManager.AddProperty("FrameTime", this.GetType(), DateTime.Now);

            for (int i = 0; i < Settings.NumberOfDrivers; ++i) {
                pluginManager.AddProperty("CarProgress" + i.ToString(), this.GetType(), 0.0);
                pluginManager.AddProperty("GapToLeader" + i.ToString(), this.GetType(), 0.0);
                pluginManager.AddProperty("DeltaToDriverAhead" + i.ToString(), this.GetType(), 0.0);
                pluginManager.AddProperty("DriverName" + i.ToString(), this.GetType(), "");
            }

            // Declare an event 
            pluginManager.AddEvent("SpeedWarning", this.GetType());

            // Declare an action which can be called
            pluginManager.AddAction("IncrementSpeedWarning", this.GetType(), (a, b) =>
            {
                Settings.SpeedWarningLevel++;
                SimHub.Logging.Current.Info("Speed warning changed");
            });

            // Declare an action which can be called
            pluginManager.AddAction("DecrementSpeedWarning", this.GetType(), (a, b) =>
            {
                Settings.SpeedWarningLevel--;
            });
        }
    }
}