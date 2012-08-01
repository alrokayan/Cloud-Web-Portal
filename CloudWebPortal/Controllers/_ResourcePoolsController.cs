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
    public class _ResourcePoolsController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        /// <summary>
        /// WIDGET: List all resource pools and it’s machines
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult List()
        {
            //Get all the Resource Pools from the database
            //Pass them to the view
            return PartialView(db.ResourcePools.ToList());
        }

        /// <summary>
        /// WIDGET: Detailed information about a specific ResourcePool
        /// </summary>
        /// <param name="id">ResourcePool ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Pass to the view a list of Machine Login Credentials
            ViewBag.MachineLoginCredentials = db.MachineLoginCredentials.ToList();

            //Get the ResourcePool entity from the database using the given ID
            //Pass this entity to the view
            return PartialView(db.ResourcePools.Find(id));
        }

        /// <summary>
        /// WIDGET: A form to create a new ResourcePool
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create()
        {
            return PartialView();
        } 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourcepool"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult CreatePost(ResourcePool resourcepool)
        {
            if (ModelState.IsValid)
            {
                db.ResourcePools.Add(resourcepool);
                db.SaveChanges();
                return RedirectToAction("Dashboard", "Home");  
            }

            return View("~/Views/Home/Dashboard.cshtml", resourcepool);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific ResourcePool by getting it’s ID
        /// </summary>
        /// <param name="id">ResourcePool ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the ResourcePool entity from the database.
            ResourcePool resourcepool = db.ResourcePools.Find(id);
            //Pass the whole entity to the view so the user can edit it.
            return PartialView(resourcepool);
        }

        /// <summary>
        /// The post method to edit a specific ResourcePool
        /// </summary>
        /// <param name="resourcepool"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult EditPost(ResourcePool resourcepool)
        {
            if (ModelState.IsValid)
            {
                db.Entry(resourcepool).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Dashboard", "Home"); 
            }
            return View("~/Views/Home/Dashboard.cshtml", resourcepool);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a ResourcePool
        /// </summary>
        /// <param name="id">ResourcePool ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the ResourcePool entity from the database using the given ID
            ResourcePool resourcepool = db.ResourcePools.Find(id);
            //Pass this entity to the view
            return PartialView(resourcepool);
        }

        /// <summary>
        /// The post method to delete a specific ResourcePool
        /// </summary>
        /// <param name="ResourcePoolId"></param>
        /// <returns></returns>
        [HttpPost, ActionName("DeletePost")]
        [Authorize]
        public ActionResult DeleteConfirmed(int ResourcePoolId)
        {
            ResourcePool resourcepool = db.ResourcePools.Find(ResourcePoolId);

            if (ResourcePoolId == 1)
            {
                ModelState.AddModelError(String.Empty, "Sorry, you can't delete \"Default\" resource pool");
                return View("~/Views/Home/Dashboard.cshtml", resourcepool);
            }

            for(int x=0; x<resourcepool.Machines.Count(); x++)
            {
                for (int i = 0; i < resourcepool.Machines.ToList()[x].SoftwareAppliances.Count(); i++)
                    resourcepool.Machines.ToList()[x].SoftwareAppliances.Remove(resourcepool.Machines.ToList()[x].SoftwareAppliances.ToList()[i]);

                for (int i = 0; i < resourcepool.Machines.ToList()[x].Workers.Count(); i++)
                {
                    for (int j = 0; j < resourcepool.Machines.ToList()[x].Workers.ToList()[i].Services.Count(); j++)
                        resourcepool.Machines.ToList()[x].Workers.ToList()[i].Services.Remove(resourcepool.Machines.ToList()[x].Workers.ToList()[i].Services.ToList()[j]);
                    db.SaveChanges();

                    resourcepool.Machines.ToList()[x].Workers.Remove(resourcepool.Machines.ToList()[x].Workers.ToList()[i]);
                }
                db.SaveChanges();
                resourcepool.Machines.Remove(resourcepool.Machines.ToList()[x]);

                for (int i = 0; i < resourcepool.Machines.ToList()[x].Masters.Count(); i++)
                {
                    for (int j = 0; j < resourcepool.Machines.ToList()[x].Masters.ToList()[i].Services.Count(); j++)
                        resourcepool.Machines.ToList()[x].Masters.ToList()[i].Services.Remove(resourcepool.Machines.ToList()[x].Masters.ToList()[i].Services.ToList()[j]);
                    db.SaveChanges();

                    resourcepool.Machines.ToList()[x].Masters.Remove(resourcepool.Machines.ToList()[x].Masters.ToList()[i]);
                }
                db.SaveChanges();
                resourcepool.Machines.Remove(resourcepool.Machines.ToList()[x]);
            }
            db.SaveChanges();

            db.ResourcePools.Remove(resourcepool);
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