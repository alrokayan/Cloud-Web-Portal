/*
 * Copyright 2011,2012 CLOUDS Laboratory, The University of Melbourne
 *  
 *  This file is part of Cloud Web Portal (CWP).
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Aneka;
using Aneka.Runtime;
using Aneka.PAL.Management.Proxy;

namespace CloudWebPortal.Logic.Reporting
{
    public class LiveResourceUtilizationManagement
    {
        public String masterUri;
        private IContainerManagerProxy proxy = ProxyCreator.CreateContainerManagerProxy();
        private NodeInfo[] nodes = new NodeInfo[] { };

        public double totalCpuUsage = 0;
        public double totalCpuAvailable = 0;
        public double totalMemoryUsage = 0;
        public double totalMemoryAvailable = 0;

        public LiveResourceUtilizationManagement(String MasterUri)
        {
            masterUri = MasterUri;
            //get all the active nodes from the master container
            nodes = proxy.GetNodeInfos(MasterUri, true);
        }

        /// <summary>
        /// Get the latest CPU Usgae Percentage number from the Master
        /// </summary>
        /// <returns>CPU Usgae Percentage</returns>
        public double getCpuUsagePercentForAllNodesAndUpdateTotalVariables()
        {
            double cpuUsagePercent = 0;

            try
            {
                if (proxy.PingNode(masterUri) == true)
                {

                    PerformanceInfo[] performance = proxy.GetPerformanceInfos(masterUri);

                    for (int i = 0; i < performance.Length; i++)
                    {
                        totalCpuUsage += performance[i].CpuUsage;
                        totalCpuAvailable += performance[i].CpuAvailable;

                        totalMemoryUsage += performance[i].MemoryUsage;
                        totalMemoryAvailable += performance[i].MemoryAvailable;
                    }

                    int numOfNodes = nodes.Length;

                    double cpuAvailablPercent = totalCpuAvailable / numOfNodes;

                    if (cpuAvailablPercent > 100)
                    {
                        cpuAvailablPercent = 100;
                    }

                    cpuUsagePercent = 100 - cpuAvailablPercent; 
                }
            }
            catch (Exception ex)
            {

            }
            return cpuUsagePercent;
        }

        /// <summary>
        /// If the user choose a detailed summary, this will get the latest CPU Usgae Percentage number for a specific node
        /// </summary>
        /// <param name="nodeUri">Node URI</param>
        /// <returns>CPU Usgae Percentage for a specific node</returns>
        public double getCpuUsagePercentForNode(String nodeUri)
        {
            double cpuUsagePercent = 0;

            try
            {
                if (proxy.PingNode(masterUri) == true)
                {

                    PerformanceInfo[] performance = proxy.GetPerformanceInfos(masterUri);

                    double CpuAvailable = 0;

                    String nodeID = String.Empty;
                    foreach(var node in nodes)
                        if (node.Uri == nodeUri)
                        {
                            nodeID = node.Id;
                            break;
                        }
                    if (nodeID == String.Empty)
                        return 0;

                    foreach(var p in performance)
                    {
                        if (nodeID == p.NodeId)
                        {
                            CpuAvailable = p.CpuAvailable;
                            break;
                        }
                    }

                    cpuUsagePercent = 100 - CpuAvailable; 
                }
            }
            catch (Exception ex)
            {

            }
            return cpuUsagePercent;
        }
    }
}
