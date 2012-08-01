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

namespace CloudWebPortal.Controllers
{ 
    public class _SoftwareAppliancesController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        /// <summary>
        /// WIDGET: A table of software appliances with the ability to edit or delete each Service
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult List()
        {
            //Get and pass to the view all software appliances
            return PartialView(db.SoftwareAppliances.ToList());
        }

        /// <summary>
        /// WIDGET: Detailed information about a specific SoftwareAppliance
        /// </summary>
        /// <param name="id">SoftwareAppliance ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Get the SoftwareAppliance entity from the database using the given ID
            SoftwareAppliance softwareappliance = db.SoftwareAppliances.Find(id);
            //Pass this entity to the view
            return PartialView(softwareappliance);
        }

        /// <summary>
        /// WIDGET: A form to create a new SoftwareAppliance
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create()
        {
            //Pass to the view a dropdown list of Machines
            ViewBag.Machines = new SelectList(db.Machines.ToList(), "MachineId", "DisplayName");
            return PartialView();
        } 

        /// <summary>
        /// The post method to create new SoftwareAppliance
        /// </summary>
        /// <param name="softwareappliance"></param>
        /// <param name="MachinesList"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult CreatePost(SoftwareAppliance softwareappliance, int?[] MachinesList)
        {
            if (ModelState.IsValid)
            {
                db.SoftwareAppliances.Add(softwareappliance);
                db.SaveChanges();

                if (MachinesList != null)
                {
                    softwareappliance.Machines = new List<Machine>();
                    foreach (var machine in MachinesList)
                        softwareappliance.Machines.Add(db.Machines.Find(machine));
                    db.SaveChanges();
                }

                return RedirectToAction("Dashboard", "Home");  
            }

            return View("~/Views/Home/Dashboard.cshtml", softwareappliance);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific SoftwareAppliance by getting it’s ID
        /// </summary>
        /// <param name="id">SoftwareAppliance ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the SoftwareAppliance entity from the database.
            SoftwareAppliance softwareappliance = db.SoftwareAppliances.Find(id);

            //Pass to the view a dropdown list of selected Machines
            List<int> MachinesIds = new List<int>();
            foreach (Machine m in softwareappliance.Machines.ToList())
                MachinesIds.Add(m.MachineId);
            ViewBag.Machines = new MultiSelectList(db.Machines.ToList(), "MachineId", "DisplayName", MachinesIds);

            //Pass the whole entity to the view so the user can edit it.
            return PartialView(softwareappliance);
        }

        /// <summary>
        /// This is the post method to edit a specific SoftwareAppliance
        /// </summary>
        /// <param name="softwareappliance"></param>
        /// <param name="MachinesList"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult EditPost(SoftwareAppliance softwareappliance, int?[] MachinesList)
        {
            if (ModelState.IsValid)
            {
                db.Entry(softwareappliance).State = EntityState.Modified;
                db.SaveChanges();

                //Machines List
                SoftwareAppliance softwareapplianceFromDB = db.SoftwareAppliances.Include(x => x.Machines).Where(x => x.SoftwareApplianceId == softwareappliance.SoftwareApplianceId).First();

                if (softwareapplianceFromDB.Machines == null)
                    softwareapplianceFromDB.Machines = new List<Machine>();
                else
                {
                    foreach (Machine m in db.Machines.ToList())
                        softwareapplianceFromDB.Machines.Remove(m);
                    db.SaveChanges();
                }

                if (MachinesList != null)
                {
                    foreach (int machineID in MachinesList)
                    {
                        var machine = db.Machines.Find(machineID);
                        softwareapplianceFromDB.Machines.Add(machine);
                    }
                    db.SaveChanges();
                }

                return RedirectToAction("Dashboard", "Home");
            }
            return View("~/Views/Home/Dashboard.cshtml", softwareappliance);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a SoftwareAppliance
        /// </summary>
        /// <param name="id">SoftwareAppliance ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the SoftwareAppliance entity from the database using the given ID
            SoftwareAppliance softwareappliance = db.SoftwareAppliances.Find(id);
            //Pass this entity to the view
            return PartialView(softwareappliance);
        }

        /// <summary>
        /// This is the post method to delete a specific SoftwareAppliance
        /// </summary>
        /// <param name="SoftwareApplianceId"></param>
        /// <returns></returns>
        [HttpPost, ActionName("DeletePost")]
        [Authorize]
        public ActionResult DeleteConfirmed(int SoftwareApplianceId)
        {
            SoftwareAppliance softwareappliance = db.SoftwareAppliances.Find(SoftwareApplianceId);

            for (int i = 0; i < softwareappliance.Machines.Count(); i++)
                softwareappliance.Machines.Remove(softwareappliance.Machines.ToList()[i]);
            db.SaveChanges();

            db.SoftwareAppliances.Remove(softwareappliance);
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