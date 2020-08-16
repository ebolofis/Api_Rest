using Symposium.Models.Enums;
using Symposium.Helpers;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using Symposium.Helpers.Classes;
using log4net;
using Symposium.Helpers.Interfaces;
using Symposium.Plugins;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_CustomerTasks : IDA_CustomerTasks
    {
        IDA_CustomerDT custDT;
        IDA_LoyaltyDT loyaltyDT;
        LocalConfigurationHelper configHlp;
        LoginFailuresHelper loginFailuresHlp;
        ICashedLoginsHelper cashedLoginsHelper;


        protected ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public DA_CustomerTasks(IDA_CustomerDT _custDT, IDA_LoyaltyDT _loyaltyDT, LocalConfigurationHelper configHlp, LoginFailuresHelper loginFailuresHlp, ICashedLoginsHelper cashedLoginsHelper)
        {
            this.custDT = _custDT;
            this.loyaltyDT = _loyaltyDT;
            this.configHlp = configHlp;
            this.loginFailuresHlp = loginFailuresHlp;
            this.cashedLoginsHelper = cashedLoginsHelper;
        }

        /// <summary>
        /// Authenticate User. On failure return 0.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns>CustomerId</returns>
        public long LoginUser(DBInfoModel dbInfo, DALoginModel loginModel)
        {
            loginModel.Password = MD5Helper.SHA1(loginModel.Password);

            loginFailuresHlp.HistoricFailure(loginModel);

            //1. search the cashed list of logins
            long id = cashedLoginsHelper.LoginExists(loginModel);
            if (id > -1) return id;

            //2. search the DB
            long custId = custDT.LoginUser(dbInfo, loginModel);

            if (custId <= 0)
            {
                logger.Warn("Login Fail for Customer " + (loginModel.Email ?? "<NULL>"));
                loginFailuresHlp.AddFailure(loginModel);
                loginFailuresHlp.ManyFailures(loginModel);
                throw new BusinessException(Symposium.Resources.Errors.USERLOGINFAILED);
            }
            else
                cashedLoginsHelper.AddLogin(loginModel, custId); //3. add login to the cashed list of logins

            return custId;
        }

        /// <summary>
        /// Authenticate User with given authToken. On failure return 0.
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="authToken">authToken</param>
        /// <returns></returns>
        public long LoginUser(DBInfoModel dbInfo, string authToken)
        {
            authToken = MD5Helper.SHA1(authToken);

            //1. search the cashed list of logins
            long id = cashedLoginsHelper.LoginExists(authToken);
            if (id > -1) return id;

            //2. search the DB
            long custId = custDT.LoginUser(dbInfo, authToken);

            if (custId <= 0)
            {
                logger.Warn("Login Fail for Customer with AuthToken " + (authToken ?? "<NULL>"));
                throw new BusinessException(Symposium.Resources.Errors.USERLOGINFAILED);
            }
            else
                cashedLoginsHelper.AddLogin(authToken, custId); //3. add login to the cashed list of logins

            return custId;
        }

        /// <summary>
        /// check if the email exists in DB. If so then throw exception
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public void CheckUniqueEmail(DBInfoModel dbInfo, string email)
        {
            if (email == null || email == "") return;
            int c = custDT.getEmailCount(dbInfo, email);
            if (c > 0) throw new BusinessException(Symposium.Resources.Errors.UNIQUEEMAIL);
        }


        /// <summary>
        /// Search Customers 
        /// type:
        /// 0: search by firstname or lastname
        /// 1: search by Address and AddressNo 
        /// 2: ΑΦΜ
        /// 3: Phone1 ή Phone2 ή Mobile
        /// search: Λεκτικό αναζήτησης
        /// </summary>
        /// <param name="type"></param>
        /// <param name="search">Λεκτικό αναζήτησης</param>
        /// <returns>List of customers + addresses </returns>
        public List<DASearchCustomerModel> SearchCustomers(DBInfoModel dbInfo, DA_CustomerSearchTypeEnum type, string search)
        {
            configHlp.CheckDeliveryAgent();

           // List<DASearchCustomerModel> res2 = null;
            List<DASearchCustomerModel> res = custDT.SearchCustomers(dbInfo, type, search);

            //if (search.IndexOfAny(fonienta) != -1)
            //{
            //    search = search.Replace("ά", "α").Replace("Ά", "Α")
            //        .Replace("έ", "ε").Replace("Έ", "Ε")
            //        .Replace("ό", "ο").Replace("Ό", "Ο")
            //        .Replace("ί", "ι").Replace("Ί", "Ι")
            //        .Replace("ή", "η").Replace("Ή", "Η")
            //        .Replace("ύ", "υ").Replace("Ύ", "Υ")
            //        .Replace("ώ", "ω").Replace("Ώ", "Ω");
            //    res2 = custDT.SearchCustomers(dbInfo, type, search);
            //    res.AddRange(res2);
            //}
            return res;
        }

        /// <summary>
        /// return true if mobile exists into an active customer (isDeleted=0)
        /// </summary>
        /// <param name="mobile">mobile</param>
        /// <returns></returns>
        public bool mobileExists(DBInfoModel dbInfo, string mobile)
        {
            return custDT.mobileExists(dbInfo, mobile);
        }

        /// <summary>
        /// return true if email exists into an active customer (isDeleted=0)
        /// </summary>
        /// <param name="email">email</param>
        /// <returns></returns>
        public bool emailExists(DBInfoModel dbInfo, string email)
        {
            return custDT.emailExists(dbInfo, email);
        }


        /// <summary>
        /// get Customer by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DACustomerModel GetCustomer(DBInfoModel dbInfo, long id)
        {
            configHlp.CheckDeliveryAgent();
            return custDT.GetCustomer(dbInfo, id);
        }


        /// <summary>
        /// Gets customers with given email and mobile
        /// </summary>
        /// <param name="dbInf"></param>
        /// <param name="email"></param>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public List<DACustomerModel> GetCustomersByEmailMobile(DBInfoModel dbInfo, string email, string mobile)
        {
            return custDT.GetCustomersByEmailMobile(dbInfo, email, mobile);
        }

       

        /// <summary>
        /// Add Customer 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long AddCustomer(DBInfoModel dbInfo, DACustomerModel Model)
        {
            Model.CreateDate = DateTime.Now;
            long customerId = custDT.AddCustomer(dbInfo, Model);
            Model.Id = customerId;
            //Εισάγουμε τους αρχικούς πόντους loyalty.
            loyaltyDT.InsertInitPoints(dbInfo, Model);

            return customerId;
        }

        /// <summary>
        /// Update Customer 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long UpdateCustomer(DBInfoModel dbInfo, DACustomerModel Model)
        {
            //1. preserve ExtIds
            DACustomerModel modelDB= GetCustomer(dbInfo, Model.Id);
            if (!string.IsNullOrWhiteSpace(modelDB.ExtId1) && string.IsNullOrWhiteSpace(Model.ExtId1)) Model.ExtId1 = modelDB.ExtId1;
            if (!string.IsNullOrWhiteSpace(modelDB.ExtId2) && string.IsNullOrWhiteSpace(Model.ExtId2)) Model.ExtId2 = modelDB.ExtId2;
            if (!string.IsNullOrWhiteSpace(modelDB.ExtId3) && string.IsNullOrWhiteSpace(Model.ExtId3)) Model.ExtId3 = modelDB.ExtId3;
            if (!string.IsNullOrWhiteSpace(modelDB.ExtId4) && string.IsNullOrWhiteSpace(Model.ExtId4)) Model.ExtId4 = modelDB.ExtId4;
            //2. update customer
            return custDT.UpdateCustomer(dbInfo, Model);
        }

        /// <summary>
        /// Delete Customer 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public long DeleteCustomer(DBInfoModel dbInfo, long Id)
        {
            loyaltyDT.DeleteCustomerGainPoints(dbInfo, Id);

            return custDT.DeleteCustomer(dbInfo, Id);
        }

        /// <summary>
        /// sanitize customer for insert
        /// </summary>
        /// <param name="customer"></param>
        public void SanitizeInsertCustomer(IDA_CustomerModel customer)
        {
            SanitizeUpdateCustomer(customer);
            customer.Id = 0;
            customer.BillingAddressesId = 0;

            //if password is empty then set as password the email
            if (customer.Password != null) customer.Password = customer.Password.Trim();
            if (customer.Password == null || customer.Password == "") customer.Password = customer.Email;
            customer.Password = MD5Helper.SHA1(customer.Password);

            //move phone to mobile
            if(customer.Mobile==null && 
               customer.Phone1!=null && 
               (customer.Phone1.StartsWith(configHlp.PhonePrefix()+ configHlp.MobilePrefix()) || customer.Phone1.StartsWith(configHlp.MobilePrefix())))
            {
                customer.Mobile = customer.Phone1;
                customer.Phone1 = null;
            }
        }

        /// <summary>
        /// sanitize customer for update
        /// </summary>
        /// <param name="customer"></param>
        public void SanitizeUpdateCustomer(IDA_CustomerModel customer)
        {
            if (customer.Email != null) customer.Email = customer.Email.ToLower().Trim();
            if (customer.FirstName != null) customer.FirstName = customer.FirstName.Trim();
            if (customer.LastName != null) customer.LastName = customer.LastName.Trim();
            if (customer.Phone1 != null) customer.Phone1 = customer.Phone1.Trim();
            if (customer.Phone2 != null) customer.Phone2 = customer.Phone2.Trim();
            if (customer.Mobile != null) customer.Mobile = customer.Mobile.Trim();
            if (customer.VatNo != null) customer.VatNo = customer.VatNo.Trim();
            if (customer.Notes != null) customer.Notes = customer.Notes.Trim();
            customer.IsDeleted = false;
            customer.SessionKey = null;
        }

        /// <summary>
        /// check if at least one phone exists to customer model
        /// </summary>
        /// <param name="customer"></param>
        public void CheckPhoneExistanse(IDA_CustomerModel customer)
        {
            if (
                (customer.Phone1 == null || customer.Phone1 == "") &&
                (customer.Phone2 == null || customer.Phone2 == "") &&
                (customer.Mobile == null || customer.Mobile == "")
                ) throw new BusinessException(Symposium.Resources.Errors.PHONESNULL);
        }


        /// <summary>
        /// Change SessionKey 
        /// </summary>
        /// <param name="Store">db</param>
        /// <param name="Id">CustomerId</param>
        ///  <param name="SessionKey">the new SessionKey</param>
        /// <returns></returns>
        public void UpdateSessionKey(DBInfoModel Store, long Id, string SessionKey)
        {
            custDT.UpdateSessionKey(Store, Id, SessionKey);
        }

        /// <summary>
        /// Create SessionKey 
        /// </summary>
        /// <returns></returns>
        public string CreateSessionKey()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        /// <summary>
        /// Create Authorization Token
        /// </summary>
        /// <returns></returns>
        public string CreateAuthToken()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        /// <summary>
        /// Authenticate User based on email and SessionKey 
        /// </summary>
        /// <param name="Store">db</param>
        /// <param name="email"></param>
        /// <param name="SessionKey">SessionKey</param>
        /// <returns>CustomerId</returns>
        public long LoginUserSessionKey(DBInfoModel Store, DALoginSessionKeyModel loginModel)
        {

            return custDT.LoginUserSessionKey(Store, loginModel);
        }


        /// <summary>
        /// Change Password (also change SessionKey="") 
        /// </summary>
        /// <param name="Store">db</param>
        /// <param name="Id">CustomerId</param>
        ///  <param name="Password">the new Password</param>
        /// <returns></returns>
        public void UpdatePassword(DBInfoModel Store, long Id, string Password)
        {
            cashedLoginsHelper.RemoveLogin(Id);
            Password = MD5Helper.SHA1(Password);
            custDT.UpdatePassword(Store, Id, Password);
        }


        /// <summary>
        /// Reset password of customer with Id = customerId and clear email of other customers
        /// </summary>
        /// <param name="customers"></param>
        /// <param name="customerId"></param>
        /// <param name="newPassword"></param>
        public void UpdateOnePasswordClearOtherEmails(DBInfoModel Store, List<DACustomerModel> customers, long customerId, string newPassword)
        {
            cashedLoginsHelper.RemoveLogin(customerId);
            string encryptedPassword = MD5Helper.SHA1(newPassword);
            custDT.UpdateOnePasswordClearOtherEmails(Store, customers, customerId, encryptedPassword);
        }

        /// <summary>
        /// Get external id 2 of customer with given email
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public string GetExternalId2(DBInfoModel Store, string email)
        {
            return custDT.GetExternalId2(Store, email);
        }


        /// <summary>
        /// check if password of customer with given email exists
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public bool hasPassword(DBInfoModel Store, string email)
        {
            return custDT.hasPassword(Store, email);
        }

        /// <summary>
        /// Return first customerId for a customer with mobile and empty email. Otherwise return 0.
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public long ExistMobile(DBInfoModel Store, string mobile)
        {
            return custDT.ExistMobile(Store, mobile);
        }


        /// <summary>
        /// Return first customerId for a customer with email and empty mobile. Otherwise return 0.
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public long ExistEmail(DBInfoModel Store, string email)
        {
            return custDT.ExistEmail(Store, email);
        }



        /// <summary>
        /// Return first customerId for a Customer with email and mobile. Otherwise return 0.
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="existModel"></param>
        /// <returns></returns>
        public long ExistMobileEmail(DBInfoModel Store, DACustomerIdentifyModel existModel)
        {
            return custDT.ExistMobileEmail(Store, existModel);
        }


        /// <summary>
        /// Get customer info from 3rd party source by phone number
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="phone"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public DACustomerModel GetCustomerInfoExternalByPhone(DBInfoModel dbInfo, string phone, Dictionary<string, dynamic> configuration)
        {
            PluginHelper pluginHelper = new PluginHelper();
            DACustomerModel customer = null;
            try
            {
                object ImplementedClassInstance = pluginHelper.InstanciatePlugin(typeof(ExternalCustomer));
                object[] InvokedMethodParameters = { dbInfo, logger, phone, configuration };
                customer = pluginHelper.InvokePluginMethod<DACustomerModel>(ImplementedClassInstance, "InvokeExternalCustomerInfo", new[] { typeof(DBInfoModel), typeof(ILog), typeof(string), typeof(Dictionary<string, dynamic>) }, InvokedMethodParameters);
            }
            catch (Exception e)
            {
                logger.Error("Error calling ExternalCustomer plugin: " + e.ToString());
                return customer;
            }
            return customer;
        }

        public void UpdateLastOrderNote(DBInfoModel dbInfo, long customerId, string lastOrderNote)
        {
            custDT.UpdateLastOrderNote(dbInfo, customerId, lastOrderNote);
            return;
        }

    }
}
