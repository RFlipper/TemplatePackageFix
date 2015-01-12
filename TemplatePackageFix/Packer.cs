using Microsoft.Deployment.Compression;
using Microsoft.Deployment.Compression.Cab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plumsail.TemplatePackageFix
{
    /// <summary>
    /// Wrap pack/unpack functionality
    /// </summary>
    public class Packer : IDisposable
    {
        public readonly string OriginalPath;
        public readonly string TempFolder = Path.GetTempPath() + Guid.NewGuid();

        public Packer(string path)
        {
            OriginalPath = path;

            Directory.CreateDirectory(TempFolder);
            if (File.Exists(OriginalPath))
            {
                var cab = new CabInfo(OriginalPath);
                cab.Unpack(TempFolder);
            }
        }
        public void Dispose()
        {
            var cab = new CabInfo(OriginalPath);
            cab.Pack(TempFolder, true, CompressionLevel.Normal, (sender, args) => { });

            Directory.Delete(TempFolder, true);
        }
    }
}
