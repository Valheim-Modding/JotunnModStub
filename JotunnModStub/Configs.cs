using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JotunnModStub
{
    public static class Configs
    {
        private static ConfigFile defaultConfiguration;
        public static ConfigEntry<int> ExampleConfig;

        static Configs()
        {
            //Acceptable value ranges can be defined to allow configuration via a slider in the BepInEx ConfigurationManager: https://github.com/BepInEx/BepInEx.ConfigurationManager
            ExampleConfig = defaultConfiguration.Bind<int>("Main Section", "Example configuration integer", 1, new ConfigDescription("This is an example config, using a range limitation for ConfigurationManager", new AcceptableValueRange<int>(0, 100)));
        }
    }
}
