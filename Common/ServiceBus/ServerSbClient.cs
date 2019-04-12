using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Common.Models;
using Common.Models.Properties;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NLog;

namespace Common.ServiceBus
{
    public class ServerSbClient
    {
        public const string BroadcastPropertiesTopic = "BROADCAST_PROPERTIES_TOPIC";
        public const string StateRequestTopic = "STATE_REQUEST_TOPIC";

        public const string BroadcastPropertiesSub = BroadcastPropertiesTopic + "_SUBSCRIPTION";
        public const string StateRequestSub = StateRequestTopic + "_SUBSCRIPTION";

        private readonly string _cnString;
        private readonly string _infoQueueName;
        private readonly NamespaceManager _namespaceManager;
        private const int MsgSizeLimit = 260000;

        private readonly string _outDir;
        private readonly ILogger _logger;

        public ServerSbClient(string outDir, string cnString, string infoQueueName)
        {
            _cnString = cnString;
            _infoQueueName = infoQueueName;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_cnString);
            _logger = Logger.Logger.Current;
            _cnString = cnString;
            _outDir = outDir;
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_cnString);
        }

        public void CreateInfoListener (string queueName)
        {
            if (!_namespaceManager.QueueExists(queueName))
                _namespaceManager.CreateQueue(queueName);
            var client = QueueClient.CreateFromConnectionString(_cnString, queueName, ReceiveMode.ReceiveAndDelete);
            client.OnMessage(message =>
            {
                ProcessMsg(message.GetBody<DownloadFileMsg>());
            });
        }

        private void ProcessMsg (DownloadFileMsg fileMsg)
        {
            var client = QueueClient.CreateFromConnectionString(_cnString, fileMsg.QueueName, ReceiveMode.ReceiveAndDelete);

            using (var stream = new MemoryStream())
            {
                var count = 0;
                var content = new List<byte>();
                var messages = client.ReceiveBatch(fileMsg.AmountOfParts);
                foreach (var msg in messages)
                {
                    if (msg != null)
                    {
                        var body = msg.GetBody<byte[]>();
                        content.AddRange(body);
                        count++;
                    }
                }
                stream.Write(content.ToArray(), 0, content.Count);

                 _namespaceManager.DeleteQueue (fileMsg.QueueName);
                if (count != fileMsg.AmountOfParts)
                {
                    _logger.Error("Not all parts of file were received.");
                    return;
                }

                stream.Seek(0, SeekOrigin.Begin);
                var filePath = Path.Combine(_outDir, fileMsg.FileName);

                using (var fileStream = File.Create(filePath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
            }
        }
        
        public void InitTopics()
        {
            if (!_namespaceManager.TopicExists(BroadcastPropertiesTopic))
                _namespaceManager.CreateTopic(BroadcastPropertiesTopic);

            if (!_namespaceManager.SubscriptionExists(BroadcastPropertiesTopic, BroadcastPropertiesSub))
                _namespaceManager.CreateSubscription(BroadcastPropertiesTopic, BroadcastPropertiesSub);

            if (!_namespaceManager.TopicExists(StateRequestTopic))
                _namespaceManager.CreateTopic(StateRequestTopic);

            if (!_namespaceManager.SubscriptionExists(StateRequestTopic, StateRequestSub))
                _namespaceManager.CreateSubscription(StateRequestTopic, StateRequestSub);
        }


        public void SendPropertiesUpdate(Properties properties)
        {
            var client = TopicClient.CreateFromConnectionString(_cnString, BroadcastPropertiesTopic);
            var message = new BrokeredMessage(properties);
            client.Send(message);
        }

        public void SendStateRequest()
        {
            var client = TopicClient.CreateFromConnectionString(_cnString, StateRequestTopic);
            var message = new BrokeredMessage("StateRequest");
            client.Send(message);
        }

        public void OnPropsUpdated(Action<Properties> updateProps)
        {
            var client = SubscriptionClient.CreateFromConnectionString(_cnString, BroadcastPropertiesTopic, BroadcastPropertiesSub, ReceiveMode.PeekLock);
            client.OnMessage(message =>
            {
                try
                {
                    updateProps(message.GetBody<Properties>());
                    message.Complete();
                }
                catch (Exception)
                {
                    message.Abandon();
                    throw;
                }
            });
        }

        public void OnStateRequestReceived(Action sendState)
        {
            var client = SubscriptionClient.CreateFromConnectionString(_cnString, StateRequestTopic, StateRequestSub, ReceiveMode.ReceiveAndDelete);
            client.OnMessage(message =>
            {
                sendState();
            });
        }

        public void SendFile(Stream stream, string fileName)
        {
            var fileQueue = $"file_{Guid.NewGuid()}";
            if (!_namespaceManager.QueueExists(_infoQueueName))
                _namespaceManager.CreateQueue(_infoQueueName);
            _namespaceManager.CreateQueue(fileQueue);
            var client = QueueClient.CreateFromConnectionString(_cnString, fileQueue);

            var size = MsgSizeLimit;
            var buffer = new byte[size > 0 ? size : stream.Length];
            var total = stream.Length;
            size = size > total ? (int)total : size;
            var count = 0;

            var messages = new List<BrokeredMessage>();
            while (stream.Read(buffer, 0, size) > 0)
            {
                messages.Add(new BrokeredMessage(buffer));
                count++;
                total -= size;
                size = size > total ? (int)total : size;
                buffer = new byte[size];
            }
            client.SendBatch(messages);

            var infoQueueClient = QueueClient.CreateFromConnectionString(_cnString, _infoQueueName);
            var infoMessage = new BrokeredMessage(new DownloadFileMsg
            {
                FileName = fileName,
                QueueName = fileQueue,
                AmountOfParts = count
            });
            infoQueueClient.Send(infoMessage);
        }

        //public void SendState(StateProperties props, string queueName)
        //{
        //    try
        //    {
        //        if (!_namespaceManager.QueueExists(queueName))
        //            _namespaceManager.CreateQueue(queueName);
        //        var client = QueueClient.CreateFromConnectionString(_cnString, queueName);
        //        var message = new BrokeredMessage(props);
        //        client.Send(message);
        //    }
        //    catch (Exception exception)
        //    {
        //        Logger.Logger.Current.Error(exception);
        //        _namespaceManager.DeleteQueue(queueName);
        //    }
        //}
    }
}
