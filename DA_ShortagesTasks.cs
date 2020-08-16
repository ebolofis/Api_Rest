using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Flows.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_ShortagesTasks : IDA_ShortagesTasks
    {
        IDA_ShortagesDT shortagesDT;

        public DA_ShortagesTasks(IDA_ShortagesDT _shortagesDT)
        {
            this.shortagesDT = _shortagesDT;
        }

        /// <summary>
        /// Get a List of Shortages 
        /// </summary>
        /// <param name="Store">DB info</param>
        /// <returns></returns>
        public List<DA_ShortagesExtModel> GetShortages(DBInfoModel dbInfo)
        {
            return shortagesDT.GetShortages(dbInfo);
        }

        /// <summary>
        /// Get a List of Shortages for a store
        /// </summary>
        /// <param name="dbInfo">DB info</param>
        /// <param name="StoreId">Store Id</param>
        /// <returns></returns>
        public List<DA_ShortagesExtModel> GetShortagesByStore(DBInfoModel dbInfo, long StoreId)
        {
            return shortagesDT.GetShortagesByStore(dbInfo, StoreId);
        }


        /// <summary>
        /// Get Shortage by id
        /// </summary>
        /// <param name="dbInfo">DB info</param>
        /// <param name="Id">DA_ShortageProds.Id</param>
        /// <returns></returns>
        public DA_ShortagesExtModel GetShortage(DBInfoModel dbInfo, int Id)
        {
            return shortagesDT.GetShortage(dbInfo, Id);
        }


        /// <summary>
        /// Insert new Shortage 
        /// </summary>
        /// <param name="dbInfo">DB info</param>
        /// <param name="model">DA_ShortageProdsModel to insert</param>
        /// <returns></returns>
        public long Insert(DBInfoModel dbInfo, DA_ShortageProdsModel model)
        {
            return shortagesDT.Insert(dbInfo, model);
        }

        /// <summary>
        /// Delete a Shortage by id
        /// </summary>
        /// <param name="dbInfo">DB info</param>
        /// <param name="Id">DA_ShortageProds.Id</param>
        /// <returns>return num of records affected</returns>
        public int Delete(DBInfoModel dbInfo, int Id)
        {
            return shortagesDT.Delete(dbInfo, Id);
        }


        /// <summary>
        /// Delete all temporary Shortages for a store
        /// </summary>
        /// <param name="dbInfo">DB info</param>
        /// <param name="Id">Store Id</param>
        /// <returns>return num of records affected</returns>
        public int DeleteTemp(DBInfoModel dbInfo, long StoreId)
        {
            return shortagesDT.DeleteTemp(dbInfo, StoreId);
        }
    }
}
