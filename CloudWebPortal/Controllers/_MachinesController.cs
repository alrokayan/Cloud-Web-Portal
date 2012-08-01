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
using Aneka;
using CloudWebPortal.Models;
using CloudWebPortal.Logic;
using Aneka.PAL.Management.Model;

namespace CloudWebPortal.Controllers
{ 
    public class _MachinesController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        public delegate void AsynchronousCallback(int machineID, string mapPath);
        public delegate void AsynchronousUpdateContainersCallback(int machineID);
        
        /// <summary>
        /// WIDGET: Detailed information about a specific Machine
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Get the Resource Pool name associated with this machine, and then pass it to the view
            ViewBag.ResourcePool = "Without Resource Pool";
            foreach (ResourcePool r in db.ResourcePools.ToList())
                if (r.Machines.Where(x => x.MachineId == id).Count() > 0)
                    ViewBag.ResourcePool = r.ResourcePoolDisplayName;

            //Get the Machine Login Credential associated with this machine, and then pass it to the view
            ViewBag.MachineLoginCredential = "Without Login Credential";
            foreach (MachineLoginCredential m in db.MachineLoginCredentials.ToList())
                if (m.Machines.Where(x => x.MachineId == id).Count() > 0)
                    ViewBag.MachineLoginCredential = m.Username;

            //Get the Machine entity from the database using the given ID
            CloudWebPortal.Models.Machine machine = db.Machines.Find(id);

            //Pass this entity to the view
            return PartialView(machine);
        }

        /// <summary>
        /// WIDGET: 
        /// </summary>
        /// <param name="id">ResourcePool ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create(int? id)
        {
            //Pass to the view a dropdown list of Machine Platforms.
            ViewBag.MachinePlatforms = new SelectList(db.MachinePlatforms.ToList(), "MachinePlatformId", "Platform", 1);
            //Pass to the view a dropdown list of Machine Types.
            ViewBag.MachineTypes = new SelectList(db.MachineTypes.ToList(), "MachineTypeId", "Type", 1);
            //Pass to the view a dropdown list of Resource Pools, if we passed the id it will selected as the default one.
            ViewBag.ResourcePools = new SelectList(db.ResourcePools.ToList(), "ResourcePoolId", "ResourcePoolDisplayName", id);
            //Pass to the view a dropdown list of Login Credentials.
            ViewBag.LoginCredentials = new SelectList(db.MachineLoginCredentials.ToList(), "MachineLoginCredentialId", "Username");
            //Pass to the view a Multi-Select list of Software Appliances.
            ViewBag.SoftwareAppliances = new MultiSelectList(db.SoftwareAppliances.ToList(), "SoftwareApplianceId", "Name");
            return PartialView();
        } 

        /// <summary>
        /// The post method to create a new Machine
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="daemon"></param>
        /// <param name="InstallDaemon"></param>
        /// <param name="ResourcePool"></param>
        /// <param name="LoginCredential"></param>
        /// <param name="PlatformID"></param>
        /// <param name="TypeID"></param>
        /// <param name="SoftwareAppliancesList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult CreatePost(CloudWebPortal.Models.Machine machine, Daemon daemon, bool InstallDaemon, int? ResourcePool, int? LoginCredential, int? PlatformID, int? TypeID, int?[] SoftwareAppliancesList)
        {
            MachinesManagement machineManagment = new MachinesManagement();
            DaemonsManagement daemonManagement = new DaemonsManagement();

            if (!ResourcePool.HasValue)
                ModelState.AddModelError(String.Empty, "Please Select A Resource Pool");

            if (!PlatformID.HasValue)
                ModelState.AddModelError(String.Empty, "Machine Platform Is Requared");
            else
                machine.Platform = db.MachinePlatforms.Find(PlatformID);

            if (!TypeID.HasValue)
                ModelState.AddModelError(String.Empty, "Machine Type Is Requared");
            else
                machine.Type = db.MachineTypes.Find(TypeID);

            if (ModelState.IsValid)
            {
                machine.StatusEnum = DaemonProbeStatus.Unknown;

                //Create Machine and Daemon
                machine.Daemon = daemon;
                db.Machines.Add(machine);
                db.SaveChanges();

                //Add To A Resource Pool
                if (ResourcePool.HasValue)
                {
                    db.ResourcePools.Find(ResourcePool).Machines.Add(machine);
                    db.SaveChanges();
                }

                //Link Login Credential
                MachineLoginCredential login = null;
                if (LoginCredential.HasValue)
                {
                    login = db.MachineLoginCredentials.Find(LoginCredential);
                    login.Machines.Add(machine);
                    db.SaveChanges();
                }

                //Add Software Appliances
                if (SoftwareAppliancesList != null)
                {
                    machine.SoftwareAppliances = new List<SoftwareAppliance>();
                    foreach (int appID in SoftwareAppliancesList)
                    {
                        var app = db.SoftwareAppliances.Find(appID);
                        machine.SoftwareAppliances.Add(app);
                    }
                    db.SaveChanges();
                }

                machine.StatusEnum = machineManagment.UpdateMachineStatus(machine.MachineId, login).Status;

                if (InstallDaemon && machine.StatusEnum == DaemonProbeStatus.ServiceNotInstalled && login != null)
                {
                    UpdateInProgressMessage(machine.MachineId, "Installing Daemon");

                    AsynchronousCallback installingDaemonThread = new AsynchronousCallback(daemonManagement.InstallDaemon);
                    installingDaemonThread.BeginInvoke(machine.MachineId, System.Web.HttpContext.Current.Server.MapPath("~/"), null, null);
                }
                else
                {
                    if (machine.StatusEnum == DaemonProbeStatus.ServiceStarted && login != null)
                    {
                        FetchExistCloud(machine.MachineId);
                    }
                }

                return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
            }

            return View("~/Views/Infrastructure/ManageMachinesandResourcePools.cshtml", machine);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific Machine by getting it’s ID
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the Machine entity from the database.
            CloudWebPortal.Models.Machine machine = db.Machines.Find(id);

            //Pass to the view a dropdown list of Machine Platforms.
            ViewBag.MachinePlatforms = new SelectList(db.MachinePlatforms.ToList(), "MachinePlatformId", "Platform", machine.Platform.MachinePlatformId);
            //Pass to the view a dropdown list of Machine Types.
            ViewBag.MachineTypes = new SelectList(db.MachineTypes.ToList(), "MachineTypeId", "Type", machine.Type.MachineTypeId);

            //Pass to the view a dropdown list of Resource Pools, if we passed the id it will selected as the default one.
            int ResourcePoolID = 0;
            foreach(ResourcePool r in db.ResourcePools.ToList())
                if( r.Machines.Where(x => x.MachineId == id).Count() > 0)
                    ResourcePoolID = r.ResourcePoolId;
            ViewBag.ResourcePools = new SelectList(db.ResourcePools.ToList(), "ResourcePoolId", "ResourcePoolDisplayName", ResourcePoolID);

            //Pass to the view a dropdown list of Login Credentials.
            int LoginCredentialId = 0;
            foreach (MachineLoginCredential m in db.MachineLoginCredentials.ToList())
                if (m.Machines.Where(x => x.MachineId == id).Count() > 0)
                    LoginCredentialId = m.MachineLoginCredentialId;
            ViewBag.LoginCredentials = new SelectList(db.MachineLoginCredentials.ToList(), "MachineLoginCredentialId", "Username", LoginCredentialId);

            //Pass to the view a Multi-Select list of Software Appliances.
            List<int> SoftwareAppliancesIds = new List<int>();
            foreach (SoftwareAppliance s in machine.SoftwareAppliances.ToList())
                SoftwareAppliancesIds.Add(s.SoftwareApplianceId);
            ViewBag.SoftwareAppliances = new MultiSelectList(db.SoftwareAppliances.ToList(), "SoftwareApplianceId", "Name", SoftwareAppliancesIds);

            //Pass the whole entity to the view so the user can edit it.
            return PartialView(machine);
        }

        /// <summary>
        /// The post method to edit a Machine
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="daemon"></param>
        /// <param name="ResourcePool"></param>
        /// <param name="LoginCredential"></param>
        /// <param name="PlatformID"></param>
        /// <param name="TypeID"></param>
        /// <param name="SoftwareAppliancesList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult EditPost(CloudWebPortal.Models.Machine machine, Daemon daemon, int? ResourcePool, int? LoginCredential, int? PlatformID, int? TypeID, int?[] SoftwareAppliancesList)
        {
            MachinesManagement machineManagment = new MachinesManagement();

            if(machine.StatusEnum == DaemonProbeStatus.ServiceStarted)
                ModelState.AddModelError(String.Empty, "Editing the machine while the daemon is running is not supported yet, please stop the daemon first");

            if (!ResourcePool.HasValue)
                ModelState.AddModelError(String.Empty, "Please Select A Resource Pool");
            if (!PlatformID.HasValue)
                ModelState.AddModelError(String.Empty, "Machine Platform Is Requared");
            if (!TypeID.HasValue)
                ModelState.AddModelError(String.Empty, "Machine Type Is Requared");

            if (ModelState.IsValid)
            {
                db.Entry(machine).State = EntityState.Modified;
                db.SaveChanges();

                machine.Daemon = daemon;
                db.SaveChanges();


                var machine2 = db.Machines.Include(x => x.Platform).Include(x => x.Type).Where(x => x.MachineId == machine.MachineId).First();
                if (machine2.Platform == null)
                    machine2.Platform = new MachinePlatform();
                if (machine2.Type == null)
                    machine2.Type = new MachineType();
                MachinePlatform platform = db.MachinePlatforms.Find(PlatformID);
                machine.Platform = platform;
                MachineType type = db.MachineTypes.Find(TypeID);
                machine.Type = type;
                db.SaveChanges();
                machine2 = null;

                //Resource Pool List
                var ResourcePoolsList = db.ResourcePools.ToList();
                for(int ResourcePoolIndex = 0; ResourcePoolIndex < ResourcePoolsList.Count(); ResourcePoolIndex++)
                    if (ResourcePoolsList[ResourcePoolIndex].Machines.Where(x => x.MachineId == machine.MachineId).Count() > 0)
                        db.ResourcePools.ToList()[ResourcePoolIndex].Machines.Remove(machine);
                db.SaveChanges();
                if (ResourcePool.HasValue)
                {
                    db.ResourcePools.Find(ResourcePool).Machines.Add(machine);
                    db.SaveChanges();
                }

                //Login List
                var MachineLoginCredentialList = db.MachineLoginCredentials.ToList();
                for(int MachineLoginCredentialIndex = 0; MachineLoginCredentialIndex < MachineLoginCredentialList.Count(); MachineLoginCredentialIndex++)
                    if (MachineLoginCredentialList[MachineLoginCredentialIndex].Machines.Where(x => x.MachineId == machine.MachineId).Count() > 0)
                        db.MachineLoginCredentials.ToList()[MachineLoginCredentialIndex].Machines.Remove(machine);
                db.SaveChanges();
                MachineLoginCredential login = null;
                if (LoginCredential.HasValue)
                {
                    login = db.MachineLoginCredentials.Find(LoginCredential);
                    login.Machines.Add(machine);
                    db.SaveChanges();
                }
                
                //SoftwareAppliances List
                machine2 = db.Machines.Include(x => x.SoftwareAppliances).Where(x => x.MachineId == machine.MachineId).First();

                if(machine2.SoftwareAppliances == null)
                    machine2.SoftwareAppliances = new List<SoftwareAppliance>();

                foreach(SoftwareAppliance app in db.SoftwareAppliances.ToList())
                    machine2.SoftwareAppliances.Remove(app);
                db.SaveChanges();

                if (SoftwareAppliancesList != null)
                {
                    foreach (int appID in SoftwareAppliancesList)
                    {
                        var app = db.SoftwareAppliances.Find(appID);
                        machine2.SoftwareAppliances.Add(app);
                    }
                    db.SaveChanges();
                }

                machineManagment.UpdateMachineStatus(machine.MachineId, login);

                return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
            }
            return View("~/Views/Infrastructure/ManageMachinesandResourcePools.cshtml", machine);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a Machine
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the Machine entity from the database using the given ID
            CloudWebPortal.Models.Machine machine = db.Machines.Find(id);
            //Pass this entity to the view
            return PartialView(machine);
        }

        /// <summary>
        /// The post method to delete a Machine
        /// </summary>
        /// <param name="MachineId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost, ActionName("DeletePost")]
        public ActionResult DeleteConfirmed(int MachineId)
        {
            CloudWebPortal.Models.Machine machine = db.Machines.Find(MachineId);

            for (int i = 0; i < machine.SoftwareAppliances.Count(); i++)
                machine.SoftwareAppliances.Remove(machine.SoftwareAppliances.ToList()[i]);

            for (int i = 0; i < machine.Workers.Count(); i++)
            {
                for (int j = 0; j < machine.Workers.ToList()[i].Services.Count(); j++)
                    machine.Workers.ToList()[i].Services.Remove(machine.Workers.ToList()[i].Services.ToList()[j]);
                db.SaveChanges();

                machine.Workers.Remove(machine.Workers.ToList()[i]);
            }

            for (int i = 0; i < machine.Masters.Count(); i++)
            {
                for (int j = 0; j < machine.Masters.ToList()[i].Services.Count(); j++)
                    machine.Masters.ToList()[i].Services.Remove(machine.Masters.ToList()[i].Services.ToList()[j]);
                db.SaveChanges();

                machine.Masters.Remove(machine.Masters.ToList()[i]);
            }

            db.SaveChanges();
            db.Machines.Remove(machine);
            db.SaveChanges();
            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        /// <summary>
        /// Ajax call to start a Daemon
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult StartDaemon(int id)
        {
            DaemonsManagement daemonManagement = new DaemonsManagement();

            UpdateInProgressMessage(id, "Starting Daemon");

            AsynchronousCallback DaemonThread = new AsynchronousCallback(daemonManagement.StartDaemon);
            DaemonThread.BeginInvoke(id, System.Web.HttpContext.Current.Server.MapPath("~/"), null, null);

            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        /// <summary>
        /// Ajax call to stop Daemon
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult StopDaemon(int id)
        {
            DaemonsManagement daemonManagement = new DaemonsManagement();

            UpdateInProgressMessage(id, "Stopping Daemon");

            AsynchronousCallback DaemonThread = new AsynchronousCallback(daemonManagement.StopDaemon);
            DaemonThread.BeginInvoke(id, System.Web.HttpContext.Current.Server.MapPath("~/"), null, null);

            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        /// <summary>
        /// Ajax call to uninstall a Daemon
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult UninstallDaemon(int id)
        {
            DaemonsManagement daemonManagement = new DaemonsManagement();

            UpdateInProgressMessage(id, "Uninstalling Daemon");

            AsynchronousCallback DaemonThread = new AsynchronousCallback(daemonManagement.UninstallDaemon);
            DaemonThread.BeginInvoke(id, System.Web.HttpContext.Current.Server.MapPath("~/"), null, null);

            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        /// <summary>
        /// Ajax call to Install a Daemon
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult InstallDaemon(int id)
        {
            DaemonsManagement daemonManagement = new DaemonsManagement();

            UpdateInProgressMessage(id, "Installing Daemon");

            AsynchronousCallback DaemonThread = new AsynchronousCallback(daemonManagement.InstallDaemon);
            DaemonThread.BeginInvoke(id, System.Web.HttpContext.Current.Server.MapPath("~/"), null, null);

            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        /// <summary>
        /// Ajax call to restart a Daemon
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult RestartDaemon(int id)
        {
            DaemonsManagement daemonManagement = new DaemonsManagement();

            UpdateInProgressMessage(id, "Restarting Daemon");

            AsynchronousCallback DaemonThread = new AsynchronousCallback(daemonManagement.RestartDaemon);
            DaemonThread.BeginInvoke(id, System.Web.HttpContext.Current.Server.MapPath("~/"), null, null);

            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        /// <summary>
        /// Ajax call to fetch exist cloud
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult FetchExistCloud(int id)
        {
            DaemonsManagement daemonManagement = new DaemonsManagement();

            UpdateInProgressMessage(id, "Fetching Exist Cloud");

            AsynchronousUpdateContainersCallback installingDaemonThread = new AsynchronousUpdateContainersCallback(daemonManagement.UpdateContainers);
            installingDaemonThread.BeginInvoke(id, null, null);

            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        /// <summary>
        /// Ajax call to refresh machine status
        /// </summary>
        /// <param name="id">Machine ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult RefreshMachine(int id)
        {
            MachinesManagement machineManagment = new MachinesManagement();

            CloudWebPortal.Models.Machine machine = db.Machines.Find(id);
            machineManagment.UpdateMachineStatus(machine.MachineId, machineManagment.getLogin(machine));
            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }
        
        /// <summary>
        /// Ajax call to Refresh All Machines In a specific Resource Pool
        /// </summary>
        /// <param name="id">Resource Pool ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult RefreshAllMachineInResourcePool(int id)
        {
            MachinesManagement machineManagment = new MachinesManagement();

            List<CloudWebPortal.Models.Machine> machineList = db.ResourcePools.Find(id).Machines.ToList();
            foreach (var machine in machineList)
                machineManagment.UpdateMachineStatus(machine.MachineId, machineManagment.getLogin(machine));
            return RedirectToAction("ManageMachinesandResourcePools", "Infrastructure");
        }

        private void UpdateInProgressMessage(int machineID, string message)
        {
            CloudWebPortal.Models.Machine machine = db.Machines.Find(machineID);
            machine.isInProgress = true;
            machine.ProgressMesage = message;
            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}