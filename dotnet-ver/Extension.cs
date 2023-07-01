using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace dotnet_ver
{
    public static class Extensions
    {
        /// <remarks>
        /// Source: http://stackoverflow.com/a/14892813/1636276
        /// </remarks>
        public static XElement GetOrCreateElement(this XContainer container, string name)
        {
            var element = container.Element(name);
            if (element == null)
            {
                element = new XElement(name);
                container.Add(element);
            }
            return element;
        }


        public static XElement GetElement(this XContainer container, string name)
        {
            var element = container.Element(name);

            return element;
        }

    }
}
