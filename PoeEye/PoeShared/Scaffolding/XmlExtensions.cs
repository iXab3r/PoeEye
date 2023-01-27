using System.Xml;
using System.Xml.XPath;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public static class XmlExtensions {
    
    public static void SetAttribute(this XPathNavigator nav, string localName, [NotNull] string namespaceUri, string value) {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException(nameof(namespaceUri));
        }

        if (!nav.MoveToAttribute(localName, namespaceUri)) {
            throw new XmlException($"Couldn't find attribute '{localName}'.");
        }
        nav.SetValue(value);
        nav.MoveToParent();
    }
}