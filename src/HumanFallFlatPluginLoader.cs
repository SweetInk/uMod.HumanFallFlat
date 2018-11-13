using System;
using uMod.Plugins;

namespace uMod.HumanFallFlat
{
    /// <summary>
    /// Responsible for loading the core Human: Fall Flat plugin
    /// </summary>
    public class HumanFallFlatPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(HumanFallFlat) };
    }
}
