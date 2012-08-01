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

namespace CloudWebPortal.Logic
{
    public class WorkerContainersManagement : ContainersManagement
    {
        private MachinesManagement machineManagment = new MachinesManagement();
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        /// <summary>
        /// Create a new worker from its information in the database
        /// </summary>
        /// <param name="workerID"></param>
        public void CreateWorker(int workerID)
        {
            CloudWebPortal.Models.Cloud cloud = cloudLookupFromWorkerId(workerID);

            try
            {
                CloudWebPortal.Models.Machine masterMachine = machineLookupFromMasterId(cloud.Master.MasterId);
                CloudWebPortal.Models.Machine workerMachine = machineLookupFromWorkerId(workerID);
                CloudWebPortal.Models.Master master = cloud.Master;
                CloudWebPortal.Models.Worker worker = db.Workers.Find(workerID);

                DaemonProbeResult result = new DaemonProbeResult(DaemonProbeStatus.Unknown, null);
                if (masterMachine.StatusEnum == DaemonProbeStatus.ServiceStarted && workerMachine.StatusEnum == DaemonProbeStatus.ServiceStarted)
                    result = new DaemonProbeResult(DaemonProbeStatus.ServiceStarted, null);
                else
                    return;

                Aneka.PAL.Management.Model.Machine AnekaWorkerMachine = machineManagment.ToAnekaPALMachine(workerMachine);

                String containerId = CreateContainer(workerMachine.Daemon.Directory,
                    workerMachine.IP,
                    workerMachine.Daemon.Port,
                    worker.Port,
                    false,
                    cloud.CloudName,
                    worker.Cost,
                    cloud.SecuritySharedKey,
                    workerMachine.SoftwareAppliances,
                    worker.Services,
                    AnekaWorkerMachine.DaemonUri,
                    master.Port,
                    masterMachine.IP,
                    null,//for master only
                    null);//for master only

                Worker workerFromDB = db.Workers.Find(workerID);
                workerFromDB.AnekaContainerID = containerId;
                db.SaveChanges();

                FinishedWorkerActivity(workerID);
            }
            catch (Exception ex)
            {
                Worker workerFromDB = db.Workers.Find(workerID);
                workerFromDB.StatusEnum = Aneka.UI.Configuration.ProbeStatus.Error;
                workerFromDB.ProgressMesage = "Error while creating worker container: " + ex;
                workerFromDB.isInProgress = false;
                db.SaveChanges();
            }

        }

        /// <summary>
        /// Start Worker Container
        /// </summary>
        /// <param name="WorkerID">Worker ID</param>
        public void StartWorkerContainer(int WorkerID)
        {
            try
            {
                CloudWebPortal.Models.Cloud cloud = cloudLookupFromWorkerId(WorkerID);
                Worker worker = db.Workers.Find(WorkerID);
                CloudWebPortal.Models.Machine machine = machineLookupFromWorkerId(WorkerID);

                if (machine == null)
                    return;

                Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                StartContainer(AnekaMachine.DaemonUri, worker.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Worker workerFromDB = db.Workers.Find(WorkerID);
                workerFromDB.StatusEnum = Aneka.UI.Configuration.ProbeStatus.Error;
                workerFromDB.ProgressMesage = "Error while creating the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedWorkerActivity(WorkerID);
            }
        }

        /// <summary>
        /// Stop Worker Container
        /// </summary>
        /// <param name="WorkerID">Worker ID</param>
        public void StopWorkerContainer(int WorkerID)
        {
            try
            {
                CloudWebPortal.Models.Cloud cloud = cloudLookupFromWorkerId(WorkerID);
                Worker worker = db.Workers.Find(WorkerID);
                CloudWebPortal.Models.Machine machine = machineLookupFromWorkerId(WorkerID);

                if (machine == null)
                    return;

                Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                StopContainer(AnekaMachine.DaemonUri, worker.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Worker workerFromDB = db.Workers.Find(WorkerID);
                workerFromDB.StatusEnum = Aneka.UI.Configuration.ProbeStatus.Error;
                workerFromDB.ProgressMesage = "Error while stoping the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedWorkerActivity(WorkerID);
            }
        }

        /// <summary>
        /// Uninstall Worker Container
        /// </summary>
        /// <param name="WorkerID">Worker ID</param>
        public void DestroyWorkerContainer(int WorkerID)
        {
            try
            {
                CloudWebPortal.Models.Cloud cloud = cloudLookupFromWorkerId(WorkerID);
                Worker worker = db.Workers.Find(WorkerID);
                CloudWebPortal.Models.Machine machine = machineLookupFromWorkerId(WorkerID);

                if (machine == null)
                    return;

                Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                DestroyContainer(AnekaMachine.DaemonUri, worker.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Worker workerFromDB = db.Workers.Find(WorkerID);
                workerFromDB.StatusEnum = Aneka.UI.Configuration.ProbeStatus.Error;
                workerFromDB.ProgressMesage = "Error while destroying the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedWorkerActivity(WorkerID);
            }
        }

        /// <summary>
        /// Restart Worker Container
        /// </summary>
        /// <param name="WorkerID">Worker ID</param>
        public void RestartWorkerContainer(int WorkerID)
        {
            try
            {
                CloudWebPortal.Models.Cloud cloud = cloudLookupFromWorkerId(WorkerID);
                Worker worker = db.Workers.Find(WorkerID);
                CloudWebPortal.Models.Machine machine = machineLookupFromWorkerId(WorkerID);

                if (machine == null)
                    return;

                Aneka.PAL.Management.Model.Machine AnekaMachine = machineManagment.ToAnekaPALMachine(machine);

                RestartContainer(AnekaMachine.DaemonUri, worker.AnekaContainerID);
            }
            catch (Exception ex)
            {
                Worker workerFromDB = db.Workers.Find(WorkerID);
                workerFromDB.StatusEnum = Aneka.UI.Configuration.ProbeStatus.Error;
                workerFromDB.ProgressMesage = "Error while restarting the container: " + ex;
                db.SaveChanges();
            }
            finally
            {
                FinishedWorkerActivity(WorkerID);
            }
        }

        /// <summary>
        /// Lookup for a Cloud by sending worker ID
        /// </summary>
        /// <param name="workerID">Worker ID</param>
        /// <returns>Cloud entity</returns>
        public Cloud cloudLookupFromWorkerId(int workerID)
        {
            Cloud cloud = null;

            List<Cloud> clouds = db.Clouds.ToList();

            foreach (Cloud cloudFromDB in clouds)
            {
                if (cloudFromDB.Workers == null)
                    continue;
                foreach (Worker worker in cloudFromDB.Workers)
                    if (worker.WorkerId == workerID)
                    {
                        cloud = cloudFromDB;
                        break;
                    }
                if (cloud != null)
                    break;
            }

            return cloud;
        }

        /// <summary>
        /// Machine Lookup From Worker ID
        /// </summary>
        /// <param name="workerID">Worker ID</param>
        /// <returns>Machine Entity</returns>
        private CloudWebPortal.Models.Machine machineLookupFromWorkerId(int workerID)
        {
            CloudWebPortal.Models.Machine machine = null;
            foreach (CloudWebPortal.Models.Machine m in db.Machines.ToList())
            {
                foreach (CloudWebPortal.Models.Worker worker in m.Workers)
                    if (worker.WorkerId == workerID)
                    {
                        machine = m;
                        break;
                    }
                if (machine != null)
                    break;
            }

            return machine;
        }

        /// <summary>
        /// Change the Worker Status in the datavase as finishes last activity
        /// </summary>
        /// <param name="workerID"></param>
        public void FinishedWorkerActivity(int workerID)
        {
            RefreshWorker(workerID);
            Worker worker = db.Workers.Find(workerID);
            if (worker.StatusEnum == ProbeStatus.ServiceActive)
                worker.isInstalled = true;
            worker.isInProgress = false;
            worker.ProgressMesage = String.Empty;
            db.SaveChanges();
        }

        /// <summary>
        /// Refresh Worker Status
        /// </summary>
        /// <param name="WorkerID">Worker ID</param>
        /// <returns>Worker Status</returns>
        public ProbeResult RefreshWorker(int WorkerID)
        {
            Worker worker = db.Workers.Find(WorkerID);
            CloudWebPortal.Models.Cloud cloud = cloudLookupFromWorkerId(WorkerID);
            CloudWebPortal.Models.Machine machine = machineLookupFromWorkerId(WorkerID);

            ProbeResult result = new ProbeResult(GetContainerUri(machine.IP, worker.Port), ProbeStatus.Error);

            Uri workerUri = null;
            string container = null;

            try
            {
                if (machine == null || worker.AnekaContainerID == null)
                    return result;

                container = GetContainerUri(machine.IP, worker.Port);
                workerUri = new Uri(container);

                // [CV] NOTE: we are now sure that everything is at least
                //            feasible for starting asynchronous probing.
                result = ProbeWorker(workerUri);

            }
            catch (Exception ex)
            {
                result = new ProbeResult(container, ProbeStatus.Error, ex);
            }
            finally
            {
                worker.StatusEnum = result.Status;
                if (result.Status == ProbeStatus.ServiceActive)
                    worker.isInstalled = true;
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
        private ProbeResult ProbeWorker(Uri state)
        {
            ProbeResult outcome = null;
            Uri workerUri = state;
            try
            {
                string ip = workerUri.Host;
                if (workerUri.HostNameType == UriHostNameType.Dns)
                {
                    ip = NetworkUtil.GetIPByHostName(workerUri.Host);
                }
                bool bActive = NetworkUtil.Ping(ip);
                if (bActive == true)
                {
                    bActive = NetworkUtil.Probe(ip, workerUri.Port);
                    if (bActive == true)
                    {
                        outcome = ProbeContainer(workerUri.OriginalString);
                    }
                    else
                    {
                        outcome = new ProbeResult(workerUri.OriginalString, ProbeStatus.ServiceUnactive);
                    }
                }
                else
                {
                    outcome = new ProbeResult(workerUri.OriginalString, ProbeStatus.NetworkUnreachable);
                }
            }
            catch (Exception ex)
            {
                outcome = new ProbeResult(workerUri.OriginalString, ProbeStatus.Error, ex);
                return outcome;
            }

            if (outcome == null)
            {
                outcome = new ProbeResult(workerUri.Host, ProbeStatus.ServiceActive);
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

        /// <summary>
        /// Refresh All Workers in a specific cloud
        /// </summary>
        /// <param name="cloudID">cloud ID</param>
        public void RefreshAllWorkers(int cloudID)
        {
            Cloud cloud = db.Clouds.Find(cloudID);
            foreach (Worker worker in cloud.Workers)
                RefreshWorker(worker.WorkerId);
        }
    }
}