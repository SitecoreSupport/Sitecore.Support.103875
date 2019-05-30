using System.Linq;
using Sitecore.Data;
using Sitecore.Diagnostics;
using System.Collections.Generic;
using Sitecore.Links;

namespace Sitecore.Support.Links
{
  /// <summary>
  ///   Wraps the <see cref="LinkDatabase" /> instance and extends its functionality.
  /// </summary>
  public class BrokenLinksFilter
  {

    #region Private methods

    protected bool IsSourceItemContentOrMedia(Database database, ItemLink itemLink)
    {
      Assert.ArgumentNotNull(database, "database");
      Assert.ArgumentNotNull(itemLink, "itemLink");

      var item = database.GetItem(itemLink.SourceItemID);

      var isSourceItemContentOrMedia = item != null && (item.Paths.IsContentItem || item.Paths.IsMediaItem);

      return isSourceItemContentOrMedia;
    }

    #endregion

    #region Public methods

    /// <summary>
    ///   Method filters out 'Broken Links' related to the system items
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="brokenLinks">List of broken links.</param>
    /// <returns><see cref="ItemLink" />[]</returns>
    public virtual ItemLink[] ExcludeSystemItemLinks([NotNull] Database database, [NotNull] IReadOnlyCollection<ItemLink> brokenLinks)
    {
      Assert.ArgumentNotNull(brokenLinks, "brokenLinks");

      var filteredLinks = brokenLinks.Where(link => this.IsSourceItemContentOrMedia(database, link)).ToArray();

      return filteredLinks;
    }
    #endregion
  }
}