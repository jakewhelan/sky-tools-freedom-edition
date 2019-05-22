﻿// <copyright file="IValueViewItem.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace SkyTools.UI
{
    /// <summary>
    /// An interface for the view items that display configuration values.
    /// </summary>
    public interface IValueViewItem
    {
        /// <summary>Refreshes this view item by re-fetching its value from the bound configuration property.</summary>
        void Refresh();
    }
}
