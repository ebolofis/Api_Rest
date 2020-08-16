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
    public class DA_CustomerTokenTasks : IDA_CustomerTokenTasks
    {
        IDA_CustomerTokenDT customerTokenDT;

        public DA_CustomerTokenTasks(IDA_CustomerTokenDT customerTokenDT)
        {
            this.customerTokenDT = customerTokenDT;
        }

        /// <summary>
        /// Gets customer token by customer id
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        public DA_CustomerTokenModel GetCustomerToken(DBInfoModel dbInfo, long customerId)
        {
            DA_CustomerTokenModel customerToken = customerTokenDT.SelectCustomerToken(dbInfo, customerId);
            return customerToken;
        }

        /// <summary>
        /// Upserts customer token
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public long SetCustomerToken(DBInfoModel dbInfo, DATokenModel model)
        {
            long customerTokenId = 0;
            long customerId = model.CustomerId;
            DA_CustomerTokenModel customerToken = customerTokenDT.SelectCustomerToken(dbInfo, customerId);
            if (customerToken != null)
            {
                customerToken.Token = model.Token;
                customerTokenId = customerTokenDT.UpdateCustomerToken(dbInfo, customerToken);
            }
            else
            {
                customerToken = new DA_CustomerTokenModel();
                customerToken.CustomerId = model.CustomerId;
                customerToken.Token = model.Token;
                customerTokenId = customerTokenDT.InsertCustomerToken(dbInfo, customerToken);
            }
            return customerTokenId;
        }

        /// <summary>
        /// Deletes customer token by customer id
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="customerId"></param>
        /// <returns></returns>
        public long DeleteCustomerToken(DBInfoModel dbInfo, long customerId)
        {
            long customerTokenId = 0;
            DA_CustomerTokenModel customerToken = customerTokenDT.SelectCustomerToken(dbInfo, customerId);
            if (customerToken != null)
            {
                long id = customerToken.Id;
                customerTokenId = customerTokenDT.DeleteCustomerToken(dbInfo, id);
            }
            return customerTokenId;
        }

    }
}
