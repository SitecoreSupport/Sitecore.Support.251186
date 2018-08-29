// Sitecore.XA.Foundation.Search.Services.FacetService
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Buckets.Extensions;
using Sitecore.Buckets.Interfaces;
using Sitecore.Buckets.Util;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Globalization;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.Search;
using Sitecore.XA.Foundation.Search.Extensions;
using Sitecore.XA.Foundation.Search.Models;
using Sitecore.XA.Foundation.Search.Services;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using Sitecore.XA.Foundation.SitecoreExtensions.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.XA.Foundation.Search.Services
{
  public class FacetService : IFacetService
  {
    protected ISearchContextService SearchContextService
    {
      get;
      set;
    }

    protected IContentRepository ContentRepository
    {
      get;
      set;
    }

    protected IContext Context
    {
      get;
    }

    public FacetService(ISearchContextService searchContextService, IContentRepository contentRepository)
    {
      SearchContextService = searchContextService;
      ContentRepository = contentRepository;
      Context = ServiceLocator.ServiceProvider.GetService<IContext>();
    }

    public IList<Facet> GetFacets(IQueryable<ContentPage> query, IEnumerable<Item> facets, string language, out Timer queryTimer)
    {
      List<Facet> list = new List<Facet>();
      query = BuildFacetQuery(query, facets);
      FacetResults facets2 = default(FacetResults);
      using (queryTimer = new Timer())
      {
        facets2 = query.GetFacets();
      }
      foreach (FacetCategory category in facets2.Categories)
      {
        FacetNames facetNames = GetFacetNames(facets, category.Name, language);
        Facet group = new Facet(facetNames.Key, facetNames.FriendlyName);
        (from v in category.Values
         orderby v.Name
         select v).ForEach(delegate (Sitecore.ContentSearch.Linq.FacetValue facetValue)
         {
           group.Values.Add(new Sitecore.XA.Foundation.Search.Models.FacetValue(facetValue.Name, facetValue.AggregateCount));
         });
        list.Add(group);
      }
      return list;
    }

    private Item getTenant(Item item)
    {
      Item parent = item.Parent;
      if (parent.TemplateID == new ID("{644F6518-5B75-4F43-B804-91DAB55EB702}"))
      {
        return parent;
      }
      else if (Sitecore.ItemIDs.RootID == parent.ID)
      {
        return null;
      }
      else
      {
        return getTenant(parent);
      }
    }

    public IList<Item> GetFacetItems(IEnumerable<string> facets, string siteName)
    {
      List<Item> list = new List<Item>();
      HashSet<string> facetSearchSet = new HashSet<string>(from f in facets
                                                           select f.ToLower());
      Item homeItem = SearchContextService.GetHomeItem(siteName);
      if (homeItem == null)
      {
        return list;
      }
      MultilistField sharedSites = getTenant(homeItem)?.Fields["SharedSites"];
      List<Item> sites = new List<Item>();
      sites.Add(homeItem);
      if (sharedSites != null)
      {
        foreach (ID i in sharedSites.TargetIDs)
        {
          sites.Add(Sitecore.Context.Database.GetItem(i));
        }
      }
      foreach (Item site in sites)
      {
        Item item = ServiceLocator.ServiceProvider.GetService<IMultisiteContext>().GetSettingsItem(site).FirstChildInheritingFrom(Sitecore.XA.Foundation.Search.Templates.FacetsGrouping.ID);
        if (item != null)
        {
          Item[] array = (from i in item.EnsureFallbackVersionIfExist().Axes.GetDescendants()
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
          Item[] array = (from i in Context.Database.GetItem(Sitecore.Buckets.Util.Constants.FacetFolder).EnsureFallbackVersionIfExist().Axes.GetDescendants()
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
      }
      return list;
    }

    protected virtual FacetNames GetFacetNames(IEnumerable<Item> facets, string categoryName, string language)
    {
      IEnumerable<Language> source = language.ParseLanguages().ToList();
      Item item = facets.FirstOrDefault((Item i) => ((BaseItem)i)["Parameters"] == categoryName);
      if (item != null && source.Any() && item.Languages.Contains(source.First()))
      {
        Item item2 = ContentRepository.GetItem(item.ID, source.First());
        if (item2 != null)
        {
          item = item2;
        }
      }
      string friendlyName = categoryName;
      string key = categoryName;
      if (item != null)
      {
        friendlyName = ((BaseItem)item)["Name"];
        key = item.Name;
      }
      return new FacetNames
      {
        Key = key,
        FriendlyName = friendlyName
      };
    }

    private IQueryable<ContentPage> BuildFacetQuery(IQueryable<ContentPage> query, IEnumerable<Item> facets)
    {
      IQueryable<ContentPage> queryable = query;
      foreach (Item facet in facets)
      {
        int minimumResultCount = 1;
        Field field = facet.Fields[Sitecore.Buckets.Util.Constants.EnabledFacet];
        bool flag;
        ISimpleFacet simpleFacet;
        if (field != null && field.Value == "1")
        {
          Field field2 = facet.Fields[Sitecore.Buckets.Util.Constants.MiniumFacetCount];
          if (field2 != null)
          {
            int.TryParse(field2.Value, out minimumResultCount);
          }
          flag = false;
          simpleFacet = null;
          Field field3 = facet.Fields[Sitecore.Buckets.Util.Constants.FacetFilter];
          if (field3 != null && field3.Value.IsNotEmpty())
          {
            simpleFacet = typeof(SiteFacetSearcher).GetMethod("GetFacetFilter", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { field3.Value }) as ISimpleFacet;//SiteFacetSearcher.GetFacetFilter(field3.Value);
            if (simpleFacet != null)
            {
              flag = true;
              goto IL_00bc;
            }
            continue;
          }
          goto IL_00bc;
        }
        continue;
        IL_00bc:
        Field parameters = facet.Fields[Sitecore.Buckets.Util.Constants.FacetParameters];
        if (parameters != null)
        {
          string[] array = (from fn in parameters.Value.Split(',')
                            select fn.Trim()).ToArray();
          if (flag)
          {
            queryable = queryable.FacetOn((ContentPage d) => ((SearchResultItem)d)[parameters.Value], minimumResultCount, simpleFacet.Filters());
          }
          else if (array.Length == 1)
          {
            string fieldName = array.First();
            queryable = queryable.FacetOn((ContentPage d) => ((SearchResultItem)d)[fieldName], minimumResultCount);
          }
          else
          {
            queryable = queryable.FacetPivotOn((FacetPivotQuery<ContentPage> p) => ProcessFacetPivotQuery(parameters, p), minimumResultCount);
          }
        }
      }
      return queryable;
    }

    private FacetPivotQuery<ContentPage> ProcessFacetPivotQuery(Field parameters, FacetPivotQuery<ContentPage> p)
    {
      string[] array = parameters.Value.Split(',');
      foreach (string fieldName in array)
      {
        p.FacetOn((ContentPage d) => ((SearchResultItem)d)[fieldName]);
      }
      return p;
    }
  }
}
