using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.ProcessingNodes.Logging;
using Gravity.Server.ProcessingNodes.Routing;
using Gravity.Server.ProcessingNodes.Server;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.ProcessingNodes.Transform;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Utility;
using Urchin.Client.Interfaces;

namespace Gravity.Server.Pipeline
{
    internal class NodeGraph: INodeGraph
    {
        private readonly IExpressionParser _expressionParser;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IFactory _factory;
        private readonly IBufferPool _bufferPool;
        private readonly ILogFactory _logFactory;
        private readonly IDisposable _configuration;
        private readonly IConnectionThreadPool _connectionThreadPool;
        private readonly Queue<DisposeQueueItem> _disposeQueue;

        private INodeGraph _currentInstance;
        private INodeGraph _newInstance;
        private Thread _thread;

        public NodeGraph(
            IConfigurationStore configuration,
            IExpressionParser expressionParser,
            IHostingEnvironment hostingEnvironment,
            IFactory factory,
            IBufferPool bufferPool,
            ILogFactory logFactory,
            IConnectionThreadPool connectionThreadPool)
        {
            _expressionParser = expressionParser;
            _hostingEnvironment = hostingEnvironment;
            _factory = factory;
            _bufferPool = bufferPool;
            _logFactory = logFactory;
            _connectionThreadPool = connectionThreadPool;
            _disposeQueue = new Queue<DisposeQueueItem>();

            _configuration = configuration.Register<NodeGraphConfiguration>("/gravity/nodeGraph", Configure);

            _thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(100);

                        bool UpdateGraph(INodeGraph graph, bool logActions)
                        {
                            if (graph == null) return false;

                            var offline = false;
                            var nodes = graph.GetNodes(n => n);
                            foreach (var node in nodes)
                            {
                                try
                                {
                                    node.UpdateStatus();
                                    if (logActions)
                                    {
                                        if (node.Offline && !node.Disabled)
                                        {
                                            Trace.WriteLine($"[CONFIG] Node {node.Name} is OFFLINE");
                                            offline = true;
                                        }
                                        else
                                        {
                                            Trace.WriteLine($"[CONFIG] Node {node.Name} is online");
                                        }
                                    }
                                    else
                                    {
                                        if (node.Offline && !node.Disabled) 
                                            offline = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine($"[EXCEPTION] Failed to update {node.Name} node status. {ex.Message}");
                                }
                            }

                            return !offline;
                        }

                        UpdateGraph(_currentInstance, false);

                        if (!ReferenceEquals(_currentInstance, _newInstance))
                        {
                            Trace.WriteLine($"[CONFIG] Checking new node graph to see if it online yet");
                            if (UpdateGraph(_newInstance, true))
                            {
                                Trace.WriteLine($"[CONFIG] Bringing new node graph online");

                                var prior = _currentInstance as NodeGraphInstance;
                                _currentInstance = _newInstance;

                                if (prior != null)
                                {
                                    // Make sure nodes have finished processing current requests before disposing
                                    foreach (var node in prior.Nodes)
                                        lock(_disposeQueue) _disposeQueue.Enqueue(new DisposeQueueItem
                                        {
#if DEBUG
                                            WhenUtc = DateTime.UtcNow.AddSeconds(30),
#else
                                            WhenUtc = DateTime.UtcNow.AddMinutes(5),
#endif
                                            Disposable = node
                                        });
                                }
                            }
                        }

                        lock (_disposeQueue)
                        {
                            if (_disposeQueue.Count > 0)
                            {
                                var next = _disposeQueue.Peek();
                                if (next != null && DateTime.UtcNow >= next.WhenUtc)
                                {
                                    Trace.WriteLine($"[DISPOSE] Disposing of " + next.Disposable);
                                    next.Disposable.Dispose();
                                    _disposeQueue.Dequeue();
                                }
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[EXCEPTION] Node graph update thread {ex.Message}");
                    }
                }
            })
            {
                Name = "Update availability",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            _thread.Start();
        }

        public void Configure(NodeGraphConfiguration configuration)
        {
            Trace.WriteLine($"[CONFIG] There is a new node graph configuration");
            try
            {
                configuration = configuration.Sanitize();
            }
            catch (Exception ex)
            {
                _configurationException = ex;
                Trace.WriteLine($"[CONFIG] Exception processing node graph configuration. " + ex.Message);
                throw new Exception("There was a problem with sanitizing the configuration data", ex);
            }

            var nodes = new List<INode>();

            try
            { 
                ConfigureCorsNodes(configuration, nodes);
                ConfigureInternalPageNodes(configuration, nodes);
                ConfigureLeastConnectionsNodes(configuration, nodes);
                ConfigureResponseNodes(configuration, nodes);
                ConfigureRoundRobinNodes(configuration, nodes);
                ConfigureRouterNodes(configuration, nodes);
                ConfigureServerNodes(configuration, nodes);
                ConfigureStickySessionNodes(configuration, nodes);
                ConfigureTransformNodes(configuration, nodes);
                ConfigureChangeLogFilterNodes(configuration, nodes);
                ConfigureCustomLogNodes(configuration, nodes);
            }
            catch (Exception ex)
            {
                _configurationException = ex;
                Trace.WriteLine($"[CONFIG] Exception configuring node graph nodes. " + ex.Message);
                throw new Exception("There was a problem re-configuring nodes", ex);
            }

            var instance = new NodeGraphInstance
            {
                Nodes = nodes.ToArray()
            };

            try
            {
                foreach (var node in nodes)
                    node.Bind(instance);
            }
            catch (Exception ex)
            {
                _configurationException = ex;
                instance.ConfigurationException = ex;

                Trace.WriteLine($"[CONFIG] Exception binding nodes into a graph. " + ex.Message);
                throw new Exception("There was a problem with binding nodes into a graph", ex);
            }

            _newInstance = instance;
            _configurationException = null;

            if (_currentInstance == null)
            {
                Trace.WriteLine($"[CONFIG] There is no current node graph instance, the new configuration will be applied immediately");
                _currentInstance = instance;
            }
            else
            {
                Trace.WriteLine($"[CONFIG] Waiting for new node graph to come online");
            }
        }

        private void ConfigureCorsNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.CorsNodes != null)
            {
                foreach (var corsConfiguration in configuration.CorsNodes)
                {
                    var node = new CorsNode
                    {
                        Name = corsConfiguration.Name,
                        Disabled = corsConfiguration.Disabled,
                        OutputNode = corsConfiguration.OutputNode,
                        AllowCredentials = corsConfiguration.AllowCredentials,
                        AllowedMethods = corsConfiguration.AllowedMethods,
                        AllowedHeaders = corsConfiguration.AllowedHeaders,
                        AllowedOrigins = corsConfiguration.AllowedOrigins,
                        ExposedHeaders = corsConfiguration.ExposedHeaders,
                        WebsiteOrigin = corsConfiguration.WebsiteOrigin
                    };
                    corsConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureInternalPageNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.InternalNodes != null)
            {
                foreach (var internalPageConfiguration in configuration.InternalNodes)
                {
                    var node = new InternalNode
                    {
                        Name = internalPageConfiguration.Name,
                        Disabled = internalPageConfiguration.Disabled,
                    };
                    internalPageConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureLeastConnectionsNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.LeastConnectionsNodes != null)
            {
                foreach (var leastConnectionsConfiguration in configuration.LeastConnectionsNodes)
                {
                    var node = new LeastConnectionsNode
                    {
                        Name = leastConnectionsConfiguration.Name,
                        Disabled = leastConnectionsConfiguration.Disabled,
                        Outputs = leastConnectionsConfiguration.Outputs
                    };
                    leastConnectionsConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureResponseNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.ResponseNodes != null)
            {
                foreach (var responseNodeConfiguration in configuration.ResponseNodes)
                {
                    var node = new ResponseNode
                    {
                        Name = responseNodeConfiguration.Name,
                        Disabled = responseNodeConfiguration.Disabled,
                        StatusCode = responseNodeConfiguration.StatusCode,
                        ReasonPhrase = responseNodeConfiguration.ReasonPhrase ?? "OK",
                        Content = responseNodeConfiguration.Content ?? string.Empty,
                        ContentFile = responseNodeConfiguration.ContentFile,
                    };
                    if (responseNodeConfiguration.Headers != null)
                    {
                        node.HeaderNames = responseNodeConfiguration.Headers.Select(h => h.HeaderName).ToArray();
                        node.HeaderValues = responseNodeConfiguration.Headers.Select(h => h.HeaderValue).ToArray();
                    }
                    responseNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureRoundRobinNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.RoundRobinNodes != null)
            {
                foreach (var roundRobinConfiguration in configuration.RoundRobinNodes)
                {
                    var node = new RoundRobinNode
                    {
                        Name = roundRobinConfiguration.Name,
                        Disabled = roundRobinConfiguration.Disabled,
                        Outputs = roundRobinConfiguration.Outputs
                    };
                    roundRobinConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureRouterNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.RouterNodes != null)
            {
                foreach (var routerNodeConfiguration in configuration.RouterNodes)
                {
                    var node = new RoutingNode(_expressionParser)
                    {
                        Name = routerNodeConfiguration.Name,
                        Disabled = routerNodeConfiguration.Disabled,
                        Outputs = routerNodeConfiguration.Outputs
                    };
                    routerNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureServerNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.ServerNodes != null)
            {
                foreach (var serverNodeConfiguration in configuration.ServerNodes)
                {
                    var node = new ServerNode(_bufferPool, _logFactory, _connectionThreadPool)
                    {
                        Name = serverNodeConfiguration.Name,
                        Disabled = serverNodeConfiguration.Disabled,
                        DomainName = serverNodeConfiguration.Host,
                        Port = serverNodeConfiguration.Port,
                        ConnectionTimeout = serverNodeConfiguration.ConnectionTimeout,
                        ResponseTimeout = serverNodeConfiguration.ResponseTimeout,
                        ReadTimeout = TimeSpan.FromMilliseconds(serverNodeConfiguration.ReadTimeoutMs),
                        ReuseConnections = serverNodeConfiguration.ReuseConnections,
                        MaximumConnectionCount = serverNodeConfiguration.MaximumConnectionCount,
                        DnsLookupInterval = serverNodeConfiguration.DnsLookupInterval,
                        RecalculateInterval = serverNodeConfiguration.RecalculateInterval,
                        HealthCheckPort = serverNodeConfiguration.HealthCheckPort,
                        HealthCheckHost = serverNodeConfiguration.HealthCheckHost,
                        HealthCheckPath = new PathString(serverNodeConfiguration.HealthCheckPath),
                        HealthCheckMethod = serverNodeConfiguration.HealthCheckMethod,
                        HealthCheckCodes = serverNodeConfiguration.HealthCheckCodes,
                        HealthCheckLog = serverNodeConfiguration.HealthCheckLog,
                        HealthCheckInterval = serverNodeConfiguration.HealthCheckInterval,
                        HealthCheckUnhealthyInterval = serverNodeConfiguration.HealthCheckUnhealthyInterval,
                        HealthCheckMaximumFailCount = serverNodeConfiguration.HealthCheckMaximumFailCount,
                        HealthCheckLogDirectory = serverNodeConfiguration.HealthCheckLogDirectory
                    };
                    node.Initialize();
                    serverNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureStickySessionNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.StickySessionNodes != null)
            {
                foreach (var stickySessionNodeConfiguration in configuration.StickySessionNodes)
                {
                    var node = new StickySessionNode
                    {
                        Name = stickySessionNodeConfiguration.Name,
                        Disabled = stickySessionNodeConfiguration.Disabled,
                        Outputs = stickySessionNodeConfiguration.Outputs,
                        SessionCookie = stickySessionNodeConfiguration.SesionCookie,
                        SessionDuration = stickySessionNodeConfiguration.SessionDuration
                    };
                    stickySessionNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureTransformNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            Func<string[], string> joinScript = s =>
            {
                if (s == null || s.Length == 0) return null;
                return string.Join(Environment.NewLine, s);
            };

            if (configuration.TransformNodes != null)
            {
                foreach (var transformNodeConfiguration in configuration.TransformNodes)
                {
                    var node = new TransformNode(_hostingEnvironment, _factory)
                    {
                        Name = transformNodeConfiguration.Name,
                        Disabled = transformNodeConfiguration.Disabled,
                        OutputNode = transformNodeConfiguration.OutputNode,
                        Description = joinScript(transformNodeConfiguration.Description),
                        ScriptLanguage = transformNodeConfiguration.ScriptLanguage,
                        RequestScript = joinScript(transformNodeConfiguration.RequestScript),
                        ResponseScript = joinScript(transformNodeConfiguration.ResponseScript),
                        RequestScriptFile = transformNodeConfiguration.RequestScriptFile,
                        ResponseScriptFile = transformNodeConfiguration.ResponseScriptFile,
                    };
                    transformNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureChangeLogFilterNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.ChangeLogFilterNodes != null)
            {
                foreach (var changeLogFilterNodeConfiguration in configuration.ChangeLogFilterNodes)
                {
                    var node = new ChangeLogFilterNode
                    {
                        Name = changeLogFilterNodeConfiguration.Name,
                        Disabled = changeLogFilterNodeConfiguration.Disabled,
                        OutputNode = changeLogFilterNodeConfiguration.OutputNode,
                        MaximumLogLevel = changeLogFilterNodeConfiguration.MaximumLogLevel,
                        LogTypes = changeLogFilterNodeConfiguration.LogTypes
                    };
                    changeLogFilterNodeConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        private void ConfigureCustomLogNodes(NodeGraphConfiguration configuration, List<INode> nodes)
        {
            if (configuration.CustomLogNodes != null)
            {
                foreach (var customLogConfiguration in configuration.CustomLogNodes)
                {
                    var node = new CustomLogNode
                    {
                        Name = customLogConfiguration.Name,
                        Disabled = customLogConfiguration.Disabled,
                        OutputNode = customLogConfiguration.OutputNode,
                        Methods = customLogConfiguration.Methods,
                        StatusCodes = customLogConfiguration.StatusCodes,
                        Directory = customLogConfiguration.Directory,
                        FileNamePrefix = customLogConfiguration.FileNamePrefix,
                        MaximumLogFileAge = customLogConfiguration.MaximumLogFileAge,
                        MaximumLogFileSize = customLogConfiguration.MaximumLogFileSize,
                        Detailed = customLogConfiguration.Detailed,
                        ContentType = customLogConfiguration.ContentType,
                    };
                    customLogConfiguration.Node = node;
                    nodes.Add(node);
                }
            }
        }

        INode INodeGraph.NodeByName(string name)
        {
            return _currentInstance?.NodeByName(name);
        }

        T[] INodeGraph.GetNodes<T>(Func<INode, T> map, Func<INode, bool> predicate)
        {
            return _currentInstance?.GetNodes(map, predicate);
        }

        public Exception _configurationException;
        public Exception ConfigurationException => _configurationException ?? _currentInstance?.ConfigurationException;

        private class NodeGraphInstance: INodeGraph
        {
            public INode[] Nodes;

            INode INodeGraph.NodeByName(string name)
            {
                return Nodes.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
            }

            void INodeGraph.Configure(NodeGraphConfiguration configuration)
            {
            }

            T[] INodeGraph.GetNodes<T>(Func<INode, T> map, Func<INode, bool> predicate)
            {
                var enumeration = (IEnumerable<INode>)Nodes;
                if (predicate != null) enumeration = enumeration.Where(predicate);
                return enumeration.Select(map).ToArray();
            }

            public Exception ConfigurationException { get; set; }
        }

        private class DisposeQueueItem
        {
            public DateTime WhenUtc;
            public IDisposable Disposable;
        }
    }
}
