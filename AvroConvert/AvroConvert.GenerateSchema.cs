﻿namespace AvroConvert
{
    using FastDeepCloner;
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            object inMemoryInstance = AddAvroRequiredAttributesToObject(obj.GetType());

            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(inMemoryInstance.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(inMemoryInstance, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();

            return result;
        }

        private static object AddAvroRequiredAttributesToObject(Type objType)
        {
            var inMemoryInstance = AddCustomAttributeToObject<DataContractAttribute>(objType);

             PropertyInfo[] properties = inMemoryInstance.GetType().GetProperties();
           var clonerProperties = DeepCloner.GetFastDeepClonerProperties(inMemoryInstance.GetType());

            //            var prop = 
            //            prop.Attributes.Add(new JsonIgnoreAttribute());

            foreach (var prop in properties)
            {
                var clonedAttribute = clonerProperties.Single(n => n.Name == prop.Name);
                clonedAttribute.Attributes.Add(new DataMemberAttribute());
               //   prop.SetValue(inMemoryInstance, AddCustomAttributeToObject<DataMemberAttribute>(prop.PropertyType));
               prop.SetValue(inMemoryInstance, clonedAttribute);

             //   prop.Attributes.Add(new DataMemberAttribute());

                if (!(prop.PropertyType.GetTypeInfo().IsValueType ||
                      prop.PropertyType == typeof(string)))
                {
                    //Its complex type

                    prop.SetValue(inMemoryInstance, AddCustomAttributeToObject<DataContractAttribute>(prop.PropertyType));
                    prop.SetValue(inMemoryInstance, AddAvroRequiredAttributesToObject(prop.PropertyType));
                }

              //  PropertyInfo originalProp = inMemoryInstance.GetType().GetProperty(prop.Name);
           //     PropertyInfo originalProp = inMemoryInstance.GetType().GetProperty(prop.Name);

           //     inMemoryInstance.GetType().pro

           //     originalProp.SetValue(inMemoryInstance, prop.GetValue(inMemoryInstance), null);
            }


            return inMemoryInstance;
        }



        private static object AddCustomAttributeToObject<T>(Type objType)
        {
            var assemblyName = new System.Reflection.AssemblyName("InMemory");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(objType.Assembly.GetName(),
                AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var typeBuilder = moduleBuilder.DefineType(objType.Name, System.Reflection.TypeAttributes.Public, objType);

            var attributeConstructor = typeof(T).GetConstructor(new Type[] { });
            var attributeProperties = typeof(T).GetProperties();

            var attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new string[] { }, attributeProperties.Where(p => p.Name == "Name").ToArray(), new object[] { objType.Name });

            typeBuilder.SetCustomAttribute(attributeBuilder);

            var inMemoryType = typeBuilder.CreateType();
            var inMemoryInstance = Activator.CreateInstance(inMemoryType);

            return inMemoryInstance;
        }
    }
}
