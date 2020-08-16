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
    public class DA_OrderStatusTasks : IDA_OrderStatusTasks
    {
        IDA_OrderStatusDT dt;

        public DA_OrderStatusTasks(IDA_OrderStatusDT dt)
        {
            this.dt = dt;
        }

        /// <summary>
        /// Insert's a New Model To DB
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public long AddNewModel(DBInfoModel dbInfo, DA_OrderStatusModel item)
        {
            return dt.AddNewModel(dbInfo, item);
        }


        /// <summary>
        /// Insert's a list of DA_OrderStatus and return's Succeded and not
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public List<ResultsAfterDA_OrderActionsModel> AddNewList(DBInfoModel dbInfo, List<DA_OrderStatusModel> model)
        {
            return dt.AddNewList(dbInfo, model);
        }

        /// <summary>
        /// Get's a List of orders with max status onhold (based on statusdate) and hour different bwtween now and statusdate bigger than 2
        /// </summary>
        /// <param name="Store"></param>
        /// <returns></returns>
        public List<long> GetOnHoldOrdersForDelete(DBInfoModel Store, int delMinutes)
        {
            return dt.GetOnHoldOrdersForDelete(Store, delMinutes);
        }
    }
}
