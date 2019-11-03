using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmExtensions;
using System.Text.RegularExpressions;
using System.ServiceModel;

namespace Microsoft.Crm.Sdk.Samples
{
    public class LoanStagingPostCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

        }
        public class Program
        {
            List<Common.Mapping> mappings = new List<Common.Mapping>();
            //string validationMessage = string.Empty;
            //string infoMessage = string.Empty;
            string errorMessage = string.Empty;
            //string defaultMessage = string.Empty;
            Dictionary<string, string> dcConfigDetails = new Dictionary<string, string>();
            bool validationStatus = true;
            bool canReturn = false;
            //string errorLog = string.Empty;
            bool errorLogConfig = false;

            public void ProcessImportRecord(IServiceProvider serviceProvider)
            {
                //Extract the tracing service for use in debugging sandboxed plug-ins.
                ITracingService tracingService =
                    (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Obtain the execution context from the service provider.
                Microsoft.Xrm.Sdk.IPluginExecutionContext context = (Microsoft.Xrm.Sdk.IPluginExecutionContext)
                    serviceProvider.GetService(typeof(Microsoft.Xrm.Sdk.IPluginExecutionContext));

                // Obtain the organization service reference.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                Entity loanStaging = null;
                Common objCommon = new Common();
                if (context.MessageName.ToLower() == "update" && context.Depth > 1)
                    return;
                try
                {
                    if (context.PostEntityImages.Contains("PostImage") && context.PostEntityImages["PostImage"] is Entity)
                    {
                        loanStaging = (Entity)context.PostEntityImages["PostImage"];
                    }
                    else if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                    {
                        loanStaging = (Entity)context.InputParameters["Target"];
                    }
                    if (loanStaging.LogicalName != LoanStaging.EntityName)
                        return;
                    //Context.FetchConfigDetails()
                    UpsertLaonDetails(loanStaging, service, ref errorMessage);
                    if (errorLogConfig)
                    {
                        objCommon.CreateErrorLog(loanStaging.Id.ToString(), errorMessage, service);
                    }
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException(ex.Message);
                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException(ex.Message);
                }
            }

            public void UpsertLaonDetails(Entity loanStaging, IOrganizationService service, ref string errorMessage)
            {
                //Fetch Mapping Details [ImportDetailsMapping]
                Guid importMasterDataId = new Guid("");
                Entity objLoan = null;
                Common objCommon = new Common();
                if (!objCommon.FetchMappings(importMasterDataId, ref mappings, service, ref errorMessage))
                {
                    objCommon.UpdateStagingLog(loanStaging.Id, Guid.Empty, LoanStaging.EntityName, errorMessage, service);
                    return;
                }
                //Check for Mandatory fields and Max Length Allowed
                objCommon.MandatoryValidation(loanStaging, mappings, ref validationStatus, ref canReturn, ref errorMessage);
                //Form Loan Object based on mappings which is require for Create/Update
                objLoan = objCommon.FormTargetEntityObject(LoanStaging.EntityName, loanStaging, mappings, service, ref validationStatus, ref canReturn, ref errorMessage);
                if (canReturn)
                {
                    objCommon.UpdateStagingLog(loanStaging.Id, Guid.Empty, LoanStaging.EntityName, errorMessage, service);
                    return;
                }
                Guid loanId = Guid.Empty;
                //Check for Existing Active Loan record
                bool existingLoan = false;
                Entity objExistingLoan = GetPreviousLoan("", service);
                if (objExistingLoan != null && objExistingLoan.Id != Guid.Empty)
                {
                    existingLoan = true;
                    loanId = objExistingLoan.Id;
                }
                //Existing Active Loan record present in SYSTEM. update dirty fields
                if (existingLoan)
                {
                    objCommon.UpdateRecordIfDirty(objExistingLoan, objLoan, Lead.EntityName, mappings, service, ref validationStatus, ref canReturn, ref errorMessage);
                    if (canReturn)
                    {
                        objCommon.UpdateStagingLog(loanStaging.Id, Guid.Empty, LoanStaging.EntityName, errorMessage, service);
                        return;
                    }
                }
                //No Existing Active Loan Present in SYSTEM. Created new Lead with all details
                else
                {
                    try
                    {
                        loanId = service.Create(objLoan);
                    }
                    catch (Exception ex)
                    {
                        objCommon.UpdateValidationMessage("Error while create Loan record " + ex.Message, ref errorMessage);
                        validationStatus = false;
                        canReturn = true;
                    }
                }
                if (canReturn)
                {
                    objCommon.UpdateStagingLog(loanStaging.Id, Guid.Empty, LoanStaging.EntityName, errorMessage, service);
                    return;
                }
                if (loanId != Guid.Empty)
                    objCommon.UpdateStagingLog(loanStaging.Id, loanId, LoanStaging.EntityName, errorMessage, service);
            }

            public Entity GetPreviousLoan(string externalId, IOrganizationService service)
            {
                Entity objExistingLoan = null;
                //// Define Condition Values
                //var QEims_loanStaging_ims_email = "john.doeryan@test.com";
                //// Instantiate QueryExpression QEims_loanStaging
                //var QEims_loanStaging = new QueryExpression("lead");
                //// Add all columns to QEims_loanStaging.ColumnSet
                //QEims_loanStaging.ColumnSet.AllColumns = true;
                //// Define filter QEims_loanStaging.Criteria
                //QEims_loanStaging.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, QEims_loanStaging_ims_email);
                //EntityCollection ecloanStagings = service.RetrieveMultiple(QEims_loanStaging);
                //if (ecloanStagings != null && ecloanStagings.Entities.Count > 0)
                //{
                //    objExistingLead = ecloanStagings.Entities[0];
                //}
                return objExistingLoan;
            }
        }
    }
}
