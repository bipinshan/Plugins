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

namespace InfosysMS
{
    public class LeadStagingPostCreate : IPlugin
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
                Entity leadStaging = null;
                Common objCommon = new Common();
                if (context.MessageName.ToLower() == "update" && context.Depth > 1)
                    return;
                try
                {
                    if (context.PostEntityImages.Contains("PostImage") && context.PostEntityImages["PostImage"] is Entity)
                    {
                        leadStaging = (Entity)context.PostEntityImages["PostImage"];
                    }
                    else if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                    {
                        leadStaging = (Entity)context.InputParameters["Target"];
                    }
                    if (leadStaging.LogicalName != LeadStaging.EntityName)
                        return;
                    //Context.FetchConfigDetails()
                    UpsertLeadDetails(leadStaging, service, ref errorMessage);
                    if (errorLogConfig)
                    {
                        objCommon.CreateErrorLog(leadStaging.Id.ToString(), errorMessage, service);
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

            public void UpsertLeadDetails(Entity leadStaging, IOrganizationService service, ref string errorMessage)
            {
                //Fetch Mapping Details [ImportDetailsMapping]
                Guid importMasterDataId = new Guid("");
                Entity objLead = null;
                Common objCommon = new Common();
                if (!objCommon.FetchMappings(importMasterDataId, ref mappings, service, ref errorMessage))
                {
                    objCommon.UpdateStagingLog(leadStaging.Id, Guid.Empty, LeadStaging.EntityName, errorMessage, service);
                    return;
                }
                //Check for Mandatory fields and Max Length Allowed
                objCommon.MandatoryValidation(leadStaging, mappings, ref validationStatus, ref canReturn, ref errorMessage);
                //Form Lead Object based on mappings which is require for Create/Update
                objLead = objCommon.FormTargetEntityObject(LeadStaging.EntityName, leadStaging, mappings, service, ref validationStatus, ref canReturn, ref errorMessage);
                if (canReturn)
                {
                    objCommon.UpdateStagingLog(leadStaging.Id, Guid.Empty, LeadStaging.EntityName, errorMessage, service);
                    return;
                }
                Guid leadId = Guid.Empty;
                //Check for Existing Active Lead record
                bool existingLead = false;
                Entity objExistingLead=GetPreviousLead("",service);
                if(objExistingLead!=null && objExistingLead.Id!=Guid.Empty)
                {
                    existingLead=true;
                    leadId=objExistingLead.Id;
                }
                //Existing Active Lead record present in SYSTEM. update dirty fields
                if (existingLead)
                {
                    objCommon.UpdateRecordIfDirty(objExistingLead, objLead, Lead.EntityName, mappings, service, ref validationStatus, ref canReturn, ref errorMessage);
                    if (canReturn)
                    {
                        objCommon.UpdateStagingLog(leadStaging.Id, Guid.Empty, LeadStaging.EntityName, errorMessage, service);
                        return;
                    }
                }
                //No Existing Active Lead Present in SYSTEM. Created new Lead with all details
                else
                {
                    try
                    {
                        leadId = service.Create(objLead);
                    }
                    catch (Exception ex)
                    {
                        objCommon.UpdateValidationMessage("Error while create Lead record " + ex.Message,ref errorMessage);
                        validationStatus = false;
                        canReturn = true;
                    }
                }
                if (canReturn)
                {
                    objCommon.UpdateStagingLog(leadStaging.Id, Guid.Empty, LeadStaging.EntityName, errorMessage, service);
                    return;
                }
                if(leadId!=Guid.Empty)
                objCommon.UpdateStagingLog(leadStaging.Id, leadId, LeadStaging.EntityName, errorMessage, service);
            }

            public Entity GetPreviousLead(string externalId, IOrganizationService service)
            {
                Entity objExistingLead = null;
                // Define Condition Values
                var QEims_leadstaging_ims_email = "john.doeryan@test.com";
                // Instantiate QueryExpression QEims_leadstaging
                var QEims_leadstaging = new QueryExpression("lead");
                // Add all columns to QEims_leadstaging.ColumnSet
                QEims_leadstaging.ColumnSet.AllColumns = true;
                // Define filter QEims_leadstaging.Criteria
                QEims_leadstaging.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, QEims_leadstaging_ims_email);
                EntityCollection ecLeadStagings = service.RetrieveMultiple(QEims_leadstaging);
                if (ecLeadStagings != null && ecLeadStagings.Entities.Count > 0)
                {
                    objExistingLead = ecLeadStagings.Entities[0];
                }
                return objExistingLead;
            }
        }
    }
}
