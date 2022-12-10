using ProcessMemory;
using System;
using System.Diagnostics;
using System.Linq;

namespace SRTPluginProviderSIGNALIS
{
    internal class GameMemoryScanner : IDisposable
    {
        // Variables
        private ProcessMemoryHandler memoryAccess;
        private GameMemory gameMemoryValues;
        private GameVersion gameVersion;
        public bool HasScanned;
        public bool ProcessRunning => memoryAccess != null && memoryAccess.ProcessRunning;
        public int ProcessExitCode => (memoryAccess != null) ? memoryAccess.ProcessExitCode : 0;

        // Pointer Address Variables
        private int pointerPlayerHP;
        private int pointerPlayerAmmo;
        private int pointerEnemyHP;

        // Pointer Classes
        private IntPtr BaseAddressSIGNALIS { get; set; }
        private IntPtr BaseAddressGameAssembly { get; set; }
        private IntPtr BaseAddressUnityPlayer { get; set; }
        private MultilevelPointer PointerPlayerHP { get; set; }
        private MultilevelPointer PointerPlayerAmmo { get; set; }
        private MultilevelPointer PointerEnemyHPCount { get; set; }
        private MultilevelPointer[] PointerEnemyHPs { get; set; }

        internal GameMemoryScanner(Process process = null)
        {
            gameMemoryValues = new GameMemory();
            if (process != null)
                Initialize(process);
        }

        internal unsafe void Initialize(Process process)
        {
            if (process == null)
                return; // Do not continue if this is null.

            int pid = GetProcessId(process).Value;
            memoryAccess = new ProcessMemoryHandler(pid);
            if (ProcessRunning)
            {
                ProcessModule? gameAssemblyModule = process.Modules.Cast<ProcessModule>().Where(a => string.Equals(a.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                ProcessModule? unityPlayerModule = process.Modules.Cast<ProcessModule>().Where(a => string.Equals(a.ModuleName, "UnityPlayer.dll", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (gameAssemblyModule is null || unityPlayerModule is null)
                    return; // Do not continue if we cannot get the base addresses we need.

                BaseAddressSIGNALIS = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn't.
                BaseAddressGameAssembly = gameAssemblyModule.BaseAddress;
                BaseAddressUnityPlayer = unityPlayerModule.BaseAddress;
                gameVersion = GameHashes.DetectVersion(gameAssemblyModule.FileName);
                SelectPointerAddresses(pid);
            }
        }

        private void SelectPointerAddresses(int pid)
        {
            switch (gameVersion)
            {
                default: // Latest or unknown version.
                case GameVersion.SIGNALIS_20221027_1:
                    {
                        pointerPlayerHP = 0x020B1450;
                        pointerPlayerAmmo = 0x020A0578;
                        pointerEnemyHP = 0x020B1450;
                        break;
                    }
            }

            // Setup the pointers.
            PointerPlayerHP = new MultilevelPointer(
                memoryAccess,
                IntPtr.Add(BaseAddressGameAssembly, pointerPlayerHP),
                0xB8
            );

            PointerPlayerAmmo = new MultilevelPointer(
                memoryAccess,
                IntPtr.Add(BaseAddressGameAssembly, pointerPlayerAmmo),
                0xB8,
                0x28
            );

            PointerEnemyHPCount = new MultilevelPointer(
                memoryAccess,
                IntPtr.Add(BaseAddressGameAssembly, pointerEnemyHP),
                0xB8,
                0xB8,
                0x68
            );
        }

        internal void UpdatePointers()
        {
            PointerPlayerHP.UpdatePointers();
            PointerPlayerAmmo.UpdatePointers();
            PointerEnemyHPCount.UpdatePointers();
            if (PointerEnemyHPs is not null)
                for (int i = 0; i < PointerEnemyHPs.Length; ++i)
                    PointerEnemyHPs[i].UpdatePointers();
        }

        private unsafe void GenerateEnemyEntries()
        {
            gameMemoryValues._enemyTableCount = PointerEnemyHPCount.DerefLong(0x18);

            if (PointerEnemyHPs is null || PointerEnemyHPs.Length != gameMemoryValues._enemyTableCount) // Enter if the pointer table is null (first run) or the size does not match.
            {
                gameMemoryValues._enemyHP = new long[gameMemoryValues._enemyTableCount];
                PointerEnemyHPs = new MultilevelPointer[gameMemoryValues._enemyTableCount]; // Create a new enemy pointer table array with the detected size.
                for (int i = 0; i < PointerEnemyHPs.Length; ++i) // Loop through and create all of the pointers for the table.
                    PointerEnemyHPs[i] = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddressGameAssembly, pointerEnemyHP), 0xB8, 0xB8, 0x68, 0x20 + (i * 0x08), 0x20, 0xE8);
            }
        }

        internal unsafe IGameMemory Refresh()
        {
            // Player HP
            gameMemoryValues._playerHP = PointerPlayerHP.DerefInt(0x08);

            // Player Ammo
            gameMemoryValues._playerAmmo = PointerPlayerAmmo.DerefInt(0x44);

            // Enemy HP
            GenerateEnemyEntries();
            for (int i = 0; i < PointerEnemyHPs.Length; ++i)
                gameMemoryValues._enemyHP[i] = PointerEnemyHPs[i].DerefLong(0x18);

            HasScanned = true;
            return gameMemoryValues;
        }

        private int? GetProcessId(Process process) => process?.Id;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (memoryAccess != null)
                        memoryAccess.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~REmake1Memory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
