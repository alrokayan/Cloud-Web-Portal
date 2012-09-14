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
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CloudWebPortal.Models;
using CloudWebPortal.Logic;
using CloudWebPortal.Areas.Aneka.Models;

namespace CloudWebPortal.Areas.Aneka.Controllers
{ 
    public class _CloudsController : Controller
    {
        private AnekaDbContext db = new AnekaDbContext();

        public delegate void AsynchronousCallback(int CloudID);

        /// <summary>
        /// WIDGET: A list of all created clouds. With a summary for each one, this summary includes a small list of all associated nodes, and some hardware statistics like: available disc space.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult List()
        {
            //Get all clouds from the database, and pass it to the view.
            return PartialView(db.Clouds.ToList());
        }

        /// <summary>
        /// This widget is the largest, and the most complicated one. It will show al the cloud, nodes information.
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            MasterContainersManagement masterContainersManagement = new MasterContainersManagement();

            //Get the cloud entity from the database
            Cloud cloud = db.Clouds.Find(id);

            if (cloud == null)
            {
                cloud = new Cloud();
                cloud.CloudName = "Cloud could not be found";
                return PartialView();
            }

            //Pass the master machine entity to the view
            ViewBag.MasterMachine = masterContainersManagement.machineLookupFromMasterId(cloud.Master.MasterId);

            //Create and pass a Dictionary to the view, the key is the worker ID, and the value id the machine entity.
            Dictionary<int, Machine> WorkersMachines = new Dictionary<int, Machine>();
            List<Machine> AllMachines = db.Machines.ToList();
            foreach (Worker worker in cloud.Workers)
            {
                foreach (Machine machine in AllMachines)
                {
                    if(machine.Workers.Contains(worker))
                        WorkersMachines.Add(worker.WorkerId,machine);
                }
            }
            ViewBag.WorkersMachines = WorkersMachines;

            //Pass the cloud entity to the view
            return PartialView(cloud);
        }

        /// <summary>
        /// WIDGET: A form to create a new cloud.
        /// Note: the user will input both the cloud and the master information.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create()
        {
            //Get a list of active machines, and pass them to the view
            int serviceStartedInt = (int)global::Aneka.PAL.Management.Model.DaemonProbeStatus.ServiceStarted;
            ViewBag.Machines = new SelectList(db.Machines.Where(x => x.Status == serviceStartedInt).ToList(), "MachineId", "DisplayName", 1);
            
            //Get a list of all services, and pass them to the view
            ViewBag.Services = new MultiSelectList(db.Services.ToList(), "ServiceId", "Name");
            
            return PartialView();
        } 

        /// <summary>
        /// This is the post method of Create, after the user press the submit button.
        /// </summary>
        /// <param name="cloud">Cloud details from the form</param>
        /// <param name="master">Master details from the form</param>
        /// <param name="masterMachineID">Selected master machine ID</param>
        /// <param name="selectedServicesIDs">List of selected services IDs</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult CreatePost(Cloud cloud, Master master, int? masterMachineID, int?[] selectedServicesIDs)
        {
            if (!masterMachineID.HasValue)
                ModelState.AddModelError(String.Empty, "You must select a master");
            if (selectedServicesIDs == null)
                ModelState.AddModelError(String.Empty, "You must select at least one service");

            if (ModelState.IsValid)
            {
                master.isInstalled = false;
                master.Services = new List<Service>();
                foreach (var sericeId in selectedServicesIDs)
                {
                    Service service = db.Services.Find(sericeId);
                    master.Services.Add(service);
                }
                db.SaveChanges();

                Machine machine = db.Machines.Find(masterMachineID);
                if (machine.Masters == null)
                    machine.Masters = new List<Master>();
                machine.Masters.Add(master);
                db.SaveChanges();

                cloud.Master = master;
                db.SaveChanges();

                db.Clouds.Add(cloud);
                db.SaveChanges();

                CreateContainer(cloud.CloudId);

                return RedirectToAction("Dashboard", "Home");
            }

            return View("~/Views/Home/Dashboard.cshtml",cloud);
        }
        
        /// <summary>
        /// A form to edit a selected cloud.
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            Cloud cloud = db.Clouds.Find(id);

            //Get the master machine entity, and pass it to the view as dropdown list
            int MachinesID = 0;
            foreach (Machine m in db.Machines.ToList())
                if (m.Masters.Where(x => x.MasterId == cloud.Master.MasterId).ToList().Count > 0)
                {
                    MachinesID = m.MachineId;
                    break;
                }
            ViewBag.Machines = new SelectList(db.Machines.ToList(), "MachineId", "DisplayName", MachinesID);

            //Get all associated services to this cloud, and pass it to the view as a multi select list.
            List<int> servicesIDs = new List<int>();
            foreach (Service s in cloud.Master.Services)
                servicesIDs.Add(s.ServiceId);
            ViewBag.Services = new MultiSelectList(db.Services.ToList(), "ServiceId", "Name", servicesIDs);
            
            return PartialView(cloud);
        }

        /// <summary>
        /// This is the post method for the Edit from
        /// </summary>
        /// <param name="cloud">Cloud entity with the new data</param>
        /// <param name="master">Master entity with the new data</param>
        /// <param name="masterMachineID">the new Master ID</param>
        /// <param name="selectedServicesIDs">the new list of the services</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult EditPost(Cloud cloud, Master master, int? masterMachineID, int?[] selectedServicesIDs)
        {
            if (!masterMachineID.HasValue)
                ModelState.AddModelError(String.Empty, "You must select a master");
            if (selectedServicesIDs == null)
                ModelState.AddModelError(String.Empty, "You must select at least one service");

            if (ModelState.IsValid)
            {
                //db.Entry(cloud).State = EntityState.Modified;
                //db.SaveChanges();

                Cloud cloudFromDB = db.Clouds.Find(cloud.CloudId);
                cloudFromDB.CloudName = cloud.CloudName;
                cloudFromDB.DBConnectionString = cloud.DBConnectionString;
                db.SaveChanges();
                
                Master masterFromDB = db.Masters.Find(master.MasterId);

                masterFromDB.Cost = master.Cost;
                masterFromDB.DisplayName = master.DisplayName;
                masterFromDB.MasterFailoverBackupURI = master.MasterFailoverBackupURI;
                masterFromDB.Port = master.Port;

                if (masterFromDB.Services != null)
                {
                    List<Service> services = masterFromDB.Services.ToList();
                    foreach (var service in services)
                        masterFromDB.Services.Remove(service);
                }
                else
                    masterFromDB.Services = new List<Service>();

                foreach (var sericeId in selectedServicesIDs)
                {
                    Service service = db.Services.Find(sericeId);
                    masterFromDB.Services.Add(service);
                }
                db.SaveChanges();


                foreach (Machine machineFromDB in db.Machines.ToList())
                    if (machineFromDB.Masters.Where(x => x.MasterId == masterFromDB.MasterId).ToList().Count > 0)
                    {
                        machineFromDB.Masters.Remove(masterFromDB);
                        db.SaveChanges();
                        break;
                    }

                Machine machine = db.Machines.Find(masterMachineID);
                if (machine.Masters == null)
                    machine.Masters = new List<Master>();
                machine.Masters.Add(masterFromDB);
                db.SaveChanges();

                //////TO DO
                //Edit Container in the cloud

                return RedirectToAction("Dashboard", "Home");
            }
            return View("~/Views/Home/Dashboard.cshtml",cloud);
        }

        /// <summary>
        /// A confirmation message to delete a cloud
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get and pass the cloud
            Cloud cloud = db.Clouds.Find(id);
            return PartialView(cloud);
        }

        /// <summary>
        /// The post method to delete the cloud
        /// </summary>
        /// <param name="CloudId">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost, ActionName("DeletePost")]
        public ActionResult DeleteConfirmed(int CloudId)
        {
            Cloud cloud = db.Clouds.Find(CloudId);

            //Remove all CloudUserAccounts
            for (int i = 0; i < cloud.CloudUserAccounts.Count(); i++)
                cloud.CloudUserAccounts.Remove(cloud.CloudUserAccounts.ToList()[i]);

            //Remove all Workers and their Services
            for (int i = 0; i < cloud.Workers.Count(); i++)
            {
                var worker = cloud.Workers.ToList()[i];
                for (int j = 0; j < worker.Services.Count(); j++)
                    worker.Services.Remove(worker.Services.ToList()[j]);
                cloud.Workers.Remove(worker);
            }
            
            //Remove the Master services, and the master it self
            for (int i = 0; i < cloud.Master.Services.Count(); i++)
                cloud.Master.Services.Remove(cloud.Master.Services.ToList()[i]);
            cloud.Master = null;

            db.Clouds.Remove(cloud);
            db.SaveChanges();
            return RedirectToAction("Dashboard", "Home");
        }

        /// <summary>
        /// AJAX Call to start the master
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult StartContainer(int id)
        {
            MasterContainersManagement masterContainersManagement = new MasterContainersManagement();

            UpdateInProgressMessage(id, "Starting Master Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(masterContainersManagement.StartMasterContainer);
            ContainerThread.BeginInvoke(id, null, null);
            return RedirectToAction("CloudDetails", "CloudManagement", new { id = id });
        }

        /// <summary>
        /// AJAX call to stop the master
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult StopContainer(int id)
        {
            MasterContainersManagement masterContainersManagement = new MasterContainersManagement();

            UpdateInProgressMessage(id, "Stoping Master Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(masterContainersManagement.StopMasterContainer);
            ContainerThread.BeginInvoke(id, null, null);
            return RedirectToAction("CloudDetails", "CloudManagement", new { id = id });
        }

        /// <summary>
        /// AJAX Call to delete the master files
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult DestroyContainer(int id)
        {
            MasterContainersManagement masterContainersManagement = new MasterContainersManagement();

            UpdateInProgressMessage(id, "Destroying Master Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(masterContainersManagement.DestroyMasterContainer);
            ContainerThread.BeginInvoke(id, null, null);
            return RedirectToAction("CloudDetails", "CloudManagement", new { id = id });
        }

        /// <summary>
        /// AJAX Call to create the master
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult CreateContainer(int id)
        {
            MasterContainersManagement masterContainersManagement = new MasterContainersManagement();

            UpdateInProgressMessage(id, "Creating Master Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(masterContainersManagement.CreateCloud);
            ContainerThread.BeginInvoke(id, null, null);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = id });
        }

        /// <summary>
        /// AJAX Call to restart the master
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult RestartContainer(int id)
        {
            MasterContainersManagement masterContainersManagement = new MasterContainersManagement();

            UpdateInProgressMessage(id, "Restarting Master Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(masterContainersManagement.RestartMasterContainer);
            ContainerThread.BeginInvoke(id, null, null);
            return RedirectToAction("CloudDetails", "CloudManagement", new { id = id });
        }


        private void UpdateInProgressMessage(int cloudID, string message)
        {
            Cloud cloud = db.Clouds.Find(cloudID);
            if (cloud.Master == null)
                cloud.Master = new Master();
            cloud.Master.isInProgress = true;
            cloud.Master.ProgressMesage = message;
            db.SaveChanges();
        }

        /// <summary>
        /// AJAX Call to refresh master status
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult RefreshMaster(int id)
        {
            MasterContainersManagement masterContainersManagement = new MasterContainersManagement();

            masterContainersManagement.RefreshMaster(id);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}