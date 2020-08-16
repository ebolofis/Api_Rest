using log4net;
using Microsoft.AspNet.SignalR;
using Symposium.Helpers;
using Symposium.Helpers.Classes;
using Symposium.Helpers.Interfaces;
using Symposium.Models.Enums;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.Plugins;
using Symposium.WebApi.DataAccess.Interfaces.DT;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Flows;
using Symposium.WebApi.MainLogic.Interfaces.Flows.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_HangFireJobsTasks : IDA_HangFireJobsTasks
    {
        ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IDA_HangFireJobsDT dt;
        IDA_OrderStatusDT dt1;
        IDA_OrdersDT dt2;
        IDA_ClientJobsTasks clientTask;
        IDA_ScheduledTaskesFlows schedTasks;
        IDA_OrderStatusTasks daOrderStatusTask;
        IOrderStatusTasks orderStatusTask;
        IOrderDT orderDT;
        IOrderFlows orderFlows;
        IDA_OrdersTasks daOrdersTask;

        IWebApiClientHelper webHlp;
        IWebApiClientRestSharpHelper webRestSharpHlp;
        IHubContext hub = GlobalHost.ConnectionManager.GetHubContext<Helpers.Hubs.DA_Hub>();

        public DA_HangFireJobsTasks(IDA_HangFireJobsDT dt, IDA_OrderStatusDT dt1, IDA_OrdersDT dt2, IDA_ClientJobsTasks clientTask,
            IWebApiClientHelper webHlp, IWebApiClientRestSharpHelper webRestSharpHlp, IDA_ScheduledTaskesFlows schedTasks,
            IDA_OrderStatusTasks daOrderStatusTask, IOrderStatusTasks orderStatusTask,
            IOrderDT orderDT, IOrderFlows orderFlows, IDA_OrdersTasks daOrdersTask)
        {
            this.dt = dt;
            this.dt1 = dt1;
            this.dt2 = dt2;
            this.clientTask = clientTask;
            this.webHlp = webHlp;
            this.webRestSharpHlp = webRestSharpHlp;
            this.schedTasks = schedTasks;
            this.daOrderStatusTask = daOrderStatusTask;
            this.orderStatusTask = orderStatusTask;
            this.orderDT = orderDT;
            this.orderFlows = orderFlows;
            this.daOrdersTask = daOrdersTask;
        }

        /// <summary>
        /// Send's orders from DA Server to Client
        /// </summary>
        public void DA_ServerOrder(DBInfoModel Store, int delMinutes)
        {
            //Delete all OnHold orders with passed hour more than 2
            DeleteOnHoldOrders(Store, delMinutes);

            List<long> DAOrderIds = dt.GetDAOrderIdsToSend(Store);

            int returnCode = 0;
            string ErrorMess = "";

            //######################## Create List of Plugins #######################//
            //#######################################################################//
            PluginHelper pluginHelper = new PluginHelper();
            bool SendOrdersResponce = false;
            List<object> ImplementedClassInstance = pluginHelper.InstanciatePluginList(typeof(SendOrdersToExternalSystem));
            //#######################################################################//

            foreach (long item in DAOrderIds)
            {
                try
                {
                    OrderFromDAToClientForWebCallModel sendModel = dt.GetOrdersToSend(Store, item);

                    if (!orderFlows.CheckClientStoreOrderStatus(Store, sendModel.StoreModel.Id ?? 0, (OrderTypeStatus)sendModel.Order.OrderType))
                    {
                        //dt.SetErrorToDA_Order(Store, sendModel.Order.Id, "Store does not support order with type " +
                        //    ((OrderTypeStatus)sendModel.Order.OrderType == OrderTypeStatus.Delivery ? "Delivery" : "Take Out"));
                        dt.SetErrorToDA_Order(Store, sendModel.Order.Id, string.Format(Symposium.Resources.Errors.STORENOTSUPPORT, ((OrderTypeStatus)sendModel.Order.OrderType == OrderTypeStatus.Delivery ? "Delivery" : "Take Out")));

                        continue;
                    }

                    //######################## Plugin for Send Orders To External Systemt ###################//
                    //#######################################################################################//
                    object[] InvokedMethodParameters = { Store, logger, webRestSharpHlp, dt, dt1, dt2, sendModel };
                    foreach (object pluginClassInstance in ImplementedClassInstance)
                    {
                        SendOrdersResponce = pluginHelper.InvokePluginMethod<bool>(pluginClassInstance, "InvokeOmnirestOrders", new[] { typeof(DBInfoModel), typeof(ILog), typeof(IWebApiClientRestSharpHelper), typeof(IDA_HangFireJobsDT), typeof(IDA_OrderStatusDT), typeof(IDA_OrdersDT), typeof(OrderFromDAToClientForWebCallModel) }, InvokedMethodParameters);
                        if (SendOrdersResponce == true)
                        {
                            throw new SendOrdersToExternalSystemException();
                        }
                    }
                    //######################################################################################//

                    sendModel.Order.ExtType = (int)ExternalSystemOrderEnum.DeliveryAgent;

                    DA_ClientsResponceModel responceModel = new DA_ClientsResponceModel();
                    responceModel.AgentId = sendModel.Order.AgentNo;
                    responceModel.Id = sendModel.Order.Id;
                    responceModel.Origin = (DA_OrderOriginEnum)sendModel.Order.Origin;
                    responceModel.Success = false;
                    responceModel.Error = "";

                    string url = sendModel.StoreModel.WebApi;
                    if (url.Substring(url.Length - 1, 1) != "/")
                        url += "/";
                    url += "api/v3/Orders/InsertDeliveryOrders";
                    string res = "";
                    try
                    {
                        res = webHlp.PostRequest(sendModel, url, sendModel.StoreModel.Username + ":" + sendModel.StoreModel.Password, null, out returnCode, out ErrorMess, "application/json", "Basic");
                    }
                    catch (Exception ex)
                    {
                        ErrorMess = ex.ToString();
                        returnCode = 404;
                    }
                    if (returnCode != 200)
                    {
                        logger.Error("DA ORDER : " + sendModel.Order.Id.ToString() + " not send \r\n"
                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                    + "Order body : " + item.ToString());
                        responceModel.Error = "DA ORDER : " + sendModel.Order.Id.ToString() + " not send \r\n"
                                    + "Error Code " + returnCode.ToString() + ", Message : " + ErrorMess + " \r\n "
                                    + "Order body : " + item.ToString();
                        dt.SetErrorToDA_Order(Store, sendModel.Order.Id, ErrorMess);
                    }
                    else
                    {
                        if (res.StartsWith("<!DOCTYPE html>"))
                        {
                            logger.Error("For Order Id : " + sendModel.Order.Id.ToString() + " the WebApi is not valid (" + url + ")");
                            responceModel.Error = "For Order Id : " + sendModel.Order.Id.ToString() + " the WebApi is not valid (" + url + ")";
                            dt.SetErrorToDA_Order(Store, sendModel.Order.Id, "WebApi (" + url + ") with credentials (username : " + sendModel.StoreModel.Username + ", password : " + sendModel.StoreModel.Password.Substring(0, 1) + "******) is not valid");
                        }
                        else
                        {
                            List<ResultsAfterDA_OrderActionsModel> results = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ResultsAfterDA_OrderActionsModel>>(res);

                            foreach (ResultsAfterDA_OrderActionsModel resOrder in results)
                            {
                                //Set property Success equals to responce from client
                                responceModel.Success = resOrder.Succeded;

                                if (resOrder.Succeded)
                                {
                                    if (!dt.UpdateOrderWithSendStatus(Store, resOrder.DA_Order_Id, resOrder.Store_Order_Id, resOrder.Store_Order_No, (short)resOrder.Store_Order_Status, out ErrorMess))
                                    {
                                        dt.SetErrorToDA_Order(Store, sendModel.Order.Id, ErrorMess);
                                    }
                                    DA_OrderStatusModel orderStatus = new DA_OrderStatusModel();
                                    orderStatus.OrderDAId = resOrder.DA_Order_Id;
                                    orderStatus.Status = (short)resOrder.Store_Order_Status;
                                    orderStatus.StatusDate = DateTime.Now;

                                    orderStatus.Id = daOrderStatusTask.AddNewModel(Store, orderStatus);
                                    if (orderStatus.Id < 1)
                                    {
                                        dt.SetErrorToDA_Order(Store, sendModel.Order.Id, "Cannot set new status " + resOrder.Store_Order_Status + " for the order");
                                    }
                                }
                                else
                                {
                                    responceModel.Error = resOrder.Errors;
                                    dt.SetErrorToDA_Order(Store, sendModel.Order.Id, resOrder.Errors);
                                }

                                /*Send Data to hub*/
                                hub.Clients.Group("Agents").clientsResponse(responceModel);
                            }
                        }
                    }

                }
                catch (SendOrdersToExternalSystemException customEx)
                {

                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
                finally
                {
                    System.Threading.Thread.Sleep(80);
                }
            }
        }

        public class SendOrdersToExternalSystemException : Exception
        {

        }

        /// <summary>
        /// Update's orders from client to DA Server
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="DA_URL"></param>
        /// <param name="DA_UserName"></param>
        /// <param name="DA_Password"></param>
        /// <param name="ExtType"></param>
        public void DA_UpdateOrderStatus(DBInfoModel Store, string DA_URL, string DA_UserName, string DA_Password, ExternalSystemOrderEnum ExtType)
        {
            //Get's all records from Store OrderStatus table. IsNull(IsSend,0) = 0 
            List<OrderStatusModel> StoreStatuses = orderStatusTask.GetNotSendStatus(Store, ExtType);

            int returnCode;
            string ErrorMess;

            //Check's if char @ exists on username begging
            if (DA_UserName.Substring(DA_UserName.Length - 1, 1) != "@")
                DA_UserName = DA_UserName + "@";

            //Check's last character of URL to be /
            if (DA_URL.Substring(DA_URL.Length - 1, 1) != "/")
                DA_URL += "/";

            //Delivery Server call to update status
            DA_URL += "api/v3/da/Orders/UpdateDA_OrderStatus";

            //Result's list to inform store for succeeded records
            List<ResultsAfterDA_OrderActionsModel> results = new List<ResultsAfterDA_OrderActionsModel>();

            //List of model to send to server for insert
            List<DA_OrderStatusModel> StatusToSend = new List<DA_OrderStatusModel>();
            foreach (OrderStatusModel item in StoreStatuses)
            {
                DA_OrderStatusModel tmp = new DA_OrderStatusModel();
                //Check's if DAOrderId is null. If it is null then 
                //from OrderId field get's an OrderModel and get's the ExtKey 
                //Field as DAOrderId.
                if ((item.DAOrderId ?? 0) == 0)
                {
                    OrderModel order = orderDT.GetOrderById(Store, item.OrderId ?? 0);
                    tmp.OrderDAId = long.Parse(string.IsNullOrEmpty(order.ExtKey) ? "0" : order.ExtKey);
                }
                else
                    tmp.OrderDAId = item.DAOrderId ?? 0;

                //If Server model.OrderDAId is equal to 0 then the store record
                //marked as succeeded. There is no ExtKey for order so this record 
                //can not updated to server so succeeded and not get it next time
                if (tmp.OrderDAId != 0)
                {
                    tmp.Status = (short)item.Status;
                    tmp.StatusDate = item.TimeChanged ?? DateTime.Now;
                    StatusToSend.Add(tmp);
                }
                else
                {
                    results.Add(new ResultsAfterDA_OrderActionsModel()
                    {
                        Store_Order_No = item.Id,
                        Succeded = true
                    });
                }
                //System.Threading.Thread.Sleep(80);
            }

            if (StatusToSend != null && StatusToSend.Count > 0)
            {
                //Api call to send data fo post
                string res = webHlp.PostRequest(StatusToSend, DA_URL, DA_UserName + ":" + DA_Password, null, out returnCode, out ErrorMess, "application/json", "Basic");

                //Succesfully respons == 200
                if (returnCode != 200)
                    logger.Error("Can not update order status from Store to DA Server. Error : " + ErrorMess);
                else
                {
                    //Ulr is not like http://sisifos:8080... this is not valid url but not return's error
                    if (res.StartsWith("<!DOCTYPE html>"))
                        logger.Error("For order status update from store to agent the WebApi is not valid (" + DA_URL + ")");
                    else
                    {
                        //Server result for succeeded and not succeeded records
                        List<ResultsAfterDA_OrderActionsModel> ServerResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ResultsAfterDA_OrderActionsModel>>(res);

                        //for not succeeded records write log file
                        if (results.Where(w => w.Succeded == false).Count() > 0)
                        {
                            string ErrorResults = "";
                            var errors = results.Where(w => w.Succeded == false).Select(s => new { s.DA_Order_Id, s.Errors, s.Store_Order_Status }).ToList();
                            foreach (var item in errors)
                                ErrorResults += "Order Id : " + item.DA_Order_Id.ToString() + ", Status : " + item.Store_Order_Status + ", Error : " + item.Errors + " \n";
                            logger.Error("Failed Order Status to Insert : \n" + ErrorResults);
                        }

                        //List with Id's to udate store orderstatus as send
                        List<long> updated = new List<long>();
                        foreach (ResultsAfterDA_OrderActionsModel item in ServerResults)
                        {
                            List<long> fld = StatusToSend.FindAll(f => f.OrderDAId == item.DA_Order_Id && f.Status == (short)item.Store_Order_Status).Select(s => s.OrderDAId).ToList();
                            updated.AddRange(StoreStatuses.Where(w => fld.Any(a => a == w.DAOrderId)).Select(s => s.Id));
                        }

                        //Add's all record's without DAOrderId found before Api Call
                        updated.AddRange(results.Select(s => s.Store_Order_No));

                        //Final list with distinct Id's to update store orderstatus
                        List<long> UpdateStoreIds = updated.Select(s => s).Distinct().ToList();

                        //Update's store orderstatus table with IsSend = true
                        orderStatusTask.UpdateListOfOrderStatusToIsSendById(Store, UpdateStoreIds, true);
                    }
                }
            }
        }

        /// <summary>
        /// Delete's Onhold orders after 2 hours passed
        /// </summary>
        /// <param name="Store"></param>
        public void DeleteOnHoldOrders(DBInfoModel Store, int delMinutes)
        {
            //string sError;
            List<long> delOrder = daOrderStatusTask.GetOnHoldOrdersForDelete(Store, delMinutes);

            string daCancelStatusesStringRaw = MainConfigurationHelper.GetSubConfiguration(MainConfigurationHelper.apiDeliveryConfiguration, "DA_Cancel");
            string daCancelStatusesString = daCancelStatusesStringRaw.Trim();
            IEnumerable<OrderStatusEnum> cancelStasus = (IEnumerable<OrderStatusEnum>)daCancelStatusesString.Split(',').Select(s => Convert.ToInt32(s)).Cast<OrderStatusEnum>().ToArray();
            foreach (long item in delOrder)
            {
                try
                {
                    daOrdersTask.CancelOrder(Store, item, cancelStasus.ToArray(), false);
                }
                catch (Exception ex)
                {
                    daOrdersTask.SetErrorMessageToDAOrder(Store, item, ex.ToString());
                    logger.Error("Can not delete OnHold order with Id : " + item.ToString() + " \r\n" + ex.ToString());
                }
                //if (!daOrdersTask.DeleteOrders(Store, item, out sError))
                //{
                //    daOrdersTask.SetErrorMessageToDAOrder(Store, item, sError);
                //    logger.Error("Can not delete OnHold order with Id : " + item.ToString() + " \r\n" + sError);
                //}
                //else
                //    logger.Info("Order with Id " + item.ToString() + " deleted because has status on hold and passed 2 hours from the last status changess");
            }
        }

    }
}
