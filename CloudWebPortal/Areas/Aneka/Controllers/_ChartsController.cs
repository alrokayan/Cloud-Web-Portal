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
using System.Web.Mvc;
using System.Web.Script.Serialization;
using CloudWebPortal.Logic;
using CloudWebPortal.Models;
using CloudWebPortal.Logic.Reporting;
using Aneka.Accounting;
using CloudWebPortal.Areas.Aneka.Models;

namespace CloudWebPortal.Areas.Aneka.Controllers
{
    public class _ChartsController : Controller
    {
        private AnekaDbContext db = new AnekaDbContext();

        /// <summary>
        /// WIDGET: Display a historical CPU utilization by selecting a range of time/date.
        /// Also, you can display a summary of the CPU utilization or display each node in a separate line.
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult CPU_Utilization_Range(int id)
        {
            ContainersManagement containersManagement = new ContainersManagement();

            //Get a cloud and a master machine entities from the database.
            Cloud cloud = db.Clouds.Find(id);
            Machine machine = containersManagement.machineLookupFromMasterId(cloud.Master.MasterId);

            //Pass the master machine IP and the master port number to the view.
            ViewBag.masterHostIP = machine.IP;
            ViewBag.masterPort = cloud.Master.Port;
            return View(cloud);
        }

        /// <summary>
        /// WIDGET: Display a live chart of the current cloud (master and all workers) CPU utilization
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult CPU_Utilization_Live(int id)
        {
            ContainersManagement containersManagement = new ContainersManagement();

            //Get a cloud and a master machine entities from the database.
            Cloud cloud = db.Clouds.Find(id);
            Machine machine = containersManagement.machineLookupFromMasterId(cloud.Master.MasterId);

            //Pass the master machine IP and the master port number to the view.
            ViewBag.masterHostIP = machine.IP;
            ViewBag.masterPort = cloud.Master.Port;
            return View(cloud);
        }

        /// <summary>
        /// This is an AJAX call method to get a specific information from the server in CSV formate.
        /// It will be reformatted in the JavaScript
        /// </summary>
        /// <param name="ChartSeriesType">This will be: Summary or Detailed</param>
        /// <param name="masterHostIP"></param>
        /// <param name="masterPort"></param>
        /// <param name="fromDateYear"></param>
        /// <param name="fromDateMonth"></param>
        /// <param name="fromDateDay"></param>
        /// <param name="fromTimeHour"></param>
        /// <param name="fromDateMin"></param>
        /// <param name="toDateYear"></param>
        /// <param name="toDateMonth"></param>
        /// <param name="toDateDay"></param>
        /// <param name="toTimeHour"></param>
        /// <param name="toDateMin"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult GetCSVData(String ChartSeriesType, String masterHostIP, int masterPort,
            int fromDateYear,
            int fromDateMonth,
            int fromDateDay,
            int fromTimeHour,
            int fromDateMin,
            int toDateYear,
            int toDateMonth,
            int toDateDay,
            int toTimeHour,
            int toDateMin)
        {
            String username = "Administrator";
            String password = "";

            global::Aneka.Entity.ServiceAddress serviceAddress = new global::Aneka.Entity.ServiceAddress(ContainersManagement.GetContainerUri(masterHostIP, masterPort), "ReportingService");
            global::Aneka.Security.UserCredentials userCredentials = new global::Aneka.Security.UserCredentials(username, password);
            ResourceUtilizationManagement resourceUtilizationManagement = new Logic.Reporting.ResourceUtilizationManagement(serviceAddress, userCredentials);


            DateTime fromDate = new DateTime(fromDateYear, fromDateMonth, fromDateDay, fromTimeHour, fromDateMin,0);
            DateTime toDate = new DateTime(toDateYear, toDateMonth, toDateDay, toTimeHour, toDateMin, 0);
            CloudWebPortal.Logic.Reporting.Helpers.PeriodSelection periodSelection = new Logic.Reporting.Helpers.PeriodSelection(fromDate, toDate);
            
            String csv = String.Empty;
            if (ChartSeriesType == "Summary")
            {
                resourceUtilizationManagement.QueryPerformanceData(periodSelection);
                List<DateTime> abscissaData = resourceUtilizationManagement.abscissaData;

                List<double> ordinateData = resourceUtilizationManagement.ordinateData;
                Dictionary<DateTime, double> Data = new Dictionary<DateTime, double>();
                for (int i = 0; i < abscissaData.Count; i++)
                    Data.Add(abscissaData[i], ordinateData[i]);

                csv = "Cloud Overview Utilization";
                foreach (var d in Data)
                {
                    csv += "," + d.Key.Year + "-" + d.Key.Month + "-" + d.Key.Day + "-" + d.Key.Hour + "-" + d.Key.Minute + "-" + d.Key.Second + "-" + d.Key.Millisecond
                    + "," + d.Value;
                }
            }
            if (ChartSeriesType == "Detailed")
            {
                resourceUtilizationManagement.QueryPerformanceData_Nodes(periodSelection);
                List<DateTime> abscissaData = resourceUtilizationManagement.abscissaData;

                IDictionary<String, List<double>> ordinateDataForNodes = resourceUtilizationManagement.ordinateDataForNodes;
                foreach (var aNode in ordinateDataForNodes)
                {
                    List<double> ordinateData = aNode.Value;
                    Dictionary<DateTime, double> Data = new Dictionary<DateTime, double>();
                    for (int i = 0; i < abscissaData.Count; i++)
                        Data.Add(abscissaData[i], ordinateData[i]);

                    if (csv != String.Empty)
                        csv += "\n";
                    csv += aNode.Key;
                    foreach (var d in Data)
                    {
                        csv += "," + d.Key.Year + "-" + d.Key.Month + "-" + d.Key.Day + "-" + d.Key.Hour + "-" + d.Key.Minute + "-" + d.Key.Second + "-" + d.Key.Millisecond
                        + "," + d.Value;
                    }
                }
            }

            return Content(csv);
        }

        /// <summary>
        /// This will be used to display a pie chart showing how many failed, succeeded, aborted jobs.
        /// There are many jobs status, not just three.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult JobsStatusSummary(int id)
        {
            ContainersManagement containersManagement = new ContainersManagement();

            //Get a cloud and a master machine entities from the database.
            Cloud cloud = db.Clouds.Find(id);
            Machine machine = containersManagement.machineLookupFromMasterId(cloud.Master.MasterId);

            //Pass the master machine IP and the master port number to the view.
            ViewBag.masterHostIP = machine.IP;
            ViewBag.masterPort = cloud.Master.Port;
            return View(cloud);
        }

        /// <summary>
        /// This is an AJAX call method to get a specific information from the server in CSV formate.
        /// It will be reformatted in the JavaScript
        /// </summary>
        /// <param name="ChartSeriesType">Currently it should be: JobsStatusSummary</param>
        /// <param name="masterHostIP"></param>
        /// <param name="masterPort"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult GetCSVDataNoDates(String ChartSeriesType, String masterHostIP, int masterPort)
        {
            String username = "Administrator";
            String password = "";

            String csv = String.Empty;

            if (ChartSeriesType == "JobsStatusSummary")
            {
                ApplicationManagement applicationManagement = new ApplicationManagement(username, password, masterHostIP, masterPort);
                List<global::Aneka.Accounting.IAccountable> ActiveApplications = applicationManagement.getDoneApplications(appQureyType.Type.ALL, null, null);
                double Total = 0;
                double Completed = 0;
                double Failed = 0;
                double Aborted = 0;
                double Rejected = 0;
                double Unsubmitted = 0;
                double Unknown = 0;

                foreach (global::Aneka.Entity.ApplicationView app in ActiveApplications)
                {
                    Total += app.Total;
                    Completed += app.Completed;
                    Failed += app.Failed;
                    Aborted += app.Aborted;
                    Rejected += app.Rejected;
                    Unsubmitted += app.Unsubmitted;
                    Unknown += app.Unknown;
                }

                csv += "Total," + Total;
                csv += "\nCompleted," + (Completed / Total) * 100;
                if(Failed > 0)
                    csv += "\nFailed," + (Failed / Total) * 100;
                if (Aborted > 0)
                    csv += "\nAborted," + (Aborted / Total) * 100;
                if (Rejected > 0)
                    csv += "\nRejected," + (Rejected / Total) * 100;
                if (Unsubmitted > 0)
                    csv += "\nUnsubmitted," + (Unsubmitted / Total) * 100;
                if (Unknown > 0)
                    csv += "\nUnknown," + (Unknown / Total) * 100;
            }

            return Content(csv);
        }

        /// <summary>
        /// AJAX Call
        /// </summary>
        /// <param name="masterHostIP"></param>
        /// <param name="masterPort"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult GetLiveCpuUsagePercentForCloud(String masterHostIP, int masterPort)
        {
            String cpuUsagePercent = "0";

            LiveResourceUtilizationManagement liveResourceUtilizationManagement = new LiveResourceUtilizationManagement(ContainersManagement.GetContainerUri(masterHostIP, masterPort));

            double cpuUsagePercentInDouble = liveResourceUtilizationManagement.getCpuUsagePercentForAllNodesAndUpdateTotalVariables();
            cpuUsagePercent = Convert.ToString(cpuUsagePercentInDouble);

            return Content(cpuUsagePercent);
        }

        /// <summary>
        /// AJAX Call
        /// </summary>
        /// <param name="masterHostIP"></param>
        /// <param name="masterPort"></param>
        /// <param name="nodeHostIP"></param>
        /// <param name="nodePort"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult GetLiveCpuUsagePercentForNode(String masterHostIP, int masterPort, String nodeHostIP, int nodePort)
        {
            String cpuUsagePercent = "0";

            LiveResourceUtilizationManagement liveResourceUtilizationManagement = new Logic.Reporting.LiveResourceUtilizationManagement(ContainersManagement.GetContainerUri(masterHostIP, masterPort));

            double cpuUsagePercentInDouble = liveResourceUtilizationManagement.getCpuUsagePercentForNode(ContainersManagement.GetContainerUri(nodeHostIP, nodePort));
            cpuUsagePercent = Convert.ToString(cpuUsagePercentInDouble);

            return Content(cpuUsagePercent);
        }
    }
}
