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
    public class ContainersManagement
    {
        private MachinesManagement machineManagment = new MachinesManagement();
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        //Check this function in CosoleForm: private ContainerMetadata SaveOrUpdateContainer(Uri anekaDaemonServiceUrl, ContainerProperty property, int previousImageIndex)
        public void UpdateContainers(Aneka.PAL.Management.Model.Machine anekaDaemonMachine)
        {
            IDaemonProxy proxy = ProxyCreator.CreateDaemonProxy(anekaDaemonMachine.DaemonUri);
            ContainerProperty[] properties = proxy.QueryContainers(new string[] { });

            foreach (ContainerProperty property in properties)
            {
                string tmpConfig = Path.GetTempFileName();
                if (string.IsNullOrEmpty(property.ConfigurationBase64Content) == false)
                {
                    //write aneka xml to the tmp config file
                    IOUtil.WriteBase64EncodedToFile(tmpConfig, property.ConfigurationBase64Content);

                    //load the aneka clone from the tmp config
                    Aneka.Runtime.Configuration configure = Aneka.Runtime.Configuration.GetConfiguration(tmpConfig);


                    //create a new container metadata with updated clone properties
                    bool isMaster = configure.NodeType != Aneka.Runtime.NodeType.Worker;
                    if (isMaster)
                    {
                        MasterContainersManagement masterContainersManagement = new MasterContainersManagement();
                        masterContainersManagement.buildExistMaster(configure, property);
                    }
                    else
                    {
                        //Uri uri = new Uri(configure.IndexServerUri);
                        //CloudWebPortal.Models.Machine machine = db.Machines.Where(x => x.IP == uri.Host).First();
                        //Cloud cloud = db.Clouds.Where(x => ).First();//MUST GET THE CLOUD
                        //Worker worker = new Worker();

                        //worker.Port = uri.Port;

                        //if (property.Status == ContainerStatus.Started)
                        //{
                        //    IContainerManagerProxy containerProxy = ProxyCreator.CreateContainerManagerProxy();
                        //    NodeInfo nodeInfo = containerProxy.GetNodeInfo(uri.ToString());

                        //    if (nodeInfo != null)
                        //    {
                        //        worker.isQuarantined = containerProxy.NodeQuarantined(nodeInfo.IndexServerUri, nodeInfo.Id);
                        //        //metadata.Services = nodeInfo.GetServices(); //MUST BE DONE
                        //    }

                        //}

                    }

                    //! metadata = new ContainerMetadata(string.Format("tcp://{0}:{1}/Aneka", anekaDaemonServiceUrl.Host, configure.Port));
                    //! metadata.InternalId = configure.Id;
                    //! metadata.AnekaDaemonServiceUrl = anekaDaemonServiceUrl;
                }
            }
        }

        /// <summary>
        /// Creating a new Container
        /// </summary>
        /// <param name="ContainerInstallationDirectory">Container Installation Directory</param>
        /// <param name="ContainerMachineIP">Container Machine IP address</param>
        /// <param name="ContainerMachineDaemonPort">Container Machine Daemon Port number</param>
        /// <param name="ContainerPort">Container Port number</param>
        /// <param name="isMaster">Is this Container a Master?</param>
        /// <param name="CloudName">Cloud Name, just for display</param>
        /// <param name="Cost">Cost</param>
        /// <param name="SharedAuthenticationKey">Shared Authentication Key</param>
        /// <param name="SoftwareAppliances">A list of Software Appliances</param>
        /// <param name="Services">A list of Services</param>
        /// <param name="ContainerMachineDaemonUri">Container Machine Daemon URI</param>
        /// <param name="MasterPort">Master Port number</param>
        /// <param name="MasterMachineIP">Master Machine IP address</param>
        /// <param name="DBConnectionString">DB Connection String</param>
        /// <param name="MasterBackupUri">Master Backup URI</param>
        /// <returns></returns>
        protected String CreateContainer(String ContainerInstallationDirectory,
            String ContainerMachineIP,
            int ContainerMachineDaemonPort,
            int ContainerPort,
            bool isMaster,
            String CloudName,
            int Cost,
            String SharedAuthenticationKey,
            System.Collections.Generic.ICollection<SoftwareAppliance> SoftwareAppliances,
            System.Collections.Generic.ICollection<Service> Services,
            String ContainerMachineDaemonUri,
            int? MasterPort, //For workers only
            String MasterMachineIP,//For workers only
            String DBConnectionString,//For master only
            String MasterBackupUri)//For master only
        {
            try
            {

                AnekaConfiguration configuration = new AnekaConfiguration();
                configuration.Daemon = new DaemonConfiguration();
                configuration.Container = new ContainerConfiguration();

                configuration.Home = ContainerInstallationDirectory + "\\Runtime\\Daemon\\LocalRepository\\Container\\";
                configuration.Host = ContainerMachineIP;

                configuration.Daemon.Port = ContainerMachineDaemonPort;

                configuration.Container.Port = ContainerPort;
                configuration.Container.BuiltInSecurityMode = Aneka.Management.Config.BuiltInSecurityMode.ANEKA;
                configuration.Container.IsMasterNode = isMaster;
                if(MasterPort.HasValue && MasterMachineIP != null)
                    configuration.Container.IndexNodeUri = GetContainerUri(MasterMachineIP, (int)MasterPort);
                if (isMaster == true && DBConnectionString != null)
                {
                    configuration.Container.ConnectionString = DBConnectionString;
                    configuration.Container.IsMemoryPersistence = false;
                    configuration.Container.BuiltInSecurityMode = BuiltInSecurityMode.ANEKA;
                    configuration.Container.ProviderName = "System.Data.SqlClient";
                }
                else
                {
                    configuration.Container.ConnectionString = null;
                    configuration.Container.IsMemoryPersistence = true;
                    configuration.Container.BuiltInSecurityMode = BuiltInSecurityMode.ANONYMOUS;
                }
                configuration.Container.Cost = Cost;
                configuration.Container.InstanceName = CloudName;
                configuration.Container.SharedAuthenticationKey = SharedAuthenticationKey;

                configuration.Container.AutoDectectNodeCapacity = true;

                if (isMaster == true && MasterBackupUri != null)
                {
                    configuration.Container.Masters.Add(ContainerMachineDaemonUri);
                    configuration.Container.Masters.Add(MasterBackupUri);
                }
                
                List<Software> softwares = new List<Software>();
                foreach (SoftwareAppliance sw in SoftwareAppliances)
                {
                    int majorVersion = Convert.ToInt32(sw.Version.Split(new char[] { '.' }).ElementAt(0));
                    int minorVersion = Convert.ToInt32(sw.Version.Split(new char[] { '.' }).ElementAt(1));
                    softwares.Add(new Software(sw.Name, sw.Vendor, majorVersion, minorVersion));
                }
                configuration.Container.Appliances = softwares.ToArray();

                //All Services
                System.Collections.Generic.Dictionary<string, IConfigurableEntityLoader> services = new Dictionary<string, IConfigurableEntityLoader>();
                if (isMaster)
                {
                    services.Add("ReportingService", new Aneka.Management.Config.Entity.ReportingServiceConfigureProperty());
                    services.Add("ResourceProvisioningService", new Aneka.Management.Config.Entity.ResourceProvisioningServiceConfigureProperty());
                    services.Add("TaskScheduler", new Aneka.Management.Config.TaskSchedulingServiceConfigureProperty());
                    services.Add("ThreadScheduler", new Aneka.Management.Config.ThreadSchedulingServiceConfigureProperty());
                    services.Add("MapReduceScheduler", new Aneka.Management.Config.Entity.MapReduceSchedulingServiceConfigureProperty());
                    services.Add("StorageService", new Aneka.Management.Config.Entity.StorageServiceConfigureProperty());
                }
                services.Add("LoggingService", new Aneka.Management.Config.Entity.LoggingServiceConfigureProperty());
                services.Add("MonitoringService", new Aneka.Management.Config.Entity.MonitoringServiceConfigureProperty());
                services.Add("DiscoveryService", new Aneka.Management.Config.Entity.DiscoveryServiceConfigureProperty());
                services.Add("TaskExecutionService", new Aneka.Management.Config.Entity.TaskExecutionServiceConfigurableProperty());
                services.Add("ThreadExecutionService", new Aneka.Management.Config.Entity.ThreadExecutionServiceConfigurableProperty());
                services.Add("MapReduceExecutor", new Aneka.Management.Config.Entity.MapReduceExecutionServiceConfigureProperty());

                foreach (IConfigurableEntityLoader entity in services.Values)
                {
                    //check whether it implements the IConfigurationPropertiesCallback method
                    IConfigurationPropertiesCallback configurationCallback
                        = entity as IConfigurationPropertiesCallback;

                    if (configurationCallback != null)
                    {
                        try
                        {
                            configurationCallback.UpdateProperties(configuration);
                        }
                        catch (Exception)
                        {
                            //skip this item, don't add it into the services
                            continue;
                        }
                    }
                }

                configuration.Container.Services = services;

                //Selected Services
                if (Services != null)
                {
                    System.Collections.Generic.Dictionary<string, IConfigurableEntityLoader> requiredServices = new Dictionary<string, IConfigurableEntityLoader>();
                    foreach (Service service in Services)
                    {
                        try
                        {
                            if (service.SpringSegmentXML == "<HardCoded>LoggingService</HardCoded>")
                                requiredServices.Add("LoggingService", new Aneka.Management.Config.Entity.LoggingServiceConfigureProperty());
                            if (service.SpringSegmentXML == "<HardCoded>MonitoringService</HardCoded>")
                                requiredServices.Add("MonitoringService", new Aneka.Management.Config.Entity.MonitoringServiceConfigureProperty());
                            if (service.SpringSegmentXML == "<HardCoded>DiscoveryService</HardCoded>")
                                requiredServices.Add("DiscoveryService", new Aneka.Management.Config.Entity.DiscoveryServiceConfigureProperty());
                            if (service.SpringSegmentXML == "<HardCoded>TaskExecutionService</HardCoded>")
                                requiredServices.Add("TaskExecutionService", new Aneka.Management.Config.Entity.TaskExecutionServiceConfigurableProperty());
                            if (service.SpringSegmentXML == "<HardCoded>ThreadExecutionService</HardCoded>")
                                requiredServices.Add("ThreadExecutionService", new Aneka.Management.Config.Entity.ThreadExecutionServiceConfigurableProperty());
                            if (service.SpringSegmentXML == "<HardCoded>MapReduceExecutor</HardCoded>")
                                requiredServices.Add("MapReduceExecutor", new Aneka.Management.Config.Entity.MapReduceExecutionServiceConfigureProperty());

                            //Master Only Services
                            if (isMaster)
                            {
                                if (service.SpringSegmentXML == "<HardCoded>ReportingService</HardCoded>")
                                    requiredServices.Add("ReportingService", new Aneka.Management.Config.Entity.ReportingServiceConfigureProperty());
                                if (service.SpringSegmentXML == "<HardCoded>ResourceProvisioningService</HardCoded>")
                                    requiredServices.Add("ResourceProvisioningService", new Aneka.Management.Config.Entity.ResourceProvisioningServiceConfigureProperty());
                                if (service.SpringSegmentXML == "<HardCoded>TaskScheduler</HardCoded>")
                                    requiredServices.Add("TaskScheduler", new Aneka.Management.Config.TaskSchedulingServiceConfigureProperty());
                                if (service.SpringSegmentXML == "<HardCoded>ThreadScheduler</HardCoded>")
                                    requiredServices.Add("ThreadScheduler", new Aneka.Management.Config.ThreadSchedulingServiceConfigureProperty());
                                if (service.SpringSegmentXML == "<HardCoded>MapReduceScheduler</HardCoded>")
                                    requiredServices.Add("MapReduceScheduler", new Aneka.Management.Config.Entity.MapReduceSchedulingServiceConfigureProperty());
                                if (service.SpringSegmentXML == "<HardCoded>StorageService</HardCoded>")
                                    requiredServices.Add("StorageService", new Aneka.Management.Config.Entity.StorageServiceConfigureProperty());
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    foreach (IConfigurableEntityLoader entity in requiredServices.Values)
                    {
                        //check whether it implements the IConfigurationPropertiesCallback method
                        IConfigurationPropertiesCallback configurationCallback
                            = entity as IConfigurationPropertiesCallback;

                        if (configurationCallback != null)
                        {
                            try
                            {
                                configurationCallback.UpdateProperties(configuration);
                            }
                            catch (Exception ex)
                            {
                                //skip this item, don't add it into the services
                                continue;
                            }
                        }
                    }

                    configuration.Container.RequiredServices = requiredServices;
                }
                else
                    configuration.Container.RequiredServices = null;


                string[] configs = ConfigurationWizardUtil.GenerateContainerConfigurations(configuration);
                string containerId = ConfigurationWizardUtil.GenerateContainerId();

                if (isMaster == true && DBConnectionString != null)
                {
                    try
                    {
                        // [CV] NOTE: we do not need to get the home of the configuration
                        //            but the current home because we use the setup of the
                        //            management studio to install the database tables...

                        ConfigurationWizardUtil.InstallDatabaseSupport(configuration.Container, null);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Cannot Install Database Support: The installation of the container has been interrupted\n" + ex);
                    }
                }

                IDaemonProxy proxy = ProxyCreator.CreateDaemonProxy(ContainerMachineDaemonUri);
                ContainerProperty property = proxy.CreateContainer(containerId, configs, true);

                if (property.Status != ContainerStatus.Started)
                    throw new Exception("Error while creating a container: " + property.Status);

                return containerId;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        /// <summary>
        /// Starting a Container
        /// </summary>
        /// <param name="DaemonUri">Daemon URI</param>
        /// <param name="AnekaContainerID">Aneka Container ID</param>
        protected void StartContainer(String DaemonUri, String AnekaContainerID)
        {
            try
            {
                //get the Aneka Daemon proxy
                IDaemonProxy proxy = ProxyCreator.CreateDaemonProxy(DaemonUri);//AnekaDaemonServiceUrl = tcp://128.250.28.174:9000/daemon

                proxy.StartContainers(new string[] { AnekaContainerID });// ContainerName = fc4a9750-1441-4ce1-ba07-5a8affbe9f93
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Stopping a Container
        /// </summary>
        /// <param name="DaemonUri">Daemon URI</param>
        /// <param name="AnekaContainerID">Aneka Container ID</param>
        protected void StopContainer(String DaemonUri, String AnekaContainerID)
        {
            try
            {
                //get the Aneka Daemon proxy
                IDaemonProxy proxy = ProxyCreator.CreateDaemonProxy(DaemonUri);//AnekaDaemonServiceUrl = tcp://128.250.28.174:9000/daemon

                proxy.StopContainers(new string[] { AnekaContainerID });// ContainerName = fc4a9750-1441-4ce1-ba07-5a8affbe9f93
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Uninstalling a Container
        /// </summary>
        /// <param name="DaemonUri">Daemon URI</param>
        /// <param name="AnekaContainerID">Aneka Container ID</param>
        protected void DestroyContainer(String DaemonUri, String AnekaContainerID)
        {
            try
            {
                //get the Aneka Daemon proxy
                IDaemonProxy proxy = ProxyCreator.CreateDaemonProxy(DaemonUri);//AnekaDaemonServiceUrl = tcp://128.250.28.174:9000/daemon

                proxy.DestroyContainers(new string[] { AnekaContainerID });// ContainerName = fc4a9750-1441-4ce1-ba07-5a8affbe9f93
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Restarting a Container
        /// </summary>
        /// <param name="DaemonUri">Daemon URI</param>
        /// <param name="AnekaContainerID">Aneka Container ID</param>
        protected void RestartContainer(String DaemonUri, String AnekaContainerID)
        {
            try
            {
                //get the Aneka Daemon proxy
                IDaemonProxy proxy = ProxyCreator.CreateDaemonProxy(DaemonUri);//AnekaDaemonServiceUrl = tcp://128.250.28.174:9000/daemon

                proxy.RestartContainers(new string[] { AnekaContainerID });// ContainerName = fc4a9750-1441-4ce1-ba07-5a8affbe9f93
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Lookup for a machine by passing Master ID
        /// </summary>
        /// <param name="masterID">Master ID</param>
        /// <returns>An Aneka Web Portal Machine</returns>
        public CloudWebPortal.Models.Machine machineLookupFromMasterId(int masterID)
        {
            CloudWebPortal.Models.Machine machine = null;
            foreach (CloudWebPortal.Models.Machine m in db.Machines.ToList())
            {
                foreach (CloudWebPortal.Models.Master master in m.Masters)
                    if (master.MasterId == masterID)
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
        /// Gets a <see langword="string"/> representing the URI of the
        /// container given the <paramref name="host"/> and <paramref name="port"/>.
        /// </summary>
        /// <param name="host">A <see langword="string"/> containing the IP or the host name of the node hosting the container.</param>
        /// <param name="port">An integer representing the port where the container might be installed.</param>
        /// <returns>A <see langword="string"/> containing the uri of
        /// the container.</returns>
        public static string GetContainerUri(string host, int port)
        {
            string uri = string.Format("tcp://{0}:{1}/Aneka",
                                        host == null ? "[host]" : host,
                                        port);
            return uri;
        }

        /// <summary>
        /// Probes a <paramref name="uri"/> to see whether there is
        /// container active there.
        /// </summary>
        /// <param name="uri">A <see langword="string"/> containing the 
        /// unique reosurce identifier of a possible remote container.
        /// </param>
        /// <returns>A <see cref="T:Aneka.UI.Configuration.ProbeResult"/> specifing the
        /// result of the probe operation.</returns>
        protected ProbeResult ProbeContainer(string uri)
        {

            ProbeResult result = null;
            try
            {
                ProbeQueryMessage probe = new ProbeQueryMessage("");
                Message msg = MessageDispatcher.SendMessage(new Uri(uri), probe);
                ProbeReplyMessage reply = msg as ProbeReplyMessage;

                Exception inner = null;

                if (reply == null)
                {
                    ErrorMessage error = msg as ErrorMessage;
                    if (error != null)
                    {
                        inner = error.Cause;
                    }
                    else
                    {
                        string errMsg = string.Format("Invalid response to probe: " + msg.GetType().FullName);
                        inner = new Exception(errMsg);
                        inner.Data.Add("message", msg);
                    }

                }

                result = new ProbeResult(uri, ProbeStatus.ServiceActive, inner);
            }
            catch (Exception ex)
            {

                // [CV] NOTE: we already know that the port is active if we call
                //            this method, hence it might be another service.
                result = new ProbeResult(uri, ProbeStatus.ServiceUnknown, ex);
            }

            return result;

        }
    }
}