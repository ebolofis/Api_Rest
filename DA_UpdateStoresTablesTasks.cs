using log4net;
using Symposium.Helpers.Interfaces;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Flows.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_UpdateStoresTablesTasks : IDA_UpdateStoresTablesTasks
    {
        ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IDA_ScheduledTaskesFlows schedTasks;
        IDA_HangFireJobsDT dt;
        IWebApiClientHelper webHlp;

        public DA_UpdateStoresTablesTasks(IDA_ScheduledTaskesFlows schedTasks, IDA_HangFireJobsDT dt,
            IWebApiClientHelper webHlp)
        {
            this.schedTasks = schedTasks;
            this.dt = dt;
            this.webHlp = webHlp;
        }

        /// <summary>
        /// Updates store tables with data from server 
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="DelAfterFaild"></param>
        /// <param name="ClientId"></param>
        public void UpdateTables(DBInfoModel Store, int DelAfterFaild, long? ClientId)
        {
            string APICall = "";
            int returnCode = 0;
            string ErrorMess = "";
            string result;
            StringBuilder SQL = new StringBuilder();
            string DelIDs = "";

            List<long> sendedIds = new List<long>();

            List<RecordsForUpdateStoreModel> Delete = new List<RecordsForUpdateStoreModel>();
            List<RecordsForUpdateStoreModel> record = schedTasks.GetListDataToUpdateFromServer(Store, out Delete, ClientId);
            List<DA_StoreModel> clientStores = dt.GetStoresList(Store);

            //TimeSpan tm = new TimeSpan(0, 5, 0);
            //Thread.Sleep(tm);
            //return;

            dt.DeleteSchedulerAfterFaild(Store, DelAfterFaild);
            try
            {
                foreach (RecordsForUpdateStoreModel item in record)
                {
                    DelIDs = "";
                    DA_StoreModel stToUpdate = clientStores.Find(f => f.Id == item.StoreId);
                    if (stToUpdate != null)
                    {
                        /*Accounts (Short 1)*/
                        if (item.Accounts.Count > 0)
                        {
                            try
                            {
                                item.Accounts.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Accounts.Count > 0)
                                {
                                    List<AccountSched_Model> sendedAcc = new List<AccountSched_Model>();
                                    sendedAcc.AddRange(item.Accounts.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sendedAcc.Select(s => s.MasterId).ToList());
                                    item.Accounts.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sendedAcc[0].StoreFullURL;
                                    result = webHlp.PostRequest(sendedAcc, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Accounts Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Account's Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 1);

                                        DelIDs = "";
                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                logger.Error("Account Exception : " + ex.ToString());
                            }
                        }
                        /*Categories (Short 5)*/
                        if (item.Categories.Count > 0)
                        {
                            try
                            {
                                item.Categories.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.Categories.Count > 0)
                                {
                                    List<CategoriesSched_Model> sendedCat = new List<CategoriesSched_Model>();
                                    sendedCat.AddRange(item.Categories.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sendedCat.Select(s => s.MasterId));
                                    item.Categories.RemoveAll(r => sendedIds.Contains(r.MasterId));
                                    APICall = sendedCat[0].StoreFullURL;
                                    result = webHlp.PostRequest(sendedCat, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Categories Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Categories Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 5);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Categories Exception : " + ex.ToString());
                            }
                        }
                        /*Ingredient_ProdCategoryAssoc (Short 10)*/
                        if (item.IngedProdAssoc.Count > 0)
                        {
                            try
                            {
                                item.IngedProdAssoc.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.IngedProdAssoc.Count > 0)
                                {
                                    List<IngedProdCategAssocSched_Model> sended = new List<IngedProdCategAssocSched_Model>();
                                    sended.AddRange(item.IngedProdAssoc.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.IngedProdAssoc.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Ingredient_ProdCategoryAssoc Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Ingredient_ProdCategoryAssoc Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 10);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Ingredient_ProdCategoryAssoc Exception : " + ex.ToString());
                            }
                        }
                        /*IngredientCategories (Short 15)*/
                        if (item.IngredCategories.Count > 0)
                        {
                            try
                            {
                                item.IngredCategories.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.IngredCategories.Count > 0)
                                {
                                    List<IngredCategoriesSched_Model> sended = new List<IngredCategoriesSched_Model>();
                                    sended.AddRange(item.IngredCategories.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.IngredCategories.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on IngredientCategories Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Ingredient CategoriesIds not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 15);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Ingredient CategoriesIds Exception : " + ex.ToString());
                            }
                        }
                        /*PageSet (Short 20)*/
                        if (item.PageSet.Count > 0)
                        {
                            try
                            {
                                //string tmp = Newtonsoft.Json.JsonConvert.SerializeObject(item.PageSet);
                                item.PageSet.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PageSet.Count > 0)
                                {
                                    List<PageSetSched_Model> sended = new List<PageSetSched_Model>();
                                    sended.AddRange(item.PageSet.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PageSet.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PageSet Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("PageSet Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 20);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PageSet Exception : " + ex.ToString());
                            }
                        }
                        /*PricelistMaste (Short 25)*/
                        if (item.PriceListMaster.Count > 0)
                        {
                            try
                            {
                                item.PriceListMaster.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceListMaster.Count > 0)
                                {
                                    List<PriceListMasterSched_Model> sended = new List<PriceListMasterSched_Model>();
                                    sended.AddRange(item.PriceListMaster.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceListMaster.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PricelistMaster Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Pricelist Master Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 25);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Pricelist Master Exception : " + ex.ToString());
                            }
                        }
                        /*SalesType (Short 30)*/
                        if (item.SalesType.Count > 0)
                        {
                            try
                            {
                                item.SalesType.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.SalesType.Count > 0)
                                {
                                    List<SalesTypeSched_Model> sended = new List<SalesTypeSched_Model>();
                                    sended.AddRange(item.SalesType.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.SalesType.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on SalesType Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Sales Type Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 30);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Sales Type Exception : " + ex.ToString());
                            }
                        }
                        /*Taxes (Short 35)*/
                        if (item.Taxes.Count > 0)
                        {
                            try
                            {
                                item.Taxes.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Taxes.Count > 0)
                                {
                                    List<TaxSched_Model> sended = new List<TaxSched_Model>();
                                    sended.AddRange(item.Taxes.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Taxes.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Taxes Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Taxes Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 35);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Taxes Exception : " + ex.ToString());
                            }
                        }
                        /*Units (Short 40)*/
                        if (item.Units.Count > 0)
                        {
                            try
                            {
                                item.Units.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Units.Count > 0)
                                {
                                    List<UnitsSched_Model> sended = new List<UnitsSched_Model>();
                                    sended.AddRange(item.Units.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Units.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Units Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Units Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 40);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Units Exception : " + ex.ToString());
                            }
                        }
                        /*Vat (Short 45)*/
                        if (item.Vats.Count > 0)
                        {
                            try
                            {
                                item.Vats.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Vats.Count > 0)
                                {

                                    List<VatSched_Model> sended = new List<VatSched_Model>();
                                    sended.AddRange(item.Vats.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Vats.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Vats Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Vats Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 45);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Vats Exception : " + ex.ToString());
                            }
                        }
                        /*Ingredients (Short 50)*/
                        if (item.Ingredients.Count > 0)
                        {
                            try
                            {
                                item.Ingredients.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Ingredients.Count > 0)
                                {
                                    List<IngredientsSched_Model> sended = new List<IngredientsSched_Model>();
                                    sended.AddRange(item.Ingredients.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Ingredients.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Ingredients Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Ingredients Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 50);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Ingredients Exception : " + ex.ToString());
                            }
                        }
                        /*Pages (Short 55)*/
                        if (item.Pages.Count > 0)
                        {
                            try
                            {
                                item.Pages.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Pages.Count > 0)
                                {
                                    List<PagesSched_Model> sended = new List<PagesSched_Model>();
                                    sended.AddRange(item.Pages.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Pages.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Pages Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Pages Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 55);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Pages Exception : " + ex.ToString());
                            }
                        }
                        /*PriceList (Short 60)*/
                        if (item.PriceList.Count > 0)
                        {
                            try
                            {
                                item.PriceList.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceList.Count > 0)
                                {
                                    List<PriceListSched_Model> sended = new List<PriceListSched_Model>();
                                    sended.AddRange(item.PriceList.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceList.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PriceList Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("PriceList Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 60);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PriceList Exception : " + ex.ToString());
                            }
                        }
                        /*PriceList_EffectiveHours (Short 65)*/
                        if (item.PriceListEffectHours.Count > 0)
                        {
                            try
                            {
                                item.PriceListEffectHours.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceListEffectHours.Count > 0)
                                {
                                    List<PriceList_EffHoursSched_Model> sended = new List<PriceList_EffHoursSched_Model>();
                                    sended.AddRange(item.PriceListEffectHours.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceListEffectHours.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PriceList_EffectiveHours Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("PriceList Effective Hours Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 65);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PriceList Effective Hours Exception : " + ex.ToString());
                            }
                        }
                        /*ProductCategories (Short 70)*/
                        if (item.ProductCategories.Count > 0)
                        {
                            try
                            {
                                item.ProductCategories.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.ProductCategories.Count > 0)
                                {
                                    List<ProductCategoriesSched_Model> sended = new List<ProductCategoriesSched_Model>();
                                    sended.AddRange(item.ProductCategories.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.ProductCategories.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on ProductCategories Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Categories Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 70);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Categories Exception : " + ex.ToString());
                            }
                        }
                        /*Product (Short 75)*/
                        if (item.Products.Count > 0)
                        {
                            try
                            {
                                item.Products.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Products.Count > 0)
                                {
                                    List<ProductSched_Model> sended = new List<ProductSched_Model>();
                                    sended.AddRange(item.Products.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Products.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Products Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Products Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 75);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Products Exception : " + ex.ToString());
                            }
                        }
                        /*ProductBarcodes (Short 77)*/
                        if (item.ProductBarcodes.Count > 0)
                        {
                            try
                            {
                                item.ProductBarcodes.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.ProductBarcodes.Count > 0)
                                {
                                    List<ProductBarcodesSched_Model> sended = new List<ProductBarcodesSched_Model>();
                                    sended.AddRange(item.ProductBarcodes.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.ProductBarcodes.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Product Barcodes Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Barcodes's Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 77);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Barcodes Exception : " + ex.ToString());
                            }
                        }
                        /*ProductRecipe (Short 78)*/
                        if (item.ProductRecipies.Count > 0)
                        {
                            try
                            {
                                item.ProductRecipies.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.ProductRecipies.Count > 0)
                                {
                                    List<ProductRecipeSched_Model> sended = new List<ProductRecipeSched_Model>();
                                    sended.AddRange(item.ProductRecipies.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.ProductRecipies.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Product Recipes Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Recipies's Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 78);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Recipies Exception : " + ex.ToString());
                            }
                        }
                        /*ProductExtras (Short 80)*/
                        if (item.ProductExtras.Count > 0)
                        {
                            try
                            {
                                item.ProductExtras.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.ProductExtras.Count > 0)
                                {
                                    List<ProductExtrasSched_Model> sended = new List<ProductExtrasSched_Model>();
                                    sended.AddRange(item.ProductExtras.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId).ToList());

                                    item.ProductExtras.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on ProductExtras Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Extras Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 80);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Extras Exception : " + ex.ToString());
                            }
                        }
                        /*PriceListDetails (Short 85)*/
                        if (item.PriceListDetails.Count > 0)
                        {
                            try
                            {
                                item.PriceListDetails.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceListDetails.Count > 0)
                                {
                                    List<PriceListDetailSched_Model> sended = new List<PriceListDetailSched_Model>();
                                    sended.AddRange(item.PriceListDetails.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceListDetails.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PriceListDetails Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                            logger.Error("PriceList Details Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 85);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PriceList Details Exception : " + ex.ToString());
                            }
                        }
                        /*PageButtons (Short 90)*/
                        if (item.PageButtons.Count > 0)
                        {
                            try
                            {
                                item.PageButtons.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PageButtons.Count > 0)
                                {
                                    List<PageButtonSched_Model> sended = new List<PageButtonSched_Model>();
                                    sended.AddRange(item.PageButtons.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PageButtons.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PageButtons Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Page Buttons Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 90);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Page Buttons Exception : " + ex.ToString());
                            }
                        }
                        /*PageButtonDetails (Short 95)*/
                        if (item.PageButtonDetails.Count > 0)
                        {
                            try
                            {
                                item.PageButtonDetails.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PageButtonDetails.Count > 0)
                                {
                                    List<PageButtonDetSched_Model> sended = new List<PageButtonDetSched_Model>();
                                    sended.AddRange(item.PageButtonDetails.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PageButtonDetails.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PageButtonDetails Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Page Button Details Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 95);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Page Button Details Exception : " + ex.ToString());
                            }
                        }
                        /*Promotions Headers (Short 100)*/
                        if (item.PromotionsHeaders.Count > 0)
                        {
                            try
                            {
                                item.PromotionsHeaders.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PromotionsHeaders.Count > 0)
                                {
                                    List<PromotionsHeaderSched_Model> sended = new List<PromotionsHeaderSched_Model>();
                                    sended.AddRange(item.PromotionsHeaders.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PromotionsHeaders.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Promotion Headers Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Promotions Headers Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 100);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Promotions Headers Exception : " + ex.ToString());
                            }
                        }
                        /*Promotions Combos (Short 105)*/
                        if (item.PromotionCombos.Count > 0)
                        {
                            try
                            {
                                item.PromotionCombos.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PromotionCombos.Count > 0)
                                {
                                    List<PromotionsCombosSched_Model> sended = new List<PromotionsCombosSched_Model>();
                                    sended.AddRange(item.PromotionCombos.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PromotionCombos.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Promotion Combos Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Promotions Combos Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 105);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Promotions Combos Exception : " + ex.ToString());
                            }
                        }
                        /*Promotions Discounts (Short 110)*/
                        if (item.PromotionDiscount.Count > 0)
                        {
                            try
                            {
                                item.PromotionDiscount.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PromotionDiscount.Count > 0)
                                {
                                    List<PromotionsDiscountsSched_Model> sended = new List<PromotionsDiscountsSched_Model>();
                                    sended.AddRange(item.PromotionDiscount.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PromotionDiscount.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Promotion Discounts Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Promotions Discounts Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 110);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Promotions Discounts Exception : " + ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        logger.Error("No Store record found for ID : " + item.ToString());
                    }
                }

                /*Delete Records*/
                foreach (RecordsForUpdateStoreModel item in Delete)
                {
                    DelIDs = "";
                    DA_StoreModel stToUpdate = clientStores.Find(f => f.Id == item.StoreId);
                    if (stToUpdate != null)
                    {
                        /*Promotions Descounts*/
                        if (item.PromotionDiscount.Count > 0)
                        {
                            try
                            {
                                item.PromotionDiscount.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.PromotionDiscount.Count > 0)
                                {
                                    List<PromotionsDiscountsSched_Model> sended = new List<PromotionsDiscountsSched_Model>();
                                    sended.AddRange(item.PromotionDiscount.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PromotionDiscount.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Promotion Discounts Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Promotions Discounts Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 110);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Promotions Discounts Exception : " + ex.ToString());
                            }
                        }
                        /*Promotions Combos*/
                        if (item.PromotionCombos.Count > 0)
                        {
                            try
                            {
                                item.PromotionCombos.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.PromotionCombos.Count > 0)
                                {
                                    List<PromotionsCombosSched_Model> sended = new List<PromotionsCombosSched_Model>();
                                    sended.AddRange(item.PromotionCombos.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PromotionCombos.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Promotions Combos Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Promotions Combos Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 105);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Promotions Combos Exception : " + ex.ToString());
                            }
                        }
                        /*Promotions Headers*/
                        if (item.PromotionsHeaders.Count > 0)
                        {
                            try
                            {
                                item.PromotionsHeaders.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.PromotionsHeaders.Count > 0)
                                {
                                    List<PromotionsHeaderSched_Model> sended = new List<PromotionsHeaderSched_Model>();
                                    sended.AddRange(item.PromotionsHeaders.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PromotionsHeaders.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Promotions Headers Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Promotions Headers Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 100);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Promotions Headers Exception : " + ex.ToString());
                            }
                        }
                        /*PageButtonDetails*/
                        if (item.PageButtonDetails.Count > 0)
                        {
                            try
                            {
                                item.PageButtonDetails.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.PageButtonDetails.Count > 0)
                                {
                                    List<PageButtonDetSched_Model> sended = new List<PageButtonDetSched_Model>();
                                    sended.AddRange(item.PageButtonDetails.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PageButtonDetails.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PageButtonDetails Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Page Button Details Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 95);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Page Button Details Exception : " + ex.ToString());
                            }
                        }
                        /*PageButtons*/
                        if (item.PageButtons.Count > 0)
                        {
                            try
                            {
                                item.PageButtons.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PageButtons.Count > 0)
                                {
                                    List<PageButtonSched_Model> sended = new List<PageButtonSched_Model>();
                                    sended.AddRange(item.PageButtons.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PageButtons.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PageButtons Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Page Buttons Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 90);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Page Buttons Exception : " + ex.ToString());
                            }
                        }
                        /*PriceListDetails*/
                        if (item.PriceListDetails.Count > 0)
                        {
                            try
                            {
                                item.PriceListDetails.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceListDetails.Count > 0)
                                {
                                    List<PriceListDetailSched_Model> sended = new List<PriceListDetailSched_Model>();
                                    sended.AddRange(item.PriceListDetails.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceListDetails.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PriceListDetails Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                            logger.Error("PriceList Details Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 85);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PriceList Details Exception : " + ex.ToString());
                            }
                        }
                        /*ProductExtras*/
                        if (item.ProductExtras.Count > 0)
                        {
                            try
                            {
                                item.ProductExtras.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);
                                while (item.ProductExtras.Count > 0)
                                {
                                    List<ProductExtrasSched_Model> sended = new List<ProductExtrasSched_Model>();
                                    sended.AddRange(item.ProductExtras.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.ProductExtras.RemoveAll(r => sendedIds.Contains(r.MasterId));


                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on ProductExtras Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Extras Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 80);
                                        DelIDs = "";
                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Extras Exception : " + ex.ToString());
                            }
                        }
                        /*ProductBarcodes*/
                        if (item.ProductBarcodes.Count > 0)
                        {
                            try
                            {
                                item.ProductBarcodes.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.ProductBarcodes.Count > 0)
                                {
                                    List<ProductBarcodesSched_Model> sended = new List<ProductBarcodesSched_Model>();
                                    sended.AddRange(item.ProductBarcodes.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.ProductBarcodes.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Product Barcodes Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Barcodes's Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 77);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Barcodes Exception : " + ex.ToString());
                            }
                        }
                        /*ProductRecipe*/
                        if (item.ProductRecipies.Count > 0)
                        {
                            try
                            {
                                item.ProductRecipies.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.ProductRecipies.Count > 0)
                                {
                                    List<ProductRecipeSched_Model> sended = new List<ProductRecipeSched_Model>();
                                    sended.AddRange(item.ProductRecipies.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.ProductRecipies.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Product Recipies Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Recipies's Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 78);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Recipies Exception : " + ex.ToString());
                            }
                        }
                        /*Product*/
                        if (item.Products.Count > 0)
                        {
                            try
                            {
                                item.Products.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Products.Count > 0)
                                {
                                    List<ProductSched_Model> sended = new List<ProductSched_Model>();
                                    sended.AddRange(item.Products.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Products.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Products Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Products Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 75);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Products Exception : " + ex.ToString());
                            }
                        }
                        /*ProductCategories*/
                        if (item.ProductCategories.Count > 0)
                        {
                            try
                            {
                                item.ProductCategories.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.ProductCategories.Count > 0)
                                {
                                    List<ProductCategoriesSched_Model> sended = new List<ProductCategoriesSched_Model>();
                                    sended.AddRange(item.ProductCategories.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.ProductCategories.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on ProductCategories Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Product Categories Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 70);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Product Categories Exception : " + ex.ToString());
                            }
                        }
                        /*PriceList_EffectiveHours*/
                        if (item.PriceListEffectHours.Count > 0)
                        {
                            try
                            {
                                item.PriceListEffectHours.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceListEffectHours.Count > 0)
                                {
                                    List<PriceList_EffHoursSched_Model> sended = new List<PriceList_EffHoursSched_Model>();
                                    sended.AddRange(item.PriceListEffectHours.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceListEffectHours.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PriceList_EffectiveHours Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("PriceList Effective Hours Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 65);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PriceList Effective Hours Exception : " + ex.ToString());
                            }
                        }
                        /*PriceList*/
                        if (item.PriceList.Count > 0)
                        {
                            try
                            {
                                item.PriceList.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceList.Count > 0)
                                {
                                    List<PriceListSched_Model> sended = new List<PriceListSched_Model>();
                                    sended.AddRange(item.PriceList.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceList.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PriceList Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("PriceList Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 60);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PriceList Exception : " + ex.ToString());
                            }
                        }
                        /*Pages*/
                        if (item.Pages.Count > 0)
                        {
                            try
                            {
                                item.Pages.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Pages.Count > 0)
                                {
                                    List<PagesSched_Model> sended = new List<PagesSched_Model>();
                                    sended.AddRange(item.Pages.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Pages.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Pages Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Pages Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 55);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Pages Exception : " + ex.ToString());
                            }
                        }
                        /*Ingredients*/
                        if (item.Ingredients.Count > 0)
                        {
                            try
                            {
                                item.Ingredients.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Ingredients.Count > 0)
                                {
                                    List<IngredientsSched_Model> sended = new List<IngredientsSched_Model>();
                                    sended.AddRange(item.Ingredients.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Ingredients.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Ingredients Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Ingredients Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 50);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Ingredients Exception : " + ex.ToString());
                            }
                        }
                        /*Vat*/
                        if (item.Vats.Count > 0)
                        {
                            try
                            {
                                item.Vats.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Vats.Count > 0)
                                {
                                    List<VatSched_Model> sended = new List<VatSched_Model>();
                                    sended.AddRange(item.Vats.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Vats.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Vats Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Vats Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 45);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Vats Exception : " + ex.ToString());
                            }
                        }
                        /*Units*/
                        if (item.Units.Count > 0)
                        {
                            try
                            {
                                item.Units.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Units.Count > 0)
                                {
                                    List<UnitsSched_Model> sended = new List<UnitsSched_Model>();
                                    sended.AddRange(item.Units.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Units.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Units Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Units Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 40);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Units Exception : " + ex.ToString());
                            }
                        }
                        /*Taxes*/
                        if (item.Taxes.Count > 0)
                        {
                            try
                            {
                                item.Taxes.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Taxes.Count > 0)
                                {
                                    List<TaxSched_Model> sended = new List<TaxSched_Model>();
                                    sended.AddRange(item.Taxes.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Taxes.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Taxes Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Taxes Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 35);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Taxes Exception : " + ex.ToString());
                            }
                        }
                        /*SalesType*/
                        if (item.SalesType.Count > 0)
                        {
                            try
                            {
                                item.SalesType.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.SalesType.Count > 0)
                                {
                                    List<SalesTypeSched_Model> sended = new List<SalesTypeSched_Model>();
                                    sended.AddRange(item.SalesType.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.SalesType.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on SalesType Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Sales Type Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 30);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Sales Type Exception : " + ex.ToString());
                            }
                        }
                        /*PricelistMaste*/
                        if (item.PriceListMaster.Count > 0)
                        {
                            try
                            {
                                item.PriceListMaster.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PriceListMaster.Count > 0)
                                {
                                    List<PriceListMasterSched_Model> sended = new List<PriceListMasterSched_Model>();
                                    sended.AddRange(item.PriceListMaster.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PriceListMaster.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PricelistMaster Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Pricelist Master Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 25);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Pricelist Master Exception : " + ex.ToString());
                            }
                        }
                        /*PageSet*/
                        if (item.PageSet.Count > 0)
                        {
                            try
                            {
                                //string tmp = Newtonsoft.Json.JsonConvert.SerializeObject(item.PageSet);
                                item.PageSet.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.PageSet.Count > 0)
                                {
                                    List<PageSetSched_Model> sended = new List<PageSetSched_Model>();
                                    sended.AddRange(item.PageSet.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.PageSet.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on PageSet Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("PageSet Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 20);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("PageSet Exception : " + ex.ToString());
                            }
                        }
                        /*IngredientCategories*/
                        if (item.IngredCategories.Count > 0)
                        {
                            try
                            {
                                item.IngredCategories.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.IngredCategories.Count > 0)
                                {
                                    List<IngredCategoriesSched_Model> sended = new List<IngredCategoriesSched_Model>();
                                    sended.AddRange(item.IngredCategories.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.IngredCategories.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on IngredientCategories Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Ingredient CategoriesIds not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 15);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Ingredient CategoriesIds Exception : " + ex.ToString());
                            }
                        }
                        /*Ingredient_ProdCategoryAssoc*/
                        if (item.IngedProdAssoc.Count > 0)
                        {
                            try
                            {
                                item.IngedProdAssoc.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.IngedProdAssoc.Count > 0)
                                {
                                    List<IngedProdCategAssocSched_Model> sended = new List<IngedProdCategAssocSched_Model>();
                                    sended.AddRange(item.IngedProdAssoc.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.IngedProdAssoc.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Ingredient_ProdCategoryAssoc Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Ingredient_ProdCategoryAssoc Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 10);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Ingredient_ProdCategoryAssoc Exception : " + ex.ToString());
                            }
                        }
                        /*Categories*/
                        if (item.Categories.Count > 0)
                        {
                            try
                            {
                                item.Categories.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Categories.Count > 0)
                                {
                                    List<CategoriesSched_Model> sended = new List<CategoriesSched_Model>();
                                    sended.AddRange(item.Categories.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Categories.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Categories Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Categories Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 5);
                                        DelIDs = "";

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Categories Exception : " + ex.ToString());
                            }
                        }
                        /*Accounts*/
                        if (item.Accounts.Count > 0)
                        {
                            try
                            {
                                item.Accounts.Where(w => w.DAId == 0 || w.DAId == null).ToList().ForEach(f => f.DAId = f.Id);

                                while (item.Accounts.Count > 0)
                                {
                                    List<AccountSched_Model> sended = new List<AccountSched_Model>();
                                    sended.AddRange(item.Accounts.Take(500));
                                    sendedIds.Clear();
                                    sendedIds.AddRange(sended.Select(s => s.MasterId));
                                    item.Accounts.RemoveAll(r => sendedIds.Contains(r.MasterId));

                                    APICall = sended[0].StoreFullURL;
                                    result = webHlp.PostRequest(sended, APICall, stToUpdate.Username + ":" + stToUpdate.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                                    if (returnCode != 200)
                                    {
                                        logger.Error("Error on Accounts Upsert \r\n"
                                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                                    + "Order body : " + item.ToString());
                                    }
                                    else
                                    {
                                        UpsertListResultModel res = Newtonsoft.Json.JsonConvert.DeserializeObject<UpsertListResultModel>(result);
                                        IList<string> SuccedIds = res.Results.Where(w => w.Succeded).Select(s => s.MasterID.ToString()).ToList();
                                        DelIDs += string.Join(",", SuccedIds);
                                        if (res.TotalFailed > 0)
                                        {
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(f => !f.Succeded).Select(s => s.DAID.ToString()).ToList();
                                            logger.Error("Account's Ids not succeeded : " + string.Join(",", SuccedIds));
                                            SuccedIds.Clear();
                                            SuccedIds = res.Results.Where(w => !w.Succeded && !string.IsNullOrEmpty(w.ErrorReason)).Select(s => "Store : " + s.StoreId.ToString() + " - " + s.DAID.ToString() + " [" + s.ErrorReason + "]").ToList();
                                            logger.Error(string.Join("\r\n", SuccedIds));
                                        }
                                        //Delete succeeded records
                                        if (!string.IsNullOrEmpty(DelIDs))
                                        {
                                            if (!dt.DeleteSchedulerKeys(Store, DelIDs))
                                                throw new Exception();
                                        }
                                        dt.UpdateFaildNos(Store, item.StoreId, 1);
                                        DelIDs = "";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Account Exception : " + ex.ToString());
                            }
                        }

                    }
                    else
                    {
                        logger.Error("No Store record found for ID : " + item.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error("DA_UpdateClientTables [API : " + APICall + "] \r\n" + ex.ToString());
            }
        }
    }
}
