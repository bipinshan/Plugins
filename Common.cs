using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Net;
using System.Text.RegularExpressions;

namespace XrmExtensions
{
    public class Common
    {
        public class Mapping
        {
            public string Source;
            public string Target;
            public string TargetEntity;
            public int DataType;
            public int SourceDatatype;
            public string LookupEntityAttribute;
            public string LookupEntityName;
            public bool Mandatory;
            public string CrmDisplayName;
            public int MaxLengthAllowed;
            public bool CreatelookupRecord;
            public string value;
        }

        public static Dictionary<String, String> FetchOptionSetList(IOrganizationService service, String entityName, String fieldName)
        {
            Dictionary<String, String> dcOptionDic = new Dictionary<String, String>();

            try
            {
                if (String.Equals(entityName, "GlobalOptionSet", StringComparison.OrdinalIgnoreCase))
                {
                    #region "--- Global OptionSet ---"
                    RetrieveOptionSetRequest retrieveOptionSetRequest = new RetrieveOptionSetRequest
                    {
                        Name = fieldName
                    };

                    // Execute the request.
                    RetrieveOptionSetResponse retrieveOptionSetResponse = (RetrieveOptionSetResponse)service.Execute(retrieveOptionSetRequest);

                    // Access the retrieved OptionSetMetadata.
                    OptionSetMetadata retrievedOptionSetMetadata = (OptionSetMetadata)retrieveOptionSetResponse.OptionSetMetadata;

                    // Get the current options list for the retrieved attribute.
                    OptionMetadata[] optionList = retrievedOptionSetMetadata.Options.ToArray();

                    for (int optionCount = 0; optionCount < optionList.Length; optionCount++)
                    {
                        dcOptionDic.Add(optionList[optionCount].Label.UserLocalizedLabel.Label, optionList[optionCount].Value.ToString());
                    }
                    return dcOptionDic;
                    #endregion
                }
                else
                {
                    #region "--- Entity OptionSet ---"
                    RetrieveAttributeRequest attributeRequest = new RetrieveAttributeRequest
                    {
                        EntityLogicalName = entityName,
                        LogicalName = fieldName,
                        RetrieveAsIfPublished = true
                    };
                    // Execute the request
                    RetrieveAttributeResponse attributeResponse = (RetrieveAttributeResponse)service.Execute(attributeRequest);
                    OptionMetadata[] optionList = (((Microsoft.Xrm.Sdk.Metadata.EnumAttributeMetadata)(attributeResponse.AttributeMetadata)).OptionSet.Options).ToArray();
                    for (int optionCount = 0; optionCount < optionList.Length; optionCount++)
                    {
                        dcOptionDic.Add(optionList[optionCount].Label.UserLocalizedLabel.Label, optionList[optionCount].Value.ToString());
                    }
                    return dcOptionDic;
                    #endregion
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string GetId(string lookupEntityName, string filterAttributeName, Mapping mapping, IOrganizationService service)
        {
            string filterAttributeValue = WebUtility.HtmlEncode(mapping.value);
            lookupEntityName = lookupEntityName.Trim();
            Guid entityId = Guid.Empty;
            try
            {
                var fetchXml = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
               "<entity name='" + lookupEntityName + "'>" +
               "<attribute name='" + lookupEntityName + "id" + "' />" +
               "<filter type='and'>" +
                 "<condition attribute='" + filterAttributeName + "' operator='eq' value='" + filterAttributeValue + "'/>" +
                 (lookupEntityName == "systemuser" ? "<condition attribute='isdisabled' operator='eq' value='0'/>" : lookupEntityName != "territory" ?
                 "<condition attribute='statecode' operator='eq' value='0'/>" : string.Empty) +
               "</filter></entity></fetch>";

                EntityCollection ecEntities = service.RetrieveMultiple(new FetchExpression(fetchXml));
                if (ecEntities != null && ecEntities.Entities.Count > 0)
                {
                    Entity objEntity = ecEntities.Entities[0];
                    if (objEntity != null)
                    {
                        entityId = objEntity.Id;
                    }
                }
            }
            catch (Exception e)
            {
                entityId = Guid.Empty;
            }
            return (entityId == Guid.Empty ? string.Empty : entityId.ToString());
        }

        public void MandatoryValidation(Entity sourceEntity, List<Mapping> mapings, ref bool ValidationStatus, ref bool canReturn, ref string errorMessage)
        {
            foreach (Mapping objMapping in mapings)
            {
                if (objMapping.Mandatory && objMapping.Source != null)//mandatory data check
                {
                    if (!sourceEntity.Contains(objMapping.Source))
                    {
                        //update validationmessage, validationstatus->false, canreturn->true
                        UpdateValidationMessage("Mandatory Check failed", ref errorMessage);
                        ValidationStatus = false;
                        canReturn = true;
                    }
                }
                if (objMapping.MaxLengthAllowed != 0 && sourceEntity.Contains(objMapping.Source) &&
                    (objMapping.SourceDatatype == (int)ImportDetailsMapping.SourceDataType_OptionSet.SingleLineOfText) || objMapping.SourceDatatype == (int)ImportDetailsMapping.SourceDataType_OptionSet.MultipleLineOfText)
                {
                    string value = sourceEntity.GetAttributeValue<string>(objMapping.Source);
                    if (!string.IsNullOrEmpty(value) && value.Length > objMapping.MaxLengthAllowed)//maximum length check
                    {
                        //update validationmessage, validationstatus->false, canreturn->true
                        UpdateValidationMessage("Max Length Allowed Check failed", ref errorMessage);
                        ValidationStatus = false;
                        canReturn = true;
                    }
                }
            }
        }

        public bool FetchMappings(Guid importDataMasterId, ref List<Common.Mapping> mappings, IOrganizationService service, ref string errorMessage)
        {
            try
            {
                var queryMappings = new QueryExpression(ImportDetailsMapping.EntityName);
                queryMappings.ColumnSet = new ColumnSet(true);
                queryMappings.Criteria.AddCondition(ImportDetailsMapping.ImportDataMaster, ConditionOperator.Equal, importDataMasterId);
                queryMappings.Criteria.AddCondition("statecode", ConditionOperator.Equal, (int)ImportDetailsMapping.Status_OptionSet.Active);
                EntityCollection ecMappings = service.RetrieveMultiple(queryMappings);
                if (ecMappings != null && ecMappings.Entities.Count > 0)
                {
                    foreach (Entity objMapping in ecMappings.Entities)
                    {
                        mappings.Add(new Common.Mapping
                        {
                            //int x=(int)Enum.Parse(typeof(ImportDetailsMapping.SourceDataType_OptionSet),ImportDetailsMapping.SourceDataType_OptionSet.SingleLineOfText.ToString())
                            Source = objMapping.GetAttributeValue<string>(ImportDetailsMapping.SourceField),
                            Target = objMapping.GetAttributeValue<string>(ImportDetailsMapping.TargetField),
                            TargetEntity = objMapping.GetAttributeValue<string>(ImportDetailsMapping.PrimaryName),
                            DataType = objMapping.Contains(ImportDetailsMapping.TargetDataType) ? objMapping.GetAttributeValue<OptionSetValue>
                            (ImportDetailsMapping.TargetDataType).Value : (int)ImportDetailsMapping.TargetDataType_OptionSet.SingleLineOfText,
                            SourceDatatype = objMapping.Contains(ImportDetailsMapping.SourceDataType) ? objMapping.GetAttributeValue<OptionSetValue>
                            (ImportDetailsMapping.SourceDataType).Value : (int)ImportDetailsMapping.SourceDataType_OptionSet.SingleLineOfText,
                            LookupEntityAttribute = objMapping.GetAttributeValue<string>(ImportDetailsMapping.LookupentityAttributeFilterCondition),
                            LookupEntityName = objMapping.GetAttributeValue<string>(ImportDetailsMapping.LookupEntityName),
                            Mandatory = objMapping.Contains(ImportDetailsMapping.IsDataMandatoryforallrecords) ?
                            objMapping.GetAttributeValue<bool>(ImportDetailsMapping.IsDataMandatoryforallrecords) : false,
                            CrmDisplayName = objMapping.GetAttributeValue<string>(ImportDetailsMapping.CrmDisplayName),
                            MaxLengthAllowed = objMapping.Contains(ImportDetailsMapping.MaximumLengthAllowed) ?
                            objMapping.GetAttributeValue<int>(ImportDetailsMapping.MaximumLengthAllowed) : 0,
                            CreatelookupRecord = objMapping.Contains(ImportDetailsMapping.CreateLookupRecordUnresolved) ?
                            objMapping.GetAttributeValue<bool>(ImportDetailsMapping.CreateLookupRecordUnresolved) : false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateValidationMessage("Error while fetching mappings: " + ex.Message, ref errorMessage);
                return false;
            }
            return true;
        }

        public void SetValueToTargetEntity(ref Entity entity, Common.Mapping mapping, IOrganizationService service, ref bool ValidationStatus, ref bool canReturn, ref string errorMessage)
        {
            try
            {
                if (!string.IsNullOrEmpty(mapping.value))
                {
                    if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.SingleLineOfText ||
                        mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.MultipleLineOfText)
                    {
                        entity[mapping.Target] = mapping.value;
                    }
                    else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.WholeNumber)
                    {
                        mapping.value = mapping.value.Replace('%',' ').Trim();
                        entity[mapping.Target] = Convert.ToInt32(mapping.value);
                    }
                    else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.DateTime)
                    {
                        DateTime dt;
                        if (DateTime.TryParse(mapping.value, out dt))
                        {
                            //dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                            entity[mapping.Target] = dt;
                        }
                    }
                    else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.Optonset)
                    {
                        bool matchFound = false;
                        Dictionary<string, string> dcOptionSet = Common.FetchOptionSetList(service, mapping.TargetEntity, mapping.Target);
                        KeyValuePair<string, string> optionSetVal = dcOptionSet.Where(x => x.Key.ToUpper() == mapping.value.ToUpper()).FirstOrDefault();
                        if (optionSetVal.Value != null)
                        {
                            entity[mapping.Target] = new OptionSetValue(Convert.ToInt32(optionSetVal.Value));
                            matchFound = true;
                        }
                        if (!matchFound)
                        {
                            entity[mapping.Target] = null;
                            //Add Log To UpdateValidationMessage();
                            UpdateValidationMessage("Info: No Matching Optionset found in Target Entity " + mapping.TargetEntity + " in Target Field " + mapping.CrmDisplayName, ref errorMessage);
                        }
                    }
                    else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.Lookup)
                    {
                        string sourceValue = mapping.value;
                        mapping.value = Common.GetId(mapping.LookupEntityName, mapping.LookupEntityAttribute, mapping, service);
                        if (!string.IsNullOrEmpty(mapping.value))
                        {
                            entity[mapping.Target] = new EntityReference(mapping.LookupEntityName, new Guid(mapping.value));
                        }
                        else
                        {
                            //Add Log To UpdateValidationMessage();
                            UpdateValidationMessage("Info: No Matching Master Data found in Target Entity " + mapping.LookupEntityName + " For Target Field " + mapping.CrmDisplayName, ref errorMessage);
                        }
                    }
                    else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.TwoOptions)
                    {
                        if (mapping.value.Equals("Y", StringComparison.OrdinalIgnoreCase) || mapping.value.Equals("1", StringComparison.OrdinalIgnoreCase)
                            || mapping.value.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            entity[mapping.Target] = true;
                        }
                        else if (mapping.value.Equals("N", StringComparison.OrdinalIgnoreCase) || mapping.value.Equals("0", StringComparison.OrdinalIgnoreCase)
                            || mapping.value.Equals("No", StringComparison.OrdinalIgnoreCase))
                        {
                            entity[mapping.Target] = false;
                        }
                        else
                        {
                            //Add Log To UpdateValidationMessage();
                            UpdateValidationMessage("Info: No Matching Two Option found in Target Entity " + mapping.TargetEntity + " in Target Field " + mapping.CrmDisplayName, ref errorMessage);
                        }
                    }
                    else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.Currency)
                    {
                        //Replace $ and , with Space
                        mapping.value = mapping.value.Replace('$', ' ').Replace(',', ' ');
                        //Remove Space from string
                        mapping.value = Regex.Replace(mapping.value, @"\s+", "");
                        Money currencyField = new Money();
                        currencyField.Value = Convert.ToDecimal(mapping.value);
                        entity[mapping.Target] = currencyField;
                    }
                }
            }
            catch (Exception ex)
            {
                //validationStatus-> False canReturn->true  Add log UpdateValidationMessage
                UpdateValidationMessage("Error while setting value to Target " + ex.Message, ref errorMessage);
                ValidationStatus = false;
                canReturn = true;
            }
        }

        public Entity FormTargetEntityObject(string targetEntity, Entity entityStaging, List<Common.Mapping> mappings, IOrganizationService service, ref bool ValidationStatus, ref bool canReturn, ref string errorMessage)
        {
            Entity objTargetEntity = new Entity(targetEntity);

            foreach (Common.Mapping objMapping in mappings)
            {
                try
                {
                    Common.Mapping mapping = objMapping;
                    GetValueFromSourceEntity(entityStaging, ref mapping, ref ValidationStatus, ref canReturn, ref errorMessage);
                    if (!string.IsNullOrEmpty(mapping.value))
                    {
                        SetValueToTargetEntity(ref objTargetEntity, mapping, service, ref ValidationStatus, ref canReturn, ref errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    //validationStatus-> False canReturn->true  Add log UpdateValidationMessage
                    UpdateValidationMessage("Error While FormTargetEntityObject " + ex.Message, ref errorMessage);
                    ValidationStatus = false;
                    canReturn = true;
                }
            }
            return objTargetEntity;
        }

        public void GetValueFromSourceEntity(Entity entityStaging, ref Common.Mapping mapping, ref bool ValidationStatus, ref bool canReturn, ref string errorMessage)
        {
            try
            {
                if (mapping.SourceDatatype == (int)ImportDetailsMapping.SourceDataType_OptionSet.SingleLineOfText ||
                        mapping.SourceDatatype == (int)ImportDetailsMapping.SourceDataType_OptionSet.MultipleLineOfText)
                {
                    mapping.value = entityStaging.GetAttributeValue<string>(mapping.Source);
                }
                else if (mapping.SourceDatatype == (int)ImportDetailsMapping.SourceDataType_OptionSet.WholeNumber)
                {
                    mapping.value = entityStaging.GetAttributeValue<int>(mapping.Source).ToString();
                }
                else if (mapping.SourceDatatype == (int)ImportDetailsMapping.SourceDataType_OptionSet.DateTime)
                {
                    mapping.value = entityStaging.GetAttributeValue<DateTime>(mapping.Source).ToShortDateString();
                }
                else
                {
                    mapping.value = string.Empty;
                }
            }
            catch (Exception ex)
            {
                //validationStatus-> False canReturn->true  Add log UpdateValidationMessage
                UpdateValidationMessage("Error While GetValueFromSourceEntity " + ex.Message, ref errorMessage);
                ValidationStatus = false;
                canReturn = true;
            }
        }

        public void UpdateRecordIfDirty(Entity existingRec, Entity objNewRec, string targetEntity, List<Common.Mapping> mappings, IOrganizationService service, ref bool ValidationStatus, ref bool canReturn, ref string errorMessage)
        {
            int dirtyFields = 0;// flag to know the dirty fields count
            int updateFlag = 0;
            Entity recordTobBeUpdated = new Entity(targetEntity);
            recordTobBeUpdated.Id = existingRec.Id;
            foreach (Common.Mapping mapping in mappings)
            {
                if (mapping.TargetEntity == targetEntity)
                {
                    try
                    {
                        IsDirty(existingRec, objNewRec, ref recordTobBeUpdated, mapping.Target, ref updateFlag, ref dirtyFields,ref ValidationStatus,ref canReturn, ref errorMessage);
                        if (updateFlag != (int)Constants.NewRecord.ValueIsNull)
                        {
                            if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.SingleLineOfText ||
                                    mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.MultipleLineOfText)
                            {
                                if (updateFlag == (int)Constants.NewRecord.ValueIsNotNull || (updateFlag == (int)Constants.NewRecord.ValueIsDiffFromOldValue &&
                                    !existingRec.GetAttributeValue<string>(mapping.Target).Equals(objNewRec.GetAttributeValue<string>(mapping.Target))))
                                {
                                    recordTobBeUpdated[mapping.Target] = objNewRec.GetAttributeValue<string>(mapping.Target);
                                    dirtyFields++;
                                }
                            }
                            else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.DateTime)
                            {
                                if (updateFlag == (int)Constants.NewRecord.ValueIsNotNull || (updateFlag == (int)Constants.NewRecord.ValueIsDiffFromOldValue &&
                                    !existingRec.GetAttributeValue<DateTime>(mapping.Target).ToShortDateString().Equals(objNewRec.GetAttributeValue<DateTime>(mapping.Target).ToShortDateString())))
                                {
                                    recordTobBeUpdated[mapping.Target] = objNewRec.GetAttributeValue<DateTime>(mapping.Target);
                                    dirtyFields++;
                                }
                            }
                            else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.WholeNumber)
                            {
                                if (updateFlag == (int)Constants.NewRecord.ValueIsNotNull || (updateFlag == (int)Constants.NewRecord.ValueIsDiffFromOldValue &&
                                    !existingRec.GetAttributeValue<int>(mapping.Target).ToString().Equals(objNewRec.GetAttributeValue<int>(mapping.Target).ToString())))
                                {
                                    recordTobBeUpdated[mapping.Target] = objNewRec.GetAttributeValue<int>(mapping.Target);
                                    dirtyFields++;
                                }
                            }
                            else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.TwoOptions)
                            {
                                if (updateFlag == (int)Constants.NewRecord.ValueIsNotNull || (updateFlag == (int)Constants.NewRecord.ValueIsDiffFromOldValue &&
                                    !existingRec.GetAttributeValue<bool>(mapping.Target).ToString().Equals(objNewRec.GetAttributeValue<bool>(mapping.Target).ToString())))
                                {
                                    recordTobBeUpdated[mapping.Target] = objNewRec.GetAttributeValue<bool>(mapping.Target);
                                    dirtyFields++;
                                }
                            }
                            else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.Currency)
                            {
                                if (updateFlag == (int)Constants.NewRecord.ValueIsNotNull || (updateFlag == (int)Constants.NewRecord.ValueIsDiffFromOldValue &&
                                    !existingRec.GetAttributeValue<Money>(mapping.Target).Value.ToString().Equals(objNewRec.GetAttributeValue<Money>(mapping.Target).Value.ToString())))
                                {
                                    recordTobBeUpdated[mapping.Target] = objNewRec.GetAttributeValue<Money>(mapping.Target);
                                    dirtyFields++;
                                }
                            }
                            else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.Optonset)
                            {
                                if (updateFlag == (int)Constants.NewRecord.ValueIsNotNull || (updateFlag == (int)Constants.NewRecord.ValueIsDiffFromOldValue &&
                                    !existingRec.GetAttributeValue<OptionSetValue>(mapping.Target).Value.ToString().Equals(objNewRec.GetAttributeValue<OptionSetValue>(mapping.Target).Value.ToString())))
                                {
                                    recordTobBeUpdated[mapping.Target] = new OptionSetValue(objNewRec.GetAttributeValue<OptionSetValue>(mapping.Target).Value);
                                    dirtyFields++;
                                }
                            }
                            else if (mapping.DataType == (int)ImportDetailsMapping.TargetDataType_OptionSet.Lookup)
                            {
                                if (updateFlag == (int)Constants.NewRecord.ValueIsNotNull || (updateFlag == (int)Constants.NewRecord.ValueIsDiffFromOldValue &&
                                    !existingRec.GetAttributeValue<EntityReference>(mapping.Target).Id.ToString().Equals(objNewRec.GetAttributeValue<EntityReference>(mapping.Target).Id.ToString())))
                                {
                                    recordTobBeUpdated[mapping.Target] = new EntityReference(objNewRec.GetAttributeValue<EntityReference>(mapping.Target).LogicalName,
                                        objNewRec.GetAttributeValue<EntityReference>(mapping.Target).Id);
                                    dirtyFields++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //validationStatus-> False canReturn->true  Add log UpdateValidationMessage
                        UpdateValidationMessage("Error While UpdateRecordIfDirty " + ex.Message, ref errorMessage);
                        ValidationStatus = false;
                        canReturn = true;
                    }
                }
            }
            if (dirtyFields > 0)
            {
                try
                {
                    service.Update(recordTobBeUpdated);
                }
                catch (Exception ex)
                {
                    //validationStatus-> False canReturn->true  Add log UpdateValidationMessage
                    UpdateValidationMessage("Error While UPDATING " + targetEntity + " " + ex.Message, ref errorMessage);
                    ValidationStatus = false;
                    canReturn = true;
                }
            }
        }

        public void UpdateStagingLog(Guid stagingId, Guid targetEntityId, string staginEntityName, string validationMessage, IOrganizationService service)
        {
            try
            {
                Entity objStaging = new Entity(staginEntityName);
                objStaging.Id = stagingId;
                validationMessage = string.IsNullOrEmpty(validationMessage) ? string.Empty : validationMessage.Substring(0, Math.Min(2000, validationMessage.ToString().Length));
                objStaging[LeadStaging.ErrorLog] = validationMessage;
                if (!Guid.Empty.Equals(targetEntityId))
                {
                    if (staginEntityName == LeadStaging.EntityName)
                        objStaging[LeadStaging.Lead] = new EntityReference(Lead.EntityName, targetEntityId);
                    else if (staginEntityName == LoanStaging.EntityName)
                        objStaging[LoanStaging.LoanAmount] = new EntityReference(Loan.EntityName, targetEntityId);
                }
                service.Update(objStaging);
            }
            catch (InvalidOperationException ex)
            {
                CreateLog(stagingId.ToString(), validationMessage, service);
            }
            catch (Exception e)
            {
                CreateLog(stagingId.ToString(),validationMessage,service);
            }
        }

        public void UpdateValidationMessage(string message, ref string errorMessage)
        {
            if (!string.IsNullOrEmpty(message))
            {
                errorMessage = errorMessage + (errorMessage.Contains(message) ? string.Empty : (message + Environment.NewLine));
            }
        }

        public string GetMessage(string key, Dictionary<string, string> dcConfigDetails)
        {
            if (dcConfigDetails.ContainsKey(key))
            {
                return dcConfigDetails[key];
            }
            else
            {
                return "They Key '" + key + "' is not present in the configurations. Please check the configurations.";
            }
        }

        public void IsDirty(Entity existingRec, Entity objNewRec, ref Entity recordToBeUpdated, string targetField, ref int updateFlag, ref int dirtyFields, ref bool ValidationStatus, ref bool canReturn, ref string errorMessage)
        {
            try
            {
                updateFlag = 0;
                if ((existingRec.Contains(targetField) && !objNewRec.Contains(targetField)) ||
                    ((existingRec.Contains(targetField) || !existingRec.Contains(targetField)) && objNewRec.Contains(targetField) && objNewRec[targetField] == null))
                    updateFlag = (int)Constants.NewRecord.ValueIsNull;
                else if (!existingRec.Contains(targetField) && objNewRec.Contains(targetField))
                    updateFlag = (int)Constants.NewRecord.ValueIsNotNull;
                else if (existingRec.Contains(targetField) && objNewRec.Contains(targetField))
                    updateFlag = (int)Constants.NewRecord.ValueIsDiffFromOldValue;
            }
            catch (Exception ex)
            {
                UpdateValidationMessage("Error While checking if fields are changed " + ex.Message, ref errorMessage);
                ValidationStatus = false;
                canReturn = true;
            }
        }

        public void CreateErrorLog(string externalId, string errorMessage, IOrganizationService service)
        {
            try
            {
                externalId = string.IsNullOrEmpty(externalId) ? string.Empty : externalId;
                Entity errorLog = new Entity(ErrorLog.EntityName);
                errorMessage = errorMessage.Substring(0, Math.Min(4900, errorMessage.Length));
                errorLog[ErrorLog.PrimaryName] = "LeadStagingPostCreate: " + externalId;
                errorLog[ErrorLog.ErrorDetails] = errorMessage;
                service.Create(errorLog);
            }
            catch (Exception ex)
            {
                CreateLog(externalId, ex.Message, service);
            }
        }

        public void CreateLog(string externalId, string errorMessage, IOrganizationService service)
        {
            try
            {
                externalId = string.IsNullOrEmpty(externalId) ? string.Empty : externalId;
                Entity errorLog = new Entity(ErrorLog.EntityName);
                errorMessage = errorMessage.Substring(0, Math.Min(4900, errorMessage.Length));
                errorLog[ErrorLog.PrimaryName] = "LeadStagingPostCreate: " + externalId;
                errorLog[ErrorLog.ErrorDetails] = errorMessage;
                service.Create(errorLog);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidPluginExecutionException("Error while creating Error Log " + errorMessage + " " + e.Message);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error while creating Error Log " + errorMessage + " " + ex.Message);
            }
        }
    }
}
