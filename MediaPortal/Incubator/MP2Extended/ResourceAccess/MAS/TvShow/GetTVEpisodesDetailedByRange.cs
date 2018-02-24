﻿using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "start", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVEpisodesDetailedByRange : GetTVEpisodesDetailed
  {
    public IList<WebTVEpisodeDetailed> Process(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      // output
      IEnumerable<WebTVEpisodeDetailed> output = Process(null, sort, order);

      // get range
      output = output.TakeRange(start, end);

      return output.ToList();
    }
  }
}