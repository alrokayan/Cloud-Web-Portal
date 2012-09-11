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
using Aneka.Management.Config;
using Aneka.Entity;
using Aneka.Config;
using Aneka.PAL.Management.Proxy;
using log4net;
using Aneka.UI.Configuration;
using Aneka.Runtime;
using System.IO;
using CloudWebPortal.Areas.Aneka.Models;

namespace CloudWebPortal.Logic
{
    public class MasterContainersManagement : ContainersManagement
    {
        private MachinesManagement machineManagment = new MachinesManagement();
        private AnekaDbContext db = new AnekaDbContext();

        /// <summary>
        /// Build Cloud from already Exist master container
        /// </summary>
        /// <param name="configure"></param>
        /// <param name="property"></param>
        public void buildExistMaster(global::Aneka.Runtime.Configuration configure, ContainerProperty property)
        {
            try
            {
                Uri uri = new Uri(configure.IndexServerUri);
                CloudWebPortal.Areas.Aneka.Models.Machine machine = db.Machines.Where(x => x.IP == uri.Host).First();
                Cloud cloud = new Cloud();
                Master master = new Master();
                if (property.Status == ContainerStatus.Started)
                {
                    IContainerManagerProxy containerProxy = ProxyCreator.CreateContainerManagerProxy();
                    NodeInfo nodeInfo = containerProxy.GetNodeInfo(uri.ToString());
                    if (nodeInfo != null)
                    {
                        master.Services = new List<Service>();
                        foreach (String serviceName in nodeInfo.GetServices())
                        {
                            foreach (Service serviceFromDB in db.Services.ToList())
                            {
                                if (serviceFromDB.SpringSegmentXML.Contains(serviceName))
                                    master.Services.Add(serviceFromDB);
                            }
                        }
                    }
                }

                master.DisplayName = configure.IndexServerUri;
                master.isInstalled = true;
                master.Port = configure.Port;
                master.AnekaContainerID = property.Id;
                master.Cost = (int)configure.Cost;
                cloud.CloudName = configure.Name;

                db.Masters.Add(master);
                machine.Masters.Add(master);
                db.SaveChanges();

                db.Clouds.Add(cloud);
                cloud.Master = master;
                db.SaveChanges();

                try
                {
                    //Get already exist users
                    Uri masterUri = new Uri(ContainersManagement.GetContainerUri(machine.IP, master.Port));
                    global::Aneka.Security.UserCredentials adminLogin = new global::Aneka.Security.UserCredentials("Administrator", String.Empty);
                    AnekaUsersManagement anekaUsersManagement = new AnekaUsersManagement(masterUri, adminLogin);
                    List<CloudUserAccount> cloudUserAccounts = anekaUsersManagement.getAllUsersFromTheCloud();
                    foreach (CloudUserAccount account in cloudUserAccounts)
                    {
                        if (account.Password == "")
                            account.Password = "Administrator";
                        account.Clouds = new List<Cloud>();
                        account.Clouds.Add(cloud);
                        db.CloudUserAccounts.Add(account);
                        db.SaveChanges();
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    FinishedMasterActivity(cloud.CloudId);
                }
                
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Build Cloud from its information in the database
        /// </summary>
        /// <param name="cloudID">CloudID</param>
        public void CreateCloud(int cloudID)
        {
            Cloud cloud = db.Clouds.Find(cloudID);

            try
            {
                CloudWebPortal.Areas.Aneka.Models.Machine machine = machineLookupFromMasterId(cloud.Master.MasterId);
                Master master = cloud.Master;

                DaemonProbeResult result = new DaemonProbeResult(DaemonProbeStatus.Unknown, null);
                if (machine.StatusEnum == DaemonProbeStatus.ServiceStarted)
                    result = new DaemonProbeResult(DaemonProbeStatus.ServiceStarted, null);
                else
                    return;

                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                byte[] key = global::Aneka.Security.Cryptography.AESCryptoUtil.GenerateKey();
                String NewSecuritySharedKey = Convert.ToBase64String(key);
               // String NewSecuritySharedKey = "whM2IcwXiQzmVbDyvOnOmPxgUS6WRp+VKVYuThtVuiM=";

                String containerId = CreateContainer(machine.Daemon.Directory,
                    machine.IP,
                    machine.Daemon.Port,
                    master.Port,
                    true,
                    cloud.CloudName,
                    master.Cost,
                    NewSecuritySharedKey,
                    machine.SoftwareAppliances,
                    master.Services,
                    AnekaMachine.DaemonUri,
                    null, //Only for worker
                    null, //Only for worker
                    cloud.DBConnectionString,//Only for master
                    cloud.Master.MasterFailoverBackupURI);//Only for master

                Master masterFromDB = db.Masters.Find(cloud.Master.MasterId);
                masterFromDB.AnekaContainerID = containerId;
                db.SaveChanges();

                Cloud cloudFromDB = db.Clouds.Find(cloudID);
                cloudFromDB.SecuritySharedKey = NewSecuritySharedKey;
                db.SaveChanges();

                //Get already exist users
                Uri masterUri = new Uri(ContainersManagement.GetContainerUri(machine.IP, master.Port));
                global::Aneka.Security.UserCredentials adminLogin = new global::Aneka.Security.UserCredentials("Administrator", String.Empty);
                AnekaUsersManagement anekaUsersManagement = new AnekaUsersManagement(masterUri, adminLogin);
                List<CloudUserAccount> cloudUserAccounts = anekaUsersManagement.getAllUsersFromTheCloud();
                foreach (CloudUserAccount account in cloudUserAccounts)
                {
                    if (account.Password == "")
                        account.Password = "Administrator";
                    account.Clouds = new List<Cloud>();
                    account.Clouds.Add(cloud);
                    db.CloudUserAccounts.Add(account);
                    db.SaveChanges();
                }

                FinishedMasterActivity(cloud.CloudId);
            }
            catch (Exception ex)
            {
                Master masterFromDB = db.Masters.Find(cloud.Master.MasterId);
                masterFromDB.StatusEnum = global::Aneka.UI.Configuration.ProbeStatus.Error;
                masterFromDB.ProgressMesage = "Error while creating master container: " + ex;
                masterFromDB.isInProgress = false;
                db.SaveChanges();
            }

        }

        /// <summary>
        /// Start Master Container
        /// </summary>
        /// <param name="CloudID">Cloud ID</param>
        public void StartMasterContainer(int CloudID)
        {
            try
            {
                Cloud cloud = db.Clouds.Find(CloudID);

                CloudWebPortal.Areas.Aneka.Models.Machine machine = machineLookupFromMasterId(cloud.Master.MasterId);

                if (machine == null)
                    return;

                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                StartContainer(AnekaMachine.DaemonUri, cloud.Master.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Master masterFromDB = db.Clouds.Find(CloudID).Master;
                masterFromDB.StatusEnum = global::Aneka.UI.Configuration.ProbeStatus.Error;
                masterFromDB.ProgressMesage = "Error while creating the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedMasterActivity(CloudID);
            }
        }

        /// <summary>
        /// Stop Master Container
        /// </summary>
        /// <param name="CloudID">Cloud ID</param>
        public void StopMasterContainer(int CloudID)
        {
            try
            {
                Cloud cloud = db.Clouds.Find(CloudID);

                CloudWebPortal.Areas.Aneka.Models.Machine machine = machineLookupFromMasterId(cloud.Master.MasterId);

                if (machine == null)
                    return;

                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                StopContainer(AnekaMachine.DaemonUri, cloud.Master.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Master masterFromDB = db.Clouds.Find(CloudID).Master;
                masterFromDB.StatusEnum = global::Aneka.UI.Configuration.ProbeStatus.Error;
                masterFromDB.ProgressMesage = "Error while creating the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedMasterActivity(CloudID);
            }
        }

        /// <summary>
        /// Uninstall Master Container
        /// </summary>
        /// <param name="CloudID">Cloud ID</param>
        public void DestroyMasterContainer(int CloudID)
        {
            try
            {
                Cloud cloud = db.Clouds.Find(CloudID);

                CloudWebPortal.Areas.Aneka.Models.Machine machine = machineLookupFromMasterId(cloud.Master.MasterId);

                if (machine == null)
                    return;

                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                DestroyContainer(AnekaMachine.DaemonUri, cloud.Master.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Master masterFromDB = db.Clouds.Find(CloudID).Master;
                masterFromDB.StatusEnum = global::Aneka.UI.Configuration.ProbeStatus.Error;
                masterFromDB.ProgressMesage = "Error while creating the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedMasterActivity(CloudID);
            }
        }

        /// <summary>
        /// Restart Master Container
        /// </summary>
        /// <param name="CloudID">Cloud ID</param>
        public void RestartMasterContainer(int CloudID)
        {
            try
            {
                Cloud cloud = db.Clouds.Find(CloudID);

                CloudWebPortal.Areas.Aneka.Models.Machine machine = machineLookupFromMasterId(cloud.Master.MasterId);

                if (machine == null)
                    return;

                global::Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                RestartContainer(AnekaMachine.DaemonUri, cloud.Master.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Master masterFromDB = db.Clouds.Find(CloudID).Master;
                masterFromDB.StatusEnum = global::Aneka.UI.Configuration.ProbeStatus.Error;
                masterFromDB.ProgressMesage = "Error while creating the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedMasterActivity(CloudID);
            }
        }

        /// <summary>
        /// Change the master Status in databse to finish last activity
        /// </summary>
        /// <param name="cloudID"></param>
        public void FinishedMasterActivity(int cloudID)
        {
            RefreshMaster(cloudID);
            Cloud cloud = db.Clouds.Find(cloudID);
            if (cloud.Master.StatusEnum == ProbeStatus.ServiceActive)
                cloud.Master.isInstalled = true;
            cloud.Master.isInProgress = false;
            cloud.Master.ProgressMesage = String.Empty;
            db.SaveChanges();
        }

        /// <summary>
        /// Refrash Master Container
        /// </summary>
        /// <param name="CloudID">Cloud ID</param>
        /// <returns>Master Status</returns>
        public ProbeResult RefreshMaster(int CloudID)
        {
            Cloud cloud = db.Clouds.Find(CloudID);
            CloudWebPortal.Areas.Aneka.Models.Machine machine = machineLookupFromMasterId(cloud.Master.MasterId);

            ProbeResult result = new ProbeResult(GetContainerUri(machine.IP, cloud.Master.Port), ProbeStatus.Error);

            Uri masterUri = null;
            string container = null;

            try
            {
                if (machine == null || cloud.Master.AnekaContainerID == null)
                    return result;

                container = GetContainerUri(machine.IP, cloud.Master.Port);
                masterUri = new Uri(container);

                // [CV] NOTE: we are now sure that everything is at least
                //            feasible for starting asynchronous probing.
                result = ProbeMaster(masterUri);

            }
            catch (Exception ex)
            {
                result = new ProbeResult(container, ProbeStatus.Error, ex);
            }
            finally
            {
                cloud.Master.StatusEnum = result.Status;
                if (result.Status == ProbeStatus.ServiceActive)
                    cloud.Master.isInstalled = true;
                db.SaveChanges();
            }

            return result;
        }

        /// <summary>
        /// Performs a test to check whether the master container
        /// is active on the given uri.
        /// </summary>
        /// <param name="state">An instance of <see cref="T:System.Uri"/>
        /// representing the unique resource identifier of the container.</param>
        private ProbeResult ProbeMaster(Uri state)
        {
            ProbeResult outcome = null;
            Uri masterUri = state;
            try
            {
                string ip = masterUri.Host;
                if (masterUri.HostNameType == UriHostNameType.Dns)
                {
                    ip = NetworkUtil.GetIPByHostName(masterUri.Host);
                }
                bool bActive = NetworkUtil.Ping(ip);
                if (bActive == true)
                {
                    bActive = NetworkUtil.Probe(ip, masterUri.Port);
                    if (bActive == true)
                    {
                        outcome = ProbeContainer(masterUri.OriginalString);
                    }
                    else
                    {
                        outcome = new ProbeResult(masterUri.OriginalString, ProbeStatus.ServiceUnactive);
                    }
                }
                else
                {
                    outcome = new ProbeResult(masterUri.OriginalString, ProbeStatus.NetworkUnreachable);
                }
            }
            catch (Exception ex)
            {
                outcome = new ProbeResult(masterUri.OriginalString, ProbeStatus.Error, ex);
                return outcome;
            }

            if (outcome == null)
            {
                outcome = new ProbeResult(masterUri.Host, ProbeStatus.ServiceActive);
            }
            //try
            //{
            //    this.Invoke(new ProbeUpdater(this.UpdateMasterUI), new object[] { outcome });
            //}
            //catch (Exception ex)
            //{
            //    // if the dialog is closing and the thread is trying to update
            //    // the user interface we have an exception, we then wrap it and
            //    // everything should be fine...
            //}
            return outcome;

        }


    }
}