﻿using System.Collections.Generic;
using LMaML.Infrastructure.Events;
using LMaML.Infrastructure.Services.Interfaces;
using iLynx.Common;

namespace LMaML.Infrastructure.Services.Implementations
{
    /// <summary>
    /// MenuService
    /// </summary>
    public class MenuService : IMenuService
    {
        private readonly IPublicTransport publicTransport;
        private readonly Dictionary<string, IMenuItem> items;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuService" /> class.
        /// </summary>
        /// <param name="publicTransport">The public transport.</param>
        public MenuService(IPublicTransport publicTransport)
        {
            this.publicTransport = publicTransport;
            publicTransport.Guard("publicTransport");
            items = new Dictionary<string, IMenuItem>();
        }

        /// <summary>
        /// Gets the root menus.
        /// </summary>
        /// <value>
        /// The root menus.
        /// </value>
        public IEnumerable<IMenuItem> RootMenus { get { return items.Values; } }

        /// <summary>
        /// Registers the root.
        /// </summary>
        /// <param name="root">The root.</param>
        public void Register(IMenuItem root)
        {
            root.Guard("root");
            var name = root.Name ?? string.Empty;
            if (items.ContainsKey(name))
                items[name] = root;
            else
                items.Add(name, root);
            root.Changed += Changed;
            Changed();
        }

        /// <summary>
        /// Roots the on changed.
        /// </summary>
        private void Changed()
        {
            publicTransport.ApplicationEventBus.Publish(new MainMenuChangedEvent());
        }
    }
}
