/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Cluster;
using Akka.Cluster.Tools.Singleton;
using Akka.Cluster.Sharding;
using Akka.Persistence;

namespace QuantApp.Kernel
{
    public class Actor
    {
        private static ActorSystem _system;

        public static ActorSystem getSystem()
        {
            if(_system == null)
            {
                // var port = 5000;
                // var host = "localhost";
                // var config = ConfigurationFactory.ParseString(@"
                //     akka {
                //         loggers                 = [""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]
                //         loglevel                = info
                //         log-config-on-start     = on
                //         actor {
                //             provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                //             serializers {
                //                 hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                //             }
                //             serialization-bindings {
                //                 ""System.Object"" = hyperion
                //             }

                //             debug{
                //                 receive         = on  # log any received message
                //                 autoreceive     = on  # log automatically received messages, e.g. PoisonPill
                //                 lifecycle       = on  # log actor lifecycle changes
                //                 event-stream    = on  # log subscription changes for Akka.NET event stream
                //                 unhandled       = on  # log unhandled messages sent to actors
                //             }
                //         }
                //         remote {
                //             helios.tcp {
                //             public-hostname = """ + host + @"""
                //             hostname = """ + host + @"""
                //             port = """ + port.ToString() + @"""
                //             }
                //         }
                //         cluster {
                //             auto-down-unreachable-after = 5s
                //             seed-nodes = [""akka.tcp://cluster-system@""" + host + ":" + port.ToString() + @"/""]
                //         }
                //     }
                //     ");

                // var port = 5000;
                // var host = "localhost";
                var config = ConfigurationFactory.ParseString(@"
                    akka {
                        loggers                 = [""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]
                        loglevel                = info
                        log-config-on-start     = on
                        actor {
                            serializers {
                                hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                            }
                            serialization-bindings {
                                ""System.Object"" = hyperion
                            }

                            debug{
                                receive         = on  # log any received message
                                autoreceive     = on  # log automatically received messages, e.g. PoisonPill
                                lifecycle       = on  # log actor lifecycle changes
                                event-stream    = on  # log subscription changes for Akka.NET event stream
                                unhandled       = on  # log unhandled messages sent to actors
                            }
                        }
                    }
                    ");

                config.WithFallback(ClusterSingletonManager.DefaultConfig());

                _system = ActorSystem.Create("cluster-system", config);
            }

            return _system;
        }
    }
}