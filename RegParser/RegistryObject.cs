using Microsoft.Win32;

namespace RegParser
{
    public class RegistryObject
    {
        public RegistryValueKind ValueKind { get; set; }
        public string KeyName { get; set; }
        public object Value { get; set; }

        public RegistryObject() { }

        public RegistryObject(string keyName, object value, RegistryValueKind valueKind)
        {
            KeyName = keyName;
            Value = value;
            ValueKind = valueKind;
        }
    }
}
