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
using System.Data.Entity;
using Aneka.PAL.Management.Model;

namespace CloudWebPortal.Models
{
    public class CloudWebPortalDbContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, add the following
        // code to the Application_Start method in your Global.asax file.
        // Note: this will destroy and re-create your database with every model change.
        // 
        // System.Data.Entity.Database.SetInitializer(new System.Data.Entity.DropCreateDatabaseIfModelChanges<CloudWebPortal.Models.WebPortalManagementContext>());

        public DbSet<WebPortalLoginCredential> WebPortalLoginCredentials { get; set; }
        public DbSet<Widget> Widgets { get; set; }
    }

    public class CloudWebPortalDbInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<CloudWebPortalDbContext>
    {
        protected override void Seed(CloudWebPortalDbContext context)
        {
            //MUST BE INSERTED --- START --
            context.WebPortalLoginCredentials.Add(new WebPortalLoginCredential { Username = "admin",      Password = "admin",     Widgets = new List<Widget>()});
            context.SaveChanges();
            //MUST BE INSERTED --- END --
            

            //The following is just for test ..
            /*
            var Widgets = new List<Widget>
            {
                new Widget { ControllerName = "_PortalUsers",   ActionName = "Create"},
                new Widget { ControllerName = "_PortalUsers",   ActionName = "Details",    ActionId = 1},
                new Widget { ControllerName = "_PortalUsers",   ActionName = "Create" },
                new Widget { ControllerName = "_PortalUsers",   ActionName = "Details",    ActionId = 1},
                new Widget { ControllerName = "_PortalUsers",   ActionName = "Create" },
                new Widget { ControllerName = "_PortalUsers",   ActionName = "Details",    ActionId = 1},
                new Widget { ControllerName = "_PortalUsers",   ActionName = "Create" }
            };
            Widgets.ForEach(s => context.Widgets.Add(s));
            context.SaveChanges();

            WebPortalLoginCredentials[0].Widgets.Add(Widgets[0]);
            WebPortalLoginCredentials[0].Widgets.Add(Widgets[1]);
            WebPortalLoginCredentials[1].Widgets.Add(Widgets[2]);
            WebPortalLoginCredentials[1].Widgets.Add(Widgets[3]);
            WebPortalLoginCredentials[2].Widgets.Add(Widgets[4]);
            WebPortalLoginCredentials[3].Widgets.Add(Widgets[5]);
            WebPortalLoginCredentials[4].Widgets.Add(Widgets[6]);
            context.SaveChanges();
            */

            base.Seed(context);
        }
    }
}
