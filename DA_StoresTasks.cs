using Symposium.Helpers;
using Symposium.Helpers.Classes;
using Symposium.Models.Enums;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_StoresTasks : IDA_StoresTasks
    {
        IDA_StoresDT storeDT;
        LocalConfigurationHelper configHlp;
        public DA_StoresTasks(IDA_StoresDT _storeDT, LocalConfigurationHelper configHlp)
        {
            this.storeDT = _storeDT;
            this.configHlp = configHlp;
        }

        /// <summary>
        /// Get a List of Stores
        /// </summary>
        /// <returns>DA_StoreModel</returns>
        public List<DA_StoreModel> GetStores(DBInfoModel dbInfo)
        {
            configHlp.CheckDeliveryAgent();
            return storeDT.GetStores(dbInfo);
        }

        /// <summary>
        /// Get a List of Stores With Latitude and Longtitude
        /// </summary>
        /// <returns>DA_StoreInfoModel</returns>
        public List<DA_StoreInfoModel> GetStoresPosition(DBInfoModel dbInfo)
        {
            configHlp.CheckDeliveryAgent();
            return storeDT.GetStoresPosition(dbInfo);
        }

        /// <summary>
        /// Get A Specific Store
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public DA_StoreInfoModel GetStoreById(DBInfoModel dbInfo, long Id)
        {
            configHlp.CheckDeliveryAgent();
            return storeDT.GetStoreById(dbInfo, Id);
        }

        /// <summary>
        /// Return Store Id based on Store Code. If no Code found then throw exception.
        /// </summary>
        /// <param name="dbInfo">dbInfo</param>
        /// <param name="Code">Store Code</param>
        /// <returns></returns>
        public long GetStoreIdFromCode(DBInfoModel dbInfo, string Code)
        {
            long? Id;
            Id= storeDT.GetStoreIdFromCode(dbInfo, Code);
            if (Id == null || Id == 0) throw new BusinessException($"Store Code {Code} not found.");
            return (long)Id;
        }

      

        /// <summary>
        /// Update DA_Store Set Notes to NUll
        /// <param name="StoreId"></param>
        /// </summary>
        public long UpdateDaStoreNotes(DBInfoModel dbInfo, long StoreId)
        {
            return storeDT.UpdateDaStoreNotes(dbInfo, StoreId);
        }

        /// <summary>
        /// insert new DA Store. Return new Id
        /// </summary>
        /// <param name="dbInfo">DB con string</param>
        /// <param name="StoreModel">DA_StoreModel</param>
        /// <returns></returns>
        public long Insert(DBInfoModel dbInfo, DA_StoreModel StoreModel)
        {
            return storeDT.Insert(dbInfo, StoreModel);
        }


        /// <summary>
        /// update a DA Store. Return number of rows affected
        /// </summary>
        /// <param name="dbInfo">DB con string</param>
        /// <param name="StoreModel">DA_StoreModel</param>
        /// <returns></returns>
        public long Update(DBInfoModel dbInfo, DA_StoreModel StoreModel)
        {
            return storeDT.Update(dbInfo, StoreModel);
        }

        /// <summary>
        /// delete a DA Store. Return number of rows affected
        /// </summary>
        /// <param name="dbInfo">DB con string</param>
        /// <param name="Id">DA_Store.Id</param>
        /// <returns></returns>
        public long Delete(DBInfoModel dbInfo, long Id)
        {
            return storeDT.Delete(dbInfo, Id);
        }
        public long BODelete(DBInfoModel DBInfo, long Id)
        {
            return storeDT.BODelete(DBInfo, Id);
        }


        /// <summary>
        /// Update Store's DeliveryTime, TakeOutTime, StoreStatus
        /// </summary>
        /// <param name="dbInfo">DB con string</param>
        /// <param name="daStoreId">DA_Stores.Id</param>
        /// <param name="deliveryTime">deliveryTime (min)</param>
        /// <param name="takeOutTime">takeOutTime (min)</param>
        /// <param name="storeStatus">storeStatus</param>
        public void UpdateTimesStatus(DBInfoModel dbInfo, long daStoreId, int deliveryTime, int takeOutTime, DAStoreStatusEnum storeStatus)
        {
             storeDT.UpdateTimesStatus(dbInfo, daStoreId, deliveryTime, takeOutTime, storeStatus);
        }
    }
}
