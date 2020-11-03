using System.Collections.Generic;
using System.IO;

namespace Fastnet.Apollo.Agents
{
    internal class DirectoryInfoComparer : IEqualityComparer<DirectoryInfo>
    {
        #region Public Methods

        public bool Equals(DirectoryInfo x, DirectoryInfo y)
        {
            return x.FullName.Equals(y.FullName);
        }
        public int GetHashCode(DirectoryInfo obj)
        {
            return obj.FullName.GetHashCode();
        }

        #endregion Public Methods
    }
}
