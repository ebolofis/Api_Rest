using log4net;
using Symposium.Helpers;
using Symposium.Helpers.Classes;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.Plugins;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_ConfigTasks : IDA_ConfigTasks
    {
        IDA_ConfigDT configDT;
        PluginHelper PluginHelper;
        public ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DA_ConfigTasks(IDA_ConfigDT _configDT, PluginHelper PluginHelper)
        {
            this.configDT = _configDT;
            this.PluginHelper = PluginHelper;
        }

        /// <summary>
        /// Get DA Config 
        /// </summary>
        /// <returns></returns>
        public DA_ConfigModel GetConfig(DBInfoModel dbInfo)
        {
            DA_ConfigModel configModel = new DA_ConfigModel();
            string staffUsername = "";
            string staffPassword = "";
            string baseURL = "";

            string progress = "";

            bool staffUsernameEndsWithAt = false;
            try
            {
                progress = "DA_Staff_Username";
                string staffUsernameRaw = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiDeliveryConfiguration, "DA_Staff_Username");
                staffUsername = staffUsernameRaw.Trim();
                progress = "DA_Staff_Password";
                string staffPasswordRaw = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiDeliveryConfiguration, "DA_Staff_Password");
                staffPassword = staffPasswordRaw.Trim();
                progress = "DA_BaseURL";
                string baseURLRaw = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiDeliveryConfiguration, "DA_BaseURL");
                baseURL = baseURLRaw.Trim();
                progress = "--";
            }
            catch (Exception)
            {
                logger.Error($" Error Reading '{progress}' from Web Config ");
                throw;
            }


            if (!baseURL.EndsWith("/")) baseURL = baseURL + "/";

            staffUsernameEndsWithAt = staffUsername.EndsWith("@");
            if (staffUsernameEndsWithAt == true)
            {
                configModel.DA_StaffUserName = staffUsername;
            }
            else
            {
                configModel.DA_StaffUserName = staffUsername + "@";
            }
            configModel.DA_StaffPassword = staffPassword;
            configModel.DA_BaseUrl = baseURL;
            return configModel;
        }

        /// <summary>
        /// Get StoreId and PosId(The FirstOrDefault PosId)
        /// </summary>
        /// <returns></returns>
        public DA_GetStorePosModel GetStorePos(DBInfoModel dbInfo)
        {
            DA_GetStorePosModel getModel = new DA_GetStorePosModel();

            string StoreIdRaw = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiDeliveryConfiguration, "DA_StoreId");
            getModel.StoreId = StoreIdRaw.Trim().ToLower();
            getModel.PosId = configDT.GetPosId(dbInfo);

            return getModel;
        }

        /// <summary>
        /// Is Delivery Agent (true or false)
        /// </summary>
        /// <returns></returns>
        public bool isDA()
        {
            bool isDeliveryAgent = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiBaseConfiguration, "IsDeliveryAgent");
            return isDeliveryAgent;
        }

        /// <summary>
        /// Is Delivery Store (true or false)
        /// </summary>
        /// <returns></returns>
        public bool isDAclient()
        {
            bool isDeliveryStore = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiBaseConfiguration, "DA_IsClient");
            return isDeliveryStore;
        }

        /// <summary>
        /// DA Store Id
        /// </summary>
        /// <returns></returns>
        public string getDAStoreId()
        {
            string daStoreIdRaw = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiDeliveryConfiguration, "DA_StoreId");
            string daStoreId = daStoreIdRaw.Trim().ToLower();
            return daStoreId;
        }

        /// <summary>
        /// DA cancelable statuses
        /// </summary>
        /// <returns></returns>
        public List<string> getDACancel()
        {
            string DACancelStatusesRaw = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiDeliveryConfiguration, "DA_Cancel");
            string DACancelStatuses = DACancelStatusesRaw.Trim();
            List<string> DACancelStatusesList = DACancelStatuses.Split(new char[] { ',' }).ToList();
            return DACancelStatusesList;
        }
    }
}
