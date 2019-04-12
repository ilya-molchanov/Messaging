using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Common.ServiceBus;
using NLog;
using ZXing;


namespace ImgScanner
{
    using System.Drawing;
    using Common.Models.Properties;

    internal class ImagesAggregator
    {
        private Document _document;
        private Section _section;
        private readonly ILogger _logger;

        private readonly string _patternPrefix;
        private int _prevPageNumber;
        private readonly List<string> _usedFiles;
        private readonly Timer _saveTimer;
        private readonly object _lockObj;

        private readonly ServerSbClient _serviceBusClient;
        private readonly ScanProperties _props;

        public ImagesAggregator(ScanProperties props, string filePattern, ServerSbClient serviceBusClient)
        {
            _props = props;
            _patternPrefix = filePattern.Split('*')[0];

            _logger = Logger.Logger.Current;
            _usedFiles = new List<string>();
            _saveTimer = new Timer(props.ScanTimeout);
            _saveTimer.Elapsed += (sender, args) => SendDocument();
            _lockObj = new object();
            InitDocAndSection();

            _serviceBusClient = serviceBusClient;
        }

        public void ProcessFile(FileStream stream)
        {
            if (_usedFiles.Contains(stream.Name))
            {
                return;
            }
            _saveTimer.Stop();
            _saveTimer.Start();

            if (IsEndingBarcode(stream, out var invalidFile))
            {
                stream.Close();
                File.Delete(stream.Name);
                SendDocument();
            }
            else if (invalidFile)
            {
                _logger.Error($"Sequence has invalid file {stream.Name} and will be moved to {_props.ErrorDir}.");
                _usedFiles.Add(stream.Name);
                MoveCorruptedSequence();
            }
            else
            {
                if (IsNewDocumentPage(stream.Name))
                {
                    stream.Close();
                    SendDocument();
                }
                AddFileToDoc(stream.Name);
            }
        }

        private void SendDocument()
        {
            lock (_lockObj)
            {
                if (_usedFiles.Count == 0)
                {
                    return;
                }

                var render = new PdfDocumentRenderer
                {
                    Document = _document
                };
                render.RenderDocument();

                using (var ms = new MemoryStream())
                {
                    render.Save(ms, false);
                    _serviceBusClient.SendFile (ms, _document.Info.Title);
                }
                DeleteUsedFiles();
                InitDocAndSection();
            }
        }

        private void DeleteUsedFiles()
        {
            foreach (var file in _usedFiles)
            {
                File.Delete(file);
            }
            _usedFiles.Clear();
        }

        private void InitDocAndSection()
        {
            var outFileName = $"scan_result_{DateTime.Now.ToFileTime()}.pdf";

            _document = new Document
            {
                Info = new DocumentInfo { Title = outFileName }
            };
            _section = _document.AddSection();
        }

        private bool IsNewDocumentPage(string file)
        {
            var pageNumberStr = Path.GetFileNameWithoutExtension(file)
                ?.Replace(_patternPrefix, "");
            if (!int.TryParse(pageNumberStr, out var currPageNumber))
            {
                currPageNumber = _prevPageNumber;
            }
            var difference = currPageNumber - _prevPageNumber;
            _prevPageNumber = currPageNumber;
            return difference > 1;
        }

        private bool IsEndingBarcode(FileStream stream, out bool invalidFile)
        {
            var reader = new BarcodeReader { AutoRotate = true };
            try
            {
                using (var bmp = (Bitmap)Image.FromStream(stream))
                {
                    var result = reader.Decode(bmp);
                    invalidFile = false;
                    return result != null && result.Text == _props.BarcodeText;
                }
            }
            catch (ArgumentException e)
            {
                _logger.Error($"File {stream.Name} does not have a valid image format.");
                _logger.Error(e);
                invalidFile = true;
                return false;
            }
        }

        private void AddFileToDoc(string file)
        {
            _section.AddPageBreak();
            var img = _section.AddImage(file);
            img.Top = 0;
            img.Left = 0;
            img.Height = _document.DefaultPageSetup.PageHeight;
            img.Width = _document.DefaultPageSetup.PageWidth;
            img.RelativeHorizontal = RelativeHorizontal.Page;
            img.RelativeVertical = RelativeVertical.Page;
            _usedFiles.Add(file);
        }

        public void MoveCorruptedSequence()
        {
            foreach (var file in _usedFiles)
            {
                var outFile = Path.Combine(_props.ErrorDir, file);
                File.Move(file, outFile);
            }
            _usedFiles.Clear();
            _logger.Error($"Sequence was moved to {_props.ErrorDir}.");
        }
    }
}