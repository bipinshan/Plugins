using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
namespace Microsoft.Crm.Sdk.Samples
{
    public class ContactPreCreateUpdate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
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

            //Variable Declarations
            string webResourceName = string.Empty;
            Entity contactType = null;
            string content = string.Empty;
            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity entity = (Entity)context.InputParameters["Target"];
                //</snippetAccountNumberPlugin2>

                // Verify that the target entity represents an contact.
                // If not, this plug-in was not registered correctly.
                if (entity.LogicalName == "contact")
                {
                    if (entity.Attributes.Contains("ims_contactgroups"))
                    {
                        tracingService.Trace("Contact Groups Found in Context");
                        EntityReference erfContactGroups = entity.GetAttributeValue<EntityReference>("ims_contactgroups");
                        if (erfContactGroups.Id != Guid.Empty)
                        {
                            //Get Web Resource Name from Contact type record
                            contactType = service.Retrieve("ims_contacttype", erfContactGroups.Id, new ColumnSet("ims_webresourcename"));
                            if (contactType != null && contactType.Attributes.Contains("ims_webresourcename"))
                            {
                                webResourceName = contactType.GetAttributeValue<string>("ims_webresourcename");
                                tracingService.Trace("Contact Groups Web Res Name Config:"+webResourceName);
                                if (webResourceName != string.Empty)
                                {
                                    //Get Web Resource content based on name
                                    var query = new QueryExpression("webresource");
                                    query.TopCount = 1;
                                    query.ColumnSet.AddColumns("name", "content");
                                    query.Criteria.AddCondition("name", ConditionOperator.Equal, webResourceName.Trim());
                                    var webResource = service.RetrieveMultiple(query);

                                    if (webResource.Entities.Count > 0)
                                    {
                                        if (webResource.Entities[0].Attributes.Contains("content"))
                                        {
                                            tracingService.Trace("Web Res Content Found");
                                            content = webResource.Entities[0].GetAttributeValue<string>("content");
                                            Entity objContact = new Entity("contact");
                                            objContact.Id = entity.Id;
                                            objContact["entityimage"] = Convert.FromBase64String(content);
                                            service.Update(objContact);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
