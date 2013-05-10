﻿using LMaML.PlayerControls.ViewModels;
using Microsoft.Practices.Unity;

namespace LMaML.PlayerControls.Views
{
    /// <summary>
    /// Interaction logic for PlayerControlsView.xaml
    /// </summary>
    public partial class PlayerControlsView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerControlsView" /> class.
        /// </summary>
        public PlayerControlsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the data context for an element when it participates in data binding.
        /// </summary>
        /// <returns>The object to use as data context.</returns>
        [Dependency]
        public new PlayerControlsViewModel DataContext
        {
            get { return (PlayerControlsViewModel) base.DataContext; }
            set { base.DataContext = value; }
        }
    }
}
