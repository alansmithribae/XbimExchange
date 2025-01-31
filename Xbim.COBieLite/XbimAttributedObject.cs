﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.COBieLite
{
   
    public class XbimAttributedObject
    {
        private readonly IfcObjectDefinition _ifcObject;
        private Dictionary<string, IfcProperty> _properties = new Dictionary<string, IfcProperty>();
        private Dictionary<string, IfcPhysicalQuantity> _quantities = new Dictionary<string, IfcPhysicalQuantity>();
        private Dictionary<string, IfcPropertySetDefinition> _propertySets=new Dictionary<string, IfcPropertySetDefinition>();

        public XbimAttributedObject(IfcObjectDefinition obj)
        {
            _ifcObject = obj;
        }

        public IfcObjectDefinition IfcObject
        {
            get { return _ifcObject; }
        }

        public void AddPropertySetDefinition(IfcPropertySetDefinition pSetDef)
        {
            if (!pSetDef.Name.HasValue || string.IsNullOrWhiteSpace(pSetDef.Name))
            {
                CoBieLiteHelper.Logger.WarnFormat("Property Set Definition: #{0}, has no defined name. It has been ignored", pSetDef.EntityLabel);
                return;
            }
            if ( _propertySets.ContainsKey(pSetDef.Name))
            {
                CoBieLiteHelper.Logger.WarnFormat("Property Set Definition: #{0}={1}, is duplicated in Entity #{2}={3}. Duplicate ignored", pSetDef.EntityLabel, pSetDef.Name, _ifcObject.EntityLabel, _ifcObject.GetType().Name);
                return;
            }
            _propertySets.Add(pSetDef.Name,pSetDef);
            var propertySet = pSetDef as IfcPropertySet;
            var quantitySet = pSetDef as IfcElementQuantity;
            if (propertySet != null)
            {
                foreach (var prop in propertySet.HasProperties)
                {
                    var uniquePropertyName = pSetDef.Name + "." + prop.Name;
                    if (_properties.ContainsKey(uniquePropertyName))
                    {
                        CoBieLiteHelper.Logger.WarnFormat("Property: #{0}={1}.{2}, is duplicated in Entity #{3}={4}. Duplicate ignored", prop.EntityLabel, pSetDef.Name, prop.Name, _ifcObject.EntityLabel, _ifcObject.GetType().Name);
                        continue;
                    }
                    _properties[uniquePropertyName] = prop;
                }
            }
            else if (quantitySet != null)
            {
                foreach (var quantity in quantitySet.Quantities)
                {
                    if (_quantities.ContainsKey(pSetDef.Name + "." + quantity.Name))
                    {
                        CoBieLiteHelper.Logger.WarnFormat("Quantity: #{0}={1}.{2}, is duplicated in Entity #{3}={4}. Duplicate ignored", quantity.EntityLabel, pSetDef.Name, quantity.Name, _ifcObject.EntityLabel, _ifcObject.GetType().Name);
                        continue;
                    }
                    _quantities[pSetDef.Name + "." + quantity.Name]= quantity;
                }
            }
        }

        public IDictionary<string, IfcPhysicalQuantity> Quantities
        {
            get { return _quantities; }
        }

        public IDictionary<string, IfcProperty> Properties
        {
            get { return _properties; }
        }

        public IfcPropertySetDefinition GetPropertySetDefinition(string pSetName)
        {
            IfcPropertySetDefinition pSetDef;
            if (_propertySets.TryGetValue(pSetName, out pSetDef))
                return pSetDef;
            return null;
        }

        public bool GetSimplePropertyValue<TValue>(string propertyName, out TValue val) where TValue : struct
        {
            IfcProperty ifcProperty;
            val = default(TValue);
            if (_properties.TryGetValue(propertyName, out ifcProperty))
            {
                val = ConvertToSimpleType<TValue>(ifcProperty);
                return true;
            }
            return false;

        }

        public bool GetSimplePropertyValue(string propertyName, out string val)
        {
            IfcProperty ifcProperty;
            val = null;
            if (_properties.TryGetValue(propertyName, out ifcProperty))
            {
                val = ConvertToString(ifcProperty);
                return true;
            }
            return false;

        }

        private string ConvertToString(IfcPropertySingleValue ifcProperty)
        {
            IfcValue ifcValue = ifcProperty.NominalValue;
            if (ifcValue == null) return null;
            if (ifcValue is IfcTimeStamp)
            {
                var timeStamp = (IfcTimeStamp)ifcValue;
                return WriteDateTime(IfcTimeStamp.ToDateTime(timeStamp));
            }
            if (ifcValue.UnderlyingSystemType == typeof(bool) || ifcValue.UnderlyingSystemType == typeof(bool?))
            {
                if (ifcValue.Value != null && (bool)ifcValue.Value)
                    return "yes";
                return "no";
            }
            // all other cases will convert to a string
            return ifcValue.Value.ToString();
        }

        private string WriteDateTime(DateTime dateTime)
        {
            if (dateTime.TimeOfDay == new TimeSpan(0)) //it is a date only
                return string.Format("{0:0000}-{1:00}-{2:00}", dateTime.Year, dateTime.Month, dateTime.Day);
            return string.Format("{0:0000}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}{6}", dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind == DateTimeKind.Utc ? "Z" : "");
        }

        private string ConvertToString(IfcProperty ifcProperty)
        {
            var ifcPropertySingleValue = ifcProperty as IfcPropertySingleValue;
            var ifcPropertyEnumeratedValue = ifcProperty as IfcPropertyEnumeratedValue;
           
            if (ifcPropertySingleValue != null)
            {
                return ConvertToString(ifcPropertySingleValue);
            }
            if (ifcPropertyEnumeratedValue != null)
            {

                if (ifcPropertyEnumeratedValue.EnumerationValues.Count == 1)
                    return ifcPropertyEnumeratedValue.EnumerationValues[0].ToString();
                var result = "";
                foreach (var enumValue in ifcPropertyEnumeratedValue.EnumerationValues)
                {
                    result += enumValue + ";";
                }
                return result.TrimEnd(new[] { ';', ' ' });
            }

            CoBieLiteHelper.Logger.WarnFormat("Conversion Error: #{0}={1} [{2}] cannot be converted to s simple value type", ifcProperty.EntityLabel, ifcProperty.Name, ifcProperty.GetType().Name);
            return null;
        }

        public bool GetAttributeValue<TCoBieValueBaseType>(string propertyName, ref TCoBieValueBaseType val) where TCoBieValueBaseType:ValueBaseType, new()
        {
            IfcProperty ifcProperty;
            IfcPhysicalQuantity ifcQuantity;
            if (_properties.TryGetValue(propertyName, out ifcProperty))
            {
                val = ConvertAttribute<TCoBieValueBaseType>(ifcProperty);
                return true;
            }
            if (_quantities.TryGetValue(propertyName, out ifcQuantity))
            {
                val = ConvertAttribute<TCoBieValueBaseType>(ifcQuantity);
                return true;
            }
           
            return false;
            
        }

        public static TCoBieValueBaseType ConvertAttribute<TCoBieValueBaseType>(IfcProperty ifcProperty) where TCoBieValueBaseType:ValueBaseType, new()
        {
            var ifcPropertySingleValue = ifcProperty as IfcPropertySingleValue;
            var ifcPropertyEnumeratedValue = ifcProperty as IfcPropertyEnumeratedValue;
            var ifcPropertyBoundedValue = ifcProperty as IfcPropertyBoundedValue;
            var ifcPropertyTableValue = ifcProperty as IfcPropertyTableValue;
            var ifcPropertyReferenceValue = ifcProperty as IfcPropertyReferenceValue;
            var ifcPropertyListValue = ifcProperty as IfcPropertyListValue;

            var result = new TCoBieValueBaseType();
            
            if (ifcPropertySingleValue != null)
            {
                SetCoBieAttributeValue(result, ifcPropertySingleValue.NominalValue);
                if(ifcPropertySingleValue.Unit!=null)
                    result.UnitName = ifcPropertySingleValue.Unit.GetName();
            }
            else if (ifcPropertyEnumeratedValue != null)
            {
                if (ifcPropertyEnumeratedValue.EnumerationReference != null)
                    result.UnitName = ifcPropertyEnumeratedValue.EnumerationReference.Unit.GetName();
                if (ifcPropertyEnumeratedValue.EnumerationValues.Count==1)
                    SetCoBieAttributeValue(result, ifcPropertyEnumeratedValue.EnumerationValues[0]);
                else if (result is StringValueType) //if it is a string we can add all the  values in a list
                {
                    var stringValueType = result as StringValueType;
                    foreach (var enumValue in ifcPropertyEnumeratedValue.EnumerationValues)
                    {
                        stringValueType.StringValue += enumValue + ";";
                    }
                    stringValueType.StringValue = stringValueType.StringValue.TrimEnd(new[] { ';', ' ' });
                }
                else
                {
                    CoBieLiteHelper.Logger.WarnFormat("IfcPropertyEnumeratedValue Conversion: Multiple Enumerated values can only be stored in a string type");
                }
                
            }
            else if (ifcPropertyBoundedValue != null)
            {
                if (result is StringValueType) //if it is a string we can add  the bounded values in a statement
                {
                    if (ifcPropertyBoundedValue.Unit != null)
                        result.UnitName = ifcPropertyBoundedValue.Unit.GetName();
                    var stringValueType = result as StringValueType;
                    stringValueType.StringValue = ifcPropertyBoundedValue.LowerBoundValue + " to " +
                                                  ifcPropertyBoundedValue.UpperBoundValue;
                }
                else
                {
                    CoBieLiteHelper.Logger.WarnFormat("IfcPropertyBoundedValue Conversion: Bounded values can only be stored in a string type");
                }
                
            }
            else if (ifcPropertyTableValue != null)
            {
                //Logger.WarnFormat("Table values are not supported in COBie");
            }
            else if (ifcPropertyReferenceValue != null)
            {
                //Logger.WarnFormat("Reference property values are not supported in COBie");
            }
            else if (ifcPropertyListValue != null)
            {
                if (ifcPropertyListValue.Unit!=null)
                    result.UnitName = ifcPropertyListValue.Unit.GetName();
                if (ifcPropertyListValue.ListValues.Count == 1)
                    SetCoBieAttributeValue(result, ifcPropertyListValue.ListValues[0]);
                else if (result is StringValueType) //if it is a string we can add all the  values in a list
                {
                    var stringValueType = result as StringValueType;
                    foreach (var listValue in ifcPropertyListValue.ListValues)
                    {
                        stringValueType.StringValue += listValue + ";";
                    }
                    stringValueType.StringValue = stringValueType.StringValue.TrimEnd(new[] {';', ' '});
                }
                else
                {
                    CoBieLiteHelper.Logger.WarnFormat("IfcPropertyList Conversion: ValueMultiple List values can only be stored in a string type");
                }
            }
            
            return result;

        }

        private static void SetCoBieAttributeValue<TCoBieValueBaseType>(TCoBieValueBaseType result, IfcValue ifcValue) where TCoBieValueBaseType : ValueBaseType
        {
            var stringValueType  = result as StringValueType;
            var decimalValueType = result as DecimalValueType;
            var booleanValueType = result as BooleanValueType;
            var integerValueType = result as IntegerValueType;

            if (stringValueType != null)
            {
                stringValueType.StringValue = ifcValue.ToString();
            }
            else if (decimalValueType != null)
            {
                try
                {
                    decimalValueType.DecimalValue = Convert.ToDouble(ifcValue.Value);
                    decimalValueType.DecimalValueSpecified = true;
                }
                catch (ArgumentNullException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Decimal Conversion: String is null.");
                }
                catch (FormatException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Decimal Conversion: String does not consist of an " + "optional sign followed by a series of digits.");
                }
                catch (OverflowException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Decimal Conversion: Overflow in string to int conversion.");
                }

            }
            else if (booleanValueType != null)
            {
                try
                {
                    booleanValueType.BooleanValue = Convert.ToBoolean(ifcValue.Value);
                    booleanValueType.BooleanValueSpecified = true;
                }
                catch (ArgumentNullException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Boolean Conversion: String is null.");
                }
                catch (FormatException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Boolean Conversion: String does not consist of an " + "optional sign followed by a series of digits.");
                }
                catch (OverflowException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Boolean Conversion: Overflow in string to int conversion.");
                }
            }
            else if (integerValueType != null)
            {
                try
                {
                    //this looks like an error in COBieLite, suggest should be same as Decimal and Boolean
                    integerValueType.IntegerValue = Convert.ToInt32(ifcValue.Value);
                }
                catch (ArgumentNullException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Integer Conversion: String is null.");
                }
                catch (FormatException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Integer Conversion: String does not consist of an " +
                                                      "optional sign followed by a series of digits.");
                }
                catch (OverflowException)
                {
                    CoBieLiteHelper.Logger.WarnFormat("Integer Conversion: Overflow in string to int conversion.");
                }
            }
            else
                CoBieLiteHelper.Logger.Warn("Unexpected ValueBaseType");
        }
        internal static TCoBieValueBaseType ConvertAttribute<TCoBieValueBaseType>(IfcPhysicalQuantity ifcQuantity) where TCoBieValueBaseType : ValueBaseType, new()
        {
            var ifcQuantityLength = ifcQuantity as IfcQuantityLength;
            var ifcQuantityArea = ifcQuantity as IfcQuantityArea;
            var ifcQuantityVolume = ifcQuantity as IfcQuantityVolume;
            var ifcQuantityCount = ifcQuantity as IfcQuantityCount;
            var ifcQuantityWeight = ifcQuantity as IfcQuantityWeight;
            var ifcQuantityTime = ifcQuantity as IfcQuantityTime;
            var ifcPhysicalComplexQuantity = ifcQuantity as IfcPhysicalComplexQuantity;
            var ifcPhysicalSimpleQuantity = ifcQuantity as IfcPhysicalSimpleQuantity;
            var result = new TCoBieValueBaseType();
            if (ifcPhysicalSimpleQuantity != null && ifcPhysicalSimpleQuantity.Unit != null)
                result.UnitName = ifcPhysicalSimpleQuantity.Unit.GetName();
            if (ifcQuantityLength != null)
            {
                SetCoBieAttributeValue(result, ifcQuantityLength.LengthValue);
            }
            else if (ifcQuantityArea != null)
            {
                SetCoBieAttributeValue(result, ifcQuantityArea.AreaValue);
            }
            else if (ifcQuantityVolume  != null)
            {
                SetCoBieAttributeValue(result, ifcQuantityVolume.VolumeValue);
            }
            else if (ifcQuantityCount != null)
            {
                SetCoBieAttributeValue(result, ifcQuantityCount.CountValue);
            }
            else if (ifcQuantityWeight != null)
            {
                SetCoBieAttributeValue(result, ifcQuantityWeight.WeightValue);
            }
            else if (ifcQuantityTime != null)
            {
                SetCoBieAttributeValue(result, ifcQuantityTime.TimeValue);
            }
            else if (ifcPhysicalComplexQuantity != null)
            {
                //Logger.WarnFormat("Ifc Physical Complex Quantities  values are not supported in COBie");
            }
            else
            {
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Converts the property to a simple type, date, string
        /// </summary>
        /// <param name="ifcProperty"></param>
        /// <returns></returns>
        static public TValue ConvertToSimpleType<TValue>(IfcProperty ifcProperty) where TValue:struct
        {

            var ifcPropertySingleValue = ifcProperty as IfcPropertySingleValue;
            var ifcPropertyEnumeratedValue = ifcProperty as IfcPropertyEnumeratedValue;
            var ifcPropertyBoundedValue = ifcProperty as IfcPropertyBoundedValue;
            var ifcPropertyTableValue = ifcProperty as IfcPropertyTableValue;
            var ifcPropertyReferenceValue = ifcProperty as IfcPropertyReferenceValue;
            var ifcPropertyListValue = ifcProperty as IfcPropertyListValue;

            if (ifcPropertySingleValue != null)
            {
                return ConvertToSimpleType<TValue>(ifcPropertySingleValue);
            }
            if (ifcPropertyEnumeratedValue != null)
            {
               
                if (ifcPropertyEnumeratedValue.EnumerationValues.Count == 1)
                    return (TValue) Convert.ChangeType(ifcPropertyEnumeratedValue.EnumerationValues[0].ToString(), typeof(TValue));
                var result = "";
                foreach (var enumValue in ifcPropertyEnumeratedValue.EnumerationValues)
                {
                    result += enumValue + ";";
                }
                result = result.TrimEnd(new[] { ';', ' ' });
                return (TValue)Convert.ChangeType(result, typeof(TValue));
            }
            if (ifcPropertyBoundedValue != null)
            {
                CoBieLiteHelper.Logger.WarnFormat("Conversion Error: PropertyBoundedValue #{0}={1} cannot be converted to s simple value type", ifcProperty.EntityLabel, ifcProperty.Name);
            }
            else if (ifcPropertyTableValue != null)
            {
                CoBieLiteHelper.Logger.WarnFormat("Conversion Error: PropertyTableValue #{0}={1} cannot be converted to s simple value type", ifcProperty.EntityLabel, ifcProperty.Name);
            }
            else if (ifcPropertyReferenceValue != null)
            {
                CoBieLiteHelper.Logger.WarnFormat("Conversion Error: PropertyReferenceValue #{0}={1} cannot be converted to s simple value type", ifcProperty.EntityLabel, ifcProperty.Name);
            }
            else if (ifcPropertyListValue != null)
            {
                CoBieLiteHelper.Logger.WarnFormat("Conversion Error: PropertyListValue #{0}={1} cannot be converted to s simple value type", ifcProperty.EntityLabel, ifcProperty.Name);
            }
            return default(TValue);
        }


        static public AttributeType ConvertToAttributeType(IfcProperty ifcProperty)
        {
            
             var attributeType = new AttributeType
             {
                 AttributeDescription = ifcProperty.Description,
                 AttributeName = ifcProperty.Name,
                 AttributeCategory = "Submitted"  

                 //srl we need to define categories, the schema proposes "As Built|Submitted|Approved|Exact Requirement|Maximum Requirement|Minimum Requirement|Requirement", should DPoW set requirements?
             };


            var ifcPropertySingleValue = ifcProperty as IfcPropertySingleValue;
            var ifcPropertyEnumeratedValue = ifcProperty as IfcPropertyEnumeratedValue;
            var ifcPropertyBoundedValue = ifcProperty as IfcPropertyBoundedValue;
            var ifcPropertyTableValue = ifcProperty as IfcPropertyTableValue;
            var ifcPropertyReferenceValue = ifcProperty as IfcPropertyReferenceValue;
            var ifcPropertyListValue = ifcProperty as IfcPropertyListValue;
            
            if (ifcPropertySingleValue != null)
            {
                attributeType.AttributeValue = GetAttributeValueType(ifcPropertySingleValue);
            }
            else if (ifcPropertyEnumeratedValue != null)
            {
                attributeType.AttributeValue = new AttributeValueType
                {
                    ItemElementName = ItemChoiceType.AttributeStringValue
                };
                var valueItem = new AttributeStringValueType();
                attributeType.AttributeValue.Item = valueItem;
                if (ifcPropertyEnumeratedValue.EnumerationReference != null && ifcPropertyEnumeratedValue.EnumerationReference.Unit!= null)
                    valueItem.UnitName = ifcPropertyEnumeratedValue.EnumerationReference.Unit.GetName();
                if (ifcPropertyEnumeratedValue.EnumerationValues.Count==1)
                    valueItem.StringValue =  ifcPropertyEnumeratedValue.EnumerationValues[0].ToString();
                else
                {
                    valueItem.StringValue = "";
                    foreach (var enumValue in ifcPropertyEnumeratedValue.EnumerationValues)
                    {
                        valueItem.StringValue += enumValue + ";";
                    }
                    valueItem.StringValue = valueItem.StringValue.TrimEnd(new[] { ';', ' ' });
                }
                //add in the allowed values
               
                if (ifcPropertyEnumeratedValue.EnumerationReference != null && ifcPropertyEnumeratedValue.EnumerationReference.EnumerationValues.Count>0)
                {
                    var allowedValues = new AllowedValueCollectionType
                    {
                        AttributeAllowedValue = new List<string>(ifcPropertyEnumeratedValue.EnumerationReference.EnumerationValues.Count)
                    };
                    foreach (var enumValue in ifcPropertyEnumeratedValue.EnumerationReference.EnumerationValues)
                    {
                        allowedValues.AttributeAllowedValue.Add(enumValue.ToString());
                       
                    }
                }
            }
            else if (ifcPropertyBoundedValue != null)
            {
                attributeType.AttributeValue = GetAttributeValue(ifcPropertyBoundedValue);
            }
            else if (ifcPropertyTableValue != null)
            {
                //Logger.WarnFormat("Table values are not supported in COBie");
            }
            else if (ifcPropertyReferenceValue != null)
            {
                //Logger.WarnFormat("Reference property values are not supported in COBie");
            }
            else if (ifcPropertyListValue != null)
            {
                //Logger.WarnFormat("Multiple List values are not supported in COBie");
            }
            return attributeType;
        }

        static private AttributeValueType GetAttributeValue(IfcPropertyBoundedValue ifcPropertyBoundedValue)
        {
            var attributeValueType = new AttributeValueType();
            IfcValue ifcValue = ifcPropertyBoundedValue.LowerBoundValue;
            //only upper and lowwer bounds are supported by COBie on integer and decimal values
            if (ifcValue.UnderlyingSystemType == typeof(int) || ifcValue.UnderlyingSystemType == typeof(long) || ifcValue.UnderlyingSystemType == typeof(short) || ifcValue.UnderlyingSystemType == typeof(byte)
                || ifcValue.UnderlyingSystemType == typeof(int?) || ifcValue.UnderlyingSystemType == typeof(long?) || ifcValue.UnderlyingSystemType == typeof(short?) || ifcValue.UnderlyingSystemType == typeof(byte?))
            {
                var integerValue = new AttributeIntegerValueType();
                if(ifcPropertyBoundedValue.UpperBoundValue!=null)
                    integerValue.MaxValueInteger = Convert.ToInt32(ifcPropertyBoundedValue.UpperBoundValue.Value);
                if (ifcPropertyBoundedValue.LowerBoundValue != null)
                    integerValue.MinValueInteger = Convert.ToInt32(ifcPropertyBoundedValue.LowerBoundValue.Value);
                attributeValueType.Item = integerValue;
                attributeValueType.ItemElementName = ItemChoiceType.AttributeIntegerValue;
            }
            else if (ifcValue.UnderlyingSystemType == typeof(double) || ifcValue.UnderlyingSystemType == typeof(float) 
                || ifcValue.UnderlyingSystemType == typeof(double?) || ifcValue.UnderlyingSystemType == typeof(float?))
            {
                var decimalValue = new AttributeDecimalValueType();
                if (ifcPropertyBoundedValue.UpperBoundValue != null)
                {
                    decimalValue.MaxValueDecimal = (double) ifcPropertyBoundedValue.UpperBoundValue.Value;
                    decimalValue.MaxValueDecimalSpecified = true;
                }
                if (ifcPropertyBoundedValue.LowerBoundValue != null)
                {
                    decimalValue.MinValueDecimal = (double) ifcPropertyBoundedValue.LowerBoundValue.Value;
                    decimalValue.MinValueDecimalSpecified = true;
                }
                attributeValueType.Item = decimalValue;
                attributeValueType.ItemElementName = ItemChoiceType.AttributeDecimalValue;
            }
            else
            {
                attributeValueType = null;
            }

            return attributeValueType;
        }

        static public TValue ConvertToSimpleType<TValue>(IfcPropertySingleValue ifcProperty) where TValue : struct
        {
            IfcValue ifcValue = ifcProperty.NominalValue;
            var value = new TValue();
            if (ifcValue is IfcMonetaryMeasure)
            {
                 value = (TValue)Convert.ChangeType(ifcValue.Value, typeof(TValue));
            }
            else if (ifcValue is IfcTimeStamp)
            {    
                var timeStamp = (IfcTimeStamp)ifcValue;
                value = (TValue)Convert.ChangeType(IfcTimeStamp.ToDateTime(timeStamp), typeof(TValue)); 
            }
            else if (value is DateTime) //sometimes these are written as strings in the ifc file
            {
                value = (TValue)Convert.ChangeType(ReadDateTime(ifcValue.Value.ToString()), typeof(TValue));
            }
            else if (ifcValue.UnderlyingSystemType == typeof(int) || ifcValue.UnderlyingSystemType == typeof(long) || ifcValue.UnderlyingSystemType == typeof(short) || ifcValue.UnderlyingSystemType == typeof(byte))
            {
                value = (TValue)Convert.ChangeType(ifcValue.Value, typeof(TValue));             
            }
            else if (ifcValue.UnderlyingSystemType == typeof(double) || ifcValue.UnderlyingSystemType == typeof(float))
            {
                value = (TValue)Convert.ChangeType(ifcValue.Value, typeof(TValue)); 
            }
            else if (ifcValue.UnderlyingSystemType == typeof(string))
            {
                value = (TValue)Convert.ChangeType(ifcValue.Value, typeof(TValue)); 
            }
            else if (ifcValue.UnderlyingSystemType == typeof(bool) || ifcValue.UnderlyingSystemType == typeof(bool?))
            {
                
                if (ifcValue != null)
                {
                    value = (TValue)Convert.ChangeType(ifcValue.Value, typeof(TValue)); 
                }
            }
            return value;
        }

        private static DateTime ReadDateTime(string str)
        {
            var parts = str.Split(new[] {':', '-', 'T', 'Z'},StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length==6) //it is a date time
            {
                int year = Convert.ToInt32(parts[0]);
                int month = Convert.ToInt32(parts[1]);
                int day = Convert.ToInt32(parts[2]);
                int hours = Convert.ToInt32(parts[3]);
                int minutes = Convert.ToInt32(parts[4]);
                int seconds = Convert.ToInt32(parts[5]);
                return new DateTime(year, month, day, hours, minutes, seconds, str.Last() == 'Z' ? DateTimeKind.Utc : DateTimeKind.Unspecified);
            }
            if (parts.Length == 3) //it is a date
            {
                int year = Convert.ToInt32(parts[0]);
                int month = Convert.ToInt32(parts[1]);
                int day = Convert.ToInt32(parts[2]);
                return new DateTime(year, month, day);
            }
            CoBieLiteHelper.Logger.WarnFormat("Date Time Conversion: An illegal date time string has been found [{0}]", str);
            return default(DateTime);
        }


        static public AttributeValueType GetAttributeValueType(IfcPropertySingleValue ifcProperty)
        {
            IfcValue ifcValue = ifcProperty.NominalValue;
            var attributeValueType = new AttributeValueType();
            if (ifcValue == null) 
                return null;
            if (ifcValue is IfcMonetaryMeasure)
            {
                attributeValueType.ItemElementName = ItemChoiceType.AttributeMonetaryValue;
                var monetaryValue = new AttributeMonetaryValueType
                {
                    MonetaryValue = Convert.ToDecimal((double) ifcValue.Value)
                };
                attributeValueType.Item = monetaryValue;
                if (ifcProperty.Unit is IfcMonetaryUnit)
                {
                    var mu = ifcProperty.Unit as IfcMonetaryUnit;
                    CurrencyUnitSimpleType cu;
                    if (Enum.TryParse(mu.Currency.ToString(), true, out cu))
                        monetaryValue.MonetaryUnit = cu;
                    else
                        CoBieLiteHelper.Logger.WarnFormat("Invalid monetary unit: {0} ", mu.Currency);
                }
            }
            else if (ifcValue is IfcTimeStamp)
            {
                attributeValueType.ItemElementName = ItemChoiceType.AttributeDateTimeValue;
                var timeStamp = (IfcTimeStamp)ifcValue;
                attributeValueType.Item = IfcTimeStamp.ToDateTime(timeStamp);
            }
            else if (ifcValue.UnderlyingSystemType == typeof(int) || ifcValue.UnderlyingSystemType == typeof(long) || ifcValue.UnderlyingSystemType == typeof(short) || ifcValue.UnderlyingSystemType == typeof(byte))
            {
                var integerValue =  new AttributeIntegerValueType {IntegerValue = Convert.ToInt32(ifcValue.Value) };
                attributeValueType.Item = integerValue;
                attributeValueType.ItemElementName = ItemChoiceType.AttributeIntegerValue;
               
            }
            else if (ifcValue.UnderlyingSystemType == typeof(double) || ifcValue.UnderlyingSystemType == typeof(float) )
            {            
                var decimalValue = new AttributeDecimalValueType { DecimalValue = (double)ifcValue.Value, DecimalValueSpecified = true};
                attributeValueType.Item = decimalValue;
                attributeValueType.ItemElementName = ItemChoiceType.AttributeDecimalValue;
               
            }
            else if (ifcValue.UnderlyingSystemType == typeof(string))
            {
                var stringValue = new AttributeStringValueType { StringValue = ifcValue.ToString() };
                attributeValueType.Item = stringValue;
                attributeValueType.ItemElementName = ItemChoiceType.AttributeStringValue;
               
            }
            else if (ifcValue.UnderlyingSystemType == typeof(bool) || ifcValue.UnderlyingSystemType == typeof(bool?))
            {
                var boolValue = new BooleanValueType();
                if (ifcValue.Value != null && (bool) ifcValue.Value)
                {                 
                    var theBool = (bool) ifcValue.Value;
                    boolValue.BooleanValue = theBool;
                    boolValue.BooleanValueSpecified = true;
                }
                attributeValueType.Item = boolValue;
                attributeValueType.ItemElementName = ItemChoiceType.AttributeBooleanValue;
            }
            else
            {
                attributeValueType = null;
            }
            return attributeValueType;
        }
    }

    
}
