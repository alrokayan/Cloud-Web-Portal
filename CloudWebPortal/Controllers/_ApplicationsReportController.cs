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
namespace CloudWebPortal.Controllers
{
    public class _ApplicationsReportController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();
        
        [Authorize]
        public ActionResult GetApplicationsList_RowsOnly(String type, String masterHostIP, int masterPort,
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
            DateTime fromDate = new DateTime(fromDateYear, fromDateMonth, fromDateDay, fromTimeHour, fromDateMin, 0);
            DateTime toDate = new DateTime(toDateYear, toDateMonth, toDateDay, toTimeHour, toDateMin, 0);

            String username = "Administrator";
            String password = "";

            ApplicationManagement applicationManagement = new ApplicationManagement(username, password, masterHostIP, masterPort);
            List<IAccountable> apps = new List<IAccountable>();

            if (type == "All")
            {
                apps = applicationManagement.getActiveApplications();
                apps.AddRange(applicationManagement.getDoneApplications(appQureyType.Type.ALL, fromDate, toDate));
            }
            if (type == "ErrorsOnly")
            {
                apps = applicationManagement.getDoneApplications(appQureyType.Type.NonSuccessfulOnly, fromDate, toDate);
            }
            if (type == "ActiveOnly")
            {
                apps = applicationManagement.getActiveApplications();
            }

            ViewBag.Applications = apps;

            return View();
        }

        public void ForceStopApp(String masterHostIP, int masterPortNumber, String ApplicationMasterService, String ApplicationID)
        {
            String username = "Administrator";
            String password = "";

            ApplicationManagement applicationManagement = new ApplicationManagement(username, password, masterHostIP, masterPortNumber);
            applicationManagement.AbortApplication(ApplicationMasterService, ApplicationID);
        }

        /// <summary>
        /// WIDGET: Get a list of application from Aneke, this list can be filtered by a range of time/date.
        /// Also, this widget can be filtered by choosing only active application, so the user can force close them or application with errors.
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult ApplicationsList(int id)
        {
            ContainersManagement containersManagement = new ContainersManagement();

            //Get a cloud and a master machine entities from the database
            Cloud cloud = db.Clouds.Find(id);
            Machine machine = containersManagement.machineLookupFromMasterId(cloud.Master.MasterId);
            //Pass the master machine IP and the master port number to the view
            ViewBag.masterHostIP = machine.IP;
            ViewBag.masterPort = cloud.Master.Port;
            return View(cloud);
        }

    }
}
