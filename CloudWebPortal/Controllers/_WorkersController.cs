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

namespace CloudWebPortal.Controllers
{ 
    public class _WorkersController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        public delegate void AsynchronousCallback(int WorkerID);

        /// <summary>
        /// WIDGET: A simple list of workers for a specific cloud
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult List(int id)
        {
            //Get that specific cloud using the Cloud ID
            Cloud cloud = db.Clouds.Find(id);

            //Create a Dictionary, the key is the worker ID and the value is the Machine entity
            Dictionary<int, Machine> WorkersMachines = new Dictionary<int, Machine>();
            List<Machine> AllMachines = db.Machines.ToList();
            foreach (Worker worker in cloud.Workers)
            {
                foreach (Machine machine in AllMachines)
                {
                    if (machine.Workers.Contains(worker))
                        WorkersMachines.Add(worker.WorkerId, machine);
                }
            }
            //Pass the Dictionary to the view
            ViewBag.WorkersMachines = WorkersMachines;

            //Pass the cloud to the view
            return PartialView(cloud);
        }

        /// <summary>
        /// WIDGET: Detailed information about a specific Worker
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Get the Worker entity from the database using the given ID
            WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();

            //Get and pass to the view the machine that has this worker
            Worker worker = db.Workers.Find(id);
            Cloud cloud = workerContainersManagement.cloudLookupFromWorkerId(id);
            ViewBag.Machine = workerContainersManagement.machineLookupFromMasterId(cloud.Master.MasterId);

            //Pass this entity to the view
            return PartialView(worker);
        }

        /// <summary>
        /// WIDGET: A form to create a new Worker
        /// </summary>
        /// <param name="id">Cloud ID to link the cloud with the new worker</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create(int? id)
        {
            //Pass to the view a dropdown list of clouds
            ViewBag.Clouds = new SelectList(db.Clouds.ToList(), "CloudId", "CloudName", id);

            //Pass to the view a Multi-Select list of workers only Services 
            ViewBag.Services = new MultiSelectList(db.Services.Where(x => x.isMasterOnly == false).ToList(), "ServiceId", "Name");

            //Pass to the view dropdown list of Machines
            ViewBag.Machines = new SelectList(db.Machines.ToList(), "MachineId", "DisplayName");

            return PartialView();
        } 

        /// <summary>
        /// The post method to create a worker
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="isQuarantined"></param>
        /// <param name="CloudID"></param>
        /// <param name="ServicesList"></param>
        /// <param name="MachineID"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult CreatePost(Worker worker, bool isQuarantined, int? CloudID, int?[] ServicesList, int? MachineID)
        {
            if (!CloudID.HasValue)
                ModelState.AddModelError(String.Empty, "Please choose a cloud");
            if (!MachineID.HasValue)
                ModelState.AddModelError(String.Empty, "Please choose a machine");
            if (!CloudID.HasValue)
                ModelState.AddModelError(String.Empty, "Please choose a cloud");
            if (ServicesList == null)
                ModelState.AddModelError(String.Empty, "Please choose at least one service");

            if (ModelState.IsValid)
            {
                //Add the worker it self
                worker.isQuarantined = isQuarantined;
                db.Workers.Add(worker);
                db.SaveChanges();

                //Add Services to the worker
                worker.Services = new List<Service>();
                foreach (int serviceID in ServicesList)
                {
                    var service = db.Services.Find(serviceID);
                    worker.Services.Add(service);
                }
                db.SaveChanges();

                //Add this worker to a cloud
                db.Clouds.Find(CloudID).Workers.Add(worker);
                db.SaveChanges();

                //Add this worker to a machine
                db.Machines.Find(MachineID).Workers.Add(worker);
                db.SaveChanges();

                CreateContainer(worker.WorkerId);

                return RedirectToAction("Dashboard", "Home");
            }

            return View("~/Views/Home/Dashboard.cshtml", worker);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific Worker by getting it’s ID
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the Worker entity from the database.
            Worker worker = db.Workers.Find(id);

            //Pass to the view a dropdown list of selected clouds.
            int CloudId = 0;
            foreach (var c in db.Clouds.ToList())
                if (c.Workers.Where(x => x.WorkerId == id).Count() > 0)
                    CloudId = c.CloudId;
            ViewBag.Clouds = new SelectList(db.Clouds.ToList(), "CloudId", "CloudName", CloudId);

            //Pass to the view a Multi-Select list of workers only selected Services.
            List<int> ServicesIds = new List<int>();
            foreach (var s in worker.Services.ToList())
                ServicesIds.Add(s.ServiceId);
            ViewBag.Services = new MultiSelectList(db.Services.Where(x => x.isMasterOnly == false).ToList(), "ServiceId", "Name", ServicesIds);

            //Pass the whole entity to the view so the user can edit it.
            return PartialView(worker);
        }

        /// <summary>
        /// This is the post method to edit a specific Worker
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="isQuarantined"></param>
        /// <param name="CloudID"></param>
        /// <param name="ServicesList"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult EditPost(Worker worker, bool isQuarantined, int? CloudID, int?[] ServicesList)
        {
            if (!CloudID.HasValue)
                ModelState.AddModelError(String.Empty, "Please choose a cloud");

            if (ModelState.IsValid)
            {
                db.Entry(worker).State = EntityState.Modified;
                db.SaveChanges();

                //Link To Cloud
                var CloudList = db.Clouds.ToList();
                for (int CloudIndex = 0; CloudIndex < CloudList.Count(); CloudIndex++)
                    if (CloudList[CloudIndex].Workers.Where(x => x.WorkerId == worker.WorkerId).Count() > 0)
                        db.Clouds.ToList()[CloudIndex].Workers.Remove(worker);
                db.SaveChanges();
                if (CloudID.HasValue)
                {
                    db.Clouds.Find(CloudID).Workers.Add(worker);
                    db.SaveChanges();
                }

                //Services List
                var worker2 = db.Workers.Include(x => x.Services).Where(x => x.WorkerId == worker.WorkerId).First();

                if (worker2.Services == null)
                    worker2.Services = new List<Service>();

                foreach (var service in db.Services.ToList())
                    worker2.Services.Remove(service);
                db.SaveChanges();

                if (ServicesList != null)
                {
                    foreach (int serviceID in ServicesList)
                    {
                        var service = db.Services.Find(serviceID);
                        worker2.Services.Add(service);
                    }
                    db.SaveChanges();
                }

                WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();
                workerContainersManagement.RefreshWorker(worker2.WorkerId);

                return RedirectToAction("Dashboard", "Home");
            }
            return View("~/Views/Home/Dashboard.cshtml", worker);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a Worker
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the Worker entity from the database using the given ID
            Worker worker = db.Workers.Find(id);

            //Pass this entity to the view
            return PartialView(worker);
        }

        /// <summary>
        /// The post method to delete a specific Worker
        /// </summary>
        /// <param name="WorkerId"></param>
        /// <returns></returns>
        [HttpPost, ActionName("DeletePost")]
        [Authorize]
        public ActionResult DeleteConfirmed(int WorkerId)
        {
            Worker worker = db.Workers.Find(WorkerId);

            for (int i = 0; i < worker.Services.Count(); i++)
                worker.Services.Remove(worker.Services.ToList()[i]);
            db.SaveChanges();

            db.Workers.Remove(worker);
            db.SaveChanges();
            return RedirectToAction("Dashboard", "Home");
        }

        /// <summary>
        /// Ajax call to Create Worker Container
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult CreateContainer(int id)
        {
            WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();

            UpdateInProgressMessage(id, "Creating Worker Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(workerContainersManagement.CreateWorker);
            ContainerThread.BeginInvoke(id, null, null);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = new WorkerContainersManagement().cloudLookupFromWorkerId(id).CloudId });
        }

        /// <summary>
        /// Ajax call to Start Worker Container
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult StartContainer(int id)
        {
            WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();

            UpdateInProgressMessage(id, "Starting Worker Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(workerContainersManagement.StartWorkerContainer);
            ContainerThread.BeginInvoke(id, null, null);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = new WorkerContainersManagement().cloudLookupFromWorkerId(id).CloudId });
        }

        /// <summary>
        /// Ajax call to Stop Worker Container
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult StopContainer(int id)
        {
            WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();

            UpdateInProgressMessage(id, "Stopping Worker Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(workerContainersManagement.StopWorkerContainer);
            ContainerThread.BeginInvoke(id, null, null);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = new WorkerContainersManagement().cloudLookupFromWorkerId(id).CloudId });
        }

        /// <summary>
        /// Ajax call to Destroy Worker Container
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult DestroyContainer(int id)
        {
            WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();

            UpdateInProgressMessage(id, "Destroying Worker Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(workerContainersManagement.DestroyWorkerContainer);
            ContainerThread.BeginInvoke(id, null, null);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = new WorkerContainersManagement().cloudLookupFromWorkerId(id).CloudId });
        }

        /// <summary>
        /// Ajax call to Restart Worker Container
        /// </summary>
        /// <param name="id">Worker ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult RestartContainer(int id)
        {
            WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();

            UpdateInProgressMessage(id, "Restarting Worker Container");
            AsynchronousCallback ContainerThread = new AsynchronousCallback(workerContainersManagement.RestartWorkerContainer);
            ContainerThread.BeginInvoke(id, null, null);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = new WorkerContainersManagement().cloudLookupFromWorkerId(id).CloudId });
        }

        private void UpdateInProgressMessage(int wrkerID, string message)
        {
            CloudWebPortal.Models.Worker worker = db.Workers.Find(wrkerID);

            worker.isInProgress = true;
            worker.ProgressMesage = message;
            db.SaveChanges();
        }

        /// <summary>
        /// Ajax call to Refresh All Workers in a specific cloud
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult RefreshAllWorkers(int id)
        {
            WorkerContainersManagement workerContainersManagement = new WorkerContainersManagement();

            workerContainersManagement.RefreshAllWorkers(id);

            return RedirectToAction("CloudDetails", "CloudManagement", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}