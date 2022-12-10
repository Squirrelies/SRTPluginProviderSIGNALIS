namespace SRTPluginProviderSIGNALIS
{
    public class GameMemory : IGameMemory
    {
        public int PlayerHP { get => _playerHP; set => _playerHP = value; }
        internal int _playerHP;

        public int PlayerAmmo { get => _playerAmmo; set => _playerAmmo = value; }
        internal int _playerAmmo;

        public long EnemyTableCount { get => _enemyTableCount; set => _enemyTableCount = value; }
        internal long _enemyTableCount;

        public long[] EnemyHP { get => _enemyHP; set => _enemyHP = value; }
        internal long[] _enemyHP;
    }
}
