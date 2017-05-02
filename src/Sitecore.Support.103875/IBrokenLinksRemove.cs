using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;

namespace Sitecore.Support
{
    public interface IBrokenLinksRemove
    {
        void RemoveBrokenLinks(Database db);
        
    }
}