using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Common;
using Common.Models;
using Common.ServiceBus;

namespace ImgScanner
{
    using System.Timers;
    using Common.Models.Properties;
    public class Scanner
    {
        private readonly Thread _workThread;
        private readonly ManualResetEvent _stopEvent;
        private readonly AutoResetEvent _newFileEvent;
        private readonly FileSystemWatcher _watcher;
        private readonly Timer _stateTimer;

        private readonly ImagesAggregator _imagesAggregator;
        private readonly ServerSbClient _sbClient;
        private readonly ScanProperties _props;

        private States _state = States.Starting;
        private const string FilePattern = "image_*.*";
        private readonly string[] _allowedExtensions = { ".bmp", ".jpg", ".jpeg", ".png" };
        private readonly string _serviceName;

        public Scanner(ScanProperties props)
        {
            _props = props;
            _serviceName = $"Scanner_{Guid.NewGuid()}";

            if (!Directory.Exists(props.InDir))
                Directory.CreateDirectory(props.InDir);
            
            if (!Directory.Exists(props.ErrorDir))
                Directory.CreateDirectory(props.ErrorDir);

            _watcher = new FileSystemWatcher(props.InDir);
            _watcher.Changed += Watcher_Changed;

            _workThread = new Thread(WorkingMethod);
            _stopEvent = new ManualResetEvent(false);
            _newFileEvent = new AutoResetEvent(false);

            _sbClient = new ServerSbClient(string.Empty, props.CnString, props.InfoQueueName);
            _sbClient.InitTopics();

            _imagesAggregator = new ImagesAggregator(props, FilePattern, _sbClient);
        }

        private void WorkingMethod()
        {
            _state = States.Processing;
            var directory = new DirectoryInfo(_props.InDir);
            do
            {
                var files = directory.GetFiles(FilePattern, SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f.Name)
                    .Where(file => _allowedExtensions.Any(file.Name.ToLower().EndsWith));

                _state = States.Waiting;
                foreach (var file in files)
                {
                    if (_stopEvent.WaitOne(TimeSpan.Zero))
                        return;

                    _state = States.Processing;
                    if (TryOpen(file.FullName, 3, out var stream))
                    {
                        using (stream)
                        {
                            _imagesAggregator.ProcessFile(stream);
                        }
                    }
                    else
                    {
                        var msg = $"Access to file {file.FullName} is denied. File will be ignored.";
                        Logger.Logger.Current.Error(msg);
                        _imagesAggregator.MoveCorruptedSequence();
                    }
                }
                _state = States.Waiting;
            }
            while (WaitHandle.WaitAny(new WaitHandle[] { _stopEvent, _newFileEvent }) != 0);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            _newFileEvent.Set();
        }

        public void Start()
        {
            _state = States.Starting;
            _workThread.Start();
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _state = States.Stopping;
            _watcher.EnableRaisingEvents = false;
            _stopEvent.Set();
            _workThread.Join();
            _stateTimer.Stop();
        }

        private static bool TryOpen(string fileName, int tryCount, out FileStream stream)
        {
            for (var i = 0; i < tryCount; i++)
            {
                try
                {
                    stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    return true;
                }
                catch (IOException)
                {
                    Thread.Sleep(3000);
                }
            }
            stream = null;
            return false;
        }
    }
}
