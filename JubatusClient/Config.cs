namespace Jubatus.Client
{
    public class Config
    {
        public Config ()
        {
            method = string.Empty;
            converter = new ConverterConfig ();
        }

        public string method;
        public ConverterConfig converter;

        public object ToRPCObject()
        {
            return new object[] {method, converter.ToRPCObject()};
        }
    }
}
