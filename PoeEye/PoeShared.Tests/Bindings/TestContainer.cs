using PoeShared.Scaffolding;

namespace PoeShared.Tests.Bindings
{
    public sealed class TestContainer : DisposableReactiveObject
    {
        public int intField;
            
        public string Id { get; set; }
            
        public int IntProperty { get; set; }
        
        public TestContainer Inner { get; set; }
    }
}