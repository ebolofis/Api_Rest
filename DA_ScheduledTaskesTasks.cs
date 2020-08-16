using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_ScheduledTaskesTasks : IDA_ScheduledTaskesTasks
    {
        IDA_ScheduledTaskesDT dt;

        public DA_ScheduledTaskesTasks(IDA_ScheduledTaskesDT dt)
        {
            this.dt = dt;
        }

        /// <summary>
        /// Return's List of records to update Store
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <returns></returns>
        public List<RecordsForUpdateStoreModel> GetListDataToUpdateFromServer(DBInfoModel dbInfo, out List<RecordsForUpdateStoreModel> Deleted, long? ClientId)
        {
            return dt.GetListDataToUpdateFromServer(dbInfo, out Deleted, ClientId);
        }
    }
}
