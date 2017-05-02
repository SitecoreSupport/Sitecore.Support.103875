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
using HtmlAgilityPack;
using Sitecore.Web;
using Sitecore.Layouts;
using Sitecore.Rules;
using System.Xml.Linq;

namespace Sitecore.Support.Data.Fields
{
    public class MultiListField : DelimitedField
    {
        /// <summary>
		/// Gets the list of target IDs.
		/// </summary>
		/// <value>The target I ds.</value>
		public ID[] TargetIDs
        {
            get
            {
                System.Collections.ArrayList arrayList = new System.Collections.ArrayList();
                string value = base.Value;
                string[] array = value.Split(new char[]
                {
                    '|'
                });
                string[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    string text = array2[i];
                    if (text.Length > 0 && ID.IsID(text))
                    {
                        arrayList.Add(ID.Parse(text));
                    }
                }
                return arrayList.ToArray(typeof(ID)) as ID[];
            }
        }

        /// <summary>
        /// Creates a new <see cref="T:Sitecore.Data.Fields.MultilistField" /> instance.
        /// </summary>
        /// <param name="innerField">Inner field.</param>
        public MultiListField(Field innerField) : base(innerField, '|')
		{
        }

        /// <summary>
        /// Converts a <see cref="T:Sitecore.Data.Fields.Field" /> to a <see cref="T:Sitecore.Data.Fields.MultilistField" />.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>The implicit operator.</returns>
        public static implicit operator MultiListField(Field field)
        {
            if (field != null)
            {
                return new MultiListField(field);
            }
            return null;
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <returns>The items.</returns>
        public Item[] GetItems()
        {
            System.Collections.ArrayList arrayList = new System.Collections.ArrayList();
            Database database = this.GetDatabase();
            if (database == null)
            {
                return null;
            }
            ID[] targetIDs = this.TargetIDs;
            for (int i = 0; i < targetIDs.Length; i++)
            {
                ID itemId = targetIDs[i];
                Item item = database.GetItem(itemId);
                if (item != null)
                {
                    arrayList.Add(item);
                }
            }
            return arrayList.ToArray(typeof(Item)) as Item[];
        }
    }
}