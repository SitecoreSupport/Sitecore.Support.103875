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
using Sitecore.Xml;

namespace Sitecore.Support.Data.Fields
{
    public class LayoutField : Sitecore.Data.Fields.LayoutField, IBrokenLinksRemove
    {
        public LayoutField(Item item) : this(item.Fields[FieldIDs.FinalLayoutField])
        {
        }
        public LayoutField(Field innerField) : base(innerField)
        {
        }
        public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
        {
        }

        /// <summary>
		/// Converts a <see cref="T:Sitecore.Data.Fields.Field" /> to a <see cref="T:Sitecore.Data.Fields.LayoutField" />.
		/// </summary>
		/// <param name="field">The field.</param>
		/// <returns>The implicit operator.</returns>
		public static implicit operator LayoutField(Field field)
        {
            if (field != null)
            {
                return new LayoutField(field);
            }
            return null;
        }
        public void RemoveBrokenLinks(Database db)
        {
            Assert.ArgumentNotNull(db, "db");
            string value = base.Value;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);
            System.Collections.ArrayList devices = layoutDefinition.Devices;
            if (devices == null)
            {
                return;
            }
            for (int i = devices.Count - 1; i >= 0; i--)
            {
                DeviceDefinition deviceDefinition = devices[i] as DeviceDefinition;
                if (deviceDefinition != null)
                {
                    if (ID.IsID(deviceDefinition.ID) && db.GetItem(ID.Parse(deviceDefinition.ID)) == null)
                    {
                        devices.Remove(deviceDefinition);
                    }
                    else if (ID.IsID(deviceDefinition.Layout) && db.GetItem(ID.Parse(deviceDefinition.Layout)) == null)
                    {
                        deviceDefinition.Layout = null;
                    }
                    else
                    {
                        if (deviceDefinition.Placeholders != null)
                        {
                            bool flag = false;
                            for (int j = deviceDefinition.Placeholders.Count - 1; j >= 0; j--)
                            {
                                PlaceholderDefinition placeholderDefinition = deviceDefinition.Placeholders[j] as PlaceholderDefinition;
                                if (placeholderDefinition != null && db.GetItem(placeholderDefinition.MetaDataItemId) == null)
                                {
                                    deviceDefinition.Placeholders.Remove(placeholderDefinition);
                                    flag = true;
                                }
                            }
                            if (flag)
                            {
                                goto IL_254;
                            }
                        }
                        if (deviceDefinition.Renderings != null)
                        {
                            for (int k = deviceDefinition.Renderings.Count - 1; k >= 0; k--)
                            {
                                RenderingDefinition renderingDefinition = deviceDefinition.Renderings[k] as RenderingDefinition;
                                if (renderingDefinition != null)
                                {
                                    if (db.GetItem(renderingDefinition.Datasource) == null)
                                    {
                                        renderingDefinition.Datasource = string.Empty;
                                    }
                                    if (ID.IsID(renderingDefinition.ItemID) && db.GetItem(ID.Parse(renderingDefinition.ItemID)) == null)
                                    {
                                        deviceDefinition.Renderings.Remove(renderingDefinition);
                                    }
                                    
                                    if (ID.IsID(renderingDefinition.Datasource) && db.GetItem(ID.Parse(renderingDefinition.Datasource)) == null)
                                    {
                                        renderingDefinition.Datasource = string.Empty;
                                    }
                                    if (!string.IsNullOrEmpty(renderingDefinition.Parameters))
                                    {
                                        Item item = base.InnerField.Database.GetItem(renderingDefinition.ItemID);
                                        if (item != null)
                                        {
                                            RenderingParametersFieldCollection parametersFields = this.GetParametersFields(item, renderingDefinition.Parameters);
                                            foreach (CustomField current in parametersFields.Values)
                                            {
                                                if (!string.IsNullOrEmpty(current.Value))
                                                {
                                                    if (current is IBrokenLinksRemove)
                                                    {
                                                        IBrokenLinksRemove iBroken = current as IBrokenLinksRemove;
                                                        iBroken.RemoveBrokenLinks(db);
                                                    }
                                                    else
                                                    {
                                                        if ((ID.IsID(current.Value) && db.GetItem(ID.Parse(current.Value)) == null) || db.GetItem(current.Value) == null)
                                                        {
                                                            current.Value = String.Empty;
                                                        }
                                                    }
                                                }
                                            }
                                            renderingDefinition.Parameters = parametersFields.GetParameters().ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            IL_254:;
            }
            base.Value = layoutDefinition.ToXml();
        }

        private RenderingParametersFieldCollection GetParametersFields(Item layoutItem, string renderingParameters)
        {
            UrlString parameters = new UrlString(renderingParameters);
            RenderingParametersFieldCollection result;
            RenderingParametersFieldCollection.TryParse(layoutItem, parameters, out result);
            return result;
        }
    }
}