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
    public class _MachinesLoginCredentialsController : Controller
    {
        private AnekaDbContext db = new AnekaDbContext();

        /// <summary>
        /// WIDGET: List all Machine Login Credentials in table
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult List()
        {
            //Get all MachineLoginCredentials entities and pass them to the view
            return PartialView(db.MachineLoginCredentials.ToList());
        }

        /// <summary>
        /// WIDGET: Detailed information about a specific MachineLoginCredential
        /// </summary>
        /// <param name="id">MachineLoginCredential ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Get the MachineLoginCredential entity from the database using the given ID
            //Pass this entity to the view
            return PartialView(db.MachineLoginCredentials.Find(id));
        }

        /// <summary>
        /// WIDGET: A form to create a new MachineLoginCredential
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create()
        {
            //Pass to the view a dropdown list  of Machines
            ViewBag.Machines = new SelectList(db.Machines.ToList(), "MachineId", "DisplayName");
            return PartialView();
        } 

        /// <summary>
        /// The post method to create a new MachineLoginCredential
        /// </summary>
        /// <param name="machinelogincredential"></param>
        /// <param name="MachinesList"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult CreatePost(MachineLoginCredential machinelogincredential, int?[] MachinesList)
        {
            MachinesManagement machineManagment = new MachinesManagement();

            if (ModelState.IsValid)
            {
                db.MachineLoginCredentials.Add(machinelogincredential);
                db.SaveChanges();

                if (MachinesList != null)
                {
                    machinelogincredential.Machines = new List<Machine>();
                    foreach (int machineID in MachinesList)
                    {
                        var machine = db.Machines.Find(machineID);
                        //ignore modifying the machine if the daemon is running
                        if (machine.StatusEnum != global::Aneka.PAL.Management.Model.DaemonProbeStatus.ServiceStarted)
                        {
                            machineManagment.UpdateMachineStatus((int)machineID, machinelogincredential);
                            machinelogincredential.Machines.Add(machine);
                        }
                        
                    }
                    db.SaveChanges();
                }

                return RedirectToAction("Dashboard", "Home"); 
            }

            return View("~/Views/Home/Dashboard.cshtml", machinelogincredential);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific MachineLoginCredential by getting it’s ID
        /// </summary>
        /// <param name="id">MachineLoginCredential ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the MachineLoginCredential entity from the database.
            MachineLoginCredential machineLoginCredential = db.MachineLoginCredentials.Find(id);

            //Pass to the view a dropdown list of selected Machines
            List<int> MachinesIds = new List<int>();
            foreach (Machine m in machineLoginCredential.Machines.ToList())
                MachinesIds.Add(m.MachineId);
            ViewBag.Machines = new MultiSelectList(db.Machines.ToList(), "MachineId", "DisplayName", MachinesIds);

            //Pass the whole entity to the view so the user can edit it.
            return PartialView(machineLoginCredential);
        }

        /// <summary>
        /// The post method to edit a MachineLoginCredential
        /// </summary>
        /// <param name="machinelogincredential"></param>
        /// <param name="MachinesList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult EditPost(MachineLoginCredential machinelogincredential, int?[] MachinesList)
        {
            MachinesManagement machineManagment = new MachinesManagement();

            if (ModelState.IsValid)
            {

                db.Entry(machinelogincredential).State = EntityState.Modified;
                db.SaveChanges();


                //Machines List
                var machinelogincredential2 = db.MachineLoginCredentials.Include(x => x.Machines).Where(x => x.MachineLoginCredentialId == machinelogincredential.MachineLoginCredentialId).First();

                if (machinelogincredential2.Machines == null)
                    machinelogincredential2.Machines = new List<Machine>();

                foreach (Machine m in db.Machines.ToList())
                    machinelogincredential2.Machines.Remove(m);
                db.SaveChanges();

                if (MachinesList != null)
                {
                    foreach (int machineID in MachinesList)
                    {
                        var machine = db.Machines.Find(machineID);
                        //ignore modifying the machine if the daemon is running
                        if (machine.StatusEnum != global::Aneka.PAL.Management.Model.DaemonProbeStatus.ServiceStarted)
                        {
                            machineManagment.UpdateMachineStatus((int)machineID, machinelogincredential);
                            machinelogincredential.Machines.Add(machine);
                        }
                    }
                    db.SaveChanges();
                }

                return RedirectToAction("Dashboard", "Home");
            }
            return View("~/Views/Home/Dashboard.cshtml",machinelogincredential);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a MachineLoginCredential
        /// </summary>
        /// <param name="id">MachineLoginCredential ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the MachineLoginCredential entity from the database using the given ID
            //Pass this entity to the view
            return PartialView(db.MachineLoginCredentials.Find(id));
        }

        /// <summary>
        /// The post method to delete a MachineLoginCredential
        /// </summary>
        /// <param name="MachineLoginCredentialId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost, ActionName("DeletePost")]
        public ActionResult DeleteConfirmed(int MachineLoginCredentialId)
        {
            MachineLoginCredential machinelogincredential = db.MachineLoginCredentials.Find(MachineLoginCredentialId);

            for(int i=0; i<machinelogincredential.Machines.Count(); i++)
                machinelogincredential.Machines.Remove(machinelogincredential.Machines.ToList()[i]);
            db.SaveChanges();

            db.MachineLoginCredentials.Remove(machinelogincredential);
            db.SaveChanges();
            return RedirectToAction("Dashboard", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}