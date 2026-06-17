using System;
using System.Reflection;
using System.Xml;

namespace RuntimeGC;

internal class DirectXmlToObject
{
	public static object ObjectFromXml(Type type, XmlNode xmlRoot, bool doPostLoad = true)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		object obj = Activator.CreateInstance(type);
		PropertyInfo[] properties = type.GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (xmlRoot[propertyInfo.Name] != null)
			{
				if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType == typeof(string))
				{
					propertyInfo.SetValue(obj, Convert.ChangeType(((XmlNode)xmlRoot[propertyInfo.Name]).InnerText, propertyInfo.PropertyType));
					continue;
				}
				XmlDocument val = new XmlDocument();
				val.LoadXml("<" + propertyInfo.Name + ">" + ((XmlNode)xmlRoot[propertyInfo.Name]).InnerXml + "</" + propertyInfo.Name + ">");
				XmlNode documentElement = (XmlNode)(object)val.DocumentElement;
				object value = ObjectFromXml(propertyInfo.PropertyType, documentElement);
				propertyInfo.SetValue(obj, value);
			}
		}
		if (doPostLoad)
		{
			MethodInfo method = type.GetMethod("DoPostLoad", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
			{
				method.Invoke(obj, null);
			}
		}
		return obj;
	}
}
