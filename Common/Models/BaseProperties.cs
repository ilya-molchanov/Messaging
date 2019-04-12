//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Common.Models
//{
//    public class BaseProperties
//    {
//        public BaseProperties(string propsArgs)
//        {
//            //inputFolders=C:\winserv\inputs\1;C:\winserv\inputs\2|scanInterval=5000|logPath=C:\winserv\scanner.log   
//            PropsArgs = propsArgs;
//            ParseProperties(propsArgs);
//        }

//        public BaseProperties(BaseProperties baseProperties)
//        {
//            Properties = baseProperties.Properties;
//            PropsArgs = baseProperties.PropsArgs;
//        }

//        public BaseProperties(IDictionary<string, string> properties)
//        {
//            Properties = properties;
//            PropsArgs = GetArgumentsLine(properties);
//        }

//        public IDictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();
//        public string PropsArgs { get; private set; }

//        public void Update(IDictionary<string, string> newValues)
//        {
//            if (newValues == null)
//            {
//                throw new ArgumentException("newValues");
//            }
//            Properties = Properties ?? new Dictionary<string, string>();
//            foreach (var newValue in newValues)
//            {

//                if (Properties.ContainsKey(newValue.Key))
//                {
//                    Properties[newValue.Key] = newValue.Value;
//                }
//                else
//                {
//                    Properties.Add(newValue);
//                }

//            }
//        }
//        public void Update(BaseProperties baseProperties)
//        {
//            if (baseProperties == null)
//            {
//                throw new ArgumentException("baseProperties");
//            }
//            Update(baseProperties.Properties);
//        }

//        protected void ParseProperties(string propsArgs)
//        {
//            var props = PropsArgs.Split('|');
//            foreach (var prop in props)
//            {
//                var splited = prop.Split('=');

//                if (splited.Length >= 2)
//                {
//                    Properties.Add(splited[0], splited[1]);
//                }
//            }
//        }

//        public static BaseProperties GetProperties(string[] args)
//        {
//            var propArgs = args.FirstOrDefault(x => x.StartsWith("-props:"));
//            if (propArgs == null)
//            {
//                return null;
//            }
//            propArgs = new string(propArgs.Skip("-props:".Length).ToArray());
//            var props = new BaseProperties(propArgs);

//            return props;
//        }


//        private string GetArgumentsLine(IDictionary<string, string> properties)
//        {
//            if (properties == null)
//            {
//                return "";

//            }
//            return string.Join("|", properties.Select(x => $"{x.Key}={x.Value}"));
//        }


//        public override string ToString()
//        {
//            return GetArgumentsLine(this.Properties);

//        }
//    }
//}
