using Symposium.Helpers.Interfaces;
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
    public class DA_StaffTasks: IDA_StaffTasks
    {
        IDA_StaffDT staffDT;
        ICashedLoginsHelper cashedLoginsHelper;

        public DA_StaffTasks(IDA_StaffDT _staffDT, ICashedLoginsHelper cashedLoginsHelper)
        {
            this.staffDT = _staffDT;
            this.cashedLoginsHelper = cashedLoginsHelper;
        }

        /// <summary>
        /// Authenticate Staff 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>StaffId</returns>
        public long LoginStaff(DBInfoModel dbInfo, DALoginStaffModel loginStaffModel)
        {
            string login = loginStaffModel.Username + ":" + loginStaffModel.Password+":Staff";

            //1. search staff into the cashed list of logins
            long id = cashedLoginsHelper.LoginExists(login);
            if (id > 0) return id;

            //2. search the DB
            id = staffDT.LoginStaff(dbInfo, loginStaffModel);

            //3. add login to the cashed list of logins
            if (id>0) cashedLoginsHelper.AddLogin(login, id);

            return id;
        }

        public DA_StaffModel GetStaffById(DBInfoModel dbInfo, long id)
        {
            return staffDT.GetStaffById(dbInfo, id);
        }

    }
}
