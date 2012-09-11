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
using Aneka;
using Aneka.PAL.Management.Model;
using Aneka.PAL.Management.Impl;
using CloudWebPortal.Models;
using System.Runtime.Serialization;
using log4net;
using System.Configuration;
using CloudWebPortal.Areas.Aneka.Models;

namespace CloudWebPortal.Logic
{
    public class DaemonsManagement
    {
        private DaemonManager daemonManager = new DaemonManager();
        private MachinesManagement machineManagment = new MachinesManagement();
        private AnekaDbContext db = new AnekaDbContext();
        private ContainersManagement containersManagement = new ContainersManagement();

        /// <summary>
        /// Force Update Containers with a new binaries
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        public void UpdateContainers(int machineID)
        {
            try
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                //Get exist Containers
                containersManagement.UpdateContainers(AnekaMachine);
            }
            catch (Exception)
            {
            }
            finally
            {
                finishedActivity(machineID);
            }
        }

        /// <summary>
        /// Install a Daemon
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        /// <param name="mapPath">An Aneka Daemon Binaries path for future uses</param>
        public void InstallDaemon(int machineID, string mapPath)
        {
            try
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);
                Daemon daemon = machine.Daemon;

                Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/");
                String RepoAccessURL = rootWebConfig.AppSettings.Settings["RepoAccessURL"].Value;
                String RepoAccessUser = rootWebConfig.AppSettings.Settings["RepoAccessUser"].Value;
                String RepoAccessPassword = rootWebConfig.AppSettings.Settings["RepoAccessPassword"].Value;

                //Repository Group
                Property Method = new Property("Method", "File");
                Property Location = new Property("Location", RepoAccessURL);
                Property Username = new Property("Username", RepoAccessUser);
                Property Password = new Property("Password", RepoAccessPassword);
                Property Platform = new Property("Platform", "Windows");
                PropertyGroup RepositoryGroup = new PropertyGroup("Repository");
                RepositoryGroup.Add(Method);
                RepositoryGroup.Add(Location);
                RepositoryGroup.Add(Username);
                RepositoryGroup.Add(Password);
                RepositoryGroup.Add(Platform);

                //Send Loader Binaries
                //string anekaDaemonBinaries = mapPath + "Repository\\Daemon\\";
                string RepoLocalLocation = rootWebConfig.AppSettings.Settings["RepoLocalLocation"].Value;
                Property DaemonPort = new Property("Port", daemon.Port);
                Property FreshInstall = new Property("FreshInstall", "True");
                Property EnableAutoUpdate = new Property("EnableAutoUpdate", "False");
                Property DaemonHomeDirectory = new Property("HomeDirectory", daemon.Directory + "\\Runtime\\Daemon\\");
                PropertyGroup DaemonGroup = new PropertyGroup("Daemon");
                DaemonGroup.Add(DaemonPort);
                DaemonGroup.Add(FreshInstall);
                DaemonGroup.Add(EnableAutoUpdate);
                DaemonGroup.Add(DaemonHomeDirectory);
                ConfigurationBase daemonInstallerConfig = new ConfigurationBase();
                daemonInstallerConfig.Add(DaemonGroup);
                daemonInstallerConfig.Add(RepositoryGroup);
                machine.isInProgress = true;
                machine.ProgressMesage = "Installing Binaries";
                db.SaveChanges();
                daemonManager.Install(AnekaMachine, daemonInstallerConfig, RepoLocalLocation + "\\Loader\\" );

                finishedActivity(machineID);
            }
            catch (Exception ex)
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                machine.StatusEnum = DaemonProbeStatus.Unknown;
                machine.ProgressMesage = "Error while stopping daemon: " + ex.Message;
                machine.isInProgress = false;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Stop a Daemon
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        /// <param name="mapPath">An Aneka Daemon Binaries path for future uses</param>
        public void StopDaemon(int machineID, string mapPath)
        {
            try
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);
                daemonManager.Stop(AnekaMachine);
                finishedActivity(machineID);
            }
            catch (Exception ex)
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                machine.StatusEnum = DaemonProbeStatus.Unknown;
                machine.ProgressMesage = "Error while stopping daemon: " + ex.Message;
                machine.isInProgress = false;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Start a Daemon
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        /// <param name="mapPath">An Aneka Daemon Binaries path for future uses</param>
        public void StartDaemon(int machineID, string mapPath)
        {
            try
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);
                daemonManager.Start(AnekaMachine);
                finishedActivity(machineID);
            }
            catch (Exception ex)
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                machine.StatusEnum = DaemonProbeStatus.Unknown;
                machine.ProgressMesage = "Error while starting daemon: " + ex.Message;
                machine.isInProgress = false;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Uninstall a Daemon
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        /// <param name="mapPath">An Aneka Daemon Binaries path for future uses</param>
        public void UninstallDaemon(int machineID, string mapPath)
        {
            try
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);
                daemonManager.Uninstall(AnekaMachine);
                finishedActivity(machineID);
            }
            catch (Exception ex)
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
                machine.StatusEnum = DaemonProbeStatus.Unknown;
                machine.ProgressMesage = "Error while uninstalling daemon: " + ex.Message;
                machine.isInProgress = false;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Restart a Daemon
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        /// <param name="mapPath">An Aneka Daemon Binaries path for future uses</param>
        public void RestartDaemon(int machineID, string mapPath)
        {
            CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);

            try
            {
                machine.isInProgress = true;
                machine.ProgressMesage = "Stopping Daemon";
                db.SaveChanges();
                StopDaemon(machineID, mapPath);
            }
            catch (Exception ex)
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machineFromDB = db.Machines.Find(machineID);
                machineFromDB.StatusEnum = DaemonProbeStatus.Unknown;
                machineFromDB.ProgressMesage = "Error while stopping daemon during the restarting: " + ex.Message;
                machine.isInProgress = false;
                db.SaveChanges();
            }

            try
            {
                machine.isInProgress = true;
                machine.ProgressMesage = "Starting Daemon";
                db.SaveChanges();
                StartDaemon(machineID, mapPath);
            }
            catch (Exception ex)
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machineFromDB = db.Machines.Find(machineID);
                machineFromDB.StatusEnum = DaemonProbeStatus.Unknown;
                machineFromDB.ProgressMesage = "Error while starting daemon during the restarting: " + ex.Message;
                machine.isInProgress = false;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// This function will change the Machine Status to finished activity 
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        private void finishedActivity(int machineID)
        {
            //Finished
            CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Find(machineID);
            machineManagment.UpdateMachineStatus(machine.MachineId, machineManagment.getLogin(machine));
            machine.isInProgress = false;
            machine.ProgressMesage = String.Empty;
            db.SaveChanges();
        }
    }
}