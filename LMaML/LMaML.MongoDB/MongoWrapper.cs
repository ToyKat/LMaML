﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using iLynx.Common.DataAccess;
using iLynx.Configuration;
using LMaML.Infrastructure.Domain;
using MongoDB.Driver;
using iLynx.Common;

//using T0yK4T.Tools.Data.Mongo;
//using System.ServiceProcess;

namespace LMaML.MongoDB
{
    /// <summary>
    ///     A "Wrapper" for mongodb - This actually just takes care of starting the service (unless mongoservice is set to false, in which case mongod will be started "standalone")
    /// </summary>
    public class MongoWrapper : IMongoWrapper, IDisposable
    {
        private readonly IConfigurableValue<string> mongoFile;

        private readonly IConfigurableValue<string> dbPath;

        private readonly IConfigurableValue<string> logFile;

        private readonly IConfigurableValue<int> mongoPort;

        private readonly IConfigurableValue<string> mongoHost;

        private readonly IConfigurableValue<string> mongoArgs;

        private Process mongoProcess;
        private bool processStarted;

        public MongoWrapper(IConfigurationManager configurationManager)
        {
            configurationManager.Guard("configurationManager");
            DatabaseName = typeof(MongoWrapper).Assembly.GetName().Name.Replace(".", "");
            mongoFile = configurationManager.GetValue("mongofile", "mongod.exe");
            dbPath = configurationManager.GetValue("dbpath", string.Format("{0}\\data\\db", Environment.CurrentDirectory));
            logFile = configurationManager.GetValue("logfile", "mongodb.log");
            mongoPort = configurationManager.GetValue("mongoport", 27017);
            mongoHost = configurationManager.GetValue("mongohost", "localhost");
            mongoArgs = configurationManager.GetValue("mongoargs", string.Format("--port {0} --dbpath \"{1}\" --logpath \"{2}\\{3}\"", mongoPort, dbPath, dbPath.Value, logFile.Value));
            if (MongoAvailable) return;
            StartMongo();
        }

        public void DumpOutput()
        {
            while (!mongoProcess.StandardOutput.EndOfStream)
                Trace.WriteLine(mongoProcess.StandardOutput.ReadLine() ?? string.Empty);
        }

        private MongoServer server;

        public MongoDatabase Database
        {
            get
            {
                if (null != server) return server.GetDatabase(DatabaseName);
                if (!MongoAvailable)
                    throw new InvalidOperationException("Mongo server is not available");
                for (var i = 0; i < 5; ++i)
                {
                    if (TryConnect(out server))
                        break;
                    Thread.CurrentThread.Join(500);
                }
                if (null == server) throw new InvalidOperationException("Unable to connect to mongo server");
                return server.GetDatabase(DatabaseName);
            }
        }

        private bool TryConnect(out MongoServer srv)
        {
            try
            {
                srv = new MongoServer(new MongoServerSettings
                {
                    ConnectTimeout = TimeSpan.FromSeconds(5),
                    Server = new MongoServerAddress(mongoHost.Value, mongoPort.Value)
                });
                srv.Connect();
            }
            catch
            {
                srv = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MongoWrapper" /> class.
        /// </summary>
        ~MongoWrapper()
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
            if (!processStarted || mongoProcess == null || mongoProcess.HasExited) return;
            mongoProcess.Kill();
            if (!mongoProcess.WaitForExit(5000))
                this.LogError("Unable to shutdown mongo server");
            mongoProcess = null;
        }

        /// <summary>
        ///     Gets a value indicating whether or not MongoDB is considered as being available
        /// </summary>
        public bool MongoAvailable
        {
            get
            {
                if (null != mongoProcess) return (mongoProcess != null && !mongoProcess.HasExited);
                var file = mongoFile.Value;
                var processes = Process.GetProcessesByName(file.Remove(file.LastIndexOf('.')));
                var p = processes.FirstOrDefault();
                if (null != p)
                    mongoProcess = p;
                return (mongoProcess != null && !mongoProcess.HasExited);
            }
        }

        private ProcessStartInfo GetInfo(string mongoExe)
        {
            var info = new ProcessStartInfo
                           {
                               FileName = mongoExe,
                               Arguments = mongoArgs.Value,
#if DEBUG
                               UseShellExecute = true,
#else
                               UseShellExecute = false,
                               RedirectStandardOutput = true,
                               CreateNoWindow = true,
#endif
                           };
            return info;
        }

        private const string LockFile = "mongod.lock";

        /// <summary>
        /// Attempts to Start MongoDB on the local machine
        /// </summary>
        /// <exception cref="System.IO.FileNotFoundException">Unable to start mongod - file not found</exception>
        public void StartMongo()
        {
            if (MongoAvailable)
                return;
            var path = dbPath.Value;
            var exefile = mongoFile.Value;
            var lockPath = Path.Combine(dbPath.Value, LockFile);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            else if (File.Exists(lockPath))
                File.Delete(lockPath);
            string mongoExe;
            if (!File.Exists((mongoExe = Path.Combine(Environment.CurrentDirectory, exefile))))
                throw new FileNotFoundException("Unable to start mongod - file not found");
            mongoProcess = new Process { StartInfo = GetInfo(mongoExe) };
            mongoProcess.Exited += MongoProcessOnExited;
            mongoProcess.Start();
            while (!mongoProcess.Responding)
                Thread.CurrentThread.Join(1);

            // TODO: Could this be refined somehow?
            Thread.CurrentThread.Join(TimeSpan.FromMilliseconds(1500));
            processStarted = true;
        }

        private void MongoProcessOnExited(object sender, EventArgs eventArgs)
        {
            this.LogWarning("Mongo Process has exited. {0}", eventArgs);
        }

        /// <summary>
        ///     Attempts to get an <see cref="IDataAdapter{T}" /> for the specified type <typeparamref name="T" />
        /// </summary>
        /// <typeparam name="T">The type for which to get an adapter</typeparam>
        /// <returns>
        ///     Returns a <see cref="MongoDBAdapter{T}" />
        /// </returns>
        public MongoDBAdapter<T> GetAdapter<T>() where T : ILibraryEntity
        {
            return new MongoDBAdapter<T>(this);
        }

        protected string DatabaseName { get; private set; }
    }
}