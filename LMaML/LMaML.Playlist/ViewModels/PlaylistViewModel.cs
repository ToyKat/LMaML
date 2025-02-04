﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using iLynx.Configuration;
using LMaML.Infrastructure;
using LMaML.Infrastructure.Commands;
using LMaML.Infrastructure.Domain.Concrete;
using LMaML.Infrastructure.Events;
using LMaML.Infrastructure.Services.Interfaces;
using LMaML.Infrastructure.Util;
using iLynx.Common;
using iLynx.Common.WPF;

namespace LMaML.Playlist.ViewModels
{
    /// <summary>
    /// PlaylistViewModel
    /// </summary>
    public class PlaylistViewModel : NotificationBase
    {
        private readonly IDispatcher dispatcher;
        private readonly IInfoBuilder<StorableTaggedFile> fileInfoBuilder;
        private readonly IGlobalHotkeyService globalHotkeyService;
        private readonly IWindowManager windowManager;
        private readonly ISearchView searchView;
        private readonly IPublicTransport publicTransport;
        private List<FileItem> files = new List<FileItem>();
        private readonly IConfigurableValue<HotkeyDescriptor> searchHotkey;
        private ICommand doubleClickCommand;
        private ICommand keyUpCommand;
        private ICommand removeDuplicatesCommand;
        private string workerMessage;

        public string WorkerMessage
        {
            get { return workerMessage; }
            set
            {
                if (value == workerMessage) return;
                workerMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the delete selected command.
        /// </summary>
        /// <value>
        /// The delete selected command.
        /// </value>
        public ICommand DeleteSelectedCommand
        {
            get { return keyUpCommand ?? (keyUpCommand = new DelegateCommand<ICollection<object>>(OnDeleteSelected)); }
        }

        /// <summary>
        /// Called when [delete selected].
        /// </summary>
        /// <param name="collection">The collection.</param>
        private void OnDeleteSelected(ICollection<object> collection)
        {
            var copy = collection.OfType<FileItem>().ToArray();
            foreach (var file in copy)
                Files.Remove(file);
            publicTransport.CommandBus.Publish(new RemoveFilesCommand(copy.Select(x => x.File)));
            RaisePropertyChanged(() => Files);
        }

        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>
        /// The files.
        /// </value>
        public List<FileItem> Files
        {
            get { return files; }
            set
            {
                if (value == files) return;
                files = value;
                RaisePropertyChanged(() => Files);
            }
        }

        private ICommand dropCommand;

        /// <summary>
        /// Gets the drop command.
        /// </summary>
        /// <value>
        /// The drop command.
        /// </value>
        public ICommand DropCommand
        {
            get { return dropCommand ?? (dropCommand = new DelegateCommand<DragEventArgs>(OnDragDrop)); }
        }

        private void OnDragDrop(DragEventArgs dragEventArgs)
        {
            if (!dragEventArgs.Data.GetFormats().Contains("FileDrop")) return;
            var fileNames = dragEventArgs.Data.GetData("FileDrop") as string[];
            if (null == fileNames) return;
            AddFilesAsync(fileNames);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            searchHotkey.ValueChanged -= SearchHotkeyOnValueChanged;
        }

        /// <summary>
        /// Adds the files async.
        /// </summary>
        /// <param name="fileNames">The file names.</param>
        private async void AddFilesAsync(IEnumerable<string> fileNames)
        {
            foreach (var dir in fileNames)
            {
                if (Directory.Exists(dir))
                    AddFiles(await RecursiveAsyncFileScanner<StorableTaggedFile>.GetFilesRecursiveAsync(new DirectoryInfo(dir), fileInfoBuilder));
                else if (File.Exists(dir))
                {
                    bool valid;
                    var result = new[] { fileInfoBuilder.Build(new FileInfo(dir), out valid) };
                    if (!valid) continue;
                    AddFiles(result);
                }
            }
        }

        public ICommand RemoveDuplicatesCommand
        {
            get { return removeDuplicatesCommand ?? (removeDuplicatesCommand = new DelegateCommand(OnRemoveDuplicates)); }
        }

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (value == isLoading) return;
                isLoading = value;
                OnPropertyChanged();
            }
        }

        private void OnRemoveDuplicates()
        {
            WorkerMessage = "Working...";
            IsLoading = true;
            publicTransport.CommandBus.Publish(new RemovePlaylistDuplicatesCommand(), OnDuplicatesRemoved);
        }

        private void OnDuplicatesRemoved()
        {
            dispatcher.InvokeIfRequired(() =>
            {
                WorkerMessage = "Done";
                IsLoading = false;
            });
        }

        /// <summary>
        /// Adds the files.
        /// </summary>
        /// <param name="ffs">The FFS.</param>
        private void AddFiles(IEnumerable<StorableTaggedFile> ffs)
        {
            publicTransport.CommandBus.Publish(new AddFilesCommand(ffs));
        }

        private FileItem selectedFile;

        /// <summary>
        /// Gets or sets the selected file.
        /// </summary>
        /// <value>
        /// The selected file.
        /// </value>
        public FileItem SelectedFile
        {
            get { return selectedFile; }
            set
            {
                if (value == selectedFile) return;
                selectedFile = value;
                RaisePropertyChanged(() => SelectedFile);
            }
        }

        /// <summary>
        /// Gets the double click command.
        /// </summary>
        /// <value>
        /// The double click command.
        /// </value>
        public ICommand DoubleClickCommand
        {
            get { return doubleClickCommand ?? (doubleClickCommand = new Microsoft.Practices.Prism.Commands.DelegateCommand<FileItem>(OnDoubleClick)); }
        }

        private ICommand sortTitle;
        private ICommand sortArtist;

        /// <summary>
        /// Gets the sort title.
        /// </summary>
        /// <value>
        /// The sort title.
        /// </value>
        public ICommand SortTitle
        {
            get { return sortTitle ?? (sortTitle = new Microsoft.Practices.Prism.Commands.DelegateCommand(OnSortTitle)); }
        }

        /// <summary>
        /// Gets or sets the sort artist.
        /// </summary>
        /// <value>
        /// The sort artist.
        /// </value>
        public ICommand SortArtist
        {
            get { return sortArtist ?? (sortArtist = new Microsoft.Practices.Prism.Commands.DelegateCommand(OnSortArtist)); }
        }

        /// <summary>
        /// Called when [sort artist].
        /// </summary>
        private void OnSortArtist()
        {
            publicTransport.CommandBus.Publish(new OrderByCommand<StorableTaggedFile, string>(x => x.Artist.Name));
            //await playlistService.OrderByAsync(x => x.Artist.Name);
        }

        /// <summary>
        /// Called when [sort title].
        /// </summary>
        private void OnSortTitle()
        {
            publicTransport.CommandBus.Publish(new OrderByCommand<StorableTaggedFile, string>(x => x.Title.Name));
        }

        /// <summary>
        /// Called when [double click].
        /// </summary>
        /// <param name="taggedFile">The tagged file.</param>
        private void OnDoubleClick(FileItem taggedFile)
        {
            if (null == taggedFile) return;
            publicTransport.CommandBus.Publish(new PlayFileCommand(taggedFile.File));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistViewModel" /> class.
        /// </summary>
        /// <param name="publicTransport">The public transport.</param>
        /// <param name="playlistService">The playlist service.</param>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="playerService">The audio player service.</param>
        /// <param name="fileInfoBuilder">The fileInfoBuilder.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="globalHotkeyService">The global hotkey service.</param>
        /// <param name="windowManager">The window manager.</param>
        /// <param name="searchView">The search view.</param>
        public PlaylistViewModel(IPublicTransport publicTransport,
            IPlaylistService playlistService,
            IDispatcher dispatcher,
            IPlayerService playerService,
            IInfoBuilder<StorableTaggedFile> fileInfoBuilder,
            IConfigurationManager configurationManager,
            IGlobalHotkeyService globalHotkeyService,
            IWindowManager windowManager,
            ISearchView searchView)
        {
            this.publicTransport = Guard.IsNull(() => publicTransport);
            Guard.IsNull(() => configurationManager);
            this.dispatcher = Guard.IsNull(() => dispatcher);
            this.fileInfoBuilder = Guard.IsNull(() => fileInfoBuilder);
            this.globalHotkeyService = Guard.IsNull(() => globalHotkeyService);
            this.windowManager = Guard.IsNull(() => windowManager);
            this.searchView = Guard.IsNull(() => searchView);
            publicTransport.ApplicationEventBus.Subscribe<PlaylistUpdatedEvent>(OnPlaylistUpdated);
            publicTransport.ApplicationEventBus.Subscribe<TrackChangedEvent>(OnTrackChanged);
            searchHotkey = configurationManager.GetValue("Search", new HotkeyDescriptor(ModifierKeys.Control | ModifierKeys.Alt, Key.J),
                                                         KnownConfigSections.GlobalHotkeys);
            searchHotkey.ValueChanged += SearchHotkeyOnValueChanged;
            globalHotkeyService.RegisterHotkey(searchHotkey.Value, OnSearch);
            searchView.PlayFile += SearchViewOnPlayFile;
            Files = new List<FileItem>(playlistService.Files.Select(x => new FileItem(x)));
            var currenTrack = playerService.CurrentTrackAsReadonly;
            if (null == currenTrack) return;
            SetPlayingFile(playlistService.Files.Find(x => x.Filename == currenTrack.Name));
        }

        /// <summary>
        /// Searches the view on play file.
        /// </summary>
        /// <param name="storableTaggedFile">The storable tagged file.</param>
        private void SearchViewOnPlayFile(StorableTaggedFile storableTaggedFile)
        {
            publicTransport.CommandBus.Publish(new PlayFileCommand(storableTaggedFile));
        }

        /// <summary>
        /// Called when [search].
        /// </summary>
        private void OnSearch()
        {
            windowManager.OpenNew(searchView, "Search", 400, 800);
        }

        /// <summary>
        /// Gets the file count.
        /// </summary>
        /// <value>
        /// The file count.
        /// </value>
        public int FileCount
        {
            get { return Files.Count; }
        }

        /// <summary>
        /// Searches the hotkey on value changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="changedEventArgs"></param>
        private void SearchHotkeyOnValueChanged(object sender,
                                                ValueChangedEventArgs<HotkeyDescriptor> changedEventArgs)
        {
            globalHotkeyService.ReRegisterHotkey(changedEventArgs.OldValue, changedEventArgs.NewValue, OnSearch);
        }

        private FileItem playingFile;
        private bool isLoading;

        /// <summary>
        /// Called when [track changed].
        /// </summary>
        /// <param name="trackChangedEvent">The track changed event.</param>
        private void OnTrackChanged(TrackChangedEvent trackChangedEvent)
        {
            dispatcher.BeginInvoke(new Action<TrackChangedEvent>(x => SetPlayingFile(x.File)), trackChangedEvent);
        }

        private void SetPlayingFile(StorableTaggedFile file)
        {
            if (null != playingFile)
                playingFile.IsPlaying = false;
            SelectedFile = Files.Find(x => x.File == file);
            playingFile = SelectedFile;
            if (null == playingFile) return;
            playingFile.IsPlaying = true;
        }

        /// <summary>
        /// Called when [playlist updated].
        /// </summary>
        /// <param name="e">The e.</param>
        private void OnPlaylistUpdated(PlaylistUpdatedEvent e)
        {
            dispatcher.InvokeIfRequired(() =>
            {
                WorkerMessage = "Adding Files";
                IsLoading = true;
            });
            var fixedFiles = new List<FileItem>(
                        publicTransport.CommandBus.GetResult(new GetPlaylistCommand()).Select(
                            x =>
                                new FileItem(x)
                                {
                                    IsPlaying = null != playingFile && playingFile.File.Filename == x.Filename
                                }));
            dispatcher.BeginInvoke(new Action(() =>
            {
                Files = fixedFiles;
                playingFile = fixedFiles.Find(x => x.IsPlaying); // Note that this could possibly be merged with the above select statement, but has been moved here for clarity.
                RaisePropertyChanged(() => FileCount);
                WorkerMessage = "Done...";
                IsLoading = false;
            }));
        }
    }
}
