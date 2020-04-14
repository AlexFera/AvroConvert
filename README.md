
[![Nuget](https://img.shields.io/badge/Nuget-v2.4.1-blue?logo=nuget)](https://www.nuget.org/packages/AvroConvert)
[![Github](https://img.shields.io/badge/Downloads-6k-blue?logo=github)](https://github.com/AdrianStrugala/AvroConvert)

# AvroConvert

**Avro format combines readability of JSON format and compression of binary data serialization.**

The main purpose of the project was to enhance communication between microservices. Replacing JSON with Avro brought three main benefits:
* Decreased the communication time between microservices
* Reduced the network traffic by about 30%
* Increased communication security - the data was not visible in plain JSON text


## Documentation

Introduction article: https://xabe.net/why-avro-api-is-the-best-choice/
\
General information: http://avro.apache.org/
\
Wiki: https://cwiki.apache.org/confluence/display/AVRO/Index

## Why choose Avro?

Benchmark with comparison to Newtonsoft.Json:

| Converter               | Request time [ms]     | Compressed size [kB] |
|-------------------------|-----------------------|----------------------|
| Json                    | 1104                  | 9945                 |
| Avro (null encoding)    | 549                   | 2435                 |
| Avro (Headless)         | 503                   | 2434                 |
| Avro (Deflate encoding) | 519                   | 206                  |

In the purpose of introducing Avro API, I've written an article, which you can read here: https://xabe.net/why-avro-api-is-the-best-choice/
\
It contains also description of the format, detailed results of the benchmarks and implementation details.

**Conclusion:** <br>
Using Avro for communication between your services significantly reduces communication time and network traffic. Additionally choosing encoding (compression algorithm) can improve the results even further.

## Code samples

#### Serialization
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject);
```

Using encoding
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject, CodecType.Snappy);
```
Supported encoding types:
- Null (default)
- Deflate
- Snappy
- GZip



#### Deserialization

```csharp
//Using generic method
CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);

//Using dynamic method
CustomClass deserializedObject = AvroConvert.Deserialize(byte[] avroObject, typeof(CustomClass));
```

Deserialization when a property value is null, but schema contains information about default value
```csharp
//Model used for serialization
public class DefaultValueClass
{
    [DefaultValue("Let's go")]
    public string justSomeProperty { get; set; }

    [DefaultValue(2137)]
    public long? andLongProperty { get; set; }
}

//Deserializing object with null data
 DefaultValueClass deserializedObject = AvroConvert.Deserialize<DefaultValueClass>(byte[] avroObject);

//Produces following object:
> deserializedObject.justSomeProperty
> "Let's go"

> deserializedObject.andLongProperty
> 2137
```



#### Generating Avro schema for C# classes

Using simple class
```csharp

//Model
public class SimpleTestClass
{
	public string justSomeProperty { get; set; }

	public long andLongProperty { get; set; }
}


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(SimpleTestClass));


//Produces following schema:
"{"type":"record","name":"AvroConvert.SimpleTestClass","fields":[{"name":"justSomeProperty","type":["null","string"]},{"name":"andLongProperty","type":"long"}]}"
```

Using class decorated with attributes
```csharp
//Model
[DataContract(Name = "User", Namespace = "user")]
public class AttributeClass
{
	[DataMember(Name = "name")]
	public string StringProperty { get; set; }

	[DataMember(Name = "favorite_number")]
	[NullableSchema]
	public int? NullableIntProperty { get; set; }

	[DataMember(Name = "favorite_color")]
	public string AndAnotherString { get; set; }
}


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(AttributeClass));


//Produces following schema:
"{"type":"record","name":"user.User","fields":[{"name":"name","type":["null","string"]},{"name":"favorite_number","type":["null","int"]},{"name":"favorite_color","type":["null","string"]}]}"
```  



#### Reading Avro schema from Avro encoded object
```csharp
string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```

## License  

AvroConvert is licensed under [Attribution-NonCommercial-ShareAlike 3.0 Unported (CC BY-NC-SA 3.0)](https://creativecommons.org/licenses/by-nc-sa/3.0/) - see [License](LICENSE.md) for details. For commercial purposes purchase AvroConvert on website - [Xabe](https://xabe.net/product/avroconvert/)


## Contribution

We want to improve AvroConvert as much as possible. If you have any idea, found next possible feature, optimization opportunity or better way for integration, leave a comment or pull request. 
