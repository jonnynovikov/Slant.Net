using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Slant.Net.Http.Response;

namespace Slant.Net.Http.Serialization
{
    /// <summary>
    /// XML deserialization
    /// </summary>
    public class XmlDeserializer : IDeserializer
    {
        private static IDeserializer _shared;

        public static IDeserializer Shared
        {
            get
            {
                if (_shared == null)
                {
                    return _shared = new XmlDeserializer();
                }
                return _shared;
            }
        }

        #region [ Members ]

        public string RootElement { get; set; }

        public string Namespace { get; set; }

        public string DateFormat { get; set; }

        public CultureInfo Culture { get; set; }

        #endregion

        public XmlDeserializer()
        {
            Culture = CultureInfo.InvariantCulture;
        }

        public T Deserialize<T>(IRestResponse response) where T : class
        {
            return Deserialize<T>(response.Body);
        }

        public T Deserialize<T>(string content)
        {
            if (string.IsNullOrEmpty(content))
                return default(T);

            var doc = XDocument.Parse(content);
            var root = doc.Root;

            if (RootElement.HasValue() && doc.Root != null)
            {
                root = doc.Root.Element(AsNamespaced(RootElement));
            }

            // autodetect xml namespace
            if (!Namespace.HasValue())
            {
                RemoveNamespace(doc);
            }

            var x = Activator.CreateInstance<T>();
            var objType = x.GetType();

            if (IsSubclassOfRawGeneric(objType, typeof (List<>)))
            {
                x = (T) HandleListDerivative(x, root, objType.Name, objType);
            }
            else
            {
                x = (T) Map(x, root);
            }

            return x;
        }

        protected virtual object Map(object x, XElement root)
        {
            var objType = x.GetType();
            var props = objType.GetProperties();

            foreach (var prop in props)
            {
                var type = prop.PropertyType;
                var typeIsPublic = type.IsPublic || type.IsNestedPublic;

                if (!typeIsPublic || !prop.CanWrite)
                    continue;

                var attributes = prop.GetCustomAttributes(typeof (DeserializeAsAttribute), false);
                XName name;

                if (attributes.Length > 0)
                {
                    var attribute = (DeserializeAsAttribute) attributes[0];
                    name = AsNamespaced(attribute.Name ?? prop.Name);
                }
                else
                {
                    name = AsNamespaced(prop.Name);
                }

                var value = GetValueFromXml(root, name, prop);

                if (value == null)
                {
                    // special case for inline list items
                    if (type.IsGenericType)
                    {
                        var genericType = type.GetGenericArguments()[0];
                        var first = GetElementByName(root, genericType.Name);
                        var list = (IList) Activator.CreateInstance(type);

                        if (first != null)
                        {
                            var elements = root.Elements(first.Name);
                            PopulateListFromElements(genericType, elements, list);
                        }

                        prop.SetValue(x, list, null);
                    }
                    continue;
                }

                // check for nullable and extract underlying type
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
                {
                    // if the value is empty, set the property to null...
                    if (value == null || string.IsNullOrEmpty(value.ToString()))
                    {
                        prop.SetValue(x, null, null);
                        continue;
                    }

                    type = type.GetGenericArguments()[0];
                }

                if (type == typeof (bool))
                {
                    var toConvert = value.ToString().ToLower();
                    prop.SetValue(x, XmlConvert.ToBoolean(toConvert), null);
                }
                else if (type.IsPrimitive)
                {
                    prop.SetValue(x, ChangeType(value, type, Culture), null);
                }
                else if (type.IsEnum)
                {
                    var converted = FindEnumValue(type, value.ToString(), Culture);
                    prop.SetValue(x, converted, null);
                }
                else if (type == typeof (Uri))
                {
                    var uri = new Uri(value.ToString(), UriKind.RelativeOrAbsolute);
                    prop.SetValue(x, uri, null);
                }
                else if (type == typeof (string))
                {
                    prop.SetValue(x, value, null);
                }
                else if (type == typeof (DateTime))
                {
                    if (DateFormat.HasValue())
                    {
                        value = DateTime.ParseExact(value.ToString(), DateFormat, Culture);
                    }
                    else
                    {
                        value = DateTime.Parse(value.ToString(), Culture);
                    }

                    prop.SetValue(x, value, null);
                }
                else if (type == typeof (DateTimeOffset))
                {
                    var toConvert = value.ToString();

                    if (!string.IsNullOrEmpty(toConvert))
                    {
                        DateTimeOffset deserialisedValue;

                        try
                        {
                            deserialisedValue = XmlConvert.ToDateTimeOffset(toConvert);
                            prop.SetValue(x, deserialisedValue, null);
                        }
                        catch (Exception)
                        {
                            object result;

                            if (TryGetFromString(toConvert, out result, type))
                            {
                                prop.SetValue(x, result, null);
                            }
                            else
                            {
                                // fallback to parse
                                deserialisedValue = DateTimeOffset.Parse(toConvert);
                                prop.SetValue(x, deserialisedValue, null);
                            }
                        }
                    }
                }
                else if (type == typeof (decimal))
                {
                    value = decimal.Parse(value.ToString(), Culture);
                    prop.SetValue(x, value, null);
                }
                else if (type == typeof (Guid))
                {
                    var raw = value.ToString();
                    value = string.IsNullOrEmpty(raw) ? Guid.Empty : new Guid(value.ToString());
                    prop.SetValue(x, value, null);
                }
                else if (type == typeof (TimeSpan))
                {
                    var timeSpan = XmlConvert.ToTimeSpan(value.ToString());
                    prop.SetValue(x, timeSpan, null);
                }
                else if (type.IsGenericType)
                {
                    var t = type.GetGenericArguments()[0];
                    var list = (IList) Activator.CreateInstance(type);
                    var container = GetElementByName(root, AsNamespaced(prop.Name));

                    if (container.HasElements)
                    {
                        var first = container.Elements().FirstOrDefault();
                        var elements = container.Elements(first.Name);

                        PopulateListFromElements(t, elements, list);
                    }

                    prop.SetValue(x, list, null);
                }
                else if (IsSubclassOfRawGeneric(type, typeof (List<>)))
                {
                    // handles classes that derive from List<T>
                    // e.g. a collection that also has attributes
                    var list = HandleListDerivative(x, root, prop.Name, type);
                    prop.SetValue(x, list, null);
                }
                else
                {
                    //fallback to type converters if possible
                    object result;

                    if (TryGetFromString(value.ToString(), out result, type))
                    {
                        prop.SetValue(x, result, null);
                    }
                    else
                    {
                        // nested property classes
                        if (root != null)
                        {
                            var element = GetElementByName(root, name);

                            if (element != null)
                            {
                                var item = CreateAndMap(type, element);
                                prop.SetValue(x, item, null);
                            }
                        }
                    }
                }
            }

            return x;
        }

        public XName AsNamespaced(string name)
        {
            return AsNamespaced(name, Namespace);
        }

        protected virtual XAttribute GetAttributeByName(XElement root, XName name)
        {
            var names = new List<XName>
            {
                name.LocalName,
                AsNamespaced(name.LocalName.ToLower()),
                AsNamespaced(StringUtils.ToCamelCase(name.LocalName, Culture))
            };

            return root.DescendantsAndSelf()
                .OrderBy(d => d.Ancestors().Count())
                .Attributes()
                .FirstOrDefault(d => names.Contains(d.Name.LocalName.RemoveUnderscoresAndDashes()));
        }

        protected virtual object GetValueFromXml(XElement root, XName name, PropertyInfo prop)
        {
            object val = null;

            if (root != null)
            {
                var element = GetElementByName(root, name);

                if (element == null)
                {
                    var attribute = GetAttributeByName(root, name);

                    if (attribute != null)
                    {
                        val = attribute.Value;
                    }
                }
                else
                {
                    if (!element.IsEmpty || element.HasElements || element.HasAttributes)
                    {
                        val = element.Value;
                    }
                }
            }

            return val;
        }

        protected virtual XElement GetElementByName(XElement root, XName name)
        {
            var lowerName = AsNamespaced(name.LocalName.ToLower());
            var camelName = AsNamespaced(StringUtils.ToCamelCase(name.LocalName, Culture));

            if (root.Element(name) != null)
            {
                return root.Element(name);
            }

            if (root.Element(lowerName) != null)
            {
                return root.Element(lowerName);
            }

            if (root.Element(camelName) != null)
            {
                return root.Element(camelName);
            }

            if (name == AsNamespaced("Value", name.NamespaceName))
            {
                return root;
            }

            // try looking for element that matches sanitized property name (Order by depth)
            return root.Descendants()
                .OrderBy(d => d.Ancestors().Count())
                .FirstOrDefault(d => d.Name.LocalName.RemoveUnderscoresAndDashes() == name.LocalName) ??
                   root.Descendants()
                       .OrderBy(d => d.Ancestors().Count())
                       .FirstOrDefault(d => d.Name.LocalName.RemoveUnderscoresAndDashes() == name.LocalName.ToLower());
        }

        private void PopulateListFromElements(Type t, IEnumerable<XElement> elements, IList list)
        {
            foreach (var element in elements)
            {
                var item = CreateAndMap(t, element);
                list.Add(item);
            }
        }

        protected virtual object CreateAndMap(Type t, XElement element)
        {
            object item;

            if (t == typeof (string))
            {
                item = element.Value;
            }
            else if (t.IsPrimitive)
            {
                item = ChangeType(element.Value, t, Culture);
            }
            else
            {
                item = Activator.CreateInstance(t);
                Map(item, element);
            }

            return item;
        }

        private object HandleListDerivative(object x, XElement root, string propName, Type type)
        {
            Type t;

            if (type.IsGenericType)
            {
                t = type.GetGenericArguments()[0];
            }
            else
            {
                t = type.BaseType.GetGenericArguments()[0];
            }

            var list = (IList)Activator.CreateInstance(type);
            var elements = root.Descendants(AsNamespaced(t.Name, Namespace));
            var name = t.Name;

            if (!elements.Any())
            {
                var lowerName = AsNamespaced(name.ToLower());
                elements = root.Descendants(lowerName);
            }

            if (!elements.Any())
            {
                var camelName = AsNamespaced(StringUtils.ToCamelCase(name, Culture));
                elements = root.Descendants(camelName);
            }

            if (!elements.Any())
            {
                elements = root.Descendants().Where(e => e.Name.LocalName.RemoveUnderscoresAndDashes() == name);
            }

            if (!elements.Any())
            {
                var lowerName = AsNamespaced(name.ToLower());
                elements = root.Descendants().Where(e => e.Name.LocalName.RemoveUnderscoresAndDashes() == lowerName);
            }

            PopulateListFromElements(t, elements, list);

            // get properties too, not just list items
            // only if this isn't a generic type
            if (!type.IsGenericType)
            {
                Map(list, root.Element(AsNamespaced(propName)) ?? root);
                // when using RootElement, the heirarchy is different
            }

            return list;
        }

        #region [ Helpers ]

        /// <summary>
        /// Returns the name of an element with the namespace if specified
        /// </summary>
        /// <param name="name">Element name</param>
        /// <param name="namespace">XML Namespace</param>
        /// <returns></returns>
        public static XName AsNamespaced(string name, string @namespace)
        {
            XName xName = name;

            if (!string.IsNullOrEmpty(@namespace))
                xName = XName.Get(name, @namespace);

            return xName;
        }

        private static void RemoveNamespace(XDocument xdoc)
        {
            foreach (var e in xdoc.Root.DescendantsAndSelf())
            {
                if (e.Name.Namespace != XNamespace.None)
                {
                    e.Name = XNamespace.None.GetName(e.Name.LocalName);
                }

                if (e.Attributes().Any(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None))
                {
                    e.ReplaceAttributes(
                        e.Attributes()
                            .Select(
                                a =>
                                    a.IsNamespaceDeclaration
                                        ? null
                                        : a.Name.Namespace != XNamespace.None
                                            ? new XAttribute(XNamespace.None.GetName(a.Name.LocalName), a.Value)
                                            : a));
                }
            }
        }

        private static bool TryGetFromString(string inputString, out object result, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);

            if (converter.CanConvertFrom(typeof (string)))
            {
                result = (converter.ConvertFromInvariantString(inputString));
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Checks a type to see if it derives from a raw generic (e.g. List[[]])
        /// </summary>
        /// <param name="toCheck"></param>
        /// <param name="generic"></param>
        /// <returns></returns>
        public static bool IsSubclassOfRawGeneric(Type toCheck, Type generic)
        {
            while (toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;

                if (generic == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public static object ChangeType(object source, Type newType, CultureInfo culture)
        {
            return Convert.ChangeType(source, newType, culture);
        }

        /// <summary>
        /// Find a value from a System.Enum by trying several possible variants
        /// of the string value of the enum.
        /// </summary>
        /// <param name="type">Type of enum</param>
        /// <param name="value">Value for which to search</param>
        /// <param name="culture">The culture used to calculate the name variants</param>
        /// <returns></returns>
        public static object FindEnumValue(Type type, string value, CultureInfo culture)
        {
            var ret = Enum.GetValues(type)
                .Cast<Enum>()
                .FirstOrDefault(v => v.ToString()
                    // На самом деле стоит проверять различные варианты имен
                    //.GetNameVariants(culture)
                    //.Contains(value, StringComparer.Create(culture, true)));
                    .Substring(0).HasValue());

            if (ret == null)
            {
                var enumValueAsUnderlyingType = Convert.ChangeType(value, Enum.GetUnderlyingType(type), culture);

                if (enumValueAsUnderlyingType != null && Enum.IsDefined(type, enumValueAsUnderlyingType))
                {
                    ret = (Enum)Enum.ToObject(type, enumValueAsUnderlyingType);
                }
            }

            return ret;
        }

        /// <summary>
        /// TODO: Check ICollection<>
        /// при десериализации, но используем List<> для упрощения
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsGenericList(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        // if needed, you can also return the type used as generic argument
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
    }
}