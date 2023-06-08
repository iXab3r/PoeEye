using System.Xml.Linq;

namespace PoeShared.Scaffolding;

public static class XDocumentExtensions
{
    public static T AddTo<T>(this T element, XContainer parent)
    {
        parent.Add(element);
        return element;
    }
    
    public static T WithAttribute<T>(this T element, XName name, string value) where T : XElement
    {
        element.SetAttributeValue(name, value);
        return element;
    }
}