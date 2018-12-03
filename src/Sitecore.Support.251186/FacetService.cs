using Microsoft.Extensions.DependencyInjection;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Search.Services;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.XA.Foundation.Search.Services
{
  public class FacetService : Sitecore.XA.Foundation.Search.Services.FacetService, IFacetService
  {
    public FacetService(ISearchContextService searchContextService) : base(searchContextService)
    {
    }

    public new List<Item> GetFacetItems(IEnumerable<string> facets, string siteName)
    {
      List<Item> list = new List<Item>();
      HashSet<string> facetSearchSet = new HashSet<string>(from f in facets
                                                           select f.ToLower());
      Item homeItem = SearchContextService.GetHomeItem(siteName);
      if (homeItem == null)
      {
        return list;
      }
      Item item = ServiceLocator.ServiceProvider.GetService<IMultisiteContext>().GetSettingsItem(homeItem).FirstChildInheritingFrom(Sitecore.XA.Foundation.Search.Templates.FacetsGrouping.ID);
      if (item != null)
      {
        Item[] array = (from i in item.EnsureFallbackVersion().Axes.GetDescendants()
                        where facetSearchSet.Contains(i.Name.ToLower())
                        select i).ToArray();
        foreach (Item item2 in array)
        {
          if (facetSearchSet.Contains(item2.Name.ToLower()))
          {
            facetSearchSet.Remove(item2.Name.ToLower());
            list.Add(item2);
          }
        }
      }
      if (facetSearchSet.Any())
      {
        Item[] array = (from i in Context.Database.GetItem(Sitecore.Buckets.Util.Constants.FacetFolder).EnsureFallbackVersion().Axes.GetDescendants()
                        where facetSearchSet.Contains(i.Name.ToLower())
                        select i).ToArray();
        foreach (Item item3 in array)
        {
          if (facetSearchSet.Contains(item3.Name.ToLower()))
          {
            facetSearchSet.Remove(item3.Name.ToLower());
            list.Add(item3);
          }
        }
      }
      return list;
    }
  }
}
