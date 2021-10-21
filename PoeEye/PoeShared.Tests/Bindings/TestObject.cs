using System;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Bindings
{
    public class TestObject : DisposableReactiveObject
    {
        public int intField;
            
        public string Id { get; set; }
            
        public int IntProperty { get; set; }
        
        public int ReadOnlyIntProperty { get; }
        
        public TestObject Inner { get; set; }

        public int PropertyThatThrows
        {
            get => Throw ? throw new NotSupportedException() : intField;
            set => intField = Throw ? throw new NotSupportedException() : value;
        }

        public bool Throw { get; set; } = true;
    }
}