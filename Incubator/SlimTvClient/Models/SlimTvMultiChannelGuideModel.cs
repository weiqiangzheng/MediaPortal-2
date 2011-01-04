﻿#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Plugins.SlimTvClient.Helpers;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.Plugins.SlimTvClient
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test state.
  /// </summary>
  public class SlimTvMultiChannelGuideModel : SlimTvGuideModelBase, IWorkflowModel
  {
    public const string MODEL_ID_STR = "5054408D-C2A9-451f-A702-E84AFCD29C10";
    
    public const Double VISIBLE_HOURS = 2.5;

    #region Constructor

    public SlimTvMultiChannelGuideModel()
    {
      _programActionsDialogName = "DialogProgramActionsFull"; // for MultiChannelGuide we need another dialog
    }

    #endregion

    #region Protected fields

    protected AbstractProperty _guideStartTimeProperty = null;

    protected DateTime GuideEndTime
    {
      get { return GuideStartTime.AddHours(VISIBLE_HOURS); }
    }

    #endregion

    #region Variables

    private ItemsList _channelList = new ItemsList();

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ChannelList
    {
      get { return _channelList; }
    }

    public DateTime GuideStartTime
    {
      get { return (DateTime)_guideStartTimeProperty.GetValue(); }
      set { _guideStartTimeProperty.SetValue(value); }
    }

    public AbstractProperty GuideStartTimeProperty
    {
      get { return _guideStartTimeProperty; }
    }

    public void ScrollForward()
    {
      GuideStartTime = GuideStartTime.AddMinutes(30);
      UpdatePrograms();
    }

    public void ScrollBackward()
    {
      GuideStartTime = GuideStartTime.AddMinutes(-30);
      UpdatePrograms();
    }

    #endregion

    #region Members

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        DateTime startDate = FormatHelper.RoundDateTime(DateTime.Now, 15, FormatHelper.RoundingDirection.Down);
        _guideStartTimeProperty = new WProperty(typeof (DateTime), startDate);

        base.InitModel();
      }
    }

    #endregion

    #region Channel, groups and programs

    protected override void UpdateChannels()
    {
      //ThreadPool.QueueUserWorkItem(BackgroundUpdateChannels);
      BackgroundUpdateChannels(null);
    }

    private void BackgroundUpdateChannels(object threadArgument)
    {
      base.UpdateChannels();
      _channelList.Clear();
      if (_channels != null)
        foreach (IChannel channel in _channels)
          _channelList.Add(new ChannelProgramListItem(channel, GetProgramsList(channel)));

      _channelList.FireChange();
    }

    protected ItemsList GetProgramsList(IChannel channel)
    {
      return GetProgramsList(channel, GuideStartTime, GuideEndTime);

    }
    protected ItemsList GetProgramsList(IChannel channel, DateTime referenceStart, DateTime referenceEnd)
    {
      ItemsList channelPrograms = new ItemsList();
      if (_tvHandler.ProgramInfo.GetPrograms(channel, referenceStart, referenceEnd, out _programs))
      {
        if (_programs.Count == 0)
          channelPrograms.Add(NoProgramPlaceholder());
        else
          foreach (IProgram program in _programs)
          {
            // Use local variable, otherwise delegate argument is not fixed
            ProgramProperties programProperties = new ProgramProperties(GuideStartTime, GuideEndTime);
            IProgram currentProgram = program;
            programProperties.SetProgram(currentProgram);

            ProgramListItem item = new ProgramListItem(programProperties)
                                     {
                                       Command = new MethodDelegateCommand(() => ShowProgramActions(currentProgram))
                                     };
            item.AdditionalProperties["PROGRAM"] = currentProgram;

            channelPrograms.Add(item);
          }
      }
      return channelPrograms;
    }

    private ProgramListItem NoProgramPlaceholder()
    {
      ILocalization loc = ServiceRegistration.Get<ILocalization>();
      DateTime today = FormatHelper.GetDay(DateTime.Now);

      ProgramProperties programProperties = new ProgramProperties(GuideStartTime, GuideEndTime)
                                              {
                                                Title = loc.ToString("[SlimTvClient.NoProgram]"),
                                                StartTime = today,
                                                EndTime = today.AddDays(1)
                                              };
      return new ProgramListItem(programProperties);
    }

    protected override void UpdateCurrentChannel()
    {
    }

    protected override void UpdatePrograms()
    {
      foreach (ChannelProgramListItem channel in _channelList)
        UpdateChannelPrograms(channel);
    }

    private void UpdateChannelPrograms(ChannelProgramListItem channel)
    {
      int programCount = channel.Programs.Count;
      if (programCount > 0)
      {
        ProgramListItem firstItem = (ProgramListItem) channel.Programs[0];
        ProgramListItem lastItem = (ProgramListItem) channel.Programs[programCount-1];
        DateTime timeFrom = firstItem.Program.StartTime;
        DateTime timeTo = lastItem.Program.EndTime;
        if (timeFrom > GuideStartTime)
          ReloadStart(channel, timeFrom);
        else
          TrimStart(channel.Programs);

        if (timeTo < GuideEndTime)
          ReloadEnd(channel, timeTo);
        else
          TrimEnd(channel.Programs);

        foreach (ProgramListItem program in channel.Programs)
          program.Program.UpdateDuration(GuideStartTime, GuideEndTime);

        RemoveZeroDurations(channel.Programs);
      }
      channel.Programs.FireChange();
    }

    private void RemoveZeroDurations(ItemsList programs)
    {
      for (int i = programs.Count - 1; i >= 0; i--)
      {
        ProgramListItem currentItem = (ProgramListItem)programs[i];
        if (currentItem.Program.RemainingDuration == 0)
          programs.Remove(currentItem);
      }
    }

    private void TrimStart(ItemsList programs)
    {
      for(int i=0; i<programs.Count; i++)
      {
        ProgramListItem firstItem = (ProgramListItem)programs[0];
        if (firstItem.Program.EndTime <= GuideStartTime)
          programs.RemoveAt(0);
        else
          break;
      }
    }

    private void TrimEnd(ItemsList programs)
    {
      for (int i = programs.Count-1; i >=0 ; i--)
      {
        ProgramListItem firstItem = (ProgramListItem)programs[i];
        if (firstItem.Program.StartTime > GuideEndTime)
          programs.RemoveAt(i);
        else
          break;
      }
    }

    private void ReloadStart(ChannelProgramListItem channel, DateTime firstFromTime)
    {
      ItemsList newItems = GetProgramsList(channel.Channel, GuideStartTime, firstFromTime.AddSeconds(-1));
      int idx = 0;
      foreach (ListItem listItem in newItems)
        channel.Programs.Insert(idx++, listItem);
    }

    private void ReloadEnd(ChannelProgramListItem channel, DateTime lastEndTime)
    {
      ItemsList newItems = GetProgramsList(channel.Channel, lastEndTime.AddSeconds(1), GuideEndTime);
      foreach (ListItem listItem in newItems)
        channel.Programs.Add(listItem);

    }

    #endregion

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    #endregion

  }
}