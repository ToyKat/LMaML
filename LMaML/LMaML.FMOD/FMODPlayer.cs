﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FMOD;
using iLynx.Configuration;
using LMaML.Infrastructure.Audio;
using iLynx.Common;
using LMaML.Infrastructure.Domain.Concrete;

namespace LMaML.FMOD
{
    /// <summary>
    /// AudioPlayer
    /// </summary>
    public class FMODPlayer : IAudioPlayer, IDisposable
    {
        private readonly IConfigurationManager configurationManager;
        private global::FMOD.System fmodSystem;
        private readonly List<uint> pluginHandles = new List<uint>();


        /// <summary>
        /// Initializes a new instance of the <see cref="FMODPlayer" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public FMODPlayer(IConfigurationManager configurationManager)
        {
            configurationManager.Guard("configurationManager");
            this.configurationManager = configurationManager;
            fmodSystem = new global::FMOD.System();
            var result = Factory.System_Create(ref fmodSystem);
            if (result != RESULT.OK)
                throw GetException("Unable to create FMOD System", result);
            result = fmodSystem.init(10, INITFLAGS.NORMAL, IntPtr.Zero);
            if (result != RESULT.OK)
                throw GetException("Unable to Initialize FMOD System", result);

            GetPlugins();
        }

        private void GetPlugins()
        {
            var pluginDir = configurationManager.GetValue("FMOD Plugin Directory", "Plugins\\Codecs");
            if (null == pluginDir) return;
            if (string.IsNullOrEmpty(pluginDir.Value)) return;
            //var path = Path.Combine(Environment.CurrentDirectory, pluginDir.Value);
            //LoadPlugins(path);
        }

        public void LoadPlugins(string dir)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(dir);

                
                var result = fmodSystem.setPluginPath(dir);
                if (RESULT.OK != result)
                {
                    this.LogWarning("Unable to set plugin path to: {0}, result: {1}", dir, result);
                    return;
                }
                foreach (var file in directoryInfo.EnumerateFiles("*.dll", SearchOption.AllDirectories))
                {
                    uint handle = 0;
                    try
                    {
                        result = fmodSystem.loadPlugin(file.FullName, ref handle, (uint)PLUGINTYPE.CODEC);
                        if (RESULT.OK != result)
                        {
                            this.LogWarning("Unable to load plugin: {0}, error was: {1}", file, result);
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                    pluginHandles.Add(handle);
                }
                //fmodSystem.loadPlugin()
            }
            catch (Exception e)
            {
                this.LogException(e, MethodBase.GetCurrentMethod());
            }
        }

        /// <summary>
        /// Creates the channel.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        public ITrack CreateChannel(StorableTaggedFile file)
        {
            var fileName = file.Filename;
            var sound = CreateSoundFromFile(fileName);
            return new FMODTrack(sound, fmodSystem, fileName);
        }

        private Sound CreateSoundFromFile(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                throw new FileNotFoundException("Can't find file", file ?? string.Empty);
            Sound sound = null;
            var result = fmodSystem.createStream(file, MODE.SOFTWARE | MODE.IGNORETAGS, ref sound);
            if (result != RESULT.OK)
                throw GetException("Unable to create sound", result);
            return sound;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FMODPlayer" /> class.
        /// </summary>
        ~FMODPlayer()
        {
            if (!disposed)
                Dispose();
        }

        private bool disposed;
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            disposed = true;
            if (fmodSystem == null) return;
            var result = fmodSystem.close();
            if (result != RESULT.OK)
                throw GetException("Unable to close FMOD System", result);
            fmodSystem = null;
        }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static Exception GetException(string text, RESULT result)
        {
            return new Exception(string.Format("{0}{1}Error Code: {2}", text, Environment.NewLine, result));
        }
    }
}