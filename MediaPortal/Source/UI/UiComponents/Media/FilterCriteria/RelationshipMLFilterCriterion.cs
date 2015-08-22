﻿#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which creates a filter by a simple attribute value.
  /// </summary>
  public class RelationshipMLFilterCriterion : MLFilterCriterion
  {
    protected Guid _role;
    protected Guid _linkedRole;
    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;

    public RelationshipMLFilterCriterion(Guid role, Guid linkedRole, MediaItemAspectMetadata.AttributeSpecification attributeType)
    {
      _role = role;
      _linkedRole = linkedRole;
    }

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");
      MediaItemQuery query = new MediaItemQuery(necessaryMIATypeIds, filter);
      IList<MediaItem> items = cd.Search(query, true);
      IList<FilterValue> result = new List<FilterValue>(items.Count);
      int numEmptyEntries = 0;
      foreach (MediaItem item in items)
      {
        string name;
        MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out name);
        if (name == string.Empty)
          numEmptyEntries ++;
        else
          result.Add(new FilterValue(name, new RelationshipFilter(item.MediaItemId, _role, _linkedRole), null, item, this));
      }
      if (numEmptyEntries > 0)
        result.Insert(0, new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, new EmptyFilter(_attributeType), null, numEmptyEntries, this));
      return result;
    }

    protected virtual string GetDisplayName (object groupKey)
    {
      return string.Format("{0}", groupKey).Trim();
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");
      IList<MLQueryResultGroup> valueGroups = cd.GroupValueGroups(_attributeType, selectAttributeFilter, ProjectionFunction.None,
          necessaryMIATypeIds, filter, true, GroupingFunction.FirstCharacter);
      IList<FilterValue> result = new List<FilterValue>(valueGroups.Count);
      int numEmptyEntries = 0;
      foreach (MLQueryResultGroup group in valueGroups)
      {
        string name = string.Format("{0}", group.GroupKey);
        name = name.Trim();
        if (name == string.Empty)
          numEmptyEntries += group.NumItemsInGroup;
        else
          result.Add(new FilterValue(name, null, group.AdditionalFilter, group.NumItemsInGroup, this));
      }
      if (numEmptyEntries > 0)
        result.Insert(0, new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, new EmptyFilter(_attributeType), null, numEmptyEntries, this));
      return result;
    }

    #endregion
  }
}
