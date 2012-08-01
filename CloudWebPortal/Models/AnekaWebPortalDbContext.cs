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
        public DbSet<MachineLoginCredential> MachineLoginCredentials { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<SoftwareAppliance> SoftwareAppliances { get; set; }
        public DbSet<CloudWebPortal.Models.Machine> Machines { get; set; }
        public DbSet<Master> Masters { get; set; }
        public DbSet<Worker> Workers { get; set; }
        public DbSet<CloudUserAccount> CloudUserAccounts { get; set; }
        public DbSet<Cloud> Clouds { get; set; }
        public DbSet<Daemon> Daemons { get; set; }
        public DbSet<ResourcePool> ResourcePools { get; set; }
        public DbSet<Widget> Widgets { get; set; }
        public DbSet<MachineType> MachineTypes { get; set; }
        public DbSet<MachinePlatform> MachinePlatforms { get; set; }
    }

    public class CloudWebPortalDbInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<CloudWebPortalDbContext>
    {
        protected override void Seed(CloudWebPortalDbContext context)
        {
            //MUST BE INSERTED --- START --
            var MachineTypes = new List<MachineType>
            {
                new MachineType { Type="PC"},
                new MachineType { Type="Virtual Machine"},
                new MachineType { Type="EC2"},
                new MachineType { Type="Azure"}
            };
            MachineTypes.ForEach(s => context.MachineTypes.Add(s));
            context.SaveChanges();

            var MachinePlatforms = new List<MachinePlatform>
            {
                new MachinePlatform { Platform="Windows"},
                new MachinePlatform { Platform="Linux"}
            };
            MachinePlatforms.ForEach(s => context.MachinePlatforms.Add(s));
            context.SaveChanges();

            context.WebPortalLoginCredentials.Add(new WebPortalLoginCredential { Username = "admin",      Password = "admin",     Widgets = new List<Widget>()});
            context.SaveChanges();

            var Services = new List<Service>
            {
                new Service { Name="Logging Service", SpringSegmentXML="<HardCoded>LoggingService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=false},
                new Service { Name="Monitoring Service", SpringSegmentXML="<HardCoded>MonitoringService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=false},
                new Service { Name="Discovery Service", SpringSegmentXML="<HardCoded>DiscoveryService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=false},
                new Service { Name="Task Execution Service", SpringSegmentXML="<HardCoded>TaskExecutionService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=false},
                new Service { Name="Thread Execution Service", SpringSegmentXML="<HardCoded>ThreadExecutionService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=false},
                new Service { Name="Map Reduce Executor Service", SpringSegmentXML="<HardCoded>MapReduceExecutor</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=false},

                //Master Only
                new Service { Name="Reporting Service", SpringSegmentXML="<HardCoded>ReportingService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=true},
                new Service { Name="Resource Provisioning Service", SpringSegmentXML="<HardCoded>ResourceProvisioningService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=true},
                new Service { Name="Task Scheduler Service", SpringSegmentXML="<HardCoded>TaskScheduler</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=true},
                new Service { Name="Thread Scheduler Service", SpringSegmentXML="<HardCoded>ThreadScheduler</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=true},
                new Service { Name="Map Reduce Scheduler Service", SpringSegmentXML="<HardCoded>MapReduceScheduler</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=true},
                new Service { Name="Storage Service", SpringSegmentXML="<HardCoded>StorageService</HardCoded>", Workers = new List<Worker>(), Masters = new List<Master>(), isMasterOnly=true}
            };
            Services.ForEach(s => context.Services.Add(s));
            context.SaveChanges();

            context.ResourcePools.Add(new ResourcePool { ResourcePoolDisplayName = "Default", Machines = new List<CloudWebPortal.Models.Machine>() });
            context.SaveChanges();
            //MUST BE INSERTED --- END --
            

            //The following is just for test ..
            /*
            var MachineLoginCredentials = new List<MachineLoginCredential>
            {
                new MachineLoginCredential{ Username= "gridadm", Password="xxxxx", Machines = new List<CloudWebPortal.Models.Machine>()},
                new MachineLoginCredential{ Username= "alrokayan", Password="xxxxx", Machines = new List<CloudWebPortal.Models.Machine>()}
            };
            MachineLoginCredentials.ForEach(s => context.MachineLoginCredentials.Add(s));
            context.SaveChanges();

            
            var WebPortalLoginCredentials = new List<WebPortalLoginCredential>
            {
                new WebPortalLoginCredential { Username = "alrokayan",  Password = "alrokayan", Widgets = new List<Widget>()},
                new WebPortalLoginCredential { Username = "all",        Password = "all",       Widgets = new List<Widget>()},
                new WebPortalLoginCredential { Username = "AnekaSU",    Password = "AnekaSU",   Widgets = new List<Widget>()},
                new WebPortalLoginCredential { Username = "NewUser",    Password = "NewUser",   Widgets = new List<Widget>()},
                new WebPortalLoginCredential { Username = "su",         Password = "su",        Widgets = new List<Widget>()}
            };
            WebPortalLoginCredentials.ForEach(s => context.WebPortalLoginCredentials.Add(s));
            context.SaveChanges();

            //var Widgets = new List<Widget>
            //{
            //    new Widget { ControllerName = "_PortalUsers",   ActionName = "Create"},
            //    new Widget { ControllerName = "_PortalUsers",   ActionName = "Details",    ActionId = 1},
            //    new Widget { ControllerName = "_PortalUsers",   ActionName = "Create" },
            //    new Widget { ControllerName = "_PortalUsers",   ActionName = "Details",    ActionId = 1},
            //    new Widget { ControllerName = "_PortalUsers",   ActionName = "Create" },
            //    new Widget { ControllerName = "_PortalUsers",   ActionName = "Details",    ActionId = 1},
            //    new Widget { ControllerName = "_PortalUsers",   ActionName = "Create" }
            //};
            //Widgets.ForEach(s => context.Widgets.Add(s));
            //context.SaveChanges();

            //WebPortalLoginCredentials[0].Widgets.Add(Widgets[0]);
            //WebPortalLoginCredentials[0].Widgets.Add(Widgets[1]);
            //WebPortalLoginCredentials[1].Widgets.Add(Widgets[2]);
            //WebPortalLoginCredentials[1].Widgets.Add(Widgets[3]);
            //WebPortalLoginCredentials[2].Widgets.Add(Widgets[4]);
            //WebPortalLoginCredentials[3].Widgets.Add(Widgets[5]);
            //WebPortalLoginCredentials[4].Widgets.Add(Widgets[6]);
            //context.SaveChanges();

            var Machines = new List<CloudWebPortal.Models.Machine>
            {
                new CloudWebPortal.Models.Machine { DisplayName="UniMelb Manjra Server", IP = "192.168.1.100",  Platform=MachinePlatforms[0],   Type=MachineTypes[0],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()},
                new CloudWebPortal.Models.Machine { DisplayName="UniMelb Lab PC10",      IP = "192.168.1.159",  Platform=MachinePlatforms[1],   Type=MachineTypes[1],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()},
                new CloudWebPortal.Models.Machine { DisplayName="UniMelb Lab PC11",      IP = "192.168.1.101",  Platform=MachinePlatforms[0],   Type=MachineTypes[1],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()},
                new CloudWebPortal.Models.Machine { DisplayName="UniMelb Lab PC12",      IP = "192.168.1.120",  Platform=MachinePlatforms[0],   Type=MachineTypes[0],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()},
                new CloudWebPortal.Models.Machine { DisplayName="UniMelb Lab PC13",      IP = "192.168.1.10",   Platform=MachinePlatforms[0],   Type=MachineTypes[1],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()},
                new CloudWebPortal.Models.Machine { DisplayName="UniMelb Lab PC14",      IP = "192.168.1.18",   Platform=MachinePlatforms[1],   Type=MachineTypes[1],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()},
                new CloudWebPortal.Models.Machine { DisplayName="EC2 Server for the Uni",IP = "29.20.100.100",  Platform=MachinePlatforms[1],   Type=MachineTypes[2],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()},
                new CloudWebPortal.Models.Machine { DisplayName="MS Azure for the Uni",  IP = "80.22.18.8",     Platform=MachinePlatforms[0],   Type=MachineTypes[3],   StatusEnum=DaemonProbeStatus.Unknown, Workers = new List<Worker>(), Masters = new List<Master>(), SoftwareAppliances = new List<SoftwareAppliance>()}
            };
            Machines.ForEach(s => context.Machines.Add(s));
            context.SaveChanges();

            var Daemons = new List<Daemon>
            {
                new Daemon { Port=2500, Directory="c:\\Aneka"},
                new Daemon { Port=2500, Directory="c:\\Aneka"},
                new Daemon { Port=2500, Directory="c:\\Aneka"},
                new Daemon { Port=2500, Directory="c:\\Aneka"},
                new Daemon { Port=2500, Directory="c:\\Aneka"},
                new Daemon { Port=2500, Directory="c:\\Aneka"},
                new Daemon { Port=2500, Directory="c:\\Aneka"},
                new Daemon { Port=2500, Directory="c:\\Aneka"}
            };
            Daemons.ForEach(s => context.Daemons.Add(s));
            context.SaveChanges();

            Machines[0].Daemon = Daemons[0];
            Machines[1].Daemon = Daemons[1];
            Machines[2].Daemon = Daemons[2];
            Machines[3].Daemon = Daemons[3];
            Machines[4].Daemon = Daemons[4];
            Machines[5].Daemon = Daemons[5];
            Machines[6].Daemon = Daemons[6];
            Machines[7].Daemon = Daemons[7];
            context.SaveChanges();

            MachineLoginCredentials[0].Machines.Add(Machines[0]);
            //MachineLoginCredentials[0].Machines.Add(Machines[1]);
            MachineLoginCredentials[0].Machines.Add(Machines[2]);
            MachineLoginCredentials[0].Machines.Add(Machines[3]);
            //MachineLoginCredentials[0].Machines.Add(Machines[4]);
            MachineLoginCredentials[0].Machines.Add(Machines[5]);
            MachineLoginCredentials[0].Machines.Add(Machines[6]);
            MachineLoginCredentials[0].Machines.Add(Machines[7]);
            context.SaveChanges();

            var ResourcePools = new List<ResourcePool>
            {
                new ResourcePool { ResourcePoolDisplayName="Lab12", Machines = new List<CloudWebPortal.Models.Machine>()},
                new ResourcePool { ResourcePoolDisplayName="EC2", Machines = new List<CloudWebPortal.Models.Machine>()}
            };
            ResourcePools.ForEach(s => context.ResourcePools.Add(s));
            context.SaveChanges();

            ResourcePools[0].Machines.Add(Machines[0]);
            ResourcePools[0].Machines.Add(Machines[1]);
            ResourcePools[0].Machines.Add(Machines[2]);
            ResourcePools[0].Machines.Add(Machines[3]);
            ResourcePools[0].Machines.Add(Machines[4]);
            ResourcePools[0].Machines.Add(Machines[5]);
            ResourcePools[1].Machines.Add(Machines[6]);
            ResourcePools[1].Machines.Add(Machines[7]);
            context.SaveChanges();


            var SoftwareAppliances = new List<SoftwareAppliance>
            {
                new SoftwareAppliance { Name="POV-Ray", Vendor="Persistence of Vision Ray Tracer Pty. Ltd.", Version="3.1", Machines = new List<CloudWebPortal.Models.Machine>()},
                new SoftwareAppliance { Name="Demo App", Vendor="Demo Pty. Ltd.", Version="8.1", Machines = new List<CloudWebPortal.Models.Machine>()},
                new SoftwareAppliance { Name="CAD", Vendor="Unknown Pty. Ltd.", Version="0.1", Machines = new List<CloudWebPortal.Models.Machine>()}
            };
            SoftwareAppliances.ForEach(s => context.SoftwareAppliances.Add(s));
            context.SaveChanges();

            Machines[0].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[1].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[2].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[2].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[2].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[2].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[3].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[4].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[5].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[5].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[6].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[7].SoftwareAppliances.Add(SoftwareAppliances[0]);
            Machines[7].SoftwareAppliances.Add(SoftwareAppliances[0]);
            context.SaveChanges();

            var Clouds = new List<Cloud>
            {
                new Cloud{ CloudName="UniMelb", DBConnectionString="Data Source=Manjra-Server;Initial Catalog=CloudWebPortalDB;Integrated Security=True", SecuritySharedKey="548t14s39639sds86191d83983k69434", Workers = new List<Worker>(), CloudUserAccounts = new List<CloudUserAccount>()},
                new Cloud{ CloudName="Hyper Cloud EC2 and Local - Test Phase", DBConnectionString="Data Source=Manjra-Server;Initial Catalog=CloudWebPortalDB;Integrated Security=True", SecuritySharedKey="34534b534b54534d39sds7rtbe8e88583983k69434", Workers = new List<Worker>(), CloudUserAccounts = new List<CloudUserAccount>()}
            };
            Clouds.ForEach(s => context.Clouds.Add(s));
            context.SaveChanges();

            var Workers = new List<Worker>
            {
                new Worker { DisplayName="UniMelb Manjra-W",         Port=9055, Services = new List<Service>()},
                new Worker { DisplayName="Lab12 PC1-W",              Cost=20.1, Port=9058, Services = new List<Service>()},
                new Worker { DisplayName="Lab12 PC7-W",              Cost=20.1, Port=9050, Services = new List<Service>()},
                new Worker { DisplayName="on EC2-W",                 Port=9050, Services = new List<Service>()},
                new Worker { DisplayName="on Azure-W",               Cost=20.1, Port=9050, Services = new List<Service>()}
            };
            Workers.ForEach(s => context.Workers.Add(s));
            context.SaveChanges();

            var Masters = new List<Master>
            {
                new Master { DisplayName="UniMelb Manjra Master",         Port=9055, Services = new List<Service>()},
                new Master { DisplayName="Lab12 PC1 Master",              Cost=20, Port=9058, Services = new List<Service>()},
                new Master { DisplayName="Lab12 PC7 Master",              Cost=20, Port=9050, Services = new List<Service>()},
                new Master { DisplayName="on EC2 Master",                 Port=9050, Services = new List<Service>()},
                new Master { DisplayName="on Azure Master",               Cost=20, Port=9050, Services = new List<Service>()}
            };
            Masters.ForEach(s => context.Masters.Add(s));
            context.SaveChanges();

            Workers[0].Services.Add(Services[0]);
            Workers[0].Services.Add(Services[1]);
            Workers[0].Services.Add(Services[2]);
            Workers[0].Services.Add(Services[3]);
            Workers[0].Services.Add(Services[4]);
            Workers[0].Services.Add(Services[5]);
            Workers[1].Services.Add(Services[0]);
            //Workers[1].Services.Add(Services[1]);
            Workers[1].Services.Add(Services[2]);
            Workers[1].Services.Add(Services[3]);
            //Workers[1].Services.Add(Services[4]);
            Workers[1].Services.Add(Services[5]);
            Workers[2].Services.Add(Services[0]);
            Workers[2].Services.Add(Services[1]);
            Workers[2].Services.Add(Services[2]);
            //Workers[2].Services.Add(Services[3]);
            Workers[2].Services.Add(Services[4]);
            //Workers[2].Services.Add(Services[5]);
            Workers[3].Services.Add(Services[0]);
            Workers[3].Services.Add(Services[1]);
            Workers[3].Services.Add(Services[2]);
            Workers[3].Services.Add(Services[3]);
            Workers[3].Services.Add(Services[4]);
            Workers[3].Services.Add(Services[5]);
            Workers[4].Services.Add(Services[0]);
            Workers[4].Services.Add(Services[1]);
            Workers[4].Services.Add(Services[2]);
            Workers[4].Services.Add(Services[3]);
            Workers[4].Services.Add(Services[4]);
            Workers[4].Services.Add(Services[5]);
            context.SaveChanges();

            Masters[0].Services.Add(Services[0]);
            Masters[0].Services.Add(Services[1]);
            Masters[0].Services.Add(Services[2]);
            Masters[0].Services.Add(Services[3]);
            Masters[0].Services.Add(Services[4]);
            Masters[0].Services.Add(Services[5]);
            Masters[1].Services.Add(Services[0]);
            Masters[1].Services.Add(Services[1]);
            Masters[1].Services.Add(Services[2]);
            Masters[1].Services.Add(Services[3]);
            Masters[1].Services.Add(Services[4]);
            Masters[1].Services.Add(Services[5]);
            Masters[2].Services.Add(Services[0]);
            Masters[2].Services.Add(Services[1]);
            Masters[2].Services.Add(Services[2]);
            Masters[2].Services.Add(Services[3]);
            Masters[2].Services.Add(Services[4]);
            Masters[2].Services.Add(Services[5]);
            Masters[3].Services.Add(Services[0]);
            Masters[3].Services.Add(Services[1]);
            Masters[3].Services.Add(Services[2]);
            Masters[3].Services.Add(Services[3]);
            Masters[3].Services.Add(Services[4]);
            Masters[3].Services.Add(Services[5]);
            Masters[4].Services.Add(Services[0]);
            Masters[4].Services.Add(Services[1]);
            Masters[4].Services.Add(Services[2]);
            Masters[4].Services.Add(Services[3]);
            Masters[4].Services.Add(Services[4]);
            Masters[4].Services.Add(Services[5]);
            context.SaveChanges();

            Machines[0].Workers.Add(Workers[0]);
            Machines[0].Workers.Add(Workers[1]);
            Machines[2].Workers.Add(Workers[2]);
            Machines[6].Workers.Add(Workers[3]);
            Machines[7].Workers.Add(Workers[4]);
            context.SaveChanges();

            Machines[0].Masters.Add(Masters[0]);
            Machines[1].Masters.Add(Masters[1]);
            context.SaveChanges();

            Clouds[0].Workers.Add(Workers[0]);
            Clouds[0].Workers.Add(Workers[1]);
            Clouds[0].Workers.Add(Workers[2]);
            Clouds[1].Workers.Add(Workers[3]);
            Clouds[1].Workers.Add(Workers[4]);
            context.SaveChanges();

            Clouds[0].Master = Masters[0];
            Clouds[1].Master = Masters[1];
            context.SaveChanges();

            var CloudUserAccounts = new List<CloudUserAccount>
            {
                new CloudUserAccount{ Username="dev", Password="dev", Clouds = new List<Cloud>()}
            };
            CloudUserAccounts.ForEach(s => context.CloudUserAccounts.Add(s));
            context.SaveChanges();

            Clouds[0].CloudUserAccounts.Add(CloudUserAccounts[0]);
            Clouds[1].CloudUserAccounts.Add(CloudUserAccounts[0]);
            context.SaveChanges();
            */

            base.Seed(context);
        }
    }
}
