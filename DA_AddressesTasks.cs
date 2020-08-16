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

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_AddressesTasks : IDA_AddressesTasks
    {
        IDA_AddressesDT addressesDT;
        LocalConfigurationHelper configHlp;
        protected ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DA_AddressesTasks(IDA_AddressesDT _addressesDT, LocalConfigurationHelper configHlp)
        {
            this.addressesDT = _addressesDT;
            this.configHlp = configHlp;
        }


        /// <summary>
        /// Get All Active addresses for a Customer
        /// </summary>
        /// <param name="dbInfo">db</param>
        /// <param name="Id">Customer Id</param>
        /// <returns></returns>
       public List<DA_AddressModel> getCustomerAddresses(DBInfoModel dbInfo, long Id)
        {
            configHlp.CheckDeliveryAgent();
            return addressesDT.getCustomerAddresses(dbInfo, Id);
        }

        /// <summary>
        /// Add new Address 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long AddAddress(DBInfoModel dbInfo, DA_AddressModel Model)
        {
            if (Model.AddressNo == null)
            {
                Model.AddressNo = "";
            }
            if (Model.City == null)
            {
                Model.City = "";
            }
            
            Model.Id= addressesDT.AddAddress(dbInfo, Model);

            logger.Info($"NEW ADDRESS.  Id : {Model.Id.ToString()}, Customer: {Model.OwnerId }, Latitude: {Model.Latitude}, Longitude: {Model.Longtitude}, Zipcode: {Model.Zipcode ?? "<null>"}");
            return Model.Id;
        }

        /// <summary>
        /// Update an Address 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long UpdateAddress(DBInfoModel dbInfo, DA_AddressModel Model)
        {
            if (Model.AddressNo == null)
            {
                Model.AddressNo = "";
            }
            if (Model.City == null)
            {
                Model.City = "";
            }
            DA_AddressModel modelDb = addressesDT.getAddress(dbInfo, Model.Id);
            if (modelDb == null) throw new BusinessException($"No address with Id {Model.Id} found");
            if (!string.IsNullOrWhiteSpace(modelDb.ExtId1) && string.IsNullOrWhiteSpace(Model.ExtId1)) Model.ExtId1 = modelDb.ExtId1;
            if (!string.IsNullOrWhiteSpace(modelDb.ExtId2) && string.IsNullOrWhiteSpace(Model.ExtId2)) Model.ExtId2 = modelDb.ExtId2;

            long res = addressesDT.UpdateAddress(dbInfo, Model);

            logger.Info($"UPDATE ADDRESS.{Environment.NewLine}      Original Address>  Id : {modelDb.Id.ToString()}, Customer: {modelDb.OwnerId }, Latitude: {modelDb.Latitude}, Longitude: {modelDb.Longtitude}, Zipcode: {modelDb.Zipcode??"<null>"}, ExtId1: {modelDb.ExtId1 ?? "<null>"}, ExtId2: {modelDb.ExtId2 ?? "<null>"} .{Environment.NewLine}       Updated Address>  Id : {Model.Id.ToString()}, Customer: {Model.OwnerId }, Latitude: {Model.Latitude}, Longitude: {Model.Longtitude}, Zipcode: {Model.Zipcode}, ExtId1: {Model.ExtId1 ?? "<null>"}, ExtId2: {Model.ExtId2 ?? "<null>"} .");
            return res;
        }

        /// <summary>
        /// Delete Address OR set the IsDeleted = 1. If address is deleted then return 1, if set IsDeleted = 1 then return 0
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public long DeleteAddress(DBInfoModel dbInfo, long Id)
        {
            return addressesDT.DeleteAddress(dbInfo, Id);
        }

        /// <summary>
        /// check if DA_AddressesModels contains the correct customerId. If not then trow exception
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="CustomerId"></param>
        public void CheckOwner(DA_AddressModel Model, long CustomerId)
        {
            if (CustomerId == 0) return;
            if (Model == null || Model.OwnerId != CustomerId) throw new BusinessException(Symposium.Resources.Errors.WRONGCUSTOMERIDS);
        }

        /// <summary>
        /// Retreive Coordinate Informations From Google or Terra Maps by giving an Address Model
        /// </summary>
        /// <param name="Model">DA_AddressModel</param>
        /// <returns>DA_AddressModel</returns>
        public DA_AddressModel GeoLocationMaps(DBInfoModel dbInfo, DA_AddressModel Model)
        {
            PluginHelper pluginHelper = new PluginHelper();
            DA_AddressModel address = new DA_AddressModel();
            try
            {
                object ImplementedClassInstance = pluginHelper.InstanciatePlugin(typeof(MapGeocode));
                object[] InvokedMethodParameters = { dbInfo, logger, Model };
                address = pluginHelper.InvokePluginMethod<DA_AddressModel>(ImplementedClassInstance, "InvokeMapGeocode", new[] { typeof(DBInfoModel), typeof(ILog), typeof(DA_AddressModel) }, InvokedMethodParameters);
            }
            catch (Exception e)
            {
                logger.Error("Error calling MapGeocode plugin: " + e.ToString());
                return address;
            }
            return address;
        }

        public long GetCustomerAddressById(DBInfoModel dbinfo, long Id)
        {
          
            return addressesDT.GetCustomerAddressById(dbinfo, Id);
        }

        public long GetCustomerAddressByExtId(DBInfoModel dbinfo, string ExtId2)
        {
            return addressesDT.GetCustomerAddressByExtId(dbinfo, ExtId2);
        }
    }
}
