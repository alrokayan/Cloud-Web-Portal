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
    public class _CloudUsersController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();


        /// <summary>
        /// WIDGET: A simple List of all Cloud User Accounts
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult List()
        {
            //Get all the CloudUserAccounts and pass them to the view
            return PartialView(db.CloudUserAccounts.ToList());
        }

        /// <summary>
        /// WIDGET: A detailed information ab
        /// </summary>
        /// <param name="id">Cloud ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Get the CloudUserAccount entity from the database using the given ID
            CloudUserAccount clouduseraccount = db.CloudUserAccounts.Find(id);

            //Pass this entity to the view
            return PartialView(clouduseraccount);
        }

        /// <summary>
        /// WIDGET: A form to create a new CloudUserAccount
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create()
        {
            //Pass a list of clouds as a multi select list to the view.
            ViewBag.Clouds = new MultiSelectList(db.Clouds.ToList(), "CloudId", "CloudName");
            return PartialView();
        } 

        /// <summary>
        /// This is the post action t create a new CloudUserAccount
        /// </summary>
        /// <param name="clouduseraccount"></param>
        /// <param name="useThisAccountForReporting"></param>
        /// <param name="CloudsList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult CreatePost(CloudUserAccount clouduseraccount, bool useThisAccountForReporting, int?[] CloudsList)
        {
            if (ModelState.IsValid)
            {
                clouduseraccount.useThisAccountForReporting = useThisAccountForReporting;
                db.CloudUserAccounts.Add(clouduseraccount);
                db.SaveChanges();

                if (CloudsList != null)
                {
                    clouduseraccount.Clouds = new List<Cloud>();
                    foreach(var cloudID in CloudsList)
                    {
                        try
                        {
                            var cloud = db.Clouds.Find(cloudID);

                            //Save the user to the cloud
                            ContainersManagement containersManagement = new ContainersManagement();
                            Machine machine = containersManagement.machineLookupFromMasterId(cloud.Master.MasterId);
                            Uri masterUri = new Uri(ContainersManagement.GetContainerUri(machine.IP, cloud.Master.Port));
                            Aneka.Security.UserCredentials adminLogin = new Aneka.Security.UserCredentials("Administrator", String.Empty);
                            AnekaUsersManagement anekaUsersManagement = new AnekaUsersManagement(masterUri, adminLogin);
                            anekaUsersManagement.createNewUser(clouduseraccount, cloud.CloudId);

                            //clouduseraccount.Clouds.Add(cloud);
                            //db.SaveChanges();
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    
                }

                return RedirectToAction("Dashboard", "Home");
            }

            return View("~/Views/Home/Dashboard.cshtml",clouduseraccount);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific CloudUserAccount by getting it’s ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the CloudUserAccount entity from the database.
            CloudUserAccount clouduseraccount = db.CloudUserAccounts.Find(id);

            //Send a list of selected clouds as a multi select list to the view.
            List<int> CloudIDs = new List<int>();
            foreach (var s in clouduseraccount.Clouds.ToList())
                CloudIDs.Add(s.CloudId);
            ViewBag.Clouds = new MultiSelectList(db.Clouds.ToList(), "CloudId", "CloudName", CloudIDs);

            //Pass the whole entity to the view so the user can edit it.
            return View(clouduseraccount);
        }

        /// <summary>
        /// This is the post method to edit a specific CloudUserAccount
        /// </summary>
        /// <param name="clouduseraccount"></param>
        /// <param name="useThisAccountForReporting"></param>
        /// <param name="CloudsList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult EditPost(CloudUserAccount clouduseraccount,bool useThisAccountForReporting, int?[] CloudsList)
        {
            if (ModelState.IsValid)
            {
                clouduseraccount.useThisAccountForReporting = useThisAccountForReporting;
                db.Entry(clouduseraccount).State = EntityState.Modified;
                db.SaveChanges();

                //Clouds List
                var clouduseraccount2 = db.CloudUserAccounts.Include(x => x.Clouds).Where(x => x.CloudUserAccountId == clouduseraccount.CloudUserAccountId).First();

                if (clouduseraccount2.Clouds == null)
                    clouduseraccount2.Clouds = new List<Cloud>();

                foreach (var user in db.Clouds.ToList())
                    clouduseraccount2.Clouds.Remove(user);
                db.SaveChanges();

                if (CloudsList != null)
                {
                    foreach (int cloudID in CloudsList)
                    {
                        var user = db.Clouds.Find(cloudID);
                        clouduseraccount2.Clouds.Add(user);
                    }
                    db.SaveChanges();
                }

                return RedirectToAction("Dashboard", "Home");
            }
            return View("~/Views/Home/Dashboard.cshtml",clouduseraccount);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a CloudUserAccount
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the CloudUserAccount entity from the database using the given ID
            CloudUserAccount clouduseraccount = db.CloudUserAccounts.Find(id);

            //Pass this entity to the view
            return View(clouduseraccount);
        }

        /// <summary>
        /// This is the post action to delete a CloudUserAccount
        /// </summary>
        /// <param name="CloudUserAccountId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost, ActionName("DeletePost")]
        public ActionResult DeleteConfirmed(int CloudUserAccountId)
        {
            CloudUserAccount clouduseraccount = db.CloudUserAccounts.Find(CloudUserAccountId);
            db.CloudUserAccounts.Remove(clouduseraccount);
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