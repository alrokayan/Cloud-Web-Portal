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
using CloudWebPortal.Areas.Aneka.Models;

namespace CloudWebPortal.Areas.Aneka.Controllers
{ 
    public class _ServicesController : Controller
    {
        private AnekaDbContext db = new AnekaDbContext();

        /// <summary>
        /// WIDGET: A table of services with the ability to edit or delete each Service
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult List()
        {
            //Get and pass to the view all Services
            return PartialView(db.Services.ToList());
        }

        /// <summary>
        /// WIDGET: Detailed information about a specific Service
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Get the Service entity from the database using the given ID
            Service service = db.Services.Find(id);
            //Pass this entity to the view
            return PartialView(service);
        }

        /// <summary>
        /// WIDGET: A form to create a new Service
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create()
        {
            //Pass to the view a dropdown list of workers
            ViewBag.Workers = new SelectList(db.Workers.ToList(), "WorkerId", "DisplayName");
            
            //Pass to the view a dropdown list of masters
            ViewBag.Masters = new SelectList(db.Masters.ToList(), "MasterId", "DisplayName");
            
            return PartialView();
        } 

        /// <summary>
        /// The post method to create a Service
        /// </summary>
        /// <param name="service"></param>
        /// <param name="WorkersList"></param>
        /// <param name="MastersList"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult CreatePost(Service service, int?[] WorkersList, int?[] MastersList)
        {
            if (ModelState.IsValid)
            {
                db.Services.Add(service);
                db.SaveChanges();

                if (WorkersList != null)
                {
                    service.Workers = new List<Worker>();
                    foreach (var workerID in WorkersList)
                        service.Workers.Add(db.Workers.Find(workerID));
                    db.SaveChanges();
                }

                if (MastersList != null)
                {
                    service.Masters = new List<Master>();
                    foreach (var masterID in MastersList)
                        service.Masters.Add(db.Masters.Find(masterID));
                    db.SaveChanges();
                }

                return RedirectToAction("Dashboard", "Home");
            }

            return View("~/Views/Home/Dashboard.cshtml", service);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific Service by getting it’s ID
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the Service entity from the database.
            Service service = db.Services.Find(id);

            //Pass to the view a dropdown list of selected worker.
            List<int> WorkersIds = new List<int>();
            foreach (Worker w in service.Workers.ToList())
                WorkersIds.Add(w.WorkerId);
            ViewBag.Workers = new MultiSelectList(db.Workers.ToList(), "WorkerId", "DisplayName", WorkersIds);

            //Pass to the view a dropdown list of selected masters.
            List<int> MastersIds = new List<int>();
            foreach (Master m in service.Masters.ToList())
                MastersIds.Add(m.MasterId);
            ViewBag.Masters = new MultiSelectList(db.Masters.ToList(), "MasterId", "DisplayName", MastersIds);

            //Pass the whole entity to the view so the user can edit it.
            return PartialView(service);
        }

        /// <summary>
        /// The post method to edit a specific Service
        /// </summary>
        /// <param name="service"></param>
        /// <param name="WorkersList"></param>
        /// <param name="MastersList"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult EditPost(Service service, int?[] WorkersList, int?[] MastersList)
        {
            if (ModelState.IsValid)
            {
                db.Entry(service).State = EntityState.Modified;
                db.SaveChanges();

                //To BE Implemented: Edit WorkersList & MastersList

                return RedirectToAction("Dashboard", "Home");
            }
            return View("~/Views/Home/Dashboard.cshtml", service);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a Service
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the Service entity from the database using the given ID
            Service service = db.Services.Find(id);
            //Pass this entity to the view
            return PartialView(service);
        }

        /// <summary>
        /// The post method to delete a specific Service
        /// </summary>
        /// <param name="ServiceId"></param>
        /// <returns></returns>
        [HttpPost, ActionName("DeletePost")]
        [Authorize]
        public ActionResult DeleteConfirmed(int ServiceId)
        {
            Service service = db.Services.Find(ServiceId);

            for (int i = 0; i < service.Workers.Count(); i++)
                service.Workers.Remove(service.Workers.ToList()[i]);
            db.SaveChanges();

            for (int i = 0; i < service.Masters.Count(); i++)
                service.Masters.Remove(service.Masters.ToList()[i]);
            db.SaveChanges();

            db.Services.Remove(service);
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