namespace SRTPluginProviderSIGNALIS
{
    public interface IGameMemory
    {
        int PlayerHP { get; }
        int PlayerAmmo { get; }
        long EnemyTableCount { get; }
        long[] EnemyHP { get; }
    }
}
