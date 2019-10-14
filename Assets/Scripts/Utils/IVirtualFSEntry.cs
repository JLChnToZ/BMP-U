using System;
using System.IO;

namespace BananaBeats.Utils {
    public interface IVirtualFSEntry : IDisposable {
        IVirtualFSEntry this[string path] { get; }

        Stream Stream { get; }

        string FullPath { get; }

        string Name { get; }

        bool IsReal { get; }

        IVirtualFSEntry Find(string query);
    }
}
