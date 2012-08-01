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
    public class _PortalUsersController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        /// <summary>
        /// WIDGET: A form to create a WebPortalLoginCredential
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult List()
        {
            //Get all the WebPortalLoginCredential entities from the database, and then pass them to the view
            return PartialView(db.WebPortalLoginCredentials.ToList());
        }

        /// <summary>
        /// WIDGET: Detailed information about a specific WebPortalLoginCredential
        /// </summary>
        /// <param name="id">WebPortalLoginCredential ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Details(int id)
        {
            //Get the WebPortalLoginCredential entity from the database using the given ID
            WebPortalLoginCredential webportallogincredential = db.WebPortalLoginCredentials.Find(id);
            //Pass this entity to the view
            return PartialView(webportallogincredential);
        }

        /// <summary>
        /// WIDGET: A form to create a new WebPortalLoginCredential
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create()
        {
            return PartialView();
        } 

        /// <summary>
        /// A post method to create a new WebPortalLoginCredential
        /// </summary>
        /// <param name="webportallogincredential"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult CreatePost(WebPortalLoginCredential webportallogincredential)
        {
            if (ModelState.IsValid)
            {
                db.WebPortalLoginCredentials.Add(webportallogincredential);
                db.SaveChanges();
                return RedirectToAction("Dashboard", "Home");
            }

            return View("~/Views/Home/Dashboard.cshtml", webportallogincredential);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific WebPortalLoginCredential by getting it’s ID
        /// </summary>
        /// <param name="id">WebPortalLoginCredential ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the WebPortalLoginCredential entity from the database.
            WebPortalLoginCredential webportallogincredential = db.WebPortalLoginCredentials.Find(id);

            //Pass the whole entity to the view so the user can edit it.
            return PartialView(webportallogincredential);
        }

        /// <summary>
        /// The post method to edit a specific WebPortalLoginCredential
        /// </summary>
        /// <param name="webportallogincredential"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult EditPost(WebPortalLoginCredential webportallogincredential)
        {
            if (ModelState.IsValid)
            {
                db.Entry(webportallogincredential).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Dashboard", "Home");
            }

            return View("~/Views/Home/Dashboard.cshtml", webportallogincredential);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a WebPortalLoginCredential
        /// </summary>
        /// <param name="id">WebPortalLoginCredential ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the WebPortalLoginCredential entity from the database using the given ID
            WebPortalLoginCredential webportallogincredential = db.WebPortalLoginCredentials.Find(id);
            //Pass this entity to the view
            return PartialView(webportallogincredential);
        }

        /// <summary>
        /// The post method to delete a specific WebPortalLoginCredential
        /// </summary>
        /// <param name="WebPortalLoginCredentialId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost, ActionName("DeletePost")]
        public ActionResult DeleteConfirmed(int WebPortalLoginCredentialId)
        {
            //Remove all widgets
            var Widgets = db.WebPortalLoginCredentials.Find(WebPortalLoginCredentialId).Widgets.ToList();

            foreach (var w in Widgets)
                db.Widgets.Remove(w);

            //Then remove the user
            WebPortalLoginCredential webportallogincredential = db.WebPortalLoginCredentials.Find(WebPortalLoginCredentialId);
            db.WebPortalLoginCredentials.Remove(webportallogincredential);
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