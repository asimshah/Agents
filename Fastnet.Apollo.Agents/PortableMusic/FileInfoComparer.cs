using System.Collections.Generic;
using System.IO;

namespace Fastnet.Apollo.Agents
{
    internal class FileInfoComparer : IEqualityComparer<FileInfo>
    {
        #region Public Methods

        public bool Equals(FileInfo x, FileInfo y)
        {
            return x.FullName.Equals(y.FullName);
        }
        public int GetHashCode(FileInfo obj)
        {
            return obj.FullName.GetHashCode();
        }

        #endregion Public Methods
    }
}
