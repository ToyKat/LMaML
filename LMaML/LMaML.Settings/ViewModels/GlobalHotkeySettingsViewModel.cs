﻿using System.Collections.Generic;
using System.Linq;
using iLynx.Configuration;
using LMaML.Infrastructure;
using iLynx.Common;
using iLynx.Common.WPF;

namespace LMaML.Settings.ViewModels
{
    /// <summary>
    /// GlobalHotkeySettingsViewModel
    /// </summary>
    public class GlobalHotkeySettingsViewModel : ISectionView
    {
        private readonly IEnumerable<IConfigurableValue> values;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalHotkeySettingsViewModel" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="values">The values.</param>
        public GlobalHotkeySettingsViewModel(string name, IEnumerable<IConfigurableValue> values)
        {
            name.Guard("name");
            values.Guard("values");
            Title = name;
            this.values = values;
        }

        public string Title { get; private set; }

        /// <summary>
        /// Gets the hot keys.
        /// </summary>
        /// <value>
        /// The hot keys.
        /// </value>
        public IEnumerable<HotkeyViewModel> HotKeys { get { return values.OfType<IConfigurableValue<HotkeyDescriptor>>().Select(x => new HotkeyViewModel(x)); } }
    }
}
