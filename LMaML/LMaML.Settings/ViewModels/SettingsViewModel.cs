﻿using System;
using System.Collections.Generic;
using System.Linq;
using iLynx.Configuration;
using LMaML.Infrastructure;
using LMaML.Infrastructure.Events;
using LMaML.Infrastructure.Services.Interfaces;
using iLynx.Common;
using iLynx.Common.WPF;

namespace LMaML.Settings.ViewModels
{
    /// <summary>
    /// SettingsViewModel
    /// </summary>
    public class SettingsViewModel : NotificationBase, IRequestClose
    {
        private readonly ISectionViewFactory viewFactory;
        private readonly IConfigurationManager configurationManager;
        private readonly IDispatcher dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel" /> class.
        /// </summary>
        /// <param name="viewFactory">The view factory.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="publicTransport">The public transport.</param>
        /// <param name="dispatcher">The dispatcher.</param>
        public SettingsViewModel(ISectionViewFactory viewFactory, IConfigurationManager configurationManager, IPublicTransport publicTransport, IDispatcher dispatcher)
        {
            viewFactory.Guard("viewFactory");
            configurationManager.Guard("configurationManager");
            publicTransport.Guard("publicTransport");
            dispatcher.Guard("dispatcher");
            this.viewFactory = viewFactory;
            this.configurationManager = configurationManager;
            this.dispatcher = dispatcher;
            publicTransport.ApplicationEventBus.Subscribe<ConfigSectionsChangedEvent>(OnConfigSectionsChanged);
        }

        /// <summary>
        /// Called when [config sections changed].
        /// </summary>
        /// <param name="configSectionsChangedEvent">The config sections changed event.</param>
        private void OnConfigSectionsChanged(ConfigSectionsChangedEvent configSectionsChangedEvent)
        {
            dispatcher.Invoke(() => RaisePropertyChanged(() => Sections));
        }

        /// <summary>
        /// Gets the sections.
        /// </summary>
        /// <value>
        /// The sections.
        /// </value>
        public IEnumerable<ISectionView> Sections
        {
            get
            {
                return configurationManager.GetCategories().Where(x => KnownConfigSections.Hidden != x).Select(cat => viewFactory.Build(cat, configurationManager.GetLoadedValues(cat)));
            }
        }

        /// <summary>
        /// Occurs when [request close].
        /// </summary>
        public event Action<IRequestClose> RequestClose;
    }
}
