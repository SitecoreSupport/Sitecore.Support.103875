using System;
using System.Collections;
using Sitecore.Links;
using Sitecore.Diagnostics;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using System.Xml.Linq;
using Sitecore.Text;
using Sitecore.Layouts;

namespace Sitecore.Support.Data.Fields
{
    public class LayoutField : Sitecore.Data.Fields.LayoutField
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

        public override void RemoveLink([NotNull] ItemLink itemLink)
        {
          Assert.ArgumentNotNull(itemLink, "itemLink");

          string value = this.Value;
          if (string.IsNullOrEmpty(value))
          {
            return;
          }

          LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);
          ArrayList devices = layoutDefinition.Devices;
          if (devices == null)
          {
            return;
          }

          string targetItemID = itemLink.TargetItemID.ToString();

          for (int n = devices.Count - 1; n >= 0; n--)
          {
            var device = devices[n] as DeviceDefinition;
            if (device == null)
            {
              continue;
            }

            if (device.ID == targetItemID)
            {
              devices.Remove(device);
              continue;
            }

            if (device.Layout == targetItemID)
            {
              device.Layout = null;
              continue;
            }

            if (device.Placeholders != null)
            {
              string targetPath = itemLink.TargetPath;
              bool isLinkFound = false;
              for (int j = device.Placeholders.Count - 1; j >= 0; j--)
              {
                var placeholderDefinition = device.Placeholders[j] as PlaceholderDefinition;
                if (placeholderDefinition == null)
                {
                  continue;
                }

                if (
                  string.Equals(
                    placeholderDefinition.MetaDataItemId, targetPath, StringComparison.InvariantCultureIgnoreCase) ||
                  string.Equals(
                    placeholderDefinition.MetaDataItemId, targetItemID, StringComparison.InvariantCultureIgnoreCase))
                {
                  device.Placeholders.Remove(placeholderDefinition);
                  isLinkFound = true;
                }
              }

              if (isLinkFound)
              {
                continue;
              }
            }

            if (device.Renderings == null)
            {
              continue;
            }

            for (int r = device.Renderings.Count - 1; r >= 0; r--)
            {
              var rendering = device.Renderings[r] as RenderingDefinition;
              if (rendering == null)
              {
                continue;
              }

              if (rendering.Datasource == itemLink.TargetPath)
              {
                rendering.Datasource = string.Empty;
              }

              if (rendering.ItemID == targetItemID)
              {
                device.Renderings.Remove(rendering);
              }

              if (rendering.Datasource == targetItemID)
              {
                rendering.Datasource = string.Empty;
              }

              if (rendering.MultiVariateTest == targetItemID)
              {
                rendering.MultiVariateTest = null;
              }

              if (!string.IsNullOrEmpty(rendering.Parameters))
              {
                Item layoutItem = this.InnerField.Database.GetItem(rendering.ItemID);

                if (layoutItem == null)
                {
                  continue;
                }

                var renderingParametersFieldCollection = this.GetParametersFields(layoutItem, rendering.Parameters);

                foreach (var field in renderingParametersFieldCollection.Values)
                {
                  if (!string.IsNullOrEmpty(field.Value))
                  {
                    field.RemoveLink(itemLink);
                  }
                }

                rendering.Parameters = renderingParametersFieldCollection.GetParameters().ToString();
              }

              if (rendering.Rules != null)
              {
                var rulesField = new RulesField(this.InnerField, rendering.Rules.ToString());
                rulesField.RemoveLink(itemLink);
                rendering.Rules = XElement.Parse(rulesField.Value);
              }
            }
          }

          this.Value = layoutDefinition.ToXml();
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