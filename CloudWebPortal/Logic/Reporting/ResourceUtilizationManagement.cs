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
using CloudWebPortal.Logic.Reporting.Helpers;
using Aneka.Reporting;

namespace CloudWebPortal.Logic.Reporting
{
    public class ResourceUtilizationManagement
    {
        private ReportManager reportManager;
        public List<DateTime> abscissaData = new List<DateTime>();
        public List<double> ordinateData = new List<double>();
        public IDictionary<String, List<double>> ordinateDataForNodes = new Dictionary<string, List<double>>();

        public ResourceUtilizationManagement(Aneka.Entity.ServiceAddress serviceAddress, Aneka.Security.UserCredentials userCredentials)
        {
            reportManager = new ReportManager(serviceAddress, userCredentials);
        }

        /// <summary>
        /// Queries the remote Reporting Service for performance data for the specified period.
        /// </summary>
        /// <param name="eventData">An instance of <see cref="T:Aneka.UI.Reporting.PeriodSelectionEventArgs"/>
        /// containing the from and to dates.</param>
        public void QueryPerformanceData(object eventData)
        {
            try
            {
                PeriodSelection periodSelectionEvent = (PeriodSelection)eventData;

                ReportingData[] reportingDataItems = reportManager.QueryReportingData(typeof(PerformanceData), periodSelectionEvent.FromDate, periodSelectionEvent.ToDate);

                PerformanceData[] performanceDataItems = Array.ConvertAll(reportingDataItems,
                                                new Converter<ReportingData, PerformanceData>(delegate(ReportingData mdata)
                                                {
                                                    return (PerformanceData)mdata;
                                                }));
                if ((performanceDataItems != null && performanceDataItems.Length > 0))
                    UpdateUtilizationGraphByCPU(periodSelectionEvent.FromDate, periodSelectionEvent.ToDate, performanceDataItems);
            }
            catch (Exception ex)
            {
                String error = "Error Querying: An error occured while querying: " + ex.Message;
            }
        }



        /// <summary>
        /// Updates the CPU utilization graph for the specified period, using the given
        /// performance data. 
        /// </summary>
        /// <param name="fromDate">The starting date of the specified period</param>
        /// <param name="toDate">The ending date of the specified period</param>
        private void UpdateUtilizationGraphByCPU(DateTime fromDate, DateTime toDate, PerformanceData[] performanceDataItems)
        {
            int maxNodes = 0;
            double minLoad = 100;
            double peakLoad = 0;
            double maxCpuCapacity = 0;
            double minCpuCapacity = double.MaxValue;
            double maxMemoryCapacity = 0;
            double minMemoryCapacity = double.MaxValue;
            double maxStorageCapacity = 0;
            double minStorageCapacity = double.MaxValue;

            DateTime peakLoadTime = fromDate;
            DateTime minLoadTime = fromDate;
            DateTime currentTime = fromDate;
            DateTime incTime = currentTime;

            // [DK] NOTE: We divide the period specified by the parameters 'fromDate'
            //            and 'toDate' into short timeslots. A timeslot is large
            //            enough to encompass performance info from all nodes in the 
            //            network (i.e. it's larger than the Heartbeat interval). We 
            //            then determine the utilization for each of these timeslots.

            while (currentTime <= toDate)
            {
                // Record the start of the timeslot
                abscissaData.Add(currentTime);

                // Determine next timeslot
                incTime = currentTime.AddSeconds(ReportingUtil.ChartTimeGranularity);

                double cpuCapacity = 0;
                double memoryCapacity = 0;
                double storageCapacity = 0;

                IDictionary<string, Node> nodes = new Dictionary<string, Node>();

                // Determine the utilization for the given timeslot by
                // finding all nodes and their average CPU usage within
                // this period.

                foreach (PerformanceData pdata in performanceDataItems)
                {
                    if (pdata.TimeStamp >= currentTime)
                    {
                        if (pdata.TimeStamp <= incTime)
                        {
                            if (nodes.ContainsKey(pdata.NodeUri) == false)
                            {
                                Node newNode = new Node(pdata.NodeUri);
                                newNode.CpuCapacity = pdata.CpuCapacity;
                                newNode.MemoryCapacity = pdata.MemoryCapacity;
                                newNode.StorageCapacity = pdata.StorageCapacity;

                                nodes[pdata.NodeUri] = newNode;
                            }

                            Node node = nodes[pdata.NodeUri];
                            node.CpuUsage = (node.CpuUsage + pdata.CpuUsage) / 2;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                double totalCpuUsage = 0;
                double cpuUsagePercent = 0;

                // Compute utilization within this slot..
                foreach (Node node in nodes.Values)
                {
                    totalCpuUsage += node.CpuUsage;
                    cpuCapacity += node.CpuCapacity;
                    memoryCapacity += node.MemoryCapacity;
                    storageCapacity += node.StorageCapacity;
                }

                if (nodes.Count != 0)
                {
                    cpuUsagePercent = totalCpuUsage / nodes.Count;
                }

                ordinateData.Add(cpuUsagePercent);

                // Determine maximums for number of nodes, CPU, memory and storage 
                // capacity for the current time slot..

                maxNodes = Math.Max(maxNodes, nodes.Count);
                maxCpuCapacity = Math.Max(maxCpuCapacity, cpuCapacity);
                minCpuCapacity = Math.Min(minCpuCapacity, cpuCapacity);
                maxMemoryCapacity = Math.Max(maxMemoryCapacity, memoryCapacity);
                minMemoryCapacity = Math.Min(minMemoryCapacity, memoryCapacity);
                maxStorageCapacity = Math.Max(maxStorageCapacity, storageCapacity);
                minStorageCapacity = Math.Min(minStorageCapacity, storageCapacity);

                peakLoadTime = (cpuUsagePercent > peakLoad) ? currentTime : peakLoadTime;
                peakLoad = Math.Max(peakLoad, cpuUsagePercent);

                minLoadTime = (cpuUsagePercent < minLoad) ? currentTime : minLoadTime;
                minLoad = Math.Min(minLoad, cpuUsagePercent);

                // Move to the next timeslot
                currentTime = incTime;
            }

        }

        /// <summary>
        /// Queries the remote Reporting Service for performance data for the specified period.
        /// </summary>
        /// <param name="eventData">An instance of <see cref="T:Aneka.UI.Reporting.PeriodSelectionEventArgs"/>
        /// containing the from and to dates.</param>
        public void QueryPerformanceData_Nodes(object eventData)
        {
            try
            {
                PeriodSelection periodSelectionEvent = (PeriodSelection)eventData;

                ReportingData[] reportingDataItems = reportManager.QueryReportingData(typeof(PerformanceData), periodSelectionEvent.FromDate, periodSelectionEvent.ToDate);

                PerformanceData[] performanceDataItems = Array.ConvertAll(reportingDataItems,
                                                new Converter<ReportingData, PerformanceData>(delegate(ReportingData mdata)
                                                {
                                                    return (PerformanceData)mdata;
                                                }));
                if ((performanceDataItems != null && performanceDataItems.Length > 0))
                    UpdateUtilizationGraphByCPU_Nodes(periodSelectionEvent.FromDate, periodSelectionEvent.ToDate, performanceDataItems);
            }
            catch (Exception ex)
            {
                String error = "Error Querying: An error occured while querying: " + ex.Message;
            }
        }

        /// <summary>
        /// Updates the CPU utilization graph for the specified period, using the given
        /// performance data. 
        /// </summary>
        /// <param name="fromDate">The starting date of the specified period</param>
        /// <param name="toDate">The ending date of the specified period</param>
        private void UpdateUtilizationGraphByCPU_Nodes(DateTime fromDate, DateTime toDate, PerformanceData[] performanceDataItems)
        {
            DateTime peakLoadTime = fromDate;
            DateTime minLoadTime = fromDate;
            DateTime currentTime = fromDate;
            DateTime incTime = currentTime;

            // [DK] NOTE: We divide the period specified by the parameters 'fromDate'
            //            and 'toDate' into short timeslots. A timeslot is large
            //            enough to encompass performance info from all nodes in the 
            //            network (i.e. it's larger than the Heartbeat interval). We 
            //            then determine the utilization for each of these timeslots.

            while (currentTime <= toDate)
            {
                // Record the start of the timeslot
                abscissaData.Add(currentTime);

                // Determine next timeslot
                incTime = currentTime.AddSeconds(ReportingUtil.ChartTimeGranularity);

                IDictionary<string, Node> nodes = new Dictionary<string, Node>();

                // Get all possible NodeUris
                foreach (PerformanceData pdata in performanceDataItems)
                {
                    if (!ordinateDataForNodes.ContainsKey(pdata.NodeUri))
                        ordinateDataForNodes.Add(pdata.NodeUri, new List<double>());
                }


                // Determine the utilization for the given timeslot by
                // finding all nodes and their average CPU usage within
                // this period.

                foreach (PerformanceData pdata in performanceDataItems)
                {
                    if (pdata.TimeStamp >= currentTime)
                    {
                        if (pdata.TimeStamp <= incTime)
                        {
                            if (nodes.ContainsKey(pdata.NodeUri) == false)
                            {
                                Node newNode = new Node(pdata.NodeUri);
                                newNode.CpuCapacity = pdata.CpuCapacity;
                                newNode.MemoryCapacity = pdata.MemoryCapacity;
                                newNode.StorageCapacity = pdata.StorageCapacity;

                                nodes[pdata.NodeUri] = newNode;
                            }

                            Node node = nodes[pdata.NodeUri];
                            node.CpuUsage = (node.CpuUsage + pdata.CpuUsage) / 2;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                foreach (var ordinateDataNode in ordinateDataForNodes)
                {
                    foreach (Node node in nodes.Values)
                    {
                        if (node.NodeUri == ordinateDataNode.Key)
                        {
                            ordinateDataNode.Value.Add(node.CpuUsage);
                            break;
                        }
                    }
                    ordinateDataNode.Value.Add(0);
                    
                }

                // Move to the next timeslot
                currentTime = incTime;
            }

            
        }
    }
}