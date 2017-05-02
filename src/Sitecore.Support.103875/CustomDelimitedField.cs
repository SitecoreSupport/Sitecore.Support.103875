using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Security.Accounts;
using Sitecore.Sites;
using Sitecore.Data;
using Sitecore.Links;
using Sitecore.Diagnostics;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using Sitecore.SecurityModel;
using Sitecore.Data.Serialization;
using Sitecore.Globalization;
using Sitecore.StringExtensions;
using Sitecore.Data.DataProviders.Sql;
using Sitecore.Reflection;
using System.Reflection;
using Sitecore.Diagnostics.PerformanceCounters;
using System.Data;
using Sitecore.Jobs;
using System.Text.RegularExpressions;
using Sitecore.Text;

namespace Sitecore.Support.Data.Fields
{
    public class DelimitedField : Sitecore.Data.Fields.DelimitedField, IBrokenLinksRemove
    {
        public DelimitedField(Field innerField, char separator) : base (innerField, separator)
        {

        }

        public void RemoveBrokenLinks(Database db)
        {
            foreach (String str in List)
            {
                ID currItemID = ID.Parse(str);
                if (!ID.IsNullOrEmpty(currItemID))
                {
                    Item item = db.GetItem(currItemID);
                    if (item == null)
                    {
                        string text = List.Remove(str);
                        base.Value = text;
                    }
                }
            }
        }
    }

}