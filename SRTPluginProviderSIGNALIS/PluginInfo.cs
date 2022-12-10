using SRTPluginBase;
using System;

namespace SRTPluginProviderSIGNALIS
{
    internal class PluginInfo : IPluginInfo
    {
        public string Name => "Game Memory Provider (SIGNALIS)";

        public string Description => "A game memory provider plugin for SIGNALIS.";

        public string Author => "Squirrelies";

        public Uri MoreInfoURL => new Uri("https://github.com/Squirrelies/SRTPluginProviderSIGNALIS");

        public int VersionMajor => assemblyVersion.Major;

        public int VersionMinor => assemblyVersion.Minor;

        public int VersionBuild => assemblyVersion.Build;

        public int VersionRevision => assemblyVersion.Revision;

        private Version assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
    }
}
