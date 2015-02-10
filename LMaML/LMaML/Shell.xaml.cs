﻿using System;
using System.IO;
using LMaML.Infrastructure.Events;
using LMaML.Infrastructure.Services.Interfaces;
using iLynx.Common;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace LMaML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Shell
    {
        private const string LayoutFile = ".\\Layout.xml";
        public Shell()
        {
            InitializeComponent();
            LoadLayout();
        }

        //[Conditional("PERSIST_LAYOUT")]
        private void SaveLayout()
        {
            using (var output = File.Open(LayoutFile, FileMode.Create, FileAccess.ReadWrite))
            {
                var layoutSerializer = new XmlLayoutSerializer(DockingManager);
                layoutSerializer.Serialize(output);
            }
        }

        //[Conditional("PERSIST_LAYOUT")]
        private void LoadLayout()
        {
            if (!File.Exists(LayoutFile)) return;
            using (var input = File.Open(LayoutFile, FileMode.Open, FileAccess.Read))
            {
                var layoutSerializer = new XmlLayoutSerializer(DockingManager);
                layoutSerializer.Deserialize(input);
            }
        }

        public IPublicTransport PublicTransport { get; set; }
        public ILogger Logger { get; set; }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            try
            {
                SaveLayout();
                PublicTransport.ApplicationEventBus.Send(new ShutdownEvent());
            }
            catch (Exception ex)
            {
                if (null == Logger) return;
                Logger.Log(LogLevel.Error, this, ex.ToString());
            }
        }
    }
}
